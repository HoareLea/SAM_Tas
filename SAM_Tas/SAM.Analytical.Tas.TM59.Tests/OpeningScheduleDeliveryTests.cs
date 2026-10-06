// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Per-child schedule-delivery diagnostics for <c>MultipleOpeningProperties</c>.</b>
    /// <para>
    /// A multiple-opening aperture writes one TBD aperture type per child, and the returned list is
    /// COMPACTED: a refused child is absent, so returned index i is not necessarily child index i. The
    /// diagnostic these tests pin down checks every child that stated a schedule against the aperture type
    /// THAT child's write returned - never against the first aperture type, which is what the previous
    /// implementation did. Checking child 0 only produced both a false "missing schedule" warning (child 0
    /// unrestricted, child 1 scheduled and delivered) and a hidden failure (child 0 scheduled, child 1
    /// scheduled but refused).
    /// </para>
    /// <para>
    /// No TAS COM: <c>Query.OpeningScheduleRequests</c> resolves each child's request from SAM objects only,
    /// and <c>Query.UndeliveredOpeningScheduleRequests</c> pairs those requests with per-child delivery
    /// flags - the production code derives those flags by reading each returned aperture type's schedule
    /// back off the TBD, and here they are simply stated per case.
    /// </para>
    /// </summary>
    [TestFixture]
    public class OpeningScheduleDeliveryTests
    {
        private static PartOOpeningProperties Unrestricted()
        {
            return new PartOOpeningProperties(1.2, 1.0, 30.0);
        }

        private static PartOOpeningProperties NightClosed()
        {
            return new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed);
        }

        private static MultipleOpeningProperties Multiple(params ISingleOpeningProperties[] children)
        {
            return new MultipleOpeningProperties(new List<ISingleOpeningProperties>(children));
        }

        // -------------------------------------------------------------------------------------------------
        // OpeningScheduleRequests: which children state a schedule
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void Requests_UnrestrictedPlusNightClosed_OnlyChild1Requests()
        {
            List<bool> requests = Multiple(Unrestricted(), NightClosed()).OpeningScheduleRequests();

            Assert.That(requests, Is.EqualTo(new[] { false, true }),
                "child 0 requests nothing, child 1 requests - the request is not a property of child 0");
        }

        [Test]
        public void Requests_NightClosedPlusUnrestricted_OnlyChild0Requests()
        {
            List<bool> requests = Multiple(NightClosed(), Unrestricted()).OpeningScheduleRequests();

            Assert.That(requests, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void Requests_NightClosedPlusNightClosed_BothRequest()
        {
            List<bool> requests = Multiple(NightClosed(), NightClosed()).OpeningScheduleRequests();

            Assert.That(requests, Is.EqualTo(new[] { true, true }));
        }

        [Test]
        public void Requests_SingleNightClosed_Requests()
        {
            Assert.That(NightClosed().OpeningScheduleRequests(), Is.EqualTo(new[] { true }));
        }

        /// <summary>
        /// A child that STATES a schedule it cannot supply still counts as requesting: its write is refused,
        /// and a refused request is exactly what the delivery check must catch rather than skip.
        /// </summary>
        [Test]
        public void Requests_StatedButUnusableSource_StillRequests()
        {
            ProfileOpeningProperties unusable = new ProfileOpeningProperties(0.6, new Profile("Empty", ProfileGroup.Ventilation.Text()));

            Assert.That(Multiple(Unrestricted(), unusable).OpeningScheduleRequests(), Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void Requests_NullOpeningProperties_NoRequests()
        {
            Assert.That(((IOpeningProperties)null).OpeningScheduleRequests(), Is.Empty);
        }

        // -------------------------------------------------------------------------------------------------
        // UndeliveredOpeningScheduleRequests: the pairing the diagnostic now makes
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>The false warning this fix removes.</b> Child 0 is unrestricted, child 1 requested a schedule
        /// and got it. The returned aperture type list is [child 0's (no schedule), child 1's (schedule)] -
        /// checking the FIRST entry, as the previous implementation did, reported a missing schedule that
        /// had in fact arrived.
        /// </summary>
        [Test]
        public void Undelivered_UnrestrictedFirst_ScheduleDeliveredOnChild1_ReportsNothing()
        {
            List<int> undelivered = Multiple(Unrestricted(), NightClosed())
                .UndeliveredOpeningScheduleRequests(new[] { false, true });

            Assert.That(undelivered, Is.Empty);
        }

        [Test]
        public void Undelivered_NightClosedFirst_ScheduleDeliveredOnChild0_ReportsNothing()
        {
            List<int> undelivered = Multiple(NightClosed(), Unrestricted())
                .UndeliveredOpeningScheduleRequests(new[] { true, false });

            Assert.That(undelivered, Is.Empty);
        }

        /// <summary>
        /// <b>The hidden failure this fix removes.</b> Both children requested; only child 0's schedule
        /// arrived. Checking the first aperture type counted this as fully delivered and said nothing about
        /// child 1.
        /// </summary>
        [Test]
        public void Undelivered_NightClosedPlusNightClosed_Child1Failed_ReportsChild1()
        {
            List<int> undelivered = Multiple(NightClosed(), NightClosed())
                .UndeliveredOpeningScheduleRequests(new[] { true, false });

            Assert.That(undelivered, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Undelivered_NightClosedPlusNightClosed_Child0Failed_ReportsChild0()
        {
            List<int> undelivered = Multiple(NightClosed(), NightClosed())
                .UndeliveredOpeningScheduleRequests(new[] { false, true });

            Assert.That(undelivered, Is.EqualTo(new[] { 0 }),
                "a schedule on child 1 must not mask child 0's missing one");
        }

        [Test]
        public void Undelivered_NightClosedPlusNightClosed_BothDelivered_ReportsNothing()
        {
            List<int> undelivered = Multiple(NightClosed(), NightClosed())
                .UndeliveredOpeningScheduleRequests(new[] { true, true });

            Assert.That(undelivered, Is.Empty);
        }

        [Test]
        public void Undelivered_SingleNightClosed_Refused_ReportsChild0()
        {
            List<int> undelivered = NightClosed().UndeliveredOpeningScheduleRequests(new[] { false });

            Assert.That(undelivered, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Undelivered_SingleUnrestricted_NeverReported()
        {
            Assert.That(Unrestricted().UndeliveredOpeningScheduleRequests(new[] { false }), Is.Empty);
            Assert.That(Unrestricted().UndeliveredOpeningScheduleRequests(new bool[0]), Is.Empty,
                "a child that requested nothing has nothing to deliver, whatever the write did");
        }

        /// <summary>
        /// A delivery list shorter than the child list - every uncompacted position a refused write never
        /// filled - is treated as not delivered, never as delivered.
        /// </summary>
        [Test]
        public void Undelivered_MissingDeliveryEntries_AreNotDelivered()
        {
            List<int> undelivered = Multiple(NightClosed(), NightClosed())
                .UndeliveredOpeningScheduleRequests(new[] { true });

            Assert.That(undelivered, Is.EqualTo(new[] { 1 }));
        }
    }
}
