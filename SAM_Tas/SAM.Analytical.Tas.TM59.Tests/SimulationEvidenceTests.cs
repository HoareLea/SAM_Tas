// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core.Tas;
using System;
using System.IO;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// D-2 and D-4. What counts as evidence that a TAS run actually happened, per output shape.
    /// <para>
    /// Three false positives motivate this fixture. The TPD entry point returned a literal <c>true</c> even
    /// when the energy centre was null and nothing ran. The conversion returned a literal <c>true</c> even
    /// when it had lost every system. And the TBD entry point returned
    /// <c>Core.Query.WaitToUnlock(path_TSD)</c>, which answers <c>true</c> as soon as an already-existing file
    /// is unlocked - so a leftover TSD made a failed run report success.
    /// </para>
    /// <para>
    /// The two shapes are held to different rules on purpose: a TBD run writes a separate TSD that may be
    /// deleted beforehand, while a TPD run writes back into the document it simulated, which must not be.
    /// </para>
    /// <para>
    /// <c>SimulationEvidence</c> is free of TAS COM types, so all of this runs with no TAS licence, no TAS
    /// install and no COM server. Only real files on disk are involved.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SimulationEvidenceTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "SAM_PR2_SimEvidence", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch
            {
                // A leftover temp directory must never fail a test.
            }
        }

        private string Path_Document(string name, long length = 4096)
        {
            string result = Path.Combine(directory, name);
            File.WriteAllBytes(result, new byte[length]);
            return result;
        }

        private static void WriteResult(string path, long length)
        {
            File.WriteAllBytes(path, new byte[length]);
        }

        // ------------------------------------------------------------------ shape contract

        [Test]
        public void UndefinedShape_Refuses()
        {
            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.Undefined, Path_Document("a.tbd"), Path.Combine(directory, "a.tsd"));

            Assert.That(simulationEvidence.Completed, Is.False);
            Assert.That(simulationEvidence.Refusals, Is.Not.Empty, "A run with no stated output shape cannot be judged.");
        }

        [Test]
        public void InPlaceShape_ClaimingASeparateOutput_Refuses()
        {
            string path_TPD = Path_Document("a.tpd");

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.InPlaceDocument, path_TPD, Path.Combine(directory, "a.tsd"));

            Assert.That(
                simulationEvidence.Refusals,
                Has.Some.Contains("must not name a separate output file"),
                "An in-place run writes into its own document; claiming a distinct output would be false.");
        }

        [Test]
        public void SeparateOutputShape_WithNoOutputNamed_Refuses()
        {
            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.SeparateOutputFile, Path_Document("a.tbd"), null);

            Assert.That(simulationEvidence.Refusals, Has.Some.Contains("must name the output file"));
        }

        // ------------------------------------------------------------------ SeparateOutputFile (TBD -> TSD)

        [Test]
        public void SeparateOutput_HappyPath_Completes()
        {
            string path_TBD = Path_Document("m.tbd");
            string path_TSD = Path.Combine(directory, "m.tsd");

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));
            WriteResult(path_TSD, 64 * 1024);
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();

            Assert.That(
                simulationEvidence.Completed,
                Is.True,
                string.Concat("Expected a completed run; refusals were: ", string.Join(" | ", simulationEvidence.Refusals)));
        }

        [Test]
        public void SeparateOutput_MissingOutput_Refuses()
        {
            string path_TBD = Path_Document("m.tbd");
            string path_TSD = Path.Combine(directory, "m.tsd");

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            simulationEvidence.Prepare(DateTime.UtcNow);
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();

            Assert.That(simulationEvidence.Completed, Is.False);
            Assert.That(simulationEvidence.Refusals, Has.Some.Contains("no output file"));
        }

        [Test]
        public void SeparateOutput_StaleLeftoverOutput_IsDeletedAndThenRefusedWhenTheRunWritesNothing()
        {
            // This is precisely the WaitToUnlock false positive: a leftover TSD from an earlier run, which
            // WaitToUnlock answered true for, making a failed simulation report success.
            string path_TBD = Path_Document("m.tbd");
            string path_TSD = Path.Combine(directory, "m.tsd");
            WriteResult(path_TSD, 64 * 1024);

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            simulationEvidence.Prepare(DateTime.UtcNow);

            Assert.That(File.Exists(path_TSD), Is.False, "The leftover output must be deleted before the run.");

            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();

            Assert.That(
                simulationEvidence.Completed,
                Is.False,
                "A run that wrote nothing must not inherit the previous run's output as its evidence.");
        }

        [Test]
        public void SeparateOutput_TrivialOutput_Refuses()
        {
            // The ~22-byte TSD TAS writes when no weather data is installed.
            string path_TBD = Path_Document("m.tbd");
            string path_TSD = Path.Combine(directory, "m.tsd");

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));
            WriteResult(path_TSD, 22);
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();

            Assert.That(simulationEvidence.Completed, Is.False);
            Assert.That(simulationEvidence.Refusals, Has.Some.Contains("22 bytes"));
        }

        [Test]
        public void SeparateOutput_CallThatThrew_Refuses()
        {
            string path_TBD = Path_Document("m.tbd");
            string path_TSD = Path.Combine(directory, "m.tsd");

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));
            simulationEvidence.RecordCallFailed("the building was null");
            WriteResult(path_TSD, 64 * 1024);
            simulationEvidence.Conclude();

            Assert.That(simulationEvidence.Completed, Is.False, "A call that failed cannot be rescued by an output file.");
        }

        // ------------------------------------------------------------------ InPlaceDocument (TPD)

        [Test]
        public void InPlace_SavedButNotReconciled_DoesNotComplete()
        {
            // The heart of the TPD false positive: Save() writes whether or not results were produced, so a
            // changed file is necessary but not sufficient.
            string path_TPD = Path_Document("m.tpd", 4096);

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.InPlaceDocument, path_TPD, null);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));
            WriteResult(path_TPD, 8192);
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();

            Assert.That(
                simulationEvidence.Completed,
                Is.False,
                "A written TPD is not evidence of results. Only the reconciliation is.");
        }

        [Test]
        public void InPlace_ReconciledResults_Completes()
        {
            string path_TPD = Path_Document("m.tpd", 4096);

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.InPlaceDocument, path_TPD, null);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));
            WriteResult(path_TPD, 8192);
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();
            simulationEvidence.RecordResultsReconciled(9, 0, 8759);

            Assert.That(
                simulationEvidence.Completed,
                Is.True,
                string.Concat("Refusals were: ", string.Join(" | ", simulationEvidence.Refusals)));
        }

        [Test]
        public void InPlace_UnchangedDocument_Refuses()
        {
            string path_TPD = Path_Document("m.tpd", 4096);

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.InPlaceDocument, path_TPD, null);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(1));
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();

            Assert.That(simulationEvidence.Completed, Is.False);
            Assert.That(simulationEvidence.Refusals, Has.Some.Contains("unchanged from its pre-call state"));
        }

        [Test]
        public void InPlace_NeverDeletesTheDocument()
        {
            string path_TPD = Path_Document("m.tpd", 4096);

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.InPlaceDocument, path_TPD, null);

            simulationEvidence.Prepare(DateTime.UtcNow);

            Assert.That(
                File.Exists(path_TPD),
                Is.True,
                "Preparing an in-place run must never delete the document being simulated.");
        }

        [Test]
        public void InPlace_ResultsThatDoNotReconcile_Refuse()
        {
            string path_TPD = Path_Document("m.tpd", 4096);

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.InPlaceDocument, path_TPD, null);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));
            WriteResult(path_TPD, 8192);
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();
            simulationEvidence.RecordResultsNotReconciled("Bedroom 2 has no ZoneTemperature series");

            Assert.That(simulationEvidence.Completed, Is.False);
            Assert.That(simulationEvidence.Refusals, Has.Some.Contains("Bedroom 2"));
        }

        // ------------------------------------------------------------------ D-4, error logs, both shapes

        [Test]
        public void ErrorLog_WrittenByThisRun_Refuses()
        {
            string path_TBD = Path_Document("m.tbd");
            string path_TSD = Path.Combine(directory, "m.tsd");

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));
            WriteResult(path_TSD, 64 * 1024);
            File.WriteAllText(simulationEvidence.Path_ErrorLog, "Simulation Failed: zone air balance");
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();

            Assert.That(
                simulationEvidence.Completed,
                Is.False,
                "TAS answers a rejected model with an error log while the COM call returns normally.");
            Assert.That(simulationEvidence.Refusals, Has.Some.Contains("Simulation Failed"));
        }

        [Test]
        public void ErrorLog_StaleFromAnEarlierRun_IsDeletedAndNotCharged()
        {
            string path_TBD = Path_Document("m.tbd");
            string path_TSD = Path.Combine(directory, "m.tsd");
            string path_ErrorLog = SimulationEvidence.ErrorLogPath(path_TBD);
            File.WriteAllText(path_ErrorLog, "Simulation Failed: an EARLIER run's failure");

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));

            Assert.That(File.Exists(path_ErrorLog), Is.False, "A stale error log must be cleared before the run.");

            WriteResult(path_TSD, 64 * 1024);
            simulationEvidence.RecordCallReturned();
            simulationEvidence.Conclude();

            Assert.That(
                simulationEvidence.Completed,
                Is.True,
                "An earlier run's error log must not be charged to this run.");
            Assert.That(simulationEvidence.ErrorLogText, Is.Null);
        }

        [Test]
        public void ErrorLog_StaleAndUndeletable_IsExcludedByTimestampNotIgnored()
        {
            string path_TBD = Path_Document("m.tbd");
            string path_TSD = Path.Combine(directory, "m.tsd");
            string path_ErrorLog = SimulationEvidence.ErrorLogPath(path_TBD);
            File.WriteAllText(path_ErrorLog, "Simulation Failed: an EARLIER run's failure");
            File.SetLastWriteTimeUtc(path_ErrorLog, DateTime.UtcNow.AddHours(-2));

            SimulationEvidence simulationEvidence;

            using (FileStream fileStream = new FileStream(path_ErrorLog, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Held open, so deletion fails and the timestamp route must be taken instead.
                simulationEvidence = new SimulationEvidence(
                    SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

                simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));

                Assert.That(
                    simulationEvidence.UtcErrorLogBefore,
                    Is.Not.Null,
                    "An undeletable stale log must have its write time recorded so it can be excluded.");

                WriteResult(path_TSD, 64 * 1024);
                simulationEvidence.RecordCallReturned();
                simulationEvidence.Conclude();
            }

            Assert.That(
                simulationEvidence.Completed,
                Is.True,
                "A stale log excluded by timestamp must not fail this run.");
            Assert.That(
                simulationEvidence.Notes,
                Has.Some.Contains("predates this run"),
                "And it must be reported, not silently ignored.");
        }

        [Test]
        public void ErrorLogPath_IsTheTasConvention()
        {
            Assert.That(
                SimulationEvidence.ErrorLogPath(Path.Combine(directory, "MyModel.tbd")),
                Is.EqualTo(Path.Combine(directory, "MyModel_error_log.txt")));
        }
    }
}
