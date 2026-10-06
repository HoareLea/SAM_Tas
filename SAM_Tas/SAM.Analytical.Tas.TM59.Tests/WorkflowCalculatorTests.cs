// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Each <see cref="WorkflowCalculator.Calculate"/> call owns a fresh <see cref="WorkflowCalculator.Notes"/>
    /// state.</b>
    /// <para>
    /// A calculator instance can be re-run. The notes list is cleared at the very top of
    /// <see cref="WorkflowCalculator.Calculate"/>, ahead of the validation gate that rejects a null model or
    /// null settings: previously the clear sat behind that gate, so a rejected run returned null with the
    /// PREVIOUS run's notes still visible - a caller reading <see cref="WorkflowCalculator.Notes"/> after a
    /// refused run would have reported notes that belonged to an earlier calculation.
    /// </para>
    /// <para>
    /// No TAS COM: the invalid-input paths under test return before anything touches a TBD type, and the
    /// prior run's notes are simulated by writing the private list directly - the only way to populate it
    /// without a licensed TAS.
    /// </para>
    /// </summary>
    [TestFixture]
    public class WorkflowCalculatorTests
    {
        /// <summary>Stands in for a completed previous run: one leftover note in the private list.</summary>
        private static void SimulatePreviousRun(WorkflowCalculator workflowCalculator)
        {
            FieldInfo fieldInfo = typeof(WorkflowCalculator).GetField("notes", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(fieldInfo, Is.Not.Null, "the private notes list this test fills in place of a real previous run");

            ((List<string>)fieldInfo.GetValue(workflowCalculator)).Add("A note left over from a previous run.");
        }

        [Test]
        public void Calculate_NullModel_AfterAPreviousRun_ExposesNoStaleNotes()
        {
            WorkflowCalculator workflowCalculator = new WorkflowCalculator();
            SimulatePreviousRun(workflowCalculator);
            Assert.That(workflowCalculator.Notes, Has.Count.EqualTo(1), "control: the previous run's note is visible before the rejected run");

            Assert.That(workflowCalculator.Calculate(null), Is.Null);

            Assert.That(workflowCalculator.Notes, Is.Empty, "a rejected run must not expose the previous run's notes");
        }

        [Test]
        public void Calculate_NullSettings_AfterAPreviousRun_ExposesNoStaleNotes()
        {
            WorkflowCalculator workflowCalculator = new WorkflowCalculator();
            SimulatePreviousRun(workflowCalculator);
            Assert.That(workflowCalculator.Notes, Has.Count.EqualTo(1), "control: the previous run's note is visible before the rejected run");

            AnalyticalModel analyticalModel = new AnalyticalModel("Rejected Run", null, null, null, new AdjacencyCluster());
            Assert.That(workflowCalculator.Calculate(analyticalModel), Is.Null, "no WorkflowSettings, so the run is rejected");

            Assert.That(workflowCalculator.Notes, Is.Empty, "a rejected run must not expose the previous run's notes");
        }

        [Test]
        public void Calculate_RejectedOnAFreshInstance_StillExposesNoNotes()
        {
            WorkflowCalculator workflowCalculator = new WorkflowCalculator();

            Assert.That(workflowCalculator.Calculate(null), Is.Null);
            Assert.That(workflowCalculator.Notes, Is.Empty);
        }
    }
}
