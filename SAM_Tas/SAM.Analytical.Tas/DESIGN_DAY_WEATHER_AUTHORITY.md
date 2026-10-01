# The first TBD of a new weather sized on the old weather's design days

`TBD → FromTBD → new SAM model → ToGbXML → WorkflowgbXML → a NEW TBD`, with a **different weather file**
selected on the workflow component.

Reported symptom: generation 1 still sized against the previous weather's design days; generation 2
re-derived the correct ones. `B1 != B2`, `B2 == B3`.

Measured, reproduced and fixed. The numbers below are from a licensed 3-generation run on the 9-space TM59
residential regression model, sizing-only, driven through the real `WorkflowCalculator.Calculate`.

---

## The distinction the fix rests on

> **A model's `CoolingDesignDays` / `HeatingDesignDays` parameters are DERIVED state, not authored state.**

`Convert.ToSAM(path_TBD, …)` does not read the design days stored in the TBD it is importing. It reads the
**weather year**, and computes the design days from it:

```csharp
Weather.WeatherData weatherData = Weather.Tas.Convert.ToSAM_WeatherData(building);
result.UpdateWeather(weatherData, [weatherData.CoolingDesignDay()], [weatherData.HeatingDesignDay()]);
```

So a model that came back out of a Weather-A TBD carries Weather-A design days, always, and by
construction. They are a *cache of the weather*, not an engineering decision.

The one place a design day IS an engineering decision is `SAMAnalytical.DesignDays`, which lets an engineer
override the heating design day's temperature / relative humidity / wind speed. That component's outputs are
wired into `SAMAnalytical.WorkflowgbXML`'s `coolingDesignDays_` / `heatingDesignDays_` inputs, and arrive as
`WorkflowSettings.DesignDays_Cooling` / `DesignDays_Heating`.

---

## Root cause: two independent reads of "what weather is this"

`WorkflowCalculator.Calculate` resolved the weather and the design days from **different sources**, with no
relationship between them:

```csharp
// weather: the workflow's own setting wins
analyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out WeatherData weatherData);
if (WorkflowSettings?.WeatherData != null) weatherData = WorkflowSettings.WeatherData;

// design days: the MODEL wins unless the caller stated them outright
analyticalModel.TryGetValue(AnalyticalModelParameter.HeatingDesignDays, out SAMCollection<DesignDay> heatingDesignDays);
if (WorkflowSettings?.DesignDays_Heating != null) heatingDesignDays = …;
```

Pick a new weather file on the component and leave `coolingDesignDays_` / `heatingDesignDays_` unwired — the
ordinary way to change weather — and the run installed Weather B in the TBD while writing Weather A's design
days next to it. `Query.Sizing` then called `tBDDocument.sizing(0)`, which sizes on exactly those design
days.

The next generation imported that TBD, read Weather B out of it, re-derived Weather B design days, and
agreed with itself from then on. Hence `B1 != B2 == B3`.

`Convert.ToTBD` (the direct, non-gbXML export) had the identical seam.

### The same root cause with no weather change at all

A model authored outside TAS carries **no** design-day parameters. `AddDesignDays` was gated on
`coolingDesignDays != null || heatingDesignDays != null`, so the first TBD generated from such a model got no
design days whatsoever — and TAS sizes a building with no design days to a **zero load** in every zone. That
is the `A0` row below.

---

## The rule

`Query.DesignDays_Authoritative` states it once, and both export paths call it:

> **A run that states its own weather makes that weather authoritative over every weather-derived design day
> the caller did not state outright.**

- Design days passed explicitly (`WorkflowSettings.DesignDays_*`, the `ToTBD` arguments) are engineering
  intent and always win.
- A run that states **no** weather of its own leaves the model's design days alone — the model's weather is
  then the only weather there is, and its design days were derived from exactly that weather.
- The model is the fallback for a slot the authoritative weather could not fill.

Nothing is discarded unnecessarily: with unchanged weather the derivation is deterministic and reproduces
the imported design days **bit for bit** — see the A-chain below, where the pre-fix and post-fix TBDs are
byte-identical apart from object GUIDs.

---

## Measured: unchanged weather (Weather A = `cibseweather2005.twd`, Belfast TRY)

`HDD sig` / `CDD sig` are SHA-256 prefixes over all 24 hours × 7 weather series of the design day as stored
in the TBD — names alone are not evidence.

### Before

| generation | weather | CDD | day | CDD sig | HDD | day | HDD sig | heating (W) | cooling (W) |
|---|---|---|---|---|---|---|---|---:|---:|
| A0 | A | **none** | – | – | **none** | – | – | **0** | **0** |
| A1 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | 3421.4747 | 0 |
| A2 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | 3421.4722 | 0 |
| A3 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | 3421.4735 | 0 |

### After

| generation | weather | CDD | day | CDD sig | HDD | day | HDD sig | heating (W) | cooling (W) |
|---|---|---|---|---|---|---|---|---:|---:|
| A0 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | 3421.4704 | 0 |
| A1 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | 3421.4747 | 0 |
| A2 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | 3421.4722 | 0 |
| A3 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | 3421.4735 | 0 |

A1/A2/A3 are unchanged by the fix — the post-fix TBDs are byte-identical to the pre-fix ones once object
GUIDs are set aside. A0 stopped being unsized and joined the fixed point.

Cooling is 0 in every row because this model's normal internal conditions are free-running (`UL = 100 °C`);
that is the model, not the sizing.

---

## Measured: weather changed at generation 1 (Weather B = `CIBSE Weather 2021.twd`, Leeds_TRY)

Weather A's heating design day is a flat **−6.6 °C** on year-day 363; Weather B's is a flat **−5.9 °C** on
year-day **67**.

### Before — B1 is the odd one out

| generation | weather | CDD | day | CDD sig | HDD | day | HDD sig | heating (W) | cooling (W) |
|---|---|---|---|---|---|---|---|---:|---:|
| A0 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | – | – |
| B1 | B | **Belfast** TRY ANN CLG | **213** | **`8A7DBA613A31993E`** | **Belfast** TRY ANN HTG | **363** | **`BBBF68CA31F32633`** | **3373.9921** | 0 |
| B2 | B | Leeds_TRY ANN CLG | 213 | `0F309F87993E5CAA` | Leeds_TRY ANN HTG | 67 | `6EA7939C351031BE` | 3278.0699 | 0 |
| B3 | B | Leeds_TRY ANN CLG | 213 | `0F309F87993E5CAA` | Leeds_TRY ANN HTG | 67 | `6EA7939C351031BE` | 3278.0712 | 0 |

B1 oversized heating by **+95.9 W, +2.93 %**, uniformly across every sized zone:

| zone | B1 (W) | B2 (W) | B1/B2 |
|---|---:|---:|---:|
| Bathroom_2 | 1173.386 | 1139.873 | 1.0294 |
| Ensuite_5 | 1216.935 | 1182.221 | 1.0294 |
| Ensuite_8 | 983.672 | 955.976 | 1.0290 |

That ratio is the design temperature difference and nothing else: with a 16 °C bathroom setpoint,
`(16 − −6.6) / (16 − −5.9) = 1.0320`. A uniform scaling, on the one input that changed.

Diffing the full B1 and B2 snapshots confirms the attribution — every internal condition, thermostat,
profile, day type, zone geometry and IC assignment is identical between them; the design days are the only
sizing-relevant difference. (One unrelated occupancy-gain difference appears in both this diff and the
A-chain diff — see the separate defect noted at the end.)

### After — B1 is already right

| generation | weather | CDD | day | CDD sig | HDD | day | HDD sig | heating (W) | cooling (W) |
|---|---|---|---|---|---|---|---|---:|---:|
| A0 | A | Belfast TRY ANN CLG | 213 | `8A7DBA613A31993E` | Belfast TRY ANN HTG | 363 | `BBBF68CA31F32633` | 3421.4704 | 0 |
| B1 | B | Leeds_TRY ANN CLG | 213 | `0F309F87993E5CAA` | Leeds_TRY ANN HTG | 67 | `6EA7939C351031BE` | 3278.0742 | 0 |
| B2 | B | Leeds_TRY ANN CLG | 213 | `0F309F87993E5CAA` | Leeds_TRY ANN HTG | 67 | `6EA7939C351031BE` | 3278.0699 | 0 |
| B3 | B | Leeds_TRY ANN CLG | 213 | `0F309F87993E5CAA` | Leeds_TRY ANN HTG | 67 | `6EA7939C351031BE` | 3278.0712 | 0 |

`B1 == B2 == B3` on every design-day signature, and the loads agree to 4.3 mW on 3278 W - **1.3 parts in
10^6**, the same spread the A chain shows and the same spread the pre-fix `B2`/`B3` pair showed between
themselves. Post-fix `B1` lands on the value the chain previously only reached at generation 2.

Diffing post-fix `B1` against `B2` leaves no design-day difference at all - not a name, not a year-day, not
an hourly value.

### The residual ~1 part in 10^6

Every chain above holds its loads to about 1.3e-6, never exactly. That is accounted for, not waved away:

- TAS sizing is **deterministic** - the pre-fix and post-fix `A1` TBDs, built from the same model on
  different builds of this library, are byte-identical including every load digit.
- Diffing consecutive generations with object GUIDs set aside leaves **exactly one** differing input: the
  occupancy-gain decay described below. Everything else - weather hash, design-day signatures, every internal
  condition, thermostat, profile, day type, zone geometry and IC assignment - is identical.

So the residual is that one input perturbing TAS's iterative solve, far below its convergence scale, and not
a design-day effect.

---

## What the design days are NOT

Two things that were suspected and measured clean, recorded so they are not re-investigated:

- **The HDD/CDD internal conditions are not weather-derived.** `Modify.AddInternalCondition_HDD` /
  `UpdateInternalCondition_HDD` build the `" - HDD"` companion from the space's own internal condition -
  its heating setpoint, infiltration rate and emitter properties. Nothing in them reads the weather, and
  they are identical across every generation of both chains. No `" - CDD"` companion is created at all: the
  normal condition is assigned to every day type except `HDD`, so it is what the cooling design day sizes on.
- **`Query.PrimaryInternalConditionIndex` is not involved.** It picks the normal condition over its
  design-day companions on import, and did so correctly throughout. It is re-pinned by a test in this work
  only because this is the first change to the design-day seam since that rule was stated.

One further observation, deliberately left alone: `Modify.AddDesignDays` calls `building.ClearDesignDays()`
and then tries to reuse an existing TBD design day by name. The clear runs first, so the reuse branch is
unreachable and every generation gets a fresh design-day object with a new GUID even when the values are
identical (visible in the tables above only as changing GUIDs). Nothing keys off that GUID, so this is
cosmetic; changing it would mean touching how design days are cleared, which no invariant here requires.

---

## Separate, still open: occupancy gains decay 4× per round trip

Found while isolating the load difference above, **not** fixed here because it is a different seam.

`Modify.Update(profile_TBD, profile, factor)` writes the per-area gain into `profile_TBD.factor` and the
normalised schedule into the hourly values. `Convert.ToSAM` reads it back with
`profile_TBD.GetExtremeValue(true)`, which is `factor × max(hourlyValues)`. For a schedule that peaks at
1.0 that round-trips exactly; for one that peaks lower it multiplies the gain by that peak, every generation.

Measured on the `1 Bed Apt. Kitchen Occupancy` profile, whose schedule peaks at 0.25:

| generation | `ticOSG` factor | `ticOLG` factor |
|---|---:|---:|
| A1 | 0.500000 | 0.366667 |
| A2 | 0.125000 | 0.091667 |
| A3 | 0.031250 | 0.022917 |

Exactly ÷4 per generation, unbounded, on both the sensible and latent occupancy gains. Same *class* as the
`ticV` factor growth fixed in PR #40 (a value written into one field and read back as if it were a different
field), different slot and opposite direction. It does not move the sizing loads here because this model's
kitchen zone sizes to zero, but it would move any cooling result that depends on occupancy gains.

It is **not** the `-28.9%` cooling drop logged in the SAM_Tas project record: that one steps once at
generation 1 -> 2 and is then a fixed point (`LC2 == LC3` exactly), whereas this decays again at every
generation. They were tracked as two separate items; both are now resolved (this one by PR #41, the
occupancy-gain decay by PR #42 - see `INTERNAL_GAIN_MAGNITUDE_AUTHORITY.md`), and re-running the historical
`-28.9%` chain with both fixes applied no longer reproduces a generation-to-generation load step.
