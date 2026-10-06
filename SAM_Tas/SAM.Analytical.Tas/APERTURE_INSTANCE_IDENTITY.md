# Physical aperture instance identity (Stage 3)

Stage 3 of the frozen three-stage aperture-definition plan, and the last. Stage 1 — reusable
`TBD.ApertureType` — is `APERTURE_TYPE_REUSE.md`. Stage 2 — reusable aperture `Construction` and
`buildingElement` — is `APERTURE_DEFINITION_REUSE.md`. **Both are unchanged here.** Stage 3 adds nothing to
what a definition is and takes nothing away from how definitions are shared; what it establishes is which
PHYSICAL window a shared definition is currently standing for, so that changing one of two hundred identical
windows changes one of them.

---

## The one rule

**Physical aperture identity is `{ ZoneGuid, SurfaceNumber }`.**

Held as `ZoneSurfaceKey` (`Classes/ZoneSurfaceKey.cs`). The SAM-side
`Pane`/`FrameZoneSurfaceReference_1`/`_2` stamps remain one representative identity per side; the parallel
`Pane`/`FrameZoneSurfaceReferences` collections preserve every physical key when one side is split into
several TAS faces.

Neither half is sufficient, which is why it is a type and not a convention. **TAS numbers surfaces per
zone**, so zone A's surface 5 and zone B's surface 5 are two different surfaces that happen to share a
number; and a zone GUID alone names a room. Every comparison of a physical surface in this codebase now goes
through either `ZoneSurfaceKey` or `Query.ZoneSurfaceReferencesMatch`, and the two agree — including that two
spellings of one GUID (braced, cased, padded) are one zone.

**What must never be used instead**, and what each of them actually is after Stage 2:

| Not identity | What it is | Why it pairs the wrong window |
|---|---|---|
| `Pane`/`FrameBuildingElementGuid` | a reusable DEFINITION binding | 200 identical windows legitimately stamp the same one |
| Construction GUID | a reusable definition | ditto, one per `ApertureConstruction` per part |
| Reusable `ApertureType` | a shared opening control | ditto, one per distinct control |
| Definition-derived name | a label on a shared object | carries no aperture GUID *by design* — that is Stage 2's invariant 4 |
| Surface area | a property of the shape | identical windows have identical areas |
| List position | nothing at all | TAS guarantees no ordering |

`AperturePhysicalIndex.ApertureGuids(buildingElementGuid)` exists and returns a **set**, deliberately: a
caller that has already resolved a surface physically may ask which apertures share the definition it points
at, and the answer being plural is the normal Stage 2 state, not a fault.

---

## What each mechanism does

### 1. `Modify.UpdateBuildingElements` — resolution, split and re-merge

Resolution order per TBD building element:

1. **The definition-membership map** — every SAM aperture whose `Pane`/`FrameBuildingElementGuid` names this
   element. Many apertures may; that is the point.
2. **Legacy GUID-in-name decoding** — for an element no aperture stamps. Byte-for-byte unchanged, so every
   TAS-authored or pre-stamping TBD behaves exactly as it always has.

On the membership path the element is compared, member by member, against what it **already carries** —
colour, opening-control assignments and feature shade, all read once before anything is written
(`Query.ApertureMatchesExistingAssignment`, `Query.FeatureShadesMatch`).

- **A member that still matches**: zero writes. Not even a rewrite to the same value — every other member
  would see it. This is the overwhelmingly common outcome of a repeated update, and what makes a repeated
  update a no-op.
- **A member that has diverged**: **split**. Its complete physical rebind set is validated first; only then
  does its required `BuildingElementDefinition` resolve an existing
  equivalent element from the Stage 2 reuse cache, or creates one. Only **that member's own** physical
  surfaces — resolved from its own complete `ZoneSurfaceReference` collection — are rebound to it. The
  element it left is never written to; if every member leaves, it is simply left in place, unused.
- **Re-merging** is the same mechanism with nothing added: a member that becomes equivalent again finds the
  common element in the cache (it is still in the building) and rebinds to it. No second equivalent
  definition is created, and the common element is not rewritten.

A pane stating a `FeatureShade` is a deliberate exception to reuse: a shade-carrying element can never be
shared (Stage 2's seed gate refuses one), so such a member always gets its own freshly created element, named
through `Query.ShadedBuildingElementName` so repeated shade splits do not collide, and is not registered for
reuse.

**Rebinding is atomic and doubly guarded** (`Query.ApertureRebindKeys` + `RebindMemberSurfaces`). The complete
intended surface set is resolved and validated before any replacement element is created/reserved/written and
before any one surface moves. Otherwise a refused validation could leave an orphan definition, while a
multi-face or two-sided member that moved one face and failed on another would be split across two elements
with its stamp calling the new one authoritative. Two independent guards must both hold:

- **the stale-stamp guard**, on the TBD side: the surface must currently point at the element the aperture
  claims it does;
- **the contested-surface guard**, on the SAM side: `AperturePhysicalIndex` must resolve that surface back to
  *this* aperture and *this* part.

Either failing rebinds **none** of the member's surfaces, creates no replacement definition and leaves its
stamp untouched. A representative-only legacy stamp cannot prove that another same-side face was not lost;
it therefore refuses until export, import or `UpdateIds` has restamped the complete set.

### 2. `AperturePhysicalIndex` — resolve exactly one, or refuse

`Classes/AperturePhysicalIndex.cs`. `{ ZoneGuid, SurfaceNumber }` in, `(aperture, part, side)` out.

Two things must be true for an answer to be safe to write through: exactly one aperture claims the surface,
and it claims it in exactly one slot. Neither is guaranteed — a model round-tripped through the pre-Stage-3
import pairing, or hand edited, can carry the same stamp twice — and a scan returning the first hit turns
that into a silent cross-bind. **The collision is detected when the index is built, and that key refuses for
ever after, with a reason.** Refusing is correct: the alternative is updating a window the user did not
change.

Refusals are reported through `UpdateBuildingElements`'s existing `out List<string> notes` channel. No public
API was widened to carry them.

### 3. The `_1` / `_2` slots — a slot is a SIDE, and a side is a ZONE

One mutator, `Modify.SetApertureZoneSurfaceReferences`, used by **all three** write paths — the direct
export, the TBD import and `UpdateIds`. It **clears both slots and refills them** from
`Query.ApertureZoneSurfaceSides`, which orders by zone GUID (ordinal). At the same time it writes the complete
canonical physical set to `Pane`/`FrameZoneSurfaceReferences`; this does not change `_1`/`_2` side semantics,
but gives rebind operations every face rather than only each side's representative.

This replaces fill-the-first-empty-slot, which had three failure modes:

- on a model already stamped by a previous run, the old `_1` survived and `_2` was overwritten. A stale stamp
  is not harmlessly redundant — TAS does not promise to reassign the same surface numbers, so it points at a
  real surface belonging to something else;
- which side was `_1` depended on the order the zones happened to be walked, so a repeated update could swap
  them and an A/B against the source would report a swap that was not one;
- an aperture whose pane is split into several faces filled **both** slots from **one** side, losing the
  other side entirely. Several surfaces in one zone are one side, and compete for one slot; the lowest
  surface number represents it.

Three or more zones is a refusal, not a truncation. Ordering normalises the zone GUID; **writing preserves
the caller's spelling**, so a re-exported model still diffs clean against its source.

### 4. The TBD → SAM import

- **Grouping** is `Query.GroupAperturePolygons` — a pure function. Each group is seeded by its own largest
  remaining polygon and gathers every polygon whose internal point falls inside the seed's face. The seed's
  own (polygon, surface) pair is captured **before** anything is removed, which fixes two defects with one
  root cause: the seed used to be paired with whatever tuple was first in the *shrunk* list — a different
  window's surface entirely — and a lone pane with no coincident frame produced an **empty** group, so a
  frameless window came back with no `ZoneSurfaceReference`, no `BuildingElementGuid` and no imported
  `OpeningProperties`.
- **Pane vs frame comes from `BEType` first**, construction name second
  (`Query.AperturePart_BuildingElementType`). TAS keeps which half a surface is on the element; a
  construction name ending `-pane`/`-frame` is our own convention on a shared label, which a foreign TBD need
  not follow. The `[0]` fabrication fallback survives only for genuinely multi-member groups where neither
  the types nor the names say anything — a **singleton** is excluded, because a lone pane is not also its own
  frame.
- **The second side is stamped, not skipped.** The import walks each zone in turn, so an internal aperture is
  met twice; the first meeting creates it and the second used to `continue` outright, leaving the aperture
  stating one surface where the TBD holds two. It now adds the second side through
  `Modify.AddApertureZoneSurfaceReference`, which re-canonicalises the pair rather than appending, so which
  zone the import walked first does not decide which side is `_1`.

### 5. `UpdateIds`

- Clears the aperture stamps alongside the panel ones, then collects every matched surface over the whole
  pass and writes them canonically at the end. Previously only the panel stamps were cleared, so a second
  pass found `_1` occupied by the previous run, wrote side 1 into `_2`, and then wrote side 2 over the top.
- Passes the zone GUID into both `Query.Match` calls, so a surface number cannot match a panel or aperture in
  a different zone.
- Re-stamps `Pane`/`FrameBuildingElementGuid` every pass — unchanged, and correct, because that names a
  DEFINITION. It is what lets a later `UpdateBuildingElements` resolve an aperture's element without decoding
  a GUID out of a name that no longer carries one.

### 6. `Query.Match`

All three overloads that compare stamps now honour the zone:

- `Match(ZoneSurfaceReference, apertures, out part)` — via `Query.ZoneSurfaceReferencesMatch`;
- `Match(IZoneSurface, List<Panel>, zoneGuid, …)` and `Match(IZoneSurface, List<Aperture>, zoneGuid, out part, …)`
  — new overloads taking the surface's own zone; the parameterless forms remain and keep the old number-only
  behaviour for callers with no zone in hand.

Two further defects fixed in passing: the panel overload's `_2` branch re-read `_1` (a copy/paste that made
the second slot unreachable and threw outright on a panel carrying `_2` without `_1`), and the
`ZoneSurfaceReference` overload **returned** on a null list entry instead of skipping, so one null made every
aperture after it unresolvable.

`ZoneSurfaceReferencesMatch` falls back to the surface number whenever **either** side states no zone. That
makes the whole change a strict tightening: anything that matched before still matches, except a
same-numbered surface in a different, GUID-stated zone.

---

## Foreign and seeded TBDs

Stage 3 is conservative by construction, and the policy is uniform: **use SAM stamps deterministically where
they exist; never invent identity where they do not.**

| The TBD holds | What happens |
|---|---|
| valid `ZoneSurfaceReference` + `BuildingElementGuid` stamps | full membership resolution, split, re-merge, atomic guarded rebind |
| no stamps on an element (TAS-authored, or exported before the stamping existed) | the original single-aperture GUID-in-name decode, unchanged |
| a surface no aperture stamps | left alone; not an error — this is what a native TBD looks like |
| a surface two apertures stamp | **refused**, with a note; neither is rebound |
| an element carrying a feature shade | never adopted for reuse (Stage 2's seed gate); a shade-stated member always gets its own element |
| a construction name outside the `-pane`/`-frame` convention | part read from `BEType`; a singleton is not fabricated into two halves |

No arbitrary foreign TBD is required to carry SAM-specific identity, and our own exported-and-reopened models
are deterministic.

### Documented ambiguity, deliberately not resolved

**The import still buckets candidate surfaces by `ApertureConstruction` GUID before grouping them
geometrically.** For a model SAM exported this is harmless — Stage 2 names a window's two constructions from
one `ApertureConstruction`, so both halves land in one bucket. For a **foreign** TBD whose pane and frame
constructions have unrelated base names, the two halves land in different buckets and the window is imported
as two apertures. Bucketing only ever *restricts* grouping, so it cannot cross-bind; and the bucket key is
also what supplies the aperture's `ApertureConstruction`, so removing it is a round-trip change rather than a
grouping fix. Left as is, and recorded here.

---

## Invariants Stage 3 must not weaken

1. **A shared definition is never mutated on reuse.** Stage 3 changes which definition a physical surface
   points at. `zoneSurface.buildingElement = resolvedDefinition` is allowed; rewriting a shared element's
   construction, colour, `BEType` or aperture types is not, and neither is rewriting a shared `Construction`,
   `ApertureType` or schedule.
2. **No physical identity in a reusable name.** Stage 2's naming is untouched: `<ApertureConstruction.Name>
   -pane`/`-frame`, and definition-derived element names with a deterministic signature discriminator.
   `aperture.UniqueName()`, an aperture GUID and a surface number stay out of them. Physical identity lives
   in the physical references.
3. **Physical multiplicity is preserved.** 200 identical windows keep 200 pane and 200 frame `zoneSurface`s
   whatever the definitions resolve to.
4. **Refusing beats guessing.** Every ambiguity — a contested surface, a stale stamp, a part disagreement,
   three zones, an unprovable definition on a shared element — declines to write and says why.

---

## Acceptance

### COM-free (runs in CI)

`SAM.Analytical.Tas.TM59.Tests` — **415 tests, all passing** (369 before this change, all unchanged and
green, including the Stage 1 `ApertureTypeReuseTests` and Stage 2 `ApertureDefinitionReuseTests` with their
sharing expectations intact).

`ApertureInstanceIdentityTests.cs` (46 tests) covers:

- **the key**: same pair equal; same number different zone NOT equal; same zone different number not equal;
  GUID spelling (braced/cased/padded) is one zone; a non-GUID zone identifier is kept, not discarded; a
  half-populated stamp yields **no key at all** rather than a wildcard;
- **resolution**: a valid pair resolves exactly one aperture with its part and side; wrong zone → no match;
  wrong surface number → no match; side 1 vs side 2; pane vs frame;
- **ambiguity**: two apertures claiming one surface refuse, regardless of listing order; one aperture
  claiming one surface as both pane and frame refuses; a contested surface does not poison the rest;
- **identical instances**: 200 windows with identical geometry, construction and one shared building element
  remain 200 distinct physical instances, each surface resolving to its own aperture, with no ambiguity — and
  the shared binding reports all 200 members rather than a winner;
- **the slots**: two zones ordered by zone not arrival; several surfaces in one zone take one slot between
  them; three zones refuse; duplicates and unusable keys dropped; no surfaces is an empty answer, not a
  refusal;
- **the mutator**: two-sided stamping identical whichever order it is given; repeated writes change nothing;
  fewer surfaces than last time clears the stale slot; a refusal leaves no stamp standing; pane and frame do
  not disturb each other; the caller's GUID spelling is preserved; multiple same-side faces survive a SAM
  JSON round trip behind the single representative slot;
- **the import's second side**: joins the first with both sides surviving; identical whichever zone was
  walked first; the same surface twice stays one side; a third zone refuses;
- **two-sided round trip**: three successive re-writes keep each side on its own surface with no inversion
  and no ambiguity; two internal apertures between the same zone pair do not cross-bind;
- **complete-set rebind**: a multi-face pane splits every physical surface and merges every surface back;
  contested ownership and representative-only legacy stamps refuse before replacement creation or movement;
- **`ZoneSurfaceReferencesMatch`**: zone-aware, spelling-tolerant, and falling back to the number when
  either side states no zone.

`GroupAperturePolygonsTests.cs` and the membership/split half of `InstanceIdentityTests.cs` cover the import
grouping and the split decision.

### Licensed TAS (2026-08-22, EDSL Tas, `TBD.exe` under `C:\Program Files\Environmental Design Solutions Limited\Tas`)

Every run is an **A/B**: one harness binary, two folder copies differing in `SAM.Analytical.Tas.dll` alone
(A = `sow/2026-Q3` at `0f66b11`, i.e. after PR #32; B = this branch). The harness observes the TBD through raw
COM accessors and its own copies of the identity primitives, never through the code under test.

| Scenario | Baseline (A) | Stage 3 (B) |
|---|---|---|
| **A - 200 identical windows**, synthetic, export + `UpdateIds`, then two further `UpdateBuildingElements` + `UpdateIds` passes | 400 surfaces, 2 elements, 1 ApertureType - but the first repeated pass adds **400 stamps** and produces **400 collisions** | **PASS.** 200 pane + 200 frame surfaces; 2 aperture elements; 1 distinct pane element; 1 ApertureType. Repeated twice: **0 stamps changed, 0 added, 0 collisions, 0 surfaces rebound** |
| **B - split one aperture** (10 shared windows, one changes its opening restriction to Night Closed) | splits, but see C | **PASS.** 3 elements (was 2), 2 distinct pane elements, **exactly 1 surface rebound and it is the changed aperture's**, old element still carries the other 9, 10 panes + 10 frames intact, all 20 stamps unchanged, 0 collisions |
| **C - merge it back** | **FAIL** - 4 failures: the aperture never returns to the shared element, its stamp stays on the split element | **PASS.** All 10 pane surfaces back on **one** element - the ORIGINAL - no new equivalent definition, counts unchanged |
| **D - identical-geometry collision, `SAM -> TBD -> SAM`**, real `ModelA.sam` (2 zones, 14 apertures **all sharing one `ApertureConstruction`**) | **FAIL** - all **28 stamps unresolved**, **3 collisions** | **PASS.** 28 stamps all resolving to the right zone/surface/part, 0 collisions; 14 apertures imported; **every pane and frame stamp lands 0.0000 m from its own aperture's centroid** (cross-pair tolerance 1.0833 m) |
| **E - two-zone aperture**, real model with one aperture injected into the shared wall, through export -> 2x update -> save/reopen -> import | **FAIL** - 10 failures: 28 unresolved, 3 collisions, and **13 panes / 14 frames spuriously two-sided** where only one aperture is; after import **0** two-sided | **PASS.** 16 panes + 16 frames; 32 stamps all resolving; exactly **1** two-sided pane and **1** two-sided frame; sides one per zone with `_1` the lower zone GUID; **2 linked pane surfaces and 2 linked frame surfaces**; two repeated updates change nothing; after save/reopen/import still exactly 1 two-sided pane and 1 two-sided frame |
| **Pre-simulation TBD A/B** - export `ModelA.sam` with each DLL and dump every fact TAS reads (constructions with layers/widths/conductivities, elements with `BEType`/colour/construction/aperture types, aperture types with Cd, and all 40 surfaces with area/type/orientation/inclination/altitude/reversed/element/link/polygon centroid+area) | - | **IDENTICAL on every one of 61 dumped facts.** The export writes the same TBD either way, as expected: its only Stage 3 change is where the SAM-side stamps are written |
| **TAS/TSD A/B** - TAS run on both exported TBDs, 14 days, then every hourly zone and surface variable differenced | - | **173,376 values compared, 0 differing, max absolute difference 0, max relative difference 0.** Exactly identical, not merely within solver noise |
| **Result-mapping stamp input** (what `Modify.AddResults` keys on, real `ModelA.sam`, 14 apertures) | **54 stamps** - nearly double the correct number | **28 stamps** - exactly 14 panes + 14 frames |

**Not run, and not claimed:** the shading-specific chain (`Simulate_Coverage`, `UpdateShading`,
`Create.SolarModel`, `CopyResults` pane/frame/panel solar mapping). `Modify.AddResults` itself was driven to
the point of consuming the stamps, but its in-process completion is blocked by a harness limitation rather
than a Stage 3 one: `SAMTBDDocument.Dispose` closes the shared TAS COM server, so after a few document
open/close cycles in one process a subsequent TSD read fails with "RPC server is unavailable" - **identically
on both builds**. What IS established is that the stamp set `AddResults` consumes is exactly correct here and
was badly wrong before (28 vs 54), and that `CopyResults` matches apertures to solar surfaces by GEOMETRY
rather than by the stamps (recorded in `APERTURE_DEFINITION_REUSE.md`), so shared definitions do not degrade
it.

**Two pre-existing behaviours the A/B settled as NOT Stage 3's:**

1. **`UpdateConstructions` duplicates aperture constructions on a Stage-2 TBD.** The first
   `UpdateBuildingElements` pass takes the construction count 4 to 8, and it then stays put.
   `UpdateConstructions` derives aperture construction names from `apertureConstruction.UniqueName()`, which
   carries the `Windows: ` prefix, where the Stage 2 export writes `<ApertureConstruction.Name> -pane`; the two
   disagree, so a duplicate, unused set is added. **Identical on both builds.** The duplicates are inert - no
   surface points at them and no element is rebound because of them.
2. **The `Modify.Update` export's own `updateGuids` stamping never reaches the caller.** `Modify.Update` opens
   with `adjacencyCluster = adjacencyCluster.UpdateNormals(...)`, which returns a NEW cluster, so every stamp
   the `updateGuids` branch writes goes onto a clone that is discarded when the method returns. The supported
   way to get stamps into a SAM model is `Modify.UpdateIds`, which mutates the cluster it is handed, and that
   is the path this gate exercises throughout. The Stage 3 change to that branch is therefore a correctness fix
   to code whose output is currently dropped, not an observable behaviour change; the plumbing was deliberately
   NOT altered, because turning it on would start mutating caller models on two public entry points
   (`WorkflowCalculator` and `SAM.Analytical.Tas.TM59.Convert.ToTBD`) that pass `updateGuids: true` today and
   get nothing.

---

## Out of scope

Deletion or garbage collection of definitions left unused by a split; profile, `InternalCondition` and
gbXML/T3D definition sharing; further Stage 1 or Stage 2 redesign; the construction-GUID import bucketing
(recorded above); refusal reporting beyond the existing `notes` channel; and any general model-identity work
outside aperture handling.
