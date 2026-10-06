// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Tas.Benchmark
{
    /// <summary>
    /// <c>benchmark-tas</c>: the B2 TAS benchmark producer. Loads a SAM AnalyticalModel, runs the
    /// native TAS route (shared gbXML → T3D → TBD → simulate/size via SAM_Tas's own
    /// <see cref="WorkflowCalculator"/>), reads the engine-neutral SAM results, and emits a B1a
    /// schema-v1 benchmark document (<c>route = Native-TAS</c>) to the SAME canonical units and shape
    /// as the OpenStudio producer (B1b), so the two are directly comparable. The two provenance
    /// hashes are computed with the B1a helpers exactly as the schema requires: <c>sourceFileHash</c>
    /// over the raw model bytes and <c>canonicalModelHash</c> over the neutral SAM model BEFORE any
    /// TAS translation.
    /// <para>
    /// Argument parsing, invariant-culture, exception mapping and exit codes are delegated to the
    /// shared benchmark CLI host (<see cref="BenchmarkCliHost"/>): <c>0</c> success, <c>2</c> usage,
    /// <c>3</c> input/IO/serialization, <c>4</c> validation, <c>5</c> producer failure.
    /// </para>
    /// <para>
    /// Runs only on a licensed EDSL Tas laptop (the TAS COM servers must be registered). The
    /// simulation is a blocking in-process COM call, so the hard timeout abandons the worker thread
    /// on process exit rather than gracefully cancelling it (see <see cref="RunWithTimeout"/>).
    /// </para>
    /// </summary>
    public static class Program
    {
        private static readonly Regex CommitPattern = new Regex("^[0-9a-f]{7,64}$", RegexOptions.CultureInvariant);

        private static readonly string[] RequiredOptions = { "model", "weather", "tbd", "out" };

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromHours(1);

        [STAThread]
        public static int Main(string[] args)
        {
            return Run(args, Console.Out, Console.Error);
        }

        /// <summary>
        /// Testable entry point: the shared host drives parsing, culture, help and exception mapping;
        /// <see cref="Execute"/> carries the producer logic. Writers are injectable so CLI tests can
        /// capture the output without touching the process <see cref="Console"/>.
        /// </summary>
        internal static int Run(string[] args, TextWriter standardOutput, TextWriter standardError)
        {
            return BenchmarkCliHost.Run(
                args,
                Usage,
                RequiredOptions,
                (arguments, _) => Execute(arguments, standardOutput),
                standardOutput,
                standardError);
        }

        private static int Execute(BenchmarkArguments arguments, TextWriter standardOutput)
        {
            // Path validation throws mapped exceptions (input/IO): a missing input is FileNotFound,
            // a bad output directory is DirectoryNotFound. The TBD is written by the run, so only its
            // directory needs to exist — validate it like an output path.
            string modelPath = BenchmarkCliPaths.ValidateInputFile(arguments.RequireOption("model"));
            string weatherPath = BenchmarkCliPaths.ValidateInputFile(arguments.RequireOption("weather"));
            string tbdPath = BenchmarkCliPaths.ValidateOutputFile(arguments.RequireOption("tbd"));
            string outputPath = BenchmarkCliPaths.ValidateOutputFile(arguments.RequireOption("out"));
            TimeSpan timeout = arguments.GetTimeout() ?? DefaultTimeout;

            // The TAS run writes the TBD (deleted first by RemoveExistingTBD), its stem sidecars
            // (.tsd/.t3d/.json/.timing.csv) and the benchmark output; when no --gbxml is supplied it
            // also writes the auto-derived <tbd>.xml. Reject up front any --tbd/--out that would make
            // one of those clobber the input --model: the CLI only writes its own artefacts, never
            // the caller's source model. A caller-supplied gbXML is reused, never written over.
            string autoGbxmlPath = string.IsNullOrWhiteSpace(arguments.GetOption("gbxml")) ? Path.ChangeExtension(tbdPath, ".xml") : null;
            string overwriteCollision = Producer.FindInputOverwriteCollision(modelPath, tbdPath, outputPath, autoGbxmlPath);
            if (overwriteCollision != null)
            {
                throw new IOException("The TAS run/output path '" + Path.GetFullPath(overwriteCollision) + "' would overwrite the input model. Choose a different --tbd or --out path.");
            }

            // Provenance hashes (B1a helpers, exactly per SCHEMA.md "Canonical model hashing").
            string sourceFileHash = BenchmarkHash.ComputeSha256(File.ReadAllBytes(modelPath));

            AnalyticalModel model = Producer.LoadModel(modelPath);

            // The canonical hash covers the neutral loaded model BEFORE any engine-specific mutation.
            string neutralJson = model.ToJsonObject().ToJsonString();
            string canonicalModelHash = BenchmarkCanonicalJson.ComputeSha256(neutralJson);

            // Start from a clean model: drop any pre-existing simulation results — including older
            // TAS-sourced ones the source filter cannot distinguish — so only THIS run's results can
            // be emitted or satisfy the success gate. The hashes above reflect the original input.
            model = Producer.StripPreviousResults(model);

            TasBenchmarkContext context = new TasBenchmarkContext
            {
                SourceModelName = string.IsNullOrWhiteSpace(model.Name) ? "model" : model.Name,
                SourceModelGuid = model.Guid.ToString("N"),
                SourceFileHash = sourceFileHash,
                CanonicalModelHash = canonicalModelHash,
                CanonicalizationVersion = BenchmarkCanonicalization.CurrentVersion,
                SamCommit = ResolveCommit(arguments.GetOption("sam-commit"), "SAM_COMMIT", typeof(AnalyticalModel), allowLocalGit: false),
                RunnerCommit = ResolveCommit(arguments.GetOption("runner-commit"), "RUNNER_COMMIT", typeof(Program), allowLocalGit: true),
                EngineName = "Tas",
                EngineVersion = Query.TasVersion(arguments.GetOption("engine-version")),
                SdkVersion = null,
                WeatherIdentity = Path.GetFileNameWithoutExtension(weatherPath),
                WeatherHash = BenchmarkHash.ComputeSha256(File.ReadAllBytes(weatherPath)),
                // Sizing (design loads) relies on the design days embedded in the source model —
                // report EmbeddedModel only when the model actually carries them.
                DesignDaySource = ResolveDesignDaySource(model),
                RunTimestampUtc = DateTimeOffset.UtcNow,
            };

            if (string.IsNullOrEmpty(context.EngineVersion))
            {
                context.EngineVersion = null;
                context.Notes.Add("TAS engine version was unavailable (EDSL exposes no version in code); pass --engine-version to record it.");
            }

            // The native TAS route consumes a shared gbXML. Reuse the caller's --gbxml when present;
            // otherwise export it from the SAM model (the same gbXML both engines can consume).
            string gbxmlPath = EnsureGbXml(arguments.GetOption("gbxml"), tbdPath, model, context);

            // The supplied --weather is authoritative: the provenance records its identity/hash, so
            // TAS MUST simulate exactly it. If it cannot be read, stop with an input error rather
            // than letting WorkflowCalculator silently fall back to the model's embedded weather
            // (which would make the document claim weather A while TAS ran weather B).
            Weather.WeatherData weatherData = LoadWeather(weatherPath);

            string tsdPath = Path.Combine(Path.GetDirectoryName(tbdPath) ?? string.Empty, Path.GetFileNameWithoutExtension(tbdPath) + ".tsd");

            // A repeatable benchmark must not reuse stale artefacts: remove any prior TBD/TSD so this
            // run starts clean (RemoveExistingTBD handles the TBD; delete the TSD explicitly).
            DeleteIfExists(tsdPath);

            WorkflowSettings settings = new WorkflowSettings
            {
                Path_TBD = tbdPath,
                Path_gbXML = gbxmlPath,
                WeatherData = weatherData,
                Simulate = true,
                Sizing = true,
                AddIZAMs = true,
                UnmetHours = true,
                UpdateZones = true,
                RemoveExistingTBD = true,
                SimulateFrom = 1,
                SimulateTo = 365,
            };

            Stopwatch stopwatch = Stopwatch.StartNew();
            AnalyticalModel calculated = RunWithTimeout(() => new WorkflowCalculator(settings).Calculate(model), timeout, out bool timedOut, out Exception runError);
            stopwatch.Stop();

            AnalyticalModel resultModel = calculated ?? model;
            context.DurationSeconds = stopwatch.Elapsed.TotalSeconds;

            // A non-null returned model is NOT sufficient: Calculate can return a (cloned) model
            // without simulating (e.g. no weather year), and result extraction can yield nothing.
            // Only declare success when the run actually produced the required measurements — model
            // annual energy AND at least one per-space load — so a missing TSD or empty results
            // becomes a failed producer document (exit 5), never a silent "success".
            bool ranClean = !timedOut && runError == null && calculated != null;

            bool tsdExists = ranClean && File.Exists(tsdPath);
            if (tsdExists)
            {
                // The workflow attaches per-space results but NOT the model-level annual energy —
                // read that explicitly (Wh/W; ToBenchmark converts to kWh/kW).
                context.ModelResult = SAM.Analytical.Tas.Convert.ToSAM_AnalyticalModelSimulationResult(tsdPath, resultModel);
            }

            bool hasModelEnergy = Producer.HasModelAnnualEnergy(context.ModelResult);
            bool hasSpaceLoad = Producer.HasSpaceLoad(resultModel, context.ModelResult?.Source);
            bool success = ranClean && tsdExists && hasModelEnergy && hasSpaceLoad;
            context.State = success ? RunState.Success : RunState.Failure;

            if (!success)
            {
                if (timedOut)
                {
                    context.Notes.Add("The TAS run exceeded the " + timeout.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "s timeout; measurements are unavailable.");
                }
                else if (runError != null)
                {
                    context.Notes.Add("The TAS run did not complete successfully; measurements are unavailable.");
                    context.Warnings.Add(runError.Message);
                }
                else if (!tsdExists)
                {
                    context.Notes.Add("No TSD was produced (the simulation did not run — e.g. the weather file has no usable year); measurements are unavailable.");
                }
                else if (!hasModelEnergy)
                {
                    context.Notes.Add("The run produced no model annual-energy result; the TAS result is treated as a failure.");
                }
                else if (!hasSpaceLoad)
                {
                    context.Notes.Add("The run produced no per-space load results; the TAS result is treated as a failure.");
                }
            }

            BenchmarkDocument document = resultModel.ToBenchmark(context);
            return Producer.Emit(document, outputPath, success, standardOutput);
        }

        /// <summary>
        /// Resolves the gbXML the TAS route imports. A caller-supplied <c>--gbxml</c> that exists is
        /// an intended shared input and is reused as-is. Otherwise a gbXML is exported from the
        /// current SAM model: to the supplied path if it does not yet exist, or — when no
        /// <c>--gbxml</c> was given — to the auto-derived <c>&lt;tbd&gt;.xml</c>, overwriting any stale
        /// file from a previous run so a rerun that reuses the output directory can never simulate a
        /// previous model's geometry while the provenance/hashes describe the current model.
        /// </summary>
        private static string EnsureGbXml(string gbxmlOption, string tbdPath, AnalyticalModel model, TasBenchmarkContext context)
        {
            bool callerSupplied = !string.IsNullOrWhiteSpace(gbxmlOption);
            string gbxmlPath = callerSupplied
                ? Path.GetFullPath(gbxmlOption)
                : Path.ChangeExtension(tbdPath, ".xml");

            if (callerSupplied && File.Exists(gbxmlPath))
            {
                return gbxmlPath;
            }

            // Auto-derived default: drop any stale gbXML first (the clean-run guarantee, alongside
            // the stale .tbd/.tsd removal). A caller-supplied-but-missing path is simply created.
            if (!callerSupplied)
            {
                DeleteIfExists(gbxmlPath);
            }

            gbXMLSerializer.gbXML gbxml = SAM.Analytical.gbXML.Convert.TogbXML(model);
            if (gbxml == null || !SAM.Core.gbXML.Create.gbXML(gbxml, gbxmlPath))
            {
                throw new InvalidOperationException("Failed to export a shared gbXML from the model to: " + gbxmlPath);
            }

            context.Notes.Add(callerSupplied
                ? "Shared gbXML was exported from the source model to the supplied --gbxml path (it did not exist)."
                : "Shared gbXML was exported from the source model (no --gbxml supplied).");
            return gbxmlPath;
        }

        /// <summary>
        /// Reads the EPW weather file into a SAM <see cref="Weather.WeatherData"/>. The supplied
        /// weather is authoritative (its identity/hash go into the provenance), so a file that
        /// cannot be read is a hard input failure (<see cref="IOException"/> → exit 3) — never a
        /// silent fall back to the model's embedded weather.
        /// </summary>
        private static Weather.WeatherData LoadWeather(string weatherPath)
        {
            Weather.WeatherData weatherData;
            try
            {
                weatherData = Weather.Convert.ToSAM(weatherPath);
            }
            catch (Exception exception) when (!(exception is IOException) && !(exception is UnauthorizedAccessException))
            {
                throw new IOException("The weather file could not be read into SAM weather data: " + weatherPath, exception);
            }

            if (weatherData == null)
            {
                throw new IOException("The weather file did not read into SAM weather data: " + weatherPath);
            }

            return weatherData;
        }

        /// <summary>Reports <see cref="DesignDaySource.EmbeddedModel"/> only when the model actually carries heating or cooling design days; otherwise <see cref="DesignDaySource.None"/>.</summary>
        private static DesignDaySource ResolveDesignDaySource(AnalyticalModel analyticalModel)
        {
            bool hasDesignDays = HasDesignDays(analyticalModel, Analytical.AnalyticalModelParameter.HeatingDesignDays)
                || HasDesignDays(analyticalModel, Analytical.AnalyticalModelParameter.CoolingDesignDays);
            return hasDesignDays ? DesignDaySource.EmbeddedModel : DesignDaySource.None;
        }

        private static bool HasDesignDays(AnalyticalModel analyticalModel, Analytical.AnalyticalModelParameter parameter)
        {
            return analyticalModel.TryGetValue(parameter, out Core.SAMCollection<DesignDay> designDays)
                && designDays != null
                && designDays.Count > 0;
        }

        /// <summary>
        /// Deletes a stale artefact and confirms it is gone. A stale TSD that cannot be removed would
        /// still satisfy the later "TSD exists" / result-reader checks and let a previous run's result
        /// masquerade as this run's — so a failed delete is a hard input/IO error (exit 3), not
        /// something to swallow.
        /// </summary>
        private static void DeleteIfExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            File.Delete(path);

            if (File.Exists(path))
            {
                throw new IOException("The stale TAS result file could not be removed: " + path);
            }
        }

        /// <summary>
        /// Runs <paramref name="work"/> on a dedicated STA thread (TAS COM requires STA) and waits up
        /// to <paramref name="timeout"/>. On timeout the worker is a background thread abandoned when
        /// the process exits — a hard timeout, not a graceful cancel (the blocking in-process COM
        /// call cannot be interrupted). Exceptions are captured and returned via
        /// <paramref name="error"/>.
        /// </summary>
        private static AnalyticalModel RunWithTimeout(Func<AnalyticalModel> work, TimeSpan timeout, out bool timedOut, out Exception error)
        {
            AnalyticalModel result = null;
            Exception captured = null;

            Thread thread = new Thread(() =>
            {
                try
                {
                    result = work();
                }
                catch (Exception exception)
                {
                    captured = exception;
                }
            })
            {
                IsBackground = true,
                Name = "benchmark-tas-worker",
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            bool completed = thread.Join(timeout);
            timedOut = !completed;
            error = completed ? captured : null;
            return completed ? result : null;
        }

        /// <summary>
        /// Resolves a 7-64 lowercase-hex commit: explicit argument, then environment variable, then
        /// the assembly's InformationalVersion SHA (the <c>+&lt;sha&gt;</c> suffix CI stamps), then —
        /// only for the runner — <c>git rev-parse HEAD</c> from the executable's directory. Returns
        /// null when none resolve; validation then rejects the document with a precise message.
        /// </summary>
        private static string ResolveCommit(string explicitValue, string environmentVariable, Type assemblyType, bool allowLocalGit)
        {
            string candidate = Normalize(explicitValue);
            if (candidate != null)
            {
                return candidate;
            }

            candidate = Normalize(Environment.GetEnvironmentVariable(environmentVariable));
            if (candidate != null)
            {
                return candidate;
            }

            candidate = Normalize(InformationalVersionSha(assemblyType));
            if (candidate != null)
            {
                return candidate;
            }

            if (allowLocalGit)
            {
                candidate = Normalize(GitHead(AppContext.BaseDirectory));
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim().ToLowerInvariant();
            return CommitPattern.IsMatch(value) ? value : null;
        }

        private static string InformationalVersionSha(Type assemblyType)
        {
            try
            {
                object[] attributes = assemblyType.Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);
                if (attributes.Length == 0)
                {
                    return null;
                }

                string informationalVersion = ((System.Reflection.AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
                int plus = informationalVersion?.IndexOf('+') ?? -1;
                return plus >= 0 ? informationalVersion.Substring(plus + 1) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GitHead(string directory)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    WorkingDirectory = directory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using (Process process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        return null;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0 ? output.Trim() : null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private const string Usage =
            "Usage: benchmark-tas --model <model.json> --weather <weather.epw> --tbd <run.tbd> --out <benchmark-TAS.json>\n" +
            "                     [--gbxml <shared.xml>] [--engine-version <version>] [--timeout-seconds <n>]\n" +
            "                     [--sam-commit <sha>] [--runner-commit <sha>]\n" +
            "\n" +
            "Runs the native TAS route (EDSL Tas laptop only). --gbxml is reused when it exists, else exported from the model.\n" +
            "Exit codes: 0 success, 2 usage error, 3 input/IO/serialization error, 4 validation failure, 5 producer failure.";
    }
}
