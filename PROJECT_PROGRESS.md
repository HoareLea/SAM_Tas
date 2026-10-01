# Project Progress

## Current: U-value workflow PR1a - default layer picker never chooses gas or glass (1 Oct 2026) - MERGED as SAM_Tas#78 (`4b5e1c18`)

**Status.** Merged into `sow/2026-Q3` (merge `4b5e1c18`, PR head `0a2c13d6`). CI green (build, SPDX), mergeable, no reviews or
comments. First of two PRs for `Tools > U Value Calculator` working first time; companion SAM_UI PR1b (classified messages +
real-app evidence) follows. Plan: SAM_UI `documentation/plans/UValue-Workflow-PLAN.md`. PR record:
`Documentation/evidence/UVALUE-LAYER-PICKER-PR1A.md`.

- **Root cause.** `Create.LayerThicknessCalculationData` picked the lowest-conductivity layer of >= 10 mm. The model's gas materials
  have real positive conductivities (0.024, 0.01622 W/mK), below mineral wool's 0.025, so the air cavity won and the target U was
  unreachable ("Could not calculate construction for given criteria."). NaN conductivity can never win (`NaN < min` is false). The
  `layerIndex == -1` fallback in `ThermalTransmittanceCalculator` skipped only conductivity <= 0, had no 10 mm filter and would also pick gas.
- **Change.** One rule by material TYPE: `Query.IsAdjustableLayer` / `Query.AdjustableLayerIndex` (new `Query/AdjustableLayerIndex.cs`; overloads for
  `Construction` + `MaterialLibrary`, a (type, conductivity, thickness) list, and `TCD.material`): gas and transparent never qualify; thickness
  < 10 mm (1e-6 m tolerance for TCD's float widths) and NaN / <= 0 conductivity do not; -1 when nothing qualifies. Used by the picker and by the
  calculator fallback, which now returns the controlled `LayerIndex -1` / NaN result instead of indexing `materials[-1]`. An explicit
  user-chosen `LayerIndex` is unchanged. `Tas.Modify.Run` and SAM core untouched.
- **Tests.** `AdjustableLayerPickerTests` (15, COM-free, in `SAM.Analytical.Tas.TM59.Tests`); full suite 1038/1038. Real TCD probe
  (SAM_UI evidence `probe/`): new picker chooses mineral wool (U 0.5 -> 33.8 mm, 0.25 s); forced `-1` fallback picks the same; old picker NaN after 1.5 s.
- **Risks.** Installed-app acceptance needs an installer containing this DLL.
- **Next step.** SAM_UI PR1b (`fix/uvalue-calculator-messages-2026-10-01`), then PR2 (new U-value window) per the plan.

## Current: Part O progress UI - coarse route progress callback (29 Sep 2026) - MERGED as SAM_Tas#77 (`1fc97570`)

**Status.** Merged into `sow/2026-Q3` (merge `1fc97570`, PR head `c9e8174d`). CI green (build, SPDX), mergeable, no reviews or
comments. Companion of SAM_UI#143 (merge SAM_Tas first; done). Full record: the SAM_UI closeout of the same date.

- **Change.** Optional, backwards-compatible, UI-independent progress callback: `SystemVentilationRouteStage` +
  `SystemVentilationRouteProgress` (Stage, Current, Total, `HasCount`); new overloads `Create.SystemVentilationRoute(..., fanHeatGainPolicy,
  Action<SystemVentilationRouteProgress>)` and `Modify.SimulateSystems(..., progress)`; old signatures delegate with null. Events are per
  air system while converting and while running `ISystem.Simulate` (real n of m, total read before the first call), plus reconcile,
  recirculation-cooling read-back and manufacturer-guidance (`SimulateEx`, one opaque call, no count). Subscriber exceptions are swallowed.
  Files: `Enums/SystemVentilationRouteStage.cs`, `Classes/SystemVentilationRouteProgress.cs`, `Classes/SystemVentilationConversionContext.cs`,
  `Convert/ToTPD/TPD.cs`, `Create/SystemVentilationRoute.cs`, `Modify/Simulate.cs`, `SystemVentilationRouteProgressTests.cs`.
- **Tests.** SAM_Tas full suite 1023/1023 (9 new, COM-free).
- **Next step.** None for this entry.

## Current: Part O Iteration 3 exchanger representation (29 Sep 2026) - MERGED as SAM_Tas#76 (`3eee7a4a`)

**Status.** Merged into `sow/2026-Q3` (merge `3eee7a4a`, PR head `8fe4b790`). Final CI green (build, SPDX), mergeable,
no reviews or comments. Follows #74/#75 in the Iteration 3 stream. PR record:
`Documentation/evidence/PARTO-IT3-EXCHANGER-REPRESENTATION.md` (with licensed probe/replay logs alongside).

- **Change.** Guidance exchanger `SensibleEfficiency` was one 335x281x2 (intake, extract, airflow) Equal table per unit
  (188,270 cells, each an out-of-process `TPD.exe` COM call). Now a pruned 2D (ODB, EDB2) state Equal table (265x266 =
  70,490 cells, exact under per-axis linear interpolation) times a 1D EFlow Multiply airflow table (2 points). Read-back
  verifies every cell, axes, variables, multiplier, extrapolation and both fractions (stricter than before).
  Files: `SAM.Analytical.Tas.TPD/Modify/GroundGuidanceCooling.cs`, `SAM.Analytical.Tas.TM59.Tests/GuidanceExchangerTableCallsTests.cs`.
- **Evidence (licensed).** Cell + axis COM calls ~809,600 -> ~321,100 (-60%). Iteration 3 total 296.6 s -> 198.9 s
  (noisy, VM varies ~1.5x; the call count is the reliable claim). Generated TPDs re-simulated: 104/104 duct series x 8760 h
  bit-identical; OperatingAirFlow.csv byte-identical; TM59 Pass/Pass; bridge profiles 32/32 and TSD temperatures 9/9 identical.
- **Rejected alternatives.** No bulk table API, no in-process COM, no copy/share across units; Lua/Curve modifiers not
  exact; a DeltaT axis on the exchanger makes `SimulateEx` fail ("Main PlantRoom Has Errors").
- **Tests.** TM59 Release **1014/1014** (14 exchanger-table tests; equivalence over 15-17M points, worst 2.2e-16 only in
  switching cells); Benchmark 16/16.
- **Deferred.** Part O progress UI, run history/resume, Design Condition cleanup.
- **Next step.** None for this entry.

## Current: Part O Iteration 3 TPD performance + guidance controller layout (29 Sep 2026) - MERGED as SAM_Tas#74 (`e706a13d`) and #75 (`a312426b`)

**Status.** Both merged into `sow/2026-Q3`, final CI green, no unresolved review comments; #75 was refreshed onto the
merged #74 and re-run before merging. Part of the Iteration 3 validation stream (SAM_Systems#33, SAM#167,
SAM_UI#140-#142 - see their closeouts).

- **#74 `perf(tpd)`** (head `5ca42009`, merge `e706a13d`). (1) Thermostat bridge: per-slot COM calls replaced by bulk
  yearly-profile read/write. Licensed: bridge ~183.8 s -> ~4.3 s, write/read ~158.3 s -> ~0.14 s; all 32 profiles x
  8760 slots identical, 9 resultant-temperature zone series identical (max diff 0); tests reject 0-based and shifted
  bulk reads. (2) Exchanger modifier tables: zero-skip on fresh tables. A fresh table answers 2x0x0 and after `SetSize`
  keeps two non-zero cells, so the `(1,1,1) == 0` probe had disabled the skip (564,810 of 564,810 cells written);
  fixed - production-size writes 564,810 -> 241,074 (~57% fewer), full read-back verification retained. Files:
  `SAM.Analytical.Tas.TPD` (`TPDProfiler`, `Convert/ToTPD/TPD.cs`, `Create/ThermostatBridge`, `Create/SystemVentilationRoute`,
  `Modify/GroundGuidanceCooling`, `Modify/GuidanceCoolingResults`) and TM59 tests. Tests **998/998**.
- **#75 `fix(tpd)`** (head `5130e9b8`, merge `a312426b`). The three manufacturer-guidance controllers, all at (0,0),
  are placed deterministically below the AHU trunk (~(270,320), (400,320), (610,320)); layout only, no topology
  change. Licensed TPD inspection confirmed all three air systems. Files: `Modify/PlaceGuidanceControllers.cs`,
  `Query/GuidanceControllerLayout.cs`, `GroundGuidanceCooling.cs`, `GuidanceControllerLayoutTests.cs`. Tests **997/997**.
- **Caveat.** Wall-clock on the licensed VM varies ~1.5x run to run; the reliable claims are call counts and exact
  equality, not cross-run timings.
- **Deferred (separate future tasks).** Larger exchanger representation optimisation, Part O progress UI, run
  history/resume, Design Condition cleanup.
- **Next step.** None for this entry.

## Current: Bridge / TM59 / weather day-major TSD reads + full-year guard (29 Sep 2026) - MERGED as SAM_Tas#73 (`7b84dd92`)

**Status.** Merged into `sow/2026-Q3` (merge `7b84dd92`, PR head `436a617c`). Final PR CI green (build, SPDX), mergeable,
no review comments. Full record: `Documentation/evidence/TSD-BRIDGE-TM59-DAYMAJOR.md` (probe + logs in
`Documentation/evidence/tsd-bridge-tm59-daymajor/`). No TAS simulation was run.

- **Bridge** (`TPD.Query.ReadThermostatBridge`): one day-major pass over days 1..365 (private `BridgeSeries`, same
  `AnnualSeries` conversion) replaces 2 x `GetAnnualZoneResult` per room. New `ReadThermostatBridge(SimulationData, ...)`
  refuses unless `firstDay == 1 && lastDay == 365`; `Create.ThermostatBridge` calls it. The bridge stays all-or-nothing:
  any refused room or read failure refuses the whole `ResultantTemperatureResults`.
- **TM59** (`Convert.ToSAM_AdjacencyCluster`): one `Query.ZoneResultSeries(zones, 1, 365, arrays)` pass. **Weather**
  (`Weather.Tas.Query.WeatherYear`): 7 arrays via daily reads.
- **Full-year guard** at the boundaries, not in the readers: `Query.FullYearRefusal`; opt-in
  `TSDConversionSettings.RequireFullYear` (default false) + `Convert.ToSAM(..., out refusal)`. The old 8760-length
  checks never detected a part year: TSD pads the unsimulated hours with -1.
- Read-only TSD opens for `Convert.ToSAM(path_TSD, settings)`, `ToSAM_AdjacencyCluster(path_TSD)`, `DesignDayNames`
  and `ToSAM_WeatherDatas(*.tsd)`.
- **EDSL (Duncan) confirmed** that day-major `GetDailyZoneResult` across all zones is the recommended access pattern,
  because TSD stores results per day and `GetAnnualZoneResult` re-reads all 365 daily records per zone. For that reason
  the adaptive annual/day-major reader is not planned.
- **Validated:** TM59 tests **990/990** (12 new), benchmark tests **16/16**. Read-only identity against the annual
  getters, IEEE bits: pr4h bridge TSD (9 zones) 0 of 157,680 bridge / 0 of 157,680 TM59 values differ; PR4 x10
  (90 zones) 0 of 1,576,800 each; PR4 x30 (270 zones, old path sampled on 4 zones) 0 of 70,080 each; weather identical
  on all three. On this laptop: x30 bridge read ~22 min (extrapolated) -> 48 s, weather 17.7 s -> 2.6 s. Cache-resident
  files are slower (x10 bridge 2.7 s -> 15.9 s), which is accepted.

**Decisions / scope.** The bridge has its own loop because TPD must not reference SAM.Analytical.Tas (namespace
shadowing). `UnmetHours`, recirculation cooling, peak APIs and SAM_UI were not changed.

**Open (separate items).** (1) SAM_UI `PartOTM59Assessment` must set `RequireFullYear = true` before the TM59 guard
takes effect on Part O (small SAM_UI PR). (2) Damaged / incomplete-TSD robustness: a file that states 1..365 but is
damaged or stopped, and TSD.exe hangs. (3) A surface-result day-major investigation (`Panel.ToSAM` with panel types,
`SetBlinds`), which needs a licensed `GetDailySurfaceResult` probe. (4) Unanswered EDSL questions: a multi-zone
getter, the day-cache cap, per-day surface storage and a completion flag.

**Next step:** the SAM_UI `RequireFullYear` adoption PR.

## Previous: TSD result-read performance - day-major `AddResults` (29 Sep 2026) - MERGED as SAM_Tas#72 (`0f4eeadb`)

**Status.** Merged into `sow/2026-Q3` (merge `0f4eeadb`, PR head `06423652`). Final PR CI green (build, SPDX), mergeable,
no review comments (only a Codex quota notice). Full record: `Documentation/evidence/TSD-RESULT-READ-PERFORMANCE.md`.

- `Modify.AddResults` / `Convert.ToSAM.Results` read the TSD series day-major (new `Query.ZoneResultSeries`) instead of
  zone by zone, and take the SAM zones' cooling peaks from those series (new `Query.PeakZoneGroupGain`, the rule TSD's own
  `GetPeakZoneGroupGains` was measured to follow) for a full-year simulation; part-year still asks TSD as before.
  `Query.Overheating` gained a series overload.
- **Validated (licensed A/B):** x30 `AddResults` **2,992 s -> 85.4 s (~35x)**; results identical apart from creation
  timestamps. TM59 tests **978/978**, benchmark tests **16/16**.

**Decisions / scope.** Access-pattern fix only; the remaining ceiling is TSD.exe's day cache. The bridge / TM59
day-major optimisation was deliberately NOT included.

**Follow-up.** Bridge / TM59 day-major done in SAM_Tas#73 (above).

## Previous: Mixed Part O PR3B-3 - mixed-scenario diagnostics + mixed TPD cooling regression (28 Sep 2026)

**Status.** Branch `fix/parto-mixed-cooling-pr3b3-2026-09-27` from `sow/2026-Q3` `fedf34cd`. Third of three PR3B domain
PRs (authoritative record: SAM `documentation/PartO-MixedDwellingStrategies-PR3B.md`). Depends on SAM#161 (merged
`85a13ec3`) and SAM_Systems#31 (PR3B-2, `MechanicalVentilationSettings.GuidanceTemplate`): CI is green only once both are
on their integration tips. **No production thermal/TPD change.**

- `SAM.Analytical.Tas.TM59/Classes/PartODiagnosticLog.cs`: the run record's `partOIteration` took `scenarios[0]` - wrong
  for a mixed building (and for a single-iteration run whose corridor scenario was listed first). New public
  `RunPartOIteration(scenarios)`: the distinct iteration of the dwelling scenarios (a common space's
  `DwellingIndependent` only when there is no dwelling), `Mixed` (`MixedPartOIteration`) when they differ, null for none.
  New run field `partOIterations` (every distinct iteration, ordinal-sorted). Space rows unchanged (already per scenario).
- `MixedGuidanceCoolingTests` (new, COM-free): the real SAM_Systems mixed materialisation (MV + MVRE for the one unit with
  guidance, SAM's airflow rule 25 l/s design -> 80 l/s) through the production `SystemVentilationConversionContext` and
  `Modify.GroundGuidanceCooling` (by reflection - its native parameters are embedded interop types): exactly one
  `GuidanceCooling`, on the cooled unit's air system; the uncooled air system has none, no exchanger/coil, and the
  grounding leaves it untouched; ventilation intent identical to the uncooled document.
- Test csproj links SAM_Systems' shipped `MVRE.json` beside `MV.json`.

**Red first** (`Documentation/evidence/parto-mixed-pr3b/pr3b3-red-first.txt`): diagnostic tests 2 fail on the
`scenarios[0]` line ("BaseNaturalVentilation" / "DwellingIndependent" instead of "Mixed" / "BasePassive"). The TPD tests
pass on unchanged production (PR3A's conclusion holds); a building-wide mutation of `GuidanceCooling(guid)` fails 2 of 4.

**Validation.** `SAM_Tas.sln` Release (VS 18 MSBuild, `-m:1` - a parallel build raced and compiled the tests against a
stale `build\SAM.Analytical.Systems.dll`): 0 errors. TM59 **970/970** (963 + 7), Benchmark 16/16. Built against SAM
`2be58f1e` (= merged #161) and SAM_Systems #31 head `56fcb8a6`.

**Follow-up.** SAM_Tas_Grasshopper `TasLogPartODiagnostics.cs:236` names the log file from `overheatingScenarios[0]` -
switch to `PartODiagnosticLog.RunPartOIteration(...) ?? "Undefined"` after this merges (separate repo, small PR).

**Next step.** Merge after SAM_Systems#31; then the licensed PR3B gate (SAM PR3B doc §4).

## Previous: PR2A closeout - Phase 2 result authority COMPLETE (27 Sep 2026)

```text
Phase 2 result authority: COMPLETE
PR2A: COMPLETE
Ready for PR2B: YES
```

- SAM#153 (PR2A-1, `SpaceLoadPeak`) merged into SAM `sow/2026-Q3` as `8a22c82b`.
- SAM_Tas#69 (PR2A-2, this repo) merged into `sow/2026-Q3` as `46dacf22` (2026-09-27, by the user).
- Reconciled: both local `sow/2026-Q3` fast-forwarded to those tips; both clean; no other merges landed after them.
- Final validation: SAM Release build 0 errors; SAM.Tests 2544/2544; SAM_Tas build 0 errors; TM59 963/963;
  Benchmark 16/16; SAM#153 CI green; SAM_Tas#69 CI green.
- Follow-up filed, not fixed: [SAM#154](https://github.com/SAM-BIM/SAM/issues/154) - in `Convert.ToSAM_Results` a
  cooling result without `LoadIndex` `continue`s past the heating **surface** results of the same space.
- **Next step.** PR2B (typed Space Design Load reporting data + collector) is SAM-only, from SAM `sow/2026-Q3`. It
  reads only `DesignDayPeak`/`AnnualPeak` and must not reference SAM_Tas. Nothing to do in this repo for PR2B.

## Previous: PR2A-2 - Tas result authority: design-day and annual peaks kept separately (27 Sep 2026) - MERGED as SAM_Tas#69 (`46dacf22`)

**Status.** Branch `fix/pr2a2-tas-peak-authority-2026-09-27` from `sow/2026-Q3` `aa00ff91`. It **depends on SAM PR2A-1**
(`feature/pr2a1-space-load-peak-2026-09-27`: `SpaceLoadPeak`, `SpaceSimulationResultParameter.DesignDayPeak` /
`AnnualPeak`). This repo builds against `..\SAM\build`, so merge and build SAM first. The full record, contract and
evidence are in SAM `documentation/Reporting-Phase2-ResultAuthority.md` §3.2 and
`documentation/evidence/reporting-phase2-gate/pr2a/`.

**Fixed in `Convert.ToSAM_Results(SimulationData)` (audit B1–B5).**
- B1: an annual heating peak larger than the HDD peak was written into the cooling variables, leaving the heating
  result "Simulation" with the HDD load/state. It now governs the heating result with the annual zone data and index. A
  load type with no design day now gets its annual result (before: none).
- B2/B3: both peaks are persisted as typed `SpaceLoadPeak`s:
  - never merged;
  - a zero peak is `Load 0` with no time, state or components;
  - no −1 sentinel reaches them.
- B4: the 1-based TSD hour is converted once, in `Query.ZeroBasedHourOfYear/OfDay` (new `Query/ZeroBasedHour.cs`). A
  design-day peak gets no hour of the year.
- B5: `Create.SpaceLoadPeak` (new) reads the full set at the peak:
  - the room state: DB, resultant, RH, humidity ratio;
  - all sensible and latent gain channels, AHUGain included, heating included;
  - the outdoor DB/RH from the TSD building results, for annual peaks only (design-day data has no weather results).
  - Aperture flows and IZAM channels are excluded: Tas returns −1 for them at valid hours.
- The loop now runs over every zone of the building data, not only the zones with design data. It is unchanged for
  normal runs.

**Compatibility.**
- The legacy `Load`/`LoadIndex`/`SizingMethod`/gains/`DesignDayTemperature` still project the governing peak:
  - DD unless annual is strictly larger;
  - raw 1-based `LoadIndex`;
  - the unchanged −1 values and `LoadIndex 0` for a zero peak.
- The only change is the B1 case (and a load type with no design day).
- The compatibility tests pass on both the old and the new code.

**Validation.**
- New `TsdResultFakes.cs`: managed TSD fakes. `ZoneData`, `BuildingData`, `Heating/CoolingDesignData` and
  `SimulationData` are plain interfaces, so the real conversion runs in `dotnet test`.
- New `TsdPeakAuthorityTests.cs`, 16 tests.
  - **Red → green:** against the old `Results.cs`, 9 failed. B1 read 50 W instead of 104.01 W. The 7
    compatibility/helper tests passed.
  - The B6 test uses Bedroom 2_3's real cooling values.
- `SAM_Tas.sln` Release (Framework MSBuild): exit 0.
- TM59 **963/963** (947 + 16). Benchmark **16/16**. SAM.Tests 2494/2494 on the SAM branch.
- Real fixtures (production `AddResults` → new `.sam` copy → reopen; the sources' SHA-256 were unchanged):
  - Bathroom_2 (`final1b\open.tsd`): heating DD 1139.796 W at design-day hour 23, no date. Annual 104.010 W at hour
    8553 = 23 Dec 09:00, outdoor −2.3 °C / 100 %. Both close to 0.0002 W.
  - B6: the TSD scan found real cooling in the TPD-bridge TSDs. `pr3\final\bridge.tsd` Bedroom 2_3 is CDD 2070.833 W
    and annual 1369.404 W (2 Aug 04:00). Studio 1_0 is CDD 1973.468 W and annual 1972.137 W. Cooling `Load = +Σ
    sensible terms` within 0.006 W; latent is outside the load.
- No licensed simulation was run: only read-only TSD opens of copies.

**Open / risks.**
- AHUGain was 0 everywhere, so its closure role when non-zero is unverified.
- Latent removal load is not persisted.
- The bridge fixture's set points are imposed (a thermostat bridge), not a designer's cooled model.
- Result freshness is unchanged (non-Part-O = Unknown).
- The Tas COM trap: open a TSD by **absolute** path. A relative path hangs the COM server with a hidden dialog, and the
  stuck server then makes later opens time out. Use a fresh copy per open.

**Next step.** Superseded by the closeout above (both PRs merged).

## Previous: replace per-run records instead of appending (26 Sep 2026) - MERGED as SAM_Tas#67 (`b32c0808`)

**Status.** Branch `fix/parto-replace-run-records-2026-09-26` from `sow/2026-Q3` `39828c6`; commits `bf0daba`, `63df74d`.
Second of three coordinated PRs: merge SAM `fix/deepclone-guidless-objects-2026-09-26` first (this repo builds against
`..\SAM\build`), then this, then SAM_UI `feature/parto-2b-sam-growth-2026-09-26`. Full record: SAM_UI
`documentation/evidence/parto-2b-sam-growth/GROWTH.md`.

**Defects (found investigating the Part O 2B per-round `.sam` growth).**
- `WorkflowCalculator` APPENDED each run's design days to the cluster, although `Modify.AddDesignDays` clears the TBD's
  and writes only the run's: +2 per run, and stale design days of an earlier weather (London beside CIBSE Z1) kept in
  the model. Now `Modify.ReplaceDesignDays` (new, `Modify/ReplaceDesignDays.cs`) - the cluster records exactly the TBD's.
- `Modify.AddResults` replaced space/surface results but appended a `ZoneSimulationResult` per zone per run. Now the
  earlier cooling result of the same source is removed first, as for spaces.
- Neither record feeds sizing, simulation, TM59 or the optimiser: no input changes (live: TM59 reports identical).

**Validation.** SAM.Analytical.Tas.TM59.Tests 947/947 (+3 `DesignDayRecordReplacementTests`). `SAM_Tas.sln` Release
exit 0. Live 2B re-run (real TAS): every round 2 design days (Z1 only) and 4 zone results; `.sam` flat at 166 KB.

**Next step.** Done. Merged in order SAM#142 (`78a57466`) -> SAM_Tas#67 (`b32c0808`) -> SAM_UI#119 (`5a0b9bf6`);
re-validated against the merged SAM build (TM59 947/947). Deployed by SAM_Deploy#50 (`7fcd79a7`).
No further SAM_Tas work is planned for this fix. The next step is Part O UX Pass 6 (final Part O consistency and
end-to-end acceptance), started in a fresh session from SAM_UI `sow/2026-Q3`; it changes SAM_Tas only if that pass
finds a defect here.

## Previous: Nuaire reply (24 Sep 2026) - exchanger then DX drop, 13 C floor

**Status.** MERGED into `sow/2026-Q3` on 2026-09-24, in order: SAM-BIM/SAM#133 (`54c43438`) -> SAM-BIM/SAM_Systems#29 (`c88c9b37`) -> SAM-BIM/SAM_Tas#65 (`1b659756`) -> SAM-BIM/SAM_UI#107 (`8f58144c`). CI green and Codex review clean (all findings fixed and answered) on every PR. The `feature/parto-nuaire-reply-2026-09-24` branches are deleted.
The full cross-repo record (evidence, decisions, TAS probes, MG
acceptance, residual uncertainties) is in SAM's `PROJECT_PROGRESS.md`, *Current* entry.

**What Nuaire's reply (A. Nash, 24 Sep 2026) changed.**
- The cooling supply is no longer `to - X`, which Nuaire called a simplified IES work-around valid at 32 C only.
  It is now the exchanger (bypass, or recovery at eta(Q)), then the DX drop less the supply-motor heat, never
  below 13 C. A coil does not heat.
- The bypass is to >= 12, to < extract, extract >= 19 C. The MVHR decides it independently of the cooling-stat.
- The cooling airflow is 60-120 l/s, with an 80 l/s default. A dwelling's own figure overrides it.
- The brochure's 2.2 kW is a combined coolth-recovery + sensible figure, not a DX duty, and is no longer used.

**This repo.**
- `Modify/GroundGuidanceCooling.cs`:
  - DX `MinimumOffcoil` = a table over the coil's OWN entering dry bulb (`EDB`), `max(13, entering - 8.245)` at
    80 l/s;
  - the exchanger table has one inclusive bypass rule at both airflows, eta 0.8 at design and 0.8576 at
    80 l/s, a 0.1 K grid to 45 C, and the extract axis continuing to 100 C;
  - DX duty = numerical `GuidanceCoolingDuty_W` = 100 kW. A 2.2 kW duty acted as a controller gain: the coil
    fell short of the law in part-signal hours;
  - the superseded intake-offset rule is refused.
- `GuidanceCoolingResults`: law, exchanger-state, floor and part-flow counts, and a floorless-rule summary. Also
  fixes a mis-attached modulating count.
- `GuidanceCoolingRecipeTests`.

**Validation.** Tests: SAM 2252, SAM_Systems 256, TM59 944, WPF 1031, all passing. Framework MSBuild: 0 errors
in all four repos.
- Representative MG annual run (closeout): COMPLETE, bias 0.53 K, RMSE 1.413 K.
  - The law and the exchanger state are exact in every full-flow hour.
  - 0 hours cooled below 13 C by the coil.
- The review fixes after that run (inclusive bypass minimums, extract axis beyond 45 C, floorless summary
  text) were not rerun annually, by owner decision: they affect only exact-threshold hours (intake exactly
  12.0 C, background hours) and extracts above 45 C (none in the MG).
- This is manufacturer modelling guidance; Nuaire has not called it certified or approved.

**Next step.** Bump the SAM_Deploy pointers to the merge commits above. Remaining manufacturer questions
are listed in SAM's `PROJECT_PROGRESS.md`.


## Previous: SAM#123 manufacturer guidance - Iteration 3 mode "Selected product - manufacturer guidance" (2026-09-24)

**Final integration review (2026-09-24, before merge; the merges followed).** One consolidated review of all four branches
against `sow/2026-Q3`; no blockers, no code changed at review.
- Each branch merges into current `sow/2026-Q3` without conflicts. The diffs are limited to the #123 guidance
  scope. The new public surface is additive, and a template, settings or route without guidance serialises
  and materialises as before.
- Local tests on the branch heads: SAM.Tests 2218/2218, SAM.Analytical.Systems.Tests 251/251,
  SAM.Analytical.Tas.TM59.Tests 938/938 (7 guidance-cooling), SAM.Analytical.UI.WPF.Tests 1031/1031.
  SAM#125 and SAM_Systems#25 CI green.
- Evidence re-checked on disk:
  - B0 TM59 reports equal the 2026-09-23 ones except the `Source:` path line.
  - The Resume MG TM59 report equals the in-session MG one except the path line. The hourly comparison agrees
    to 3 d.p. (bias 0.5672 vs 0.5674 K). It is not bit-identical, because Resume ran a fresh 1a.
- Non-blocking findings (JSON edge cases, read-back strictness, stale comments, test gaps) are in
  [SAM#130](https://github.com/SAM-BIM/SAM/issues/130). They were deliberately not fixed, so that the merged code is
  the accepted code.
- The DisplacementVent wet-room issue is [SAM#129](https://github.com/SAM-BIM/SAM/issues/129). It is not changed here.
- Wording: "certified" appears only in negations; values stay PROVISIONAL pending Nuaire.

**Status.** MERGED into `sow/2026-Q3` on 2026-09-24, in dependency order:
- [SAM#125](https://github.com/SAM-BIM/SAM/pull/125) -> `1f9a5b95`
- [SAM_Systems#25](https://github.com/SAM-BIM/SAM_Systems/pull/25) -> `6c042609`
- [SAM_Tas#63](https://github.com/SAM-BIM/SAM_Tas/pull/63) -> `83653aaa`
- [SAM_UI#105](https://github.com/SAM-BIM/SAM_UI/pull/105) -> `620aa701`

All four PRs had green CI. The `feature/parto-nuaire-manufacturer-guidance` branches are deleted. All product
values are still PROVISIONAL Nuaire guidance: Nuaire was emailed and has not yet confirmed.
[SAM#123](https://github.com/SAM-BIM/SAM/issues/123) stays open.

**Where it came from.**
- The Stage 11 TAS prototype passed. Its evidence is under
  `C:\TasOut\nuaire-guidance\REVIEW2\resume-2026-09-23\evidence\stage11\`.
- Stage 11b, the production correction, was found on the real model:
  - Controlled dampers do not converge when a supplied room's only outlet is a transfer damper
    (MVHR-02/03: more than 20 min for one day, against 3 s with the controllers pinned).
  - The elevated airflow is now carried on the fans, with the dampers uncontrolled at their elevated share.
    Room-stat controllers drive the supply and extract fans with Min = (design/elevated)^2, because a
    controlled fan's airflow goes as the square root of its signal.

**Per repo.**
- **SAM:**
  - `SupplyTemperatureRule.IntakeOffset`: `to - X(Q)`, Refuse outside the stated airflows, with reported
    domain use.
  - `CoolingActivationSignal` (room / extract).
  - A room-stat bypass keeps the rule's extract <= activation condition.
- **SAM_Systems:**
  - The Nuaire entry uses IntakeOffset 70/80/90/100/110 -> 15/14.5/14/13.5/13 K. There is no 16 C floor
    (Nuaire, 13 Aug 2025), activation is on the room stat, and the entry is marked PROVISIONAL.
  - `GuidanceSettings` materialisation: MVRE plus a supply DX coil after the exchanger.
  - `MechanicalVentilationGuidanceCooling` is the record the TAS grounding reads.
  - `Query.MechanicalVentilationGuidanceSettings`: the elevated airflow is the midpoint of the stated range
    (80 l/s), refused above unit capacity.
- **SAM_Tas:**
  - `Modify.GroundGuidanceCooling` writes and reads back:
    - DX room-stat controller: Studio/Bedroom zone, `SensorArc1`, 22.05 / 0.1 K;
    - fan controllers;
    - uncontrolled exchanger with an (ODB, EDB2, EFlow) state table;
    - DX: finite 2200 W (the table's max combined capacity), no gates, `MinimumOffcoil = to - 14.5`.
  - A disagreement refuses the grounding.
  - `Modify.GuidanceCoolingResults` gives the hourly read-back.
- **SAM_UI:**
  - New mode `SelectedProductManufacturerGuidance`, with `-It3BMG` files, a resolution step with no product
    constants, and the operation CSV and summaries persisted in the record.
  - Review-consistency branch for the mode.
  - Separately: Iteration 3 can start from a completed 1a reopened in a later session.
    `<project>.prepared.sam` and `<project>.partorun.json` are bound to the TSD, so there is no need to
    re-run Prepare & Run.

**Acceptance (2026-09-24, real `SAM Analytical.exe`, `SAM_zoningAM-CIBSEfutureZ1.sam` a7e09a25, DSY1 2050s).**
Evidence is in `C:\TasOut\parto-guidance-2026-09-24\`, outside git.
- **B0 on the new binaries:** COMPLETE. Both TM59 reports are byte-identical to 2026-09-23; bias 0.05 K,
  RMSE 0.736 K. So B0 is unchanged.
- **MG:** COMPLETE in 11.7 min, A Fail / B Fail. TM59 >26 C hours against A:
  - Studio -68 and bedrooms -101 / -96, where B0 is -6 / +8 / +12;
  - kitchens -38;
  - wet rooms -284 to -471 (Bathroom_2 becomes a Pass).
  - Annual mean bias +0.57 K, RMSE 1.48 K, from heat recovery outside cooling. The DisplacementVent-inflated
    extract (3.5-6 kh extract > 22 C while room <= 22 C) suppresses bypass. It is a model-hygiene decision
    and not changed here.
- **MG TAS read-back, per unit (design -> elevated):**
  - MVHR-01: 30 -> 80 l/s, supply = extract (< 0.02 l/s). `to - 14.5` is exact in 849/849 full-flow cooling
    hours that are not capacity-limited. 122 h are at the 2.2 kW total-duty bound.
  - MVHR-02/03: 63 -> 80 l/s. The law holds in 699/700 and 692/693 such hours; 85 h are capacity-limited.
  - All units: minimum supply 7.3 C; the stat room peaks at 36.9-39.3 C in a 40 C intake heatwave, with the
    supply at exactly `to - 14.5`.
- **Reopen:** a new session opened the saved 1a run and reviewed the MG pairing in 10.8 s with no simulation.
- **Installed state changed on this machine:**
  - `Documents\SAM\resources\...\VentilationUnitCatalogue.JSON` is now the v3 feature catalogue. The v1
    backup is at `C:\TasOut\parto-guidance-2026-09-24\catalogue-backup\documents-SAM-before.json`. Restore it
    before running `sow` binaries.
  - `%APPDATA%\SAM\SAM.Analytical.dll`/`SAM.Core.dll` and `SAM.ghlink` were overwritten by a `SAM.sln`
    build on 2026-09-24 09:44. Redeploy from `sow` to restore Grasshopper.

**Exact next step.**
1. On a clean checkout of merged `sow/2026-Q3` (all four repos), run the minimal merged-state acceptance.
   Reuse the saved runs; no new long simulation is needed.
   - Build SAM, SAM_Systems, SAM_Tas (Framework MSBuild) and SAM_UI from `sow/2026-Q3`, and run the four test
     projects.
   - Deploy those binaries. The v3 `VentilationUnitCatalogue.JSON` must be in `Documents\SAM\resources`.
   - In `SAM Analytical.exe`, reopen `C:\TasOut\parto-guidance-2026-09-24\03-Resume\` (1a with sidecar) and
     `02-Iteration3-MG`. Confirm review-only reopen of the MG pairing (A Fail / B Fail, bias 0.567 K) and that
     the reopened 1a reports that Iteration 3 can start.
   - Optionally re-run only B0 from the reopened 1a. Its TM59 reports must equal
     `01-Iteration3-B0` except the `Source:` line.
2. Bump the SAM_Deploy submodule pointers to the four merge commits.
3. Hold any "certified" wording until Nuaire replies (stat location, X vs airflow, low-ambient behaviour,
   30 l/s).
4. Decide the DisplacementVent wet-room issue ([SAM#129](https://github.com/SAM-BIM/SAM/issues/129)) and the
   non-blocking hardening ([SAM#130](https://github.com/SAM-BIM/SAM/issues/130)) separately.

## Previous: diagnostic-log Criterion 1 aligned to the same summer basis as SAM#120 (2026-09-16)

**Status.** Branch `fix/tm59-diagnostic-log-criterion1-summer-basis` off `sow/2026-Q3` (post SAM_Tas#61,
`ff862201`), PR TBD. Diagnostic-log-only follow-up to [SAM#120](https://github.com/SAM-BIM/SAM/pull/120)
(merged into SAM `sow/2026-Q3` as `e2c0e2c0`), which fixed the same annual-vs-summer basis defect in the
production TM59 Criterion 1 compliance decision. This entry does not touch, and is not gated by, PR5B's
recirculation clamp or the 3-B4 rerun below - it is a separate, narrower diagnostic-evidence fix.

**The defect, confirmed by direct source inspection.**
`SAM.Analytical.Tas.TM59.PartODiagnosticLog.SetCriterionSpecificFields`
(`SAM.Analytical.Tas.TM59/Classes/PartODiagnosticLog.cs:646,655`) wrote the extended natural/bedroom branches'
`hoursExceedingComfortRange` field from `GetOccupiedHoursExceedingComfortRange()` - the full-year basis
SAM#120 moved production TM59 Criterion 1 away from - while the adjacent `summerOccupiedHours` /
`maxExceedableSummerHours` fields on the same record were already summer-scoped. A reader comparing this
diagnostic JSON against TAS's own summer-based TM59 report would see a `hoursExceedingComfortRange` that
does not match the summer basis the other two fields on the same record describe. **This never fed a
Pass/Fail decision** - it is diagnostic evidence only, logged beside (not consumed by) the actual TM59
result's own verdict.

**The fix.** Both call sites now read the new `GetSummerOccupiedHoursExceedingComfortRange()`
(`TM59NaturalVentilationExtendedResult`, added by SAM#120 and inherited by
`TM59NaturalVentilationBedroomExtendedResult`) instead of the annual getter - the same summer-restricted
figure `summerOccupiedHours`/`maxExceedableSummerHours` already use, wrapped in the same `NonNegative()`
sentinel handling already in place. No other branch (`mechanical`, `corridor`) is touched.

**Regression** (`SAM.Analytical.Tas.TM59.Tests/PartODiagnosticLogTests.cs`): the existing
`ExtendedNaturalResult_DerivesSummerOccupiedHoursFromTheHourlySeriesInsteadOfTheAnnualBasis` (bedroom branch)
extended with a comfort-range series carrying one non-summer and one summer exceeding hour (annual count 2,
summer count 1 - the two bases can never coincide), asserting the logged `hoursExceedingComfortRange` is the
summer figure; plus a new sibling test,
`ExtendedNaturalResult_NonBedroom_LogsSummerHoursExceedingComfortRangeNotAnnual`, covering the plain
(non-bedroom) `TM59NaturalVentilationExtendedResult` branch with the same discriminating shape. Both fail
against the pre-fix annual getter and pass against the fix.

**Verified not a compliance change.** `hoursExceedingComfortRange` is a diagnostic-log field only; the TM59
result object's own `Pass`/`Criterion1` (fixed in SAM#120) is unaffected by this change. **No licensed TAS
rerun was performed or required.**

**Test results.** `SAM.Analytical.dll` rebuilt from SAM `sow/2026-Q3` at `e2c0e2c0` (confirmed to expose
`GetSummerOccupiedHoursExceedingComfortRange`), then `SAM.Analytical.Tas.TM59` and
`SAM.Analytical.Tas.TM59.Tests` rebuilt explicitly (VS MSBuild, not `--no-build` against stale binaries):
focused `PartODiagnosticLogTests` **17/17**, full `SAM.Analytical.Tas.TM59.Tests` **931/931** (930 + 1 net
new test).

**Files changed:**
- `SAM_Tas/SAM.Analytical.Tas.TM59/Classes/PartODiagnosticLog.cs` (2 call sites)
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/PartODiagnosticLogTests.cs` (1 test extended, 1 new)
- `PROJECT_PROGRESS.md` (this entry)

**This closes the last known Criterion 1 inconsistency relevant to SAM#111 closeout** - production compliance
(SAM#120) and this repo's diagnostic evidence now share the same May-September authority. No other blocker
introduced by this change.

## Previous: the merged clamp verified on the real project - refusal gone, one unrelated anomaly recorded (2026-09-16)

**Status.** [SAM_Tas#60](https://github.com/SAM-BIM/SAM_Tas/pull/60) (below) is **MERGED** as `96f8ba79` onto
`sow/2026-Q3`. This entry is the follow-up: rebuild at the merged tip and rerun Iteration 3-B4 on the real
project that originally refused, to confirm the clamp actually closes the gap.

**Work completed.** SAM/SAM_Systems/SAM_Tas/SAM_UI rebuilt Release in dependency order at their merged tips
(SAM `192d069a`, SAM_Systems `05ca0c18`, SAM_Tas `96f8ba79`, SAM_UI `9f515c4c`); confirmed
`SAM_UI\build\SAM.Analytical.Tas.TPD.dll` refreshed and containing `RecirculationCoolingClamp`. Iteration
3-B4 rerun on `SAM_zoningAM-CIBSEfutureZ1.sam` (SHA-256 `a7e09a25...`) through the preserved UI-automation
harness: the recirculation-flow refusal named below is **gone**, all 15 ledger stages COMPLETED, and
Candidate B TM59 now exists (**FAIL**, 3 of 8 rooms - `Studio 1_0`, `Kitchen_4`, `Kitchen_7`). Full detail
and deltas in `SAM\documentation\PartO-TAS-VALIDATION.md` § *3-B4 rerun on the merged clamp*.

**Important decisions and assumptions.**
- The FAIL is expected and accepted - 3-B0 already failed 4 of 8 rooms on this weather; the clamp fixes a
  reporting refusal, not the building's overheating. No further SAM_Tas change was made or is intended.
- **A genuine, unrelated observation from the coil replay, recorded but not acted on.** Re-running
  `prod B4 resolve=model stop=gen coolev=1` on the same static no-IZAM fixture: the historically-refusing
  unit (MVHR-02, `dd8a4594-...`) now measures `Count_Clamped = 2`, `MaximumClampedExcursion_Lps = 0.050278`
  - matching the PR below's own `~0.0503` measurement. A *different* unit (MVHR-03, `10fda386-...`) measured
  `Count_Clamped = 6015` at `MaximumClampedExcursion_Lps = 0.000587` - about 170x smaller than the `0.1` l/s
  clamp bound, and consistent with floating-point-scale noise at the `36` l/s floor rather than the
  ramp-overshoot mechanism this clamp targets (thousands of hours legitimately sit at-or-near that floor on
  every unit; this branch's specific arithmetic evidently lands fractionally under `36.0`). Does not change
  any TM59 result. **No production code was changed** - per this session's stop-condition, this is reported
  rather than fixed forward, and is a candidate for a future investigation into why `Count_Clamped` fires at
  this magnitude on this one branch.

**Files changed.** None in `SAM_Tas` production code or tests this session - documentation only
(`PROJECT_PROGRESS.md`, this entry), plus a machine-path repair to the preserved (non-production) evidence
harness under `C:\TasOut\parto-final-real-project\h\` and `tool\` (hardcoded `C:\Users\Virtual Machine\...`
paths from the original acceptance machine, repointed to this one; the harness's prebuilt `p0.exe` was also
stale relative to its own source and was rebuilt).

**Validation.** Unchanged from the PR below at the merged tip: `SAM.Analytical.Tas.TM59.Tests` 930/930,
`SAM.Analytical.UI.WPF.Tests` 1027/1027.

**Unresolved issues, risks, blockers.** The MVHR-03 `Count_Clamped` anomaly above is observed and measured
but not traced to a source line; negligible in magnitude and without result impact, so not blocking, but
worth a focused look before relying on `Count_Clamped` alone as a proxy for genuine near-boundary events.

**A separate, unrelated defect was found in `SAM` (not this repo) during the same session's review** - at the
time this entry was written, it blocked closing SAM #111 as fully verified (not this PR's evidence gate,
which stands on its own). Natural-ventilation Criterion 1 Pass/Fail was computed from the full-year
occupied-hours basis rather than the May-September basis TM59:2017 requires
(`SAM.Analytical\Classes\Result\TM\TMExtendedResult.cs:240-263`, vs. the report's already-correct display at
`TM59AssessmentReport.cs:266-285`). **Update: fixed in [SAM#120](https://github.com/SAM-BIM/SAM/pull/120)
the same day**, and a related diagnostic-only instance in this repo's `PartODiagnosticLog` fixed above - see
`SAM\documentation\PartO-TAS-VALIDATION.md` § *Known open defect* (historical) and § *TM59 Criterion 1 ...
found and fixed* (current) for the full trace. Nothing in SAM_Tas's production TM59 logic was ever
implicated.

**Exact recommended next step (as originally written; superseded - see the entry above).** None required in
this repo for SAM_Tas#60 itself - the fix is merged and now verified end-to-end on the real project that
originally exposed the refusal. If the MVHR-03 anomaly is to be investigated, that is a new, separate task.

## Previous: the recirculation flow is reported at its law's range, not refused for a ramp overshoot (2026-09-16, MERGED as `96f8ba79`)

Branch `fix/parto-pr5b-recirculation-flow-clamp` off `sow/2026-Q3` `ac85b5c3`, commit `602703a1`, PR
[SAM_Tas#60](https://github.com/SAM-BIM/SAM_Tas/pull/60) against `sow/2026-Q3`, **merged as `96f8ba79`**. SAM_Tas only -
no other repo has a production change from it. Evidence and the investigation it closes:
[SAM#118](https://github.com/SAM-BIM/SAM/pull/118), `documentation/PartO-TAS-VALIDATION.md`.

**Why.** The SAM#111 real-project licensed acceptance (2026-09-15) refused Iteration 3-B4 on MVHR-02 hour 4927:
`Q = 120.05027770996094` l/s against a 120 l/s ceiling + 0.05 tolerance - over by **0.00028 l/s**, with zero
heating, zero cooling below the 22 C gate, the coil at the clamped table to 2e-6 K and ventilation within
0.03 l/s of design. Nothing else departed from the PR5B contract.

**What the hourly series showed.** Of 3 units x 8760 h, only **5 hours** stood above the ceiling. Every one is
the FIRST hour the control law saturates, each followed by an hour at exactly `120.000000`, and the overshoot
rises monotonically with the size of the approach jump - 1.3 / 13.6 / 20.5 / 27.2 / 43.9 l/s of approach gave
0.00014 / 0.00037 / 0.00059 / 0.0267 / 0.0503 l/s of overshoot. A controller overshoot, not single-precision
noise.

**What changed.**
- A flow just outside the law's range is now **reported AT the range and counted**, not refused: the declared
  control cannot command a flow outside its own range (flow fraction <= 1), so the excursion is the solver's.
  The clamp runs **before** the hour's duty, range, law and table coordinates are taken.
- New `Create.RecirculationCoolingClamp_Lps = 0.1` - about twice the largest measured excursion.
- `RecirculationCoolingResult.Count_Clamped` and `MaximumClampedExcursion_Lps` carry every clamped hour.
- `RecirculationCoolingTolerance_Flow_Lps` keeps only the ventilation-deviation duty it was measured for. It
  had served the range boundary too, on one canonical Leeds TRY run (ventilation deviation 0.0263, range
  excursion 0.013); the two do not move together - on this weather the ventilation quantity was unchanged
  (0.0023 / 0.0261 / 0.0299) while the range excursion grew ~4x and exhausted the margin.

**The risk this carries, stated.** 0.1 is a **measured and reviewable** bound, not a derived one. Nothing
structural bounds a controller overshoot, so a steeper model can use more of it - which is exactly why
widening the tolerance was the wrong instrument, and why clamped hours are recorded rather than swallowed. If
a model ever approaches 0.1, revisit the constant with a fresh licensed measurement rather than raise it.
Beyond the bound the flow is left exactly as TAS answered it and **still refuses**, so a solver genuinely
running the branch outside its envelope is never clamped into silence.

**Two flows, and the split matters.** `q[h]` is the flow the declared control could have commanded: it is
what the result REPORTS and what the hour's duty and range are judged on. `qMeasured` is what TAS actually
answered, and it is what the checks of TAS's own fidelity use - the published-table comparison, the domain
observation and the off-law count - because those ask "did TAS follow its table at the flow it used?", a
question about TAS rather than about the design. Judging the table at a flow TAS did not use can refuse a
coil that followed the table exactly, wherever the ceiling sits below the table's airflow axis. Production
always derives the ceiling FROM that axis (`SAM_UI Query.PartOIteration3CoolingResolution`:
`ceiling_Lps = axis_AirFlow.Maximum`), so the two agree there today; the split keeps them right if that ever
stops being true.

**Validation.** `SAM.Analytical.Tas.TM59.Tests` **930 / 930** (was 922; +8, pinning the bound *and* the
reason - the exact excursion the real project measured, that a clamped hour is charged at the clamped flow
but judged at the measured one, and that `0.1 + 1e-6` still refuses). The table-check test was confirmed to
FAIL without the split, with the spurious refusal it exists to prevent ("the coil outlet departs from the
published table by up to 0.25 K"). `SAM.Analytical.UI.WPF.Tests` 1027 / 1027.

**Build trap, hit while verifying this.** `SAM.Analytical.Tas.TM59.Tests` consumes `SAM.Analytical.Tas.TPD`
as a prebuilt DLL. Changing only TPD source and rebuilding the solution leaves the test project's own copy
**stale**, because its own inputs did not change and its copy step is skipped - so `dotnet test --no-build`
silently runs the old TPD. Rebuild the test project itself
(`MSBuild ...SAM.Analytical.Tas.TM59.Tests.csproj -t:Rebuild`) before trusting a result.
`SAM_Tas.sln` and `SAM_UI.sln` Release (VS MSBuild 18): 0 errors.


## Part O Iteration 3 PR5B - recirculation cooling branch grounded natively and evidenced (**MERGED** as `ac85b5c3`)

2026-09-15. Branch off `sow/2026-Q3` `00f51520` (#57 creation order + #58 table round trip merged). Commit `220ea11f`
(+ this docs commit). **Merged into `sow/2026-Q3` as `ac85b5c3`** - the tip the Part O real-project licensed
acceptance was built from. It needed the SAM_Systems PR5B slice (same branch name); merge order was
SAM_Systems -> SAM_Tas -> SAM_UI, and all four repos have since merged.

**What changes** (`SAM.Analytical.Tas.TPD`):
- **Conversion context.** `Create.SystemVentilationConversionContext` takes the materialised
  `RecirculationCoolings`.
  - Branch connections are excluded from leg intent by identity.
  - A branch outside its unit's own air system or rooms is refused.
- **Dampers.** The duty-carrier damper prototype excludes branch dampers, and `Convert.ToTPD` keeps branch
  dampers Value-typed.
- **`Modify.GroundRecirculationCooling`.** Native read-back of:
  - the 96-cell table, extrapolation off;
  - gate = `HeatingSetpoint` with no modifier; `HeatingDuty` an absolute 0;
  - fan HGF 0, variable speed, absolute ceiling;
  - absolute damper shares.
  - Then the normal controller on the coil's single mixed-return inlet duct, acting on the recirculation
    dampers on every plant day type.
- **`AddModifier`** accepts `CurveModifierVariableType` header names.
- **`Modify.RecirculationCoolingResults`** runs a plant-room `SimulateEx` (duct data) on a disposable TPD
  copy. COM-free `Create.RecirculationCoolingResult` refuses any of:
  - heating;
  - cooling below the gate;
  - flow out of the law's range;
  - table error;
  - ventilation deviation.
  - Off-law hours are counted only.
- **`SystemVentilationRoute.RecirculationCoolingResults`**: a branched document is complete only with
  complete evidence.

**Validation:**
- `SAM.Analytical.Tas.TM59.Tests` 922/922 (+16).
- Licensed paired annual B0/B4 (all 3 dwellings, canonical order), full numbers in
  `SAM_UI/documentation/evidence/PR5B-PRODUCTION-ACCEPTANCE.md`:
  - every branch grounded;
  - 0 h DX cooling below 22 C, 0 h heating;
  - DX = clamped table to 2e-6 K;
  - canonical legs within 0.027 l/s of design;
  - evidence pass reproduces route room temperatures to 0 K;
  - B4 vs re-keyed B4 bit-identical;
  - TM59 8/8 Pass both sides.

## Previous: generic TAS multidimensional TableModifier round trip (branch `fix/generic-tas-multidimensional-table-roundtrip`)

2026-09-15. Branch off `sow/2026-Q3` `5d8cfad0` (the SAM#113 creation-order merge, PR #57). PR against `sow/2026-Q3`,
**not merged**. Replays only the four functional files of `495000a1` (found during SAM#111 PR5B work, previously only on
`codex/pr5b-generic-table-roundtrip`); no PR5B code or manufacturer data.

**What changes** (`SAM.Analytical.Tas.TPD`):
- `Convert/ToSAM/Modifier.cs` (TPD -> SAM): native axis order kept; gaps, duplicate or invalid axes and a missing
  multiplier operator are refused (null) instead of silently collapsed or thrown; TAS booleans read as non-zero = true
  (native `-1`).
- `Modify/AddModifier.cs` (SAM -> TPD): a 1D/2D/3D grid is validated (finite values, complete rectangular grid, unique
  coordinates, unique parseable axes) BEFORE any native modifier is allocated; real axis sizes and coordinates are
  written, each cell to its own coordinate; `Extrapolate` is written as TAS `-1`/`0`.

**Validation:** Release build 0 errors; `TableModifierRoundTripTests` 9/9; creation-order tests 7/7;
`SAM.Analytical.Tas.TM59.Tests` 906/906. Licensed (harness `C:\TasOut\pr5b-continuation\h-table`, evidence
`...\ord\table\`): 3-axis equality table (3 x 4 x 8) -> SAM JSON save/reload -> production AddModifier -> save/close/reopen
-> production ToSAM: sizes 3x4x8, axis order ODB/EDB/EFlow, equality, raw Extrapolate 0 / -1 for false / true, 96/96 cells,
max |diff| 0. TAS's own default Extrapolate for a fresh table is 0. Existing 1D behaviour: the canonical B0 conversion's 14
native modifier tables (fan/pump/heat-pump/PV, all 1D) are identical before and after the fix (flag, multiplier, sizes,
variables, values). The PR5B B4 document now generates on the integration line (it faulted `RPC_E_SERVERFAULT` at
generation without this fix) and its native creation order equals the SAM#113 canonical order.

**Exact next step:** review / CI, then merge; only then regenerate Iteration 3 B0 and B4 together on `sow/2026-Q3`.

## Previous: Part O Iteration 3 PR5A - SAM_Tas slice (SAM#111)

Branch off `sow/2026-Q3` `6c622309` (after PR #55, the B0/A parity decomposition). PR against
`sow/2026-Q3`, **not merged**. PR5A merge order is SAM -> SAM_Systems -> SAM_Tas -> SAM_UI; SAM's
slice (#117) and SAM_Systems' slice (#23) are both merged, and this branch is built against them.

**What changes** (all in `SAM.Analytical.Tas.TPD`):
- `Convert/ToTPD/Exchanger.cs`: writes `ExchCalcType` explicitly (previously never written - it only
  ever applied as `Simple` because that happens to be TAS's own default for a new exchanger,
  measured in Phase 0 X1-D) and drops the duplicate `ExchLatType` write (the same line appeared
  twice, verbatim).
- `Enums/SystemVentilationFanHeatGainPolicy.cs` (new): `ClearToZero` (the B0 control, the default,
  unchanged behaviour) / `FromSystemsGraph` (leaves a fan's `HeatGainFactor` exactly as SAM_Systems'
  PR5A settings wrote it, read back but never forced). Threaded through
  `Create.SystemVentilationConversionContext` (new optional parameter) ->
  `SystemVentilationConversionContext.FanHeatGainPolicy` (new property) ->
  `Create.SystemVentilationRoute` (new optional parameter, defaults preserve the exact prior
  signature's behaviour) -> `Modify.GroundVentilationFans` (branches on the policy instead of
  unconditionally clearing).
- `Modify/GroundVentilationExchangers.cs` (new): reads back `ExchCalcType` the same way
  `GroundVentilationFans` reads back `HeatGainFactor`, refusing if TAS did not keep `Simple` - a
  no-op wherever no exchanger exists, which includes every B0 system. Called from `Convert/ToTPD/TPD.cs`
  immediately after `GroundVentilationFans`.

**Licensed verification** (not the full A-D operating-point matrix - see the evidence doc for
scope): a purpose-built harness (`C:\TasOut\pr5a_h`, outside any repository) drove the real
`Create.MechanicalVentilation` -> `Create.SystemVentilationConversionContext` ->
`Convert.ToTPD` production path against real TAS, using the shipped `MVRE.json` and PR5A
`MechanicalVentilationUnitSettings` with two deliberately different fan heat gain factors (0.5/1.0).
Confirmed: `ExchCalcType` writes and reads back as `tpdExchangerCalcSimple`; `FromSystemsGraph`
preserves both figures distinctly; `ClearToZero` still forces both to 0 even when the source
settings state otherwise - the regression check that matters, since every existing caller gets
`ClearToZero` with no code change of their own. Full detail, including the exact native notes:
`Documentation/evidence/PARTO-PR5A-SAMTAS-CONVERSION-EVIDENCE.md`.

**Not in this slice** (frozen plan, deliberately deferred): the licensed operating-point evidence
A-D (needs a zone-identity-matched model and the full-year canonical TSD/TPD route - SAM_UI/Phase 5
territory), the B3 supply-limit setpoint (`HeatRecoverySupplyLimit_C` is carried on the SAM_Systems
settings type but nothing here interprets it), and E1/E2 (still unsourced - no real MRXBOX figure
appears anywhere).

**Validation:** `SAM.Analytical.Tas.TM59.Tests` **890/890**, unchanged from baseline (this slice adds
no COM-free test coverage - no `Fan`/`Exchanger` COM fakes exist in this repository to test against,
and building one from scratch for a single native property was judged higher-risk than the direct
licensed check above, which exercises the real interop rather than a hand-built approximation of
it). `SAM_Tas.sln` Release (VS MSBuild, .NET Framework - `dotnet build` cannot build the COM interop
projects): 0 errors.

**Next step:** independent review and merge of this PR. Then the **SAM_UI PR5A slice** (Phase 5):
orchestration/behaviour-mode, per-AHU resolution from the catalogue, paired B0/selected-product
ledger and evidence, and only then the licensed operating-point evidence A-D and any annual
acceptance.

## Previous: TBD yearly profile one-hour-shift fix - SAM-BIM/SAM_Tas PR #52
`fix/tbd-yearly-profile-one-hour-shift`, off `sow/2026-Q3` at **`81d78841`** (after PR #51, the PR3 bridge
merge). SAM_Tas only; no SAM / SAM_Systems / SAM_UI change.

2026-09-11 - **IMPLEMENTED, LICENSED BEFORE/AFTER CONFIRMED, NOT MERGED.** This is the follow-up the PR3
section below raised ("`SetYearlyValues(float[])` ignores element 0 ... raised as a separate follow-up"):
`Modify.Update` and `Modify.UpdateACCI` handed `SetYearlyValues` 0-based `float[8760]` arrays, so every
yearly profile landed one hour early with hour 8760 duplicated. Fix: `Modify.UpdateYearlyValues`
(`SAM.Analytical.Tas/Modify/Update.cs`) writes 0-based hour k into slot k+1 through an 8761-long array,
element 0 unused, one COM call, refusing anything that isn't exactly 8760 hours; `Modify.Update`'s yearly
branch and both `UpdateACCI` paths go through it. `TPD.PlantSchedule.SetYearlyValues(int[])` was measured
0-based and exact and is deliberately unchanged. Regression: `YearlyProfileAlignmentTests`, with
`TasProfileFakes.FakeProfile` corrected to the measured behaviour. Evidence:
`Documentation/evidence/TBD-YEARLY-PROFILE-SHIFT.md`; the raw probe logs were distilled into it and
removed in a hygiene pass on the review feedback. Tests: TM59 890/890 and benchmark 16/16, locally and in
CI (build + spdx green). Awaiting human review (michaldengusiak, ZiolkowskiJakub requested).

## Previous: PR3 of SAM-BIM/SAM #111 - the ResultantTemperature thermostat bridge
`part-o/iteration3-resultant-temperature-bridge`, off `sow/2026-Q3` at **`5bec3a6d`** (the PR2 merge).
SAM_Tas only; no SAM / SAM_Systems / SAM_UI change.

2026-09-10 (night) - **IMPLEMENTED, LICENSED BRIDGE ACCEPTANCE COMPLETE.** Route:
`TPD ZoneTemperature -> COPY of the route's own no-IZAM TBD -> ticLL AND ticUL := achieved air
temperature, hour by hour -> second TSD -> ResultantTemperature per Space.Guid`, behind
`IResultantTemperatureProvider` (`ThermostatBridgeResultantTemperatureProvider` is the one class to delete
when TAS Systems answers a native resultant temperature). Evidence:
`Documentation/evidence/PR3-RESULTANT-TEMPERATURE-BRIDGE.md`.

On the real fixture (`A7E09A25...7E4B`, intent byte-identical to PR2's): 8 of 8 rooms, 8760 ZoneTemperature
-> 8760 heating and 8760 cooling thermostat slots on both internal conditions, **max transfer delta 0**;
8760 finite ResultantTemperature each; achieved air within 0.0010 K of the imposed series; all 9 TBD zones
renamed to one string -> 8 of 8 rooms bit-identical; source TBD/TSD hashes unchanged; second simulation
evidenced by a fresh TSD. PR2's own acceptance (items 4-15) and 14/14 directed topology re-run PASS on
this build. Tests: `ThermostatBridgeTests` 21/21; TM59 882/882 (861 + 21); benchmark 16/16.

Native facts this PR measured, not to be re-derived:
- `TBD.profile.SetYearlyValues(float[])` **ignores element 0** and repeats the last element: a 0-based
  `float[8760]` shifts every hour by one, silently. Write `yearlyValues[h]`, h = 1..8760, and read back.
  `SAM.Analytical.Tas/Modify/Update.cs` and `UpdateACCI.cs` write 0-based arrays through it - raised as a
  separate follow-up, deliberately not changed here.
- TSD annual index k == yearly slot k + 1 == 0-based hour k (self-imposition control + acceptance, +-1 h
  shift tests). TAS Systems ZoneTemperature is single precision, so the TBD profile stores it exactly.
- SAM's TBD export gives each zone its own normal + HDD internal conditions, thermostats radiant 0 /
  proportional 0 - the air control the bridge needs; the bridge refuses rather than reconfigures otherwise.

Harness: `C:\TasOut\pr3\h` (PR2 re-acceptance harness + `Bridge.cs`, archived as
`Documentation/evidence/PR3-harness-*.txt`); modes `prep`, `source`, `bridge`, `bridgecheck`, `rename`.

## Previous: PR2 (merged as SAM-BIM/SAM_Tas #50, merge `5bec3a6d`)

## Branch
`part-o/iteration3-tas-systems-route`, off `sow/2026-Q3` at **`ec7f505`**. The corrective
re-acceptance delta sits on top of **`fc32261`**, the previously pushed head.

This is **PR2 of SAM-BIM/SAM #111** (Part O Iteration 3 - explicit Systems/TPD ventilation route).
Open as **SAM-BIM/SAM_Tas PR #50**, head **`8485cf6`**, CI green (build PASS, spdx PASS), mergeable,
**not merged**. A final PR-level sanity review passed; a Codex P2 finding found after it is fixed.

## Last updated
2026-09-10 (evening) - **CODEX P2 FIXED: the no-IZAM workflow now fails closed on a surviving IZAM.**
`WorkflowCalculator`'s `RemoveIZAMs` step verified the sweep but only NOTED a survivor and continued
to save/size/simulate - a building that refused removal would have reached the Systems route as its
"no-IZAM" source with its ventilation double-counted. Fixed on the calculator's established refusal
convention: the COM-free decision is `Query.IzamSurvivorRefusal` (new, `Query.CancelNote` precedent);
the calculator adds the refusal and returns `null` BEFORE the save, and `Create.NoIzamThermalSource`
already records a null return as a failed call. Zero-IZAM path unchanged; `RemoveIZAMs = false`
callers untouched. Regression: `NoIzamSurvivorRefusalTests` (3 tests). Evidence:
`Documentation/evidence/PR2-NOIZAM-FAILCLOSED.md`; Codex thread answered on the PR. Release build
clean; TM59 tests 861/861; benchmark tests 16/16. Earlier the same day: SPDX headers added to the
four files CI named (`76d95e8`), header check green.

2026-09-10 - **PR2 CORRECTIVE RE-ACCEPTANCE.** A manual inspection of the previously accepted `.tpd`
found the acceptance FIXTURE invalid, not the conversion: the harness authored its own ventilation
design by ascending space guid instead of reading the one the model states, so the accepted document
carried transfers running the opposite way round to Iteration 1a's real design
(`Kitchen_4 -> Bedroom 2_3` where the design states `Bedroom 2_3 -> Kitchen_4`). The whole acceptance
has been re-run on a real SAM_UI model prepared by the production route.

**`Documentation/evidence/PR2-REACCEPTANCE.md` is the valid PR2 acceptance.** The `f2.sam` engineering
acceptance in `PR2-ACCEPTANCE.md` is marked SUPERSEDED, with its independently-true TAS API findings
explicitly kept.

What the corrective run establishes, on
`SAM_zoningAM-CIBSEfutureZ1.sam` (`A7E09A25…368D7E4B`) - three flats, three MVHR systems, 8 rooms:

1. **Phase A - the fixture is a valid real-design reference.** Reproduced with the production calls
   SAM_UI makes: `PartFCalculator.Calculate("Flats")`, `Query.PartFDwellingZones`,
   `Modify.PreparePartOIteration(BasePassive, zones, {zone -> "MVHR"}, null, false)`.
   `Successful = True`, no refusal, **0 of 8 rooms discontinuous**, 0 cross-dwelling transfers. Run
   twice - reading the model's carried `PartFSpaceData` and re-running the sizing - and the two intent
   tables are byte-identical.
2. **Phase B - PR1 refuses a real model, and three staged runs say exactly why.**
   `Create.MechanicalVentilation` walks EVERY `VentilationSystem` the cluster carries and requires each
   to name a resolvable unit, while production `PreparePartOIteration` deliberately PRESERVES the
   model's authored `NV` / `UV` / `MV` systems. Recorded as PR1 debt; **`SAM_Systems` untouched**. The
   harness scopes by the design itself - a system with no design terminal materialises no duty - and
   the run asserts the intent table is byte-identical before and after that step.
3. **Phase C - one fresh no-IZAM source** from this fixture, full year, hashed before and after every
   run and `UNCHANGED`. Items 1-3 PASS: 0 IZAMs, 0 non-zero `ticV` of 30 internal conditions, `ticI`
   present on 30 of 30 and non-zero on 9.
4. **Phase D - items 4 to 15 all PASS** over hours 0..8759: 3 native systems, 8 of 8 rooms with 8760
   finite `ZoneTemperature` values, every duty on its frozen native carrier, `"Done"` per air system.
5. **Directed topology - 14 legs, 14 PASS, 0 FAIL**, every edge from `IDuct.GetUpstreamComponent` /
   `GetDownstreamComponent` and no screen coordinate read. `Bedroom 2_3 -> Kitchen_4` and
   `Bedroom 2_6 -> Kitchen_7` are native in the design's own direction. No reversal, no cross-dwelling
   edge, no invented `Corridor_1` path, no missing or extra transfer.
6. **Fan operation re-proved** - 6 of 6 fans at their derived duty in 8760 of 8760 hours,
   `HeatGainFactor = 0`. **SAM #113's flow-sizing order sensitivity was not encountered** on this
   fixture; neither fixed nor hidden.
7. **One production correction, presentation only.** The layout classified a room by its duties alone,
   so a kitchen that extracts 55 l/s AND transfers 8 l/s on to an ensuite went to the terminal
   extract-only column past the transfer column - forcing its own outgoing duct back through its own
   box, 2 passes on the first real-design run. The extract-only column is now for a room the air path
   ENDS at; 0 overlaps and 0 passes. Positions are measured inert: every component moved, all 8 zones'
   8760-hour series byte-identical.
8. **Regression.** `SystemVentilationDesignDirectionTests` - COM-free, real PR1 graph, transfer stated
   deliberately AGAINST ascending guid order - pins direction and roles against the class of defect.
   The synthetic `Design()`/`Unit()` harness path is removed, not merely unused.

The earlier closeout of `0fefca76` (below) stands as the record of the four API/configuration items it
fixed; its `f2.sam` engineering verdicts do not.

---

## Previous - 2026-09-10, PR2 CLOSEOUT after the independent re-review of `0fefca76`. Four items, and
what they found:

1. **whitespace gate** - `.gitattributes` exempts end-of-line blanks in committed
   `Documentation/evidence/*.log` transcripts only, so they stay byte-faithful; `git diff --check` is
   clean.
2. **this file** - the per-zone continuity diagnosis and the `BindVentilationLegs` read-back wording
   corrected (below).
3. **fan operation - a real defect, fixed.** The reviewed configuration's fans ran on the template's
   demand-driven occupancy function schedule, and production refused the frozen constant 1.0 yearly
   schedule. Now only a yearly schedule operable 8760 of 8760 hours is accepted; the accepted run's
   four fans deliver 44 l/s in 8760 of 8760 hours, read from TAS's own hourly fan Load.
   `Documentation/evidence/PR2-FAN-OPERATION.md`.
4. **item 10 and provenance** - one regenerated, hashed thermal source under every check; item 10
   grounded on no IZAM, zero `ticV` and explicit Systems ventilation, flags as corroboration only.

Plus, after manual inspection of the accepted TPD: a **presentation-only layout** of the explicit
route's schematic (no connectivity, duty, schedule or creation-order change; 45 overlaps and 225
duct-through-box passes down to 0 and 0), and a native **flow-sizing order sensitivity** found and
filed separately as SAM-BIM/SAM#113.

### FINAL ACCEPTED PR2 artifacts (licensed machine)

The canonical acceptance fixture is **`SAM_zoningAM-CIBSEfutureZ1.sam`**, a real SAM_UI model, prepared
by the production Approved Document O Iteration 1a route. Manual inspection of the document below in the
TAS GUI: **PASS** - one MVHR per dwelling, bedroom/living supply, transfer toward kitchen / ensuite /
bathroom extract, no cross-dwelling ventilation, `Corridor_1` outside every dwelling system.

| purpose | path | SHA-256 |
| --- | --- | --- |
| the acceptance fixture, as found | `C:\Users\michal.dengusiak\OneDrive - Tetra Tech, Inc\Documents\SAM_daily\2026-07-15 PartO\SAM_zoningAM-CIBSEfutureZ1.sam` | `A7E09A25AE29C7DBB4C690D747A96FCD9F110CA27FB4DC2ABE816755368D7E4B` |
| the prepared model Iteration 1a produced | `C:\TasOut\pr2r\a2\prepared.sam` | `5D4756F45956D5F91D01B1EA55CCC3D516762D54756231F8884F928F3332CE18` |
| **FINAL ACCEPTED PR2 TPD** | `C:\TasOut\pr2r\final\acc.tpd` | `553C9E1C364E6F528417247DCE0FE1D31B8884939EDA8A5217B46BDBF5B755FE` |
| its thermal source (TBD), no-IZAM | `C:\TasOut\pr2r\c1\src.tbd`, read as a hashed copy at `C:\TasOut\pr2r\final\src.tbd` | `A01CFBB6B8CD9EB6F7B103696A1DE2A1697C4396708C70561D45C0A18D3C9654` |
| its paired TSD | `C:\TasOut\pr2r\c1\src.tsd`, read as a hashed copy at `C:\TasOut\pr2r\final\src.tsd` | `7239B8D139F7703F6EDDA4B13EDD9D86FACBB039E470EA243A61C44BDE5A5277` |
| hashes before/after the run | `C:\TasOut\pr2r\final\provenance.txt` - both `UNCHANGED` | committed as `PR2-reacc-provenance.txt` |

The full artifact manifest is section 10 of `Documentation/evidence/PR2-REACCEPTANCE.md`.

**SUPERSEDED.** The previous artifacts - `C:\TasOut\pr2z\final-layout2\acc.tpd` and the
`C:\TasOut\pr2z\src\acc.tbd` / `acc.tsd` lineage, produced from `f2.sam` on a different licensed
machine - are **not** the accepted PR2 artifacts, and their engineering network was never Iteration 1a
parity. They are listed in `Documentation/evidence/PR2-ACCEPTANCE.md`, under that document's SUPERSEDED
banner, as the record of the investigation that produced the still-valid TAS API findings.

## Baselines

| repository | SHA | note |
| --- | --- | --- |
| SAM-BIM/SAM | `413215cca722a70b660c4ef367f6faab1d6d9357` | read-only, **untouched and clean** |
| SAM-BIM/SAM_Systems | `89cf139966f4fe426459851d09f052834551792f` | the PR1 merge (#20), read-only, **untouched and clean** |
| SAM-BIM/SAM_Tas | `ec7f50543e123f8a734b6e27b2b16c0cf1f1edde` | this repo's base |

Build order SAM -> SAM_Mollier -> SAM_Systems -> SAM_Tas. The solution needs .NET Framework MSBuild
with **Restore and Build as separate invocations**.

## THE NATIVE FACTS THE ROUTE RESTS ON

Full transcripts: `Documentation/evidence/PR2-BUILD.md` (this session's findings and the acceptance),
`PR2-CHECKPOINT-1B.md`, `PR2-NATIVE-TAS-FINDINGS.md`. Harness at `C:\TasOut\inv`, argv modes,
**one operation per process**, TAS GUI closed.

1. **A TSD `ZoneLoad.GUID` IS the TBD `zone.GUID`.** Measured with `inv.exe zoneload-identity`: 2 of 2
   load guids on fixture 1 are also TBD zone guids, and `GetZoneLoadForGuid` resolves them braced or
   bare. SAM stamps that same guid onto the analytical space as `SpaceParameter.ZoneGuid`, so the whole
   room chain is identity with no display name anywhere.
2. **Simulate the AIR SYSTEMS, not the document.** `ITPD.Simulate` / `IPlantRoom.Simulate` simulate the
   plant too, and the shipped `MV.json` plant cannot size a flow. `ISystem.Simulate` answers `"Done"`.
   **Do not "simplify" `Modify.SimulateSystems` back to the document-level call.**
3. **ABSOLUTE FLOW UNIT = l/s**, proven by TAS's own ACH arithmetic (200 m3 at 8 ACH -> 444.444).
   Applies to `SystemZone.FlowRate`, `.FreshAir` and `Damper.DesignFlowRate` alike.
4. **Read every native identifier late-bound.** The typed `ISystemComponent.GUID`/`.Name` accessors
   **throw** on a `SystemZone`. `Query.NativeIdentity` encapsulates it - do not convert to a typed read.
5. **`SystemZone.GUID` and `ZoneLoad.GUID` are DISTINCT.** `AddZoneLoad` is the pairing point;
   `GetSystemZoneZoneLoad` is zone->load; `GetZoneLoadForGuid` is load->result.
6. **`AddDuct` accepts several ducts on one ordinary component port, but TAS rejects that air system
   when it is simulated** - `"<system name> Has Errors"`. A `Junction` is the native branch carrier.
   Measured both ways this session (`inv.exe fanout` accepted the ducts; the simulation refused them).
7. **A `Damper` on `tpdFlowRateNearestZoneFlowRate` is ambiguous once more than one zone sits behind
   it**, and TAS says so. The template's supply damper is switched to
   `tpdFlowRateAllAttachedZonesFlowRate` on the explicit route; it is a derived topology component and
   the rooms' own zones remain the supply authority.
8. **TAS replaces an air system's in-memory result surface when `ISystem.Simulate` is called for the
   next system in the same document.** Reading all zones after the loop returns results for the last
   system only. `Modify.SimulateSystems` therefore captures each system's series before advancing.
9. **TAS will only simulate a design whose air is continuous in every zone** - each room's inflow
   equals its outflow. **Unit-level balance is not the decisive condition**: in the as-designed
   fixture one unit already balanced at 44/44 l/s and TAS still answered `"Sizing Flow Failed"`, and
   balancing both units left 8 of 9 rooms discontinuous and still refused. The refusal comes from
   per-zone airflow discontinuity. The licensed *simulable control* corrected the transfer duties to
   satisfy exact per-zone continuity (which, as a consequence, raised one extract terminal) - in the
   harness only. **Production never repairs or rebalances the analytical design** - the route
   refuses, which is correct: a design whose rooms do not conserve air is a defect in the design.
   The three refusal runs are tabled in `PR2-ACCEPTANCE.md`. **On a real design there is nothing to
   correct**: production `Modify.PreparePartOIteration` routes the dwelling's transfer air and then
   refuses an unbalanced node itself, so the accepted fixture arrives with 0 of 8 rooms discontinuous
   and simulates as stated - `PR2-REACCEPTANCE.md` section 3. The `balanced` and `continuity` harness
   modes are gone.
10. **`GetResultsData` returns an array that `GetValue(int)` indexes out of bounds.** Walk it with
    `foreach`.
11. **The TBD workflow needs a full-year simulation on this route.** `SimulateTo = 1` produced a
    one-day TSD and the workflow then died in its post-simulation results step with
    `COMException: The RPC server is unavailable`. `SimulateTo = 365` completes. **Root cause measured
    2026-09-10:** `TSD.exe` genuinely crashes on a one-day TSD - Windows Application Error
    `0xc0000005` in `TSD.exe 2.0.0.1` - inside `Modify.AddResults`; the results have already been read
    and it is `SAMTSDDocument.Dispose`'s `close()` that surfaces the RPC message. Reproduces on both
    fixtures, so it is not design-specific. Tracked separately; the full-year acceptance route is
    unaffected. `PR2-reacc-oneday-tsd-crash.log`.
12. **A fan answers one hourly series: `GetResultsData` variable 9, its Load = Q x dp / eta.** Every
    other variable `0..24` fails. Delivered flow is `Load x eta / dp`. A yearly plant schedule is an
    on/off table; factor 1.0 is `GetNumOperableHours() == 8760`. `GetNumOperableHours()` throws
    `"Not a Yearly Schedule"` on a function schedule, and `GetYearlyValue(hour)` answers 0 always.
13. **Native component GUIDs are per document.** Two documents from identical code and settings differ
    in every native guid (118 lines) and in nothing else. Compare regenerated documents by the
    canonical network with guids masked, and results by `ZoneLoad` guid (the TBD zone guid).
14. **Position and direction are presentation only.** Moving all 42 components of an accepted document
    left all 9 zones x 8760 hours of ZoneTemperature bit-identical (0 K). Re-measured on the accepted
    real-design document: every top-level component of all three air systems moved, and all 8 zones x
    8760 hours came back **byte-identical** - `PR2-reacc-layout-inert.txt`. A duct's bend nodes can
    only be given at creation (`IDuct.AddNode`; no removal exists).
15. **TAS flow sizing is sensitive to creation order** - the identical network sized or answered
    `"Flow Sizing Failed"` depending on PR1's guid-derived order. Fails closed. SAM-BIM/SAM#113,
    which stays **open**. **Not encountered on the accepted real-design route**: all three air systems
    completed on the first canonical run with no sizing refusal. One fixture passing is not a fix.

## What IS done - the complete PR2 build

### The conversion, by identity

* `Create.SystemVentilationConversionContext` reads PR1's lineage and the thermal source's
  room-to-TAS-zone map and states, by identity alone, what TAS is to build: every room, every leg,
  every duty. Legs are classified from PR1's **topology** - a `SystemSpace`'s connector 0 is its supply
  inlet and connector 1 its extract outlet - and cross-checked against PR1's own lineage rows.
* `Convert/ToTPD/SystemZone.cs` is the pairing point. It binds the intended `ZoneLoad` by
  `TSDData.GetZoneLoadForGuid` + `AddZoneLoad`, writes the room's supply absolutely onto `FlowRate` and
  `FreshAir` with `Type = tpdSizedVariableValue`, and records the native identities read back off the
  objects. **The `SpaceName`-to-`ZoneLoad.Name` match is gone.**
* `Modify.MaterialiseVentilationDutyCarriers` puts one `SystemDamper` into every extract and transfer
  leg of a **working copy**, with a derived guid. PR1's graph is never touched - pinned by a
  byte-for-byte JSON comparison.
* `Modify.BindVentilationLegs` records one `SystemVentilationConnectionBinding` per PR1 connection,
  and its two paths differ. A **supply** leg is a genuine read-back of the native carrier - the
  zone's `FlowRate`, written earlier at the pairing point. An **extract or transfer** leg is
  **written here**: the intended absolute duty, `DesignFlowRate.Type = tpdSizedVariableValue` and
  `DesignFlowType = tpdFlowRateValue` go onto its damper, and are then read back off TAS's own
  storage for storage verification - a native object that declined the write is refused. That
  read-back is not an independent statement; the acceptance's walk of the saved document is.
* `Create.Ducts` inserts a native `Junction` at every connector end carrying more than one leg, on the
  explicit route only.
* `Convert.ToTPD` returns the **reconciliation's verdict** when a context is supplied, and behaves
  exactly as before when one is not.

### The results, and the gate

* `SystemZoneTemperatureResults` / `SystemZoneTemperatureResult` request only
  `SpaceDataType.ZoneTemperature`, keyed by `.ToString()`, and refuse a missing room, a short series, a
  hole in the period, a non-finite value, a series off the wrong zone or load, two rooms sharing a
  zone, and a series for a room nobody asked about. The period is `EndHour - StartHour + 1`, never a
  hardcoded 8760.
* `Modify.SimulateSystems` captures each air system's series **before** simulating the next.
* `Create.IndexedDoubles` reports the reason TAS gave instead of swallowing it, on all four overloads.
* `ToSAM_SpaceSystemResult` uses `GetSystemZoneZoneLoad` rather than the first load in the collection;
  `ToSAM_SpaceSystemResults` no longer adds a null element per resultless zone.

### The route

* `NoIzamThermalSource` (in `SAM.Core.Tas`, so both assemblies can see it) states both paths, both
  cleanups and the room-to-zone map. `Create.NoIzamThermalSource` forces both cleanups on.
* `SystemVentilationRoute` carries the thermal source, the document, the evidence, the room bindings,
  the leg bindings and the zone temperatures. **There is no partial route**: on any refusal the payload
  is empty, including the document path, enforced in the constructor.
* The path guard refuses to write the TPD over the thermal source's own TBD or TSD, before anything
  runs.

### The closeout

* `Query.ContinuousOperationRefusal` + `Modify.GroundVentilationFans`: a fan's operation carrier must
  be a yearly schedule operable 8760 of 8760 hours - the frozen #111 factor 1.0, supplied by the
  caller through PR1's `MechanicalVentilationSettings.Schedule`. Function (demand-driven), hourly,
  absent or partial schedules are refused with the reason. `HeatGainFactor = 0` kept.
* `Query.VentilationLayout` / `VentilationDuctRoute` / `VentilationJunctionRectangle` +
  `Modify.LayOutVentilationSystem` + a hook before `Create.Ducts`: each room its own row in air-path
  order, transfer dampers beneath the room they leave, extract-only rooms and then extract dampers in
  columns to the right, junctions beside what they branch, ducts routed orthogonally in the lanes
  between rows. Explicit route only; positions and bend nodes only; the replicated conversion is
  untouched.
* Tests: `ContinuousOperationRefusalTests` (9), `VentilationLayoutTests` (6).

### Hardening carried out during this build

* `SimulationDiagnostic` matches a success answer **whole** and only when the text carries no measured
  failure fragment, so `"Done, but Sizing Flow Failed"` refuses. A vocabulary self-check test keeps the
  two lists disjoint.
* `SimulateSystems` keeps every air system's answer rather than only the last.
* Three quadratic paths removed: `BindVentilationLegs` filtered all legs per air system,
  `SystemVentilationRoute.Binding` scanned linearly, and the per-system result capture re-indexed the
  whole document per system.
* `SystemZoneTemperatureResult.TryGetValue` added, because `Values` is a defensive copy on every
  access and reading it per hour is quadratic in the period - which cost one acceptance run.

## Validation performed

| check | result |
| --- | --- |
| `SAM.Analytical.Tas.TM59.Tests` | **858 passed, 0 failed** (855 at the closeout + 1 layout + 2 design direction) |
| `SAM.Analytical.Tas.Benchmark.Tests` | **16 passed, 0 failed** |
| Release build, `SAM_Tas.sln`, .NET Framework MSBuild, Restore and Build separate | **0 errors** |
| `git diff --check` | clean, including the committed `.log` transcripts |
| SAM-BIM/SAM | untouched by this branch, working tree clean |
| SAM-BIM/SAM_Systems | untouched, clean, at `89cf139` |
| Phase A, production Iteration 1a on the real fixture | **PASS** - `Successful = True`, 0 of 8 rooms discontinuous, 0 cross-dwelling transfers |
| licensed acceptance items 1-3, no-IZAM source | **PASS** on the hashed source - `Documentation/evidence/PR2-REACCEPTANCE.md` |
| licensed acceptance items 4-15 | **PASS**, individually, on the candidate FINAL document |
| directed topology, native ports only | **14 legs PASS, 0 FAIL** - `PR2-reacc-topology.txt` |
| fan operation, frozen factor 1.0 | 6 of 6 fans at their derived duty in 8760 of 8760 hours - `PR2-reacc-fan-operation.txt` |
| layout | 0 overlapping boxes, 0 duct passes through a box; 8760-hour ZoneTemperature byte-identical after moving every component |
| licensed full-period route | 3 air systems `"Done"`, 8 of 8 rooms with 8760 finite values |
| scaling | 2.20 indexed lookups per room at 100, 1,000 **and** 5,000 rooms |
| SUPERSEDED, kept as record | the `f2.sam` engineering acceptance - `Documentation/evidence/PR2-ACCEPTANCE.md` |

**One caveat, stated plainly.** The test project's copy of a referenced assembly can go stale after a
solution build: `dotnet build` the test project before `dotnet test --no-build`, or a run will silently
exercise the previous DLL. That cost one confusing failure in this session.

## What is NOT done

* **The branch-junction topology, the orphan-component skip and the nearest-zone damper switch are
  covered by the licensed acceptance only**, not by unit tests: they are decisions about native TAS
  behaviour taken inside COM-touching code.
* **No licensed run at 1,000 or 5,000 rooms.** The scaling evidence is structural, by design - a
  licensed annual simulation at that size would measure TAS, not this code.
* **PR3 and PR4** are not started. `SystemVentilationRoute` is the seam they consume.
* **PR1 has no system scope** - **SAM-BIM/SAM#114**. It cannot be told which of a model's authored
  ventilation systems is the design under assessment, and it refuses any real model that carries the
  `NV` / `UV` / `MV` systems production `PreparePartOIteration` deliberately preserves. Diagnosed with
  three staged licensed runs in `PR2-REACCEPTANCE.md` section 4 and worked around in the acceptance
  harness only, with byte-identity of the intent table asserted across the scoping step.
  **Not fixed here**: `SAM_Systems` is frozen and untouched. #114 states both candidate ownerships -
  a `SAM_Systems` filtering API, or PR4 / SAM_UI caller scoping - and prescribes neither.
  **#114 must be considered before PR4 is declared complete.**
* **A 1-day TAS simulation period crashes `TSD.exe`** natively (`0xc0000005`) inside
  `Modify.AddResults` - **SAM-BIM/SAM#115**. Reproduces on the old fixture too, so it is not this
  design; the full-year runs the acceptance uses are unaffected. Transcript
  `PR2-reacc-oneday-tsd-crash.log`. No production change in this closeout.
* **TAS flow sizing order sensitivity** - **SAM-BIM/SAM#113**, still **open** and unchanged. Not
  encountered on the accepted real-design route; commented there, not closed.
* **The flow-sizing order sensitivity (SAM-BIM/SAM#113)** is recorded, not fixed: fixing it touches
  creation order, which the closeout forbade. The route fails closed on it.
* **The caller must supply the frozen schedule.** PR2 verifies it and refuses without it; supplying
  the constant 1.0 `YearlySchedule` through `MechanicalVentilationSettings.Schedule` is PR4's job.

## Exact recommended next step

Review this branch's diff, then start PR3 against `SystemVentilationRoute`. It already carries
everything PR3 needs - the thermal source with both paths, the room bindings with the full identity
chain, the leg bindings with each design airflow, and the zone temperatures - so PR3 should never need
to open the TPD, guess a basename, match a display name or reconstruct topology. If it finds itself
doing any of those, the seam is wrong and should be widened rather than worked around.

---

## Previous (2026-09-05): the workflow's working model - ownership, and only one copy of it

**Status: implemented and tested. Not merged. Blocked on SAM-BIM/SAM#100.**

### The defect (F1/F3 of the DeepSeek V4 Pro Max review)

`WorkflowCalculator.Calculate` has always worked on a copy and handed the result back, so that a run which
failed or was cancelled left the caller's model as it was. The copy was
`new AnalyticalModel(analyticalModel)`, which rebuilds the cluster's dictionaries but stores **the same**
`Space`, `Panel` and `Aperture` instances.

That is safe for an operation writing by same-guid replacement. This one does not:

- `Modify.UpdateIds` reads the live objects out of the cluster and stamps `SpaceParameter.ZoneGuid`,
  `PanelParameter.ZoneSurfaceReference_1`/`_2`, `PanelParameter.BuildingElementGuid` and the aperture
  identity parameters straight onto their parameter sets **in place**;
- `UpdateAdiabatic`, `UpdateBuildingElements`, `UpdateThermalParameters` and `UpdateApertureDefinitions` do
  the same.

Every one of those writes was visible through the caller's model. On Iteration 2B the caller **is** the
retained last-valid design of the previous round, so a round that stamped it and then failed or was
cancelled handed that design back carrying a later run's identities - disagreeing with its own persisted
`SimulationResultProvenance.Fingerprint_Model`, which reads on reopening as a false "the model has changed
since the simulation results were produced from it" and forces a re-simulation nobody needed.

### The fix, and why at the copy

One deep working copy at the top of `Calculate`, through the SAM authority. Converting each conversion step
to replacement semantics - the alternative - would be a redesign of the TAS model conversion for an
isolation guarantee one copy already gives, and it would have to be repeated for every step added later.

### And then only one of them

`Calculate(AnalyticalModel)` is unchanged and still takes the copy, so every caller that has not thought
about ownership - the Grasshopper components, the benchmark CLI, `Modify.RunWorkflow`'s other callers -
keeps the guarantee it had.

`Calculate(AnalyticalModel, bool analyticalModel_Owned)` is the seam. `true` is a promise about the
argument: the caller has already taken a deep working copy that nothing else holds.
`Modify.RunPartOSimulation` is the boundary that makes it for a Part O run. Without it the normal Part O
path cloned the whole model **three** times over for one guarantee - `Simulate`, `RunPartOSimulation` and
here - which on a five thousand space project is most of a second and half a gigabyte of allocation to no
end.

### Changed files

- `SAM_Tas/SAM.Analytical.Tas/Classes/WorkflowCalculator.cs`
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/WorkflowModelOwnershipTests.cs`

The tests use the **production** `SAM.Analytical.Tas` identity enums on the real types and reproduce exactly
the read-mutate-`AddObject` sequence `UpdateIds` uses, including the `RemoveAperture`/`AddAperture` pairing.
No TAS licence: what is pinned is the ownership of the objects those writes land on, which is decided by the
copy and not by TAS. They cover the shallow leak, the deep isolation, a failed or cancelled round leaving
`SimulationResultProvenance.IsCurrent` true, a successful run still adopting the stamped model, the
warm-start path being isolated **even when the stamped values are identical**, and relations surviving.

### Merge order

1. **SAM-BIM/SAM#100** - the ownership constructor, the deep-clone completeness fix, and the TM59 series
   rules. Nothing else compiles without it.
2. **SAM-BIM/SAM_Tas#48** - the workflow's owned-model overload. Needs #100.
3. **SAM-BIM/SAM_UI#87** - the Part O boundaries, the capacity-envelope name, and the full-year authority.
   Needs #100. Independent of #48 to compile; both are needed for the F1/F3 invariant to hold end to end.

`SAM_Systems` is untouched.

### Validation

| Suite | Result |
| --- | --- |
| `SAM.Tests` | 1934 passed, 0 failed |
| `SAM.Analytical.Tas.TM59.Tests` | 690 passed, 0 failed |
| `SAM.Analytical.UI.WPF.Tests` | 510 passed, 0 failed |

`SAM.sln`, `SAM_Tas.sln` (MSBuild - COM references) and `SAM_UI.sln` all build with 0 errors.
`git diff --check` is clean in all three repositories.

Deep-clone cost, measured Release on 5,000 spaces / 30,000 panels: **250.7 ms, 136.8 MB**, against
36.5 ms / 13.8 MB for the shallow copy. Paid **once** per Part O TAS run.

### Remaining risks and next task

- No licensed TAS run was made. Every invariant here is established by deterministic tests over production
  code; what is not covered is TAS's own behaviour, which none of these changes touch.
- `SAM.Weather.Query.RunningMeanDryBulbTemperatures` still throws on a weather year shorter than the one the
  running mean needs, so a TSD with a damaged weather record fails loudly rather than being refused with a
  diagnostic. Pre-existing, characterised by test, and a fix reaches wider than Part O.
- Next: the pre-Iteration-3 list is unchanged - PF3-PF7 and `SetSpaceDesignFlowRate` indexing, the Part O
  defaults / minimum-click audit, re-isolation, the Grasshopper variable-output updater, catalogue identity
  drift, final UI acceptance, then freeze Iterations 1-2.

## Previous (2026-09-03, later): one plant zone per unit, however many times the model is converted

**Status: root-caused, fixed and tested.** The defect reported from a comparison of working Part O TBD
files - six MVHR plant zones where three were expected, three of them without internal conditions.

### Root cause

`Modify.UpdateIZAMs` built each unit's plant zone unconditionally. For every `AirHandlingUnit` carrying an
`AirHandlingUnitAirMovement` it created a fresh 3 x 3 x 2 box (`Create.AdjacencyCluster`), appended it to
the TBD building through `Update`, and renamed it to the unit. **Nothing looked for the zone it had written
on a previous run.**

The trigger is the Part O warm start. `WorkflowCalculator` copies the canonical TBD onto the round's path
and skips only the geometry conversion block - everything after it, `UpdateIZAMs` included, still runs. So
a round opens a file that already carries the baseline's plant zones and appends its own beside them.

That also explains the missing internal conditions exactly. The ICs and IZAMs are removed **by name**
(`RemoveInternalConditions` / `RemoveIZAMs`) before the rebuild, and the rebuild assigns the fresh ones to
the new zone. The older zone therefore survives with its name, its geometry, and nothing else: three
zones with internal conditions and three without, which is what was observed.

Because each round starts from a fresh copy of the canonical rather than from its predecessor, the count
was pinned at exactly 2x per warm-started round rather than growing 3x, 4x - which is why it was 6 and not
12, and why it was easy to miss.

**Not** a duplicate-IZAM problem: the IZAMs and ICs were correctly replaced. The duplicates were zones
only, and the orphans were inert - no internal condition, no air movements, nothing referencing them - so
TAS still simulated. Their one real hazard is geometric: `elevation` restarts from the same datum on every
run, so the second round's boxes are created at the same elevations as the first round's.

### The fix

Idempotency at the authority, `SAM.Analytical.Tas.Modify`. No TBD file is patched after generation.

- **`PlantZoneIdentity`** (new) - the generated zone states which unit it belongs to, as one `"; "`
  segment of `TBD.zone.description`: `[SAM_AHU_V1]=<AirHandlingUnit.Guid>`. That description is the
  channel the exporter already uses for SAM-only data (`[Id]`, `[LevelName]`, `SAMZoneMetadata`'s own
  section), and every segment this class does not own is preserved verbatim. Versioned, with a single
  `Compose`/`Parse` pair.
- **`Modify.ResolvePlantZoneReuse`** (new) - decides, per unit, which existing zone is its plant zone.
  Two passes: **identity** (the zone states this unit's guid - unaffected by renaming the unit or by two
  units sharing a name), then **adoption** of a pre-fix zone that states nothing, matched by name and only
  when that name is not one of the model's own spaces. One zone is claimed at most once. It never deletes.
- **`UpdateIZAMs`** now takes the resolved zone where there is one and builds a box only where there is
  not, then stamps the identity every round - so a zone adopted by name answers by guid from then on.

`AirHandlingUnit.Guid` rather than the zone name is the identity because the name is a presentation
string: not unique, and changed by a rename, either of which would orphan the zone and duplicate it.

`ResolvePlantZoneReuse` is pure `SAM.Analytical` - no TBD/COM type - for the same reason
`ResolveAirHandlingUnitMovements` is: it can be tested with no TAS licence, install or COM server.
`PlantZoneCandidate` is the plain carrier that keeps it that way.

### What is deliberately NOT done

Zones left over from the accumulation that already happened are **not** removed. They cannot be told apart
from a modeller's own work with confidence, and the contract is to stop the growth, not to prune history.
A model reconverted after this fix settles at one zone per unit from its next run onward.

### Semantics untouched

`PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow` is read by
neither the plant-zone decision nor the airflow decision, and no airflow code path was touched. Pinned by
`ReusingAPlantZone_DoesNotChangeTheAirflowAndIZAMPlan` and by SAM's own 1831-test suite, re-run green.

### Files changed

- `SAM_Tas/SAM.Analytical.Tas/Classes/PlantZoneIdentity.cs` - **new.**
- `SAM_Tas/SAM.Analytical.Tas/Classes/PlantZoneCandidate.cs` - **new.**
- `SAM_Tas/SAM.Analytical.Tas/Modify/UpdateIZAMs.cs` - `ResolvePlantZoneReuse`, `PlantZoneCandidates`, and
  the create-or-reuse branch in the AHU loop.
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/PlantZoneIdempotencyTests.cs` - **new**, 15 tests.

### Validation

- `SAM.Analytical.Tas.TM59.Tests` - **684 passed** (669 baseline + 15).
- `SAM.Analytical.Tas.Benchmark.Tests` - **16 passed**.
- `SAM_Tas.sln` (MSBuild, Debug) - **0 errors**.
- `SAM` **1831 passed**, `SAM_UI` **411 passed**, both against the rebuilt `SAM.Analytical.Tas.dll`.

**The tests are load-bearing.** With the reuse resolution temporarily short-circuited to return nothing -
the pre-fix behaviour - 9 of the 15 fail, and the baseline-plus-three-rounds test reports **"Expected: 3,
But was: 6"**: the reported symptom exactly. The repeated-rounds-on-one-file test goes 2 -> 12.

### Not performed

No licensed TAS run. The change is pinned by automated coverage at the conversion authority; a licensed
before/after zone count on the real multi-flat model is still worth doing before release.

## Superseded (2026-09-03): the plant zone's HDD/CDD exclusion, named so it can be pinned

**Status: implemented and tested; PR SAM-BIM/SAM_Tas#46 open against `sow/2026-Q3`. Behaviour-neutral.**

Where the engineering is: **SAM-BIM/SAM#92** (two model-generation defects TAS refuses a model for, and
the `Create.Log` rules that now report both) and **SAM-BIM/SAM_UI#82** (`SAMAnalytical.Check` as a
mandatory Part O pre-simulation gate).

### What changed

`Modify.UpdateIZAMs` already excluded the **HDD** and **CDD** design daytypes from the internal condition
it assigns to an air handling unit's generated plant zone, as a predicate inside a COM loop no test could
reach:

```csharp
dayTypes.RemoveAll(x => x.name.Equals("CDD") || x.name.Equals("HDD"));
```

That is extracted to `Query.DayType_PlantZoneInternalCondition`, which holds the decision and the reason.
`UpdateIZAMs` filters the calendar's daytypes through it. **Which daytypes are excluded does not change.**

### Why it is worth naming

TAS notices the exclusion and warns:

> `Zone 'MVHR-01' is missing internal conditions on some daytypes.`

**The message is expected.** The generated zone is a duct volume standing in for a unit rather than a
room, and it is deliberately not wanted active in the heating and cooling design-day sizing runs. Adding
HDD and CDD internal conditions to silence the warning would put it into those runs, which is the thing
the exclusion exists to prevent.

So the risk worth guarding against is somebody helpfully adding them back to make a TAS warning go away -
which became more likely once SAM_UI#82 made `SAMAnalytical.Check` **Errors** fatal on a prepared Part O
run. This is that guard. Nothing on that path promotes the warning to an error.

### Important decisions and assumptions

- The predicate matches the two names **exactly**. A daytype merely containing `HDD` is a different
  daytype and keeps its internal condition; dropping it would silently shrink the schedule the plant zone
  runs on.
- An **unnamed** daytype is kept rather than throwing - which the old predicate would have done on a null
  `name`. It is not one of the two the exclusion is about.
- No TBD or COM type appears in the query or the tests, so the tests run with no TAS licence, install or
  COM server - the same rule the rest of `SAM.Analytical.Tas.TM59.Tests` follows.

### Files changed

- `SAM_Tas/SAM.Analytical.Tas/Query/DayType_PlantZoneInternalCondition.cs` - new.
- `SAM_Tas/SAM.Analytical.Tas/Modify/UpdateIZAMs.cs` - applies the extracted predicate.
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/PlantZoneDayTypeTests.cs` - new, 13 tests.
- `PROJECT_PROGRESS.md` - this entry.

### Tests, builds and validation

- Full `SAM.Analytical.Tas.TM59.Tests` **669 passed**, 0 failed. `SAM_Tas.sln` builds clean (VS 18
  MSBuild).
- Validated end to end by the licensed TAS acceptance run recorded in SAM#92: Flat 1 isolated out of
  `000000_SAM_AnalyticalModel-It1a-futureZ1.sam` completes a full year (TSD 4,691,076 bytes) **with the
  HDD/CDD exclusion in place**, which is the evidence that the warning is not a blocker.

### Unresolved issues, risks and blockers

- **Observed, not investigated:** the full-building Part O `Opt` TBDs from that acceptance project carry
  **six** MVHR plant zones - three named `MVHR-01/02/03` with internal conditions and three with none at
  all - so `UpdateIZAMs` appears to accumulate a duplicate plant zone per optimisation round rather than
  reusing or removing the previous one. It does not stop TAS and is **not addressed here**. Worth a
  separate look.
- Copilot asked for this `PROJECT_PROGRESS.md` entry on PR #46; addressed by this update.

### Exact recommended next step

Wait for the re-requested Codex review on **SAM-BIM/SAM_Tas#46**. **Do not merge.**

---

## Latest (2026-09-02): starting a run from a canonical TBD

**Status: implemented and tested; PR open against `sow/2026-Q3`.**

### Why it was needed - measured, not assumed

An Approved Document O Iteration 2B optimisation runs the same thermal case ten times over ten designs, and
between rounds only the **ventilation** state changes: the design airflow on each terminal, the balanced
system duty, and the transfer/mechanical network `PreparePartOIteration` rebuilds from them. The geometry,
zones, surfaces, apertures, constructions and the shading calculation are identical every round.

The licensed acceptance run's own timing CSV says how much that is worth. One round of
`SAM_zoningAM-CIBSEfutureZ1.sam`:

| Step | ms |
| --- | --- |
| Opening TBD file | 4 937 |
| Updating Weather Data | 28 |
| Updating HDD and CDD Day Types | 99 |
| Opening T3D file | 10 174 |
| Importing gbXML | 43 |
| Updating T3D file | 345 |
| **T3D to TBD -> Shading** | **26 011** |
| *conversion subtotal* | **41 638** |
| Reusing Aperture Definitions | 1 345 |
| Updating Aperture Types | 2 216 |
| Updating Ids | 3 779 |
| Updating Zones | 1 872 |
| Add IZAMs | 620 |
| Setting Adiabatic | 1 955 |
| Updating Building Elements | 978 |
| **Simulating Model** | **3 622** |
| Adding Results | 5 033 |
| other | ~1 400 |
| **TOTAL** | **64 247** |

**The full-year TAS simulation is 3.6 s of a 64.2 s round.** The conversion is 41.6 s - 65% - and with the
two aperture steps that only the gbXML route needs, 45.2 s, i.e. **70%**.

### What was added

`WorkflowSettings.Path_TBD_Canonical`. Where it is set, `WorkflowCalculator.Calculate` copies that TBD to
`Path_TBD` and **skips the conversion block only** - everything after it runs exactly as it always does:
the adiabatic and building-element updates, `Modify.UpdateIds` (which stamps the TAS zone identities the
assessment resolves results through), `UpdateZones`, the zone groups, `UpdateIZAMs`, sizing, a **real**
full-year simulation, and the results.

**Nothing is reimplemented.** This is the same method body entered with a TBD that already exists, which is
what makes the warm-started and full paths structurally equivalent rather than equivalent by inspection.
The conversion block was already gated on `Path_gbXML`, so the change is a copy, a step count, and three
guards.

### The guards

- **Both `Path_gbXML` and `Path_TBD_Canonical`** - contradictory instructions ("convert the geometry" /
  "the geometry is already converted"). Refused, with its reason, **before** the setup that deletes an
  existing T3D and TBD.
- **A canonical that is not on disk** - refused by name rather than silently falling through to a
  conversion the caller did not ask for. Whether to fall back is the caller's decision.
- **A canonical that is the run's own TBD** - refused, because the copy would overwrite its own source and
  every later round would then start from whatever the last one left behind. Compared on full,
  case-insensitive paths, so a differently spelled route to the same file is caught too.

Each refusal fires `Ended` after `Started`, which the pre-existing null-input return does not - a listener
left waiting on a run that announced itself is a small defect, and pairing them is free here.

### Files

| File | Change |
| --- | --- |
| `SAM_Tas/SAM.Analytical.Tas/Classes/WorkflowSettings.cs` | `Path_TBD_Canonical`, carried through the copy constructor and the JSON round trip. |
| `SAM_Tas/SAM.Analytical.Tas/Classes/WorkflowCalculator.cs` | The copy, its step count, and the three guards. |
| `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/WorkflowCanonicalTBDTests.cs` | New - 6 tests. |

### Validation

- `SAM_Tas.sln` builds clean (MSBuild; the COM references need the .NET Framework MSBuild).
- `SAM.Analytical.Tas.TM59.Tests`: **655 passed, 0 failed** (649 baseline + 6 new).
- The 6 new tests cover the three guards - including the path-spelling variant of the self-overwrite one -
  plus a refused run reporting only its own reason, and the setting surviving a copy and a JSON round trip.
- No TAS COM is needed: every guard returns before anything touches a TBD type, the same way
  `WorkflowCalculatorTests` works.

### What is deliberately NOT decided here

Whether a canonical TBD is still **valid** for the current model. This class cannot know what changed since
it was made; the caller proves compatibility and falls back to the full path where it cannot - see
`SAM_UI`'s `PartOCanonicalTBD`. What this half guarantees is that the canonical file is only ever read.

### Issues / blockers

- None known.

### Next step

- Merge order: `SAM-BIM/SAM#90` -> `SAM-BIM/SAM_UI#79` -> **this** -> SAM_UI's warm-start PR.

## Superseded (2026-08-28): the Part O base MVHR half - merged as PR #44

### The final architecture, this repo's half of it

**SAM physical model (`SAM.Analytical`, unchanged by this repo): four legs, always.**
```
Outside -> AHU (generic Base MVHR unit)
AHU     -> supply room(s)
extract room(s) -> AHU
AHU     -> Outside
```
Every design-terminal-served room is supplied from the unit, or extracted back to it, or both; the
dwelling's own internal transfer air closes each room's balance; the unit is a full four-legged node like
any other MEP system. This is the truth `SAM.Analytical.Modify.AddAirMovementObjects` writes and it is
never altered for export - see [SAM-BIM/SAM#77](https://github.com/SAM-BIM/SAM/pull/77) for that side of
the work, most recently the "one Base MVHR system per assessed dwelling" fix (`PartFCalculator` sizes each
dwelling independently, so this iteration now builds one generic system/AHU **per assessed dwelling zone**,
never one shared system across independent dwellings).

**TAS export (`SAM_Tas`, this repo): the extract leg is flattened, export-only.**
```
Outside -> MVHR
MVHR    -> supply room(s)
extract room(s) -> Outside          (flattened - see below)
```
`Modify.UpdateIZAMs`/`Query.DesignTerminalExtractFlattening` write a design-terminal room's extract as
`room -> Outside` directly rather than `room -> unit` plus `unit -> Outside`, and give the unit **no**
exhaust for that duty. The reason is TAS-specific and does not touch the SAM model: `Modify.UpdateIZAMs`
represents the AHU as one well-mixed TAS thermal zone, which cannot hold a supply airstream and an extract
airstream passing through it without them mixing - a licensed A/B measured what that mixing costs at
+755 W of unstated heat-recovery-shaped gain on the unit's own zone. Flattening removes the meeting point;
every node still conserves exactly, because the same air still leaves the building exactly once.

### This session's fix - second-round Codex P1 (stale AHU exhaust)

**Before this fix**, `Modify.UpdateIZAMs` only queued an air handling unit's outward "IZAM `<AHU>` TO
OUTSIDE" name for removal from *inside* the loop that walks the unit's *current* outward
`SpaceAirMovement`s. A TBD written by an earlier export - back when the unit still had its own exhaust,
before this session's flattening, or before any other topology change removed it - carries that IZAM. A
re-export of a model where the unit no longer builds one never queued it for removal, so the stale exhaust
survived: unmatched outflow on the unit's zone, which TAS refuses to simulate.

**The fix**: the outward name is now queued unconditionally, for every AHU this run is about to process
(every one carrying an `AirHandlingUnitAirMovement`) - independent of whether a current replacement
movement exists. An AHU this run does not touch at all (no `AirHandlingUnitAirMovement`) still contributes
nothing to the removal set, so a modeller-authored unit and its own hand-built IZAMs are untouched.

**Extracted for testability**: the resolution step (`ahuMovements` / `ahuOutwardMovements` /
`icNamesToReplace` / `izamNamesToReplace`) is pulled out of `Modify.UpdateIZAMs` into a new public
`Modify.ResolveAirHandlingUnitMovements`, pure `SAM.Analytical` with no TBD/COM type anywhere in it - so it
runs COM-free, with no licence. `ResolveAirHandlingUnitMovementsTests.cs` (new, 4 tests) pins: an AHU with
no current outward movement still gets its stale name queued (the defect); an AHU with a current outward
movement still gets it queued and carried forward (no regression); an AHU with no
`AirHandlingUnitAirMovement` at all is not processed (the "don't touch modeller AHUs" guard); a flattened
AHU (`guids_AirHandlingUnit_NoExhaust`) still queues its name but does not write a replacement.

`SAM.Analytical.Tas.TM59.Tests`: **649 passed, 0 failed** (was 645, +4).

### Keeping the already-fixed `room -> AHU` stale-name removal intact

The first-round fix - queuing BOTH the pre-flattening `"IZAM <room> TO <unit>"` name and the current
`"IZAM <room> TO OUTSIDE"` name before nulling the movement's destination, so either vintage of a room's
own extract is replaced cleanly on re-export - is untouched by this session's change. Both stale-removal
paths (the room's own extract, and now the unit's own exhaust) are queued unconditionally, independent of
each other and of the current topology, which is what re-exporting an old TBD onto a model built by ANY
prior commit on this branch now requires.

### Licensed status - `A0.sam` retired as the Base MVHR whole-model acceptance fixture

Re-ran the licensed acceptance against the original `C:\TasOut\v40\A0.sam` fixture on the real TAS
harness, exactly as the "Original A0 Final Acceptance" section of `SAM/documentation/PartO-TAS-VALIDATION.md`
describes. **The original acceptance assessed the WHOLE model - all four zones (Flat 1, Corridor, Flat 2,
Flat 3) - confirmed straight from the harness source, not assumed.** Under the SAM-side "one Base MVHR
system per assessed dwelling" fix, that same whole-model run now **refuses outright**, and so does every
one of A0's three flats assessed in isolation:

| dwelling | own net design-duty imbalance | result assessed alone |
|---|---|---|
| Flat 1 (`Studio 1_0`, `Bathroom_2`) | -19.5 l/s | refuses |
| Flat 2 (`Bedroom 2_3`, `Living Kitchen_4`, `Ensuite_5`) | +26 l/s | refuses |
| Flat 3 (`Bedroom 2_6`, `Kitchen_7`, `Ensuite_8`) | -6.5 l/s | refuses |

**This is a fixture defect the fix correctly exposes, not a code regression.** The three imbalances sum to
exactly zero only across the whole building - the fingerprint of the exact bug being fixed: the old
single-shared-system code let one flat's surplus transfer air balance a different flat's deficit through
whatever adjacency happened to connect them, and the "156/156 l/s, 0 W gain, 0 residual" result the original
acceptance recorded was resting on that invalid cross-dwelling transfer. Confirmed directly, twice:

- **Adjacency is not the cause.** Flat 2's own three rooms are directly wall-connected to each other
  (`Bedroom 2_3 <-> Living Kitchen_4 <-> Ensuite_5`) with no need to route through `Corridor_1` or another
  flat - so a self-contained transfer route exists in principle. It still refuses, because `Bedroom 2_3`
  alone (45.5 l/s supply, no extract) needs to shed more air internally than the other two rooms can sink
  (19.5 l/s of combined extract capacity), a **26 l/s deficit intrinsic to this flat's own Part F sizing**,
  independent of any code scope.
- **Every flat fails the same way**, each for its own room-level reason (`Modify.RefuseUnbalancedAirMovement`
  names the exact rooms and residuals in every case) - there is no dwelling-scoping bug hiding behind one
  passing case and two failing ones; the fixture's Part F terminal data was simply never authored (or
  re-sized) to balance per dwelling, only in aggregate.

**Disposition.** `A0.sam` can no longer serve as the licensed acceptance fixture for Base MVHR's
per-dwelling behaviour - not because the fix is wrong, but because doing so would require either (a)
re-authoring the fixture's per-flat Part F terminal duties to actually balance, which is design work on the
fixture, not this PR, or (b) letting transfer air cross dwelling boundaries again, which would silently
reintroduce the bug this PR closes. Production logic was **not** changed to make `A0.sam` pass. The
multi-dwelling seam this fixture can no longer prove is instead pinned by
[SAM-BIM/SAM#77](https://github.com/SAM-BIM/SAM/pull/77)'s own automated `SAM.Tests` (a real, independently
balanced two-dwelling fixture built for exactly this purpose: `TwoAssessedDwellings_EachGetsItsOwnVentilationSystemAndAirHandlingUnit`
and its three siblings). `A0.sam`'s **Iteration 1b** (Base Natural Ventilation) route is untouched and was
re-confirmed live: `1b OPEN/NIGHT` still reproduces the historic **16690/78840** figure exactly, because 1b
never reaches `PrepareBaseMVHR` at all. This session's own fix (the stale AHU exhaust) is a pure `SAM_Tas`
export-path change, unaffected by any of the above, and is fully pinned by the COM-free
`ResolveAirHandlingUnitMovementsTests`.

Evidence: `C:\TasOut\p1a4\` (whole-model refusal, `zonespaces` adjacency dump, 1b regression log) and
`C:\TasOut\single\` (Flat 1/Flat 2 isolated re-runs). Full detail belongs in
`SAM/documentation/PartO-TAS-VALIDATION.md`, not duplicated here.

### Current risks / limitations, carried forward unchanged

- `Modify.Simulate` returns `true` even for a simulation TAS refused to run (it only waits for the TSD to
  unlock) - still not fixed, still the reason every licensed check here reads the TBD/TSD directly rather
  than trusting a boolean.
- No manufacturer MVHR unit, no heat-recovery efficiency, no summer bypass, no cooling/tempering, no 23°C
  MVHR supply setpoint - Iteration 1a states the topology and duty a real unit will have to meet;
  selecting one is Iteration 2.
- The legacy `InternalCondition.VentilationProfileName` / `ticV` double-count risk that a previous Codex
  round flagged is now closed on the SAM side by an explicit refusal in `PreparePartOIteration` (a served
  space carrying a Ventilation profile that actually resolves in the model's library refuses preparation
  rather than risking both `ticV` and the directional IZAM realizing the same air) - see
  [SAM-BIM/SAM#77](https://github.com/SAM-BIM/SAM/pull/77).

### Exact next step

Merged: SAM#77 then SAM_Tas#44, both CI-green, full regression suites green
(`SAM.Tests` 1479/1479, `SAM.Analytical.Tas.TM59.Tests` 649/649), licensed status disposed as above. Do not
start Iteration 2 (manufacturer MVHR selection) next - a fresh, genuinely per-dwelling-balanced fixture
would be a reasonable follow-up if a full multi-dwelling licensed run is ever wanted, but it is not a
blocker for anything currently planned.

---

## Previous session (2026-08-27, mass flow)
The accepted Iteration 1a recorded a defect it deliberately did not fix: **TAS reads an inter-zone air
movement's stored flow as a mass flow in kg/s, and SAM writes a volume flow in m3/s.** That is now closed
in this repo, and only in this repo.

### The evidence
Two independent sources, used together rather than one instead of the other:

- The **EDSL Building Simulator documentation** states the Inter-Zone Air Movement flow rate as a
  time-varying *mass flow rate*, and its Inter-Zone Air Movement table gives the unit explicitly as
  **kg/s**. It is the same document that states the balance rule the accepted work was built on.
- The **licensed file itself**: read back through Tas's own accessors, the profile
  `Modify.UpdateIZAMs` writes reports `units=kg/s` - TAS's own declaration about the field SAM was filling
  with cubic metres.

Nothing failed while this was wrong. The model balanced, simulated and produced a full year of results,
for a dwelling ventilated about 21% below its design. That is the whole reason it needs a test.

### The change
- **`Query.IZAMMassFlow_KgPerSecond` / `Query.IZAMVolumeFlow_M3PerSecond`** (new) with
  **`Query.IZAMAirDensity_KgPerM3`**, which is `SAM.Core.FluidProperty.Air.Density` = **1.210 kg/m3** -
  SAM's own authority, the value `Modify.AddAirMovementObjects` already writes as an air handling unit's
  density profile. A second constant was deliberately **not** minted to reach the 1.204 kg/m3 of dry air
  at 20 C and sea level: two competing densities for the same air is a worse defect than a slightly
  different one.
- **`Modify.UpdateIZAMProfile`** (new): the one seam where a SAM `SpaceAirMovement` becomes a TBD profile.
  All three write sites in `Modify.UpdateIZAMs` go through it, so **every** shape is converted - outside
  into the unit, unit into a room, room to room transfer, room back to the unit, and the unit's exhaust.
  One density across the whole graph, which is what keeps a network that balanced by volume balancing by
  mass exactly; a per-movement temperature-corrected density would unbalance every node, which is precisely
  what TAS refuses.

**Nothing in `SAM` changed.** `SpaceAirMovement.AirFlow`, the Approved Document F requirement and the
design terminal duties all stay volumetric. The conversion is an interoperability concern and lives at the
interoperability boundary.

**The legacy `Create.IZAM` / `Modify.UpdateIZAMsBySpaceParameter` route is NOT converted.** It builds a
movement from `SAM_IZAM_*` space parameters a modeller typed by hand, in whatever unit they meant;
rescaling those would silently change existing models. Only the Part O runtime realization, which knows its
own values are m3/s, is converted.

`SAM.Analytical.Tas.TM59.Tests`: **642 passed, 0 failed** (was 633, +9). `IZAMMassFlowTests` runs the real
`Modify.UpdateIZAMProfile` against the existing managed `FakeProfile` - no licence, no COM - and pins both
the conversion and the inequality that catches a regression back to writing m3/s.

### Licensed evidence
In the SAM repo: `SAM/documentation/PartO-TAS-VALIDATION.md` §"Iteration 1a / Base MVHR - the two
magnitude and scope corrections (2026-08-27)". Every IZAM in the re-run acceptance TBD reads back in kg/s,
converts back exactly to its l/s design duty, and every zone conserves **mass** to 5e-6 l/s. The
Iteration 1b OPEN/NIGHT regression is unchanged at **16 690**, as it must be: the natural ventilation
route writes no inter-zone air movement at all.

Still deliberately **not** fixed: `Modify.Simulate` returns true for a simulation TAS refused to run.

---

## Previous session (2026-08-27, the inter-zone air movement shape)
Iteration 1a's licensed annual simulation was refused by TAS with `Simulation Failed`. The cause is
**conservation**: TAS refuses a TBD in which any one zone's inter-zone air movements do not balance, and
balance over the building as a whole is not enough. Both facts are established by experiment against the
TAS-authored control files in `Tas Data\Sample Projects`, not from documentation alone - see
`SAM/documentation/PartO-TAS-VALIDATION.md` §"Iteration 1a / Base MVHR - the block resolved (2026-08-27)".

### What this repo contributes this session
`Modify.UpdateIZAMs` now writes an air handling unit's **outward** movements - the exhaust that takes the
extract air out of the building. `TBD.IIZAM` exposes a source zone, target zones and `fromOutside`, and no
outward flag of any kind; a late-bound probe against the live COM object confirms the Building Simulator's
"To Outside" field is not reachable through automation, so this is not an absence in the checked-in
interop. The representation is therefore **assigned to the zone, no source zone, `fromOutside = 0`** -
which is exactly the shape of the TAS-authored *"From Atrium to Outside"* movement in the shipped
`example.tbd`, and re-creating that movement through `Building.AddIZAM` keeps that file balanced and
simulating.

The `To`-endpoint change of `4f70f08d` **survived evaluation against that control** and is kept: a TBD
inter-zone air movement only ever moves air INTO the zones it is assigned to, so a room's extract has to be
a movement on the UNIT's zone sourced from the room.

Ruled out as causes, each by experiment: `profile.factor` versus `profile.value` (**both work** - TAS's own
files put the flow in `value`, SAM puts it in `factor`), `profile.units`, profile and movement names,
day-type coverage including `HDD`, and the unit's zone itself.

Two pre-existing defects found and deliberately **not** fixed here: TAS reads the stored flow as a mass
flow in kg/s while SAM writes m3/s, and `Modify.Simulate` returns true for a simulation TAS refused to run
- it only waits for the TSD to unlock, so every failed licensed run has reported success.

`SAM.Analytical.Tas.TM59.Tests`: **633 passed, 0 failed**. The Base MVHR fixture gained the internal
partition it always should have had: two rooms that share no separating element cannot move transfer air
between them, and the preparation is right to refuse such a model.

---

## Previous session (2026-08-26, Iteration 1b)

### What this repo contributes
`SAM.Analytical.Tas.TM59.Tests/PartONaturalVentilationWorkflowTests.cs` carries ONE naturally ventilated
dwelling from the production `Modify.PreparePartOIteration` to the three places the TAS side has to agree
with it - the exported ventilation type, the aperture control the TBD write is given, and the TM59
criterion - with **no TAS COM, no licence and no install**. `Building`/`Zone` are XML writers over
analytical objects, `Query.ApertureTypeDefinition` is the COM-free half of `Modify.SetApertureType`, and
`TMOverheatingCalculator` reads hourly series off a `Space`.

This session the fixture became **two cases**, parameterised by `OpeningRestriction`, so it builds exactly
what the licensed acceptance runs:

- **NV-OPEN** - `Unrestricted`. Reaches the aperture-definition write with the same function, factor and
  discharge coefficient as NV-NIGHT and **no** availability schedule, which is how "unrestricted" is
  represented.
- **NV-NIGHT** - `NightClosed` 08-23. Reaches it with `PartO_DayOpen_08_23` and all 24 values.

Both are prepared at `PartOIteration.BaseNaturalVentilation` (Iteration 1b) on the explicit
`PartOVentilationMode.NaturalVentilation` route - the preparation's settled route is **asserted**, not
inferred from the airflow answer - and `TheTwoCases_DifferOnlyInTheOpeningAvailability` pins the A/B
invariant: same zone, internal conditions compared field by field, no continuous mechanical supply or
extract on **either**, same opening geometry and TAS function, availability schedule as the one difference.

The fixture's internal condition still states `VentilationSystemTypeName = "MVRE"` on purpose, so every
assertion has a control showing the pre-scenario derivation answering "mechanical" for the same model. The
explicit route wins, or these tests would pass for the wrong reason.

`SAM.Analytical.Tas.TM59.Tests`: **624 passed, 0 failed** (was 620, +4).

### Licensed evidence
Lives in the SAM repo - `SAM/documentation/PartO-TAS-VALIDATION.md` section "Iteration 1b / Base Natural
Ventilation - licensed A/B acceptance (2026-08-26)". The two produced TBDs differ by exactly one line (the
extra `ApertureType` carrying the schedule); zero `"flow"` keys in either NV case's zone descriptions
against 8 in the MVRE control; and 16 690 of 78 840 hourly resultant temperatures differ, with the largest
delta - 0.674 K - on the one space whose window was restricted.

### One TAS-side limitation this recorded
`SAM.Analytical.Tas` has **no TBD write path for exhaust at all**.
`InternalConditionParameter.ExhaustAirFlow` is read in exactly one place in this repository -
`PartODiagnosticLog`, for reporting - and reaches no TBD field. Only the supply side travels
(`SupplyAirFlow*` -> `freshAirRate` / `ticV` factor, plus the `SAMZoneMetadata` decomposition in the zone
description). This is why Part O Iteration 1b models no wet-room intermittent extract runtime behaviour:
SAM has the Table 1.1 rate but no operating schedule, and TAS has neither. See
`SAM/documentation/PartO-ARCHITECTURE.md` section 6.

---

## Branch
`fix/tas-design-day-weather-authority` (off `sow/2026-Q3` at `74cb422a`, i.e. with **PR #40 merged**).
The first TBD generated for a NEW weather file sized on the PREVIOUS weather's design days. See
`SAM.Analytical.Tas/DESIGN_DAY_WEATHER_AUTHORITY.md`.

Previously: `fix/tas-ventilation-ticv-factor-growth` (off `sow/2026-Q3` at `d10bfac`, i.e. with **PR #39 merged**) - PR #40, merged 2026-08-24.
The ventilation `ticV` factor grew without bound across repeated TBD round trips. See
`SAM.Analytical.Tas/VENTILATION_TICV_ROUND_TRIP.md`.

Previously: `fix/tas-fromtbd-gbxml-aperture-roundtrip` (off `sow/2026-Q3` at `372518a6`, i.e. with **PR #38
merged**) - PR #39, merged 2026-08-24. Aperture identity across a FULL gbXML round trip -
`TBD -> FromTBD -> new SAM -> gbXML -> a NEW TBD`. See
`SAM.Analytical.Tas/APERTURE_ROUND_TRIP_IDENTITY.md`.

Previously: `feature/tas-profile-round-trip-hardening` (off `sow/2026-Q3` at `610696e9`, i.e. with **PR #37 merged** —
the profile definition reuse this work hardens). PR #36 (`UpdateIds` zone identity) and PR #37 (profile
definition reuse) are both merged; see the "Previously" sections below for their state at merge time.

The reusable-aperture programme is complete and merged on all three routes:
`feature/tas-aperture-hardening` was PR #35, merged 2026-08-23.
`feature/tas-aperture-definition-reuse-gbxml` was PR #34, merged 2026-08-23.
Stage 3 was `feature/tas-aperture-instance-identity` (PR #33, merged 2026-08-22).
Stage 3's S3-C1/S3-C2 were `sow/2026-Q3-instance-identity` (PR #32, merged 2026-08-21).

Stage 1 was `feature/tas-aperturetype-reuse` (PR #30, merged 2026-08-21).
Stage 2 was `feature/tas-aperture-definition-reuse` (PR #31, merged 2026-08-21).

## Last updated
2026-08-25 (occupancy sensible/latent gains decayed on every round trip - LICENSED).

**Found while validating PR #41.** A TM59 kitchen's occupancy gain shrank by a factor of four on every
generation of `Convert.ToSAM -> TogbXML -> WorkflowCalculator -> new TBD` - `ticOSG.factor` 2.0 -> 0.5 ->
0.125 -> 0.031 W/m2, unbounded. Root cause: the import read `profile.GetExtremeValue(true)`
(= `factor * max(values)`) where the export writes the magnitude as `profile.factor` and the schedule as
the profile's raw values, so `G(n+1) = G(n) * schedulePeak`. A fixed point only for a schedule normalised
to 1.0 - which is why lighting, equipment and infiltration in the same model never moved and this kitchen,
peaking at 0.25, did. What actually decayed was the OCCUPANCY (37.5 -> 150 -> 600 -> 2400 m2/person); the
authored 75 W/p sensible and 55 W/p latent were preserved throughout, which is why it hid for so long.

Fixed by `Query.GainMagnitude` (= the factor), used by all seven magnitude-carrying slots on both the TBD
and TIC import paths. `ticV` already did this (PR #40); this generalises the same rule. Licensed A/B over
two 3-generation chains: gains and design loads are now a fixed point from generation 0 onward, per-space
cooling on the affected zone stops drifting (-5.7% over three generations before, stable after), the
schedule shape is untouched and the profile library still holds 29 GUIDs / 27 names in every generation.
Full tables and the classification of every gain slot:
`SAM_Tas/SAM.Analytical.Tas/INTERNAL_GAIN_MAGNITUDE_AUTHORITY.md`.

**Update, same day, after re-checking the historical -28.9% drop against the now-corrected workflow (PR
#41 + this fix, PR #42 both applied).** That figure was measured on an EARLIER branch state, before either
fix existed. Re-ran the 3-generation chain against the branch's current HEAD on the SAME production
regression model the -28.9% figure came from (`SAM_daily/2026-08-05-PartO/SAM_zoningAM_v2.sam`, weather
`CIBSE Weather 2021.twd`, `Convert.ToSAM -> TogbXML -> WorkflowCalculator.Calculate`, sizing on):
`GAINP` lines are byte-identical P1 == P2 == P3, and total heating is stable to 1.5e-6 relative (3278.075 W
-> 3278.070 W -> 3278.071 W) across all three. This model's normal internal conditions are free-running
(cooling always sizes to 0), so it carries no cooling signal on its own; the earlier synthetic 24 C
cooling-setpoint chain in `INTERNAL_GAIN_MAGNITUDE_AUTHORITY.md` already showed cooling stable to 7e-6
relative post-fix on the same seed family. **No generation-to-generation load step reproduces on the
current workflow.** The exact historical contribution of the design-day-authority defect (PR #41) versus
the gain-magnitude defect (PR #42) to the original 28.9% figure has NOT been forensically isolated - both
were live bugs on the branch that measurement was taken from, and disentangling which fraction each one
contributed was not attempted. That specific attribution question is closed as moot rather than answered:
**3-generation design-load round-trip stability is solved on the current workflow**, and a fresh
investigation is warranted only if a future run of `sow/2026-Q3` reproduces a material generation-to-
generation load difference again.

2026-08-25 (design-day weather authority - HDD/CDD round trip investigated, fixed and licensed).

**Question asked.** With unchanged weather, do HDD/CDD and the design loads drift across generations? When
the weather changes A -> B, does the FIRST Weather-B TBD already use Weather-B design days?

**Answers, from a licensed 3-generation chain** on the 9-space TM59 residential model
(`Convert.ToSAM` -> `TogbXML` -> `WorkflowCalculator.Calculate`, sizing on; Weather A =
`cibseweather2005.twd` / Belfast TRY, Weather B = `CIBSE Weather 2021.twd` / Leeds_TRY; artifacts in
`C:/TasOut/dd2` before and `C:/TasOut/dd3` after):

1. **Unchanged weather: no drift.** A1/A2/A3 carry byte-identical design days (SHA-256 over all 24 hours x 7
   weather series) and heating loads within 1.3e-6. Only the design-day object GUIDs change.
2. **Weather changed: the defect was real.** `B1` was written with Weather B installed but **Weather A's**
   design days - `Belfast TRY ANN HTG`, year-day 363, flat -6.6 C, instead of `Leeds_TRY ANN HTG`, year-day
   67, flat -5.9 C. `B1` oversized heating by **+95.9 W (+2.93%)**, uniformly across every sized zone
   (ratio 1.0294 / 1.0294 / 1.0290), which is the design temperature difference and nothing else. `B2 == B3`.
3. **A worse case with no weather change at all.** A model authored outside TAS carries no design-day
   parameters, `AddDesignDays` was gated on them being non-null, and the first TBD got **no design days at
   all** - TAS sized every zone to **0 W**. That is the `A0` row.

**Root cause.** `WorkflowCalculator.Calculate` (and `Convert.ToTBD`) resolved the weather and the design days
from unrelated sources: `WorkflowSettings.WeatherData` won for the weather, the MODEL won for the design
days. But a model's design-day parameters are DERIVED, not authored - `Convert.ToSAM(path_TBD, ...)` computes
them from the weather it finds in the TBD it imports. So changing weather on the component while leaving
`coolingDesignDays_` / `heatingDesignDays_` unwired installed Weather B next to Weather A's design days, and
`tBDDocument.sizing(0)` sized on those.

**Fix.** `Query.DesignDays_Authoritative` states the rule once and both export paths call it: *a run that
states its own weather makes that weather authoritative over every weather-derived design day the caller did
not state outright.* Explicitly-passed design days (`WorkflowSettings.DesignDays_*`, the `ToTBD` arguments)
are engineering intent and still win; a run that states no weather leaves the model's design days alone; the
model is the fallback for a slot the authoritative weather cannot fill.

**Nothing is discarded unnecessarily.** With unchanged weather the re-derivation reproduces the imported
design days bit for bit: the post-fix `A1` TBD is byte-identical to the pre-fix one, every load digit
included. After the fix `A0..A3` and `B1..B3` are each a true fixed point, and `B1` lands on the value the
chain previously only reached at generation 2 (3278.07 W).

**Found, NOT fixed, separate seam:** occupancy sensible/latent gains decay by exactly the schedule's peak
value on every round trip. `Modify.Update(profile_TBD, profile, factor)` writes the per-area gain into
`profile_TBD.factor`; `Convert.ToSAM` reads it back as `GetExtremeValue(true)` = `factor x max(hourlyValues)`.
On `1 Bed Apt. Kitchen Occupancy` (schedule peaks at 0.25) `ticOSG.factor` went 0.5 -> 0.125 -> 0.03125 and
`ticOLG.factor` 0.366667 -> 0.091667 -> 0.022917 across three generations - unbounded, and the same class as
PR #40's `ticV` growth in a different slot. It does NOT match the signature of the logged -28.9% cooling drop
(that steps once and then holds; this decays every generation), so treat them as two separate open items.

**Tests.** `SAM.Analytical.Tas.TM59.Tests` **588/588** Debug and Release (+13: `DesignDayWeatherAuthorityTests`,
COM-free, synthetic weather years). `SAM.Analytical.Tas.Benchmark.Tests` **16/16** Debug and Release.
`SAM_Tas.sln` builds clean in both configurations (only the pre-existing MSB3270/MSB3277 and XML-doc
warnings). `git diff --check` clean.

2026-08-25 (ventilation ticV round trip REDESIGNED around requirement-vs-realisation, licensed and green).
Earlier in that programme: 2026-08-24 (found, not yet investigated: a ~29% cooling-load drop between
generation 1 and 2).

While validating the ventilation fix above, ran heating/cooling design loads (`Modify.UpdateDesignLoads`, the
same call `WorkflowCalculator` makes after sizing) across a 3-generation chain built with BOTH this session's
fixes applied (`RA0.tbd` -> `LC1` -> `LC2` -> `LC3`, `Convert.ToSAM` -> `TogbXML` -> `WorkflowCalculator`,
sizing on, `_importUnused_`/`_importSurfaceShades_` both false):

| generation | heating (W) | cooling (W) |
|---|---|---|
| RA0 (seed) | 11,037 | 115,192 |
| LC1 | 11,289 (+2.3%) | 115,168 (~same) |
| **LC2** | 11,726 (+3.9%) | **81,883 (-28.9%)** |
| LC3 | 11,726 (same) | 81,883 (same) |

`ticV.factor`/`freshAirRate` are RULED OUT as the cause - confirmed identical (2.44/1.72 Studio/Bedroom,
8.0 l/s/p) across all three generations via direct COM dump. Cooling drops sharply exactly once, between
generation 1 and 2, then reaches a fixed point (LC2 == LC3 exactly) - the same signature as both bugs fixed
this session (stable gen0->gen1, a jump at gen1->gen2, then stable). A ~29% cooling swing with no ventilation
change points at solar gain: glazing g-value/SHGC, shading, or aperture-type control are the leading
suspects, not yet narrowed down.

**Not yet investigated further - logged per explicit instruction to stop here.** Whoever picks this up next
should reproduce with the `P39` harness pattern used for the other two fixes (`Convert.ToSAM` ->
`TogbXML` -> `WorkflowCalculator.Calculate`, `Modify.UpdateDesignLoads` to read back loads), then diff the
SAM-side `ApertureConstruction`/glazing parameters and any shading/blind state between generation 1 and 2 to
find what actually changes at that seam.

2026-08-25 (ventilation ticV factor grew on every round trip - REDESIGNED and LICENSED).

**Reported from a real Grasshopper run:** the ventilation profile VALUES were identical across generations
but the FACTOR kept climbing, without bound.

**The distinction this now rests on**, clarified with the SAM design author and against the TAS 9.5.7 manual:
SAM ventilation properties describe an engineering airflow REQUIREMENT (four simultaneous bases, summed by
`Query.CalculatedSupplyAirFlow`). They do NOT prescribe how TAS realises it - that may be a TBD Internal
Condition Ventilation profile, an IZAM, Tas Systems, or another explicit workflow. `InternalGain.freshAirRate`
is TAS's Outside Air field (Part L / EPC / Tas Systems) and does **not** itself supply the thermal zone in the
TSD simulation; `ticV.factor` is Building Simulator mechanical ventilation and exists only where a Ventilation
profile has been assigned.

**Root cause.** The export wrote the whole summed total into `ticV.factor`; the import read that total back
into the single `SupplyAirChangesPerHour` BASIS. So last generation's output became an ingredient of this
generation's sum, re-adding the other bases once per generation. Zones with no `AreaPerPerson` have a NaN
per-person term, were never inflated, and sat at a stable 1.00 ACH - the fingerprint that isolated it.

**Fix (three parts).**
1. `Query.VentilationAirChangesPerHour(Space)` is the FULL calculated requirement over the volume - every
   basis, per-person included. Where a Ventilation profile has chosen Building Simulator ventilation as the
   realisation, it must deliver all of the requirement. `freshAirRate` holding the same per-person rate is not
   a double count, because it does not supply the zone in the TSD.
2. `SAMZoneMetadata` - a versioned SAM-only section of `TBD.zone.description` (beside the existing `[Id]` /
   `[LevelName]`) carrying the four authored bases plus a flag for whether a Ventilation profile realised
   them, and a fingerprint of the native TAS fields as the export left them. One parser/writer owns the whole
   string; it rewrites what it manages, PRESERVES unrelated content (previously overwritten), is deterministic
   and invariant-culture, stores no derived geometry, and is extensible for exhaust later.
3. `Modify.RestoreVentilationRequirement` puts the authored bases back on import in place of what was inferred
   from the total - REMOVING a basis the export did not record, not just writing the ones it did - and, where
   the metadata says no profile was assigned, removes the `VentilationProfileName` the native import writes
   from the mere presence of a `ticV` slot. Stale metadata (native fields no longer matching) is refused whole,
   the native import stands, and a note is stamped on the space (`"SAM Zone Metadata Note"`).

**The `profile != null` gate in `Modify.UpdateInternalCondition` is unchanged and load-bearing**: SAM airflow
data never activates a TBD Ventilation profile by itself. The explicit seam remains
`SAMAnalytical.UpdateVentilationProfile`. Part F (`ApplyPartFVentilationRates`) and Part O
(`PreparePartOIteration`) are untouched and neither assigns a Ventilation profile.

**Supersedes the first attempt on this branch** (commit `140acb4c`), which SUBTRACTED the per-person term from
the factor on the assumption that `freshAirRate` supplies it to the Building Simulator. That made the round
trip stable by changing the physical total, and is retracted.

**Licensed acceptance** on the 9-zone residential fixture, `Convert.ToSAM` -> `TogbXML` ->
`WorkflowCalculator.Calculate` (sizing on), artifacts in `C:/TasOut/v40`:
- *Chain B, explicit Ventilation profile, 3 generations.* Authored `ach=1.0` + `flowPerPerson=0.008`.
  `ticV.factor` `Bedroom 2_3` **1.137143 -> 1.137143 -> 1.137143**, `Studio 1_0` / `Living Kitchen_4`
  **1.192** throughout, `Kitchen_7` **1.048**, `Corridor_1` / `Bathroom_2` / `Ensuite_5` / `Ensuite_8`
  **1.000**. `freshAirRate` 8.0 l/s/p throughout. Each import restores `ach=1.0` and `flowPerPerson=0.008` -
  the AUTHORED bases, not the total. Baseline on the same starting model: **1.137143 -> 1.274286 -> 1.411...**
- *Chain C, requirement data with NO Ventilation profile, 2 generations.* `flowPerArea=0.0004`,
  `flowPerPerson=0.008`, `ach=1.0`, profile removed. `ticV.factor` never written (holds the T3D default of 1),
  all three bases restored unchanged, `ventProfileName` ABSENT after import so nothing is activated,
  `freshAirRate` still 8.0, calculated requirement identical across generations (1.497143 ACH on
  `Bedroom 2_3`).
- *Stale metadata on a real file.* `TBD1.tbd` re-opened, `freshAirRate` set to 12 on all 18 internal
  conditions, re-imported: section refused, native import used in full (`flowPerPerson=0.012`,
  `ach=1.137143`), note visible on every space.
- *Programme invariants.* `40 aperture part(s) considered; 40 rebound` in every generation of both chains.
  Profile reuse, Part F, Part O unchanged; no design-day change included.

**Tests.** `SAM.Analytical.Tas.TM59.Tests` **575/575** Debug and Release (+21 net: new
`VentilationRequirementMetadataTests`, `VentilationAirflowMagnitudeTests` per-person block rewritten to the
corrected semantics). `SAM.Analytical.Tas.Benchmark.Tests` **16/16** Debug and Release. `SAM_Tas.sln` builds
clean in both configurations (only the pre-existing MSB3270/MSB3277 and XML-doc warnings). The suite contains
its own control - `WithoutTheMetadata_TheSameChainCompounds` shows 2.44 -> 3.16 -> 3.88 with the mechanism
removed - so disabling the fix fails the tests. Detail in
`SAM.Analytical.Tas/VENTILATION_TICV_ROUND_TRIP.md`.

**Files changed.** New: `Classes/SAMZoneMetadata.cs`, `Modify/RestoreVentilationRequirement.cs`,
`Create/ZoneMetadata.cs`, `Query/PrimaryInternalConditionIndex.cs`,
`SAM.Analytical.Tas.TM59.Tests/VentilationRequirementMetadataTests.cs`. Modified:
`Query/VentilationAirChangesPerHour.cs`, `Modify/UpdateZone.cs`, `Modify/UpdateInternalCondition.cs`
(comment only), `Convert/ToSAM/Space.cs`, `Convert/ToSAM/AdjacencyCluster.cs`,
`SAM.Analytical.Tas.TM59.Tests/VentilationAirflowMagnitudeTests.cs`,
`SAM.Analytical.Tas/VENTILATION_TICV_ROUND_TRIP.md`.

Previously: 2026-08-24 (aperture round trip, second pass - the REAL Grasshopper failure, reproduced and fixed - LICENSED).

**The previous entry's caveat was wrong and is retracted.** It said the reported symptom "did not reproduce"
and pointed at a Grasshopper install running older DLLs. The DLL was current - measured, not assumed: the
deployed `SAM.Analytical.Tas.dll` was byte-identical (SHA-256 `D70C6D7F…`) to the repository build and
contained the `RemoveApertureTasIdentity` change. The symptom reproduces on every run. The earlier harness
missed it because it left **one `SAMAnalytical.FromTBD` input at its default**: `_importUnused_`.

**The chain.** With `_importUnused_` on, `Convert.ToSAM` also reconstructs aperture constructions no element
references. The previous generation's TBD holds exactly one such leftover, and it comes back as an
`ApertureConstruction` with frame layers and **no pane layers**; `TogbXML` writes it as a second
`<WindowType>` with a `<Frame>` and no `<Glaze>`. An A/B on the two exported gbXMLs - identical but for that
one element and an unused opaque `<Construction>` - shows it flips TAS's own gbXML import from `GLAZING`
(`BEType` 12) panes to **`DOORELEMENT` (`BEType` 14)** panes, for every opening in the model at once.

**The defect.** Three places read that element and two disagreed with the import. `Convert.ToSAM` uses
`Query.AperturePart_BuildingElementType` and calls a door leaf a PANE; `Query.Match` - how `Modify.UpdateIds`
decides which half of an aperture a surface is - used `Query.AperturePart(int)` and called it a FRAME; and
the sweep's `ApertureBuildingElementUsage.IsAperture` did not recognise it as an aperture element at all.
So `UpdateIds` collected BOTH of a window's surfaces into its frame set and none into its pane set, and bound
the frame to the pane's element. `UpdateApertureDefinitions` then skipped every pane for want of a binding
and `Query.ApertureRebindKeys` refused every frame - correctly, about state that only existed because of the
misreading. `40 considered / 0 rebound`, and TBD2 kept **40 per-aperture GUID-named building elements**
instead of 3. That is the reported symptom, element-for-element against the user's own `Flat1-rerun.tbd`.

**The fix.** One reading in one place: `Query.AperturePart_BEType(int)` is added beside
`Query.AperturePart_BuildingElementType` (which now delegates to it) as the single definition of which half
of an opening a TBD element is - `GLAZING`, `ROOFLIGHT`, `DOORELEMENT` are panes, `FRAMEELEMENT` is a frame,
everything else refuses. `Query.Match` reads it instead of `Query.AperturePart(int)`, and
`ApertureBuildingElementUsage.IsAperture` asks it too so the sweep recognises exactly what the reader does -
without which the emptied door elements survived the rebind. `Query.AperturePart(int)` stays as the
*write*-side helper, unchanged and now documented as such. **No refusal was relaxed**; physical identity is
still `{ZoneGuid, SurfaceNumber}`, a `BuildingElementGuid` is still only a definition binding, and the
sweep's holds-no-surface and not-canonical gates are untouched.

**Licensed acceptance** on `Flat1.tbd` *as the failing Grasshopper run produced it*, through
`Convert.ToSAM` -> `TogbXML` -> `WorkflowCalculator.Calculate` with the component's own settings, all four
`_importUnused_` x `_importSurfaceShades_` combinations, two generations each: every generation
`40 considered / 40 rebound`, zero refusals, `zones=9 surfaces=110 surfaces_pane=20 surfaces_frame=20
elements=8 apertureElements=3`, TBD2 structurally identical to TBD3. Before the fix the two `_importUnused_`
rows gave `20 considered / 0 rebound` and 45 elements. Tests: `SAM.Analytical.Tas.TM59.Tests` **554/554**
Debug and Release (+18 new cases in `ApertureDoorTypedPartTests.cs`), `SAM.Analytical.Tas.Benchmark.Tests`
**16/16** both; two mutation checks bite (reverting the door reading fails 3, reverting the sweep test fails
2). Detail in `SAM.Analytical.Tas/APERTURE_ROUND_TRIP_IDENTITY.md`.

**Review fix, before merge.** Copilot's PR review caught a second instance of the same shape of bug in
`Modify.UpdateIds`'s clearing loop: an `AdjacencyCluster` can hold one aperture as both a panel-held object
and a standalone cluster object, real models carry both, and `GetAperture(guid)`/`GetObject<Aperture>(guid)`
return the standalone one - not the panel one that gets restamped. The clearing loop cleared only the
panel-held copy, so a part the refresh could not re-match left its **standalone** copy still carrying the
previous TBD's binding - reachable through `GetAperture`/`GetObject<Aperture>` even though the panel copy now
correctly read unstamped. `UpdateApertureDefinitions`'s own successful restamp (`RestampApertureBinding`)
already updates both shapes for this exact reason; the clearing pass now does too. Re-verified licensed with
`_importSurfaceShades_` on (the mode that creates the standalone copies): still `40 considered / 40 rebound`,
zero refusals, both generations. Full test suite re-run clean (554/554 + 16/16, both configs).

Previously: 2026-08-24 (aperture identity across a full gbXML round trip - LICENSED). **`Modify.UpdateIds` cleared every
aperture's physical stamps unconditionally but never cleared `Pane`/`FrameBuildingElementGuid` - which in
fact was never cleared anywhere.** A part the refresh could not re-match therefore carried the PREVIOUS TBD's
definition binding forward as apparent current state, so `Modify.UpdateApertureDefinitions` counted it as
bound and `Query.ApertureRebindKeys` refused it - the mechanism behind both reported refusal classes and a
`40 considered / 0 rebound` outcome. Fixed by one new mutator, `Modify.RemoveApertureTasIdentity`, which
clears stamps and bindings together, called from the one unconditional clearing pass in `UpdateIds`. **No
refusal was relaxed.** Licensed: A0 -> TBD1 -> FromTBD -> A1 -> TBD2 -> FromTBD -> A2 -> TBD3 on the 9-zone /
20-aperture Flat1 fixture - all three TBDs hold 40 aperture parts on **3** aperture building elements (one
shared frame, two panes), `40 rebound` with zero refusals in every generation, and every simulation-effective
aperture field identical across generations. **Caveat: the originally reported symptom did not reproduce at
base `372518a6`** - the chain already reached the fixed point there; the fix is proven by a mutation check
instead (disabling the binding clear fails three of the new tests, one with the exact reported message).
Detail and limitations in `SAM.Analytical.Tas/APERTURE_ROUND_TRIP_IDENTITY.md`. **That caveat is retracted -
see the entry above: the symptom does reproduce, with `_importUnused_` on, and the harness was the thing at
fault, not the deployment.**

Previously: 2026-08-24 (profile round-trip hardening - LICENSED, and corrected) - **The two leftovers PR #37 pinned as
baseline are fixed, and the licensed run caught a third defect the fix itself activated: the imported
ventilation rate was inflated by 3600/volume.** Full detail in
`SAM_Tas/SAM.Analytical.Tas/PROFILE_ROUND_TRIP_HARDENING.md`.

**1. HDD naming (export-side).** `Modify/UpdateInternalCondition_HDD` stamped `profile.Name` onto the
flattened single-value `ticValueProfile`s it writes for the HDD sizing condition's `ticI` and `ticLL`
slots - one name carrying two differently-valued definitions, which the next import legitimately
discriminated, accreting one `_<hash>` suffix per SAM → TAS → SAM generation on exactly those two
categories (the licensed "2 of 20 names grow" residual). The flattened profiles are now named after
themselves: `profile.Name + " - HDD"` (the HDD condition's own naming convention). Import unchanged - its
discrimination stays as the safety net for genuinely same-named TAS-authored input.

**2. Ventilation ticV (import-side).** An unfinished WIP from `13c4284c` (2023): the import wrote
`VentilationProfileName` but `ticV` was never in the library emitter's slot set, so the reference always
dangled and the export silently kept TAS defaults for imported models' ventilation. `ticV` is now
collected like every other internal-gain slot (`Query.ProfileReuseIndex.ProfileSlots_InternalGain` +
the legacy `Convert.ToSAM_Profiles` mirror), and the reference is routed through the same index helper
(`Convert.ToSAM(TBD.InternalCondition, …)`). This is the one intended simulation-effective change vs the
pre-fix baseline: an imported model's ventilation schedule now round-trips instead of dropping to TAS
defaults, so licensed acceptance compares ticV fields against the SOURCE TBD, not only baseline-vs-feature.

**Files changed:** `SAM_Tas/SAM.Analytical.Tas/Modify/UpdateInternalCondition_HDD.cs`,
`SAM_Tas/SAM.Analytical.Tas/Query/ProfileReuseIndex.cs`,
`SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/Profiles.cs`,
`SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/InternalCondition.cs`,
`SAM_Tas/SAM.Analytical.Tas/Modify/UpdateInternalConditionTemplate.cs`,
`SAM_Tas/SAM.Analytical.Tas/Query/ProfileName.cs` (shared `ProfileName_HDD` naming rule),
`SAM_Tas/SAM.Analytical.Tas/Query/ProfileReuseIndex.cs`,
`SAM_Tas/SAM.Analytical.Tas/Classes/ProfileReuseIndex.cs` (`Reserve`),
`SAM_Tas/SAM.Analytical.Tas.TM59.Tests/ProfileDefinitionReuseTests.cs` (+1 net: the baseline-pin
`References_VentilationSlotIsNotCollected_…` became
`References_VentilationSlotIsCollected_SoItsReferenceResolves`; new
`Naming_HDDFlattenedProfilesWithTheirOwnNames_ReachAStableFixedPoint`),
`SAM_Tas/SAM.Analytical.Tas.TM59.Tests/VentilationAirflowMagnitudeTests.cs` (new, 11 tests - the COM-free
guard that would have caught the magnitude failure before TAS was run),
`SAM_Tas/SAM.Analytical.Tas/PROFILE_ROUND_TRIP_HARDENING.md` (new handover doc),
`SAM_Tas/SAM.Analytical.Tas/PROFILE_DEFINITION_REUSE.md` (two statements reworded where recorded), this
file.

**Validation:** `SAM_Tas.sln` builds 0 errors Debug AND Release (Framework MSBuild; pre-existing
MSB3270/MSB3277 warnings only). `SAM.Analytical.Tas.TM59.Tests`: **521/521 Debug and Release**
(497 inherited, +1 net, +17 ventilation-magnitude, +6 zero-length/reservation/slot-key guards). `SAM.Analytical.Tas.Benchmark.Tests`: **16/16** both. NOTE: the sibling dependency outputs were stale/missing on this machine and had
to be rebuilt first (`SAM_gbXML` Core+Analytical, all four `SAM_SolarCalculator` projects, three
`SAM_Systems` projects, `SAM_Validation/SAM.Analytical.Benchmark`) - build outputs only, no source changes
in those repos.

**Deferred (documented in the handover doc, not forgotten):** function-profile semantics end to end
(import of `profile.function`, `Core.Tas.Query.Values` reading the hourly/yearly function *variants*, the
zero-count re-export writing 24 NaNs, the inverted `double.IsNaN` guards on
`VentilationFunctionSetback`/`VentilationFunctionFactor` in `UpdateInternalCondition`, and the template
path never writing function strings). Neither PR #37 licensed model exercises a function profile; SAM has
no home for a function string outside Lighting/Ventilation. Separate task.

**3. Ventilation MAGNITUDE (the licensed correction).** The first licensed run of this branch failed.
Collecting `ticV` woke a dormant unit defect: `profile.GetExtremeValue(true)` on a `ticV` slot is a peak
**air change rate**, but the import stored it in `InternalConditionParameter.SupplyAirFlow`, declared
`[m3/s]`. `Query.CalculatedSupplyAirFlow` read it as m³/s and the export's `/ volume * 3600` inflated it
by 3600/volume. Neither licensed model could show this (both carry `ticV = factor 1.0, value 0.0`, so
their round trip is simulation-inert); an authored **2.0 ACH** source profile made it a
**40.8 ACH** round trip - a peak hourly heating error of 69,950 W against the source, 19.3× worse than the
baseline that dropped ventilation altogether. It was harmless for as long as the reference dangled,
because the export could not resolve the profile and never wrote the factor.

The correction is unit-only, at two sites: the import writes
`InternalConditionParameter.SupplyAirChangesPerHour` (`[ACH]`, whose `rate × volume / 3600` the export's
conversion exactly inverts) in **both** `Convert/ToSAM/InternalCondition.cs` overloads (TBD and TIC shared
the mis-mapping); and `Modify/UpdateInternalConditionTemplate.cs` prefers that parameter for its `ticV`
factor, falling back to `SupplyAirFlow` so SAM-authored templates keep the factor they have always been
given. `CalculatedSupplyAirFlow` itself is untouched - the rate is routed to the basis that already
inverts correctly instead of being compensated for downstream. Corrected result: **2.0 ACH → 2.0 ACH**,
`infVentGain` within 0.003 %, peak heating error 69,950 W → **326 W**.

**Licensed acceptance (2026-08-24).** One-DLL-swap isolation (67 files, exactly 1 differing) across three
builds - baseline `610696e9`, first attempt `e2e88ca4`, corrected head - each proving its own identity by
reflecting its production slot table, the corrected build reproducing byte-identically under `-t:Rebuild`.
Unresolved ventilation references **4 → 0** (ModelA) and **36 → 0** (TM59); library **20 → 21** and
**30 → 31** with every ticV slot deduped onto one definition and counts stable across generations; HDD
names reach a fixed point (generation 2 == 3 == 4 byte-identical, where the baseline accretes one
`_<hash>` per generation and never converges); non-ticV simulation-effective fields **0 differences** in
792 and 5346; both real models **0 differences** against baseline in 227,760 and 1,024,920 simulated
values, before and after the correction. Zone-GUID churn confirmed as noise by a same-DLL control.
Also measured directly: TAS's `internalGain.freshAirRate` is **inert** in a TBD simulation (40 vs
0 l/s/p → 0 differences in 227,760 values), which is why a source carrying both an ACH schedule and a
dormant per-person rate still round-trips to their additive sum - established export design, recorded but
not changed.

**4. Zero-length ticV guard (Codex P2).** Collecting `ticV` also gave zero-length TAS **function**
profiles a resolvable library entry for the first time: `Core.Tas.Query.Values` has no case for
`ticFunctionProfile`, so it flattens to zero values, PR #37's exclusion branch still emits a legacy-named
library entry, and `VentilationProfileName` then resolved to it - after which `Modify.Update`'s dead
`Count == -1` guard let a `Count == 0` profile fall into the `Count <= 24` branch and overwrite the function
profile with 24 hourly values. New `Query.IsCollectableSlot(int, IEnumerable<double>)` (true for every slot
except `ticV`, and for `ticV` only when its values are non-empty) is consulted by both collectors, so a
zero-length `ticV` is registered nowhere, its reference falls back to the legacy name and dangles exactly as
it did before PR #38 - the safe deferred behaviour. Deliberately `ticV`-scoped; the other eleven slots keep
PR #37's treatment, and no function support is attempted. Four COM-free tests pin it. Normal `ticV` is
unaffected - confirmed licensed as byte-identical to the accepted build (792 non-ticV + 56 ticV fields,
0 differences) with the 2.0 ACH oracle intact, so the full A/B was not rerun.

**5. Review round (Codex + Copilot).** Three further findings, all addressed. (a) The magnitude fix stored
`GetExtremeValue(true)` = `factor * max(values)`; because `Modify.Update` re-applies the raw values on top of
whatever basis it is given, that scaled the schedule twice (`factor * max^2`) - invisible for a profile
normalised to a peak of 1, which is what the first authored oracle used. The import now stores
`profile_TBD.factor`; a non-normalised source (factor 2.0, peak 0.5 = 1.0 ACH) round-tripped as 0.5 ACH
before and **1.0 ACH exactly** after, with the normalised 2.0 ACH oracle unchanged and TM59 source-factor
agreement improving 26/54 -> 44/54. (b) Skipping a zero-length `ticV` left its legacy name unclaimed, so
`Resolve` could hand that same string to an unrelated canonical definition and turn the intended dangling
reference into a live one; the skip path now calls the new `ProfileReuseIndex.Reserve(category, name)` -
a claim with no definition, no library entry and no answerable slot - and `Resolve` seeds its claim set from
it. (c) The legacy `Convert.ToSAM_Profiles` mirror repeated the twelve slots by hand; both collectors now
`foreach` over the shared slot tables behind the shared `Query.IsCollectableSlot` gate, so they cannot drift
and one assertion pins both. All three verified licensed as behaviour-neutral on the real models (0
differences across 792/5346 non-ticV fields, ModelA re-simulated at 0/227,760, library 21/31, 0 unresolved).
**6. Second review pass (Codex).** The `Reserve` fix in item 5(b) closes a coincidental STRING collision
between two different internal conditions, but Codex found it does not close a slot-KEY collision: two TBD
internal conditions can share the exact same name (a duplicate space name, a generic template) while
disagreeing on `ticV`, and `Reserve` never touches `definitionsBySlot`/`excludedNamesBySlot`. `Register`
gained an `bool suppressLibraryEntry` parameter so a skipped `ticV` still goes through the same ambiguity
tracking every other excluded slot already uses - only the final library-emission step is skipped. Verified
by first reverting to the Reserve-only behaviour and confirming a new test (`IC name = "Duplicate"`, one
`ticV` zero-length, one ordinary) genuinely failed - `GetProfileName("Duplicate", ticV)` answered the
ordinary profile's name instead of null - then restoring the fix and confirming it passes. 521/521 tests
Debug and Release; no full licensed A/B rerun (the cheap 2.0 ACH oracle and both real models' import
integrity were re-checked and are unchanged).

A fourth finding - a source declaring both an ACH schedule and a TAS-inert `freshAirRate` round-tripping to
their additive sum - is answered in the handover doc rather than changed: it is the established export design
and narrowing it would drop ventilation for native SAM models.

**Recommended next step:** [PR #38](https://github.com/SAM-BIM/SAM_Tas/pull/38) is OPEN against
`sow/2026-Q3` with the licensed gate **run and passed** after the magnitude correction and the review round. Remaining: human
review, then merge. Merging remains a human call - it was not done here.

---

## Previously
2026-08-23 - **PR #37 (profile definition reuse) merged at `610696e9`.** The section below is its state
at merge time; its two pinned leftovers (name growth, dangling ventilation reference) are what the
current branch fixes.

## Previously (PR #37, at merge time)
Branch `feature/tas-profile-definition-reuse` (off `sow/2026-Q3` at `2950b27c`, i.e. after PR #35 merged the
aperture hardening fixes). **PR #36 (`fix/tas-updateids-gbxml-zone-identity`) has since been merged into
`sow/2026-Q3` at `03f9757` and is merged into this branch**, so the branch now carries it; the two touch
disjoint code (PR #36: `UpdateIds`/`Match`/zone identity; this branch: profile definitions).
Stage 1 complete; **licensed A/B PASSED - see "Last updated". Final review finding fixed (see
"Post-review fix" below). PR #37 OPEN against `sow/2026-Q3`, Copilot review comments addressed (see
"Post-review fix (2)" below). Not yet merged.**

### PR #37 last entry
2026-08-23 (later still) - **Post-review fix (2): addressed the GitHub Copilot automated review on
[PR #37](https://github.com/SAM-BIM/SAM_Tas/pull/37).** Codex's review hit its usage limit and left no
comments; nothing from it to address. Six real findings, no behavioural or reusable-profile-dedup change:

**Binary compatibility (4 findings, all the same shape).** `InternalCondition.cs`, `Space.cs`,
`AdjacencyCluster.cs` and `AddUnusedInternalConditions.cs` each added the new `ProfileReuseIndex`
parameter as an *optional* parameter on the EXISTING public method, rather than as a genuinely new
overload. An optional parameter is a compile-time convenience only - it still changes the method's
CLR metadata arity, so a caller compiled against the previous signature (e.g. `ToSAM(TBD.InternalCondition, double)`)
throws `MissingMethodException` at runtime against the new DLL, and old-arity method-group conversions
stop compiling. This project already has an established, deliberate fix for exactly this shape -
`AnalyticalModel.ToSAM(string, bool)` forwards to `ToSAM(string, bool, bool)` - and Copilot correctly
spotted that this branch's four new call sites did not follow it. Fixed as a mechanical split: each
method's previous exact signature is now a separate forwarding overload (calling the new one with
`null`), and the indexed body moved to a new overload with the extra parameter non-optional. No
internal call site needed to change - they all already pass every parameter explicitly. Confirmed via
`git show <merge-base>:<file>` that all four really did have a narrower public signature before this
branch, so none of these was a pre-existing false positive.

**`ProfileReuseIndex.Profiles` returned excluded profiles before `Resolve()`.** The property's own doc
says "Empty until `Resolve` has run"; the getter unconditionally appended `excludedProfiles` regardless
of resolution state. The one production caller (`ProfileLibrary.cs`) already guards on `.Resolved`
first, so this was latent rather than a live bug, but it violated its own contract for any future or
test caller that trusted the doc. Fixed: the getter now returns empty before `Resolve`, matching the
doc, with no change to post-`Resolve` behaviour (verified no test reads `.Profiles` before calling
`Resolve()`).

**Stale "no second COM read" doc claim** in `Query/ProfileReuseIndex.cs`. The doc said the slot lookup
never re-reads TAS, but `Convert.ToSAM.ProfileName`'s ambiguous-slot fallback calls
`Core.Tas.Query.Values(profile_TBD)` again - confirmed by reading that fallback path. Fixed:
doc now states the guarantee applies to building the index, and separately documents the one
fallback exception and why it is still correct, not just cheap.

**Stale test count** in `PROFILE_DEFINITION_REUSE.md` ("27 COM-free tests"): the zero-length
ambiguity fix added two more, actual count is 29 (`grep -c '\[Test\]'` confirms). Fixed.

**Stale branch-status prose** in this file (this section and "Next step" above): both still said
"PR not opened" / "then open ONE PR", which stopped being true the moment PR #37 was opened. Fixed to
name the open PR and the actual remaining step (review, then merge).

**Files changed:** `InternalCondition.cs`, `Space.cs`, `AdjacencyCluster.cs`,
`AddUnusedInternalConditions.cs` (binary-compat overload split), `Classes/ProfileReuseIndex.cs`
(`Profiles` getter), `Query/ProfileReuseIndex.cs` (doc), `PROFILE_DEFINITION_REUSE.md` (stale count),
`PROJECT_PROGRESS.md` (this entry and the branch-status lines above).

**Validation:** `SAM.Analytical.Tas` rebuilt with VS Framework MSBuild (0 errors).
`SAM.Analytical.Tas.TM59.Tests` **497 passed / 0 failed**, unchanged - none of these six findings
touch a code path any existing or new test exercises differently; the binary-compat split is
metadata-only (same runtime behaviour, same call graph, since every internal call site already passed
every parameter explicitly) and was checked by full rebuild + full test re-run rather than by
reasoning alone. `SAM.Analytical.Tas.Benchmark.Tests` **16/16**.

**Licensed A/B NOT rerun, deliberately - same reasoning as the first post-review fix**: none of these
six findings touch reusable-profile dedup, canonical naming, or any TAS-simulation-effective behaviour;
four are pure API-surface/binary-compatibility fixes, one is a defensive contract fix on a path no
production caller reaches, and two are documentation-only.

---

2026-08-23 (later) - **Post-review fix: the zero-length exclusion path now marks a contested slot key
ambiguous, as the reusable path already did.**

**The defect.** In `ProfileReuseIndex.Register`, the `!profileDefinition.IsReusable` branch wrote the
excluded legacy name into `excludedNamesBySlot` first-wins. A slot key is
`(internal condition name, slot)` and a name is not an identity, so two TBD internal conditions can share
a name, share a slot, and still hold **different** zero-length (TAS function) profiles. Legacy import
writes `Duplicate [Daylight]` for one and `Duplicate [Dimmer]` for the other; the first-wins map answered
`Duplicate [Daylight]` for both. Because that name does exist in the library, the result was a **silent
misreference**, not a dangling reference - the harder of the two to notice. Low severity: it needs
duplicate TBD internal-condition names AND differing function profiles on the same slot.

**The fix** (narrow, and mirrors the reusable branch exactly): same key + same excluded name keeps the
mapping; same key + a different excluded name calls `MarkAmbiguous(key)`; once ambiguous the slot
fast-path answers nothing, permanently. The library entries themselves are untouched - both are still
added and deduped by `category::name` - so both conditions fall through
`Convert.ToSAM.ProfileName`'s chain (slot -> definitional -> legacy) to their own legacy name, which the
library carries. **The reusable path, dedup, canonical naming and `DefinitionCount` are all unchanged**;
the only behavioural delta is the same-key/different-excluded-name case, which previously answered wrongly
and now answers nothing.

**Files changed:** `SAM_Tas/SAM.Analytical.Tas/Classes/ProfileReuseIndex.cs` (the branch, plus the field
comment that still claimed first-wins); `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/ProfileDefinitionReuseTests.cs`
(+2 tests); `SAM_Tas/SAM.Analytical.Tas/PROFILE_DEFINITION_REUSE.md` (the ambiguity rule now lists all
three cases).

**Validation:** `SAM.Analytical.Tas` rebuilt with VS Framework MSBuild (0 errors; the test project
references `build/SAM.Analytical.Tas.dll`, not a `ProjectReference`, so this rebuild is required for the
tests to see the change). `SAM.Analytical.Tas.TM59.Tests` **497 passed / 0 failed** (495 previous + 2 new).
The new `References_SlotThatIsZeroLengthOnBothConditionsUnderDifferentNames_AnswersNothing` was confirmed
to FAIL against the pre-fix DLL with `Expected: null / But was: "Duplicate [Daylight]"`, so it pins the
defect rather than merely passing; `References_SlotRegisteredTwiceWithTheSameZeroLengthName_KeepsAnswering`
pins the agreement half so the fix cannot over-trigger.

**Licensed A/B NOT rerun, deliberately.** The change cannot alter reusable-profile behaviour, and the
licensed acceptance already records that **neither licensed model exercises zero-length function profiles
at all** - so an A/B could not observe this path. The earlier A/B result stands unchanged.

---

## This branch's main work: profile definition reuse
2026-08-23 - **Value-based deduplication of the SAM `Profile` definitions a TBD import creates.**
Full detail, invariants and the deliberate exclusions live in
`SAM_Tas/SAM.Analytical.Tas/PROFILE_DEFINITION_REUSE.md`.

**The problem.** A SAM `Profile` is a library-level REUSABLE DEFINITION - a native SAM model already shares
one `ProfileLibrary` entry across every `InternalCondition` that references it. The TBD import did not:
`Convert.ToSAM_Profiles` minted one profile per TBD internal-condition slot and named it
`"{internal condition} [{profile}]"`, so the name stated a PLACE rather than a SHAPE, which is what made
sharing impossible. `ModelA-Tas.sam`: 44 collected slots, 42 library entries, **20 distinct
`(Category, flattened Values)` definitions.**

**The rule now.** Reusable-definition equality is the SAM `Category` string (raw, ordinal) plus the complete
flattened values plus the value count, compared by exact IEEE-754 bit pattern with `-0.0` normalised to
`0.0` and every NaN canonicalised. No TAS internal-condition name, no space name, no profile Guid, no
encounter order. Zero-length (TAS function) profiles are **excluded** from dedup - their flattened form is
an incomplete representation of them - and keep today's per-internal-condition import verbatim.

**Deterministic naming.** Canonical name = the ordinal-smallest normalised source TAS profile name in the
equality group; on collision within a category, `_<signature hash>`; if even that is claimed,
`_<signature hash>_<k>`. It never refuses, never drops a profile and never overwrites one. Determinism
comes from claiming names in `ProfileDefinition.CompareTo` order (category, value count, value bits), not
in traversal order, so a reversed walk or a repeated import produces identical names. All ordering is
`StringComparer.Ordinal`.

**One index for the whole conversion.** `Query.ProfileReuseIndex(TBD.Building)` reads every slot once over
COM and is threaded through the library build, the zone/internal-condition conversion AND
`Modify.AddUnusedInternalConditions`. That last path was the gap the independent review found: with
`importUnused: true` it called `internalCondition_TBD.ToSAM()` with no index, which after dedup would have
left the unowned template conditions pointing at legacy names the library no longer carries.

**Deliberately unchanged** (all pre-existing, all confirmed still present): `ticV` is still not emitted into
the imported `ProfileLibrary`, so `VentilationProfileName` still dangles - the slot is NOT collected and the
reference keeps its legacy name, and a test pins that as baseline rather than as a regression of this work.
TBD `InternalCondition` sharing, opaque `BuildingElement` reuse, construction naming and the function-profile
import semantics are all untouched.

**Adjacent survey** (asked for alongside the change): no other TAS -> SAM import object is both a SAM
reusable library definition and cloned/renamed per space out of TAS provenance. Materials are keyed by TBD
material name building-wide; constructions by TBD construction GUID building-wide; aperture constructions by
`Query.ApertureConstructionPairKey` building-wide (the previous programme). SAM `InternalCondition` is the
one adjacent case and is NOT a pure library definition here: `Convert.ToSAM(TBD.InternalCondition, double)`
bakes the owning zone's floor area into `AreaPerPerson` and the per-person gains, so per-space instances are
semantically required, not provenance artefacts. Out of scope and unchanged.

**Validation this session:** `SAM_Tas.sln` builds with 0 errors in Debug AND Release (VS Framework MSBuild;
only the pre-existing MSB3270/MSB3277 and XML-doc warnings). `SAM.Analytical.Tas.TM59.Tests`
**495 passed / 0 failed** in both Debug and Release (457 pre-existing, unmodified, + 27 new in
`ProfileDefinitionReuseTests.cs`, + 11 arriving with PR #36); `SAM.Analytical.Tas.Benchmark.Tests` 16/16.

**LICENSED A/B: PASSED (2026-08-23, EDSL Tas).** Full evidence in
`SAM_Tas/SAM.Analytical.Tas/PROFILE_DEFINITION_REUSE.md` → "Licensed acceptance". One-DLL swap (67 files,
all hash-identical but `SAM.Analytical.Tas.dll`), input `.tbd` generated once with the baseline DLL so both
sides import identical TAS input. Two rounds, because PR #36 merged mid-validation: round 1 baseline
**`2950b27c`** vs **`d5ba1082`**; round 2 baseline **`03f97570`** (the current merge-base) vs **`95dabb6b`**.
Two real models — `ModelA-Tas.sam` (2 spaces, 4 ICs, 44 slots, normal + HDD) and the real TM59 residential
project `SAM_zoningAM_v2zonesisDomestic.sam` (**9 spaces, 27 ICs, 396 slots**, conditions genuinely shared
across spaces).

| | ModelA-Tas | TM59 project model |
|---|---:|---:|
| SAM `ProfileLibrary` entries, baseline → feature | **42 → 20** | **369 → 30** |
| SAM-side fields compared / semantic differences | 176 / **0** | 1584 / **0** |
| TAS simulation-effective fields compared / differences | 852 / **0** | 5754 / **0** |
| hourly TSD values compared / differing | 227 760 / **0** | 1 024 920 / **0** |

Every numeric field compared as its exact IEEE-754 bit pattern, the TBD read back with TAS's own
`Get*(index)` accessors rather than the helpers under test, and a full 1–365 day simulation run against a
real TAS weather year. The **only** differences are the three predicted diagnostic ones —
`profile_TBD.name`, `profile_TBD.description`, `thermostat.name` — plus the zone GUID, which a
same-DLL-twice control run shows TAS re-mints on every export regardless. `internalCondition_TBD.name`,
IC counts and per-zone assignment are unchanged. The known dangling `VentilationProfileName` is unchanged
(4 / 36 unresolved references, the same set on both sides, all `Ventilation`). Repeat import is
byte-identical. The one coverage gap is stated rather than implied: **zero-length TAS function profiles are
not exercised by either licensed model** (both carry only value/hourly profiles), so their exclusion from
dedup rests on the COM-free tests.

---

## Also this session (merged in from PR #36)
2026-08-23 - **PR #36 revalidated on the user's exact production file and Codex review addressed.** Exact input:
`C:\Users\michal.dengusiak\OneDrive - Tetra Tech, Inc\Documents\SAM_daily\2027-08-03-HVAC\SAM_zoningAM_v2zonesisDomestic.sam`
(SHA-256 `CF0C749D8148EC7433482528040B4E32EAC5E5B6A6B91042C6029FF17E19537F`). Route was exactly
`AnalyticalModel -> SAM.Analytical.gbXML.Convert.ToFile -> WorkflowCalculator`, the engine behind
`SAMAnalytical.WorkflowgbXML`, with `Simulate=false` as in the warning-producing run.

**Exact seam diagnostic, before any new behavioural edit.** The checkout's stale pre-PR build output
reproduced `0 considered / 40 carry no building element stamp` twice, while retaining 9 TBD zones, 110
zoneSurfaces (20 pane + 20 frame) and 20 SAM apertures. After rebuilding the actual PR head `b585e87`,
temporary `UpdateIds` instrumentation reported: **9/9 spaces -> zones; 110/110 TBD zoneSurfaces -> SAM
panels; 40/40 aperture zoneSurfaces -> SAM apertures; 20 pane + 20 frame identifications; 40/40
BuildingElementGuid writes; 20 unique aperture collectors.** The instrumentation was then removed.

The exact model does NOT expose a second translation algorithm. TAS all-panel, non-shade and
space-related bboxes are identical at `[-30.5,-8,0]-[30.5,8,4]`; the corresponding SAM subsets are all
`[0,0,0]-[61,16,4]`. Every subset therefore proves the same TBD->SAM translation `(30.5,8,0)`. There are
no shades/non-building outliers, no differing SAM/TBD subset and no extra TAS transform. The production
file's domestic-zone metadata does not alter the relevant 9-space/50-panel/20-aperture geometry. Thus the
previous `SAM_zoningAM_v2.sam` passed for the same geometric reason; the apparent difference here was the
DLL actually loaded, not the SAM file. No further translation code was added.

**Behavioural fix already in PR #36.** `UpdateIds` computes that TBD->SAM centroid translation once and
passes it only to translation-aware panel/aperture matches. ZoneGuid is captured before clearing and
resolved GUID-first with exact-name fallback. The exact model now reports **40 considered / 40 rebound / 0
already shared; 40 aperture building elements removed** and no no-stamp note. The 40 per-instance aperture
elements become 3 reusable definitions: frame x20 surfaces, pane x15 and pane x5. Both first and repeated
runs preserve 9 zones, 110 zoneSurfaces, 20 pane surfaces, 20 frame surfaces and 20 physical apertures;
the returned model has 20 pane + 20 frame BuildingElementGuid stamps and 20 + 20 physical-surface stamps.
Repeat run produces the identical summary and element-use multiset `{20,15,5}` with no added definition.

**Codex P2.** `Query.Match` now keeps the original public panel and aperture CLR signatures exactly and
forwards each to a separate translation-aware overload. Only `UpdateIds` calls the overloads with a
translation. A reflection regression pins the original and translation-aware signatures for both return
types; the test project explicitly references `Interop.TBD` for that metadata-only check.

**Later Codex P2 (unassigned-panel centroid).** No behavioural change was made because its premise is false
for `AdjacencyCluster`: `Shade(panel)` returns true exactly when the panel has no related `Space`. Therefore
the existing `GetPanels().FindAll(x => !Shade(x))` SAM centroid already is the space-related subset, and an
unassigned panel cannot be a non-shade outlier. This is also what the licensed seam trace measured: all
non-shade and space-related bboxes were identical on both sides. A COM-free regression now pins the invariant
with an orphan wall 1000 m away; it is classified as a shade and cannot move the non-shade centroid.

**Newest Codex P2 (SAM-only resolved-space subset) - now fully validated, including the exact licensed
rerun, and pushed.** A SAM space added after export (or absent because TAS failed to export it) has related
panels, so those panels are non-shades but have no TBD counterpart. `UpdateIds` now resolves all SAM spaces
against the TBD zones first, derives BOTH centroids only from panels belonging to those successfully shared
space/zone pairs, and reuses the same resolution map for stamping. The new COM-free regression has two
non-shade spaces 1000 m apart and passes only the shared space to the translation subset; the SAM-only panel
is excluded.

**Exact licensed two-pass rerun with this fix, same production file
(`SAM_zoningAM_v2zonesisDomestic.sam`, same SHA-256 as above), same route
(`AnalyticalModel -> SAM.Analytical.gbXML.Convert.ToFile -> WorkflowCalculator`, `Simulate=false`).**
Built with the .NET Framework MSBuild (Debug then Release, `-t:Restore`/`-t:Build` as separate invocations),
then a temporary `net8.0-windows`/x64 probe (`.scratch/PR36Probe`, removed after use per the discipline
below) drove the real route against `build/SAM.Analytical.Tas.dll`/`SAM.Core.Tas.dll` end to end - no
COM-crossing `List<T>` helper calls (see `[[tas-licensed-harness-troubleshooting]]`), output under the short
`C:\PR36Out` path, one stray `TBD.exe` from the run stopped afterwards.
- **Before** (pre-fix baseline, same file, reproduced from the stale pre-PR build as before): **0 aperture
  part(s) considered, 40 carry no building element stamp**, while still keeping 9 TBD zones, 50 SAM panels,
  110 TBD zoneSurfaces (20 pane + 20 frame) and 20 physical apertures.
- **After**, PASS 1: **40 aperture part(s) considered; 40 rebound onto a shared definition, 0 already on
  one; 40 aperture building elements removed afterwards.** SAM side: 9 spaces, 50 panels, 20 physical
  apertures, **20/20 pane and 20/20 frame `BuildingElementGuid` stamps, 20/20 pane and 20/20 frame physical
  zone-surface stamps** (100% both ways, no no-stamp note). TBD side unchanged: 9 zones, 110 zoneSurfaces
  (20 pane + 20 frame), 8 building elements, reduced to **3** reusable aperture definitions with element-use
  multiset **{20, 15, 5}**.
- **Repeat run (PASS 2, same process, TBD re-exported and re-solved)**: byte-identical summary line to PASS
  1 - same 40/40/0, same 20/20/20/20 stamps, same {20,15,5} multiset, no additional definition created. The
  fix is deterministic on this file.
- This exactly reproduces the already-validated fixed state recorded above from `b585e87` (before this
  newest SAM-only-subset refinement existed) - the newest fix is a generalisation for models with SAM
  spaces absent from the TBD and does not change behaviour on this file, as expected: TAS all-panel,
  non-shade and space-related bboxes were already identical for this file, so the new "shared-only" subset
  and the old "non-shade" subset select the same panels here. Confirms no regression from the newest change.

**Files changed in this follow-up:** `SAM_Tas/SAM.Analytical.Tas/Query/Match.cs`,
`SAM_Tas/SAM.Analytical.Tas/Modify/UpdateIds.cs` (SAM-only resolved-space subset),
`SAM_Tas/SAM.Analytical.Tas.TM59.Tests/UpdateIdsZoneResolutionTests.cs`,
`SAM_Tas/SAM.Analytical.Tas.TM59.Tests/SAM.Analytical.Tas.TM59.Tests.csproj`, this file.

**Validation:** focused Debug **11/11**, focused Release **11/11**; full TM59 Debug **468/468**, full TM59
Release **468/468**; solution Debug and Release build with **0 errors** (only the pre-existing
MSB3270/MSB3277 processor-architecture/System.Memory warnings). Exact licensed two-pass rerun on the
production file passes as described above, deterministically. The temporary `.scratch/PR36Probe` harness
was deleted before committing, matching the `APERTURE_TYPE_REUSE.md`-style discipline for scratch harnesses.

**Unresolved / out of scope:** the same recentring also afflicts any OTHER geometry-matching step on this
route that lacks the compensation (`CopyResults` matches apertures to solar surfaces by geometry; the
simulation/results legs were not run here - `Simulate=false`). Not touched; would need its own licensed
validation.

**Recommended next step:** all automated validation and the exact-model licensed acceptance are green;
reply to Codex comment `3839130533` confirming the SAM-only resolved-space subset fix is implemented and
licensed-validated, and await fresh CI checks on the pushed commit. Do not merge automatically - that
remains a human call.

---

## Previously
2026-08-23 - **Two aperture hardening fixes, both pre-existing defects deliberately kept out of PR #34.**
Full detail in `SAM_Tas/SAM.Analytical.Tas/APERTURE_HARDENING.md`; the two limitations they close are
reworded where they were recorded, in `APERTURE_DEFINITION_REUSE_GBXML.md`.

**1. A stated `ApertureParameter.FeatureShade` never reached the TBD pane.** Two causes, neither of them
the name decode (a licensed probe showed all 14 pane elements decoded their aperture GUID and found the
aperture). First, an `AdjacencyCluster` can hold one aperture BOTH on its panel and as a cluster object,
real models carry both - all 14 in `ModelA.sam` do, straight off disk - and
`AdjacencyCluster.GetAperture(guid)` answers from `GetObject<Aperture>` FIRST, so
`Modify.UpdateBuildingElements` read colour, opening controls and the shade off a stale copy the user's
edit never reached. New `AperturePanelIndex` (`Classes/` + `Query/`) answers from the panel walk only, and
is now shared with `Modify.UpdateApertureDefinitions`, which had built its own local dictionary for exactly
this hazard. Second, **licensed TAS silently drops the FIRST `AssignFeatureShade`** onto a building
`T3DDocument.ExportNew` has only just written; re-assigning the SAME object lands it. `Modify.SetFeatureShades`
now establishes the assignment by RE-READING the element and repeats up to three times, and reports honestly
when it never took. `UpdateBuildingElements` gained a `feature shade stated / written` summary note.

**2. The importer paired a window's pane and frame by construction NAME.** Stage 2 shares by value, so two
`ApertureConstruction`s with identical panes and different frames export as one shared pane construction plus
two frames; bucketing surfaces by the base name left after stripping `-pane`/`-frame` put the second family's
pane in the FIRST family's bucket and its frame in a bucket of its own. **The rule is now: physical grouping
is geometric (`Query.GroupAperturePolygons` over ALL of a zone's aperture surfaces at once), which half a
surface is comes from its element's `BEType`, and family identity is the PAIR of construction identities the
two halves carry (`Query.ApertureConstructionPairKey`, GUID first and name only as a fallback). The name is
chosen afterwards (`Query.ApertureConstructionName`) and labels the family; it never decides it.**

**Licensed A/B (2026-08-23, EDSL Tas, `ModelA.sam`, one-DLL swap against a `e9b5a3d0` worktree build):**

| | baseline | this branch |
|---|---:|---:|
| shaded gbXML run: feature shades on pane elements | 0 | **1** |
| shaded gbXML run: aperture building elements | 3 | 4 (the extra one is the shaded pane's own) |
| two-family round trip: physical apertures imported | **21** | **14** |
| two-family round trip: families | A x14, B x7 (B's panes lost) | **A x7, B x7** |
| two-family round trip: pane+frame stamps | both=7, paneOnly=7, frameOnly=7 | **both=14** |

The inverse case (shared FRAME, two panes) reconstructs the same way. `ModelA.sam` unmodified is unchanged
on both routes: 28 -> 3 aperture building elements, 2 aperture constructions, 14 apertures imported under one
`Windows: SIM_EXT_GLZ` with `both=14` stamps, and **a second full workflow run reproduces every count with
nothing added and no duplicated shade**. Tests: **457/457 in Debug and Release** (18 new in
`ApertureHardeningTests.cs`).

**Residual, all pre-existing and out of scope:** `Convert.ToTBD(analyticalModel, ...)` writes no aperture
`FeatureShade` at all (`Modify.Update`'s shade block has been commented out for years, and
`UpdateBuildingElements` is the only writer anywhere); the import never reads a shade back off a TBD element;
`Query.UpdateT3D` still resolves its aperture through `AdjacencyCluster.GetAperture`, the same stale-copy
defect, and changing it needs its own licensed T3D validation.

---

## Earlier
2026-08-22 - **The standard gbXML workflow now gets the same reusable aperture definitions the direct
`SAMAnalytical.TBD` route has.** Stage 2 scoped itself to the direct `Modify.Update` export and declared the
gbXML/T3D route out of scope; that gap is now closed. Full detail, invariants, the A/B table and the
deliberate limitations live in `SAM_Tas/SAM.Analytical.Tas/APERTURE_DEFINITION_REUSE_GBXML.md`.

**Root cause.** On this route SAM_Tas does not write the TBD - TAS's `T3DDocument.ExportNew` does, from a T3D
in which every aperture is its own `window`, because the gbXML opening name has to carry the aperture GUID for
`Query.UpdateT3D` to decode it back. TAS therefore writes one aperture building element per aperture per part,
named after that aperture, and nothing afterwards collapsed them (`UpdateBuildingElements` only ever SPLITS).
The T3D cannot be canonicalised first: `Interop.TAS3D` exposes no surface or opening object at all.

**Fix.** One gbXML-gated step, `Modify.UpdateApertureDefinitions`, placed AFTER `Modify.UpdateIds` so it reads
the physical stamps that step has just written instead of re-deriving them. It adds no new rules: definition
resolution is `Modify.ResolveApertureDefinition`, extracted verbatim from `Modify.Update` so both routes share
one resolver; physical resolution is Stage 3's `Query.AperturePhysicalIndex`/`ApertureRebindKeys`, unchanged.
Afterwards orphaned elements are swept (`markDelete` + `DeleteMarkedBuildingElements`; TBD has no
`RemoveBuildingElement`) and instance-named or superseded aperture constructions removed. Nothing named after
a physical aperture may be ADOPTED as a shared definition
(`BuildingReuseCache.RefuseSeededDefinitions` + `Query.NamesContainingApertureGuid`).

**Licensed A/B (2026-08-22, EDSL Tas, `ModelA.sam`, builds differing in `SAM.Analytical.Tas.dll` only):**
aperture building elements **28 -> 3**, with 14/14 pane/frame `zoneSurface`s unchanged, 2 aperture
constructions, 2 aperture types, and the 14 apertures resolving to **2 distinct pane bindings and 1 frame
binding**. The resulting definitions are identical to the direct route's name for name and surface count for
surface count. A repeated run adds nothing.

**Three defects the licensed run caught that no COM-free test could:** the re-stamp always refused
(`AdjacencyCluster.GetAperture(guid, out panel)` returns early when the aperture is also a cluster object and
leaves `panel` null); `DeleteMarkedBuildingElements` returns a STATUS (-1 on success), not a count; and
leaving one construction under its signature-qualified name broke the import's pane/frame pairing, so it
reported 28 apertures for 14 windows. All three are fixed, the last by reclaiming the plain name once the
sweep frees it.

**Known, out-of-scope:** `Modify.UpdateConstruction` sets `material.width` only for a TRANSPARENT material, so
the frame construction `Modify.UpdateConstructions` writes earlier in the workflow differs from the Stage 2
definition in that one field. The pass works around it rather than changing a writer used by every route.

**Validation:** COM-free suite **438/438** Debug and Release (419 pre-existing unchanged, 19 new); Debug and
Release solution builds **0 errors**; SPDX headers present on every new file; `git diff --check` clean.

**Immediate next step:** open the PR for `feature/tas-aperture-definition-reuse-gbxml`. Do NOT start
InternalCondition/profile work or opaque BuildingElement optimisation.

---

## Previous session

2026-08-22 - **PR #33 final review pass and focused licensed acceptance complete.** PR #32 (S3-C1 + S3-C2)
is merged;
`feature/tas-aperture-instance-identity` closes six further identity gaps it left, adds the handover doc
`SAM_Tas/SAM.Analytical.Tas/APERTURE_INSTANCE_IDENTITY.md`, takes the COM-free suite to **419/419**, and has
been through a full licensed-TAS A/B against the `0f66b11` baseline.

Physical aperture identity is now `{ ZoneGuid, SurfaceNumber }` and nothing else, held as one value type
(`ZoneSurfaceKey`) that every physical comparison goes through. A surface claimed by two apertures REFUSES
rather than resolving to whichever was enumerated first. The `_1`/`_2` slots are canonical - a slot is a SIDE
and a side is a ZONE - and all three write paths (export, import, `UpdateIds`) go through one mutator that
clears before it fills.

**Licensed headline (2026-08-22, EDSL Tas):** every scenario passes on this branch and FAILS on the baseline.
200 identical windows repeat-update with 0 stamps changed and 0 collisions where the baseline produces 400
collisions; a split rebinds exactly one surface and merges back onto the original element where the baseline
strands it; a real 2-zone model with 14 apertures sharing one construction round-trips with every stamp
0.0000 m from its own aperture where the baseline leaves all 28 unresolved; a two-zone aperture keeps exactly
one two-sided pane and one two-sided frame through export/update/save/reopen/import where the baseline
reports 13-14 spuriously two-sided and 0 after import. The exported TBD is **identical on all 61 dumped
facts** between the two builds, and a TAS run of both agrees on **173,376 result values with 0 differing,
max absolute and relative difference 0**. Full table in `APERTURE_INSTANCE_IDENTITY.md`.

The final PR review found both Codex behavioural comments valid. Multi-face aperture parts now preserve a
separate complete canonical surface set while `_1`/`_2` remain representative sides; a split/merge rebinds
that entire set or refuses. Complete-set validation now precedes replacement lookup/creation, cache
reservation, controls, schedules, shade and split counting, so an invalid/contested stamp creates no orphan
and moves nothing. Representative-only legacy stamps refuse until restamped rather than risk a partial move.
The two Copilot comments (XML return contract and `workk internla`) are also fixed.

**Validation:** focused regressions **4/4**; full COM-free suite Debug **419/419** and Release **419/419**;
Debug and Release solution builds **0 errors** (only existing MSB3270/MSB3277 and legacy compiler/XML-doc
warnings). The first post-review SPDX run found that the changed legacy `ApertureParameter.cs` had no required
header; the final commit adds that header, and `git diff --check` passes.

**Focused licensed acceptance (2026-08-22, EDSL Tas, exact `ed6d659` Release DLL): PASS, 0 failures.**

- **Multi-face split/merge-back:** aperture `353144a1-3d6a-4daf-b12b-24bf7556bbce`; complete pane set
  `{A67E0FA9-DC62-44EB-A1E0-9CB988807FC6, 5}` and `{A67E0FA9-DC62-44EB-A1E0-9CB988807FC6, 13}`; representative
  `_1` is surface 5 and `_2` is empty. Both faces moved from element
  `{D36F2CA6-79A4-40EC-A531-DED89D40C8AE}` to `{F719D1F4-A112-4AA3-9CE0-72FFDC59F0D9}` on divergence and both
  returned to the original element on merge. Old/new surface counts were **4/0 before**, **2/2 after split**,
  and **4/0 after merge**. Unrelated apertures and the original definition were byte-for-value unchanged;
  merge created no further element and left the split element unused.
- **Contested refusal/no orphan:** changed aperture `17c8ddfd-3f58-4ebc-89bf-0d9968c43aa6`, contestant
  `3f882c59-5f42-4cf5-be9a-304a43b33535`, contested key
  `{D72E02ED-3D5C-48F2-93C0-9DA38CA94695, 5}`. Both the first and repeated update refused before creation;
  binding `{8B6851B8-8F32-4271-8D23-89C66C4E3D85}` and every physical surface stayed unchanged, split count
  stayed zero, and counts stayed exactly **4 building elements / 2 aperture elements / 8 constructions /
  1 aperture type / 0 schedules / 0 feature shades**. Repetition accumulated nothing.

The scratch driver and generated `.tbd` files are deliberately uncommitted. The fixture first settles the
known one-time legacy construction reconciliation, then measures only the refused rebind, so its unchanged
construction count is specific to the P2 acceptance rather than obscured by that pre-existing behaviour.

**Immediate next step:** merge PR #33 once GitHub checks and re-review are green. Do not broaden into Stage 4.

## Current status
**The gbXML route is done and awaiting PR** - see the "Last updated" section above and
`SAM_Tas/SAM.Analytical.Tas/APERTURE_DEFINITION_REUSE_GBXML.md`. Both front ends inherit it: Grasshopper's
`SAMAnalytical.WorkflowgbXML` and SAM_UI's simulate-cases and multitasker flows all construct the same
`WorkflowCalculator`, so no change was needed in `SAM`, `SAM_UI` or `SAM_gbXML`.

**Stage 1 is merged.** The export shares one `TBD.ApertureType` across every building element stating the
same opening control. Full detail, the S1-C0 probe result and the licensed-TAS acceptance table live in
`SAM_Tas/SAM.Analytical.Tas/APERTURE_TYPE_REUSE.md`.

**Stage 2 is merged** (PR #31, 2026-08-21). The direct `Modify.Update` export shares one `TBD.Construction`
and one aperture `TBD.buildingElement` across every aperture stating the same content, instead of creating
one per aperture per part. 200 identical windows go from 400 constructions and 400 elements to 2 and 2,
while all 400 physical `zoneSurface`s remain. Full detail, invariants, seed gates, deliberate limitations
and the acceptance table live in `SAM_Tas/SAM.Analytical.Tas/APERTURE_DEFINITION_REUSE.md`.

**Stage 3 (physical-instance identity hardening) is in progress** on
`sow/2026-Q3-instance-identity` (PR #32 -> `sow/2026-Q3`):

- **S3-C1 done** (`11a856a1`) - `UpdateBuildingElements` resolves which SAM aperture(s) a TBD building
  element stands for via STAMPS, not name-decode: `Modify.UpdateIds` now also stamps each aperture's
  `Pane/FrameBuildingElementGuid` with the GUID of the TBD element its export bound it to; the update path
  resolves through a definition-membership map built from those stamps (many apertures may stamp one
  shared element), falling back to the ORIGINAL single-aperture GUID-in-name decode, byte-for-byte
  unchanged, for every element no aperture stamps (all TAS-authored/legacy TBDs). A shared element is never
  mutated; a divergent member is split onto its own element and only its own stamped surfaces are rebound.
  Also fixes `Query.Match`'s ZoneSurfaceReference overload comparing SurfaceNumber alone across zones
  (TAS numbers surfaces PER ZONE) - now requires ZoneGuid agreement when both sides state one, exposed as
  COM-free `Query.ZoneSurfaceReferencesMatch`.
- **S3-C2 done** (`c79be01d`) - the aperture import's inline polygon grouping extracted as COM-free
  `Query.GroupAperturePolygons`, fixing two real import bugs with one root cause (the seed's key half was
  read back off the shrunk tuple list): a seed with a coincident partner got a DIFFERENT aperture's
  surface key attached, and a lone pane with no coincident frame produced an EMPTY group - no
  ZoneSurfaceReference, no BuildingElementGuid, no imported OpeningProperties for that aperture at all.
- **S3-C3 DONE** on `feature/tas-aperture-instance-identity` - the handover doc
  (`APERTURE_INSTANCE_IDENTITY.md`) is written and seven further identity gaps PR #32 left are closed (export
  and `UpdateIds` never cleared their stamps; the import dropped every internal aperture's second side and
  read pane/frame off the construction name; two `Query.Match` overloads still ignored the zone; nothing
  detected a physical surface claimed by two apertures; and an element created by a SPLIT could never be
  updated again, so a split aperture could never merge back). 415/415 COM-free tests pass, and the
  **licensed-TAS A/B against the `0f66b11` baseline passes every scenario** - see the S3-C3 section below.

The frozen three-stage plan is
`C:\Users\Virtual Machine\.claude\plans\you-are-in-plan-lazy-pebble.md` (approved rev. 2, 2026-08-21).
Stage 3's first item was a known, already-planned consequence of Stage 2 that PR #31's Codex review caught
independently: `UpdateBuildingElements` degraded (note-based, not silent corruption) when fed a Stage-2
TBD, because Stage 2 element names no longer carry a single aperture's GUID by design - see
`APERTURE_DEFINITION_REUSE.md`, "Known limitation: `UpdateBuildingElements` on a Stage-2 TBD". S3-C1 is
the fix.

## Stage 3 - Codex review fixes (PR #32, this session)

The Codex review of PR #32 raised five findings; all are fixed:

- **P1 - legacy word-set construction fallback restored** (`Modify/UpdateBuildingElements.cs`). The Stage 3
  rewrite kept building `constructionWordSets` but stopped USING it, so a legacy element whose name carried
  all of a construction's words without either side being a literal suffix fell through to the null check
  and got no construction, colour, opening controls or schedules. The subset-of-words fallback is restored
  after the exact/suffix matches, before the null check.
- **P1 - feature shade joins the split decision** (`Query/ApertureMatchesExistingAssignment.cs`, new
  `Query/FeatureShadesMatch.cs`, call site). A stamped pane adding, removing or changing ONLY its
  `FeatureShade` still matched on colour and openings, so it stayed bound and never reached
  `SetFeatureShades`. The element's current shade (read once via `GetFeatureShade(1)`, converted to SAM) is
  now compared by CONTENT - float-precision, NaN-aware, name/description excluded (TAS auto-names shades) -
  and a mismatch splits exactly as a colour change does. Consequential invariant: a pane stating a shade
  never takes the reuse cache (a shade-carrying element is never shareable - the seed gate's own rule), is
  always created fresh, gets the shade written, and is NOT registered for reuse.
- **P1 - a lone pane is no longer stamped as its own frame** (`Convert/ToSAM/AdjacencyCluster.cs`). S3-C2's
  one-member groups newly fed singletons into a fallback that assigned `zoneSurfaces_Aperture[0]` to BOTH
  pane and frame, and frame-first reference matching then classified the pane as a frame. A singleton now
  keeps only the part its construction name (or, suffixless, its element's `BEType`) states; the `[0]`
  fabrication fallback is retained for multi-member groups only.
- **P2 - rebind validates the complete surface set before moving any** (`RebindMemberSurfaces`). A
  two-sided member whose second surface was missing/stale previously rebound the first and still advanced
  the BuildingElementGuid stamp, splitting the aperture across old and new elements. Now any resolution or
  stale-stamp failure rebinds NONE of the member's surfaces and leaves its stamp untouched.
- **P1 - this file updated** (it still read "Stage 3 not started" and recommended opening the
  already-merged Stage 2 PR).
- **P1 - collision-safe naming for repeated shade splits** (second review round; new
  `Query/ShadedBuildingElementName.cs`, call site). The plain two-name budget (preferred +
  signature-qualified) excludes the shade from the signature, so a second shade split of one definition -
  or a re-split after another shade change - derived a name that was already taken and `BuildingElementName`
  returned null, leaving the pane bound to the wrong-shaded element. The shade-aware variant falls back to
  a shade-content discriminator (FNV-1a over the definition signature plus the stored-float bit pattern of
  every shade field), then a counter, keeping the `Windows: <base>_<8 hex> -pane` convention shape so the
  name still decomposes.
- **CI (not Codex, same session):** `build.yml`'s dependency-clone fallback could not map a sow FEATURE
  branch (`sow/2026-Q3-instance-identity`) to a dependency-repo branch on PUSH events (empty `base_ref`) and
  fell through the stale hardcoded `sow/2026-Q2` to the default branch, which no longer carries
  `SAM.Analytical.Benchmark` - every push build failed in the SAM_Validation step while the PR build passed.
  Both clone steps now derive the quarter branch (`sow/2026-Q3`) from the ref name as a fallback.

Files changed: `SAM_Tas/SAM.Analytical.Tas/Modify/UpdateBuildingElements.cs`,
`SAM_Tas/SAM.Analytical.Tas/Query/ApertureMatchesExistingAssignment.cs`,
`SAM_Tas/SAM.Analytical.Tas/Query/FeatureShadesMatch.cs` (new),
`SAM_Tas/SAM.Analytical.Tas/Query/ShadedBuildingElementName.cs` (new),
`SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/AdjacencyCluster.cs`,
`SAM_Tas/SAM.Analytical.Tas.TM59.Tests/InstanceIdentityTests.cs` (+13 tests: shade add/remove/change,
float round-trip stability, frame-ignores-shade, `FeatureShadesMatch` null/text/NaN cases, and the four
shade-split naming cases), `.github/workflows/build.yml`, this file.

Validation: `SAM.Analytical.Tas.csproj` builds with 0 errors in Debug AND Release (Framework MSBuild; the
MSB3270 COM-architecture warnings are pre-existing). `SAM.Analytical.Tas.TM59.Tests`: **369/369 pass** in
both configurations (356 pre-existing, unchanged, + 13 new). CI on the final head: build (push AND
pull_request) + spdx all pass. Licensed TAS: not run - S3-C3 owns that gate.

---

## Stage 3 - S3-C3: physical instance identity completed (branch `feature/tas-aperture-instance-identity`)

Off `sow/2026-Q3` at `0f66b11` (i.e. after PR #32 merged S3-C1 and S3-C2). This branch finishes Stage 3:
the gaps PR #32 left, the handover doc, and the COM-free suite for both. **The licensed-TAS gate has NOT been
run and Stage 3 is not mergeable until it has.**

Full mechanism write-up: `SAM_Tas/SAM.Analytical.Tas/APERTURE_INSTANCE_IDENTITY.md`. It documents PR #32's
half as well as this one, so it is the single handover document for the stage.

### What PR #32 had already fixed (do not redo)

`UpdateBuildingElements` membership resolution + split/re-merge + atomic guarded rebind; the import's
seed-key and lone-pane grouping bugs (`Query.GroupAperturePolygons`); `UpdateIds` stamping
`Pane`/`FrameBuildingElementGuid`; `Query.Match`'s `ZoneSurfaceReference` overload made zone-aware
(`Query.ZoneSurfaceReferencesMatch`); feature-shade divergence in the split decision and collision-safe
shade-split naming.

### Gaps this branch closes

1. **The direct export never cleared its stamps.** `Modify.Update` filled
   `Pane`/`FrameZoneSurfaceReference_1` then `_2` in creation order and only where the slot was EMPTY. On a
   model already stamped by a previous export that left the old `_1` standing - pointing at a surface number
   TAS need not have reassigned to the same surface - and overwrote `_2`. An aperture whose pane is split
   into several faces filled BOTH slots from ONE side, losing the other side entirely.
2. **`UpdateIds` cleared the panel stamps but not the aperture ones**, so a second pass wrote side 1 into
   `_2` and then side 2 over the top of it.
3. **The import dropped the second side of every internal aperture.** The pass that meets an already-created
   aperture in the adjacent zone did `continue`, so a two-zone aperture came back stating one physical
   surface where the TBD holds two.
4. **The import read pane vs frame off the CONSTRUCTION NAME**, with `BEType` consulted only for singletons -
   so a multi-member group whose constructions were named unconventionally fell through to the `[0]`
   fabrication and stamped one surface as both halves.
5. **`Query.Match`'s other two overloads still compared SurfaceNumber alone**, ignoring the zone; the panel
   overload's `_2` branch also re-read `_1` (unreachable second slot, and an outright throw on a panel with
   `_2` but no `_1`); and the `ZoneSurfaceReference` overload RETURNED on a null list entry instead of
   skipping, so one null made every aperture after it unresolvable.
6. **Nothing detected a physical surface claimed by TWO apertures.** The membership map is keyed by
   building-element GUID (shared by design, so blind to it) and the surface index was last-wins. A contested
   surface would have been rebound to whichever aperture the enumeration reached first.
7. **An element created by a SPLIT could never be updated again** - found by the licensed gate, not by
   reading. `UpdateBuildingElements` resolves an element's construction from its NAME and skips the element
   entirely when none matches; a split-created element carries a collision-discriminated name
   (`Windows: SIM_EXT_GLZ_1F3A0C21 -pane`) that no construction is named and that the word-set test cannot
   match either. So every subsequent pass counted it under `count_GlazingWithoutConstruction` and skipped it
   before the aperture block - which meant a split aperture could never MERGE BACK. Fixed by falling back to
   the construction the element itself already carries; the name matches stay first, because re-deriving from
   the name is how an updated construction reaches an element at all.

### What was added

| File | Role |
|---|---|
| `Classes/ZoneSurfaceKey.cs` | `{ ZoneGuid, SurfaceNumber }` - immutable, value equality, normalised zone GUID. COM-free. |
| `Query/ZoneSurfaceKey.cs` | Zone-GUID normalisation and the factories. A half-populated stamp yields NO key rather than a wildcard. |
| `Classes/AperturePhysicalIdentity.cs` | One SAM aperture's four stamps plus its two definition bindings, as values. |
| `Query/AperturePhysicalIdentity.cs` | COM-free factory from an `Aperture`, and the whole-model index factory. |
| `Classes/AperturePhysicalIndex.cs` | Surface -> (aperture, part, side), REFUSING any key two apertures claim. Cannot be asked which aperture a building element is. |
| `Query/ApertureZoneSurfaceSides.cs` | The canonical slot rule - a slot is a SIDE and a side is a ZONE - the parameter maps, and the clear helper. |
| `Modify/SetApertureZoneSurfaceReferences.cs` | The ONE mutator for the stamps (plus `AddApertureZoneSurfaceReference` for the import's second side, and `Query.ApertureZoneSurfaceReferences` to read them back). |
| `Query/AperturePart_BuildingElementType.cs` | Pane/frame from `BEType`, deliberately NOT via `Query.AperturePart(int)` - that overload answers `Frame` for TAS's DOOR type, which is right where it is used and wrong as a statement about which half a physical surface is. |
| `APERTURE_INSTANCE_IDENTITY.md` | The Stage 3 handover doc, covering PR #32's half too. |
| `Tests/ApertureInstanceIdentityTests.cs` | 46 COM-free tests. |

### What was changed

- `Modify/Update.cs` - aperture surfaces collected with their ZONE and PANEL (`ApertureZoneSurfaceRecord`);
  the four inline stamp blocks replaced by one deferred canonical pass through the shared mutator. Stage 2
  definition resolution untouched.
- `Modify/UpdateIds.cs` - aperture stamps cleared alongside the panel ones; surfaces collected over the pass
  (`ApertureSurfaceCollector`) and written canonically at the end; zone GUID passed into both `Match` calls.
- `Convert/ToSAM/AdjacencyCluster.cs` - second side stamped instead of skipped; `BEType`-first pane/frame
  classification with the name convention as fallback and the `[0]` fabrication only for genuine multi-member
  groups; first-side stamps through the shared mutator.
- `Query/Match.cs` - zone-aware overloads for the panel and aperture-by-surface forms; `_2`-branch copy/paste
  fixed; null-entry abort fixed; `ZoneSurfaceReferencesMatch` normalises the GUID so it agrees with
  `ZoneSurfaceKey`.
- `Modify/UpdateBuildingElements.cs` - `AperturePhysicalIndex` built once and consulted by
  `RebindMemberSurfaces` as a contested-surface guard alongside the existing stale-stamp guard; the surface
  index re-keyed on `ZoneSurfaceKey` (replacing the raw `SurfaceKey` string format) so every physical
  comparison in the codebase uses one key type; ambiguities reported per surface and in the summary.

### Decisions worth not re-deriving

- **PR #32's design was kept, not replaced.** An earlier pass on this branch had built a parallel decision
  engine (`ApertureSurfaceActions`) and grouping helper (`ApertureSurfaceGroups`) plus an extraction of
  Stage 2's resolve-or-create; all were dropped on merging PR #32 rather than run alongside it. Two competing
  formulations of one decision is worse than either.
- **A slot is a SIDE and a side is a ZONE.** Several surfaces in one zone compete for one slot; the lowest
  surface number represents it. Three zones is a refusal, not a truncation.
- **Ordering normalises the zone GUID; writing preserves the caller's spelling**, so a re-exported model
  still diffs clean against its source.
- **`ZoneSurfaceReferencesMatch` falls back to the number when EITHER side states no zone**, which makes the
  whole zone-awareness change a strict tightening rather than a new class of refusal.
- **The import's construction-GUID bucketing was deliberately left alone** and documented instead: it only
  ever restricts grouping (so it cannot cross-bind), and the bucket key is also what supplies the aperture's
  `ApertureConstruction`, so changing it is a round-trip change rather than a grouping fix. Recorded in
  `APERTURE_INSTANCE_IDENTITY.md`, "Documented ambiguity".

### Validation performed

- `SAM.Analytical.Tas.csproj` and the full `SAM_Tas.sln` build with **0 errors** in Debug and Release
  (Framework MSBuild; the MSB3270/MSB3277 warnings are pre-existing).
- `SAM.Analytical.Tas.TM59.Tests`: **415/415 pass** (369 before this branch, all unchanged and green -
  including Stage 1 `ApertureTypeReuseTests` and Stage 2 `ApertureDefinitionReuseTests` with their sharing
  expectations intact - plus 46 new).
- **Licensed TAS A/B, 2026-08-22, every scenario PASSES on this branch and FAILS on the `0f66b11` baseline.**
  Full table in `APERTURE_INSTANCE_IDENTITY.md`, "Licensed TAS". Summary: A (200 identical windows, repeated
  update) 0 vs 400 collisions; B (split one) exactly 1 surface rebound, old element keeps the other 9;
  C (merge back) all 10 back on the ORIGINAL element vs baseline stranding it; D (real 2-zone model, 14
  apertures sharing one construction, `SAM -> TBD -> SAM`) every stamp 0.0000 m from its own aperture vs 28
  unresolved + 3 collisions; E (two-zone aperture through export/2x update/save/reopen/import) exactly 1
  two-sided pane and 1 two-sided frame with both link surfaces, vs 13-14 spuriously two-sided and 0 after
  import. Pre-simulation TBD **identical on all 61 dumped facts**; TAS/TSD A/B **173,376 values, 0 differing,
  max absolute and relative difference 0**.

### Licensed harness (not committed, same discipline as Stages 1 and 2)

`Gate3.exe`, a `net8.0-windows` console project (`Gate3.csproj`, `Program.cs`, `Model.cs`, `Tbd.cs`,
`Local.cs`, `Tsd.cs`) referencing the built DLL set by `HintPath` off `TasLibs`/`SamLibs` MSBuild properties,
so ONE binary runs against either branch's `SAM.Analytical.Tas.dll`. Commands: `probe`, `diag`, `diag2`,
`diag3`, `imp`, `inspect`, `dump`, `sim`, `cmp`, `results`, `a <n> <dir>`, `b <dir>`, `d <model> <dir>`,
`e <model> <dir>`. Three things worth not rediscovering:

- **Run it from a SHORT output path.** TAS shows a modal "Fail to save to file" dialog and then dies with
  "RPC server is unavailable" when the target path is long - the scratchpad path was long enough to trigger
  it. `C:\Gate3Out` works. Kill any stray `TBD.exe` first; a stuck one holds the file.
- **Never call a SAM_Tas helper that returns `List<TBD.*>`.** `SAM.Core.Tas` is built with
  `EmbedInteropTypes=True`, so those signatures cannot cross an assembly boundary at all (CS1769). `Tbd.cs`
  walks TAS's own 0-based `Get*(index)` accessors instead, which is also the right thing for a harness: what
  the TBD holds must be observed independently of the code being observed.
- **`SAMTBDDocument.Dispose` closes the shared TAS COM server**, so a handful of document open/close cycles
  in one process makes a later TSD read fail. Simulation is therefore run in a CHILD process, and the
  result-mapping leg still could not be driven to completion in-process.

Real models used: `SAM_Deploy/SAM_SolarCalculator/SAM_SolarCalculator.Tests/Fixtures/ModelA.sam` (2 spaces,
11 panels, 14 apertures, ALL sharing one `ApertureConstruction` - ideal for the collision scenario). A
synthetic hand-built box is used for A and B/C; note its hand-wound wall faces do NOT survive
`Convert.ToSAM` (only the two horizontal panels come back), which is why D and E use a real model.

### Unresolved

**Two legs of the licensed gate were NOT run and are not claimed:**

1. **The shading-specific chain** - `Simulate_Coverage`, `UpdateShading`, `Create.SolarModel`, `CopyResults`
   pane/frame/panel solar mapping. Not attempted. Mitigating evidence rather than a substitute:
   `CopyResults` matches apertures to solar surfaces by GEOMETRY, not by the stamps (recorded in
   `APERTURE_DEFINITION_REUSE.md`), and the TAS/TSD A/B is exactly identical.
2. **`Modify.AddResults` end to end.** Driven to the point of consuming the stamps, then blocked by the
   `SAMTBDDocument.Dispose` COM-server teardown described above - **identically on both builds**, so it is a
   harness limitation, not a Stage 3 behaviour. What IS established is that the stamp set `AddResults` keys on
   is exactly right here (28 stamps for 14 apertures) and was badly wrong before (54).

**Two pre-existing defects found and deliberately NOT fixed** (both confirmed identical on the baseline, both
recorded in `APERTURE_INSTANCE_IDENTITY.md`):

1. `Modify.UpdateConstructions` adds a duplicate, unused set of aperture constructions on a Stage-2 TBD
   (count 4 to 8 on the first `UpdateBuildingElements`, then stable) because its name derivation carries the
   `Windows: ` prefix where the Stage 2 export does not. Inert - nothing points at the duplicates.
2. **`Modify.Update`'s own `updateGuids` stamping never reaches the caller**, because the method opens by
   reassigning its parameter to `adjacencyCluster.UpdateNormals(...)`, which returns a NEW cluster. Every
   stamp that branch writes lands on a clone that is discarded on return. `Modify.UpdateIds` is the live path
   and is what the gate exercises. NOT fixed here: turning it on would start mutating caller models on two
   public entry points (`WorkflowCalculator`, `SAM.Analytical.Tas.TM59.Convert.ToTBD`) that pass
   `updateGuids: true` today and get nothing, which is well outside what Stage 3 was chartered to change.
   **Worth a decision before anyone relies on export-side stamping.**

### Exact recommended next step

One focused independent diff review of `0f66b11..HEAD`, then open the PR. The licensed gate is done.

---

## Stage 2 - blocking merge gate (ALL ROWS NOW PASS - see "licensed acceptance progress" below)

These were the merge conditions. All three are now satisfied on licensed TAS; the evidence is recorded in
the acceptance section below and summarised in `APERTURE_DEFINITION_REUSE.md`'s table.

1. **A/B simulation.** Export the same real model twice - once with the old per-aperture building-element
   behaviour (`sow/2026-Q3`) and once with shared elements (this branch) - run TAS/TSD on both, and compare
   zone results. They must be numerically equivalent within normal solver noise.
2. **One real shaded project**, checking `UpdateShading`, `CopyResults` and aperture solar-result mapping.
   `CopyResults` already matches apertures to solar surfaces by GEOMETRY (its own comment records that the
   stamped building-element GUID is really the shared *construction* GUID), so sharing should not affect it -
   but confirm rather than assume.
3. **Object counts and round trip**, per the table in `APERTURE_DEFINITION_REUSE.md`: 200 identical windows
   -> 400 surfaces / 2 constructions / 2 elements with Stage 1 counts unchanged; several constructions,
   several opening controls, sealed windows, doors; a repeated export adding nothing; and
   SAM -> TBD -> SAM preserving aperture count, geometry, pane/frame classification, construction layers and
   `OpeningProperties`.

**One thing only licensed TAS can settle:** what a freshly added `TBD.buildingElement` carries for `ground`,
`markDelete` and `width`. The building-element seed gate refuses anything non-zero there, because the export
writes none of them. If TBD's own default is non-zero, no seeded element is ever adopted - safe in itself
(under-reuse, never unsafe sharing), but the "repeated export adds nothing" row would fail and a third
export could then hit the double-name refusal. That row is what detects it; if it fails, relax those three
fields to "equal to what a freshly created element reports" rather than "zero".

**RESOLVED on licensed TAS (2026-08-21):** a freshly created `TBD.buildingElement` reports `ground=0`,
`markDelete=0`, `width=0`, `ghost=0` - live and after save/reopen. The seed gate's zero assumption was
already correct for these three fields; **no fix was needed there.**

**A genuine defect WAS found, one level down, by the exact same class of check** - see "Stage 2 - licensed
acceptance progress" below: `Query.ConstructionMaterialDefinition`'s opaque/transparent branches assumed a
fresh `TBD.material`'s untouched `dynamicViscosity`/`convectionCoefficient` read back as `0`; licensed TAS
reports `1E-05`/`0.001`. Fixed and verified - see that section.

---

## Stage 2 - licensed acceptance progress (this session, on this machine)

**Environment:** EDSL Tas build 17044, `HKLM\SOFTWARE\EDSL\TasManager`, COM confirmed live
(`New-Object -ComObject TBD.Document` succeeds). A standalone harness (`Gate.exe`, **not committed** - see
"Licensed harness" below, same discipline as Stage 1's) drives the real `Modify.Update` against real `.tbd`
files through TBD COM.

### 0. Starting-state confirmation - PASS
Branch `feature/tas-aperture-definition-reuse`; HEAD contains `a178124` and `a5a98ed`; base `f3f5802` is an
ancestor; `git status` clean; `git diff --stat f3f5802..HEAD` touches only
`SAM_Tas/SAM.Analytical.Tas/**`, `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/ApertureDefinitionReuseTests.cs`,
and the two docs (`APERTURE_DEFINITION_REUSE.md`, `PROJECT_PROGRESS.md`) - no `SAM`, `SAM_Tas_Grasshopper`,
`UpdateBuildingElements`/`UpdateIds`/import-grouping/gbXML/T3D files touched. `SAM.Analytical.Tas.csproj`
builds clean (0 errors, Debug) against the licensed interop.

### 1. Fresh-object defaults probe - PASS, no gate fix needed
`Gate.exe probe`: a fresh `TBD.buildingElement` -> `ground=0 markDelete=0 width=0 ghost=0 BEType=0
colour=0`, unchanged after save/reopen. Matches the seed gate's assumption exactly.

### 2A. 200 identical pane+frame windows, first export - PASS
`Gate.exe counts 200`: 400 aperture zoneSurfaces (200 pane + 200 frame) over 200 physical apertures; exactly
**2** aperture Constructions (`SIM_EXT_GLZ -pane`, `SIM_EXT_GLZ -frame`); exactly **2** aperture
BuildingElements (`Windows: SIM_EXT_GLZ -pane`, `Windows: SIM_EXT_GLZ -frame`); **1** Stage 1 ApertureType
(`Opening Cd0.395 F1`, identical opening control on all 200); **0** schedules (no schedule-bearing control
used); 2 distinct aperture BuildingElement guids; 0 part-mismatched surfaces (every element's own
construction is the same part as the element).

### 2B. Repeat export into the same store - FOUND A GENUINE DEFECT, THEN FIXED, THEN PASS
**First run (before the fix):** the second `Modify.Update` call into the SAME open building produced
`+2 Construction, +2 BuildingElement` (collision-suffixed names, e.g. `SIM_EXT_GLZ_5EFF1698 -pane`,
`Windows: SIM_EXT_GLZ_A47E0E09 -pane`) instead of `+0` - a real "repeated export adds nothing" **failure**.

**Diagnosis.** Neither the seed gate's own refusal checks were firing (`Gate.exe diagnose` showed both the
original and the new collision-suffixed objects individually passing their seed gates with `proven=true`) -
the mismatch was in field-by-field content EQUALITY between the fresh, factory-computed
`ConstructionDefinition` and the one read back off the just-created `TBD.Construction`. A targeted
field-by-field diff (`Gate.exe diffconstruction`) isolated it to exactly two fields on every opaque
(`Timber`) and transparent (`Glass 6mm`) material layer: `dynamicViscosity` and `convectionCoefficient`.
`Query.ConstructionMaterialDefinition` (the COM-free mirror of `Modify.UpdateMaterial`) assumed these read
back as `0` for opaque/transparent materials, because `UpdateMaterial` never writes them for those two
kinds (only for `GasMaterial`) - the code's own comment already said "a fresh TBD material's own values
stand", it just had the value wrong. Licensed TAS reports `dynamicViscosity = 1E-05` and
`convectionCoefficient = 0.001` on a material `construction.AddMaterial()` creates and the opaque/transparent
`UpdateMaterial` overload leaves those two fields untouched on - confirmed by reading back three separate
already-exported opaque/transparent materials (frame Timber, both pane Glass 6mm layers), all agreeing
exactly, while the one Gas layer (which IS written) matched the mirror already.

**Fix applied:** `SAM_Tas/SAM.Analytical.Tas/Query/ConstructionMaterialDefinition.cs` - the opaque and
transparent branches now use the confirmed TAS defaults (`1E-05f`/`0.001f`, named constants
`FreshOpaqueOrTransparentDynamicViscosity`/`FreshOpaqueOrTransparentConvectionCoefficient`) instead of `0`
for these two fields. This is exactly the same class of correction the merge-gate note already anticipated
for the BuildingElement seed gate's `ground`/`markDelete`/`width` (which turned out not to need it) - here
applied one level down, on the construction-material mirror, where it genuinely was needed. Nothing about
the conservative foreign-object refusal gates was touched or weakened; this is purely the "what does a
value we never write read back as" mirror.

**Verification after the fix:**
- `SAM.Analytical.Tas.csproj` rebuilds clean (Debug, 0 errors).
- `SAM.Analytical.Tas.TM59.Tests` (net8.0): **337/337 pass** (was already 337/337 before the fix - the fix
  only changes what a value the tests never assert on-the-nose reads back as on real TAS; no COM-free test
  regressed or needed updating).
- `Gate.exe counts 200` re-run end to end: repeat-export deltas are now **Construction +0, BuildingElement
  +0, ApertureType +0, schedule +0, distinct aperture BE guid +0, part-mismatched surface +0** - the gate
  row now passes. (Zone +1 and aperture zoneSurface +400 on the repeat are expected: a repeat export adds a
  second zone/set of physical surfaces, exactly as Stage 1's own repeat-export scenario does.)

### 3. Definition variants - PASS, all 18
`Gate.exe variants`: construction variants C1-C8 (identical pane+frame reuse; different material; different
width; different layer order; frame-shared-across-different-panes; different frame material; pane-never-
equals-frame-even-with-identical-layers; same preferred name + different content -> deterministic
hash-suffixed distinct names) and building-element variants B1-B8 (different construction; different
colour; different opening control; no-openings bare element; opening multiplicity; one-vs-two-identical-
openings; window != door; `ApertureType.Undefined` still gets an element) **all match expectations**, and no
generated name contains a physical aperture GUID or `aperture.UniqueName()` in any scenario.

**B2 (colour) needed the harness's own expectation corrected, not the code.** First run expected 4 elements
(2 colours x 2 parts) and got 3; diagnosed by reading `Query/Color.cs` (pre-Stage-2, untouched by this
branch's diff - confirmed via `git log`/`git diff f3f5802..HEAD`): `ApertureParameter.Color` only overrides
the **pane's** colour; the frame always takes the type-derived default regardless. So 2 panes (one per
colour) + 1 shared frame = 3 is the CORRECT expected count. Harness fixed, re-run, all 18 pass.

### 4. Round trip - PASS, both former failures diagnosed by a licensed A/B against `f3f5802`
The two rows previously recorded here as unexplained failures (geometry, construction layers) were
**over-strict harness assertions, not Stage 2 regressions.** Settled by running the SAME round trip twice on
this machine - once with `SAM.Analytical.Tas.dll` built from `f3f5802`, once from HEAD - over one small
deterministic model, and diffing field by field. Method and result:

**Method.** A minimal `net8.0-windows` console harness (`RT.exe`, not committed, same discipline as the rest)
builds one 5 x 4 x 3 m space with two 1.0 x 1.2 m `SIM_EXT_GLZ` windows (3-layer pane Glass/Air/Glass +
Timber frame; window 1 carries `PartOOpeningProperties`, window 2 none), then runs
`analyticalModel.ToTBD(path)` -> save/close -> `Convert.ToSAM(path, false)`, dumping ~520 named fields per
run: SAM-side aperture geometry, part faces, layers, materials, opening properties and identity stamps;
TBD-side constructions, every material property, both stored widths, building elements, colours, `BEType`
and aperture types. Only `SAM.Analytical.Tas.dll` differs between the two runs; every other assembly, the
model and the code path are identical. Both sides were run twice - fully deterministic apart from
TAS-assigned GUIDs, including the collision-suffix hash.

**Result.** Of the ~520 fields: **219 SAM-side fields (every geometry, area, coordinate, layer, material,
transparency, opening-property and count field) are identical between baseline and Stage 2, and all 186
`tbd.construction` material/width fields are identical too.** The only differences are the intended Stage 2
sharing effects - aperture building elements 4 -> 3 (the frame is now shared by both windows; the two panes
stay separate because they genuinely differ: openable vs fixed changes both `Modify.SetColor`'s colour and
the aperture-type count), the definition-derived names, and the `BuildingElementGuid` stamps that follow
from them - plus TAS's own per-run GUIDs.

- **PASS** - physical aperture count preserved (2 -> 2) on both sides; sharing a `BuildingElementGuid` does
  not collapse physical apertures (invariant 9).
- **PASS** - `OpeningProperties` preserved on both sides (1 -> 1 aperture carrying one).
- **PASS** - pane/frame classification preserved on both sides.
- **PASS (was FAIL)** - geometry. The physical geometry round-trips exactly: the exported pane surface is
  0.99 m2 and the frame surface 0.21 m2, which is exactly what `Aperture.GetFace3Ds(Pane/Frame)` derives
  from the 1.2 m2 source aperture, and the imported aperture's external edge is the same 1.2 m2 rectangle at
  the same coordinates. What differs is only the imported `Aperture`'s composite `Face3D`: the export writes
  frame and pane as two surfaces, and `Convert.ToSAM` reassembles them into one face with a hole, so
  `GetFace3D().GetArea()` reads 0.21 (ring) where the source read 1.2 (solid). Comparing that composite area
  to the source's is what the old assertion did, and it is the wrong comparison - the derived part faces,
  which are what TAS simulates, agree to the last digit. **Identical on `f3f5802`.**
- **PASS (was FAIL)** - construction layers. Every orig -> round-trip layer difference is float read-back or
  a field TBD does not store, and every one of them is byte-identical on baseline: layer thicknesses
  `0.016 -> 0.016000001` and `0.05 -> 0.050000001` (`Convert.ToSingle`), `ThermalConductivity
  0.13 -> 0.129999995`, `Material.Group -> empty` (TBD has no such field), and - the first differing field
  walking the pane layers in order - `Glass 6mm` `SpecificHeatCapacity 750 -> NaN` and `Density
  2500 -> NaN`, because the TBD transparent `UpdateMaterial` overload writes neither (already documented in
  `Query/ConstructionMaterialDefinition.cs`). Layer ORDER, names, count, `additionalHeatTransfer` (0),
  construction type (`tcdTransparentConstruction` pane / `tcdOpaqueConstruction` frame) and both stored
  widths all round-trip exactly. **Identical on `f3f5802`.**

Two further pre-existing behaviours the A/B surfaced, both identical on baseline and therefore out of scope
here (noted so they are not rediscovered): `Convert.ToSAM` groups a zone's aperture surfaces by
`ApertureConstruction` guid and pairs them by descending area, so with two identical windows the imported
`Frame`/`Pane` `ZoneSurfaceReference`s can cross-pair between windows; and a `PartOOpeningProperties`
returns as a `ProfileOpeningProperties`. Neither is caused or changed by Stage 2.

### 5. A/B TAS/TSD simulation - PASS, bit-identical
**Model:** `C:\Users\Virtual Machine\Documents\SAM_daily\2026-08-05-PartO\SAM_zoningAM_v2.sam` - a real
9-zone Part O flat: 9 spaces, 50 panels, **20 apertures, every one pane+frame and every one carrying
`PartOOpeningProperties`**, all on the single `SIM_EXT_GLZ` aperture construction. Exported through
`analyticalModel.ToTBD(path)` with the model's OWN embedded weather (`United Kingdom, London`, lat 51.48,
lon -0.45) so both sides get byte-identical weather, then `Modify.Simulate(tbd, tsd, 1, 365)` - the same
full-year run period, timestep, controls and shading on both sides. The only difference between the two
runs is which `SAM.Analytical.Tas.dll` sits next to the harness.

**Pre-simulation equivalence - confirmed BEFORE simulating, as the gate requires:**

| | baseline `f3f5802` | Stage 2 |
|---|---|---|
| zones | 9 | 9 |
| physical `zoneSurface`s | 110 | 110 |
| of which aperture surfaces | 40 | 40 |
| total surface area | 3379.999993533 m2 | 3379.999993533 m2 |
| total aperture surface area | 64.799998492 m2 | 64.799998492 m2 |
| Constructions (aperture) | 6 (2) | 6 (2) |
| `ApertureType`s | 2 | 2 |
| **aperture BuildingElements** | **40** | **3** |

All **110 per-surface rows are byte-identical** - area, `type`, orientation, inclination, altitude,
altitudeRange, room-surface count, the construction assigned, `BEType`, colour and aperture-type count -
and all **510 construction/material/width lines are byte-identical**. Only the element NAMES differ, by
design: baseline writes one element per aperture per part (`Windows: SIM_EXT_GLZ <aperture-guid> -pane`),
Stage 2 writes `Windows: SIM_EXT_GLZ -frame` shared by all 20 windows plus two panes,
`Windows: SIM_EXT_GLZ -pane` and `Windows: SIM_EXT_GLZ_AAF00869 -pane`, because the model states two
distinct opening controls (`Opening Cd0.411 F1` and `Opening Cd0.477 F1`) - the same two `ApertureType`s
both sides already carry.

**Numeric result comparison:** 22 zone variables x 9 zones x 8760 h = 1,734,480 values, plus 7 surface
variables x 47 TSD surface records x 8760 h = 2,882,040 values.

- **values compared: 4,616,520**
- **values differing: 0**
- **maximum absolute difference: 0** (no location - there is no differing value)
- **maximum relative difference: 0** (same)
- verdict: **exactly zero**, not floating/solver noise. The two TSDs agree bit for bit.

Zone variables compared: DryBulbTemperature, MeanRadiantTemperature, ResultantTemperature, SensibleLoad,
HeatingLoad, CoolingLoad, SolarGain, LightingGain, InfiltrationVentilationGain, AirMovementGain,
BuildingHeatTransfer, ExternalConductionOpaque, ExternalConductionGlazing, OccupantSensibleGain,
EquipmentSensibleGain, HumidityRatio, relativeHumidity, LatentLoad, Infiltration, Ventilation,
ZoneApertureFlowIn, ZoneApertureFlowOut. Surface variables: InternalSolarGain, ExternalSolarGain,
InternalConduction, ExternalConduction, ApertureFlowIn, ApertureFlowOut, ApertureOpening. Five of the zone
variables (SensibleLoad, HeatingLoad, CoolingLoad, Ventilation, AirMovementGain) are all-zero in this
unconditioned model - stated so the count is not read as 22 independently varying quantities; the other 17
carry real magnitudes (e.g. ExternalSolarGain up to 9956.4, InfiltrationVentilationGain up to 12345.1,
DryBulbTemperature up to 31.77).

### 6. Real shaded-project regression - PASS
**Model:** `C:\Users\Virtual Machine\Documents\SAM_daily\2026-08-13-Shading\test-file-kolobrzeg.sam` -
one room with a real shading context (56 panels for one space; location Kolobrzeg, lat 54.18, lon 15.58)
and 3 pane+frame apertures. The Part O model above was tried first and produced no SAM solar results at all
(see the note below), so this is the model the regression actually ran on.

**Chain exercised, end to end, on both sides:** `SAM.Analytical.SolarCalculator.Modify.Simulate_Coverage`
over TAS's own 25 representative shade days x 24 h = 600 timesteps (56 surfaces, 62 coverage results
including each aperture's own `-pane`/`-frame` surfaces) -> `analyticalModel.ToTBD` -> `Modify.UpdateShading`
-> `Modify.Simulate(1, 365)` -> `Create.SolarModel(building)` -> `Modify.CopyResults` -> aperture
solar-result mapping.

**Stage 2 sharing was really in effect here** - aperture BuildingElements 6 -> 2 - while the physical side
stayed identical (12 zoneSurfaces, 6 of them aperture, total area 76.947999999 m2, 5 constructions, both
sides).

| Field | baseline `f3f5802` | Stage 2 |
|---|---|---|
| SAM coverage results attached | 62 (56 panels + aperture parts) | 62 |
| `UpdateShading` returned | true | true |
| TAS shade-day calendar | 25 days, no fallback | 25 days, no fallback |
| shade-proportion values read back | 7200 | 7200 |
| surfaces carrying shade data | 12 | 12 |
| `Create.SolarModel` linked faces / coverage results / values | 12 / 12 / 3096 | 12 / 12 / 3096 |
| `CopyResults` apertures with results | 3 | 3 |
| `CopyResults` pane / frame / panel results | 5 / 5 / 62 | 5 / 5 / 62 |
| per-aperture result rows (name, count, sum, max) | 10 | 10, identical |

**114 comparable dumped fields, 0 differing** - including every per-surface shade-proportion value count,
sum and max, and every per-aperture coverage row. The TAS simulation of the two shaded exports was compared
the same way as step 5: **928,560 values, 0 differing, max absolute difference 0.**

Re-running the same regression on the Part O model gave **146 comparable fields, 0 differing** and
**4,616,520 TSD values, 0 differing** as well, but with the shading chain empty on both sides (see below),
so it corroborates rather than adds coverage.

**Two harness-side findings, neither a Stage 2 issue, recorded so they are not rediscovered:**
- The shade read-back **must reopen the TBD read-WRITE**. On a read-only reopen TAS reports no shade-day
  calendar at all and `GetShadeProportion` returns -1 for every hour, so the whole chain looks empty.
  `Modify.LogShadeRoundTrip` already documents this; the harness hit it first-hand. `SAMTBDDocument.Dispose`
  only `close()`s, so a read-write reopen does not modify the file.
- `SAM.Analytical.SolarCalculator`'s `Simulate` and `Simulate_Coverage` both return **zero results** for the
  Part O model (`SAM_zoningAM_v2.sam`) even though it has a Location, weather data and 21 sun-exposed faces
  (9 roofs + 12 external walls), returning in ~0.1 s. The same calls work on the Kolobrzeg model. This lives
  in the sibling `SAM_SolarCalculator` repo, is untouched by this branch's diff, and was **not** chased -
  it is out of scope for Stage 2. It is only why the shaded regression uses the Kolobrzeg model.

### A/B build recipe (proven)
`git worktree add
../SAM_Tas_baseline_f3f5802 f3f5802` next to this checkout so its `..\..\..\SAM\build` hint paths still
resolve, then run MSBuild's `-t:Restore` and `-t:Build` as SEPARATE invocations (a combined
`-t:Restore,Build` fails with `CS0518 Predefined type 'System.String' is not defined` because the build
half does not pick up the assets the restore half just wrote). Use the .NET Framework MSBuild
(`vswhere -latest -find MSBuild\**\Bin\MSBuild.exe`) - `dotnet build` cannot run `ResolveComReference`.
`git diff f3f5802..HEAD` touches only `SAM.Analytical.Tas`, so the A/B is a one-DLL swap in an otherwise
identical output folder.

### Licensed harness (not committed - same discipline as Stage 1's `APERTURE_TYPE_REUSE.md` harness)
A standalone `net8.0-windows` console project (`Gate.exe`), referencing the built
`SAM_Tas/build/*.dll` set (SAM.Core/SAM.Analytical/SAM.Geometry/SAM.Weather/SAM.Architectural,
SAM.*.Tas, the Interop.* PIAs) by `HintPath` off a `LibsDir` MSBuild property, so the SAME harness binary
can be pointed at either this branch's `SAM.Analytical.Tas.dll` or the `sow/2026-Q3` baseline's for the A/B.
Commands implemented: `probe`, `sanity`, `diagnose <tbd>`, `diffconstruction <tbd>`, `counts <n>`,
`variants`, `roundtrip`, `inspect <model>`, `export <model> <weather|-> <tbd>`, `simulate <tbd> <tsd>`,
`compare <tsdA> <tsdB>`, `shaded <model> <weather|-> <dir>`. Source lived in a scratchpad and **has since
been lost with that session** - if this needs re-running from a fresh checkout, re-derive it from this
description and `ApertureDefinitionReuseTests.cs`'s fixture builders (`Library()`, `Glazing()`, `PartO()`)
rather than trying to recover the scratchpad files.

Steps 4, 5 and 6 were done with a second, smaller harness (`RT.exe`, also scratchpad-only), which is the
simpler thing to re-derive: `Program.cs` + `Commands.cs`, referencing the same DLL set with
`<Private>true</Private>` so the whole dependency closure lands in `bin`, plus
`<UseWindowsForms>true</UseWindowsForms>` (without it `SAM.Analytical.Query.Color` dies on
`System.Drawing.Common is not supported on this platform`) and the `SAM_SolarCalculator/build` assemblies
for step 6. It prints one `key<TAB>value` line per observed field so two runs diff mechanically. Commands:

    RT.exe rt <label> <outdir>                          # step 4: synthetic 2-window round trip
    RT.exe inspect <model.sam> <out.txt>                # what a real model carries
    RT.exe export <model.sam> <weather|-> <tbd> <out>   # ToTBD + full pre-simulation dump
    RT.exe surfaces <tbd> <out.txt>                     # pre-simulation dump of an existing TBD
    RT.exe sim <tbd> <tsd> <dayFirst> <dayLast>         # Modify.Simulate
    RT.exe compare <tsdA> <tsdB> <out.txt>              # the numeric A/B over zone + surface variables
    RT.exe shaded <model.sam> <weather|-> <label> <dir> # Simulate_Coverage -> ToTBD -> UpdateShading
                                                        #   -> simulate -> SolarModel -> CopyResults

Run each from two folder copies that differ only in `SAM.Analytical.Tas.dll`. The pre-simulation dump
deliberately reports each surface's geometry and construction assignment on one line and its building-element
NAME on another, because Stage 2 changes the name on purpose and only the first line is an A/B assertion.

**Important environment note found while building the harness:** target **`net8.0-windows`**, matching
`benchmark/SAM.Analytical.Tas.Benchmark.Cli` (the repo's own licensed-TAS CLI), NOT `net48`/`net481`. A
`net48` console host reproduces a genuine, deterministic `SAM.Core` defect
(`SAM.Core.Modify.SetValue(ParameterizedSAMObject, Assembly, ...)` re-adds the very `ParameterSet` it just
populated via `parameterizedSAMObject.Add(parameterSet)`, which self-`Copy()`s onto itself) on the FIRST
ever `.SetValue(SomeEnumParameter, value)` call on any object, because .NET Framework's
`Dictionary<TKey,TValue>` bumps its mutation-version counter on every indexer write, including a same-key
overwrite, so the self-enumerate-while-write throws `InvalidOperationException: Collection was modified`.
.NET 8's `Dictionary` does not bump the version on a same-key overwrite, so the identical call sequence is
harmless there - matching why the committed `net8.0` test suite and the `net8.0-windows` benchmark CLI never
hit it. This is a **pre-existing `SAM.Core` defect, out of scope for Stage 2** (it lives in the sibling `SAM`
repo, is unrelated to aperture-definition reuse, and is dormant under every environment this codebase is
actually run in) - noted here only so it is not rediscovered from scratch; not fixed, not filed.
Confirmed empirically with an isolated `sanity` probe (single `Space`, three `SetValue` calls) before
retargeting.

Two more harmless SAM.Core/SAM.Analytical quirks the harness had to route around while building its own
synthetic test model (again, not Stage 2, not fixed): `Create.AdjacencyCluster(shells, spaces)` and
`Create.Panels(shell)` both eventually touch `SAM.Analytical.ActiveSetting.Setting`, whose cold-start
`GetDefault()` hits the exact same self-`Copy()` pattern above on its own second `SetValue` call in a
process with no prior SAM settings load (e.g. this bare console harness, never the NUnit test host or a
Revit/Grasshopper session, both of which warm this differently). Routed around by assembling the harness's
`AdjacencyCluster`/`Panel`s by hand from `Query.PanelType(Vector3D)` and
`Create.Panel(Construction, PanelType, Face3D)` (both pure, no `ActiveSetting` touch) instead.

## Stage 2 - what landed

- `Classes/ConstructionMaterialDefinition.cs`, `ConstructionLayerDefinition.cs`,
  `ConstructionDefinition.cs`, `ApertureTypeAssignment.cs`, `BuildingElementDefinition.cs`,
  `BuildingElementSeed.cs` - immutable, COM-free value equality over the whole of what the export writes.
- `Query/ConstructionMaterialDefinition.cs` - the COM-free MIRROR of `Modify.UpdateMaterial`, field for
  field and clamp for clamp. This is what lets a construction already in the TBD be proven equal to one
  about to be written.
- `Query/ConstructionDefinition.cs`, `Query/BuildingElementDefinition.cs` - COM-free factories from a SAM
  `ApertureConstruction` / `Aperture`.
- `Query/ConstructionDefinitionTBD.cs`, `Query/BuildingElementDefinitionTBD.cs` - seed READERS. They read
  and decide nothing.
- `Query/BuildingElementDefinitionSeed.cs` - both seed GATES, as pure functions of what was read, which is
  what makes them testable with no installed TAS.
- `Query/ConstructionSignature.cs`, `ConstructionName.cs`, `BuildingElementName.cs` - deterministic
  FNV-1a signatures over exact Single bit patterns, and definition-derived naming.
- `Classes/BuildingReuseCache.cs` - extended with constructions and aperture building elements. Purely
  additive: 378 lines added, 7 removed, and all 7 are doc rewording. Stage 1's schedules, aperture types,
  day types and assignment tracking are byte-for-byte unchanged.
- `Modify/Update.cs` - the aperture block of the direct export resolves DEFINITIONS instead of names. The
  physical-surface block, the panel block and the identity stamps are unchanged.

## Stage 2 - decisions worth not re-deriving

- **The bug this fixes is not just duplication.** Both objects were looked up BY NAME, and a name match was
  taken as a content match. That was harmless only while the name carried the aperture's GUID; once names
  are derived from the reusable SAM `ApertureConstruction`, a by-name lookup would hand one window another
  window's glazing. Hence full content equality, and a deterministic collision suffix rather than adoption.
- **`AperturePart` is construction identity even though TAS does not store it.** The aperture import pairs a
  window's two constructions by stripping the `-pane`/`-frame` suffix and reading each side's layers, so a
  pane and a frame with identical layers must not collapse - the round trip would lose half the window.
  For the same reason the collision discriminator goes on the BASE (`SIM_EXT_GLZ_1F3A0C21 -pane`), keeping
  the part suffix terminal.
- **Underscores are KEPT in the construction name base**, unlike Stage 1's aperture-type naming. Real names
  are full of them (`SIM_EXT_GLZ`) and this base is the round-trip identity of the `ApertureConstruction`;
  stripping them silently renamed it. Found by a test, not by inspection.
- **The new names match what the TCD route already writes** (`Convert.ToTCD_Constructions`:
  `apertureConstruction.Name + " -pane"`), so the two routes agree and the round-tripped
  `ApertureConstruction` now carries the model's own name instead of an aperture's unique name. That is a
  genuine round-trip improvement, not just a rename.
- **NaN compares equal to NaN in the Stage 2 definitions**, unlike Stage 1's `ApertureTypeDefinition`. A
  material that states no conductivity stores NaN; under `==` such a layer would never equal itself and
  every window would get its own construction. NaN is normalised to the canonical `float.NaN` on the way in
  so the bit-pattern signature stays in agreement with equality, as signed zero already was.
- **`ApertureType.Undefined` is a distinct value, not a missing one.** Refusing it would take a building
  element away from an aperture that used to get one (the `Windows: ` prefix has always covered everything
  that is not a door). It shares among its own kind and never merges with a real window.
- **Mirror bugs can only cause under-reuse.** Two layers created in one export run through the same mirror
  on the same input, so a mirror that disagreed with the writer would give both the same answer and both
  the same TBD material. What it would cost is recognising a SEEDED construction.
- **Refusals are discarded on this path.** `Modify.Update` returns `void` with no notes channel and adding
  one would change its signature and every caller. Every outcome is the conservative one, so what is lost is
  diagnosability. Stage 3 owns reporting.
- **`UpdateBuildingElements` is unaffected**, despite decoding aperture GUIDs out of element names: it runs
  only on the gbXML/T3D route in `WorkflowCalculator`, never after a direct `Modify.Update`.
- **Seed classification is deferred** to the first lookup that needs it. `Modify.UpdateIZAMs` re-enters
  `Modify.Update` once per air handling unit with a synthetic, aperture-less cluster, and classifying every
  seeded construction's layers on each pass would be a per-IZAM COM cost paid for nothing.

## Stage 2 - validation performed

- `SAM_Tas.sln` builds with 0 errors (Debug) after the change; `SAM.Analytical.Tas` alone also builds clean.
- `SAM.Analytical.Tas.TM59.Tests`: **337/337 pass**. 233 pre-existing (unchanged, including all 80 Stage 1
  aperture-type tests) plus **104 new** in `ApertureDefinitionReuseTests.cs`.
- The new tests found two genuine defects, both fixed before commit: the construction name base was
  stripping underscores (so `SIM_EXT_GLZ` became `SIMEXTGLZ`), and an `ApertureType.Undefined` aperture was
  being refused a building element altogether.
- Licensed TAS: **not run.** See the merge gate above.

---

## Stage 1 (merged as PR #30) - retained for continuity

The subsections below describe Stage 1 as it was developed on `feature/tas-aperturetype-reuse`; "same
branch" in them means that branch, not this one.

### Stage 1 correction pass (2026-08-21, on `feature/tas-aperturetype-reuse`, commits rewritten in place)

Three focused fixes, no architecture change:

- **Exact numeric collision identity.** Display names stay rounded (`Opening Cd0.62 F1`), but
  `Query.ApertureTypeSignature` now carries the **exact IEEE-754 Single bit pattern** of Cd and factor
  (`SingleBitsHex`), so two TAS float definitions like `0.6201`/`0.6202` can never share a deterministic
  collision identity. Equality was and stays exact float equality.
- **Name reservation vs reusable registration.** `BuildingReuseCache` now separates the two:
  `ReserveScheduleName` / `ScheduleNames()` and `ReserveApertureType` hold the namespace of a created
  object whose write later fails (no `RemoveSchedule`; a created aperture type is left in place by
  policy), without ever making it reusable. `RegisterApertureType` upgrades a reservation in place,
  identified by the same COM reference. `GetOrCreateSchedule` reserves on naming; the shared
  `SetApertureType` path reserves on naming.
- **Full read-back verification of newly created shared types.** After the complete write (Cd,
  description, profile value/factor/setback/type/function, schedule, day types), the new type is read
  back through the existing seed reader and must equal the requested definition; otherwise the write
  refuses, keeping the name reserved and the object non-reusable and unassigned. Runs only for newly
  created definitions.

Files changed: `Query/ApertureTypeSignature.cs`, `Classes/BuildingReuseCache.cs`,
`Create/GetOrCreateSchedule.cs`, `Modify/SetApertureType.cs`, plus tests and docs.

Validation: `SAM_Tas.sln` 0 errors Debug + Release; `SAM.Analytical.Tas.TM59.Tests` **230/230** both
configurations (77 in `ApertureTypeReuseTests.cs`, incl. close-float collision, late-failure
name-reservation and read-back mismatch/refusal; existing schedule tests unchanged). Licensed-TAS
acceptance re-run on this machine after the hardening: 200 identical windows -> 1 ApertureType
(`Opening Cd0.395 F1 S00FFFE`) + 1 schedule + 200 assignments; repeat export -> +0/+0, no second
openings; 5 variants -> 5 distinct types; 50 windows x 2 identical children -> exactly 2 ordinal types.
No issue notes anywhere. The acceptance harness remains uncommitted (rebuilt as a scratch net481 console
per `APERTURE_TYPE_REUSE.md`); the produced `.tbd` files live under `%TEMP%\aperture-accept`.

Next step: open the Stage 1 PR (two commits: `feat(tas): reuse equivalent aperture types`,
`test(tas): validate aperture type reuse`).

### Codex review fixes (2026-08-21, on `feature/tas-aperturetype-reuse`, one commit on top of the two Stage 1 commits)

The Codex review of PR #30 raised two genuine findings; both are fixed:

- **P1 - index-derived reuse ordinal for the compatibility overload.** The
  `SetApertureType(building, buildingElement, single, out refusal, name, index)` overload forwarded a
  fixed ordinal, so two calls for two identical indexed openings both resolved to occurrence 1 and the
  second collapsed into the first's assignment. The legacy 1-based `index` now doubles as the reuse
  ordinal via the new COM-free `Query.ApertureTypeOrdinal(int)` (position is the occurrence - exact for
  identical children, conservative for different ones). The multiple-opening entry point already
  computes the true per-definition occurrence and is unaffected.
- **P2 - signed-zero hashing.** `ApertureTypeDefinition.Equals` uses float equality under which `-0f`
  and `+0f` are equal, while the signature hashed their distinct IEEE-754 bit patterns, so an equal pair
  could produce different hash codes (the .NET dictionary contract). The constructor now normalises
  signed zero to positive zero for Cd and factor, keeping `Equals`, `GetHashCode` and the deterministic
  name signature in agreement.

Files changed: `Classes/ApertureTypeDefinition.cs`, `Query/ApertureTypeDefinition.cs`,
`Modify/SetApertureType.cs`, `APERTURE_TYPE_REUSE.md`, plus tests.

Validation: `SAM_Tas.sln` 0 errors Debug + Release (CI recipe: sibling deps at `sow/2026-Q3` rebuilt
first); `SAM.Analytical.Tas.TM59.Tests` **233/233** Debug + Release (was 230/230; +3 regression tests:
`Equality_SignedZero_NormalisesBeforeEqualityAndHashing`, `Ordinal_IndexDerived_IsThePosition`,
`Export_TwoIdenticalIndexedWrites_ProduceTwoTypesAndTwoAssignments`).

### Stage 1 - what landed

- `Classes/ApertureTypeDefinition.cs` - immutable value equality over Cd, factor (after the Part O
  `AlwaysClosed -> 0` override), profile mode, function text, the 24 schedule values, description and
  day-type membership. COM-free.
- `Query/ApertureType{Definition,DefinitionTBD,Signature,Index,Name,Reconciliation}.cs` - the COM-free
  factory and ordinal keying, the seed reader (existing type -> definition, or a refusal), deterministic
  FNV-1a signature/collision hash, first-equal lookup, name derivation/decomposition/legacy-name test, and
  the reconciliation decision (Create / Reuse / Legacy / Refuse).
- `Classes/BuildingReuseCache.cs` - one COM pass over schedules, aperture types and day types; lifetime is
  one open document. Replaces two full aperture-type scans per opening child and a per-child rebuild of
  every schedule's 24 values.
- `Create/GetOrCreateSchedule.cs` - cache-taking overload only; behaviour identical, `cache: null` is the
  original byte for byte.
- `Modify/SetApertureType.cs` - the reuse path, plus `SetApertureType_Named` holding the previous
  per-element write verbatim for the legacy fence.
- Cache threaded through `Modify/SetApertureTypes.cs`, `Modify/Update.cs`,
  `Modify/UpdateBuildingElements.cs`. Every new parameter is optional and defaults to null, so all
  pre-existing call sites compile and behave unchanged.

### Stage 1 - decisions worth not re-deriving

- **S1-C0 = Outcome A (day-type membership is readable).** `TBD.IApertureType.GetDayType(int)` exists in
  the Interop.TBD metadata and licensed TAS confirms faithful read-back, including across save/reopen.
  Membership is therefore an equality field - compared **as a set**, because TAS reports it in the order
  `SetDayType` was called in, not calendar order. The conservative Outcome B policy is NOT in force.
- **Reuse writes nothing.** A shared definition is immutable; anything short of full equality creates a new
  type under a deterministic, collision-suffixed name. Proven by write-log assertions on the fakes.
- **`sheltered` is a conservative seed gate** added beyond the plan's list: SAM never writes it, so
  adopting a sheltered type would apply a shelter the model does not state. Refusing to reuse is the safe
  direction.
- **The licensed-TAS acceptance harness is not committed.** `SAM.Analytical.Tas.TM59.Tests` deliberately
  carries no COM reference (`TESTING.md`), and adding a second COM-referencing project is out of Stage 1's
  scope. The scenarios and their results are recorded in `APERTURE_TYPE_REUSE.md` so the run can be
  reproduced.

---

## Previous branch (merged): `feature/partf-terminal-transfer-compliance` (SAM_Tas#29)

The Part O availability schedule export is **complete and accepted on licensed TAS**. The mapping fix
landed in `7ef2aff3f3f81949be8b15bf6a797848c2800bf2` ("fix: correct TAS availability schedule mapping")
and the acceptance run passed with no warnings. Two further Codex findings on the TPD-full preparation
are implemented and tested. The final review cleanup (below) fixed the WorkflowCalculator stale-notes gate
and made the MultipleOpeningProperties schedule diagnostic per-child. The schedule-removal transition (D3)
remains deferred.

## TAS availability schedule export - accepted
Accepted commit: `7ef2aff3f3f81949be8b15bf6a797848c2800bf2`.

The adapter crosses two independent COM conventions, and crosses them separately:

- `Query.ScheduleValueFromTBD` - COM TRUE comes back as the VARIANT_BOOL bit pattern, so **-1 maps to 1**.
  Every other value passes through unchanged, so an unexpected read-back still fails the caller's
  comparison rather than being taken for "true".
- `Query.ScheduleIndexTBD` - TBD's 24-slot hourly indexed properties are 1-based hour-ending, so
  **SAM hour `h` is TBD slot `h + 1`** and the slots written are 1..24, never 0. Note that TBD's
  COLLECTION getters (`GetSchedule`, `GetBuildingElement`, ...) are 0-based; it is the hourly indexed
  properties, and only those, that start at 1.
- `Modify.SetScheduleValues` is the only place that writes schedule values, and it reads every written
  slot straight back through the same COM object. A mismatch is a **refusal** naming the SAM hour, the TBD
  slot, the written value, the read value and the raw COM value - the schedule is left unassigned rather
  than exporting a control that does not match the model.

Licensed acceptance result:

- 20 schedules requested, **20 read back**, **0 assignment warnings**.
- `PartO_DayOpen_08_23` visually correct in TAS: 00:00-08:00 OFF, 08:00-23:00 ON, 23:00-24:00 OFF.
- **Slot 24** verified separately with `openingHour = 23`, `closingHour = 24`.

Writing 0-based against a 1-based convention had put every hour one slot early - a 15-hour ON block from
07:00 to 22:00 instead of 08:00 to 23:00 - and writing and reading at the same wrong index masked it
completely.

## Completed (this session)
Final review cleanup - two P2 findings from a Codex review of `7ef2aff3`, verified against `296882e7`
and both confirmed:

- **`WorkflowCalculator.Calculate` stale notes.** The validation gate (`analyticalModel == null ||
  WorkflowSettings == null`) returned BEFORE `notes.Clear()`, so a re-used calculator whose next run was
  rejected still exposed the previous run's notes. Fixed by clearing at the top of `Calculate`, ahead of
  the gate. Regression: `WorkflowCalculatorTests` (3 tests; the previous run is simulated by writing the
  private notes list, the only COM-free way to populate it).
- **`SetApertureTypes` / `UpdateBuildingElements` schedule diagnostics checked only `apertureTypes[0]`.**
  For `MultipleOpeningProperties` the returned list is compacted (refused children are absent), so child
  order does not survive partial failure - checking the first entry both falsely reported a missing
  schedule when child 0 was unrestricted and child 1 was scheduled-and-delivered, and hid a later child's
  failure behind child 0's schedule. Fix: the write overload now reports the correspondence
  (`out List<int> childIndices`, parallel to the returned list), each requesting child is read back against
  the aperture type ITS write returned (`ScheduleDeliveryByChild` + `ApertureTypeSchedule`), and the
  requested/written counters count per requesting child. New COM-free seams `Query.OpeningScheduleRequests`
  and `Query.UndeliveredOpeningScheduleRequests` carry the pairing decision; strict refusal behaviour of
  `SetApertureType` is untouched. Regression: `OpeningScheduleDeliveryTests` (14 tests) covering
  Unrestricted+NightClosed, NightClosed+Unrestricted, NightClosed+NightClosed and the child-0-only failure
  modes. Summary note wording adjusted ("opening(s) requested") since counts are now per child.

Earlier this branch (`296882e7` and before):
- `ResultantTemperaturePreparation.Transferred`: a non-null but EMPTY `IndexedDoubles` zone-temperature
  series is now skipped exactly as an absent one is. It counted towards the payload before, so
  `TryBeginSecondPass` considered the transfer usable and copied the TBD, and the COM write beyond that
  seam reads `values.Count` off it - either throwing out of a route whose contract is refusal, or writing
  a default series that is then reported as a systems-aware answer. (Codex 3820973150)
- `ResultantTemperaturePreparation.TryBeginSecondPass`: a failed `File.Copy` - a locked or read-only
  `_TPDThermostat.tbd`, the routine case being a previous run still open in TAS - is now a refusal with
  the payload cleared, not an escaping `IOException`. `Modify.CalculateResultantTemperature` does not
  catch, so it would otherwise surface as a bare exception on a port that reports every other failure as
  a refusal. The design TBD is still never touched. (Codex 3815817512)
- Regression tests for both, in `PreparationBoundaryTests`:
  `AnEmptyZoneTemperatureSeries_IsNotTakenAsAUsablePayload` and
  `ACopyTargetThatCannotBeWritten_IsRefusedRatherThanThrown`.

## Final pre-merge pass (this session) - backlog triage and fixes

The rounds 1-2 Codex backlog on PR #29 was re-triaged against the current head. Implemented:

- `ApproximateResultantTemperatureMap.Synthesise`: the zone-temperature series must now cover EXACTLY
  the radiant series - the count must equal the radiant length and the maximum index must be
  `radiantCount - 1`. A longer series was previously truncated silently; a gapped series (min 0, correct
  count, a missing key) was zero-filled by the bounded read - both fabricated plausible resultant
  temperatures. (Codex 3803884880, 3796840741)
- `PartODiagnosticLog.BuildHourlyRecords`: identity fields (`designSpaceGuid`, `simulatedSpaceGuid`,
  `designZoneGuidRaw`, `simulatedZoneGuidRaw`, `identityMode`, `series`) are now set on EVERY hourly
  record, including the non-extended refusals - previously those rows could not be attributed to a flat,
  which is exactly what this logger exists to diagnose. (Codex 3804809062)

**Confirmed already fixed by earlier commits:** 3795669836 (radiant value validation - `TryGetDouble`),
3796840735 (cluster clone semantics), 3815817499 (conflict refusals surfaced to the workflow),
3815817505 (conflict check precedes every profile-control write; the method's non-transactional
`description` write is documented), 3795669830 (empty-space refusal in ToTM59).

**Classified DEFERRED / legacy / next branch:** 3802556065 (legacy approximate route treats an empty
model as supported - contract of the compatibility route; the TPD-full route refuses empty payloads),
3804809050 (scenario overload for `ToTBD` - new API; the Part O production component routes scenarios
through `ToXml` with the map directly), 3803359698 (last-wins duplicate results - documented, deliberate),
3821601792 (D3 schedule-removal transition - parked).

## Workflow diagnostics
`SAM_Tas_Grasshopper` `f023594ef3f53a9d8d2411b6fe5bc21a9b363ad0` ("chore: clean TAS workflow schedule
diagnostics") removed the verbose per-aperture D1 success commentary. A successful run is quiet; problem
lines still reach the canvas.

## Deferred
- **D3 - schedule-removal transition (Codex 3821601792).** Clearing an obsolete TBD schedule when an
  opening restriction is removed. Not implemented: `TBD.Building` exposes no `RemoveSchedule`, and
  ownership of a schedule cannot be proven across processes because reuse-by-value deliberately adopts
  either a previous export's schedule or a user-authored one. Needs its own design pass. **No transition
  code exists in this repository** - there is no `Query/ScheduleTransition.cs` and no
  `TryResolveScheduleTransition`.
- **D2 - aperture matching.** Parked. `Query.Apertures(...)` already performs the relevant `Face3D.InRange`
  geometry check, and the D2 proposal introduced a tolerance inconsistency. No D2 code exists here.

## Files changed

Profile definition reuse (this session, branch `feature/tas-profile-definition-reuse`):
- `SAM_Tas/SAM.Analytical.Tas/Classes/ProfileDefinition.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Classes/ProfileReuseIndex.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ProfileSignature.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ProfileName.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ProfileReuseIndex.cs` (new - the only COM read)
- `SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/ProfileLibrary.cs` (index overload)
- `SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/InternalCondition.cs` (index parameter + reference resolution)
- `SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/Space.cs` (index threaded)
- `SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/AdjacencyCluster.cs` (index threaded)
- `SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/AnalyticalModel.cs` (index built once, threaded to all three consumers)
- `SAM_Tas/SAM.Analytical.Tas/Modify/AddUnusedInternalConditions.cs` (index parameter - the review's uncovered path)
- `SAM_Tas/SAM.Analytical.Tas/Modify/AddUnusedConstructions.cs` (cref only)
- `SAM_Tas/SAM.Analytical.Tas/Modify/UpdateInternalConditionTemplate.cs` (crefs only)
- `SAM_Tas/SAM.Analytical.Tas/PROFILE_DEFINITION_REUSE.md` (new)
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/ProfileDefinitionReuseTests.cs` (new, +27)
- `PROJECT_PROGRESS.md` (this file)

`Convert/ToSAM/Profiles.cs` and `Convert/ToSAM/Profile.cs` are deliberately UNTOUCHED - they are the legacy
library build that `ToSAM_ProfileLibrary(TBD.Building)` still uses when no index is supplied.

Stage 1 - reusable aperture types (earlier session, branch `feature/tas-aperturetype-reuse`):
- `SAM_Tas/SAM.Analytical.Tas/Classes/ApertureTypeDefinition.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Classes/BuildingReuseCache.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Enums/ApertureTypeProfileMode.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Enums/ApertureTypeReconciliation.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ApertureTypeDefinition.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ApertureTypeDefinitionTBD.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ApertureTypeSignature.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ApertureTypeIndex.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ApertureTypeName.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Query/ApertureTypeReconciliation.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas/Create/GetOrCreateSchedule.cs` (cache overload only)
- `SAM_Tas/SAM.Analytical.Tas/Modify/SetApertureType.cs`
- `SAM_Tas/SAM.Analytical.Tas/Modify/SetApertureTypes.cs`
- `SAM_Tas/SAM.Analytical.Tas/Modify/Update.cs` (cache construction + one call site)
- `SAM_Tas/SAM.Analytical.Tas/Modify/UpdateBuildingElements.cs` (cache construction + one call site)
- `SAM_Tas/SAM.Analytical.Tas/APERTURE_TYPE_REUSE.md` (new)
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/ApertureTypeReuseTests.cs` (new, +80)
- `PROJECT_PROGRESS.md` (this file)

Final pre-merge pass (previous branch):
- `SAM_Tas/SAM.Analytical.Tas.TPD/Classes/ApproximateResultantTemperatureMap.cs`
- `SAM_Tas/SAM.Analytical.Tas.TM59/Classes/PartODiagnosticLog.cs`
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/PreparationBoundaryTests.cs` (+2)
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/PartODiagnosticLogTests.cs` (extended hourly-refusal test)
- `PROJECT_PROGRESS.md` (this file)

Final review cleanup (committed 2026-08-20 as "fix: verify opening schedules per child and clear workflow
notes per run" + "docs: record final Part O review cleanup"):
- `SAM_Tas/SAM.Analytical.Tas/Classes/WorkflowCalculator.cs`
- `SAM_Tas/SAM.Analytical.Tas/Modify/SetApertureTypes.cs`
- `SAM_Tas/SAM.Analytical.Tas/Modify/UpdateBuildingElements.cs`
- `SAM_Tas/SAM.Analytical.Tas/Query/OpeningScheduleRequests.cs` (new)
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/WorkflowCalculatorTests.cs` (new, +3)
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/OpeningScheduleDeliveryTests.cs` (new, +14)

Previous session:
- `SAM_Tas/SAM.Analytical.Tas.TPD/Classes/ResultantTemperaturePreparation.cs`
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/PreparationBoundaryTests.cs` (+2)
- `PROJECT_PROGRESS.md` (this file)

## Validation

This session (`feature/tas-profile-definition-reuse`):
- `SAM_Tas.sln` built with the VS Framework MSBuild in **Debug and Release**: 0 errors. Only the
  pre-existing MSB3270/MSB3277 and XML-doc warnings; the new files add none.
- `SAM.Analytical.Tas.TM59.Tests` Debug **484 passed / 0 failed**, Release **484 passed / 0 failed**
  (457 pre-existing and unmodified, + 27 new). `SAM.Analytical.Tas.Benchmark.Tests` 16/16 Release.
- `ModelA-Tas.sam` verified independently of the code, straight off the fixture: 42 `SAM.Analytical.Profile`
  entries, **20 distinct `(Category, flattened Values)`**, name collisions at exactly
  `Infiltration::Constant` and `Heating::HTG_7to19_21`, and no `Ventilation`-category profile at all (the
  known dangling `VentilationProfileName`). Those counts are reproduced behaviourally by
  `ModelA_FortyTwoProfilesCollapseToTwenty` / `ModelA_TheTwoNameCollisionsAreDiscriminated` from the fixture's
  own slot data, so the test does not depend on a file in a sibling repo.
- **Licensed TAS A/B run and PASSED** (2026-08-23) against baseline **`2950b27c`** (round 1) and
  **`03f97570`** (round 2, after PR #36 merged) — see "Last updated" for the table and
  `PROFILE_DEFINITION_REUSE.md` → "Licensed acceptance" for the full evidence. Note the baseline for this
  feature is `2950b27c` / `03f97570`, **not** `fff9984d`: that SHA belongs to the PR #34 gbXML aperture
  programme and is 16 commits too old here, so using it would fold the unrelated PR #34/#35 aperture
  changes into the comparison.

Earlier sessions:
- `SAM_Tas.sln` rebuilt with the VS Framework MSBuild in **Debug and Release**: 0 errors. Only the
  pre-existing MSB3270 (MSIL vs AMD64 interop) and MSB3277 (System.Memory unification) warnings.
- `SAM.Analytical.Tas.TM59.Tests` Debug: **233 passed, 0 failed**. Release: **233 passed, 0 failed**
  (153 pre-existing, unmodified, + 80 aperture-type-reuse tests - 77 Stage 1 + 3 for the Codex fixes:
  signed-zero equality/hashing, index-derived ordinal mapping, and the indexed-twins write regression).
- Licensed TAS acceptance (2026-08-21, before the Codex fixes; the fixes are COM-free seams exercised by
  the tests above - the licensed harness was not re-run for them), all pass, driven
  through the real `Modify.SetApertureTypes` -> `SetApertureType` -> `BuildingReuseCache` ->
  `Create.GetOrCreateSchedule` against a real `.tbd` created and reopened via `TBD.TBDDocument`:
  - 200 identical windows -> **1 ApertureType** (`Opening Cd0.395 F1 S00FFFE`), **1 schedule**, 200
    assignments, no issue notes;
  - repeat export into that TBD -> **0** additional types/schedules, no element gained a second opening;
  - 10 **new** elements added to the saved TBD -> **0** additional types/schedules, each adopts the
    **seeded** type (so the seed read survives save/reopen);
  - 5 control variants over 200 windows -> **5 ApertureTypes**, names distinct, none carrying aperture
    identity;
  - 50 windows x 2 identical children -> **exactly 2 ApertureTypes**, every element keeps both openings;
  - legacy per-element type -> written in place, no shared type created alongside;
  - stale shared type -> refused with the type named, Cd unchanged, no second opening, no replacement type.
- Note the documented build order: the test project references already-built assemblies by `HintPath`
  (the COM-referencing projects cannot be built by the .NET Core MSBuild), so the SAM libraries and
  `SAM_Tas.sln` must be built before `dotnet test`. See `SAM.Analytical.Tas.TM59.Tests/TESTING.md`.
  This session followed the CI recipe: sibling deps cloned at `sow/2026-Q3`, benchmark schema built,
  all dependency solutions rebuilt with the Framework MSBuild, then `SAM_Tas.sln` Debug + Release and
  `dotnet test` per configuration.

## Issues / blockers
- ~~Licensed TAS A/B outstanding for profile definition reuse~~ — **CLEARED 2026-08-23, PASSED.** The gate
  ran against baseline **`2950b27c`** (round 1) and **`03f97570`** (round 2, the current merge-base after
  PR #36), one-DLL swap, on two real models. Compared per zone / internal condition / profile slot:
  internal-condition count, internal-condition names, the profile slot, the TAS profile type, the complete
  values, `factor`, `function`, `setbackValue` — **852 and 5754 simulation-effective fields, 0 differences**
  — plus a full simulation (**227 760 and 1 024 920 hourly values, 0 differing**). SAM `Profile` counts
  **42 → 20** and **369 → 30**. The predicted diagnostic-only differences (`profile_TBD.name`,
  `profile_TBD.description`, `thermostat.name`) were the only ones. Detail in
  `PROFILE_DEFINITION_REUSE.md` → "Licensed acceptance".
  *The baseline for this feature is `2950b27c` / `03f97570`, NOT `fff9984d`* — that SHA is PR #34's gbXML
  aperture baseline, 16 commits too old, and would drag the unrelated PR #34/#35 aperture work into the diff.
- **Open, name-only, pre-existing:** the export writes one profile name onto two differently-valued TAS
  profiles (a zone's internal condition and its HDD sibling both take the space condition's profile name
  while keeping their own values). Across repeated full round trips that makes 2 of the 20 shared names
  accrete one `_<hash>` suffix per generation. The definition count never grows and no simulation value
  changes; the baseline is worse on the same measure (24 of 42 names re-nest per generation). Fixing the
  export's name/value mismatch is separate work.
- Pre-existing and deliberately not fixed *in PR #37*: `ticV` was never emitted into the imported
  `ProfileLibrary`, so `VentilationProfileName` dangled (pinned as baseline by a test, not silently
  changed). **PR #38 fixes this** - see "Last updated" above;
  the TBD function-profile import does not preserve complete function semantics, which is why zero-length
  profiles are excluded from dedup; TBD `InternalCondition` sharing is unchanged.
- Carried over from earlier branches: D3 (schedule-removal transition) and D2 (aperture matching)
  remain deferred - see **Deferred** above.

## Next step
- **PR #40 is ready for human review and merge.** Branch `fix/tas-ventilation-ticv-factor-growth`. The
  first attempt on this branch (`140acb4c`, per-person subtraction) is superseded by the metadata design
  described under "Last updated"; the PR body has been rewritten accordingly. Merging remains a human call -
  it was NOT done here.
- Do not fold the ~29% cooling-load drop (logged 2026-08-24, `NEXT_SESSION_PROMPT.md`) into PR #40. It is a
  separate design-day / solar-gain investigation on its own branch.
- Exhaust airflow is deliberately NOT in PR #40. `SAMZoneMetadata`'s payload is a JSON object so an
  `"exhaust"` section can be added beside `"ventilation"` without a version bump, when that work is scoped.
- Throwaway licensed harness: `C:/TasOut/inv` (`Inv.exe venttbd|ventsam|ventauthor|venttamper|fromtbd|togbxml|workflow`),
  artifacts in `C:/TasOut/v40`. Both are outside the repo and will not exist on another machine - rebuild
  from the `run-tas` skill if the licensed chain needs re-running.

## Previously (superseded next steps)
- The licensed A/B is done and recorded, PR #36 is merged into this branch, and
  [PR #37](https://github.com/SAM-BIM/SAM_Tas/pull/37) is open against `sow/2026-Q3` with the Copilot
  automated review addressed (see "Post-review fix (2)" below). Remaining: human review of PR #37, then
  merge. Merging remains a human call - it was not done here.
- Stage 2 (`ConstructionDefinition` + `BuildingElementDefinition`, direct-export path only) follows the
  frozen plan section E on its own branch. Do not start it inside this one.
