# Part O Iteration 3 — manufacturer-guidance exchanger representation (PR record)

Branch `perf/parto-exchanger-representation-2026-09-29`, from `sow/2026-Q3` `5f31f21b`
(after SAM_Tas#74 `e706a13d` and #75 `a312426b`). **Not merged.** `PROJECT_PROGRESS.md` is not touched
on this branch (closeout after merge, per `AGENTS.md`).

## Status

Implemented, unit-tested and licensed-validated. The PR is open for review. Nothing else is pending on
this branch.

## Problem

The guidance exchanger's `SensibleEfficiency` carried one (intake ODB, extract EDB2, own airflow EFlow)
equality table of **335 x 281 x 2 = 188,270 cells per unit** on the real 3-dwelling project. Every cell is
one out-of-process call to `TPD.exe`, a `LocalServer32`-only COM server with no in-process route
(~0.21-0.23 ms per call). Its write + full read-back took ~169 s of a ~297 s Iteration 3 run.

## What the table represents (`PARTO-IT3-EXCHANGER-surface.py.txt` / `.log`)

- Values: layer 1 (design airflow) holds {0, 0.8}; layer 2 (elevated airflow) holds {0, 0.8576}.
  **One bit per cell**: bypass (0) when `intake >= 12 && extract > intake && extract >= 19`, otherwise
  recovery.
- Layer 2 has exactly layer 1's pattern, so F(i, e, f) = G(i, e) x h(f). Under trilinear interpolation
  this equals bilinear(G) x linear(h) exactly in real arithmetic.
- The boundary consists of three planes. The two thresholds need only a 0.01 K step. The extract = intake
  **diagonal** crosses every 0.1 K line of the [19, 45]^2 square, so that square keeps the full lattice
  (261 x 261). Everything else is runs of identical lines: 263 distinct rows / 263 distinct columns,
  331 row transitions in total.
- A breakpoint whose whole line of states equals both neighbours is removable **exactly** under per-axis
  linear interpolation. The result is 265 x 266 = **70,490** cells, the minimum for one rectilinear table.

## Alternatives investigated

| Candidate | Exact? | Cells / calls vs 188,270 per unit | TAS-supported? | Decision |
| --- | --- | --- | --- | --- |
| Bulk / whole-table setter or getter | would be | ~1 call | **No** - `IProfileDataModifierTable` has only per-cell `SetDataValue`/`GetDataValue`, `AddPoint`, `Clear` (interop reflected) | n/a |
| In-process COM | same data | same count, ~100x cheaper per call | **No** - `TPD.Document` is `LocalServer32` (TPD.exe) only | n/a |
| Table copy / clone / share across units | same data | writes only | **No** copy/clone/share API on `ProfileData`; the units differ in design airflow (30 vs 63 l/s) | rejected |
| Lua modifier (`AddModifierLua`, `Code`) | **No** - cannot reproduce TAS's own lattice interpolation bit-for-bit; a sharp rule changes the smear hours | ~2 calls | exists in interop; runtime variable API undocumented | rejected on exactness (not probed) |
| `tpdProfileDataVariableDeltaT` axis (sharp diagonal) | **No** - changes the 0.1 K diagonal smear | ~4x4x4 cells | **No on the exchanger** - licensed: a DeltaT table on `SensibleEfficiency` makes `SimulateEx` answer "Main PlantRoom Has Errors" (`exchprobe-DT`) | rejected |
| Curve modifier | **No** (cannot represent a 2D step) | few | exists | rejected |
| Pruned grid only (3D) | yes | 140,980 (-25%) | yes | subsumed |
| Airflow factored out only (2D Equal x 1D Multiply) | yes | 94,137 (-50%) | yes (licensed) | subsumed |
| **Pruned 2D state (Equal) x 2-point airflow table (Multiply)** | **yes - licensed bit-identical** | **70,492 (-62.6%)** | **yes** | **selected** |
| Sum of block tables (Add) around the diagonal | exact only in real arithmetic; many modifiers | ~261n | untested | rejected: complexity and FP summation |

## Selected change (`SAM.Analytical.Tas.TPD/Modify/GroundGuidanceCooling.cs`)

- `GuidanceRecipe.State(i, e)` = 0 in bypass, else 1. `StateIntakes_C` / `StateExtracts_C` are
  `Intakes_C` / `Extracts_C` pruned by `Prune` (the rule above; both ends always kept). The full lattice
  stays the rule's definition.
- `WriteExchangerStateTable`: 2D (ODB, EDB2) Equal table, `SetSize(n1, n2, 0)` (licensed:
  `GetAxisSize(3)` = 0). It keeps the measured zero-skip: old-extent cells are always written, the
  far-corner probe runs, and only new zero cells are skipped.
- `WriteExchangerAirflowTable`: 1D EFlow **Multiply** table (design -> `ExtractFraction`,
  elevated -> `CoolingExtractFraction`), added second.
- Read-back (refuses on any disagreement): efficiency value; exactly 2 modifiers, both tables; the state
  table's extrapolation, multiplier (Equal), sizes (n1, n2, 0), variables (ODB/EDB2), both axes and
  **every cell**; the airflow table's extrapolation, multiplier (Multiply), variable (EFlow), size, axis
  and both fractions. Latent efficiency is checked as before. This is strictly more than the old read-back
  checked (variables and multiplier were not checked before).

## Call counts (deterministic; profiler counters + code)

| All 3 units | BEFORE | AFTER |
| --- | ---: | ---: |
| Table cells held | 564,810 | 211,470 + 6 |
| Cell writes (`SetDataValue`) | 241,074 | 106,437 + 6 |
| Cell reads (`GetDataValue`, read-back) | 564,810 | 211,470 + 6 |
| Axis writes / reads | 1,854 / 1,854 | 1,599 / 1,599 |
| **Cell + axis COM calls** | **~809,600** | **~321,100 (-60%, 2.5x fewer)** |

## Timing (NOISY - the VM's COM speed varies ~1.5x run to run; same-session pair, clean builds)

| Stage | BEFORE `run-exch2-before` | AFTER `run-exch2-after` |
| --- | ---: | ---: |
| Guidance: write exchanger table(s) | 51.1 s | 24.0 s |
| Guidance: read back exchanger table(s) | 117.9 s | 45.9 s |
| Plantroom: manufacturer-guidance cooling | 169.5 s | 70.5 s |
| TPD conversion total (`Convert.ToTPD`) | 172.4 s | 73.3 s |
| Iteration 3 SystemsConversion | 229.4 s | 132.7 s |
| **Iteration 3 total** | **296.6 s** | **198.9 s** |

The per-call cost is unchanged (0.21-0.23 ms), so the saving is the call count. An earlier pair
(`run-exch-*`, stale sibling builds) gave 312.8 -> 203.5 s.

## Functional equivalence (licensed)

1. **Probe** (`exchprobe`, a copy of the real project's generated TPD; old tables replaced in place):
   plant-room `SimulateEx` with duct data. V1 (Equal 2D, then Multiply 1D) and V2 (reverse order) both
   gave **104 of 104 duct flow/temperature series x 8760 h bit-identical** (max |diff| 0).
2. **Iteration 3 replay A/B** (`poi replay`, unchanged production pipeline; only
   `SAM.Analytical.Tas.TPD.dll` differs in source; MVIDs logged):
   - Both `IsComplete True`. Reconciliation: "8 room(s) reconcile by identity" in both. Reference A and
     Candidate B TM59: Pass / Pass in both.
   - Generated TPDs re-simulated (`tpdcmp`): **104/104 duct series bit-identical**. BEFORE exchangers:
     1 modifier 335x281x2. AFTER: [265x266x0 Equal ODB/EDB2] [2x0x0 Multiply EFlow].
   - `OperatingAirFlow.csv`: byte-identical. TM59 reports (Reference A, Candidate B): identical except the
     `Source:` path. Bridge thermostat profiles: **32/32** slot series identical; TSD resultant
     temperature: **9/9** zone series identical (max diff 0).
   - Run record JSON: identical after normalising paths, run GUID, ticks and file lengths. Review:
     all guidance operation notes and verdicts identical. The only differences are TAS file
     sizes/hashes/timestamps (these vary between runs of identical code: the no-IZAM TBD was 399,599 /
     399,540 / 399,613 bytes across three earlier identical-code runs) and the captured TPD state
     (164,598 -> 126,470 bytes, the smaller table).

## Tests

- `SAM.Analytical.Tas.TM59.Tests` Release: **1014 passed / 0 failed**. `GuidanceExchangerTableCallsTests`
  went from 5 to 14 tests. They cover: old-vs-new equivalence over 15-17M domain points (every full-lattice
  breakpoint, +/-0.005 K, midpoints, a sweep beyond both ends, 200k random points, 5 airflows incl. below
  design and above elevated) for 4 recipes (12/19/22, 15/15/24, 19/12/22, 12.35/19.07/23.5), worst
  2.2e-16 and only inside switching cells; kept breakpoints; State x fraction = the guidance; call counts;
  old-extent overwrite; non-zero-initialised table; and refusals for cells, axes, variables, multiplier,
  extrapolation and the airflow table.
- `SAM.Analytical.Tas.Benchmark.Tests`: **16 passed / 0 failed**.
- Test bin `SAM.Analytical.Tas.TPD.dll` byte-identical to the freshly built `build\` DLL.

## Files changed

- `SAM_Tas/SAM.Analytical.Tas.TPD/Modify/GroundGuidanceCooling.cs`
- `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/GuidanceExchangerTableCallsTests.cs`
- `Documentation/evidence/PARTO-IT3-EXCHANGER-*` (this record, surface analysis, probe harness source,
  probe/compare logs)

## Harness and traps (outside the repo: `C:\TasOut\partoi-2026-09-29`)

- `h\Exch.cs` (copied here as `PARTO-IT3-EXCHANGER-harness-Exch.cs.txt`): modes `exchprobe` and `tpdcmp`.
  `replay` and `bridgecmp` are the existing modes.
- **Never put a harness build output inside `h\`**: MSBuild's `{CandidateAssemblyFiles}` outranks
  `HintPath`, so `h\bin-before\*.dll` was silently used by the next build. Outputs go to
  `hbin-before` / `hbin-after` / `hbin-tools`.
- A replay against stale SAM / SAM_UI `build\` outputs refused reconciliation (the three wet rooms
  "Candidate B only") in BOTH cases. After a clean rebuild of all four repos at their `sow/2026-Q3` heads,
  both reconciled.
- `tpdcmp` keys must drop native component GUIDs (they are per document).

## Risks / unresolved

- The Multiply-after-Equal stacking order is licensed-verified on this TAS build only. The read-back pins
  the order, types, multipliers and variables, so a TAS that reorders modifiers is refused, never simulated.
- FP: the product can differ from the old trilinear value in the last bit, and only inside switching cells.
  Licensed results are bit-identical on this project.

## Remaining bottleneck (AFTER, noisy)

The exchanger table is still the largest sub-stage (read-back 45.9 s + write 24.0 s). Next are
ThermalSource (54.9 s), TAS `ISystem.Simulate` x3 (31.8 s) and the guidance-evidence `SimulateEx` (22.8 s).
Further exchanger gains need a representation without the 0.1 K diagonal lattice, which changes the
smear. That is a semantics decision for the owner, not a performance refactor.

## Next step

Review the PR. After merge: fetch `sow/2026-Q3` and add the `PROJECT_PROGRESS.md` closeout entry with the
merge SHA.
