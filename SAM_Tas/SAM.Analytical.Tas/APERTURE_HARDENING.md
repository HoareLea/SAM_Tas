# Aperture hardening: identity for a write, and the importer's pane/frame relationship

Two defects left open by the reusable-aperture programme (`APERTURE_DEFINITION_REUSE.md`,
`APERTURE_DEFINITION_REUSE_GBXML.md`), both **pre-existing on both routes** and both fixed here. They share
one theme — *resolve by identity, never by a name or by whichever copy answers first* — but they are
separate defects and neither fix changes any reusable-definition rule.

Nothing in this work touches definition equality, physical-instance identity, the pane/frame distinction,
the immutability of shared definitions, or the split/rebind discipline.

---

## 1. `FeatureShade` never reached the TBD pane

### Root cause: the export read the wrong copy of the aperture

An `AdjacencyCluster` can hold one aperture in **two shapes**:

- on its **panel** — what `AdjacencyCluster.GetApertures()`, `Query.AperturePhysicalIndex` and
  `UpdateBuildingElements`' own membership map all read, and the only shape an ordinary edit
  (`panel.RemoveAperture` / `panel.AddAperture`) reaches;
- as a **cluster object** in its own right.

Real models carry both — **all 14 apertures in the licensed `ModelA.sam` fixture do, straight off disk.** The
two copies are not kept in step.

`AdjacencyCluster.GetAperture(guid)` tries `GetObject<Aperture>` **first** and returns as soon as that hits.
So it answers with the cluster object — the copy the user's edit never reached — and its
`GetAperture(guid, out panel)` overload leaves `panel` null for the same reason.

`Modify.UpdateBuildingElements` resolved through it in two places, and everything it writes off the returned
aperture was therefore read from the stale copy: **colour, opening controls and the feature shade alike.**

**The name decode was never the problem.** A licensed probe confirmed the legacy path resolves perfectly on
the gbXML route — all 14 pane elements decoded their aperture GUID out of TAS's element name and found the
aperture (`decoded=True … found=True`). It simply found the wrong *copy*:

```text
decode  name=Windows: SIM_EXT_GLZ a2c8ed88-… -pane  decoded=True  found=True
        standaloneObject=True  statedViaGetAperture=False  statedViaPanel=True
```

### Second root cause: licensed TAS drops the first `AssignFeatureShade`

Resolving the right aperture was necessary but not sufficient. With the correct aperture in hand,
`Modify.SetFeatureShades` still left the element with **no shade**, while returning as if it had succeeded.

Measured on licensed TAS, writing the same shade to the same element three times in a row reads back
`0, 1, 1`; a raw `Building.AddFeatureShade` / `buildingElement.AssignFeatureShade` pair with no SAM code in
between reproduces it exactly, and re-assigning **the same object** a second time lands it:

```text
attempt1  returned=1  elementShades=0
attempt2  returned=1  elementShades=1
rawAssign1              elementShades=0
rawAssign2(sameObject)  elementShades=1
```

The first assignment onto a building TAS has only just written (`T3DDocument.ExportNew`) is silently a
no-op.

### The fix

| | |
|---|---|
| `Classes/AperturePanelIndex.cs` | Aperture GUID → the **panel-held** aperture and its owning panel, built from the panel walk. No fallback to `GetObject<Aperture>`: an aperture no panel holds is not part of the model's physical fabric, and inventing one would put the stale copy back. |
| `Query/AperturePanelIndex.cs` | `adjacencyCluster.AperturePanelIndex()`. |
| `Modify/UpdateBuildingElements.cs` | Both aperture resolutions — the legacy GUID-decode path and the Stage-3 split path's re-stamp — go through the index. Adds a `feature shade stated / written` summary note. |
| `Modify/UpdateApertureDefinitions.cs` | Its own local panel dictionary (added in PR #34 for exactly this hazard) replaced by the shared index, so both passes resolve an aperture the same way. |
| `Modify/SetFeatureShades.cs` | The assignment is **established by re-reading the element**, and repeated up to three times until it takes. The retry re-assigns the *same* `TBD.FeatureShade` — never a new one — so it can leave neither a duplicate on the element nor an orphan on the building. If it never attaches, the caller is told nothing is on the element rather than being handed the object created for it. |

`UpdateApertureDefinitions`' existing rule is unchanged and now does what it was written for: a pane stating
a `FeatureShade` is left on its own dedicated element and never considered for a shared definition, while
**its frame still reuses the shared frame definition normally.**

### What a shaded aperture now produces

`ModelA.sam` (14 windows, one shared `ApertureConstruction`), one aperture given a `FeatureShade`, run
through `WorkflowCalculator`'s own gbXML branch:

| | baseline `e9b5a3d0` | this branch |
|---|---:|---:|
| aperture building elements after `UpdateApertureDefinitions` | 3 | **4** |
| feature shades on the pane elements | **0** | **1** |
| shaded pane's own element / surfaces | — | dedicated, 1 |
| shared frame element surfaces | 14 | 14 |
| unshaded panes on shared definitions | 11 + 3 | 10 + 3 |
| apertures the import reports | 14 | 14 |

The fourth element is the shaded pane's own, which is the intended Stage-3 outcome, not a regression: the
unshaded 13 still collapse onto the same shared definitions, and the shaded pane's **frame** is one of the
14 on the shared frame definition. The shade survives save/reopen, and a **second run of the whole workflow
reproduces every count with nothing added and no second shade.**

---

## 2. The importer paired a window's halves by construction NAME

### Root cause

Stage 2 shares a definition **by value**. Two SAM `ApertureConstruction`s with identical pane layers and
different frame layers therefore export as **one** shared pane construction — under whichever family created
it — plus **two** frame constructions:

```text
Family A   pane P   frame F1        TBD:  "A -pane"   "A -frame"
Family B   pane P   frame F2              (shares "A -pane")   "B -frame"
```

The importer bucketed a zone's aperture surfaces by the construction **base name** left after stripping
`-pane`/`-frame`, and grouped each bucket geometrically. Family B's pane fell into Family A's bucket and its
frame into a bucket of its own, so **one window came back as two apertures — one frameless, one paneless.**

Measured on the licensed baseline, 14 windows in two such families:

```text
IMPORTED  apertures=21                       (14 physical windows)
          FAMILY_A 14   FAMILY_B 7
          FAMILY_B paneLayers=0              (its pane content lost entirely)
          stamps: both=7  paneOnly=7  frameOnly=7
```

### The relationship rule after the fix

**Physical grouping is geometric. Family identity is the pair of construction identities. The name is a
label chosen afterwards.**

1. **Which surfaces are one physical aperture** — every aperture surface in a zone goes into ONE list, and
   `Query.GroupAperturePolygons` groups them by coincidence: a frame ring and the pane inset in it. No
   construction, and no name, takes part in this decision. (Two distinct windows are never coincident, so
   every group that formed under the old per-construction bucketing forms identically now — except the
   cross-family case, which is the defect.)

2. **Which half each surface is** — `Query.AperturePart_BuildingElementType`, i.e. the element's own
   `BEType`. The `-pane`/`-frame` construction-naming convention is consulted **only** where `BEType` says
   nothing, and the "if all else fails, the group's seed is both" last resort is unchanged and still applies
   only to a group of more than one surface.

3. **Which family the aperture belongs to** — `Query.ApertureConstructionPairKey(paneKey, frameKey)`, where
   each key is the half's `TBD.Construction.GUID` (its name only when it carries no GUID), and an absent
   half keys as empty. Two apertures share a SAM `ApertureConstruction` **iff they have the same pair**.
   `(P, F1)` and `(P, F2)` are different families however they are named; `(P, F1)` is one family in every
   zone it appears in; and `(P, no frame)` never merges into `(P, F)`.

4. **The family's content** — pane layers from the pane half's construction, frame layers from the frame
   half's. Not the old "combine whichever side is empty by name" merge.

5. **The family's NAME** — `Query.ApertureConstructionName`: the pane's base name preferred, the frame's
   base name when that is already another family's, and the lowest free `~n` discrimination when both are.
   For an ordinary window whose two halves agree, both candidates are the same name and this is exactly what
   the import has always produced. Where they disagree, the taken one is the borrowed half and the free one
   is the family's own, which recovers the original name in both directions.

**Names are a label, not the relationship.** `Query.ApertureConstructionNameBase` — stripping the part
suffix — is the only place a name is still read, and only to choose what to call a family already
identified.

### What the round trip now produces

Licensed, 14 windows split into two families, exported through the direct route and imported back:

| | baseline `e9b5a3d0` | this branch |
|---|---:|---:|
| physical apertures imported | **21** | **14** |
| families reconstructed | A ×14, B ×7 | **A ×7, B ×7** |
| Family B pane layers | **0 (lost)** | 3, identical to A's |
| F1 / F2 | — | **distinct** (0.05 / 0.10) |
| pane+frame stamps | both=7, paneOnly=7, frameOnly=7 | **both=14** |

The inverse case — `Family C = pane P1 + shared frame F`, `Family D = pane P2 + shared frame F` —
reconstructs the same way: 14 apertures, C ×7 and D ×7, both frames identical and the two panes distinct.

**A single-family model is unchanged**: `ModelA.sam` imports as 14 apertures under one
`Windows: SIM_EXT_GLZ`, with `both=14` stamps, on both routes.

### Interaction with the shade fix

A shaded pane keeps its **own dedicated building element** but that element still carries the **shared pane
construction**, so its pair is the same as its unshaded siblings' and it rejoins their family on import:
the shaded gbXML fixture imports as **14 apertures under one construction, `both=14`**. Verified explicitly.

---

## Residual limitations

- **`Convert.ToTBD(analyticalModel, …)` — the direct `SAMAnalytical.TBD` export — writes no aperture
  `FeatureShade` at all.** `Modify.Update`'s shade block has been commented out since long before this work;
  `Modify.UpdateBuildingElements` is the only writer of an aperture shade anywhere, and the direct export
  does not call it. A shade reaches a directly-exported TBD only via the `Tas.UpdateBuildingElements`
  component or `WorkflowTBD`, and on a Stage-2 TBD that path additionally cannot resolve a
  definition-named element to an aperture on its first run (the known limitation in
  `APERTURE_DEFINITION_REUSE.md`). Out of scope here; not made worse.

- **The import does not read a `FeatureShade` back off a TBD element.** `Convert.ToSAM(TBD.IFeatureShade)`
  exists but `Convert.ToSAM_AdjacencyCluster` never calls it, so a round trip does not restore
  `ApertureParameter.FeatureShade` onto the SAM aperture. Pre-existing; the shaded aperture still
  round-trips as one correctly paired aperture.

- **A family NAME can still be borrowed** where the model's constructions no longer carry both families'
  base names. On the gbXML route the pre-existing `Modify.UpdateConstruction` material-width disagreement
  (documented in `APERTURE_DEFINITION_REUSE_GBXML.md`) can leave a shaded pane on a value-identical but
  *different* construction object from its unshaded siblings', which is genuinely a third pair, and the
  reconstructed names then follow the constructions that survive rather than the original family names. The
  aperture COUNT, the pane/frame pairing and the layer content are correct in every case; only the labels
  drift. Identity is the pair, so nothing downstream depends on the label.

- **`Query.UpdateT3D` still resolves its aperture through `AdjacencyCluster.GetAperture(guid)`** and so can
  read frame percentage, colour and window position type off the same stale copy. It is the same defect
  class, was not part of either reported issue, and changing it needs its own licensed T3D validation.
