# Aperture identity across a FULL gbXML round trip

`TBD → FromTBD → new SAM model → ToGbXML → WorkflowgbXML → a NEW TBD`.

The seam the earlier aperture programme (PR #34/#36) never pinned, and the one defect it left behind.
Stages 1–3 are unchanged here: `APERTURE_TYPE_REUSE.md`, `APERTURE_DEFINITION_REUSE.md`,
`APERTURE_DEFINITION_REUSE_GBXML.md` and `APERTURE_INSTANCE_IDENTITY.md` all stand exactly as written.
Nothing below relaxes a refusal, changes what physical identity is, or changes what a definition is.

---

## The workflow that was missing

Previous acceptance covered:

- `SAM → WorkflowgbXML → TBD`, and
- repeated operation against a **kept** TBD.

Both keep one TBD for the whole exercise. Neither exercises the extra reconstruction seam:

```
A0 → ToGbXML → WorkflowgbXML → TBD1
   → FromTBD → A1                      <-- a model rebuilt from one TBD ...
   → ToGbXML → WorkflowgbXML → TBD2    <-- ... and then used to write a DIFFERENT one
```

Across that seam a `Pane`/`FrameBuildingElementGuid` changes meaning, and that is the whole problem.

---

## What a BuildingElementGuid actually says

> **"This part was bound to definition X *in the TBD it was last stamped against*."**

It is a **binding**, never a physical identity — `APERTURE_INSTANCE_IDENTITY.md` establishes that, and it is
unchanged. What this document adds is the other half: a binding is also **scoped to one file**.

TAS mints its own aperture `buildingElement`s on every gbXML/T3D conversion — it must, because the gbXML
opening name carries the aperture GUID that `Query.UpdateT3D` decodes back. So the GUID an aperture imported
from TBD1 carries names an element **of TBD1**. In TBD2 that element does not exist, and the physical surface
the aperture claims really sits on a fresh TBD2 element.

A stale binding is therefore not harmlessly redundant — exactly the argument
`Query.RemoveApertureZoneSurfaceReferences` already made for the *physical* stamps, which TAS likewise does
not promise to renumber consistently.

---

## Root cause

`Modify.UpdateIds` refreshes an aperture against the newly written TBD. It:

1. **clears** every aperture's physical stamps — unconditionally, for every aperture on every panel; then
2. **refills** stamp and binding **only where the geometric match succeeded**.

Step 1 cleared the stamps and **not** the binding. `Pane`/`FrameBuildingElementGuid` was in fact never
cleared anywhere in the codebase. So a part that step 2 could not re-match kept the binding it was given
against the **previous** TBD, and every later pass read it as the current one.

Downstream, `Modify.UpdateApertureDefinitions`:

- counts the part as bound, because `buildingElementGuid_From` is non-empty — so it is **"considered"**;
- hands it to `Query.ApertureRebindKeys`, which refuses it.

Both observed refusal classes fall out of this one state:

| Refusal | Reached when |
|---|---|
| `SAM aperture '…' has representative Pane side stamps but no preserved complete physical surface set` | the stamps were cleared and not refilled, while the stale binding still makes the part look bound. **The message names representative stamps, but the gate only tests the complete-set flag — a fully cleared part reaches it too.** |
| `Physical surface {Zone}/{N} is not currently bound to the element the aperture stamp claims` | the stale TBD1 binding is compared against the surface's real, current TBD2 element |

Hence a whole model could report:

```
Aperture definitions: 40 aperture part(s) considered; 0 rebound onto a shared definition, 0 already on one.
```

The refusals were **correct** — they were simply being asked about state that was never current. That is why
the fix is upstream of them and the gates themselves do not move.

---

## The fix

One new mutator, `Modify.RemoveApertureTasIdentity` (`Modify/RemoveApertureTasIdentity.cs`), which forgets
everything an aperture states about the TBD it was last stamped against — **both parts' physical stamps and
both parts' definition bindings** — and one call site: the unconditional clearing pass in `Modify.UpdateIds`.

The per-part primitive `Query.RemoveApertureBuildingElementGuid` sits beside the existing
`Query.RemoveApertureZoneSurfaceReferences`.

**Deliberately not called from `Modify.SetApertureZoneSurfaceReferences`.** That mutator owns the physical
stamps alone; the direct export and the TBD import both write a binding through it and must keep theirs. Only
a pass that is about to re-resolve the binding against a new TBD may clear it.

### Why the safety semantics are preserved

- A part that **is** re-matched is restamped and rebound in the same pass — outcome unchanged. The licensed
  run below rebinds 40/40 in every generation.
- A part that is **not** re-matched now reads as **unstamped**, and `UpdateApertureDefinitions` records it as
  `count_NoStamp` — "no binding to move and nothing to create" — instead of counting it as bound and then
  refusing. That is the honest record of a refresh that could not resolve it.
- `Query.ApertureRebindKeys` is **untouched**: incomplete set, contested ownership and wrong-element bindings
  all still refuse.
- Nothing infers ownership from a `BuildingElementGuid`; no physical instance is merged; no geometric match
  is made more permissive; no identity is parsed out of an element name.

---

## Licensed acceptance (door-typed fix)

Seed: **`Flat1.tbd` as the failing Grasshopper run itself produced it** — not a rebuilt fixture. Weather
`CIBSE Weather 2021.twd`, sizing-only, short paths under `C:\P39`. Each leg calls exactly what the component
calls: `Convert.ToSAM(path, importUnused, importSurfaceShades)`, `TogbXML(MacroDistance, 0.00001)` +
`Core.gbXML.Create.gbXML`, then `WorkflowCalculator.Calculate` with the settings
`SAMAnalyticalWorkflowgbXML.SolveInstance` builds (`_removeTBD_` false, `_sizing_` true, `_simulate_` false,
`T3DWindowsPlacement_` false, `_useBEthickness_` false, `_addIZAMs_` true). The only thing omitted is
`Grasshopper.Tas.Modify.RunWorkflow`'s WPF progress dialog, which cannot run headless and touches no model
state.

**All four input combinations, two generations each — TBD1 → TBD2 → TBD3:**

| `_importUnused_` | `_importSurfaceShades_` | before the fix | after the fix (gen 2 and gen 3) |
|---|---|---|---|
| false | false | 40 considered / **40 rebound** | 40 considered / **40 rebound**, 3 aperture BEs |
| false | true | 40 considered / **40 rebound** | 40 considered / **40 rebound**, 3 aperture BEs |
| **true** | false | 20 considered / **0 rebound**, 45 elements | 40 considered / **40 rebound**, 3 aperture BEs |
| **true** | true | 20 considered / **0 rebound**, 45 elements | 40 considered / **40 rebound**, 3 aperture BEs |

Every post-fix generation: `zones=9 surfaces=110 surfaces_pane=20 surfaces_frame=20 elements=8
apertureElements=3`, zero `ISSUE` notes, and TBD2 structurally identical to TBD3 — a fixed point.

The two failing rows are the reported symptom, reproduced: TAS wrote `BEType` 14 panes, `UpdateIds` left
every aperture with `paneBE=absent paneKeys=0 frameBE=present frameKeys=2`, and the 40 per-aperture
`Windows: SIM_EXT_GLZ <apertureGuid> -pane/-frame` elements survived — matching the real
`Flat1-rerun.tbd` element-for-element.

---

## Licensed acceptance (PR #39 binding clear, earlier)

Fixture: `SAM_zoningAM_v2.sam` — the 9-zone / 20-aperture Flat1 model. Weather `UKWeather.twd`, sizing-only,
short scratch paths under `C:\TasOut\rt2`, run over the normal Grasshopper route
(`ToGbXML` → `WorkflowCalculator`) via the `run-tas` skill.

### Gate B — TBD1 vs TBD2, and Gate C — TBD3

| | zones | zoneSurfaces | aperture parts | aperture BEs | aperture Cons | pane / frame bindings |
|---|---|---|---|---|---|---|
| TBD1 | 9 | 110 | 40 | **3** | 2 | 2 / 1 |
| TBD2 | 9 | 110 | 40 | **3** | 2 | 2 / 1 |
| TBD3 | 9 | 110 | 40 | **3** | 2 | 2 / 1 |

Three aperture building elements in every generation: **one shared frame and two panes**, the two panes
differing because their aperture-type content genuinely differs. No instance-GUID-named element survives; no
progressive construction proliferation. Every generation reported:

```
Aperture definitions: 40 aperture part(s) considered; 40 rebound onto a shared definition, 0 already on one.
```

with **zero refusals**.

### Gate A — A0 vs A1 vs A2

Same 9 spaces, 50 panels, 20 apertures, one aperture family, identical pane/frame layer content and identical
geometry (total pane area 57.408 m², frame 7.392 m²) throughout. `A1` and `A2` are **identical** on the whole
structural aperture dump — the fixed point is reached at the first reconstruction.

Two A0 → A1 differences, both **pre-existing `FromTBD` reconstruction behaviour, unchanged by this fix**
(byte-identical before and after it):

- the aperture construction is renamed `SIM_EXT_GLZ` → `Windows: SIM_EXT_GLZ`;
- `PartOOpeningProperties` comes back as `ProfileOpeningProperties`, because TAS stores opening control as
  aperture types and profiles rather than as the Part O-specific SAM form. The **aperture-type semantics
  survive** — `Opening Cd0.411 F1` and `Opening Cd0.477 F1` are present and identical in TBD1, TBD2 and TBD3.

### Simulation safety

Comparing every simulation-effective aperture field between generations — zone, part, `BEType`, area,
**construction layer content** (materials and thicknesses, not names) and aperture types:

- TBD1 == TBD2: **identical**
- TBD2 == TBD3: **identical**

The only cross-generation delta is the frame construction's **name** (`SIM_EXT_GLZ -frame` →
`Windows: SIM_EXT_GLZ -frame`), applied once at generation 2 and stable thereafter. Names are diagnostic, not
simulation semantics, so no TSD comparison was required.

---

## Why PR #34/#36 acceptance did not catch it

Their acceptance never rebuilt a SAM model from one TBD and then used it to write another, so an aperture
never carried a *foreign* file's binding into `UpdateIds`. Within a single TBD the binding an aperture holds
is genuinely current, and the un-cleared binding is simply overwritten by a successful match — invisible.

---

## The door-typed pane — the defect the first attempt missed

**Correction.** The paragraph this section replaces said the reported symptom "did not reproduce" and
suggested a Grasshopper install running older DLLs. **Both statements were wrong.** The symptom reproduces
every time, on this same fixture, and the loaded DLL was current — measured, not assumed: the deployed
`SAM.Analytical.Tas.dll` was byte-identical (SHA-256 `D70C6D7F…`) to the repository build and contained the
`RemoveApertureTasIdentity` change. What the first attempt actually missed was **one input flag**.

### TAS does not always type an opening's pane GLAZING

`SAMAnalytical.FromTBD` has an `_importUnused_` input. With it **on**, `Convert.ToSAM` also reconstructs
aperture constructions that no element in the TBD references. A TBD written by the previous generation holds
exactly such a leftover — the superseded frame construction the definition sweep freed — and it comes back as
an `ApertureConstruction` with **frame layers and no pane layers**. `TogbXML` then writes it out as a second
`<WindowType>` carrying a `<Frame>` and no `<Glaze>`.

That single unreferenced, pane-less `WindowType` is the whole trigger. An A/B on the two exported gbXMLs —
identical but for it and one unused opaque `<Construction>` — flips TAS's own gbXML import from writing
`GLAZING` (`BEType` 12) panes to writing **`DOORELEMENT` (`BEType` 14)** panes, for **every opening in the
model at once**. TAS decides this per file, not per window.

### Two readings of that element disagreed with the import's

| reader | helper | a door-typed pane surface is … |
| --- | --- | --- |
| `Convert.ToSAM` | `Query.AperturePart_BuildingElementType` | a **PANE** ✔ |
| `Query.Match`, i.e. `Modify.UpdateIds` | `Query.AperturePart(int)` | a **FRAME** ✘ |
| the sweep in `Modify.UpdateApertureDefinitions` | `ApertureBuildingElementUsage.IsAperture` | **not an aperture element at all** ✘ |

`Query.AperturePart(int)` is the *write*-side helper, where "the half that is not glazing" is what is wanted;
it is correct where it is used and is unchanged. Reading a TBD element with it is not. The doc-comment on
`Query.AperturePart_BuildingElementType` had predicted the exact consequence — *"would classify a door's own
surface as the frame and leave the opening with no pane"* — but `Query.Match` had never been moved onto it.

### What that did, measured

`Modify.UpdateIds` collected **both** of a window's surfaces into its frame set and none into its pane set,
and wrote `FrameBuildingElementGuid` from whichever element the pass touched last — the pane's. So:

- the pane part carried no binding, so `Modify.UpdateApertureDefinitions` counted it `no stamp` and skipped it;
- the frame part claimed two surfaces sitting on two different elements, so `Query.ApertureRebindKeys`
  refused it — **correctly**, about state that only existed because of the misreading.

Result: `40 aperture part(s) considered; 0 rebound`, and TBD2 keeping **40 per-aperture, GUID-named
building elements** instead of 3. That is the reported symptom, exactly.

And once the rebind was fixed, the sweep still could not remove the twenty per-aperture door elements it had
just emptied, because its own aperture test named only `GLAZING` and `FRAMEELEMENT`. Twenty surface-less
leftovers remained in TAS's Building Elements list — the very thing the sweep exists to prevent.

### The fix

One reading, in one place:

- `Query.AperturePart_BEType(int)` is added beside `Query.AperturePart_BuildingElementType`, which now
  delegates to it. It is the single definition of *which half of an opening a TBD element is*: `GLAZING`,
  `ROOFLIGHT` and `DOORELEMENT` are panes, `FRAMEELEMENT` is a frame, everything else refuses.
- `Query.Match` reads it instead of `Query.AperturePart(int)`.
- `ApertureBuildingElementUsage.IsAperture` asks it too, so the sweep recognises exactly what the reader does.

Nothing is loosened. Physical identity is still `{ZoneGuid, SurfaceNumber}`; a `BuildingElementGuid` is still
only a definition binding; the sweep's other two gates (holds no surface, is not canonical) are untouched, so
an element standing for a real opening is still never a candidate. `Query.ApertureRebindKeys` is unchanged.

### The clearing pass had the same two-shapes gap the fix above closes

Caught in review, before merge. An `AdjacencyCluster` can hold the same aperture in **two shapes**: on its
panel (what `panel.Apertures`, `Query.AperturePhysicalIndex` and the export's own membership map all read)
and, independently, as a cluster **object** in its own right — real models carry both, `_importSurfaceShades_`
being one route that does. `AdjacencyCluster.GetAperture(guid)`/`GetObject<Aperture>(guid)` return the
standalone copy, not the panel one (see `AperturePanelIndex.cs`). `Modify.UpdateApertureDefinitions`'s own
successful restamp already writes **both** shapes for exactly this reason
(`RestampApertureBinding`) — but `UpdateIds`'s clearing loop only cleared the panel-held copy.

So a part the refresh could not re-match ended the pass **unstamped on its panel** (the honest, current
state) while its standalone copy still carried the previous TBD's binding — the exact stale state this whole
document exists to eliminate, now reachable through `GetAperture`/`GetObject<Aperture>` instead of through
the panel. Fixed by clearing the standalone copy alongside the panel one, in the same loop, through the same
`RemoveApertureTasIdentity` mutator. Re-verified licensed with `_importSurfaceShades_` on (the mode that
creates the standalone copies): still `40 considered / 40 rebound`, zero refusals, in both generations.

### Why the earlier library harness passed

It called `Convert.ToSAM(path, importUnused: false, …)` — the flag left at its default. The failing Grasshopper run had
`_importUnused_` **on** — visible in its own artifacts as the second `<WindowType>` in `Flat1-rerun.xml`.
With the flag off there is no pane-less `WindowType`, TAS types the panes `GLAZING`, and the misreading never
fires. One unexercised input, and the harness reported a green 40/40 on a chain that fails in Grasshopper.
`_importSurfaceShades_` turned out to be irrelevant to the failure (it only adds standalone cluster aperture
copies); the licensed acceptance above now covers both flags in both states.

---

## Remaining limitations

- The `Windows: ` construction-name prefix added at the first round trip is pre-existing naming behaviour and
  is not addressed here. It applies once and does not accrete.
- `Convert.ToSAM(importUnused: true)` reconstructing a **pane-less** `ApertureConstruction` from an unpaired
  frame construction, and `TogbXML` then exporting it as a `<WindowType>` with no `<Glaze>`, is left as it
  is. It is what provokes TAS into door-typing the model, and it is arguably wrong on its own terms — but the
  round trip is now correct whichever way TAS types an opening, which is the property worth having. Fixing
  the export as well would only hide a class of input the reader must handle regardless.
- `PartOOpeningProperties → ProfileOpeningProperties` is a lossy edge of TBD reconstruction, not of this
  seam.
