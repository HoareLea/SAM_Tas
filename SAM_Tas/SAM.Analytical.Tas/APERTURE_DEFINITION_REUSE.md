# Reusable TBD aperture constructions and building elements (Stage 2)

Stage 2 of the frozen three-stage aperture-definition plan. Stage 1 — reusable
`TBD.ApertureType`s and schedules — is `APERTURE_TYPE_REUSE.md` and is **unchanged** here except for the
cache extension this stage needs.

**Scope: the direct `Modify.Update` export only.** (The gbXML/T3D route has since been brought to the same
behaviour — see `APERTURE_DEFINITION_REUSE_GBXML.md`, which reuses this stage's resolver rather than
duplicating it. Everything below still describes Stage 2 itself.) The gbXML/T3D route,
`UpdateBuildingElements`, `UpdateIds`, the import grouping, Stage 1 aperture-type and schedule semantics and
the legacy aperture workflows are all untouched — their CODE is unchanged. One of them has a KNOWN, ALREADY-ACCEPTED behavioural
consequence when pointed at a Stage-2 TBD, spelled out where it belongs below rather than left implicit in
"untouched": see "Known limitation: `UpdateBuildingElements` on a Stage-2 TBD".

---

## What changed

The direct export used to create **one `TBD.Construction` and one `TBD.buildingElement` per aperture per
part**, named after `aperture.UniqueName()` — i.e. after the aperture's GUID. Two hundred identical windows
produced four hundred constructions and four hundred building elements.

They are now **reusable definitions**, exactly as a `TBD.ApertureType` already was:

```text
200 identical windows
  → 400 zoneSurfaces          (unchanged: the physical windows)
  →   2 Constructions         SIM_EXT_GLZ -pane / SIM_EXT_GLZ -frame
  →   2 buildingElements      Windows: SIM_EXT_GLZ -pane / Windows: SIM_EXT_GLZ -frame
  →   Stage 1 ApertureTypes and schedules: unchanged
```

---

## Invariants

1. **A shared definition is immutable.** When an equivalent construction or building element is found,
   nothing on it is written — not even rewritten to the same value. Every other aperture referencing it
   would see the write. Pinned by the write-log assertions in
   `ApertureDefinitionReuseTests.Export_ASharedHit_PerformsNoWritesOnTheSharedDefinitions`.

2. **Identity is the definition, never the name.** This replaces the one genuinely unsafe behaviour on this
   path: both objects were looked up **by name**, and a name match was taken as content match. That was
   harmless only because the name carried the aperture's GUID and so matched nothing but itself. With names
   derived from the reusable SAM `ApertureConstruction`, a by-name lookup would hand one window another
   window's glazing. So: full content equality decides reuse; a name already taken by *different* content is
   never adopted and never mutated, and the new object gets a deterministic collision-suffixed name.

   | Definition | Fields |
   |---|---|
   | `ConstructionMaterialDefinition` | Every property `TBD.IMaterial` exposes bar `width`: name, type, description, conductivity, specific heat, density, vapour diffusion factor, the four reflectances, the two emissivities, solar and light transmittance, dynamic viscosity, convection coefficient, `isBlind`. |
   | `ConstructionLayerDefinition` | The material, plus **both** widths TBD stores for a layer (`construction.materialWidth[i]` and `material.width`). |
   | `ConstructionDefinition` | `AperturePart`, `ConstructionTypes` value, `additionalHeatTransfer`, description, and the **ordered** layers. |
   | `ApertureTypeAssignment` | One opening: its Stage 1 `ApertureTypeDefinition` plus the **ordinal** — which occurrence of that control it is. |
   | `BuildingElementDefinition` | `ApertureType` (window/door), `AperturePart`, `BEType`, colour, the `ConstructionDefinition`, and the **ordered** openings. |

   Float comparison is exact, with two deliberate normalisations on the way in: signed zero to positive
   zero, and any NaN to the canonical `float.NaN`. Both keep `Equals` in agreement with a `GetHashCode`
   derived from an IEEE-754 bit-pattern signature. NaN compares **equal** to NaN here — unlike Stage 1's
   `ApertureTypeDefinition`, where Cd and factor are never NaN — because a material that states no
   conductivity stores NaN, and under `==` such a layer would never equal itself, giving every window its
   own construction.

3. **`AperturePart` is part of construction identity**, and not because TAS stores it — it does not. A pane
   and a frame holding identical layers would otherwise collapse into ONE `TBD.Construction`, and the round
   trip would come back with one half of the window missing: the import reads each half's layers off the
   construction that half's building element carries, so the two halves must be two constructions.

   *(Until `APERTURE_HARDENING.md` this invariant also carried the import's pane/frame PAIRING, which used
   to strip the `-pane`/`-frame` suffix and match a window's two halves by the remaining base name. It no
   longer does: the import groups a window's surfaces geometrically and identifies its family by the PAIR of
   construction identities the halves carry. Keeping pane and frame distinct is still required — the
   suffixes are still how a reader tells them apart, and how `Convert.ToSAM_ApertureConstruction` decides
   which layer list a construction is — but the round trip no longer depends on the two names sharing a
   base.)*

4. **No generated name contains physical aperture identity.** The base is the SAM
   `ApertureConstruction`'s own name — a reusable definition shared by every window built from it:

   ```text
   <ApertureConstruction.Name> -pane                    SIM_EXT_GLZ -pane
   Windows: |Doors: <ApertureConstruction.Name> -pane   Windows: SIM_EXT_GLZ -pane
   ```

   This is the shape the TCD route already writes (`Convert.ToTCD_Constructions`), so the two routes now
   agree, and it is the shape the import reads back — which makes the round-tripped `ApertureConstruction`
   carry the model's own name instead of an aperture's unique name.

   **The part suffix stays terminal**, so a collision discriminator goes on the BASE:
   `SIM_EXT_GLZ_1F3A0C21 -pane`. A name ending `-pane_1F3A0C21` would not be recognised as a pane at all.
   The discriminator is the FNV-1a hash of the full signature — deterministic, so the same definition
   resolves to the same name on every repeated export, never a TAS/UI-style `(1)`/`(2)` counter.

   **Underscores are kept** in the base, unlike in Stage 1's aperture-type naming. Real construction names
   are full of them (`SIM_EXT_GLZ`), and this base is the round-trip identity of the `ApertureConstruction`;
   mangling it to `SIMEXTGLZ` would silently rename the construction on every round trip. The underscore is
   still the discriminator's separator, so a base itself ending in `_` plus eight hex digits decomposes
   ambiguously — but only in the BASE, which no production code reads. What decomposition is asked for is the
   part suffix and the `Windows: `/`Doors: ` prefix, and those stay unambiguous.

5. **Opening multiplicity is preserved exactly.** The openings are an **ordered list of
   `(definition, ordinal)`**, so a window with two identical openings is not the same definition as a
   one-opening window, nor as a window carrying only the ordinal-2 type. Stage 1's rule that N children
   produce N distinct aperture types is untouched; what Stage 2 adds is that the *element* carrying them is
   shared with every other window stating the same set.

6. **No openings is a definition, not a gap.** A window stating no `OpeningProperties` has an empty list,
   and every sealed window in the model resolves to the one bare element. An empty list is not equal to a
   one-entry list.

7. **Windows and doors never merge.** `BEType` is written from the aperture PART, so a door's pane carries
   the glazing type and `BEType` cannot tell a door from a window. `ApertureType` is therefore a field of the
   definition in its own right. `ApertureType.Undefined` is a **distinct value, not a missing one**: the
   write has always handled such an aperture (the `Windows: ` prefix covers everything that is not a door),
   so refusing it would take a building element away from an aperture that used to get one.

8. **Registration follows a completed write; a name is reserved from the moment an object exists.** Same
   two-step discipline as Stage 1. A construction is registered as reusable only after its layers are
   written and its content was predictable; an element only once
   `SetApertureTypes` returned as many aperture types as the definition claims. A partly written opening set
   is reserved by name and never shared — a shared element is never written to again, so there would be no
   correcting it.

9. **Identity stamps are unchanged.** `Pane`/`FrameZoneSurfaceReference` and
   `Pane`/`FrameBuildingElementGuid` are stamped exactly as before. After sharing, many apertures
   legitimately stamp the **same** `BuildingElementGuid`; `ZoneSurfaceReference` stays one per physical
   surface. That is what the TSD result mapping (`Modify.AddResults`) and the import both key on. Stage 3
   owns hardening update/round-trip identity.

10. **The physical surfaces are untouched.** Every `zoneSurface`, `RoomSurface` and `Perimeter` the export
    created before, it creates now; the only change in that block is that the two parts are collected in a
    `List` keyed by part rather than a `Dictionary` keyed by the element name (which is no longer this
    aperture's own), which also makes the frame-before-pane ordering explicit rather than incidental.

### Seed gates (a pre-existing object this export must not adopt)

Recorded with a reason, contributing its **name** to collision avoidance and nothing else. Both gates are
pure functions of what was read out of COM (`Query.ConstructionDefinition(string, int, float, string, …)` and
`Query.BuildingElementDefinition(BuildingElementSeed, …)`), which is what makes them testable without an
installed TAS.

**Construction:** a name outside the `-pane`/`-frame` convention (which half of a window it is cannot be
established); materials that will not report; a layer whose material will not read.

**Building element:** a name outside the `Windows: …/-pane` convention; a non-default `ghost`; a non-empty
`description`; an assigned feature shade; an assigned substitute element; a non-default `ground`,
`markDelete` or `width`; a construction that fails its own gate; an opening whose control may not be reused
or whose name does not carry the Stage 1 convention that states its occurrence; any opening at all on a
frame.

Passing every gate makes an object a **candidate**, not a match; its definition is then compared field by
field.

### Why the material mirror is safe

`Query.ConstructionMaterialDefinition(Core.IMaterial)` mirrors what `Modify.UpdateMaterial` writes onto a
fresh `TBD.material`, field for field and clamp for clamp — including the two places the TBD write differs
from its TCD sibling (opaque stores `tcdOpaqueMaterial` not `tcdOpaqueLayer`; the transparent write touches
neither `specificHeat` nor `density`). A field an overload leaves alone is reported as the value a fresh TBD
material holds.

The failure mode is one-directional. Two layers created in one export run through the same mirror on the
same input, so a mirror that disagreed with the writer could not merge two materials the model states as
different — it would give the same answer for both, and both would still get the same TBD material. What a
disagreement would cost is recognising a construction that was **already** in the TBD: under-reuse, never
unsafe sharing.

---

## What each file does

| File | Role |
|---|---|
| `Classes/ConstructionMaterialDefinition.cs` | Immutable value equality over the TBD material fields. COM-free. |
| `Classes/ConstructionLayerDefinition.cs` | One layer: the material plus both widths TBD stores. |
| `Classes/ConstructionDefinition.cs` | The construction: part, type, additional heat transfer, description, ordered layers. |
| `Classes/ApertureTypeAssignment.cs` | One opening: Stage 1 definition plus ordinal. |
| `Classes/BuildingElementDefinition.cs` | The element: aperture type, part, `BEType`, colour, construction, ordered openings. |
| `Classes/BuildingElementSeed.cs` | Everything readable off a pre-existing aperture element, read out of COM and into a value. |
| `Query/ConstructionMaterialDefinition.cs` | The COM-free mirror of `Modify.UpdateMaterial`. |
| `Query/ConstructionDefinition.cs` | COM-free factory from a SAM `ApertureConstruction` + part + `MaterialLibrary`. |
| `Query/ConstructionDefinitionTBD.cs` | The seed READER: a `TBD.Construction`/`TBD.material` → values. Decides nothing. |
| `Query/BuildingElementDefinition.cs` | COM-free factory from a SAM `Aperture`, plus `ApertureTypeAssignments`. |
| `Query/BuildingElementDefinitionSeed.cs` | The two seed GATES, both pure functions. |
| `Query/BuildingElementDefinitionTBD.cs` | The seed READER for an element. Decides nothing. |
| `Query/ConstructionSignature.cs` | Deterministic FNV-1a signatures and collision hashes over exact Single bit patterns. Never `GetHashCode`. |
| `Query/ConstructionName.cs` | Construction name derivation, sanitisation, decomposition. |
| `Query/BuildingElementName.cs` | Element name derivation and decomposition; the `Windows: `/`Doors: ` prefixes. |
| `Classes/BuildingReuseCache.cs` | **Extended**: constructions and aperture building elements alongside Stage 1's schedules and aperture types. Purely additive. |
| `Modify/Update.cs` | The aperture block of the direct export, rewritten to resolve definitions instead of names. Its resolve-or-create half now lives in `Modify/ResolveApertureDefinition.cs`, shared with the gbXML route. |

**Deferred seed classification.** The cache constructor reads only the NAMES of the building's constructions
and elements — a name is what occupies the namespace, and it is one property read. The content read that
decides reuse happens on the first lookup that needs it (`EnsureSeedClassification`). `Modify.UpdateIZAMs`
re-enters `Modify.Update` once per air handling unit with a synthetic, aperture-less cluster; classifying
every seeded construction's layers on each of those passes would be a per-IZAM COM cost paid for an answer
nothing asks for.

---

## Deliberate limitations

- **Refusals are not reported on this entry point.** `Modify.Update` returns `void` and has no notes
  channel; adding one would change its signature and every caller. The refusals are therefore discarded, and
  what that costs is diagnosability, not correctness — every outcome is the conservative one, an extra
  definition or none. Stage 3 owns reporting.
- **A double name collision refuses, and the surfaces then carry no building element.** Same "rather than
  guess a third name, write nothing" discipline as Stage 1. Reaching it needs both the preferred and the
  signature-qualified name taken by content this export cannot adopt.
- **The panel path still resolves its construction and element by NAME.** Unchanged, and out of Stage 2's
  scope. Aperture naming consults the panel names too, so an aperture never collides with a panel; the
  reverse would need a SAM `Construction` literally named `X -pane`.

---

## Acceptance

### COM-free (`SAM.Analytical.Tas.TM59.Tests/ApertureDefinitionReuseTests.cs`, runs in CI)

104 tests. Construction: equality field by field, layer order, both widths, every one of the seventeen
material-content fields (parameterised), description, pane vs frame with identical layers, NaN and signed
zero, unproven content, signature determinism and exact bit-pattern identity, the COM-free factory
(including the skip of a layer the library cannot resolve, and the gas clamp), naming and its deterministic
collision suffix with the part suffix still terminal, the underscore-preserving base, decomposition, and the
seed gates including **same name / different content → a different definition**.

Building element: reuse on equal definitions, and a separate definition for a different construction,
colour, `BEType`, opening control, opening multiplicity or ordinal; windows vs doors; pane vs frame; the
bare no-openings definition; `ApertureType.Undefined`; every seed gate individually; naming and
decomposition.

A fake-COM harness whose fakes **record every property set** drives the export decision sequence in
`Modify.Update`'s own order, delegating every decision to the production helpers: 200 identical windows →
400 surfaces, 2 constructions, 2 elements; 99 further identical windows add **not one write** to either
shared object; a repeated export adds nothing; a mismatched existing construction and a mismatched existing
element are **never written to** and a distinct collision-suffixed object is created instead; 50 windows ×
2 identical openings keep both openings on one shared element; and a mixed model of several constructions,
several opening controls, sealed windows and doors resolves to one definition each with no physical GUID in
any generated name.

### Licensed TAS — COMPLETE, every row passes (2026-08-21; EDSL Tas build 17044)

| Scenario | Expected | Result |
|---|---|---|
| 200 identical windows, direct export | 400 aperture surfaces; **2** constructions; **2** elements; Stage 1 type and schedule counts unchanged | **PASS** — 400 (200 pane + 200 frame); 2 constructions; 2 elements; 1 ApertureType, 0 schedules |
| several constructions / controls / sealed windows / doors | one definition each, no GUID in any name | **PASS** — all 18 definition-variant scenarios (C1–C8, B1–B8) |
| repeated export | **0** additional equivalent definitions | **PASS, after a fix** — see below |
| round trip SAM → TBD → SAM | aperture count, geometry, pane/frame classification, construction layers, `OpeningProperties` all preserved | **PASS, A/B against `f3f5802`** — see below |
| **A/B simulation** — same real model exported with the old per-aperture behaviour and with shared elements, both run through TAS/TSD | zone results numerically equivalent within solver noise | **PASS — exactly identical, not merely within noise.** Real 9-zone Part O model, 20 pane+frame windows, model's own weather, full year 1–365 both sides. Aperture building elements 40 → 3; all 110 physical `zoneSurface` rows and all 510 construction/material/width lines byte-identical *before* simulating. **4,616,520 result values compared (22 zone variables × 9 zones + 7 surface variables × 47 surface records, × 8760 h); 0 differing; max absolute and relative difference 0.** |
| one real shaded project | `UpdateShading`, `CopyResults` and aperture solar-result mapping unaffected | **PASS.** Kolobrzeg room with real shading context, 3 pane+frame apertures, aperture elements 6 → 2. Full chain `Simulate_Coverage` → `ToTBD` → `UpdateShading` → TAS 1–365 → `Create.SolarModel` → `CopyResults`: **114 compared fields, 0 differing** (shade-day calendar, 7200 shade-proportion reads over 12 shaded surfaces, 12 linked faces / 12 coverage results / 3096 coverage values, 3 apertures mapped with 5 pane + 5 frame + 62 panel results and identical per-aperture sums), plus **928,560 TSD values, 0 differing.** |

Full evidence, the pre-simulation equivalence table and the per-variable breakdown are in
the SAM_Tas project record, "5. A/B TAS/TSD simulation" and "6. Real shaded-project regression".

**Confirmed on licensed TAS:** the value a freshly added `buildingElement` carries for `ground`,
`markDelete` and `width` is `0` for all three, live and after save/reopen — exactly what the seed gate
already assumed. **No fix was needed here.**

**A fix WAS needed one level down, on the construction-material mirror**, found by this exact repeated-export
row. `Query.ConstructionMaterialDefinition` (`Query/ConstructionMaterialDefinition.cs`) mirrors what
`Modify.UpdateMaterial` writes onto a fresh `TBD.material`; it assumed a fresh, untouched
`dynamicViscosity`/`convectionCoefficient` reads back as `0` for opaque and transparent materials (accurate
that the opaque/transparent `UpdateMaterial` overloads never write them — only a `GasMaterial` does — but
wrong about what TAS's own default for an untouched field is). Licensed TAS reports `1E-05`/`0.001`, not
`0`. That mismatch made every opaque/transparent layer compare as content-different from its own
just-created seed, so a repeated export created a second, collision-suffixed Construction and
BuildingElement instead of reusing the first — the exact "repeated export adds nothing" failure this row
exists to catch, and the same class of issue anticipated below for `ground`/`markDelete`/`width`, just on
the material mirror rather than the building-element seed gate. Fixed by using the confirmed TAS defaults
for those two fields on the opaque/transparent branches; verified end to end (repeat export now shows `+0`
on Construction/BuildingElement/ApertureType/schedule/distinct-BE-guid) and against the full COM-free suite
(337/337, unchanged). The conservative foreign-object refusal gates were not touched.

**The round-trip row is an A/B, not an absolute comparison.** Comparing a round-tripped SAM model with its
source finds real differences that have nothing to do with this stage — the import reassembles the exported
pane and frame surfaces into one `Face3D` with a hole, so the aperture's own `GetFace3D().GetArea()` reads
the frame ring rather than the whole opening; layer thicknesses come back through `float`; TBD stores no
material `Group`, and its transparent `UpdateMaterial` overload writes neither `specificHeat` nor `density`,
so both read back `NaN`. The row therefore asks only whether **Stage 2 differs from the `f3f5802` baseline**,
and it does not: running the identical model and code path with only `SAM.Analytical.Tas.dll` swapped, all
219 SAM-side fields and all 186 TBD construction/material fields are identical, and the only differences are
the intended sharing effects (aperture building elements 4 → 3 over two windows, definition-derived names,
and the `BuildingElementGuid` stamps that follow). Physical geometry is untouched: pane 0.99 m², frame
0.21 m², exactly what `Aperture.GetFace3Ds` derives from the 1.2 m² source aperture, on both sides.
Full field-by-field record in the SAM_Tas project record, "4. Round trip".

`CopyResults` already matches apertures to solar surfaces by **geometry**, having recorded that the stamped
building-element GUID "is actually the *construction* GUID (shared across all surfaces using the same
construction), so it only separates pane-construction from frame-construction". Shared elements therefore
do not degrade that mapping — but the shaded-project run confirms it rather than assuming it.

---

## Known limitation: `UpdateBuildingElements` on a Stage-2 TBD — RESOLVED BY STAGE 3

`Modify.UpdateBuildingElements` is a separate, public, supported follow-up API (also exposed as the
`Tas.UpdateBuildingElements` Grasshopper component) that re-applies colour, opening-control writes and
`FeatureShade`s onto an *already-exported* TBD, without a full re-export. It identifies the SAM aperture a
`TBD.buildingElement` belongs to by decoding the aperture's own GUID out of the element's **name**
(`Query.UniqueNameDecomposition` → `AdjacencyCluster.GetAperture(guid)`).

That decode was safe as long as every aperture element's name carried its own aperture's GUID — true for
every element the direct export wrote before this stage. Stage 2 element names are **definition-derived**
(`Windows: SIM_EXT_GLZ -pane`, invariant 4 above) and carry no physical aperture identity at all — by design,
since one shared element can now legitimately belong to many apertures, so there is no longer a single GUID
that could go in the name.

**Consequence:** feeding a Stage-2-produced TBD into `UpdateBuildingElements` cannot resolve any aperture
element by name. Per element this means: colour falls back to the generic per-`ApertureType` default instead
of the aperture's own (`Query.Color`); the opening-control/schedule write and the `FeatureShade` write are
both skipped entirely for that element. This is **not silent corruption** — the 4-argument overload
(`out List<string> notes`) increments `count_GlazingWithoutAperture` and reports it in the summary note the
same way it already does for any TAS-authored/legacy TBD with no GUID-carrying name — but the 2-argument
overload the Grasshopper component calls **discards `notes`**, so that surface reports only a boolean
`successful`, with no visibility into the degradation. This second gap pre-dates Stage 2 (it already applied
to any TAS-authored TBD) — Stage 2 adds a new, common case that triggers it.

This was foreseen and explicitly deferred when the three-stage plan was approved: *"A Stage-2 TBD fed to
[`UpdateBuildingElements`] hits the existing `count_GlazingWithoutAperture` note — degraded but honest;
Stage 3 restores it via the instance mapping."* The fix — resolving elements via the definition-membership
map (`Pane/FrameBuildingElementGuid` → stamped apertures) and the `ZoneSurfaceReference` surface index
instead of name decoding, with name decoding kept only as the legacy fallback — is Stage 3's own first item
(§F.1–F.3 of the plan), not something this stage can do without either putting physical identity back into
shared names (defeating the point of sharing) or building the Stage 3 machinery early.

**Until Stage 3 lands:** a model export pipeline that relies on `UpdateBuildingElements` as a follow-up step
after the direct `Modify.Update` export (rather than a full re-export) will not get colour/opening-control/
feature-shade updates on aperture elements from that follow-up call. A full re-export via `Modify.Update`
itself is unaffected — it writes constructions and elements directly, never through this name-decode path.

**STAGE 3 HAS LANDED, and this paragraph is now history.** `UpdateBuildingElements` resolves the SAM
aperture(s) an element stands for from the `Pane`/`FrameBuildingElementGuid` stamps and the
`ZoneSurfaceReference` surface index instead of the element name, so a Stage-2 TBD is resolved fully. Name
decoding survives only as the legacy fallback for an element no aperture stamps, byte-for-byte unchanged, so
TAS-authored TBDs behave exactly as they always have. A shared element is still never mutated: a member whose
SAM aperture has diverged is SPLIT onto its own element and only its own physical surfaces are rebound. Full
detail in `APERTURE_INSTANCE_IDENTITY.md`.

## Out of scope for Stage 2

Physical-instance identity hardening (`ZoneSurfaceReference` resolution on update), the
`UpdateBuildingElements` name-decode replacement, the import grouping fixes, refusal reporting on
`Modify.Update`, and the gbXML/T3D route — all Stage 3 or later, all unchanged here.

Every one of those is now done: the first four in `APERTURE_INSTANCE_IDENTITY.md`, and the gbXML/T3D route
in `APERTURE_DEFINITION_REUSE_GBXML.md` — where `Modify.UpdateApertureDefinitions` rebinds each physical
surface onto the definition its aperture states, through the very resolver described here (extracted from
`Modify.Update` as `Modify.ResolveApertureDefinition` so the two routes cannot drift). Stage 2's own
invariants are untouched by any of it — `Modify.Update` still resolves definitions by exactly the rules above, and
the only change Stage 3 makes to this file's subject is that identity stamp 9 ("Stage 3 owns hardening
update/round-trip identity") is now discharged.
