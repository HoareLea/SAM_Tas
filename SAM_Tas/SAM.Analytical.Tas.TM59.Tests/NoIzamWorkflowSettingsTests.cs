// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Tas;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// A-1 and I-4. The two no-IZAM switches exist, default off, and survive a round trip.
    /// <para>
    /// <b>I-4 is the load-bearing test here.</b> Part O Iteration 3 needs a genuinely IZAM-free thermal
    /// source, but every existing caller - the Grasshopper components, the benchmark CLI, the Iteration 1a
    /// and 2B runs whose results are frozen - must behave exactly as before. Both switches therefore default
    /// to <c>false</c>, and <c>AddIZAMs</c> keeps its own default of <c>true</c>. If any of those three moves,
    /// a frozen result moves with it.
    /// </para>
    /// <para>
    /// A-1 records why the new <c>RemoveIZAMs</c> switch is needed at all: <c>AddIZAMs = false</c> only skips
    /// <i>creation</i>. It removes nothing, so IZAMs inherited from a canonical warm start or a reused TBD
    /// survive it. That is a statement about the settings contract; the behavioural half - that a sweep
    /// actually clears an inherited IZAM - is native acceptance evidence item 1, because it needs a real
    /// <c>TBD.Building</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    public class NoIzamWorkflowSettingsTests
    {
        [Test]
        public void Defaults_AreUnchangedForEveryExistingCaller()
        {
            WorkflowSettings workflowSettings = new WorkflowSettings();

            Assert.Multiple(() =>
            {
                Assert.That(
                    workflowSettings.AddIZAMs,
                    Is.True,
                    "AddIZAMs must keep its existing default. Every Iteration 1a/2B result was produced with it true.");

                Assert.That(
                    workflowSettings.RemoveIZAMs,
                    Is.False,
                    "RemoveIZAMs must default OFF so no existing caller starts sweeping IZAMs it wanted.");

                Assert.That(
                    workflowSettings.RemoveMechanicalVentilationGains,
                    Is.False,
                    "RemoveMechanicalVentilationGains must default OFF so no existing caller loses its ticV.");
            });
        }

        [Test]
        public void Defaults_AreCarriedByTheCopyConstructor()
        {
            WorkflowSettings workflowSettings = new WorkflowSettings(new WorkflowSettings());

            Assert.Multiple(() =>
            {
                Assert.That(workflowSettings.AddIZAMs, Is.True);
                Assert.That(workflowSettings.RemoveIZAMs, Is.False);
                Assert.That(workflowSettings.RemoveMechanicalVentilationGains, Is.False);
            });
        }

        [Test]
        public void CopyConstructor_CarriesBothSwitches()
        {
            WorkflowSettings source = new WorkflowSettings
            {
                AddIZAMs = false,
                RemoveIZAMs = true,
                RemoveMechanicalVentilationGains = true,
            };

            WorkflowSettings copy = new WorkflowSettings(source);

            Assert.Multiple(() =>
            {
                Assert.That(copy.AddIZAMs, Is.False);
                Assert.That(copy.RemoveIZAMs, Is.True);
                Assert.That(copy.RemoveMechanicalVentilationGains, Is.True);
            });
        }

        [Test]
        public void JsonRoundTrip_CarriesBothSwitches()
        {
            WorkflowSettings source = new WorkflowSettings
            {
                AddIZAMs = false,
                RemoveIZAMs = true,
                RemoveMechanicalVentilationGains = true,
            };

            JsonObject jsonObject = source.ToJsonObject();

            Assert.That(jsonObject, Is.Not.Null);

            WorkflowSettings destination = new WorkflowSettings(jsonObject);

            Assert.Multiple(() =>
            {
                Assert.That(destination.AddIZAMs, Is.False);
                Assert.That(destination.RemoveIZAMs, Is.True);
                Assert.That(destination.RemoveMechanicalVentilationGains, Is.True);
            });
        }

        [Test]
        public void JsonWithoutTheNewKeys_StillReadsAsOff()
        {
            // Settings persisted before this PR carry neither key. They must read back as OFF, not as the
            // default(bool) of some other path, and certainly not as ON.
            WorkflowSettings source = new WorkflowSettings();
            JsonObject jsonObject = source.ToJsonObject();

            jsonObject.Remove("RemoveIZAMs");
            jsonObject.Remove("RemoveMechanicalVentilationGains");

            WorkflowSettings destination = new WorkflowSettings(jsonObject);

            Assert.Multiple(() =>
            {
                Assert.That(destination.RemoveIZAMs, Is.False);
                Assert.That(destination.RemoveMechanicalVentilationGains, Is.False);
                Assert.That(destination.AddIZAMs, Is.True, "And the pre-existing switch is unaffected.");
            });
        }

        [Test]
        public void AddIZAMsFalse_IsNotByItselfAnIzamFreeSource()
        {
            // A-1, stated as a contract rather than as behaviour: the two switches are INDEPENDENT. Turning
            // creation off leaves removal off, which is exactly why RemoveIZAMs had to be added - an
            // inherited IZAM survives AddIZAMs = false. The behavioural half is native evidence item 1.
            WorkflowSettings workflowSettings = new WorkflowSettings { AddIZAMs = false };

            Assert.That(
                workflowSettings.RemoveIZAMs,
                Is.False,
                "AddIZAMs = false must not imply removal: it skips creation only, and an inherited IZAM "
                + "would survive it. A caller wanting an IZAM-free TBD must ask for the sweep explicitly.");
        }
    }
}
