// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Starting a workflow run from an already-converted canonical TBD</b> -
    /// <see cref="WorkflowSettings.Path_TBD_Canonical"/>.
    ///
    /// <para><b>Why the seam exists</b></para>
    /// <para>
    /// An Approved Document O Iteration 2B optimisation runs the same thermal case ten times over ten
    /// designs, and between rounds only the ventilation state changes. Measured on the licensed acceptance
    /// model, the geometry conversion is 41.6 s of a 64.2 s round while the full-year simulation itself is
    /// 3.6 s - so converting once and starting every later round from that TBD removes most of the work
    /// without removing any of the physics.
    /// </para>
    ///
    /// <para><b>What is tested here, and what is not</b></para>
    /// <para>
    /// The <b>guards</b>, which are the part that can silently destroy evidence if they are wrong: a
    /// contradictory pair of instructions, a canonical file that is not there, and - most importantly - a
    /// canonical path that is also the run's own target, which would have each round overwrite the baseline
    /// every later round depends on. All three return before anything touches a TBD type, so they are
    /// tested without a licensed TAS, exactly as <see cref="WorkflowCalculatorTests"/> is.
    /// </para>
    /// <para>
    /// That the warm-started run then produces the <i>same engineering result</i> as the full conversion is
    /// not a unit-testable claim - it needs a licensed TAS and a real model - and is proven by the A/B
    /// comparison in the acceptance evidence instead.
    /// </para>
    /// </summary>
    [TestFixture]
    public class WorkflowCanonicalTBDTests
    {
        /// <summary>
        /// A gbXML to convert and a canonical TBD to start from are contradictory instructions - one says
        /// the geometry must be converted, the other that it already is. Choosing between them would be the
        /// calculator deciding something only the caller can, so the run is refused, and refused
        /// <b>before</b> the setup that deletes an existing T3D and TBD.
        /// </summary>
        [Test]
        public void Calculate_GivenBothAgbXMLAndACanonicalTBD_IsRefusedWithItsReasonAndDeletesNothing()
        {
            string directory = Directory(out string path_TBD);

            string path_Canonical = Path.Combine(directory, "Canonical.tbd");
            string path_gbXML = Path.Combine(directory, "Model.xml");

            File.WriteAllText(path_Canonical, "canonical");
            File.WriteAllText(path_gbXML, "gbxml");
            File.WriteAllText(path_TBD, "this round's tbd");

            WorkflowCalculator workflowCalculator = new WorkflowCalculator(new WorkflowSettings()
            {
                Path_TBD = path_TBD,
                Path_TBD_Canonical = path_Canonical,
                Path_gbXML = path_gbXML,
            });

            Assert.That(workflowCalculator.Calculate(Model()), Is.Null, "contradictory instructions are refused");

            Assert.That(workflowCalculator.Notes, Has.Count.EqualTo(1));
            Assert.That(workflowCalculator.Notes[0], Does.Contain("contradictory"));

            //Nothing was touched - which matters because the conversion path deletes both of these.
            Assert.That(File.ReadAllText(path_Canonical), Is.EqualTo("canonical"));
            Assert.That(File.ReadAllText(path_TBD), Is.EqualTo("this round's tbd"));

            Cleanup(directory);
        }

        /// <summary>
        /// A canonical TBD that is not on disk is not a warm start - it is a caller with nothing to start
        /// from. Refused by name, rather than silently falling through to a conversion the caller did not
        /// ask for: whether to fall back to the full path is the caller's decision, and making it here
        /// would hide the fact that the baseline had gone.
        /// </summary>
        [Test]
        public void Calculate_GivenACanonicalTBDThatDoesNotExist_IsRefusedByName()
        {
            string directory = Directory(out string path_TBD);

            string path_Canonical = Path.Combine(directory, "Missing.tbd");

            WorkflowCalculator workflowCalculator = new WorkflowCalculator(new WorkflowSettings()
            {
                Path_TBD = path_TBD,
                Path_TBD_Canonical = path_Canonical,
            });

            Assert.That(workflowCalculator.Calculate(Model()), Is.Null);

            Assert.That(workflowCalculator.Notes, Has.Count.EqualTo(1));
            Assert.That(workflowCalculator.Notes[0], Does.Contain("Missing.tbd"));
            Assert.That(workflowCalculator.Notes[0], Does.Contain("does not exist"));

            Assert.That(File.Exists(path_TBD), Is.False, "nothing was created");

            Cleanup(directory);
        }

        /// <summary>
        /// <b>The guard that matters most.</b> A canonical path that is also the run's own TBD would have
        /// the copy overwrite its own source - so every later round would start from whatever the last one
        /// left behind, which is precisely the cumulative mutation a canonical baseline exists to prevent.
        /// Refused rather than quietly resolved into a no-op copy, because a caller that has confused the
        /// two has lost its baseline and needs to know.
        /// </summary>
        [Test]
        public void Calculate_WhereTheCanonicalTBDIsTheRunsOwnTBD_IsRefusedRatherThanOverwritingTheBaseline()
        {
            string directory = Directory(out string path_TBD);

            File.WriteAllText(path_TBD, "the baseline every later round depends on");

            WorkflowCalculator workflowCalculator = new WorkflowCalculator(new WorkflowSettings()
            {
                Path_TBD = path_TBD,
                Path_TBD_Canonical = path_TBD,
            });

            Assert.That(workflowCalculator.Calculate(Model()), Is.Null);

            Assert.That(workflowCalculator.Notes, Has.Count.EqualTo(1));
            Assert.That(workflowCalculator.Notes[0], Does.Contain("same file"));

            Assert.That(File.ReadAllText(path_TBD), Is.EqualTo("the baseline every later round depends on"));

            Cleanup(directory);
        }

        /// <summary>The same guard, where the two paths differ only in how they are written.</summary>
        [Test]
        public void Calculate_WhereTheTwoPathsDifferOnlyInSpelling_IsStillTheSameFile()
        {
            string directory = Directory(out string path_TBD);

            File.WriteAllText(path_TBD, "the baseline");

            WorkflowCalculator workflowCalculator = new WorkflowCalculator(new WorkflowSettings()
            {
                Path_TBD = path_TBD,
                //The same file, reached through the directory rather than named directly - and upper cased,
                //because Windows paths are not case sensitive and a case-sensitive comparison would let this
                //through.
                Path_TBD_Canonical = Path.Combine(directory, ".", Path.GetFileName(path_TBD)).ToUpperInvariant(),
            });

            Assert.That(workflowCalculator.Calculate(Model()), Is.Null);

            Assert.That(workflowCalculator.Notes, Has.Count.EqualTo(1));
            Assert.That(workflowCalculator.Notes[0], Does.Contain("same file"));

            Assert.That(File.ReadAllText(path_TBD), Is.EqualTo("the baseline"));

            Cleanup(directory);
        }

        /// <summary>
        /// A refused warm start leaves no previous run's notes visible either - the same invariant
        /// <see cref="WorkflowCalculatorTests"/> pins for the other rejected inputs. A run reports its own
        /// reason and nothing else.
        /// </summary>
        [Test]
        public void Calculate_RefusedWarmStart_ReportsOnlyItsOwnReason()
        {
            string directory = Directory(out string path_TBD);

            WorkflowSettings workflowSettings = new WorkflowSettings()
            {
                Path_TBD = path_TBD,
                Path_TBD_Canonical = Path.Combine(directory, "Missing.tbd"),
            };

            WorkflowCalculator workflowCalculator = new WorkflowCalculator(workflowSettings);

            Assert.That(workflowCalculator.Calculate(Model()), Is.Null);
            Assert.That(workflowCalculator.Notes, Has.Count.EqualTo(1));

            //Run again: still exactly one note, its own.
            Assert.That(workflowCalculator.Calculate(Model()), Is.Null);
            Assert.That(workflowCalculator.Notes, Has.Count.EqualTo(1));

            Cleanup(directory);
        }

        /// <summary>
        /// The setting survives being copied and round-tripped, so a caller that persists its workflow
        /// settings does not silently lose its warm start and convert every round in full.
        /// </summary>
        [Test]
        public void Path_TBD_Canonical_SurvivesACopyAndAJsonRoundTrip()
        {
            WorkflowSettings workflowSettings = new WorkflowSettings()
            {
                Path_TBD = @"C:\out\Project-Opt03.tbd",
                Path_TBD_Canonical = @"C:\out\Project.tbd",
            };

            Assert.That(new WorkflowSettings(workflowSettings).Path_TBD_Canonical, Is.EqualTo(@"C:\out\Project.tbd"));

            WorkflowSettings workflowSettings_RoundTripped = new WorkflowSettings(workflowSettings.ToJsonObject());

            Assert.That(workflowSettings_RoundTripped.Path_TBD_Canonical, Is.EqualTo(@"C:\out\Project.tbd"));
            Assert.That(workflowSettings_RoundTripped.Path_TBD, Is.EqualTo(@"C:\out\Project-Opt03.tbd"));

            //And a settings object with none stays null rather than defaulting to its own TBD, which would
            //turn every ordinary run into a self-overwriting warm start.
            Assert.That(new WorkflowSettings(new WorkflowSettings() { Path_TBD = @"C:\out\Project.tbd" }.ToJsonObject()).Path_TBD_Canonical, Is.Null);
        }

        // ---- Fixture ---------------------------------------------------------------------------------------

        private static AnalyticalModel Model()
        {
            return new AnalyticalModel("Warm Start Fixture", null, null, null, new AdjacencyCluster());
        }

        private static string Directory(out string path_TBD)
        {
            string result = Path.Combine(Path.GetTempPath(), string.Format("SAM_WarmStart_{0}", System.Guid.NewGuid()));

            System.IO.Directory.CreateDirectory(result);

            path_TBD = Path.Combine(result, "Round.tbd");

            return result;
        }

        private static void Cleanup(string directory)
        {
            try
            {
                System.IO.Directory.Delete(directory, true);
            }
            catch (IOException)
            {
                //A temp directory left behind is not a test failure.
            }
        }
    }
}
