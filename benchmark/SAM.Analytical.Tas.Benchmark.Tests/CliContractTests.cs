// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using NUnit.Framework;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Tas.Benchmark.Tests
{
    /// <summary>
    /// The producer honours the shared benchmark CLI contract (<see cref="BenchmarkCliHost"/>): exit
    /// <c>0</c> success, <c>2</c> usage, <c>3</c> input/IO/serialization, <c>4</c> validation,
    /// <c>5</c> producer failure. These exercise the COM-free building blocks the CLI composes —
    /// <see cref="Producer.LoadModel"/>, <see cref="Producer.Emit"/> and the shared host — so the
    /// exit-code decisions are verified offline with no TAS install (the CLI's live orchestration is
    /// covered by the TAS-laptop <c>Simulation</c>-category run).
    /// </summary>
    [TestFixture]
    public class CliContractTests
    {
        private const string Usage = "usage: benchmark-tas";

        [Test]
        public void Help_ReturnsSuccess()
        {
            StringWriter output = new StringWriter();

            int exitCode = BenchmarkCliHost.Run(new[] { "--help" }, Usage, RequiredOptions(), Body, output, new StringWriter());

            Assert.That(exitCode, Is.EqualTo((int)BenchmarkExitCode.Success));
            Assert.That(output.ToString(), Does.Contain(Usage));
        }

        [Test]
        public void MissingRequiredOption_ReturnsUsageError()
        {
            StringWriter error = new StringWriter();

            int exitCode = BenchmarkCliHost.Run(new[] { "--model", "model.json" }, Usage, RequiredOptions(), Body, new StringWriter(), error);

            Assert.That(exitCode, Is.EqualTo((int)BenchmarkExitCode.InvalidUsage));
            Assert.That(error.ToString(), Does.Contain("Missing required option"));
        }

        [Test]
        public void NonexistentInputFile_ReturnsInputError()
        {
            string missing = Path.Combine(Path.GetTempPath(), "sam-tas-missing-" + Guid.NewGuid().ToString("N") + ".json");

            int exitCode = BenchmarkCliHost.Run(
                new[] { "--model", missing },
                Usage,
                new[] { "model" },
                (arguments, _) =>
                {
                    // Mirrors the CLI: validating a missing input throws a mapped IO exception (exit 3).
                    BenchmarkCliPaths.ValidateInputFile(arguments.RequireOption("model"));
                    return 0;
                },
                new StringWriter(),
                new StringWriter());

            Assert.That(exitCode, Is.EqualTo((int)BenchmarkExitCode.InputOutputOrSerializationFailure));
        }

        [Test]
        public void ModelFileWithNoSamModel_ReturnsInputError()
        {
            string modelPath = Path.GetTempFileName();
            File.WriteAllText(modelPath, "[]");   // well-formed JSON, but no SAM object
            try
            {
                int exitCode = BenchmarkCliHost.Run(
                    new[] { "--model", modelPath },
                    Usage,
                    new[] { "model" },
                    (arguments, _) =>
                    {
                        Producer.LoadModel(arguments.RequireOption("model"));   // throws JsonException -> exit 3
                        return 0;
                    },
                    new StringWriter(),
                    new StringWriter());

                Assert.That(exitCode, Is.EqualTo((int)BenchmarkExitCode.InputOutputOrSerializationFailure));
            }
            finally
            {
                File.Delete(modelPath);
            }
        }

        [Test]
        public void Emit_InvalidDocument_ReturnsValidationFailure_AndWritesNothing()
        {
            string outputPath = OutputPath();

            int exitCode = BenchmarkCliHost.Run(
                Array.Empty<string>(),
                Usage,
                Array.Empty<string>(),
                (_, _) => Producer.Emit(new BenchmarkDocument(), outputPath, runSucceeded: true, new StringWriter()),
                new StringWriter(),
                new StringWriter());

            Assert.That(exitCode, Is.EqualTo((int)BenchmarkExitCode.ValidationFailure));
            Assert.That(File.Exists(outputPath), Is.False, "an invalid document must not be written");
        }

        [Test]
        public void Emit_ValidDocument_WithFailedRun_ReturnsProducerFailure_AndWritesDocument()
        {
            string outputPath = OutputPath();
            try
            {
                int exitCode = Producer.Emit(BenchmarkFixture.GoldenDocument(), outputPath, runSucceeded: false, new StringWriter());

                Assert.That(exitCode, Is.EqualTo((int)BenchmarkExitCode.ProducerFailure));
                Assert.That(File.Exists(outputPath), Is.True, "a valid document is still written for a failed run");
            }
            finally
            {
                Delete(outputPath);
            }
        }

        [Test]
        public void Emit_ValidDocument_WithSucceededRun_ReturnsSuccess()
        {
            string outputPath = OutputPath();
            try
            {
                int exitCode = Producer.Emit(BenchmarkFixture.GoldenDocument(), outputPath, runSucceeded: true, new StringWriter());

                Assert.That(exitCode, Is.EqualTo((int)BenchmarkExitCode.Success));
                Assert.That(File.Exists(outputPath), Is.True);
            }
            finally
            {
                Delete(outputPath);
            }
        }

        [Test]
        public void InputOverwriteCollision_RejectsModelEqualToTbdSidecarsOrOut()
        {
            string dir = Path.GetTempPath();
            string outJson = Path.Combine(dir, "benchmark-out.json");
            string tbd = Path.Combine(dir, "house.tbd");

            // --tbd is the model itself (RemoveExistingTBD would delete it) — the .sam case.
            Assert.That(Producer.FindInputOverwriteCollision(Path.Combine(dir, "house.sam"), Path.Combine(dir, "house.sam"), outJson), Is.Not.Null);
            // A stem sidecar the run writes lands on the model.
            Assert.That(Producer.FindInputOverwriteCollision(Path.Combine(dir, "house.json"), tbd, outJson), Is.Not.Null, ".json sidecar");
            Assert.That(Producer.FindInputOverwriteCollision(Path.Combine(dir, "house.tsd"), tbd, outJson), Is.Not.Null, ".tsd sidecar");
            // The auto gbXML is only written (and only a collision candidate) when no --gbxml is supplied.
            string autoGbxml = Path.Combine(dir, "house.xml");
            Assert.That(Producer.FindInputOverwriteCollision(autoGbxml, tbd, outJson, autoGbxml), Is.Not.Null, "auto gbXML written");
            Assert.That(Producer.FindInputOverwriteCollision(autoGbxml, tbd, outJson, null), Is.Null, "supplied --gbxml: <stem>.xml is not written, so no collision");
            // The benchmark output lands on the model.
            Assert.That(Producer.FindInputOverwriteCollision(Path.Combine(dir, "house.sam"), tbd, Path.Combine(dir, "house.sam")), Is.Not.Null, "--out == --model");
            // A well-separated set of paths is fine.
            Assert.That(Producer.FindInputOverwriteCollision(Path.Combine(dir, "house.sam"), tbd, outJson), Is.Null, "no collision");
        }

        private static int Body(BenchmarkArguments arguments, System.Threading.CancellationToken cancellationToken)
        {
            return 0;
        }

        private static string[] RequiredOptions()
        {
            return new[] { "model", "weather", "tbd", "out" };
        }

        private static string OutputPath()
        {
            return Path.Combine(Path.GetTempPath(), "sam-tas-out-" + Guid.NewGuid().ToString("N") + ".json");
        }

        private static void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
