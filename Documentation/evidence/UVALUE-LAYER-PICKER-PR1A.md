# U-value workflow PR1a - default layer picker excludes gas and glass (PR record)

Branch `fix/uvalue-layer-picker-2026-10-01`, from `sow/2026-Q3` `057faf37`. **Not merged.** `PROJECT_PROGRESS.md`
is not touched on this branch (closeout after merge, per `AGENTS.md`). Plan: SAM_UI
`documentation/plans/UValue-Workflow-PLAN.md`. Companion: SAM_UI PR1b (specific error messages + real-app evidence),
which depends on this PR being merged and `SAM_Tas\build` rebuilt.

## Status

Implemented and unit-tested; PR open for review. Waiting for CI and explicit merge approval. The TCD-side
fallback is compile- and unit-covered through the shared rule; its real COM path is exercised by the PR1b
real-app evidence.

## Problem (verified against code and the user's model)

`Create.LayerThicknessCalculationData` picked the lowest-conductivity layer of 10 mm or more. On wall
`SIM_EXT_SLD` (Air 50 / cement particleboard 12 / mineral wool 80 / Air 50 / rainscreen 3) the model's gas
materials have real, positive conductivities (0.024 and 0.01622 W/mK), below mineral wool's 0.025, so the air
cavity won; the target U was then not bracketed, `Calculate_ByDivision` returned NaN and the UI said "Could not
calculate construction for given criteria." NaN conductivity can never win (`NaN < min` is false), so the
earlier "0 or NaN" hypothesis was wrong. The `layerIndex == -1` fallback in `ThermalTransmittanceCalculator`
skipped only conductivity <= 0, had no 10 mm filter, and would also pick gas.

## Change

- New `Query.IsAdjustableLayer(MaterialType, conductivity, thickness)` - one rule, by material **type**: gas and
  transparent never qualify; conductivity NaN or <= 0 and thickness < 10 mm (tolerance 1e-6 m for TCD's float
  widths) do not qualify. `Query.AdjustableLayerIndex(...)` returns the lowest-conductivity qualifying layer, or
  -1 (no gas fallback). Overloads: `Construction` + `MaterialLibrary`; a plain list of (type, conductivity,
  thickness); `IEnumerable<TCD.material>` (maps `TBD.MaterialTypes` gas / transparent / opaque).
  New file `SAM.Analytical.Tas/Query/AdjustableLayerIndex.cs`.
- `Create.LayerThicknessCalculationData` now calls `Query.AdjustableLayerIndex`.
- `ThermalTransmittanceCalculator.Calculate(LayerThicknessCalculationData, ...)`: for `LayerIndex == -1` uses
  the same rule on the TCD materials; if nothing qualifies returns the existing controlled result
  (`LayerIndex -1`, NaN thickness, NaN calculated U) instead of indexing `materials[-1]`. An explicit,
  user-chosen `LayerIndex` is untouched.
- `Tas.Modify.Run` is **not** changed (no SAM core change either).

## Decisions

- Tests live in `SAM.Analytical.Tas.TM59.Tests` (it already references the built `SAM.Analytical.Tas.dll` and
  the Interop DLLs without COM build problems), so nothing moves to SAM_UI's WPF test project.
- Material type for the SAM side comes from `Core.Query.MaterialType(IMaterial)`; an unknown material is
  never adjustable but keeps the index aligned with the construction layers.
- Behaviour change worth knowing: the fallback now has the 10 mm filter, which it previously lacked.

## Files changed

- `SAM.Analytical.Tas/Query/AdjustableLayerIndex.cs` (new)
- `SAM.Analytical.Tas/Create/LayerThicknessCalculationData.cs`
- `SAM.Analytical.Tas/Classes/ThermalTransmittanceCalculator.cs`
- `SAM.Analytical.Tas.TM59.Tests/AdjustableLayerPickerTests.cs` (new, 15 tests)
- `Documentation/evidence/UVALUE-LAYER-PICKER-PR1A.md` (this record)

## Validation

- Build: SAM_Tas.sln Release with VS MSBuild, SAM at its `sow/2026-Q3` head - succeeded.
- `AdjustableLayerPickerTests`: 15 passed. Cases: the exact user wall with the model's conductivities chooses
  mineral wool (index 2) and the calculation data carries it with a defined heat-flow direction; gas and
  transparent layers with the lowest conductivity are never chosen; only-gas/glass and only-thin constructions
  give -1; layers under 10 mm skipped, exactly 10 mm (and 0.01f as stored by TCD) adjustable; NaN / <= 0
  conductivity guard; unknown material keeps alignment; lowest conductivity wins, ties keep the first; the
  TCD-side layer-list rule picks mineral wool and gives -1 for gas/glass only.
- Full `SAM.Analytical.Tas.TM59.Tests` suite (Release): **1038 passed, 0 failed**.

## Risks / open

- The TCD-side fallback runs against real COM only in the PR1b real-app evidence (installed build needs this DLL).
- Failure classification in the UI (PR1b) relies on `LayerIndex == -1` from the picker.

## Next step

CI green, then explicit approval to merge. After merge: closeout entry in `PROJECT_PROGRESS.md` on
`sow/2026-Q3` (docs-only commit with the merge SHA), then start SAM_UI PR1b after rebuilding SAM_Tas.
