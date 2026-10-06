# Reusable TBD aperture types (Stage 1)

SAM→TBD export used to create **one `TBD.ApertureType` per physical aperture, per opening child**, named
after the aperture's building element — `"Windows: <name> <guid> -pane"`. A 200-window model produced 200+
aperture types where TAS semantics need perhaps 2–15.

A `TBD.ApertureType` is a **building-level reusable definition**: the same type may be assigned to any
number of building elements. Stage 1 makes the export use that, exactly as schedule sharing already does one
level down.

```text
same effective opening control          → one shared TBD.ApertureType
different Cd / factor / function /      → different TBD.ApertureType
schedule / preserved description /
day-type membership
```

```text
200 identical windows → 1 ApertureType → 1 shared schedule → 200 building-element assignments
```

Stage 1 shares **aperture types and schedules only**. Constructions and building elements are still one per
aperture; that is Stage 2.

---

## S1-C0 — day-type read-back probe (correctness precondition)

Whether day-type membership participates in definition equality depended on whether TAS lets it be read
back. It does.

**Step 1 — Interop.TBD metadata (licence-free).** `TBD.IApertureType` exposes a read member next to the
writer:

```text
dayType GetDayType(Int32 index)
Int32   SetDayType(dayType dayType, Boolean bAdd)
```

**Step 2 — licensed TAS read-back (run 2026-08-21, TAS COM available on the authoring machine).**

| Case | Result |
|---|---|
| write a strict subset (1 of 3), read live | exactly that subset |
| aperture type with nothing assigned | empty |
| write the production set (all bar HDD/CDD) | all of them |
| `SetDayType(dt, false)` | the entry disappears |
| `SetDayType(dt, true)` twice for one day type | one entry, not two |
| save, close, reopen | membership preserved verbatim |
| write order `[Weekday, Sunday]` vs `[Sunday, Weekday]` | **read back in the order written, not calendar order** |

The collection is **0-based and null-terminated**.

**Outcome A — readable.** Per the frozen plan, day-type membership is therefore an explicit simulation
identity field, read at seed time and compared in equality; a pre-existing aperture type is reusable when
**all** fields including day types verify, and the name convention carries no behavioural weight. Because
read-back order is insertion order, membership is compared **as a set** — `Query.DayTypeNames` and
`ApertureTypeDefinition` both normalise it.

The conservative Outcome B policy (never reuse pre-existing types across runs) is **not** in force and is
not implemented.

---

## Invariants

1. **A shared definition is immutable.** When an equivalent aperture type is found, nothing on it is
   written — not even rewritten to the same value. Every other element referencing it would see the write.
   Reuse requires full equality; anything less creates a new type under a deterministic, collision-suffixed
   name. Proven by the write-log assertions in `ApertureTypeReuseTests`.

2. **Identity is the definition, never the name.** `ApertureTypeDefinition` is the whole of it: discharge
   coefficient, opening factor (after the Part O `AlwaysClosed → 0` override), profile mode, function text,
   the 24 schedule values, the description, and day-type membership. Float equality is **exact** (`float`
   equality), with one deliberate exception: **signed zero is normalised to positive zero** on the way in,
   because `-0f` and `+0f` are equal under float comparison while their IEEE-754 bit patterns — and hence
   their signature hashes — differ; normalising keeps `Equals` and `GetHashCode` in agreement. Names are
   display metadata and are only derived *after* a definition search has failed — so a
   name collision is necessarily with a different definition. The deterministic collision hash is derived
   from the **exact IEEE-754 bit pattern** of each float, not from the rounded `0.###` display text: two
   TAS float definitions such as `0.6201` and `0.6202` share the readable name `Opening Cd0.62 F1` but can
   never resolve to the same collision identity.

3. **No generated name contains physical aperture identity.** The base is the opening's own
   `OpeningPropertiesParameter.Description` (a property of the reusable control), falling back to
   `"Opening"`:

   ```text
   <base> Cd<cd> F<factor>[ S<schedule signature>][ <ordinal>][_<signature hash>]
   e.g.  Bedroom openable Cd0.62 F1 S00FFFE
   ```

   `Cd`/`F` stay rounded and human-readable; only the `_<signature hash>` discriminator carries the exact
   bit identity above. A GUID-named type can never be found again by the next identical window, so sharing
   and GUID-naming were mutually exclusive. `Name_NeverContainsPhysicalApertureIdentity` pins this.

4. **Per-element opening multiplicity is preserved.** TAS keeps one entry per aperture type on a building
   element — `AssignApertureType` with a type the element already holds adds a *second* entry (verified on
   licensed TAS), and the by-name guard is what stops it. So N opening children produce N distinct types
   even when identical:

   ```text
   2 identical children → 2 distinct types, ordinal 1 and ordinal 2
   …and those ordinal types are shared with every other element that has the same two children.
   ```

   The lookup key is `(definition, ordinal)`. `childIndices` correspondence is unchanged: a refused child is
   absent from the returned list, never padded. The compatibility overload
   (`SetApertureType(building, buildingElement, single, out refusal, name, index)`) derives its ordinal from
   the legacy 1-based `index` (`Query.ApertureTypeOrdinal`): position *is* the occurrence, which is exact
   for identical children — the case where it matters — and conservative (over-splitting, never collapsing)
   for different ones. The multiple-opening entry point computes the true per-definition occurrence from the
   whole sibling set.

5. **The legacy per-element path is untouched.** When every aperture type already on an element is named
   after the element itself, those names carry the aperture GUID and are therefore exclusive to it; the
   previous in-place update applies unchanged and a legacy TBD behaves bit-for-bit as it did. It converges
   on shared types only through a fresh export. Passing an explicit `name` selects the same path. This is
   the **only** place an existing aperture type is written to.

6. **A stale shared type is refused, never rewritten or added to.** If an element carries a
   convention-named type that does not state the requested control, the write is refused with a note naming
   it: rewriting it would change every element referencing it, and adding a second would give the element
   more openings than the model states. A *foreign*, unrecognised type is left alone and the requested
   control is added alongside it — the coexistence the previous write already produced.

7. **Schedule semantics are unchanged.** `Create.GetOrCreateSchedule` gained a cache-taking overload only.
   It changes where the list of existing schedules comes from — read once per open document instead of
   rebuilt per opening child — and registers a created schedule back so the next child finds it. Validate →
   reuse by value → deterministic name → create once → 24-value read-back verify → refuse rather than
   overwrite: all identical. Passing `cache: null` is the original overload byte for byte.

8. **Reusable registration and name reservation are two different things.** A schedule or aperture type is
   registered as *reusable* only after its write has fully succeeded and read back as requested. But the
   object exists in the TBD from the moment it is created and named, and a creation whose write *later*
   fails is never withdrawn (no `RemoveSchedule`; a created aperture type is left in place by policy). So
   its name is **reserved** with the cache immediately at creation: the object can never be matched as a
   reusable definition, yet no later creation can accidentally choose the same name. Reservation upgrades
   to reusable registration only for the very same COM object, identified by reference.

9. **A newly created shared aperture type is verified by full read-back.** After the write completes —
   Cd, description, profile value/factor/setback/type/function, schedule, day types — the type is read
   back through the same seed reader that classifies pre-existing types and must equal the requested
   definition. A mismatch (or an unreadable type) refuses: the name stays reserved, the object is never
   reusable and is never assigned. This runs only for newly created unique definitions — reuse writes
   nothing, so there is nothing to verify there.

### Seed gates (a pre-existing type this export must not reuse)

Recorded with a reason, contributing its **name** to collision avoidance and nothing else:
`sheltered` set; no readable profile; `profile.value != 1`; a profile type other than
`ticValueProfile`/`ticFunctionProfile`; `ticFunctionProfile` with no function text; a schedule whose 24
values will not read back or which sits alongside a non-zero `setbackValue`; unreadable day-type membership.

`sheltered` is a conservative addition to the frozen plan's list — SAM never writes it, so adopting a
sheltered type would apply a shelter the model does not state. Refusing to reuse is the safe direction.

---

## What each file does

| File | Role |
|---|---|
| `Classes/ApertureTypeDefinition.cs` | Immutable value-equality object over the identity fields. COM-free. |
| `Enums/ApertureTypeProfileMode.cs` | Plain / ScheduleOnly / Function — which of the three write shapes. |
| `Query/ApertureTypeDefinition.cs` | COM-free factory from `ISingleOpeningProperties`, plus `ApertureTypeOrdinals` and the index-derived `ApertureTypeOrdinal`. |
| `Query/ApertureTypeDefinitionTBD.cs` | The seed reader: an existing `TBD.ApertureType` → definition, or a refusal. Also `DayTypeNames`. |
| `Query/ApertureTypeSignature.cs` | Deterministic FNV-1a signature and collision hash over the **exact Single bit patterns**. Never `GetHashCode`. |
| `Query/ApertureTypeIndex.cs` | First equal definition in a seeded list, or −1. Mirrors `ScheduleIndex`. |
| `Query/ApertureTypeName.cs` | Name derivation, sanitisation, decomposition, legacy-name test. |
| `Query/ApertureTypeReconciliation.cs` | The COM-free decision over an element's pre-existing assignments. |
| `Enums/ApertureTypeReconciliation.cs` | Create / Reuse / Legacy / Refuse. |
| `Classes/BuildingReuseCache.cs` | One COM pass over schedules, aperture types and day types; lifetime = one open document. Holds **reusable registrations and name reservations separately** — a failed creation is never reusable, but its name stays occupied. |
| `Modify/SetApertureType.cs` | The reuse path, and the fenced legacy path in `SetApertureType_Named`. |

**Cache lifetime.** `BuildingReuseCache` holds live COM references and the workflow re-opens the TBD between
steps, so it is constructed by the entry point that owns the open document — `Modify.Update`,
`Modify.UpdateBuildingElements`, and the geometric `Modify.SetApertureTypes(building, adjacencyCluster, …)` —
and never kept across one. Every threading parameter is optional and defaults to null, so all pre-existing
call sites compile and behave unchanged.

---

## Acceptance

### COM-free (`SAM.Analytical.Tas.TM59.Tests/ApertureTypeReuseTests.cs`, runs in CI)

80 tests: definition equality field by field (including close floats that round to the same display text
and signed zero, which must hash alike because it compares equal), day-type set semantics, signature
determinism and **exact Single bit-pattern identity**, naming/collision/refusal (including distinct
deterministic collision names for `0.6201` vs `0.6202`), name decomposition, the COM-free factory, ordinals
(including the index-derived ordinal the compatibility overload uses), the reconciliation decision table,
and a fake-COM harness whose fakes
**record every property set** — `FakeTBDApertureType` / `FakeTBDProfile` / `FakeTBDSchedule` /
`FakeTBDBuildingElement` / `FakeTBDBuilding`, in the style of `FakeTBDSchedule` in
`OpeningScheduleResolutionTests`. The harness delegates every decision to the production helpers, so the
write log is what makes "reuse touches nothing" a test rather than a claim. Failure injection on the fakes
pins the late-failure contract: a schedule or aperture type whose write fails after creation stays in the
TBD, is **never reusable** (even when it would read back as the requested control), and its **name stays
reserved**, so the next write lands on the deterministic qualified name.

### Licensed TAS (manual; run 2026-08-21, all pass — re-run after the reservation/read-back hardening, same results)

Driven through the real `Modify.SetApertureTypes` → `SetApertureType` → `BuildingReuseCache` →
`Create.GetOrCreateSchedule`, against a real `.tbd` created and reopened through `TBD.TBDDocument`.

| Scenario | Result |
|---|---|
| 200 identical windows | **1 ApertureType** (`Opening Cd0.395 F1 S00FFFE`), **1 schedule**, 200 assignments, no issue notes |
| repeat export into that TBD | **0 additional** aperture types, **0 additional** schedules, no element gained a second opening, no issue notes |
| 10 **new** elements added to that saved TBD | **0 additional** types/schedules; each adopts the **seeded** type — the seed read (Cd, factor, 24 values, description, day types) survives save/reopen |
| 5 control variants over 200 windows | **5 ApertureTypes**, names distinct, none containing aperture identity |
| 50 windows × 2 identical children | **exactly 2 ApertureTypes** (`… F1`, `… F1 2`); every element keeps both openings |
| element carrying its own legacy per-element type | written in place, **no** shared type created alongside, one opening |
| element carrying a stale shared type | write **refused** with the type named; Cd unchanged; no second opening; no replacement type |

The harness is not committed: `SAM.Analytical.Tas.TM59.Tests` deliberately carries no COM reference (see
`TESTING.md`), and adding a second COM-referencing project is out of Stage 1's scope. To re-run it, build a
`net481` console project referencing `SAM_Tas/build/SAM.Analytical.Tas.dll`, `SAM/build/SAM.Analytical.dll`,
`SAM/build/SAM.Core.dll` and `references_buildonly/Interop.TBD.dll`, and drive the scenarios in the table
above.

---

## Out of scope for Stage 1

> Construction and BuildingElement sharing is **Stage 2** and is now implemented - see
> [APERTURE_DEFINITION_REUSE.md](APERTURE_DEFINITION_REUSE.md). Nothing in this document changed for it
> except the `BuildingReuseCache` extension it needed.

Construction sharing, BuildingElement sharing, physical-instance identity (`ZoneSurfaceReference`
resolution), the `UpdateBuildingElements` name-decode replacement, the import grouping fixes, and the
gbXML/T3D route — all unchanged. On the gbXML route TAS itself authors one building element per aperture
from the `CADObjectId` names, so that route gains aperture-type and schedule reuse and nothing else.
