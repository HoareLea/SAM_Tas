// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core.Tas;
using System;
using System.IO;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// What the string <c>ITPD.Simulate</c> returns is allowed to decide.
    /// <para>
    /// Five returns are now measured on licensed TAS. Four accompanied runs that produced nothing, and
    /// one - <c>"Done"</c>, from <c>ISystem.Simulate</c> - accompanied a run that returned 24 finite
    /// ZoneTemperature values for a 24-hour request. Note that the "Has Errors" message embeds the
    /// plant room's own NAME, so it can only be matched as a fragment.
    /// </para>
    /// <para>
    /// The contract this fixture pins: a <b>measured failure refuses</b>; a <b>measured success is
    /// recorded but still does not pass the run</b>; anything else is preserved verbatim and decides
    /// nothing. The decisive gate is always the complete <c>ZoneTemperature</c> reconciliation.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SimulationDiagnosticTests
    {
        [TestCase("Plant room has no components")]
        [TestCase("Plant Room Has Errors")]
        [TestCase("Failed to open the TSD file")]
        [TestCase("Sizing Flow Failed")]
        public void MeasuredFailures_AreClassifiedAsFailures(string returned)
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    SimulationDiagnostic.Classify(returned),
                    Is.EqualTo(SimulationDiagnosticKind.KnownFailure),
                    "Every one of these was observed on licensed TAS accompanying a run that produced nothing.");

                Assert.That(SimulationDiagnostic.IsFailure(returned), Is.True);
            });
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void NoAnswer_IsSilentAndNotAFailure(string returned)
        {
            Assert.Multiple(() =>
            {
                Assert.That(SimulationDiagnostic.Classify(returned), Is.EqualTo(SimulationDiagnosticKind.Silent));
                Assert.That(SimulationDiagnostic.IsFailure(returned), Is.False);
            });
        }

        [Test]
        public void MeasuredSuccess_IsClassifiedAsSuccess()
        {
            // Measured on licensed TAS: ISystem.Simulate answered "Done" on a run that then returned 24
            // finite ZoneTemperature values for a 24-hour request. That is the success vocabulary.
            Assert.Multiple(() =>
            {
                Assert.That(SimulationDiagnostic.Classify("Done"), Is.EqualTo(SimulationDiagnosticKind.KnownSuccess));
                Assert.That(SimulationDiagnostic.IsSuccess("Done"), Is.True);
                Assert.That(SimulationDiagnostic.IsFailure("Done"), Is.False);
                Assert.That(SimulationDiagnostic.IsSuccess("done"), Is.True, "Casing must not matter.");
            });
        }

        [Test]
        public void PlantRoomNameIsEmbeddedInTheHasErrorsMessage()
        {
            // "Has Errors" is prefixed by the plant room's own NAME - measured as both "Plant Room Has
            // Errors" and "PR Has Errors" - so it can only be matched as a fragment, never literally.
            Assert.Multiple(() =>
            {
                Assert.That(SimulationDiagnostic.IsFailure("PR Has Errors"), Is.True);
                Assert.That(SimulationDiagnostic.IsFailure("Main PlantRoom Has Errors"), Is.True);
            });
        }

        [Test]
        public void SuccessIsCheckedBeforeFailureFragments()
        {
            // A success answer is matched WHOLE and checked first, so a future success word that happened
            // to contain a failure fragment could not be silently misread as an error.
            Assert.That(SimulationDiagnostic.Classify("Done"), Is.EqualTo(SimulationDiagnosticKind.KnownSuccess));
        }

        [TestCase("Simulation complete")]
        [TestCase("8760 hours simulated")]
        public void UnrecognisedAnswer_IsNotTreatedAsAFailure(string returned)
        {
            // This is the whole point. If TAS answers with a status on success, a non-empty test would
            // refuse every good run. Until a successful return is actually measured, an unrecognised
            // answer must decide nothing.
            Assert.Multiple(() =>
            {
                Assert.That(
                    SimulationDiagnostic.Classify(returned),
                    Is.EqualTo(SimulationDiagnosticKind.Unrecognised));

                Assert.That(
                    SimulationDiagnostic.IsFailure(returned),
                    Is.False,
                    "An unmeasured answer must not be assumed to be an error.");
            });
        }

        [Test]
        public void Classification_IsCaseInsensitive()
        {
            // TAS's own casing varies between diagnostics - "Plant room has no components" against
            // "Plant Room Has Errors" - so matching must not depend on it.
            Assert.That(SimulationDiagnostic.IsFailure("PLANT ROOM HAS ERRORS"), Is.True);
            Assert.That(SimulationDiagnostic.IsFailure("plant room has no components"), Is.True);
        }

        // ---------------------------------------------------------------- evidence integration

        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "SAM_PR2_Diag", Guid.NewGuid().ToString("N"));
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
            }
        }

        private SimulationEvidence InPlaceRun()
        {
            string path_TPD = Path.Combine(directory, "m.tpd");
            File.WriteAllBytes(path_TPD, new byte[4096]);

            SimulationEvidence simulationEvidence = new SimulationEvidence(
                SimulationOutputShape.InPlaceDocument, path_TPD, null);

            simulationEvidence.Prepare(DateTime.UtcNow.AddSeconds(-1));
            File.WriteAllBytes(path_TPD, new byte[8192]);

            return simulationEvidence;
        }

        [Test]
        public void Evidence_PreservesTheRawStringWhateverItSays()
        {
            SimulationEvidence simulationEvidence = InPlaceRun();

            simulationEvidence.RecordCallReturned("Sizing Flow Failed");

            Assert.That(
                simulationEvidence.NativeDiagnostic,
                Is.EqualTo("Sizing Flow Failed"),
                "The raw native string is evidence and must be kept verbatim.");
        }

        [Test]
        public void Evidence_MeasuredFailure_Refuses()
        {
            SimulationEvidence simulationEvidence = InPlaceRun();

            bool mayProceed = simulationEvidence.RecordCallReturned("Plant Room Has Errors");

            Assert.Multiple(() =>
            {
                Assert.That(mayProceed, Is.False);
                Assert.That(simulationEvidence.Completed, Is.False);
                Assert.That(simulationEvidence.Refusals, Has.Some.Contains("Plant Room Has Errors"));
            });
        }

        [Test]
        public void Evidence_UnrecognisedAnswer_StillNeedsTheResultsToReconcile()
        {
            SimulationEvidence simulationEvidence = InPlaceRun();

            bool mayProceed = simulationEvidence.RecordCallReturned("Simulation complete");
            simulationEvidence.Conclude();

            Assert.Multiple(() =>
            {
                Assert.That(mayProceed, Is.True, "An unmeasured answer does not refuse...");
                Assert.That(
                    simulationEvidence.Completed,
                    Is.False,
                    "...but it does not pass the run either: the reconciliation is the decisive gate.");
                Assert.That(simulationEvidence.NativeDiagnosticKind, Is.EqualTo(SimulationDiagnosticKind.Unrecognised));
                Assert.That(simulationEvidence.Notes, Has.Some.Contains("neither success nor failure"));
            });

            simulationEvidence.RecordResultsReconciled(2, 0, 23);

            Assert.That(
                simulationEvidence.Completed,
                Is.True,
                "Once the results reconcile, the run is evidenced.");
        }

        [Test]
        public void Evidence_MeasuredSuccess_StillNeedsTheResultsToReconcile()
        {
            // TAS saying "Done" is positive evidence, but it is NOT the gate. A run whose results do not
            // reconcile must still refuse - which is what stops a plant-side success being read as
            // "the ventilation results are there".
            SimulationEvidence simulationEvidence = InPlaceRun();

            bool mayProceed = simulationEvidence.RecordCallReturned("Done");
            simulationEvidence.Conclude();

            Assert.Multiple(() =>
            {
                Assert.That(mayProceed, Is.True);
                Assert.That(simulationEvidence.NativeDiagnosticKind, Is.EqualTo(SimulationDiagnosticKind.KnownSuccess));
                Assert.That(simulationEvidence.Notes, Has.Some.Contains("TAS reported success"));
                Assert.That(
                    simulationEvidence.Completed,
                    Is.False,
                    "Even a measured success does not complete the run on its own.");
            });

            simulationEvidence.RecordResultsReconciled(2, 0, 23);

            Assert.That(simulationEvidence.Completed, Is.True);
        }

        [Test]
        public void Evidence_SilentAnswer_StillNeedsTheResultsToReconcile()
        {
            SimulationEvidence simulationEvidence = InPlaceRun();

            simulationEvidence.RecordCallReturned((string)null);
            simulationEvidence.Conclude();

            Assert.That(
                simulationEvidence.Completed,
                Is.False,
                "Silence is not success either - a saved TPD proves nothing.");

            simulationEvidence.RecordResultsReconciled(2, 0, 23);

            Assert.That(simulationEvidence.Completed, Is.True);
        }

        // ------------------------------------------------------- the two vocabularies cannot conflict

        [Test]
        public void AnAnswerCarryingBothASuccessWordAndAFailureFragmentIsRefused()
        {
            //Substring matching is the hazard: a classifier that looked for "Done" anywhere would read
            //every one of these as a success. Success is matched WHOLE, and never when the answer also
            //carries a measured failure fragment - so a conflict resolves to refusal, not to success.
            string[] answers = new string[]
            {
                "Done, but Sizing Flow Failed",
                "Sizing Flow Failed - not Done",
                "Done. Plant Room Has Errors",
                "Failed to open the TSD file. Done.",
            };

            foreach (string answer in answers)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(SimulationDiagnostic.Classify(answer), Is.EqualTo(SimulationDiagnosticKind.KnownFailure), answer);
                    Assert.That(SimulationDiagnostic.IsSuccess(answer), Is.False, answer);
                    Assert.That(SimulationDiagnostic.IsFailure(answer), Is.True, answer);
                });
            }
        }

        [Test]
        public void NoDeclaredSuccessAnswerContainsADeclaredFailureFragment()
        {
            //The vocabularies must stay disjoint. If a future measured success answer ever contained a
            //failure fragment, the classifier would refuse it - correctly, but confusingly - and this
            //test is what says so at the moment the vocabulary is edited rather than months later.
            foreach (string answer in SimulationDiagnostic.KnownSuccessAnswers)
            {
                foreach (string fragment in SimulationDiagnostic.KnownFailureFragments)
                {
                    Assert.That(
                        answer.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase),
                        Is.LessThan(0),
                        string.Format("the success answer \"{0}\" carries the failure fragment \"{1}\".", answer, fragment));
                }
            }
        }

        [Test]
        public void AMeasuredSuccessIsStillNotTheGate()
        {
            SimulationEvidence simulationEvidence = InPlaceRun();

            Assert.Multiple(() =>
            {
                Assert.That(simulationEvidence.RecordCallReturned("Done"), Is.True);
                Assert.That(simulationEvidence.NativeDiagnosticKind, Is.EqualTo(SimulationDiagnosticKind.KnownSuccess));
                Assert.That(simulationEvidence.NativeDiagnostic, Is.EqualTo("Done"), "the exact native text is kept");
            });

            simulationEvidence.Conclude();

            Assert.That(
                simulationEvidence.Completed,
                Is.False,
                "TAS reporting success is positive evidence and not the gate - the results reconciliation is.");
        }
    }
}
