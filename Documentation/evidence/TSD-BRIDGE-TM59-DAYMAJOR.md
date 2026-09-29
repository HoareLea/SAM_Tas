<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Bridge / TM59 / weather day-major TSD reads + full-year guard (SAM_Tas#72 follow-up)

**Status (29 Sep 2026): code + COM-free tests + read-only identity checks on real bridge TSDs (pr4h, PR4 ×10, PR4
×30 sampled) complete; PR open against `sow/2026-Q3`, awaiting review.** Branch `perf/tsd-bridge-tm59-daymajor-2026-09-29` from `sow/2026-Q3` `cc038f04`.
Follow-up to SAM_Tas#72 (merged `0f4eeadb`); the investigation, the cause and the ×30 measurements are in
[`TSD-RESULT-READ-PERFORMANCE.md`](TSD-RESULT-READ-PERFORMANCE.md) §1-§4 and §7, and are not repeated here.
No TAS simulation was run for this PR.

## 1. What changed

| Path | Before | After |
|---|---|---|
| Bridge `TPD.Query.ReadThermostatBridge` | 2 × `GetAnnualZoneResult` per bridged room (zone-major) | one day-major pass (new private `BridgeSeries`): days 1..365, every bridged zone, resultant then dry bulb; values through the **same** `AnnualSeries` conversion |
| Bridge boundary `TPD.Create.ThermostatBridge` | read the TSD's building data | calls the new `ReadThermostatBridge(SimulationData, …)` overload: **refuses unless `firstDay == 1 && lastDay == 365`**, before any result is read |
| TM59 `Convert.ToSAM_AdjacencyCluster(BuildingData, spaceDataTypes, …)` | `Space.ToSAM(zoneData, types)` → `GetAnnualZoneResult` per zone per type | one `Query.ZoneResultSeries(zones, 1, 365, arrays)` for the converted zones; each space built from its own zone's arrays (new internal `ToSAM(zoneData, types, series)`); the public single-zone `ToSAM(zoneData, types)` is unchanged |
| Weather `Weather.Tas.Query.WeatherYear(BuildingData)` | 7 × `GetAnnualBuildingResult` | days 1..365 × 7 `GetDailyBuildingResult`, each value through the same conversion (`BuildingResultValues<T>`, extracted from `AnnualBuildingResult<T>`) |
| TM59 opt-in guard | none | `TSDConversionSettings.RequireFullYear` (default **false**; serialised only when true) + `Convert.ToSAM(path/TSDDocument, settings, out refusal)`; `Query.FullYearRefusal(SimulationData)` / `(firstDay, lastDay)` |
| TSD access mode | read-write opens | read-only: `Convert.ToSAM(path_TSD, settings)`, `ToSAM_AdjacencyCluster(path_TSD)`, `Query.DesignDayNames(path_TSD)`, `Weather.Tas.Convert.ToSAM_WeatherDatas(*.tsd)` - none of them writes |

## 2. Decisions and assumptions

- **Full-year guard at the workflow boundary, never in a reader.** `ZoneResultSeries`, the annual getters and the generic
  TSD conversion keep reading any day range (a TM52/TM59 summer run is legitimate). The bridge (always a full year:
  `ThermostatBridgePlan.FirstDay/LastDay`) refuses in its own boundary; TM59 gets an **opt-in** setting because
  `Convert.ToSAM(path, settings)` is shared by Part O (SAM_UI `PartOTM59Assessment`) and Grasshopper components.
- **Why the old length checks never worked** (measured, TSD 2.0.0.1, see memory of the #72 investigation): annual
  getters return 8760 values for ANY simulation and pad hours past `lastDay` with **-1**; daily reads outside the range
  return 24 × -1; nothing throws, is null or short. So the bridge's per-room count check and TM59's
  `HourCount_Expected` (SAM_UI) passed a part-year file and read the padding as -1 °C. `SimulationData.firstDay/lastDay`
  is the only signal (no status flag exists).
- **The bridge does not call `Query.ZoneResultSeries`.** `SAM.Analytical.Tas.TPD` does not reference
  `SAM.Analytical.Tas`; adding the reference would put that assembly's same-named `Create`, `Query`, `SpaceParameter`,
  `AnalyticalModelParameter` in scope of every TPD file (the known namespace-shadowing trap, where a same-named member
  rebinds silently). `BridgeSeries` is the same loop order, kept local, and it keeps the bridge's exact
  float→double conversion (`AnnualSeries`: null/unconvertible element → NaN), so NaN, count, duplicate-zone and
  missing-zone refusals are unchanged. A daily read that throws now refuses every readable room with a reason
  (previously the exception reached `Create.ThermostatBridge`'s catch and refused the whole read).
- **Day-major everywhere in these three paths, unconditionally.** On cache-resident TSDs it is slower (~0.24 ms per
  daily call; measured §4: +1.5 s on `pr4h`, +13 s per two-array pass on ×10), on a ×30 file it is ~25-30× faster
  (minutes to tens of minutes saved). Considered and NOT adopted: an adaptive reader (annual while each annual call is
  fast, switching to day-major once one is slow). It would win both regimes (≈ 3 s at ×10; ~12 s detection penalty
  at ×30) with identical values, but it makes the access path depend on wall-clock timing and needs its own
  licensed validation, for a saving of < 30 s on runs that take tens of minutes. A file-size threshold was rejected
  for the same reason: the cliff is only bracketed (fits at 163 MB / 90 zones, thrashes at 489 MB / 270 zones).
  Recorded as a possible follow-up (§6).
- **Days 1..365 regardless of the simulated range** in the TM59 and weather conversions, so a part-year file converts
  to exactly what the annual getters gave (padding included) - identity, not a behaviour change.
- Not changed (per scope): `UnmetHours`, recirculation-cooling reads, peak APIs, cross-stage caching, persistent TSD
  sessions, Mixed Part O architecture, TM59 rules, SAM_UI.

## 3. Files changed

- `SAM.Analytical.Tas.TPD/Query/ReadThermostatBridge.cs` - `SimulationData` overload + `FullYearRefusal(int, int)`;
  day-major `BridgeSeries`.
- `SAM.Analytical.Tas.TPD/Create/ThermostatBridge.cs` - boundary calls the guarded overload.
- `SAM.Analytical.Tas/Convert/ToSAM/AdjacencyCluster.cs`, `Convert/ToSAM/Space.cs` - TM59 day-major pass; read-only open.
- `SAM.Analytical.Tas/Convert/ToSAM/AnalyticalModel.cs` - `out refusal` overloads, `RequireFullYear`, read-only open.
- `SAM.Analytical.Tas/Classes/TSDConversionSettings.cs` - `RequireFullYear`.
- `SAM.Analytical.Tas/Query/FullYearRefusal.cs` (new).
- `SAM.Analytical.Tas/Query/DesignDayNames.cs`, `SAM.Weather.Tas/Convert/ToSAM/WeatherDatas.cs` - read-only open.
- `SAM.Weather.Tas/Query/WeatherYear.cs`, `Query/AnnualBuildingResult.cs` - day-major weather.
- Tests: `ThermostatBridgeTests.cs` (stand-in answers daily reads; 3 new tests), `TsdFullYearReadTests.cs` (new, 9
  tests), `TsdResultFakes.cs` (counted annual getters, daily building reads, `LastHour` padding), test csproj
  (references `SAM.Weather.Tas`, `SAM.Architectural`, `Interop.TAS3D` for the compiler).

## 4. Validation

**COM-free tests** (`dotnet test SAM_Tas/SAM.Analytical.Tas.TM59.Tests -c Debug` after `MSBuild SAM_Tas.sln`):
**990 / 990** (978 before + 12 new). Before the stand-in was updated, exactly the 3 bridge tests whose stand-in only
answered `GetAnnualZoneResult` failed - i.e. the reader no longer calls it. New tests cover: bridge day-major order
(day → plan-order zone → resultant, dry bulb) and whole-year values; part-year bridge refused from `SimulationData`
for (1,2) (1,364) (2,365) (0,365) (1,366) before building data is asked for, while the same padded file passes every
length check without the guard; a failing daily read refuses every readable room; TM59 conversion equals the annual
values zone for zone with **zero** annual reads; single-zone `ToSAM` and the space-name filter unchanged; a part-year
TSD converts with its -1 padding and is NOT refused by default; `FullYearRefusal`; `RequireFullYear` accepts 1..365,
refuses (1,364)/(1,2) before any zone or weather read, default converts; setting copy/serialisation; generic
`ZoneResultSeries` still reads partial ranges; weather day-major order, zero annual reads, JSON-identical
`WeatherYear` for a full and a part-year file. Existing duplicate / missing-zone / NaN / short-series /
achieved-air bridge tests unchanged and green.

**Identity + timing on real bridge TSDs** (this laptop, licensed TSD.exe 2.0.0.1, read-only copies, source hashes
unchanged, this branch's build, no simulation; probe and full log in `tsd-bridge-tm59-daymajor/`). OLD = the annual
getters the previous code called; NEW = this branch's production code (bridge `BridgeSeries`, TM59 `Convert.ToSAM`
with `RequireFullYear`, `Weather.Tas.Query.WeatherYear`); identity = IEEE bits of every compared value.

| Bridge TSD | Bridge read OLD → NEW | Weather OLD → NEW | NEW TM59 conversion (open, 2 series, zones, weather) | Identity |
|---|---|---|---|---|
| `pr4h` 16.4 MB, 9 zones | 0.31 s → 1.81 s | 0.045 s → 0.70 s | 8.5 s | bridge 0 of 157,680 differ; TM59 0 of 157,680; weather JSON identical |
| PR4 ×10 cooled 163 MB, 90 zones | 2.68 s → 15.87 s | 0.10 s → 0.65 s | 28.3 s | bridge 0 of 1,576,800; TM59 0 of 1,576,800; weather identical |
| PR4 ×30 cooled 489 MB, 270 zones | **2.5 s per series** (8 sampled series 20.0 s → ~22 min for all 540) → **48.1 s for all 540** | **17.7 s → 2.6 s** | **92.5 s** (OLD reads alone: 540 series ≈ 22 min) | 4 sampled zones: bridge 0 of 70,080; TM59 0 of 70,080; weather identical (all 7 × 8760) |

- Small and ×10 files: the decoded year fits TSD.exe's cache, so day-major is **slower** (≈ 6×, +13 s per two-array
  pass at ×10) - the known trade (§2). ×30: the cliff shows on this laptop too (2.5 s per annual series against
  ~0.03 s at ×10); day-major removes it (~25-30× on the bridge read).
- The ×30 OLD reference is sampled over 4 evenly spaced zones: the full zone-by-zone read is the path being removed.
  (Its log label prints "270 zones"; the time is for the 4 sampled zones.)
- `FullYearRefusal` accepted all three files (days 1..365).

**Previously measured large-file evidence - NOT rerun in this PR** (SAM_Tas#72 record §4, ×30 PR4 model, 483 MB
bridge TSD, 270 zones): bridge + TM59 series (resultant, dry bulb, occupant gain) read day-major **28.1 s** against
~126 min zone by zone; weather **46.1 s → 7.1 s**; both bit-identical against the annual getters.

## 5. TSD read audit (repo-wide, production code)

Classified against the proven failure pattern - **many zones/surfaces × full-year reads** on a TSD whose decoded year
exceeds TSD.exe's ~550 MB day cache.

| Path | Pattern | Class |
|---|---|---|
| Bridge `ReadThermostatBridge` | 2 annual × bridged room | **FIX NOW** (this PR) |
| TM59 `ToSAM_AdjacencyCluster` → `Space.ToSAM` | annual × zone × type | **FIX NOW** (this PR) |
| Weather `WeatherYear` | 7 building annual | **FIX NOW** (this PR) |
| `Modify.AddResults` / `ToSAM_Results` | day-major since #72 | LEAVE (done) |
| `Query.YearlyValues` → `UnmetHours(path, …)` | array → day → zone: each array one day-major year walk (2 walks) | LEAVE (cache-friendly; ~3 % measured) |
| `UnmetHours(BuildingData, …, filterByExternalTemperature: true)` | hourly building read per exceedance hour, zone after zone (re-walks the year per zone) | LEAVE - no production caller found (only the path overload, which does not filter) |
| `RecirculationCoolingResults` | 8760 hourly building reads in hour order (each day decoded once) | LEAVE (out of scope; already day-ordered) |
| `Panel.ToSAM(surfaceData, panelDataTypes)` via `ToSAM_AdjacencyCluster(…, panelDataTypes)` | annual × surface × type | **FOLLOW-UP / EDSL-DEPENDENT**: same shape as the failure pattern (surfaces outnumber zones ~5-10×), but only when panel types are requested - Part O passes none; Grasshopper `SAMAnalyticalFromTSD` can. `ISurfaceData.GetDailySurfaceResult` exists in Interop.TSD, but whether surface results share the zone day cache and are bit-identical day-major is **not measured** |
| `Modify.SetBlinds` | `ToSAM_AdjacencyCluster(null, {ExternalSolarGain})`: annual × every surface | FOLLOW-UP (same as above); no caller found in SAM_Tas / SAM_UI / SAM_Tas_Grasshopper |
| `New/PartitionSimulationResult(surfaceData, types)`, `New/SpaceSimulationResult(zoneData, types)` | annual × surface/zone | LEAVE - no production caller with types |
| `Modify/New/AddResults` (BuildingModel) | `TryGetMax` → `GetPeakZoneGroupGains` per zone group | FOLLOW-UP (peak API; the #72 series rule could apply; BuildingModel route not on Part O) |
| `ToSAM_AnalyticalModelSimulationResult` | 2 building annual (heating/cooling profile) | LEAVE (≤ 2 year walks; Grasshopper only) |
| `MaxValueDictionary`, `ValueDictionary` (`GetPeakZoneGains`), `SpaceLoadPeak`, `Create.SpaceSimulationResult`, `Results` design-day/peak-hour hourly reads | building-wide peak calls / single hours | LEAVE (per-zone cost is one day or one call) |
| TSD opens | read-only everywhere results are only read | FIX NOW for the 4 read-write opens above; LEAVE `SAMTSDDocument` itself |

## 6. Open items

- **Activating the TM59 guard needs a one-line SAM_UI change** (`PartOTM59Assessment`: `RequireFullYear = true` and
  report the `out refusal`), in a separate SAM_UI PR. Until then Part O TM59 behaves as before. SAM_UI's comment on
  `HourCount_Expected` should also say the length check cannot detect a part-year TSD.
- EDSL questions: (1) is there any supported multi-zone/multi-array series getter (none found; `OutputSelection`/
  `ZoneFilter` coclasses fail to CoCreate); (2) can the day-cache cap be raised; (3) do surface results share the zone
  day cache, i.e. is `GetDailySurfaceResult` day-major the right fix for surface reads; (4) is a status flag for an
  incomplete / stopped simulation available beyond `firstDay/lastDay`.
- Possible follow-up (only if the mid-size regression matters): the adaptive annual/day-major reader of §2.
- Follow-up: day-major surface reads (`Panel.ToSAM` with panel types, `SetBlinds`) - needs a licensed probe of
  `GetDailySurfaceResult` cost and identity first (§5).
- Not in scope: damaged-TSD timeout handling (a TAS/EDSL robustness issue, separate).
- **Next step**: review and merge this PR into `sow/2026-Q3`; then the `PROJECT_PROGRESS.md` closeout on the base
  branch; then the SAM_UI `RequireFullYear` adoption PR.
