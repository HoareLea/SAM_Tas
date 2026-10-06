// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Tas.Benchmark
{
    /// <summary>
    /// The COM-free core steps of the TAS producer that carry decisions worth unit-testing: loading
    /// the source model and the validate/write/exit-code decision. These use only the B1a schema and
    /// SAM core, so they run offline with no TAS install. The CLI (<c>Program</c>) composes these
    /// with the shared host and the TAS run.
    /// </summary>
    public static class Producer
    {
        /// <summary>
        /// Loads the source model. A file that exists but does not carry a SAM AnalyticalModel —
        /// whether it deserializes to nothing or the SAM deserializer throws part way through — is a
        /// deserialization failure, reported as <see cref="System.Text.Json.JsonException"/> so the
        /// shared host maps it to input/IO/serialization (exit 3), never a producer failure (5).
        /// </summary>
        public static AnalyticalModel LoadModel(string modelPath)
        {
            AnalyticalModel model;
            try
            {
                model = Core.Convert.ToSAM<AnalyticalModel>(modelPath)?.FirstOrDefault();
            }
            catch (Exception exception) when (!(exception is IOException) && !(exception is UnauthorizedAccessException) && !(exception is System.Text.Json.JsonException))
            {
                throw new System.Text.Json.JsonException("The model file could not be read as a SAM AnalyticalModel: " + modelPath, exception);
            }

            if (model == null)
            {
                throw new System.Text.Json.JsonException("The model file did not deserialize to a SAM AnalyticalModel: " + modelPath);
            }

            return model;
        }

        /// <summary>
        /// Validates, writes and reports a produced document, returning the shared exit code: a
        /// document that fails schema validation throws <see cref="BenchmarkValidationException"/>
        /// (the host maps it to <see cref="BenchmarkExitCode.ValidationFailure"/>); a valid document
        /// whose run did not succeed returns <see cref="BenchmarkExitCode.ProducerFailure"/>;
        /// otherwise <see cref="BenchmarkExitCode.Success"/>.
        /// </summary>
        public static int Emit(BenchmarkDocument document, string outputPath, bool runSucceeded, TextWriter standardOutput)
        {
            BenchmarkValidationResult validation = BenchmarkValidator.Validate(document);
            if (!validation.IsValid)
            {
                throw new BenchmarkValidationException(validation);
            }

            BenchmarkSerializer.Write(outputPath, document);
            standardOutput?.WriteLine("Wrote " + outputPath + " (state=" + (runSucceeded ? RunState.Success : RunState.Failure) + ", route=Native-TAS).");
            return runSucceeded ? (int)BenchmarkExitCode.Success : (int)BenchmarkExitCode.ProducerFailure;
        }

        /// <summary>
        /// Returns a copy of the model with every pre-existing simulation result removed, so this run
        /// starts from a clean model and only its OWN results can be emitted or satisfy the success
        /// gate. This is stronger than the source filter: an <b>older TAS result</b> already embedded
        /// in the input carries the same <c>SAM.Analytical.Tas</c> source, so filtering by source
        /// cannot distinguish it — it must be removed. Model identity (GUID), libraries, spaces and
        /// geometry are preserved; only <see cref="AnalyticalModelSimulationResult"/> and
        /// <see cref="SpaceSimulationResult"/> objects (and their relations) are dropped.
        /// </summary>
        public static AnalyticalModel StripPreviousResults(AnalyticalModel analyticalModel)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return analyticalModel;
            }

            RemoveObjects(adjacencyCluster, adjacencyCluster.GetObjects<SpaceSimulationResult>());
            RemoveObjects(adjacencyCluster, adjacencyCluster.GetObjects<AnalyticalModelSimulationResult>());

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }

        /// <summary>True when the model-level result carries a finite annual heating or cooling energy.</summary>
        public static bool HasModelAnnualEnergy(AnalyticalModelSimulationResult modelResult)
        {
            if (modelResult == null)
            {
                return false;
            }

            return IsFinite(modelResult, Analytical.AnalyticalModelSimulationResultParameter.ConsumptionHeating)
                || IsFinite(modelResult, Analytical.AnalyticalModelSimulationResultParameter.ConsumptionCooling);
        }

        /// <summary>
        /// True when at least one per-space result FROM THE GIVEN RESULT SOURCE carries a finite load.
        /// Combined with <see cref="StripPreviousResults"/> (which removes older same-source results
        /// up front), this guarantees the gate is satisfied only by measurements THIS run produced.
        /// </summary>
        public static bool HasSpaceLoad(AnalyticalModel analyticalModel, string resultSource)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(resultSource))
            {
                return false;
            }

            List<SpaceSimulationResult> results = analyticalModel.GetResults<SpaceSimulationResult>(resultSource);
            return results != null && results.Any(x => IsFinite(x, Analytical.SpaceSimulationResultParameter.Load));
        }

        /// <summary>
        /// Returns the first path this run would write that resolves to the same file as the input
        /// model — or null when there is no collision. A TAS run writes the TBD and, from the same
        /// stem, the <c>.tsd</c>/<c>.t3d</c>/<c>.json</c> sidecar/<c>.timing.csv</c>; the producer also
        /// writes the benchmark output and, when it exports one, the shared gbXML. Because
        /// <c>RemoveExistingTBD</c> deletes the TBD before opening it, a <c>--tbd</c> (or <c>--out</c>)
        /// pointed at the source model would destroy it — the caller must be rejected up front.
        /// <paramref name="gbxmlOutputPath"/> is the gbXML the producer will actually write (the
        /// auto-derived <c>&lt;tbd&gt;.xml</c> when no <c>--gbxml</c> was supplied), or null when a
        /// caller-supplied gbXML is reused and nothing is written there. All comparisons are
        /// full-path, case-insensitive.
        /// </summary>
        public static string FindInputOverwriteCollision(string modelPath, string tbdPath, string outputPath, string gbxmlOutputPath = null)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                return null;
            }

            string directory = Path.GetDirectoryName(tbdPath) ?? string.Empty;
            string stem = Path.GetFileNameWithoutExtension(tbdPath);
            string basePath = Path.Combine(directory, stem);

            List<string> runOutputs = new List<string>
            {
                tbdPath,
                basePath + ".tsd",
                basePath + ".t3d",
                basePath + ".json",
                basePath + ".timing.csv",
                outputPath,
            };

            if (!string.IsNullOrWhiteSpace(gbxmlOutputPath))
            {
                runOutputs.Add(gbxmlOutputPath);
            }

            return runOutputs.FirstOrDefault(candidate => PathsEqual(candidate, modelPath));
        }

        /// <summary>Case-insensitive full-path equality (Windows filesystem), tolerant of separators/relative segments.</summary>
        public static bool PathsEqual(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return false;
            }

            try
            {
                return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void RemoveObjects<T>(AdjacencyCluster adjacencyCluster, List<T> objects) where T : SAM.Core.SAMObject
        {
            if (objects == null)
            {
                return;
            }

            foreach (T @object in objects)
            {
                if (@object != null)
                {
                    adjacencyCluster.RemoveObject<T>(@object.Guid);
                }
            }
        }

        private static bool IsFinite(SAM.Core.SAMObject sAMObject, Enum parameter)
        {
            return sAMObject != null
                && sAMObject.TryGetValue(parameter, out double value)
                && !double.IsNaN(value)
                && !double.IsInfinity(value);
        }
    }
}
