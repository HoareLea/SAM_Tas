// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Tas;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// The no-IZAM fail-closed decision: when the IZAM sweep was requested and an
    /// IZAM survives it, the workflow must refuse, not note-and-continue.
    /// <para>
    /// <b>Why the helper is what is tested.</b> Asking whether an IZAM survived needs a live
    /// <c>TBD.Building</c>, which no unit test can stand up without licensed TAS. What can be pinned is
    /// the decision that answer forces - refuse, or proceed - which is the exact defect: previously the
    /// surviving-IZAM answer produced a note and the run went on to save, size and simulate. The helper
    /// is the decision, and <see cref="WorkflowCalculator"/> returns null (its established refusal
    /// convention) whenever the helper answers non-null, so <c>NoIzamThermalSource</c> records the call
    /// as failed and the source cannot come back accepted.
    /// </para>
    /// </summary>
    [TestFixture]
    public class NoIzamSurvivorRefusalTests
    {
        [Test]
        public void SweepRequested_AndAnIzamSurvived_Refuses()
        {
            string refusal = SAM.Analytical.Tas.Query.IzamSurvivorRefusal(true, true);

            Assert.Multiple(() =>
            {
                Assert.That(
                    refusal,
                    Is.Not.Null,
                    "A surviving IZAM under a requested sweep must refuse - the TBD is not IZAM-free, " +
                    "and the workflow must not continue to save/size/simulate it as the no-IZAM source.");

                Assert.That(
                    refusal,
                    Does.Contain("NOT IZAM-free"),
                    "The refusal must say why: the no-IZAM contract is broken.");
            });
        }

        [Test]
        public void SweepRequested_AndTheBuildingIsClean_Proceeds()
        {
            Assert.That(
                SAM.Analytical.Tas.Query.IzamSurvivorRefusal(true, false),
                Is.Null,
                "Zero IZAMs remaining is the success case and must stay one: no refusal, existing behaviour preserved.");
        }

        [Test]
        public void SweepNotRequested_AnyAnswer_IsNoRefusal()
        {
            // An ordinary IZAM-bearing run never asked for the sweep, so a surviving IZAM there is none
            // of this decision's business - refusal would break every existing IZAM route.
            Assert.Multiple(() =>
            {
                Assert.That(SAM.Analytical.Tas.Query.IzamSurvivorRefusal(false, true), Is.Null);
                Assert.That(SAM.Analytical.Tas.Query.IzamSurvivorRefusal(false, false), Is.Null);
            });
        }
    }
}
