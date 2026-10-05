# Reusable aperture definitions on the gbXML/T3D route

The standard Grasshopper and SAM_UI workflow — `ToGbXML → WorkflowgbXML` — now gets the same reusable
aperture definitions the direct `SAMAnalytical.TBD` export has had since Stage 2. Stage 2
(`APERTURE_DEFINITION_REUSE.md`) explicitly scoped itself to the direct `Modify.Update` export and declared
this route out of scope, so this closes a **known gap, not a regression**.

**The direct route is the behavioural reference throughout.** Success is defined as the two routes producing
the same aperture definition structure for the same analytical model, and that is what the licensed
acceptance below measures.

---

## Why the gap existed

On this route **SAM_Tas does not write the TBD.** TAS's own `T3DDocument.ExportNew` does, from a T3D in
which every aperture is its own `window`:

```text
SAMAnalytical.ToGbXML          SAM_gbXML   Convert/TogbXML/Opening.cs:38
                               opening.Name = "{name} [{aperture.Guid}]"   ← the physical GUID enters here
SAMAnalytical.WorkflowgbXML    SAM_Tas     WorkflowCalculator.Calculate
  Importing gbXML                t3DDocument.TogbXML(...)          → one T3D `window` per aperture
  Updating T3D file              Query.UpdateT3D(...)              → decodes that GUID back to the aperture
  T3D to TBD                     t3DDocument.ExportNew(...)        → TAS writes the TBD
```

Three facts make it unavoidable upstream:

- **The gbXML opening name has to carry the aperture GUID.** `Query.UpdateT3D` decodes it back to the SAM
  aperture to write colour, frame percentage and window position type. Removing it would break that.
- **The T3D cannot be canonicalised first.** `Interop.TAS3D` exposes no surface or opening object at all —
  `TAS3D.Building` offers only `GetWindow`/`AddWindow`/`GetElement`/… and `TAS3D.window` only `Delete()` plus
  scalar properties. There is no way to repoint a surface's opening at a shared window.
- **Nothing afterwards collapsed the result.** `Modify.UpdateBuildingElements` only ever SPLITS a diverged
  aperture off a shared element; it never merges. `Modify.SetApertureTypes` reuses `ApertureType`s by value,
  which is why aperture types on this route were already correct.

Measured on the licensed fixture, the raw TAS conversion produces **28 aperture building elements for 14
windows and no constructions at all**:

```text
Windows: SIM_EXT_GLZ 05f4bc0b-8f7b-4a84-b884-a5715883dda6 -pane
Windows: SIM_EXT_GLZ 05f4bc0b-8f7b-4a84-b884-a5715883dda6 -frame
… one pair per aperture, one surface each
```

Constructions were already *partly* shared by accident: `Modify.UpdateConstructions` (called at the top of
`UpdateBuildingElements`) creates `SIM_EXT_GLZ -pane`/`-frame` from the SAM `ApertureConstruction`, and the
word-set fallback in `UpdateBuildingElements` then assigns those to the per-aperture elements. **The
definitional gap was the building elements.**

---

## What was added

One gbXML-gated step in `WorkflowCalculator`, **after `Modify.UpdateIds`**:

```text
Updating Building Elements        Modify.UpdateBuildingElements     (unchanged)
Updating Ids                      Modify.UpdateIds                  (unchanged)
Reusing Aperture Definitions      Modify.UpdateApertureDefinitions  ← new, gbXML route only
Updating Thermal Parameters       …
Updating Aperture Types           Modify.SetApertureTypes           (unchanged)
```

**Running after `UpdateIds` is what keeps it small.** That step has just stamped every aperture's
`Pane/FrameZoneSurfaceReference` and its current `Pane/FrameBuildingElementGuid`, so the pass reads which
surfaces an aperture owns and which element they are on rather than re-deriving either from geometry. A
later `UpdateIds` re-derives the same stamps from the actual bindings, so the order stays idempotent.

Per aperture, per part:

1. **`Query.ApertureRebindKeys`** — Stage 3, unchanged — yields the COMPLETE physical surface set or a
   refusal. A two-sided aperture moves as one set or not at all; a surface claimed by two apertures refuses
   rather than being guessed at.
2. **`Modify.ResolveApertureDefinition`** — the Stage 2 resolve-or-create block, **extracted verbatim from
   `Modify.Update`** so both routes resolve through one resolver rather than two copies of the same equality,
   naming and reserve/register discipline.
3. If the resolved element is already the stamped one, nothing is written. Otherwise every surface in the set
   is rebound and `Pane/FrameBuildingElementGuid` re-stamped.
4. The leftovers are swept.

### It introduces no new equality rules

`ConstructionDefinition`, `BuildingElementDefinition`, their signatures, the seed gates and the two naming
functions are all Stage 2's, untouched. `Query.ApertureDefinitionBindings` is a COM-free mirror that asks
what each aperture part wants **through those same factories**, so the test suite can assert definition
counts without an installed TAS and cannot drift from what the resolver does.

---

## Invariants

1. **Physical instances are never merged.** N apertures keep N pane and N frame `zoneSurface`s.
   `{ZoneGuid, SurfaceNumber}` remains physical identity and is not touched by this pass;
   `BuildingElementGuid` remains a reusable-definition binding that many apertures legitimately share.

2. **Nothing named after a physical aperture is ever ADOPTED as a shared definition.**
   `Query.NamesContainingApertureGuid` tests every seeded construction and element name against the model's
   own aperture GUIDs — an exact test, not a pattern guess — and
   `BuildingReuseCache.RefuseSeededDefinitions` refuses the matches while keeping their NAMES reserved. TAS's
   per-aperture objects can pass every content gate, so without this the cache would hand one over and twenty
   windows would share a definition named after whichever came first. **Sharing and instance-naming are
   mutually exclusive:** an instance-named definition can never be found again by anything but itself.

3. **Pane and frame can never collapse**, however identical their layer content: `AperturePart` is a field of
   both definitions and `Query.BEType` differs (`GLAZING` vs `FRAMEELEMENT`).

4. **A refusal creates no orphan and moves no surface.** The complete surface set is validated before any
   definition is created, reserved or written.

5. **The sweep is conservative in three ways.** `Query.UnusedApertureBuildingElementGuids` marks an element
   only when it is an APERTURE element, holds NO surface, and is not one of the canonical elements this pass
   resolved onto — so an aperture whose rebind was refused never loses the definition that refusal was
   conservative about. `TBD.Building` has no `RemoveBuildingElement`, so removal is `markDelete` plus
   `Building.DeleteMarkedBuildingElements`.

6. **An unreferenced aperture construction is removed only on two grounds** — it names a physical aperture,
   or this pass superseded it (`Query.OrphanApertureConstructionNames`). Anything else unreferenced is a
   library definition with no window using it right now, and the export has always kept those; those are
   reported rather than removed.

7. **A freed plain construction name is reclaimed** (`Query.SupersededConstructionRenames`). Since
   `APERTURE_HARDENING.md` this is a NAMING concern rather than a structural one: the import pairs a
   window's halves geometrically and identifies its family by the construction PAIR, so a qualified frame
   beside a plain pane no longer breaks the round trip. What it would still do is leave the reconstructed
   `ApertureConstruction` named after whichever half's base name was free, rather than the model's own.
   Renaming is safe only here — the
   construction is one this pass created moments earlier, elements reference it by COM identity rather than
   by name, and the document has not been saved. Two guards: the plain name must actually have been removed,
   and exactly one construction must have wanted it.

---

## Licensed verification: ExportNew always replaces building elements

The orphan sweep (`Query.UnusedApertureBuildingElementGuids`) marks any surface-less pane/frame element that
is not one of this pass's canonical targets — with no name-based exemption. That is only safe if
`T3DDocument.ExportNew` never leaves a **foreign, pre-existing** aperture element in the building for the
sweep to find: one a user or some other tool authored directly in the `.tbd`, unrelated to the current T3D,
sitting there with zero surfaces because nothing in the current model happens to use it right now.

`WorkflowCalculator` only deletes the `.tbd` file itself when `WorkflowSettings.RemoveExistingTBD` is `true`;
the default (see `SAM_UI`'s `Query.DefaultWorkflowSettings`) is `false`, so a routine re-run of the workflow
calls `ExportNew` against whatever `.tbd` is already on disk rather than a freshly emptied one. Whether
`ExportNew` **replaces** that file's building elements or **merges** the T3D into them was not something the
original design traced — it stated the "no foreign element to protect" conclusion without having measured it
against `RemoveExistingTBD=false` specifically. A PR review ([2026-08-22](https://github.com/SAM-BIM/SAM_Tas/pull/34#discussion_r3837021889)) correctly flagged this gap.

**The question, put narrowly:** with `RemoveExistingTBD=false` and a `.tbd` already on disk, does
`T3DDocument.ExportNew` replace the file's building elements from scratch, or can it preserve one that the
current T3D does not represent at all?

**The probe.** A temporary diagnostic harness (not part of production code) replicated
`WorkflowCalculator.Calculate`'s own gbXML-branch sequence call-for-call — the same production methods in the
same order, stopping right after the `Convert.ToTBD` call that invokes `ExportNew`, which is exactly where
`UpdateApertureDefinitions` runs next in production:

1. Built a realistic `.tbd` via one ordinary gbXML/T3D conversion of the licensed fixture (`ModelA.sam`, 2
   spaces, 14 apertures) — 33 building elements, matching the "before" baseline elsewhere in this document.
2. Injected a sentinel: a `GLAZING` `buildingElement` named `SENTINEL_PREEXISTING_MARKER_DO_NOT_DELETE`, zero
   surfaces, not part of the T3D at all. Saved and closed.
3. Re-ran the identical gbXML-branch sequence against the **same** `.tbd` path a second time, once with
   `RemoveExistingTBD=false` and once with `=true`, each ending at the same `Convert.ToTBD` call.
4. Re-opened the resulting `.tbd`, before any SAM_Tas aperture step, and searched for the sentinel by GUID and
   by name, and diffed the full building-element GUID set against the pre-`ExportNew` snapshot.

**Result — identical under both settings:**

| | `RemoveExistingTBD=false` | `RemoveExistingTBD=true` |
|---|---|---|
| Building elements before injection | 33 | 33 |
| Building elements after sentinel added | 34 | 34 |
| Building elements after the second `ExportNew` | 33 | 33 |
| Sentinel found by GUID afterwards | **No** | **No** |
| Sentinel found by name afterwards | **No** | **No** |
| Pre-existing GUIDs surviving `ExportNew` | 1 of 34 | 1 of 34 |

The one surviving GUID in both runs is TAS's own universal placeholder — `name="Null"`,
`guid={00000000-0000-0000-0000-000000000000}`, `BEType=0` — present by default in every fresh conversion
regardless of what the file held before. It is not a preserved user object, and there is no other overlap.

**Outcome: `ExportNew` replaces a `.tbd`'s building elements from scratch, independent of
`RemoveExistingTBD`.** The review finding is **not valid** for the production path: nothing survives from a
prior state of the file for the sweep to endanger, so `Query.UnusedApertureBuildingElementGuids` needs no
provenance check beyond the three gates it already has. No production code changed as a result of this
verification — only the doc comment that previously stated the conclusion without having measured it against
`RemoveExistingTBD=false`.

---

## Why `DeleteMarkedBuildingElements`'s return value is ignored

Licensed TAS returns **-1** after successfully sweeping 28 elements. It is a status, not a count. The pass
therefore establishes what happened by re-reading the building and seeing which of the marked GUIDs are gone.

## Why the re-stamp does not use `AdjacencyCluster.GetAperture(guid, out panel)`

That overload returns as soon as it finds the aperture as a cluster OBJECT and leaves its `out panel` null —
which on this route is every aperture, so every re-stamp refused and every aperture kept a stamp naming an
element the sweep had just deleted. The pass builds its own aperture → panel index and writes **both** shapes
an aperture can be held in, because a stale copy is a stamp that names a deleted element.

## The one content disagreement this pass works around

`Modify.UpdateConstruction` sets `material.width` **only for a TRANSPARENT material**, so the frame
construction `Modify.UpdateConstructions` writes earlier in the workflow keeps the material-library default
there (0.001 m) while `construction.materialWidth` carries the real thickness (0.05 m). A
`ConstructionLayerDefinition` compares BOTH widths, so the resolver correctly declines to adopt it and
creates its own — whose content matches the direct route field for field. Fixing the shared writer instead
would change opaque material content on every route and is out of scope here; the consequence is handled by
invariants 6 and 7 above, and the end state matches the direct route.

---

## What each file does

| File | Role |
|---|---|
| `Modify/ResolveApertureDefinition.cs` | **The one aperture-definition resolver**, extracted from `Modify.Update`. Both routes call it. |
| `Modify/UpdateApertureDefinitions.cs` | The new COM pass: rebind, re-stamp, sweep, reclaim. Reports through `notes`. |
| `Query/ApertureDefinitionBindings.cs` | COM-free mirror of what each aperture part asks for, via the Stage 2 factories. |
| `Classes/ApertureDefinitionBinding.cs` | One (aperture, part) pairing with the definitions it states. |
| `Query/NamesContainingApertureGuid.cs` | The instance-named test, exact against the model's aperture GUIDs. |
| `Query/UnusedApertureBuildingElements.cs` | The element sweep decision, pure. |
| `Classes/ApertureBuildingElementUsage.cs` | An element and its post-rebind surface count, read out of COM into a value. |
| `Query/OrphanApertureConstructionNames.cs` | The construction sweep decision, pure. |
| `Query/SupersededConstructionRenames.cs` | Which freed plain names may be reclaimed, pure. |
| `Query/ZoneSurfaceIndex.cs` | `{ZoneGuid, SurfaceNumber}` → `zoneSurface`, hoisted out of `UpdateBuildingElements` so both rebinding passes resolve a stamp to the same object. |
| `Classes/BuildingReuseCache.cs` | **Extended** with `RefuseSeededDefinitions`. Purely additive. |
| `Classes/WorkflowCalculator.cs` | One gated step, and the step count. |

---

## Front-end reachability

Both front ends construct the same `WorkflowCalculator`, so **both inherit this automatically** and neither
`SAM`, `SAM_UI` nor `SAM_gbXML` needed changing:

```text
Grasshopper  SAMAnalytical.WorkflowgbXML   SAM_Tas_Grasshopper/…/SAMAnalyticalWorkflowgbXML.cs:354
  → Modify.RunWorkflow → SAM.Analytical.Tas.WorkflowCalculator.Calculate → TAS/TBD

SAM_UI       simulate cases                SAM.Analytical.UI.WPF/Modify/CreateSimulateCases.cs:75
  → Modify.RunWorkflow  (writes the gbXML itself: RunWorkflow.cs:187-205)
  → SAM.Analytical.Tas.WorkflowCalculator.Calculate → TAS/TBD
SAM_UI       multitasker workflow          SAM.Analytical.UI.WPF.Grasshopper/…/SAMAnalyticalMultitaskerWorkflow.cs:356
  → the same
```

SAM_UI's *other* TAS export (`Modify/Export.cs:46` → `Tas.Convert.ToTBD`) is the direct route and was already
correct.

---

## Acceptance

### COM-free (`SAM.Analytical.Tas.TM59.Tests/ApertureDefinitionReuseGbXMLTests.cs`, runs in CI)

19 tests, inside a suite of **438/438** passing in Debug and Release:

- 20 identical windows → 40 physical bindings, **2** element definitions and **2** construction definitions,
  and no generated name carries a physical aperture GUID;
- one diverging opening control → exactly **3** definitions, the new one bound by that aperture alone, the
  other 19 still on the shared original, the frame definition untouched;
- restoring it → back to **2**, resolving to the ORIGINAL definition, with no duplicate equivalent left;
- a frame given the pane's own layers → still 2 definitions, distinct `BEType`s, distinct names;
- a repeated run → the same definitions by value and the same names, so the second run creates nothing;
- instance-named detection in both GUID spellings and either case, and *not* on definition-named ones;
- the element sweep: only surface-less, non-canonical aperture elements; a canonical element left empty by a
  refusal is kept; a panel element is never a candidate;
- the construction sweep: instance-named and superseded removed, library definitions kept and reported, a
  still-referenced construction never touched;
- the reclaim: a freed plain name taken by the qualified construction and the bases matching afterwards; a
  name still in use never taken; a contested name given to neither;
- part selection: a frameless window asks for a pane definition only, matching the direct route's own rule.

### Licensed (EDSL Tas, `ModelA.sam` — 2 spaces, 14 apertures, one shared `ApertureConstruction`)

A/B against the `fff9984` baseline. The two builds differ in `SAM.Analytical.Tas.dll` and nothing else —
every other DLL verified hash-identical. One `WorkflowgbXML`-shaped run each.

| | A baseline | B this branch | direct route |
|---|---:|---:|---:|
| zones / total surfaces | 2 / 40 | 2 / 40 | 2 / 40 |
| aperture pane / frame surfaces | 14 / 14 | 14 / 14 | 14 / 14 |
| **aperture buildingElements** | **28** | **3** | **3** |
| aperture constructions | 2 | 2 | 2 |
| aperture types | 2 | 2 | 2 |
| distinct pane bindings (14 apertures) | 14 | **2** | — |
| distinct frame bindings | 14 | **1** | — |
| distinct physical surfaces / collisions | 28 / 0 | 28 / 0 | — |
| TBD apertures the import reports | 14 | 14 | — |

**B's aperture definitions are identical to the direct route's, name for name and surface count for surface
count:**

```text
Windows: Windows: SIM_EXT_GLZ -frame            BEType 15   14 surfaces   0 aperture types
Windows: Windows: SIM_EXT_GLZ -pane             BEType 12   11 surfaces   1 aperture type
Windows: Windows: SIM_EXT_GLZ_6364C1DA -pane    BEType 12    3 surfaces   1 aperture type
constructions   Windows: SIM_EXT_GLZ -pane      Windows: SIM_EXT_GLZ -frame
apertureTypes   Opening Cd0.374 F1              Opening Cd0.448 F1
```

The `_6364C1DA` pane is the legitimate split: the model states two different opening discharge
coefficients, so 11 surfaces take one pane definition and 3 take another — exactly as the direct route does.
No name carries a physical aperture GUID; `_6364C1DA` is a signature hash and is present identically on the
direct route.

Both aperture constructions match the direct route's content field for field, including both widths TBD
stores per layer.

**A repeated `WorkflowgbXML` run over the same `.tbd` reproduces every count above with nothing added.**

Two residual observations, both measured on the baseline too and therefore not from this work: 8 total
building elements against the direct route's 7 (the extra one is a panel element), and a "3 physical surfaces
are claimed by more than one SAM aperture" note from `UpdateBuildingElements` reading the fixture's own
pre-existing stamps, reported identically by A and B.

Stage 3's previously accepted scenarios were **not** re-run: this change touches none of their mechanics.

---

## Deliberate limitations

- **The pass is gated on the gbXML route.** A direct-route or TAS-authored TBD fed through `WorkflowTBD` is
  left completely untouched, so no existing behaviour on those paths can change.
- **`Modify.UpdateConstruction`'s opaque `material.width` write is left alone** — see above. The workaround
  costs one construction created and one removed per affected `ApertureConstruction`, and the end state
  matches the direct route.
- **The doubled `Windows: Windows: ` element prefix is pre-existing** and identical on both routes: on this
  model the SAM `ApertureConstruction` is itself named `Windows: SIM_EXT_GLZ`, and the element naming adds
  its own prefix. Out of scope here.
- **Two `ApertureConstruction`s sharing identical pane layers but different frame layers now round-trip
  correctly** - see `APERTURE_HARDENING.md`. `Modify.ResolveApertureDefinition`'s construction match is
  still content-only by design (*"Identity is the DEFINITION and never the name"*), so the second family's
  pane construction is still the FIRST family's shared object under the first family's base name. What
  changed is the IMPORTER: it no longer pairs a window's halves by that base name. Physical grouping is
  geometric and family identity is the PAIR of construction identities the two halves carry
  (`Query.ApertureConstructionPairKey`), so `(P, F1)` and `(P, F2)` reconstruct as two families however they
  are named. Licensed: 14 windows in two such families imported as 21 apertures with the second family's
  pane content lost; they now import as 14, seven per family, with both halves stamped on every one.

- **`FeatureShade` now reaches the gbXML pane building element** - see `APERTURE_HARDENING.md`. Two
  pre-existing causes, neither introduced by this PR: `Modify.UpdateBuildingElements` resolved the aperture
  through `AdjacencyCluster.GetAperture(guid)`, which answers with a STALE cluster-object copy on a model
  whose apertures are held both on their panel and as cluster objects (every one in the licensed fixture);
  and licensed TAS silently drops the FIRST `AssignFeatureShade` onto a building it has only just written
  with `ExportNew`. This PR's rule that a shade-stating pane is left on its own dedicated element, and its
  frame free to reuse the shared frame definition, is unchanged and is what the shade now lands on.

## Why off-origin models lost every stamp: `Modify.UpdateIds` (fixed on `fix/tas-updateids-gbxml-zone-identity`)

The licensed `ModelA.sam` acceptance above masked one seam: **TAS's own gbXML import recentres the building
footprint on the origin.** Licensed-observed with a hand-written gbXML box at (100..110, 50..60, 0..3): the
TBD surfaces come back at (−5..5, −5..5, 0..3) - exactly minus the footprint centre. `ModelA.sam` is
symmetric about the origin, so its run never moved and every geometric match landed. A real off-origin model
(`SAM_zoningAM_v2`, footprint centre (30.5, 8)) therefore lost the mapping at the FIRST geometric seam:
`Query.Match(zoneSurface, panels)` compared TBD-space internal points against SAM-space panels, so all 110
surfaces matched no panel, `UpdateIds` stamped nothing, and every one of its 40 aperture parts reached
`UpdateApertureDefinitions` with no building-element stamp - which correctly refused them all.

**Two defects were fixed in `UpdateIds`, one proven on the licensed model and one found while tracing it:**

1. **The recentring compensation.** `UpdateIds` now computes the same translation
   `Modify.SetApertureTypes` already applies - the difference of the two sides' non-shade panel
   bounding-box centroids (TBD → SAM, via `building.ToSAM()`) - and passes it to the two
   zoneGuid-aware `Query.Match` overloads, which move the surface's internal point into SAM coordinates
   before the bounding-box / point-in-face tests. Null when either side has no panels: the match is then
   exactly what it was before, so an already-centred model (`ModelA`) is byte-for-byte unchanged.
2. **The zone-identity ordering.** Stage 3 added the stale-stamp clearing that clears
   `SpaceParameter.ZoneGuid` from every space - BEFORE the loop that read that parameter to resolve the
   TBD zone, so the GUID path could never fire and resolution always degraded to the exact-name fallback.
   The clearing is kept (a stale stamp must never survive a failed resolution); the identity is now
   captured first (`Query.SpaceZoneGuids`) and the resolution (`Query.ResolvedZone`) reads the captured
   value: GUID authoritative when it still identifies a zone, exact name only as the compatibility
   fallback, no match a refusal - never an arbitrary zone.

**Licensed on the real model (EDSL Tas):** the 40 instance-named aperture elements collapse to **3
reusable building elements** - one shared frame (20 surfaces) and two distinct pane definitions (15 + 5
surfaces; the model genuinely states two pane contents), with all 110 physical zoneSurfaces unchanged and
20/20 pane/frame stamps written. A second workflow run reproduces every count, creates nothing new, and the
TBD re-imports as 20 apertures. See the SAM_Tas project record.

Revalidated on the production file `SAM_zoningAM_v2zonesisDomestic.sam`: its all-panel, non-shade and
space-related subsets give the same bbox on each side (TBD `[-30.5,-8,0]-[30.5,8,4]`, SAM
`[0,0,0]-[61,16,4]`), so zoning metadata introduces no different translation. Instrumented seams were
9/9 spaces to zones, 110/110 surfaces to panels, 40/40 aperture surfaces to apertures, 20 pane + 20 frame
identifications and 40 building-element stamps. The first and repeated workflows both produced the same
3 definitions and preserved all 110 surfaces and 20 apertures.

The zone-aware panel and aperture `Match` methods also retain their original public CLR signatures for
binary compatibility. Those overloads forward to separate translation-aware overloads; only `UpdateIds`
passes the translation vector. A reflection regression pins all four signatures.
