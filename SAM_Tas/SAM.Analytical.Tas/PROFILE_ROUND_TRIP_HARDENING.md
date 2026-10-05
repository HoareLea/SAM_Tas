# Profile round-trip hardening

Two pre-existing defects left open by `PROFILE_DEFINITION_REUSE.md` (PR #37), fixed together because both
live in the one profile identity seam the round-trip invariant states:

> After a profile has been canonicalised once, repeated SAM ↔ TAS round trips preserve stable profile
> identity and references: no progressive name growth, no dangling profile references, and no
> simulation-effective change.

1. **Export-side (naming):** the HDD sizing writer stamped one SAM profile name onto two
   differently-valued TAS profiles, so every SAM → TAS → SAM generation accreted one `_<hash>` suffix on
   the two affected names.
2. **Import-side (reference integrity):** `VentilationProfileName` was written for every imported
   internal condition, but the `ticV` profile behind it never reached the `ProfileLibrary`, so the
   reference always dangled.

Function-profile semantics (`ticFunctionProfile` and the hourly/yearly variants) are deliberately **not**
in scope: they are a representation gap, not an identity defect, and remain excluded from dedup exactly as
PR #37 left them. See "Out of scope" below.

---

## 1. HDD naming: one name must never carry two definitions

### Root cause

`Convert.ToTBD` always exports HDD sizing conditions (`UpdateZones(…, includeHDD: true)` →
`UpdateZone_HDD` → `AddInternalCondition_HDD` → **`Modify.UpdateInternalCondition_HDD`**). That writer
synthesises the HDD condition from the *space's* SAM `InternalCondition` and flattens two of its profiles
to single-value `ticValueProfile`s:

- `ticI` (infiltration): `value = InfiltrationAirChangesPerHour`, `factor = 1`;
- `ticLL` (heating): `value = profile.MaxValue`, `factor = 1`.

Both were named `profile.Name` — the name of the **full schedule** the normal condition carries. The TBD
therefore held, per space, `Cell 1`[ticI] = (name X, 24 hourly values) and `Cell 1 - HDD`[ticI] = (name X,
one value): one identity naming two definitions.

On the next import the reuse index legitimately registers both definitions with the same preferred base,
and because `ProfileDefinition.CompareTo` orders by value count, the one-value HDD definition claims the
bare name and the full schedule is discriminated to `X_<hash>`. The following export copies the renamed
profile's name onto *both* again, so the next import discriminates `X_<hash>` into `X_<hash>_<hash>` —
one suffix per generation, deterministic, name-only. Exactly two categories were affected (Infiltration,
Heating) because those are the only two slots the HDD writer touches — matching the licensed observation
that **2 of 20** names grew on the feature model.

### Fix

`UpdateInternalCondition_HDD` names the flattened content after itself, via `Query.ProfileName_HDD` —
`profile.Name + " - HDD"`, the same convention the HDD condition itself already carries
(`space.Name + " - HDD"`). The rule lives in one shared helper rather than inline at the writer's two write
sites so the COM-free naming test exercises the production rule instead of re-implementing it. The TBD then holds
`X` (full schedule) and `X - HDD` (flattened scalar): two names for two definitions. The import needs no
change — its discrimination stays as the safety net for genuinely same-named TAS-authored input (pinned
unchanged by `Naming_SameNameDifferentDefinitions_DiscriminatesDeterministically` and
`ModelA_TheTwoNameCollisionsAreDiscriminated`).

### Behaviour after the fix

- Import of a fixed export resolves `X` and `X - HDD` with **no** signature discriminator; a repeat
  export writes exactly those names back; the names are a **fixed point** across generations (pinned
  COM-free by `Naming_HDDFlattenedProfilesWithTheirOwnNames_ReachAStableFixedPoint`).
- A model already carrying discriminated names from a pre-fix export reshuffles **once**
  (`X_<hash>` → full schedule; `X_<hash> - HDD` → the flattened one) and is stable thereafter.
- Simulation-effective state is untouched: TAS profile names are diagnostic (PR #37's licensed A/B had
  52/432 profile-name differences with 0 simulation-effective differences), and the flattened HDD
  *values* (`InfiltrationAirChangesPerHour`, `MaxValue`) are byte-identical to before.

## 2. Ventilation: the reference and the library now agree

### Root cause

Commit `13c4284c` ("Ventilation Profile Implemented (WIP)", 2023-04-20) added both the import-side
reference (`Convert.ToSAM(TBD.InternalCondition)` writes `VentilationProfileName` +
`SupplyAirFlow`) and the export-side write (`UpdateInternalCondition` resolves
`ProfileType.Ventilation` and updates `ticV`) — but never added `ticV` to the library emitter
(`Convert.ToSAM_Profiles`). An unfinished WIP, not a design decision: `ticV` is an ordinary
internal-gain profile slot, read by the same `Core.Tas.Query.Values` flattening, with the category
mapping already in place (`ProfileType.Ventilation`, `InternalConditionParameter.VentilationProfileName`).
PR #37 pinned the dangling reference as baseline rather than change it.

The dangling reference meant the export's `internalCondition.GetProfile(ProfileType.Ventilation,
profileLibrary)` answered `null` for every imported model, so `Update(profile_TBD, …)` was skipped and
the exported `ticV` kept the fresh-`AddIC` TAS defaults — the source ventilation schedule was silently
dropped from the round trip. (PR #37's A/B could not see this: both sides dropped it identically.)

### Fix

- `Query.ProfileReuseIndex.ProfileSlots_InternalGain` gains `(ticV, ProfileType.Ventilation)`;
- `Convert.ToSAM_Profiles` (the legacy no-index collector) is driven by that same table, so the two
  collectors cannot disagree about which slots exist;
- `Convert.ToSAM(TBD.InternalCondition, …)` routes `VentilationProfileName` through the same
  `ProfileName(…)` helper as every other slot — canonical shared name with an index, legacy
  `"{IC} [{profile}]"` without one.

Ventilation definitions then dedupe by value exactly like the other seven gain slots, and the reference
resolves through the same `InternalCondition.GetProfile(ProfileType.Ventilation, profileLibrary)` lookup
the export uses (pinned COM-free by
`References_VentilationSlotIsCollected_SoItsReferenceResolves`).

### Behaviour after the fix

- The imported `ProfileLibrary` carries the ventilation profiles; `VentilationProfileName` resolves.
  On the licensed models this takes the unresolved-reference count from **4 (ModelA) / 36 (TM59 model)
  to 0**, and the library gains the distinct ventilation definitions those models carry (exact counts
  are a licensed-validation measurement — see below).
- The export now writes the imported ventilation schedule shape and the per-zone ACH factor instead of
  TAS defaults. That is a deliberate **simulation-effective restoration** for imported models — the one
  intended behavioural difference vs the pre-fix baseline — so the licensed acceptance compares the
  round-tripped TBD's `ticV` fields against the **source** TBD, not only baseline-vs-feature. (The same
  ticV field set — type/factor/value/setback/hourly/yearly — is already in the 852/5754-field dump.)
- Magnitude semantics follow every other slot: the export factor is recomputed per zone
  (`CalculatedSupplyAirFlow / volume * 3600`), exactly as native SAM models are already exported today.
  **This only round-trips if the import stores the source ticV FACTOR on the ACH basis** — see "The
  magnitude defect this PR first shipped" below, which is why the import writes
  `profile_TBD.factor` into `InternalConditionParameter.SupplyAirChangesPerHour`, and neither
  `GetExtremeValue(true)` nor `SupplyAirFlow`.
- Native SAM models are unaffected (their ventilation references already resolved). The TIC import path
  keeps its legacy per-condition reference name (it has no reuse index, like all its other slots), but it
  shared the unit mis-mapping and took the same one-line correction — see below.

## The magnitude defect this PR first shipped

The first licensed acceptance run of this branch **failed**, and it is recorded here rather than tidied
away, because the reason it failed is the reason the ventilation fix has two halves instead of one.

**What the first attempt got right.** Collecting `ticV` took the unresolved ventilation references from
4 (ModelA) / 36 (TM59) to **0**, deduped all 4 / 36 slots onto a single reusable definition (library
20 → 21 and 30 → 31), and restored the imported profile's **type and shape** exactly. Non-ticV
simulation-effective fields were untouched (792 and 5346 fields, 0 differences) and both real models
simulated identically to baseline (227,760 and 1,024,920 values, 0 differences).

**What it got wrong.** Neither licensed model carries a non-zero `ticV` (both are
`factor 1.0, value 0.0` — ventilation off), so their round trip is simulation-inert and the defect was
invisible on them. Authoring a source `ticV` of **2.0 ACH** (hourly, 0.25/1.0/0.5) exposed it:

| | type | factor | effective peak |
| --- | --- | --- | --- |
| source | `ticHourlyProfile` | 2.0 | **2.0 ACH** |
| pre-fix baseline round trip | `ticValueProfile` | 1.0 | 0 — schedule lost |
| **first-attempt** round trip | `ticHourlyProfile` ✓ | 40.8 | **40.8 ACH — 20.4×** |
| corrected round trip | `ticHourlyProfile` ✓ | 2.0 | **2.0 ACH** ✓ |

**Root cause.** `profile.GetExtremeValue(true)` on a `ticV` slot is a peak **air change rate**. The import
stored it in `InternalConditionParameter.SupplyAirFlow`, declared `[m3/s]`. `CalculatedSupplyAirFlow` then
read 2.0 as 2.0 m³/s, and the export's own `/ volume * 3600` scaled it by 3600/volume — 18× on a 200 m³
zone. The arithmetic closes exactly: `(0.008 × 6.667 + 2.0) / 200 × 3600 = 40.8`, where the 0.008 × 6.667
term is the per-person basis that alone yields the 4.8 seen when `ticV` is zero.

This was **dormant** while the ventilation reference dangled — the export could not resolve the profile,
so it never wrote the factor at all. Collecting `ticV` is precisely what made it live. Judging the change
only against the pre-fix baseline could never have caught it: baseline was 0 ACH, the first attempt was
40.8 ACH, and both are wrong against the source.

**The correction.** Two sites for the unit half, plus a second, independent magnitude defect that review
found on the same line:

- `Convert/ToSAM/InternalCondition.cs` — the ticV peak now goes to
  `InternalConditionParameter.SupplyAirChangesPerHour`, declared `[ACH]`. The export's
  `/ volume * 3600` is the exact inverse of that basis's `rate × volume / 3600`, so the rate returns
  unchanged **whatever the zone volume**. Applied to both overloads (TBD and TIC), which shared the
  mis-mapping.
- `Modify/UpdateInternalConditionTemplate.cs` — a template has no space, so whichever parameter it reads
  becomes the factor verbatim. It now prefers `SupplyAirChangesPerHour`, falling back to `SupplyAirFlow`
  so a SAM-authored template carrying only the legacy m³/s parameter keeps the factor it has always been
  given. Without the preference an imported template would have silently written a **zero** ventilation
  factor once the import stopped writing `SupplyAirFlow`.

`CalculatedSupplyAirFlow` itself is untouched: the rate is routed to the basis that already inverts
correctly rather than compensated for downstream.

**The second defect: the factor, not the peak.** This was found in review after the unit fix, and it was real. The
first correction stored `GetExtremeValue(true)` — `factor * max(values)` — on the ACH basis. But the imported
`Profile` keeps the **raw** values, and `Modify.Update` writes the basis back as `profile_TBD.factor` and
re-applies those same values, so the schedule is scaled **twice**: `factor * max^2`. It is invisible whenever
the profile is normalised to a peak of 1 — the usual TAS convention, and exactly what the first authored
oracle used, which is why that oracle passed while this was live. A source with `factor 2.0` and a peak value
of `0.5` (1.0 ACH) round-tripped as **0.5 ACH**, measured under licence. The import therefore stores
`profile_TBD.factor` itself. Licensed result: that same non-normalised source now round-trips
**1.0 ACH → 1.0 ACH exactly**, and the normalised 2.0 ACH oracle is unchanged. On the real models nothing
moves in simulation (`ticV` values are zero, so `factor × values` is zero either way — ModelA re-simulated at
**0 differences in 227,760 values**), while TM59's ticV factor agreement with the source improves from
26/54 to **44/54**.

**Licensed result of the correction.** With the ACH basis as the only specified basis, the round trip is
source-exact: factor 2.0 → 2.0, shape preserved, and the simulation matches the source to within the
pre-existing geometry/solar round-trip noise (max 909.7 W, on `solarGain`, the same value and hour the
zero-ventilation models already show). `infVentGain` agrees to **0.003 %**; peak hourly heating error
fell from **69,950 W to 326 W**. Both real models remain **0 differences** against baseline (227,760 and
1,024,920 values), and TM59 factor agreement with the source improved from 18/54 to 26/54.

**Known remaining deviation, and why it is not this fix** (raised again as a P1 in review, and answered
here rather than changed).** TAS's `internalGain.freshAirRate` is inert in
a TBD simulation — measured directly: 40 l/s/p vs 0 l/s/p over an otherwise identical model gives
**0 differences in 227,760 values**. SAM nevertheless imports it as `SupplyAirFlowPerPerson`, a real
design basis, and `CalculatedSupplyAirFlow` is purely **additive** over all four bases. So a source
carrying both 2.0 ACH and a (dormant) 8–40 l/s/p rate round-trips to 2.0 ACH *plus* that rate — 6.8 ACH
on ModelA's 40 l/s/p, ~1 ACH extra on a typical 8 l/s/p model. That is the established export design,
active for native SAM models long before this PR, and narrowing it to the ACH basis would silently drop
ventilation for every SAM model that expresses it in m³/s. Whether importing a TAS field that TAS ignores
should feed a live SAM basis is a separate question, deliberately not answered here.

## Zero-length ticV: collected only when it has a complete value representation

This was raised in review (P2), and it was a real regression of this branch's own making.

**The path.** `Core.Tas.Query.Values` has no `case` for `ticFunctionProfile`, so a TAS function profile
flattens to **zero values**. For a zero-length profile `ProfileDefinition.IsReusable` is false, and PR #37's
exclusion branch — correctly, for the eleven slots it was written for — still adds a library entry under the
profile's legacy `"{internal condition} [{profile}]"` name and lets the slot answer that name. Adding `ticV`
to the collectors therefore did something new: `VentilationProfileName` resolved to a zero-value profile for
the first time. The export's `GetProfile(ProfileType.Ventilation, profileLibrary)` returned non-null, the
ordinary value writer ran, and in `Modify.Update` a `Count == 0` profile slips past the `Count == -1` guard
and lands in the `Count <= 24` branch — replacing the TAS function profile with 24 hourly values.

Before PR #38 this could not happen: `ticV` was in neither collector, so nothing named the reference and it
simply dangled. **That dangling reference is the safe deferred behaviour**, and it is what the guard restores.

**The guard.** `Query.IsCollectableSlot(int slot, IEnumerable<double> values)` — true for every slot except
`ticV`, and for `ticV` only when the flattened values are non-empty. Both collectors consult it: the reuse
index's registration helper (`Query/ProfileReuseIndex.cs`) and the legacy mirror
(`Convert/ToSAM_Profiles`). A refused `ticV` is registered nowhere, so the index answers no name for the
slot, no library entry exists under one, `Convert.ToSAM`'s `ProfileName` helper falls back to the legacy
name, that name resolves to nothing, and the export never reaches `Modify.Update`. The TAS function profile
survives untouched.

**Reserving the name it leaves dangling.** Skipping the slot is not sufficient on its own, as review
pointed out. The dangling reference still *has* a name — the legacy `"{internal condition} [{profile}]"` the
conversion falls back to — and `Resolve` assigns canonical names from the source TAS names. If some ordinary
`ticV` definition's source name were that identical string, it could be handed that name, and the reference
intended to dangle would start resolving to an unrelated value profile, which the export would then write
over the function profile. That is realistic rather than theoretical: a round-tripped model's TAS profile
names **are** `"{condition} [{profile}]"` strings, because that is what `Modify.Update` writes back. So the
skip path calls `ProfileReuseIndex.Reserve(category, name)` — a claim with no definition, no library entry and
no slot able to answer it — and `Resolve` seeds its claim set from those reservations alongside the excluded
names, before any canonical name is assigned. Same claim-first discipline PR #37 already used for the
excluded names.

Deliberately **`ticV`-scoped**: the other eleven slots keep PR #37's exclusion behaviour exactly, because
their references already resolved and already round-tripped that way — changing them is function-profile
work, not this fix. `Modify.Update`'s dead `Count == -1` guard is likewise left alone; the guard here stops
a zero-length profile ever reaching it through the newly-collected slot, which is the whole of the
regression. **This closes the P2 without attempting function support**: function semantics remain deferred,
and nothing here brings a function profile into the value export path — it keeps one out.

**One table, both collectors.** Review also noted that the legacy `Convert.ToSAM_Profiles` mirror repeated
the twelve slots by hand, so a slot could be emitted by one collector and not the other — a reference naming
a definition the library does not carry. Both now `foreach` over the same
`Query.ProfileSlots_InternalGain` / `ProfileSlots_Thermostat` tables and share the same
`Query.IsCollectableSlot` gate, so they cannot drift and the single slot-table assertion pins both. Verified
behaviour-neutral under licence: 0 differences across every field on both real models and the authored
sources, and byte-identical SAM-side import dumps.

**A second collision, from a follow-up review.** Reserving the name closes a coincidental STRING
collision between two DIFFERENT internal conditions, but not a slot-KEY collision: two TBD internal
conditions can share the exact same NAME (a duplicate space name, a generic template) while disagreeing on
`ticV` — one a genuine schedule, the other a zero-length function profile. Both then register under the
identical `SlotKey(internalConditionName, ticV)`, and `Reserve` never touches that dictionary, so the
skipped condition's own slot-key lookup could still answer with the ordinary condition's canonical name.
`Register` gained an overload that adds `bool suppressLibraryEntry`, kept as a SEPARATE seven-argument method rather than an optional parameter on the existing one - an optional-parameter default is compile-time only, so it would not have preserved the original six-argument CLR method a pre-compiled plugin (Grasshopper/Revit, referencing this DLL as a binary) had already linked against, and that plugin would throw `MissingMethodException` the moment it loaded an upgraded SAM_Tas build. The six-argument overload now forwards to the seven-argument one with the suppression off, identical to its previous body. A skipped `ticV` still calls the SAME logic every other excluded slot uses - running the SAME ambiguity tracking - and only the final library-emission step is skipped. Verified by first reverting to the Reserve-only behaviour and confirming the new test
fails exactly as predicted (`GetProfileName("Duplicate", ticV)` answered the ordinary profile's name
instead of null), then restoring the fix and confirming it passes.

Pinned COM-free by seven tests in `ProfileDefinitionReuseTests.cs`
(`ZeroLength_Ventilation_IsNotCollectable_…`, `ZeroLength_Ventilation_NotCollected_LeavesTheReferenceDanglingExactlyAsBefore`,
`ZeroLength_ProfileCountIsZero_WhichIsWhyResolvingItWouldCorruptTheFunctionProfile`,
`ZeroLength_NonVentilationSlots_KeepTheirPR37ExclusionBehaviour`,
`ZeroLength_Ventilation_ReservesItsLegacyName_SoNoCanonicalDefinitionCanClaimIt`,
`ZeroLength_Ventilation_SlotKeyCollision_WithOrdinaryTicVOnSameNamedCondition_StaysDangling`,
`Reserve_AfterResolve_IsRefused`). Neither licensed model contains a
function profile, so the licensed A/B was **not** rerun for this guard; a focused licensed check confirmed
normal `ticV` is byte-identical to the accepted build (792 non-ticV and 56 ticV fields, 0 differences on
both authored variants) and the 2.0 ACH → 2.0 ACH oracle still holds.

## Out of scope (unchanged from PR #37, recorded so they are not rediscovered)

- **Function profiles.** Still excluded from dedup (a zero-value flattened form is an incomplete
  identity), still legacy-named per condition, and — for `ticV` — now explicitly refused a resolvable
  library entry so the value exporter can never reach one (see "Zero-length ticV" above). The full semantics — importing `profile.function` into
  `LightingControlFunction`/`VentilationFunction`, reading hourly/yearly values behind the function
  *variants* in `Core.Tas.Query.Values`, and the re-export of an imported function profile (today:
  `Modify.Update(TBD.profile, …)` writes 24 NaN hourly values for a zero-count profile, its
  `Count == -1` guard being dead) — are deferred to their own task; neither licensed model exercises a
  non-empty function profile. SAM has no home for a function string outside Lighting/Ventilation, so a
  faithful general representation is a SAM-side question, not a SAM_Tas one.
- **Two adjacent export defects found during this investigation**, both pre-existing and both left for
  the function-profile task because they only bite on function-bearing models:
  `UpdateInternalCondition`'s `VentilationFunctionSetback`/`VentilationFunctionFactor` writes are guarded
  by `double.IsNaN(…)` instead of `!double.IsNaN(…)` and therefore never fire; and the template path
  (`UpdateInternalConditionTemplate`) never writes `LightingControlFunction`/`VentilationFunction` at all.
- ~~The `InternalConditionParameter.SupplyAirFlow` unit muddle.~~ **This was wrong, and licensed
  validation disproved it.** Calling it out of scope assumed it stayed dormant; collecting ticV is
  precisely what woke it. It is now fixed — see the next section.
- TBD `InternalCondition` sharing, construction naming, and the occupancy ticOSG/ticOLG single-reference
  fold (SAM models one Occupancy profile reference; both TBD slots' definitions are in the library, but
  the condition references ticOLG's — a SAM representation limit, unchanged).

## Tests

`SAM.Analytical.Tas.TM59.Tests/ProfileDefinitionReuseTests.cs`:

- `References_VentilationSlotIsCollected_SoItsReferenceResolves` — replaces the PR #37 baseline pin.
  The production slot tables now carry `ticV` (twelve slots); a shared ventilation schedule dedupes to
  one definition; the resolved name resolves through the export's own `GetProfile` lookup.
- `Naming_HDDFlattenedProfilesWithTheirOwnNames_ReachAStableFixedPoint` — two-generation fixture over
  the post-fix export naming: no discriminator appears and generation 2 reproduces generation 1 exactly.
- The PR #37 legacy-input guarantees stand unchanged: a pre-fix (or TAS-authored) TBD whose HDD profiles
  share the design profile's name still discriminates deterministically
  (`ModelA_TheTwoNameCollisionsAreDiscriminated`,
  `Naming_SameNameDifferentDefinitions_DiscriminatesDeterministically`).

`SAM.Analytical.Tas.TM59.Tests/VentilationAirflowMagnitudeTests.cs` (11 tests) — the COM-free guard that
would have caught the magnitude failure before TAS was run. A Space plus an InternalCondition is enough
to drive the whole SAM airflow calculation, and the export's conversion is one line of arithmetic:

- `Units_SupplyAirFlowIsVolumeFlow_AndSupplyAirChangesPerHourIsARate` — pins the declared `[m3/s]` /
  `[ACH]` units the whole correction rests on.
- `ImportedTicVPeak_StoredAsAirChangesPerHour_ExportsBackAsTheSameRate` and
  `ImportedTicVPeak_OnTheAirChangesBasis_IsVolumeIndependent` — 2.0 ACH returns as 2.0 for volumes of
  50 / 200 / 1234.5 m³.
- `ImportedTicVPeak_StoredAsSupplyAirFlow_IsInflatedByTheVolumeRatio` and
  `TheTwoStorageChoices_DisagreeByTheVolumeRatio_NotByRounding` — reproduce the defect exactly (36 ACH on
  a 200 m³ zone) and assert that `SupplyAirFlow = 2.0` is **not** equivalent to
  `SupplyAirChangesPerHour = 2.0`.
- `CalculatedSupplyAirFlow_SumsEveryBasis_ItDoesNotSelectOne` — pins the additive combination rule (no
  precedence, no `max()`), so the residual per-person term can never be misattributed to the unit bug
  again; `..._WithNoBasisSpecified_IsNaN_SoTheExportFallsBackToFactorOne` pins the untouched case.
- `TemplateCondition_HasNoVolume_SoItsTicVFactorIsTheAirChangesValueItself` and
  `TemplateCondition_CarryingOnlyTheLegacyFlowParameter_StillUsesIt` — pin both halves of the template
  path's parameter preference, including that native SAM-authored template ventilation is not dropped.

## Validation

- `SAM_Tas.sln` builds with **0 errors** in Debug and Release (Framework MSBuild; only the pre-existing
  MSB3270/MSB3277 warnings).
- `SAM.Analytical.Tas.TM59.Tests`: **521/521** in Debug and Release (498 + 17 ventilation-magnitude
  + 6 zero-length/reservation/slot-key guards). `SAM.Analytical.Tas.Benchmark.Tests`: **16/16** in Debug
  and Release.
- **Licensed TAS A/B: run, and it changed the PR.** One-DLL-swap isolation (67 files, exactly 1
  differing) over three builds — baseline `610696e`, first attempt `e2e88ca`, corrected head — each
  identified by reflecting its own production slot table, with the corrected build reproducing
  byte-identically under `-t:Rebuild`.
  - Unresolved ventilation references **4 → 0** (ModelA) and **36 → 0** (TM59); library **20 → 21** and
    **30 → 31**; every ticV slot deduped onto one definition; counts stable across generations.
  - HDD name fixed point: baseline accretes one `_<hash>` **per generation** without converging
    (24 differing lines/generation on ModelA, 156 on TM59); the fix reaches generation 2 == generation 3
    == generation 4, byte-identical, with zero nested growth.
  - Non-ticV simulation-effective fields: **0 differences** in 792 (ModelA) and 5346 (TM59). Zone-GUID
    churn is noise — a same-DLL control differed in nothing else.
  - Both real models: **0 differences** against baseline in 227,760 and 1,024,920 simulated values,
    before and after the correction.
  - Authored 2.0 ACH source oracle: see "The magnitude defect this PR first shipped" above.
