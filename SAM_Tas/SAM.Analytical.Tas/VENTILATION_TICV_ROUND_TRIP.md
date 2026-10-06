# The ventilation `ticV` factor grew on every round trip

`TBD → FromTBD → new SAM model → ToGbXML → WorkflowgbXML → a NEW TBD`, repeated.

Reported from a real Grasshopper run: the ventilation profile **values** were identical across generations,
but the **factor** kept climbing. Unbounded — each generation added the same constant again.

---

## The distinction the fix rests on

> **SAM ventilation properties describe an engineering airflow REQUIREMENT. They do not prescribe HOW that
> airflow is realised in TAS.**

SAM deliberately lets an engineer state a supply-air requirement on four simultaneous bases, which
`Query.CalculatedSupplyAirFlow(Space)` **sums** into one required total:

| basis | unit |
|---|---|
| `SupplyAirFlow` | m3/s |
| `SupplyAirFlowPerArea` | m3/s/m2 |
| `SupplyAirFlowPerPerson` | m3/s/p |
| `SupplyAirChangesPerHour` | ACH |

That flexibility supports different standards and MEP workflows, including ASHRAE-style additive area and
person components. It says nothing about delivery. The requirement may end up realised through a TBD Internal
Condition **Ventilation** profile, an **IZAM**, **Tas Systems**, or another explicit workflow.

What TAS holds is narrower:

- **`InternalGain.freshAirRate`** — Outside Air, l/s/person. Feeds Part L / EPC and is available to Tas
  Systems. It does **not** itself supply air to the thermal zone in the TSD simulation.
- **`ticV.factor`** — an air change rate, following a profile, entering the dynamic thermal simulation and
  the AHU load. This is Building Simulator mechanical ventilation, and it exists only where a Ventilation
  profile has been assigned.

That `freshAirRate` is not a supply path was **measured on this codebase**, independently and before this
work: PR #37's licensed A/B set it to 40 vs 0 l/s/p on a real model and found **0 differences in 227,760
simulated hourly values** (the SAM_Tas project record, "Zero-length ticV guard" entry). It is inert in a TBD
simulation. That is why the per-person basis must reach `ticV` when a Ventilation profile is what realises
the requirement — and why holding it in both fields is not a double count.

So TAS can hold the per-person rate and, where a profile realises the requirement, one air change rate. It
cannot hold the four-basis **decomposition**.

---

## Root cause: a total written into one basis, and read back as that basis

`Modify.UpdateInternalCondition` wrote the whole summed total into `ticV.factor`. `Convert.ToSAM` read that
factor back into the single `SupplyAirChangesPerHour` basis. So:

```
import:  SupplyAirChangesPerHour := ticV.factor          <-- the whole previous TOTAL, into ONE basis
export:  ticV.factor := sum(all four bases)              <-- which re-adds the other bases
```

The figure the export produced last time became one of the ingredients summed into the figure it produced this
time. **A feedback loop**, adding the other bases' contribution once per generation, for ever.

Measured on the licensed 9-zone residential fixture (`Bedroom 2_3`, volume 420 m3, area 105 m2,
`AreaPerPerson` 52.5 → occupancy 2, `freshAirRate` 8 l/s/p → a 0.137 ACH per-person term):

| generation | `SupplyAirChangesPerHour` imported | `ticV.factor` exported |
|---|---|---|
| 1 | 1.000 | 1.137143 |
| 2 | 1.137143 | 1.274286 |
| 3 | 1.274286 | 1.411… |

The corridor, bathroom and ensuites state no `AreaPerPerson`, so their per-person term is `NaN`, they were
never inflated, and they sat at a stable 1.00 ACH throughout. That difference is itself the fingerprint.

---

## The fix

### 1. `ticV.factor` is the FULL requirement — and only where a profile chose that realisation

`Query.VentilationAirChangesPerHour(Space)` is `CalculatedSupplyAirFlow / volume * 3600` — every basis,
per-person included. A Ventilation profile assigned to the internal condition is a deliberate statement that
the Building Simulator delivers the required air, so it has to deliver **all** of it. Leaving a basis out
would under-ventilate the zone in the simulation by exactly that term.

`freshAirRate` holding the same per-person rate is **not** a Building Simulator double count: it is the
Outside Air field, which does not itself supply the zone in the TSD.

The gate in `Modify.UpdateInternalCondition` is unchanged and load-bearing:

```csharp
profile = internalCondition.GetProfile(ProfileType.Ventilation, profileLibrary);
profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticV);
if (profile_TBD != null)
{
    double value_Temp = Query.VentilationAirChangesPerHour(space);
    ...
    if (profile != null)          // <-- the ONLY thing that activates mechanical ventilation
        Update(profile_TBD, profile, value_Temp);
}
```

**The mere existence of SAM airflow data never activates a TBD Ventilation profile.** The explicit seam is
`SAMAnalytical.UpdateVentilationProfile`, which assigns the profile name when one is plugged in and removes it
when one is not.

### 2. `SAMZoneMetadata` — the authored decomposition crosses the seam

A versioned, SAM-only section of `TBD.zone.description`, the string the exporter already uses for `[Id]` and
`[LevelName]`:

```
[Id]=1234; [LevelName]=Level 01; [SAM_META_V1]={"ventilation":{"flowPerArea":0.0004,"flowPerPerson":0.008,"airChangesPerHour":1,"profile":false},"native":{"freshAirRate":8}}
```

- **Versioned** in the marker: a section this build does not recognise is left to the native TAS import
  rather than guessed at.
- **One parser/writer.** `SAMZoneMetadata.Compose` / `.Parse` own the whole string. `Compose` rewrites the
  segments it manages, **preserves anything it does not** (a TAS user's own note in the zone description now
  survives an export, which the previous unconditional overwrite did not allow) and appends its section last.
  `[Id]` and `[LevelName]` come from the space where it states them and are otherwise **kept from the existing
  description** — the import reads neither back onto the space, and they previously survived only by accident,
  because a space stating neither left the description untouched.
- **Deterministic**, invariant-culture numbers, fixed key order — a re-export of an unchanged model produces
  an unchanged file.
- **No derived geometry.** Area and volume belong to the TBD zone; a second copy here could only go stale.
- **Extensible.** The payload is a JSON object, so exhaust can be added beside `"ventilation"` later without
  a new version. Exhaust is deliberately **not** in this PR.

Deliberately **not** `InternalCondition.description` — that carries the NCM activity name, has real TAS
meaning, and is already intentionally preserved.

### 3. Import restores the authored bases, and does not activate anything

`Modify.RestoreVentilationRequirement` replaces what the native import inferred:

- the four bases come back as authored — a basis the export did **not** record is **removed**, not left
  holding the value inferred from the total (that removal is what actually breaks the feedback);
- where the metadata records `"profile": false`, the `VentilationProfileName` the native import writes from
  the mere presence of a `ticV` slot — which every TBD internal condition has — is **removed again**. Without
  that, a round trip carrying only engineering data would switch Building Simulator ventilation on by itself.

### 4. Stale transport data is refused, not trusted

The metadata records the native TAS fields as the export left them: `freshAirRate` always, and `ticV.factor`
**only** where SAM deliberately applied a Ventilation profile (where it did not, a value there is TAS's own
and is not evidence of anything). If TAS no longer states those, the file was edited after SAM wrote it: the
whole section is refused, the native import stands in full, and a note is stamped on the space
(`"SAM Zone Metadata Note"`).

This is a mismatch check, not a conflict-resolution engine.

### Without metadata, import only what TAS states

A TAS-authored TBD is unchanged: `freshAirRate` becomes `SupplyAirFlowPerPerson`, a genuine `ticV.factor`
becomes `SupplyAirChangesPerHour`. **No decomposition is invented from a total.**

---

## Licensed acceptance

Real `.tbd` files, driven through `Convert.ToSAM` → `TogbXML` → `WorkflowCalculator.Calculate` (the engine
`SAMAnalytical.WorkflowgbXML` calls), on the 9-zone residential fixture.

### Chain B — an explicit Ventilation profile, three generations

Authored bases: `SupplyAirChangesPerHour` 1.0 and `SupplyAirFlowPerPerson` 0.008 m3/s/p, Ventilation profile
assigned and resolved.

| zone | volume | required total | `ticV.factor` gen 1 → 2 → 3 | `freshAirRate` | restored `ach` / `flowPerPerson` |
|---|---|---|---|---|---|
| `Bedroom 2_3` | 420 | 0.132667 m3/s = **1.137143 ACH** | 1.137143 → 1.137143 → 1.137143 | 8.0 | 1.0 / 0.008 |
| `Bedroom 2_6` | 420 | 1.137143 ACH | 1.137143 → 1.137143 → 1.137143 | 8.0 | 1.0 / 0.008 |
| `Studio 1_0` | 300 | 1.192 ACH | 1.192 → 1.192 → 1.192 | 8.0 | 1.0 / 0.008 |
| `Living Kitchen_4` | 300 | 1.192 ACH | 1.192 → 1.192 → 1.192 | 8.0 | 1.0 / 0.008 |
| `Kitchen_7` | 300 | 1.048 ACH | 1.048 → 1.048 → 1.048 | 8.0 | 1.0 / 0.008 |
| `Corridor_1` | 1464 | 1.000 ACH | 1.000 → 1.000 → 1.000 | 8.0 | 1.0 / 0.008 |
| `Bathroom_2`, `Ensuite_5`, `Ensuite_8` | | 1.000 ACH | 1.000 → 1.000 → 1.000 | 8.0 | 1.0 / 0.008 |

Baseline on the same starting model: **1.137143 → 1.274286 → 1.411…**, climbing by the per-person term every
generation.

Each imported model is byte-for-byte the same ventilation state as the one before it: `ach=1`,
`flowPerPerson=0.008`, `ventProfileResolved=True`, `requiredACH=1.137143`. The **authored** basis returns, not
the total.

### Chain C — requirement data, no Ventilation profile, two generations

`SupplyAirFlowPerArea` 0.0004, `SupplyAirFlowPerPerson` 0.008, `SupplyAirChangesPerHour` 1.0, profile removed
(the `SAMAnalytical.UpdateVentilationProfile` opt-out).

```
DESC|[SAM_META_V1]={"ventilation":{"flowPerArea":0.0004,"flowPerPerson":0.008,"airChangesPerHour":1,"profile":false},"native":{"freshAirRate":8}}
ZONE|Bedroom 2_3|freshAirRate=8|ticVfactor=1|ticVtype=ticValueProfile
```

- `ticV.factor` is **not** written — it holds the T3D/TBD default, untouched, in both generations;
- all three authored bases return on import, unchanged;
- `ventProfileName` is **absent** after import, so the next export activates nothing;
- `freshAirRate` still carries 8 l/s/p for Part L / Tas Systems;
- the model's calculated requirement is identical across generations (`requiredACH=1.497143` on
  `Bedroom 2_3`), it is simply not being realised by the Building Simulator.

### Stale metadata, on a real file

`TBD1.tbd` re-opened and `freshAirRate` changed to 12 l/s/p on all 18 internal conditions, then imported:

```
NOTE|SAM zone metadata ignored: TAS states freshAirRate = 12, the export recorded 8. The file was edited
after SAM wrote it, so the recorded SAM airflow bases are no longer known to describe this zone - imported
what TAS states instead.
SPACE|Bedroom 2_3|flowPerPerson=0.012|ach=1.137143|ventProfileName=Ventilation
```

Conservative fallback to the native import in full, and the discrepancy is visible in the model.

### Programme invariants, all three generations of each chain

`40 aperture part(s) considered; 40 rebound onto a shared definition, 0 already on one` — unchanged from
PR #39. Profile reuse, Part F and Part O untouched. No design-day change is included.

---

## What was NOT done, and why

An earlier revision of this fix **subtracted** the per-person term from the factor, on the assumption that
`freshAirRate` supplies it to the Building Simulator independently. That made the round trip stable, but by
changing the physical total: with a Ventilation profile chosen as the realisation, the zone would have been
simulated with less air than the requirement states. `freshAirRate` is Outside Air — reporting and Systems —
not a supply path in the TSD, so the two fields do not double count. The fix is to preserve the
decomposition, not to change the total.

---

## Tests

`VentilationRequirementMetadataTests` (new, COM-free) and `VentilationAirflowMagnitudeTests` (updated):

- requirement only, no profile: all four bases survive the seam and no `ticV` behaviour is activated;
- an explicit profile: the factor is the full requirement, per-person included, and generation 1 = 2 = 3;
- **the control** — the same chain with the metadata removed compounds 2.44 → 3.16 → 3.88, so the tests fail
  if the mechanism is disabled;
- per-person only, with and without a profile;
- a TAS-authored model with no section: native import untouched, and no diagnostic;
- a malformed or future-versioned section falls back rather than guessing;
- `[Id]`, `[LevelName]` and unrelated description text all survive `Compose`; the section round-trips
  deterministically and in invariant culture (checked under `fr-FR`);
- a changed `freshAirRate` refuses the section and says so; a changed `ticV.factor` refuses it **only** where
  SAM authored that factor;
- a basis the export did not record is removed, not left as inferred.
