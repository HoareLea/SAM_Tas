# B2 / D10 — TAS producer live checkpoint

This records the mandatory B2 licensed-machine run: the `benchmark-tas` CLI driving the real EDSL
Tas engine end-to-end and emitting a schema-valid `Native-TAS` benchmark document with populated
model annual energy and non-zero space loads.

## Run

- **Date:** 2026-07-24
- **Machine:** licensed EDSL Tas install (`C:\Program Files\Environmental Design Solutions Ltd\Tas\`), engine executables present (TAS3D/TBD/TSD/…).
- **Build:** `SAM.Analytical.Tas.Benchmark.Cli` built with VS 2022 Framework MSBuild (the .NET Core CLI cannot resolve the TAS COM references) → `benchmark-tas.exe`. This confirms the CLI (Program.cs) compiles against the real TAS/SAM APIs.
- **Model:** `HungaryHouse-WeahterDDY.sam` — a real Revit-derived SAM `AnalyticalModel` (13 spaces incl. conditioned rooms + unconditioned voids), carrying embedded design days. (The minimal synthetic `SingleBox` fixture is **not** TAS-legal — TAS rejects its placeholder constructions with "Building element has an illegal construction assigned to it" — so a real model was used.)
- **Weather:** `USA_MA_Boston-Logan.Intl.AP.725090_TMYx.2004-2018.epw`. This is geographically mismatched to the model; it was used only to exercise the pipeline with a valid EPW. The producer records the supplied weather's identity/hash authoritatively (it is what TAS simulated).
- **Command:**
  ```
  benchmark-tas --model HungaryHouse-WeahterDDY.sam --weather <boston>.epw \
                --tbd hungary.tbd --out benchmark-TAS-hungary.json \
                --sam-commit <sha> --runner-commit <sha>
  ```
  (Local dev DLLs are unstamped, so the two commit SHAs are passed explicitly; CI stamps them into `AssemblyInformationalVersion` automatically.)
- **Result:** exit `0`, `state = Success`, document validated by the B1a validator, run duration ≈ 73 s.

## Acceptance checks

| B2 / D10 requirement | Result |
| --- | --- |
| TAS-laptop run | ✅ real EDSL Tas engine, 13-space house |
| Schema-valid JSON | ✅ passed `BenchmarkValidator` (exit 0, else exit 4) |
| Populated model annual energy | ✅ heating **3763.9 kWh**; peak heating **3.88 kW** @ hour 223 |
| Non-zero space loads | ✅ e.g. `GF_07 LivingRoom` heating peak **1020.6 W** @ hour 1441, design **1059.9 W**, unmet **83 h** |
| Conditioning pairing exercised | ✅ thermostats + IZAMs + TBD sizing produced per-space peak loads, design loads and unmet hours |

Other observed correctness (validates the review fixes on real data):

- **Per-space matching by relation** — all conditioned spaces resolved their heating/cooling results even though a TAS result's `Reference` is the TAS zone GUID, not the SAM space GUID.
- **Source filter / provenance** — `resultSources = ["SAM.Analytical.Tas"]` only.
- **Design-day source derived** — `designDaySource = "EmbeddedModel"` (the model carries design days), and sizing consequently produced design loads.
- **Units** — model consumption/peaks in kWh/kW, per-space loads in W.
- **Available vs measured-zero** — unconditioned voids emit `peakLoad` as `available:false`/`null` while a genuine zero (cooling under this weather) stays `value:0, available:true`.

## Not yet done (out of scope for the producer)

- **Numeric conditioning-equivalence vs OpenStudio.** A head-to-head comparison of the same model + **same weather** across TAS and OpenStudio (the D10 "pairing validated" intent) belongs to the **B3 comparator** and requires a matching EPW for both engines. This checkpoint validates the TAS producer and route mechanics; the cross-engine number alignment is B3's deliverable.
- Cooling was a measured **0** for this model under Boston weather (heating-dominated); a warmer EPW would exercise the cooling path.
