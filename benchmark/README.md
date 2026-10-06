# TAS benchmark producer (B2)

`benchmark-tas` runs the **native TAS route** over a SAM `AnalyticalModel` and emits an
engine-neutral benchmark document (B1a schema, `route = "Native-TAS"`) — the same shape and units
the OpenStudio producer (`benchmark-openstudio`, in SAM_OpenStudio) emits, so the two can be
compared directly by the B3 comparator.

## Layout

| Project | Framework | Purpose | Builds with |
| --- | --- | --- | --- |
| `SAM.Analytical.Tas.Benchmark` | `net8.0` | COM-free core: the C5→B1a mapping (`ToBenchmark`), `TasBenchmarkContext`, and the validate/write/exit-code decision (`Producer`). Unit-tested. | `dotnet` or MSBuild |
| `SAM.Analytical.Tas.Benchmark.Cli` | `net8.0-windows` | The `benchmark-tas` executable: argument handling (shared `BenchmarkCliHost`), the TAS run (`WorkflowCalculator`), weather/gbXML wiring. References the TAS COM interop. | **.NET Framework MSBuild only** (COM references) |
| `SAM.Analytical.Tas.Benchmark.Tests` | `net8.0` | Portable offline tests (no TAS): the mapping, a byte-stable golden JSON, and the CLI exit-code contract. | `dotnet test` |

The core/tests are split from the CLI so the mapping logic stays buildable and testable with the
.NET CLI: the TAS COM interop cannot be resolved by the .NET Core MSBuild (`ResolveComReference`),
so anything referencing `SAM.Analytical.Tas` must be built through the repository solution with the
.NET Framework MSBuild (as CI does).

The mandatory licensed-machine run (populated annual energy, non-zero space loads, schema-valid
output) is recorded in [`D10-CHECKPOINT.md`](D10-CHECKPOINT.md).

## Prerequisites (run only on a licensed EDSL Tas laptop)

- A licensed **EDSL Tas** installation with its COM servers registered. The install directory is
  read from the registry key `HKCU\Software\EDSL\TasManager\TasFiles` (value `Path`); the engine
  version is best-effort read from a Tas executable's file version there (override with
  `--engine-version`).
- The process runs the simulation on an STA thread (TAS COM requirement).

Building the executable requires Visual Studio / .NET Framework MSBuild (for the COM references);
`dotnet build` cannot build the CLI project.

## Usage

```
benchmark-tas --model <model.json> --weather <weather.epw> --tbd <run.tbd> --out <benchmark-TAS.json>
              [--gbxml <shared.xml>] [--engine-version <version>] [--timeout-seconds <n>]
              [--sam-commit <sha>] [--runner-commit <sha>]
```

- `--model` — a serialized SAM `AnalyticalModel` (JSON). Its embedded design days drive sizing.
- `--weather` — an EPW weather file.
- `--tbd` — where the run's `.tbd` is written; the `.tsd`/`.t3d`/`.json` land alongside it.
- `--out` — the benchmark JSON to write.
- `--gbxml` — the shared gbXML the TAS route imports. Reused when the file exists; otherwise
  exported from the model (so both engines can consume the same gbXML). Defaults to the `--tbd`
  path with a `.xml` extension.
- `--engine-version` — record the TAS version explicitly (EDSL exposes no version in code).
- `--timeout-seconds` — hard timeout for the simulation (default 3600). Because the TAS run is a
  blocking in-process COM call, a timeout abandons the worker thread on process exit rather than
  cancelling gracefully.

### Exit codes (shared benchmark CLI contract)

| Code | Meaning |
| --- | --- |
| 0 | Success |
| 2 | Usage error |
| 3 | Input / IO / serialization error |
| 4 | Produced document failed schema validation |
| 5 | Producer failure (the TAS run did not complete) |

## Units

TAS stores annual consumption in **Wh** and model peak loads in **W**; the mapping converts these
to the schema's canonical **kWh**/**kW**. Per-space peak/design loads are already **W** and are
emitted unscaled; hours are hour-of-year in `0..8759`. A present, finite, in-range measurement is
`available`; anything missing is `available:false` with a `null` value; a measured zero stays `0`.

## Regenerating the golden fixture

`SAM.Analytical.Tas.Benchmark.Tests/Fixtures/golden-tas-benchmark.json` is asserted byte-for-byte
against the serializer output. After an intentional mapping/schema change, regenerate and review the
diff:

```
SAM_BENCHMARK_UPDATE_GOLDEN=1 dotnet test benchmark/SAM.Analytical.Tas.Benchmark.Tests
```
