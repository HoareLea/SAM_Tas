# Internal-gain magnitude authority

A TBD internal-gain profile carries **two independent things**:

| TBD member | Carries | SAM counterpart |
| --- | --- | --- |
| `profile.factor` | the **magnitude** — the engineering quantity, in that slot's unit (W/m², ACH, …) | the internal-condition parameter (`LightingGainPerArea`, `InfiltrationAirChangesPerHour`, the per-area occupancy gain, …) |
| `profile.hourlyValues` / `value` / yearly values | the **schedule shape** — when, and in what proportion, the magnitude applies | the `Profile` definition in the model's `ProfileLibrary` |

What TAS simulates is their product, hour by hour: `factor × values[h]`.

The export has always written them separately — `Modify.Update(profile_TBD, profile, magnitude)` sets
`profile_TBD.factor = magnitude` and copies the SAM `Profile`'s **raw** values across untouched. The
import must therefore read them back separately too: the shape via `SAM.Core.Tas.Query.Values` (raw), the
magnitude via **`SAM.Analytical.Tas.Query.GainMagnitude`** — which is `profile.factor`, and nothing else.

> **The rule.** `TBD.profile.factor` is the authored magnitude. `profile.GetExtremeValue(true)` is
> `factor × max(values)` — the peak of the *effective* curve. They coincide only when the schedule is
> normalised to a peak of 1.0. Never use the extreme where the magnitude is meant.

## The defect this states the rule against

`Convert.ToSAM(TBD.InternalCondition, …)` read `GetExtremeValue(true)` for seven slots. Because the
export then writes that number back as the next generation's factor while re-applying the same values,
one round trip was

```
G(n+1) = G(n) × max(values)
```

— a fixed point only for a peak-1.0 schedule, and an **unbounded geometric decay** for anything else.

### Occupancy: why it decayed, and what actually decayed

Occupancy has an extra step. The import does not store a per-area occupancy gain; it derives an
occupancy density from the per-area gains and the metabolic rate:

```
gainPerArea       = sensiblePerArea + latentPerArea
areaPerPerson     = personGain / gainPerArea
sensiblePerPerson = sensiblePerArea × areaPerPerson    (= personGain × sensible / (sensible + latent))
```

The per-person split is a **ratio**, so it is scale-invariant: the authored 75 W/p and 55 W/p came back
unchanged in every generation, which is exactly why the defect was invisible in the obvious place.
`areaPerPerson` is not scale-invariant. Under-read `gainPerArea` by the schedule peak `p` and
`areaPerPerson` inflates by `1/p`, the occupancy thins by `p`, and the next export writes

```
factor(n+1) = sensiblePerPerson × occupancy(n+1) / area = factor(n) × p
```

**The quantity that decayed was the occupancy, not the gain per person.** Measured below: a 75 m² kitchen
authored at 2 occupants held 0.5 after one round trip, 0.125 after two, 0.031 after three — while its
75 W/p and 55 W/p never moved.

For lighting / equipment / pollutant / infiltration the SAM parameter *is* the per-area magnitude, so the
same wrong read produced the same `× p` recurrence one step more directly.

## Measured — licensed 3-generation chains

Seed `A0.sam` (9-space TM59 residential), identical weather (Belfast TRY) in every generation,
`Convert.ToSAM → TogbXML → WorkflowCalculator.Calculate → new TBD`, sizing on. Space `Kitchen_7`, whose
occupancy schedule peaks at **0.25**; its lighting, equipment and infiltration schedules peak at 1.0.

Generation 0 is the seed model exported straight to TBD, so `A0` is the authored truth every later
generation should reproduce.

### Chain A — the model as authored (free-running, upper limit 100 °C, so cooling sizes to 0)

| generation | OSG factor | OLG factor | occupancy (p) | m²/person | heating (W) | cooling (W) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| **before** A0 | 2.000000 | 1.466667 | 2.000 | 37.5 | 3421.470 | 0 |
| **before** A1 | 0.500000 | 0.366667 | 0.500 | 150.0 | 3421.475 | 0 |
| **before** A2 | 0.125000 | 0.091667 | 0.125 | 600.0 | 3421.472 | 0 |
| **before** A3 | 0.031250 | 0.022917 | 0.031 | 2400.0 | 3421.474 | 0 |
| **after** A0 | 2.000000 | 1.466667 | 2.000 | 37.5 | 3421.470 | 0 |
| **after** A1 | 2.000000 | 1.466667 | 2.000 | 37.5 | 3421.475 | 0 |
| **after** A2 | 2.000000 | 1.466667 | 2.000 | 37.5 | 3421.472 | 0 |
| **after** A3 | 2.000000 | 1.466667 | 2.000 | 37.5 | 3421.474 | 0 |

Ratio before the fix: exactly ×0.25 per generation, on both slots, in both directions of the derivation.
`personGain` (130 W/p) and the per-person split (75 / 55) are identical in every row of the table, before
and after — they were never the thing that moved. The residual ±0.003 W on heating is TAS's own float
noise; it is present before the fix too and is unrelated to gains (the heating design day runs on the
`" - HDD"` condition, which carries no occupancy gain).

### Chain C — same model with a flat 24 °C cooling setpoint, so cooling can respond

The seed free-runs, which makes it blind to internal gains on the cooling side. Chain C is the identical
model with one variable changed — every space's internal condition given a 24 °C cooling profile — so the
design **cooling** load actually moves.

| generation | OSG factor | OLG factor | heating (W) | cooling total (W) | cooling, `Kitchen_7` (W) |
| --- | ---: | ---: | ---: | ---: | ---: |
| **before** C0 | 2.000000 | 1.466667 | 3421.470 | 9555.511 | 618.187 |
| **before** C1 | 0.500000 | 0.366667 | 3421.475 | 9527.084 | 591.463 |
| **before** C2 | 0.125000 | 0.091667 | 3421.472 | 9519.948 | 584.747 |
| **before** C3 | 0.031250 | 0.022917 | 3421.474 | 9518.110 | 583.055 |
| **after** C0 | 2.000000 | 1.466667 | 3421.470 | 9555.511 | 618.187 |
| **after** C1 | 2.000000 | 1.466667 | 3421.475 | 9555.460 | 618.183 |
| **after** C2 | 2.000000 | 1.466667 | 3421.472 | 9555.525 | 618.184 |
| **after** C3 | 2.000000 | 1.466667 | 3421.474 | 9555.501 | 618.188 |

Before: cooling decays monotonically and converges as the gain goes to zero — −0.39 % on the building
total over three generations, **−5.7 % on the one zone whose occupancy schedule is not normalised**. The
building total is small because eight of the nine zones carry peak-1.0 schedules and were never affected;
the per-space column is the honest measure of the defect's size.

After: stable to ±0.07 W in 9555 W (7 × 10⁻⁶ relative), the same float noise the heating column already
showed before the fix.

### What else the fix changes, and what it does not

- Every gain line of the TBD snapshot is byte-identical across A1/A2/A3 and C1/C2/C3 after the fix, and
  identical to generation 0 — so `G0 = G1 = G2 = G3`, not merely `G1 = G2 = G3`.
- The SAM-side gain dumps (`snapgains`) are byte-identical across generations after the fix.
- The occupancy schedule is untouched: `1 Bed Apt. Kitchen Occupancy` still has max 0.25, min 0 and the
  same 24 values in every generation, before and after.
- The model's profile library holds 29 GUIDs / 27 names in every generation, before and after — no
  proliferation, no loss of reuse.
- **One value-level difference worth knowing about.** A slot holding a constant-zero `ticValueProfile`
  with `factor = 1` (the `ticELG` "Equipment Latent Gain" placeholder in this model) now imports its
  magnitude as 1 W/m² rather than 0, against a schedule of all zeros. The effective gain is 0 either way,
  the TBD round-trips it as a fixed point either way, and the measured loads are unchanged — but the SAM
  parameter reads 1 where it used to read 0.

## Why the schedule is not normalised instead

Rescaling the shape to a peak of 1.0 and folding the peak into the factor would preserve the effective
curve too — and would be wrong. Profile definitions are **shared**: `Query.ProfileReuseIndex` keys them on
their raw values precisely so that two internal conditions carrying the same activity reference one
definition. The peak is a property of that shared shape; the magnitude is a property of the individual
slot. Normalising would mutate a reusable definition on behalf of one of its users, and would silently
rewrite the authored schedule the model round-trips.

## The historical −28.9 % cooling drop — closed as non-reproducing

The `−28.9 %` cooling-load drop logged in the SAM_Tas project record on 2026-08-24 was measured on an earlier
branch state, before PR #41 (design-day weather authority) or this fix existed. Its signature was
different from this defect — one step change between generation 1 and 2, then an exact fixed point, versus
this decay's continuous geometric ×0.25 every generation — so it was never claimed to be the same bug.

With both fixes applied, the same production regression model the 28.9 % figure was measured on
(`SAM_daily/2026-08-05-PartO/SAM_zoningAM_v2.sam`) was re-run through the identical 3-generation chain.
Every `GAINP` line is byte-identical from generation 1 onward, and total heating is stable to 1.5 × 10⁻⁶
relative across three generations. **No generation-to-generation load step reproduces on the current
workflow.**

This does **not** prove which of the two fixes produced how much of the original 28.9 % — both defects
were live on the branch that number came from, and isolating each one's exact contribution to it was not
attempted, nor is it needed: the question that matters is whether the current workflow is stable, and it
is. The historical figure is retired as non-reproducing; a fresh investigation is warranted only if a
future run of `sow/2026-Q3` reproduces a material generation-to-generation load difference.

## Where this is enforced

- `SAM.Analytical.Tas.Query.GainMagnitude(TBD.profile)` / `(TIC.profile)` — states the rule once.
- `Convert.ToSAM(TBD.InternalCondition, …)` and `Convert.ToSAM(TIC.InternalCondition, …)` — every
  magnitude-carrying slot goes through it: `ticI`, `ticLG`, `ticOSG`, `ticOLG`, `ticESG`, `ticELG`,
  `ticCOG`. `ticV` already read `profile.factor` directly (PR #40) and is unchanged; the thermostat slots
  (`ticLL`, `ticUL`, `ticHLL`, `ticHUL`) carry setpoints in their values and no magnitude at all, so they
  are out of this seam entirely.
- `SAM.Analytical.Tas.TM59.Tests/InternalGainMagnitudeTests.cs` — COM-free fixed-point coverage at
  schedule peaks 1.0 / 0.5 / 0.25 for every affected slot, running the real import and export against the
  managed TBD stand-ins in `TasProfileFakes.cs`. Reverting the one-line change turns 11 of them red.

Related: `DESIGN_DAY_WEATHER_AUTHORITY.md` (PR #41), the `ticV` block in
`Convert/ToSAM/InternalCondition.cs` (PR #40).
