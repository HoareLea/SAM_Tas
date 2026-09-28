<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# TSD result-read performance (post-PR4 investigation)

**Status (28 Sep 2026): code + tests + licensed evidence complete; PR open, awaiting review. Case C: a SAM_Tas access-pattern fix (this PR, `AddResults`) plus a measured, not-yet-applied follow-up for the bridge and TM59; the remaining ceiling is TSD.exe's day cache.**
Branch `perf/tsd-daymajor-reads-2026-09-28` from SAM_Tas `sow/2026-Q3` `5753ad2`. Isolated from the Part O mixed
programme: PR4 (SAM_UI#137) is closed and not reopened; no Part O architecture or SAM_UI change.

## 1. The cause (measured, not inferred)

TSD.exe (64-bit, file version 2.0.0.1) decodes results **one simulated day at a time** and caches decoded days, capped
at roughly **550-570 MB** private memory. Probe `cost` mode, read-only copies of the PR4 thermal-source TSDs:

| Call | ×10 (161 MB, 90 zones) | ×30 (483 MB, 270 zones) |
|---|---|---|
| `GetDailyZoneResult`, first touch of a day | 5-11 ms | 8-18 ms |
| same day, any other zone or array | 0.3 ms | 0.2 ms |
| `GetAnnualZoneResult`, first | 2,866 ms (fills the cache: 466 MB) | 4,394 ms |
| `GetAnnualZoneResult`, **repeat of the same series** | **36 ms** | **8,077 ms** |
| `GetAnnualPeakZoneResult` / `GetAnnualSumZoneResult` (one zone) | 37 ms | 7,987 / 8,182 ms |
| `GetPeakZoneGains` (all zones) / `GetPeakZoneGroupGains` (one group) | 44 / 36 ms | 8,173 / 8,137 ms |
| `GetAnnualBuildingResult` (one weather array) | 36 ms | 8,095 ms |

At ×10 the decoded year fits in the cache; at ×30 it does not, so **every call that walks the whole year - for one zone
or for the building - evicts the days it needs next and decodes the year again, ~8 s each** (TSD.exe one core at 100 %,
private memory pinned at ~568 MB; RAM, disk and SAM are not involved). 365 daily calls for one zone cost the same as
one annual call (PR4: 22.4 s per zone for 3 arrays) because both are one year of decodes. This is the 112× / 175×
growth PR4 saw; the cliff lies between ~90 and ~270 zones on these models.

**Reading day-major removes it**: for each day, every zone and every array for that day, then the next day - each day
decoded once. ×30, all 270 zones × 3 arrays (resultant, dry-bulb, occupant sensible gain): **31.7 s** (39 ms per zone
array, 295,650 COM calls), against ~8 s per zone array (~100 min) read zone by zone; **bit-identical** to
`GetAnnualZoneResult` (0 of 9 series checked differ, float bits).

## 2. The current read pattern

| Stage (×30 cooled, PR4) | TSD | Calls | Per | ×30 time |
|---|---|---|---|---|
| Thermal source `Modify.AddResults` → `Convert.ToSAM_Results` | thermal-source | `GetPeakZoneGains` ×2 (cooling, heating) | building | ~16 s |
| | | `Query.Overheating`: 365 × `GetDailyZoneResult` × 3 arrays | **zone** (270), zone-major | ~30.5 min |
| | | `GetHourlyZoneResult` ~20-60 at the peak hours; design-day zone data; surface results at the peak | zone | small (one day each) |
| `AddResults` zone loop → `TryGetMax` | thermal-source | `GetPeakZoneGroupGains` | **SAM zone** (120) | ~16 min |
| Thermostat bridge `ReadThermostatBridge` | bridge | `GetAnnualZoneResult` resultant + dry-bulb | **bridged room** (180) | ~52 min |
| TM59 `Convert.ToSAM(TSD, {Resultant, OccupantSensibleGain})` | bridge | `GetAnnualZoneResult` ×2 | **every TSD zone** | ~74 min |
| | | weather: `GetAnnualBuildingResult` ×7 | building | ~1 min |

Stage totals from the PR4 log: thermal source "Adding Results" 3,038 s; bridge 1:21:06 of which the TSD was written
29 min in, so ~52 min are the reads; TM59 1:15:41. **TSD reads are ~178 of the 273 minutes.**

Duplicates: the resultant temperature of the 180 bridged rooms is read twice from the bridge TSD (bridge, then TM59).
Each stage opens its TSD once; the cache does not survive a close (private memory back to 8 MB on the next open), so
session reuse across stages is not a lever.

## 3. What Part O / TM59 actually consumes

Traced through SAM_UI, SAM_Tas, SAM_Systems (file:line in the investigation notes of the PR description):

| Result family (written by `AddResults`) | Part O production route |
|---|---|
| SpaceSimulationResult Cooling/Heating: loads, gains, `DesignDayPeak`/`AnnualPeak`, overheating hours | **unused** - serialized only (`.sam`, `.json`, provenance fingerprint); read by Room Data Sheets (off for Part O) and user ribbon reports |
| SurfaceSimulationResult | unused (serialized only) |
| ZoneSimulationResult (`MaxSensibleLoad` from `GetPeakZoneGroupGains`) | unused (read only in SAM_Revit) |
| `UpdateDesignLoads` | independent of `AddResults` (TBD zone loads); backfills `ZoneGuid`, which bridge and TM59 need |
| TPD conversion | reads zone loads through TPD's own `AddTSDData`, not SAM results |
| Bridge: resultant + dry-bulb per bridged room | **required** |
| TM59: resultant + occupant sensible gain per zone, weather | **required** |

So the ~51-minute `AddResults` stage is entirely generic enrichment on the Part O route. PR4 estimated ~43 min of it as
`Overheating` + zone-group peaks; measured: 270 × 6.8 s + 120 × 8.1 s ≈ 46.7 min of the 50.6 min.

## 4. Alternatives benchmarked (×30 unless stated)

| # | Approach | Result | Correct? |
|---|---|---|---|
| A | Current production `AddResults` | ×10 27.7 s; ×30 **2,992 s** (production log 3,038 s) | reference |
| B | One zone at a time, all its arrays together | no gain: `Overheating` already reads a zone's 3 arrays per day; a second array of the same zone right after the first still costs 8.1 s | - |
| C | Building-wide calls | `GetPeakZoneGains` answers all 270 zones in one 8 s year-walk - cheap *per zone*; but no building-wide per-zone *series* call exists (`GetSumZoneResultForMultipleZones` sums; batched `GetPeakZoneGroupGains` returns one column) | yes |
| D | Annual instead of 365 daily | ×10 4× faster (call overhead); ×30 identical (22.4 s per zone both): the cost is decoding, not calls | bit-identical |
| E | Reuse one opened TSD/session | each stage opens once; the cache is evicted within one read and cleared on close - nothing to reuse | - |
| F | In-memory cache in SAM_Tas | within `AddResults` no series is read twice; across bridge → TM59, 180 resultant series (~24 min) are; moot once reads are day-major | - |
| G | Skip unused families | would remove all of `AddResults` on Part O, but changes what the persisted Part O model holds (and its provenance fingerprint) - an owner decision in SAM_UI; after the day-major fix at most the remaining ~85 s at ×30 - no longer worth a Part O change on performance grounds | - |
| **DM** | **Day-major reads** (this PR, `AddResults`) | ×10 27.0 s (no cliff to remove); ×30 **85.4 s (35×)** | **bit-identical**: 7,260 / 2,420 results and every relation (§6) |
| DM | Day-major for TM59 + bridge series (not in this PR) | ×30 bridge TSD, 270 zones × resultant, dry-bulb, occupant gain (everything TM59 and the bridge read): **28.1 s** vs ~126 min today (540 + 360 annual reads × 8.1 s) | bit-identical vs `GetAnnualZoneResult` (sampled) |
| DM | Day-major weather (7 building arrays) | 46.1 s → 7.1 s (`GetDailyBuildingResult`) | bit-identical, all 7 × 8760 |
| H | Parallel reads | not attempted: one shared TSD.exe COM server (reused across client processes, one core), one cache - no documented thread safety | - |

## 5. The change (SAM.Analytical.Tas)

- **new** `Query.ZoneResultSeries(zoneDatas, firstDay, lastDay, arrays)`: day-major read; same `GetDailyZoneResult`
  calls and the same 8760-hour layout `Overheating` always built.
- `Query.Overheating(ZoneData, …)` unchanged in signature and values; now reads through `ZoneResultSeries` and
  delegates to a **new** `Overheating(float[] occupancy, float[] resultant, float[] dryBulb)` (does not modify its input).
- `Convert.ToSAM_Results(SimulationData)`: reads every zone's overheating series in one day-major pass (internal overload
  takes them).
- `Modify.AddResults(SimulationData, …)`: one day-major pass for the overheating arrays and, for a full-year simulation,
  the cooling load; each SAM zone's peak from **new** `Query.TryGetPeakZoneGroupGain` instead of `GetPeakZoneGroupGains`.
  The TSD call stays for a partial year, a zone guid answered twice in the TSD, or duplicate references in the group.
- The group-peak rule, measured against TSD and exact (value bits and hour) on 163 groups of the ×10 bridge TSD (103 with
  a positive peak, up to 90 zones) and 106 reordered multi-zone groups: **single-precision sum in the order given**, first
  hour strictly above 0 and every earlier hour, 1-based; 0 at hour 0 when nothing is positive. A double sum fails 41 of
  163; a building-order sum fails 32 of 106.

Not changed: `GetPeakZoneGains` (two building calls), hourly peak reads, surface results, `Modify/New/AddResults`
(BuildingModel; gets the `ToSAM_Results` change only), bridge, TM59, weather, SAM_UI.

## 6. Validation

Licensed, production `Modify.AddResults` on the PR4 run model's `AdjacencyCluster` and read-only TSD copies; A = the
`sow/2026-Q3` build, B = A with only `SAM.Analytical.Tas.dll` swapped (every other DLL hash-identical); runs serial
(TSD.exe is one COM server shared by all clients). Full numbers: `tsd-read-performance/probe-results.txt`.

| TSD | A | B | Results | A vs B |
|---|---|---|---|---|
| ×10 thermal source (90 spaces, 40 SAM zones) | 27.7 s (rerun 25.6 s) | 27.0 s | 2,420 | identical |
| ×30 thermal source (270 spaces, 120 SAM zones) | **2,992.4 s** | **85.4 s** | 7,260 | identical |
| ×10 bridge (20 of 40 SAM zone peaks positive, ~4.7 kW) | 26.2 s | 22.6 s | 2,420 | identical |

"Identical": every result serialised guid-free is the same text apart from its creation `DateTime` (which also differs
between two A runs - the noise floor), and every space / panel / zone has the same number of related results. Values
covered: overheating hours, max/min dry-bulb, design-day and annual peaks, gains, surface results, zone peaks and
their hour, zone identity (`Reference` = TSD zone guid).

Tests: `TsdDayMajorReadTests` (8 new): day-major call order and values; partial-range layout; `Overheating` from the
series equals the zone read and leaves the series alone; the group-peak rule (single-precision order dependence, first
hour on a tie, 0 at 0, refusals); `AddResults` takes a full-year zone peak without calling TSD, and keeps the TSD call
for a partial year. **Red first**: the full-year zone-peak test fails on the previous build (it asks TSD); the
partial-year test passes on both. `SAM.Analytical.Tas.TM59.Tests` 978/978; `SAM.Analytical.Tas.Benchmark.Tests` 16/16.

TM59 outcome, overheating metrics and cooled/uncooled reference results on the Part O route are **not** touched by this
PR (TM59 and the bridge read their own series; `AddResults` output is not consumed there, §3). No TAS simulation was
rerun.

## 7. Open items and next step

- **Estimated ×30 run**: 4:33:29 → ~3:44 with this PR (−49 min, "Adding Results" 50.6 min → ~1.4 min). With the
  follow-up below as well: ~1:40 (bridge reads ~52 min → <0.5 min, TM59 reads ~74 min → <1 min). What remains is TAS
  itself: shading 708 s, building simulation 115 s, TPD conversion ~21 s per air system (1,226 s), TPD simulation
  ~28 min, bridge TBD simulation ~29 min.
- **Follow-up (not in this PR, needs an owner decision)**: read the bridge (`ReadThermostatBridge`) and TM59
  (`Convert.ToSAM(TSD)` → `Space.ToSAM` → `GetAnnualZoneResult`) series through `Query.ZoneResultSeries`; weather
  through `GetDailyBuildingResult`. Measured 28.1 s and 7.1 s, bit-identical on the real ×30 bridge TSD. Held back
  because both routes' refusals ("a full year or the space is refused", incomplete bridge series) currently depend on
  the LENGTH `GetAnnualZoneResult` returns; a day-major read always yields 8760 hours, and how TSD answers daily calls
  for a damaged file is not measured. Decide the damaged-file semantics first (e.g. keep one annual call per file as a
  length check - 8 s - or refuse on any daily-call failure).
- Not measured: a TSD bigger than ×30 (the day-major cost is per day decoded, ~linear in file size, so ~5,000 spaces
  would be minutes, not hours - but the cache cap, TAS simulation and TPD conversion then dominate).
- EDSL questions: see the PR description.
- **Next step**: review and merge this PR into `sow/2026-Q3`; then the `PROJECT_PROGRESS.md` closeout on the base branch;
  then decide the bridge/TM59 follow-up.
