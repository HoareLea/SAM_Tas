# Profile definition reuse (TBD → SAM import)

Value-based deduplication of the SAM `Profile` definitions a TBD import creates.

## The problem

A SAM `Profile` is a **library-level reusable definition**. A native SAM model already shares one
`ProfileLibrary` entry across every `InternalCondition` that references it.

The TBD import did not. `Convert.ToSAM_Profiles` minted one SAM `Profile` per TBD
internal-condition profile slot and named it `"{internal condition} [{profile}]"`, and
`Convert.ToSAM(TBD.InternalCondition, double)` wrote the matching reference. A two-zone building
carrying one activity therefore produced two copies of every schedule, and the name of each copy
stated a **place** rather than a **shape** — which is what made sharing impossible: a name carrying
a zone cannot be found again by the next zone that needs the same profile.

`ModelA-Tas.sam` shows the shape of it: 44 collected slots, 42 library entries (two pairs silently
overwrote one another by library key), **20 distinct `(Category, flattened Values)` definitions**.

## The architecture

```
TBD zone-local IC / profile slots        (as many as TAS states — unchanged)
        ↓
imported SAM InternalConditions          (one per TBD internal condition — unchanged)
        ↓
shared SAM ProfileLibrary definitions    (one per distinct definition — this change)
```

Physical or zone identity never becomes part of reusable profile-definition identity.

## Equality — `Classes/ProfileDefinition.cs`

Two import-produced profiles are the same reusable definition when they agree on:

* the SAM **`Category`** string, compared ordinally — the raw category, not merely the resolved
  `ProfileType`, so two categories that resolve to one profile type but read differently stay two
  definitions;
* the **complete flattened values**, read through `Core.Tas.Query.Values` (the same flattening
  `Profile.GetValues()` performs, so the equality stays correct even for a range-encoded profile);
* the **value count**, which is part of identity in its own right — a one-value profile and a
  24-value profile of the same number are different shapes and TAS writes them back as different
  profile types.

Values are compared by **exact IEEE-754 bit pattern**, with two normalisations applied on the way in:

* `-0.0` → `0.0`. The simulation cannot tell them apart, and leaving the sign in would let two
  equal definitions hash differently.
* every NaN → the one canonical `double.NaN` pattern, so a definition carrying a NaN equals itself
  and signs deterministically. Raw IEEE NaN semantics give neither.

No tolerance is applied: both sides come from the same TAS read, so a tolerance could only ever
merge two profiles the model states as different.

### Zero-length (TAS function) profiles are out of scope

`Core.Tas.Query.Values` returns no values at all for a function profile, so its flattened form is an
**incomplete representation** of it and merging by that would be unsafe. Those keep today's
per-internal-condition name and today's library entry, unchanged. Their names are claimed *before*
any canonical name is assigned, so a canonical name can never displace one.

Fixing the function-profile import limitation is separate work and is not attempted here.

## Deterministic naming — `Query/ProfileName.cs`

> Every distinct reusable profile definition receives a deterministic unique SAM library name,
> independent of traversal order and of physical/zone identity.

1. **Preferred base** — the ordinal-smallest normalised source TAS profile name in the definition's
   equality group. Normalisation trims, collapses internal whitespace and drops control characters;
   it **keeps underscores**, unlike `Query.ApertureTypeNameBase`, because real TAS profile names
   carry them (`HTG_7to19_21`). A generated name is therefore not required to be decomposable —
   uniqueness comes from the claim set, not from a grammar.
2. **`<base>_<signature hash>`** when the base is already claimed within the same category by a
   different definition. The hash is `Query.ProfileSignatureHash`, i.e. FNV-1a over
   `Query.ProfileSignature`, which carries the exact value bit patterns. Never a UI-style `(1)`/`(2)`
   counter.
3. **`<base>_<signature hash>_<k>`**, `k` counting from 2, when even that is claimed.

It **never refuses, never drops a valid profile and never overwrites an existing definition** —
unlike the aperture-type case, where refusing was right because the alternative was writing over an
object the export did not author. Here every candidate is a fresh library entry.

Determinism therefore rests on the order definitions are offered in, so `ProfileReuseIndex.Resolve`
claims them in `ProfileDefinition.CompareTo` order — category ordinally, then value count, then
value bit patterns — a genuine total order derived from the definitions alone. Reverse the building
walk, or import twice, and the names are identical. Every comparison and ordering that affects
output uses `StringComparer.Ordinal` / `string.CompareOrdinal`; nothing depends on the current
culture.

`Query.ProfileSignature` is a **bounded fingerprint** (`C<category hash> N<count> V<value hash>`),
deliberately not injective — a yearly profile carries 8760 values and a name discriminator has to
stay short. Rule 3 is what a fingerprint collision falls through to. Nothing about **reuse** rests
on it; reuse is full definitional equality.

## One index for the whole conversion — `Classes/ProfileReuseIndex.cs`

`Query.ProfileReuseIndex(TBD.Building)` reads every collected slot **once** over COM and resolves
the definitions and names. The same instance is then threaded through:

* `Convert.ToSAM_ProfileLibrary(TBD.Building, ProfileReuseIndex)`
* `Convert.ToSAM(TBD.Building, Dictionary<string, Polygon3D>, ProfileReuseIndex)` →
  `Convert.ToSAM(TBD.zone, out …, ProfileReuseIndex)` →
  `Convert.ToSAM(TBD.InternalCondition, double, ProfileReuseIndex)`
* **`Modify.AddUnusedInternalConditions(AdjacencyCluster, TBD.Building, ProfileReuseIndex)`**

The last one matters. With `importUnused: true` that path converts the building-level internal
conditions no zone owns. Before this change it called `internalCondition_TBD.ToSAM()` with no index
at all, so those templates would have kept legacy `"{IC} [{profile}]"` references while the library
carried canonical names — dangling references on exactly the conditions least likely to be noticed.

Lookup is `(internal condition name, TBD profile slot) → resolved name`, so the conversion pays no
second COM read. A name is not an identity, though: if one slot key would have to stand for two
different things, the key is marked **ambiguous and answers nothing at all**. All three ways that
happens are handled the same:

* two TBD internal conditions sharing a name and disagreeing on that slot's shared definition;
* a slot that is a shared definition on one condition and a zero-length passthrough on another;
* a slot that is a zero-length passthrough on both, but under **different** legacy names — e.g. two
  `"Duplicate"` conditions whose `ticLG` profiles are `Daylight` and `Dimmer`, which legacy import
  writes as `Duplicate [Daylight]` and `Duplicate [Dimmer]`.

Answering *either* would be a wrong reference on the other side. The third case is the subtle one: the
name it would answer with names a real library entry, so the result is a **silent misreference** rather
than a dangling one. Answering nothing instead sends both callers down the fallback chain, which is
right for both — a shared definition resolves definitionally, and a zero-length one falls through to
its own legacy name, which the library still carries as its own entry.

`ProfileReuseIndex` and `ProfileDefinition` **touch no COM type**, which is what makes the whole
reuse and naming decision testable without an installed TAS.

## Backward compatibility

Every new parameter is optional and defaults to `null`, which reproduces today's behaviour exactly:

* `index != null` → canonical shared profile references;
* `index == null` → the legacy `"{IC} [{profile}]"` naming.

`Convert.ToSAM_ProfileLibrary(TBD.Building)` is untouched and still builds the legacy library.

## Deliberately unchanged

* **`ticV` / `VentilationProfileName`** — *this limitation is now closed.* At the time of this PR the
  import wrote the reference but had never emitted the ventilation profile behind it, so the slot was
  not collected and the reference kept its legacy name, pinned as baseline by
  `References_VentilationSlotIsNotCollected_…`. The follow-up profile round-trip hardening work
  (`PROFILE_ROUND_TRIP_HARDENING.md`) collects `ticV` like every other gain slot, and
  `References_VentilationSlotIsCollected_SoItsReferenceResolves` pins the fixed behaviour.
* **TBD `InternalCondition` sharing**, opaque `BuildingElement` reuse, `Construction` naming, and
  the function-profile import semantics.
* **Native SAM library semantics.** After this change, editing one shared `ProfileLibrary`
  definition affects every `InternalCondition` referencing it. That is what a SAM library is, and it
  is what a native SAM model already does. No aperture-style split/rebind is introduced.

## Export equivalence

`Modify.UpdateInternalCondition` derives the TAS profile's **type, factor and values** from the SAM
profile's values and the zone's own area/gains — the shared SAM profile supplies reusable shape and
values only, and zone/IC-local TAS state stays independently derived. The only export-visible
consequences of a renamed shared profile are diagnostic:

* `profile_TBD.name` and `profile_TBD.description` (both set from `Profile.Name` in `Modify.Update`);
* `thermostat.name`, which is the four thermostat profile names joined with `" & "`.

The required invariant is **simulation-effective TAS state unchanged**, not byte-identical TBD
output.

## Tests

`SAM.Analytical.Tas.TM59.Tests/ProfileDefinitionReuseTests.cs` — 29 COM-free tests: equality
(category, values, bit stability, signed zero, NaN determinism, value count, zero-length exclusion),
naming (canonical base, ordinal-smallest source name, first and extended discriminators, reversed
order, repeated build, ordinal not culture-aware), reference integrity (every slot resolves to
exactly one library entry with the right category and complete values, resolution through the same
`InternalCondition.GetProfile` lookup the export uses, the template path, the ventilation baseline,
and the excluded zero-length path's own ambiguity handling), plus the `ModelA-Tas` 42 → 20 regression
and its two known name collisions
(`Infiltration::Constant`, `Heating::HTG_7to19_21`).

## Licensed acceptance (EDSL Tas, 2026-08-23)

Run on a machine with a licensed EDSL Tas install (`TBD.Document` → `C:\PROGRA~1\ENVIRO~1\Tas\TBD.exe`).
One-DLL swap: two folder copies of the same harness, **67 files, verified hash-identical except
`SAM.Analytical.Tas.dll`**. The input `.tbd` for each comparison is generated **once, with the baseline
DLL**, so both sides import byte-identical TAS input.

Two rounds, because PR #36 merged into `sow/2026-Q3` mid-validation:

| round | baseline (A) | feature (B) |
|---|---|---|
| 1 | `2950b27c` (after PR #35) | `d5ba1082` |
| 2 | `03f97570` (after PR #36 — the current merge-base) | `95dabb6b` (PR #36 merged in) |

### Models

Both are real models, not synthetic TAS objects.

* **`ModelA-Tas.sam`** — 2 spaces, 4 internal conditions, 44 collected slots, 42 library entries.
  Exercises infiltration, lighting, occupancy (latent + sensible), equipment sensible + latent,
  pollutant, heating, cooling, humidification, dehumidification, and carries both normal and **HDD**
  internal conditions.
* **`SAM_zoningAM_v2zonesisDomestic.sam`** — a real TM59 residential project: **9 spaces, 27 internal
  conditions, 396 slots**, internal conditions genuinely shared across spaces (one bathroom condition on
  three spaces, one bedroom condition on two, one kitchen condition on two).

### Result

| | ModelA-Tas | TM59 project model |
|---|---:|---:|
| SAM `ProfileLibrary` entries, baseline → feature | **42 → 20** | **369 → 30** |
| spaces / internal conditions (both sides) | 2 / 4 | 9 / 27 |
| profile slots compared, key sets identical | 44 | 396 |
| SAM-side fields compared (`RESOLVED`, category, count, complete values) | 176 | 1584 |
| **SAM-side semantic differences** | **0** | **0** |
| reference-name differences (expected) | 40 | 360 |
| **TAS-side simulation-effective fields compared** | **852** | **5754** |
| **TAS-side simulation-effective differences** | **0** | **0** |
| hourly TSD values compared | **227 760** (2 zones × 13 series × 8760) | **1 024 920** (9 × 13 × 8760) |
| **hourly values differing** | **0** | **0** (dumps SHA-256 identical) |

Round 2 reproduced the TM59 numbers exactly: 369 → 30, 1584 SAM fields / 0 differences, 5754
simulation-effective fields / 0 differences, and the **same** TSD dump hash as round 1.

The TAS-side dump reads the TBD with TAS's own `Get*(index)` accessors, not through the SAM_Tas helpers
under test, and every numeric field is compared as its **exact IEEE-754 bit pattern**. Fields covered per
internal condition: `description`, `includeSolarInMRT`, both emitters' `name`/`radiantProportion`/
`viewCoefficient`, the internal gain's `name`/`description`/three radiant proportions/three view
coefficients/`personGain`/`freshAirRate`/`targetIlluminance`/`domesticHotWater`, the thermostat's
`controlRange`/`proportionalControl`, and for each of the **12 profile slots** (8 gain incl. `ticV`,
4 thermostat) `type`, `factor`, `value`, `setbackValue`, `function`, all 24 `hourlyValues` and the yearly
series. Zone `floorArea`, `volume` and `ic.count` are identical; the simulation was a full 1–365 day run
against a real TAS weather year (`cibseweather2005.twd`, Belfast TRY), identical on both sides.

### Expected diagnostic-only differences, confirmed to be the only ones

`profile_TBD.name` (52 / 432), `profile_TBD.description` (44 / 396) and `thermostat.name` (8 / 54).
`internalCondition_TBD.name` is **identical on every condition** (8 / 54 compared, 0 differing), as are
`InternalGain.name` and `.description`.

**Zone GUID is the one further difference, and it is not attributable to this change.** A control run —
the *same* baseline DLL twice — differs in exactly those same 4 lines and nothing else, so TAS minting a
fresh zone GUID per export is the experiment's noise floor.

### Watched regressions, each measured

1. *A reference to a name the library does not carry* — **0** dangling resolved references on either model.
2. *`importUnused` templates keeping legacy names* — the template conditions resolve to canonical names;
   **0** unresolved template slots other than `Ventilation`.
3. *Same name, different values collapsing* — did not happen. `ModelA-Tas` discriminated
   `Cell 1 [Constant]` (1 value) from `Cell 1 [Constant]_EC342275` (23 values) and the same for
   `HTG_7to19_21`; the TM59 model discriminated three such pairs (`HTG_1to24_16`, `No Heating`,
   `Infiltration`). The discriminator is the signature hash, never a positional counter.
4. *Different categories with identical values collapsing* — did not happen. On `ModelA-Tas` 11 of the 20
   definitions share their exact values with another definition and stayed separate by `Category`; the
   TM59 model keeps `OFF` as **three** definitions (Equipment Sensible, Lighting, Occupancy) and
   `Constant` as two. Value-only dedup would have collapsed 20 → 9.
5. *Zero-length / TAS function profiles deduplicated* — **not exercised by either licensed model**: both
   carry only `ticValueProfile` / `ticHourlyProfile` and no non-empty `function`. Covered by the COM-free
   zero-length-exclusion tests only; stated here rather than implied.
6. *The known `VentilationProfileName` defect changing* — unchanged. 4 unresolved references on
   `ModelA-Tas` and 36 on the TM59 model, **the same set on both sides**, every one of them `Ventilation`.
7. *`factor`/`function`/`setbackValue` becoming shared across zones* — no: all three are in the 852 / 5754
   identical simulation-effective fields, still derived per zone by `Modify.UpdateInternalCondition`.
8. *Internal-condition count or assignment changing* — no: 4 / 27 conditions, identical key sets, and
   identical per-zone `ic.count`.
9. *Repeat import producing different canonical names* — no: see below.

### Repeat / idempotence

Importing the same `.tbd` twice with the feature DLL produces a **byte-identical** SAM-side dump on both
models and in both rounds: 20 / 30 definitions, identical canonical names, no added suffix, all references
identical, and **zero** nested `X [Y [` names.

A *full round trip* repeated three generations (import → export → import → …) keeps the definition count
pinned at 20 and never nests a name, but **2 of the 20 names accrete one `_<hash>` suffix per generation**
(`Cell 1 [Constant]_EC342275` → `…_EC342275_EC342275`). The cause is pre-existing and outside this change:
the export writes **one profile name onto two differently-valued TAS profiles** — a zone's internal
condition and its HDD sibling both receive the space condition's profile name while keeping their own
values — so the next import legitimately sees a same-name/different-value pair and must discriminate it
(regression 3 above). The baseline does the same thing and is strictly worse on the same measure: it grows
**24 of 42** names per generation by re-nesting, reaching `Cell 2 [Cell 2 [Cell 2 [No Dehumidification]]]`.
Name-only, no simulation effect, and a reduction rather than a regression.

*This residual is now fixed* by the follow-up profile round-trip hardening work
(`PROFILE_ROUND_TRIP_HARDENING.md`): the HDD writer names its flattened single-value profiles after
themselves (`"<name> - HDD"`), so no generation manufactures the same-name/different-values pair and the
names are a fixed point.
