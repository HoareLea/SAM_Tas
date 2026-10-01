// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// The frozen #111 parity configuration is continuous operation at factor 1.0. These pin which native
    /// fan operation carriers the explicit ventilation route accepts as stating it.
    /// <para>
    /// The native facts behind each case were measured on licensed TAS and are recorded in
    /// the SAM_Tas PR record: a yearly table on in every hour answers
    /// <c>GetNumOperableHours() == 8760</c>; the shipped template's occupancy function schedule throws
    /// <c>"Not a Yearly Schedule"</c> there and switches the fans with zone demand.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ContinuousOperationRefusalTests
    {
        private static int? Type(global::TPD.tpdScheduleType tpdScheduleType) => (int)tpdScheduleType;

        [Test]
        public void AYearlyScheduleOnInEveryHourOfTheYear_IsContinuousOperation()
        {
            Assert.That(
                SAM.Analytical.Tas.TPD.Query.ContinuousOperationRefusal(Type(global::TPD.tpdScheduleType.tpdScheduleYearly), 8760),
                Is.Null);
        }

        [TestCase(8736)]
        [TestCase(0)]
        [TestCase(8759)]
        public void AYearlyScheduleOffInAnyHour_IsRefused_AndSaysHowManyHoursItCovers(int operableHours)
        {
            string refusal = SAM.Analytical.Tas.TPD.Query.ContinuousOperationRefusal(Type(global::TPD.tpdScheduleType.tpdScheduleYearly), operableHours);

            Assert.That(refusal, Is.Not.Null);
            Assert.That(refusal, Does.Contain(operableHours.ToString(System.Globalization.CultureInfo.InvariantCulture) + " of 8760"));
        }

        [Test]
        public void AYearlyScheduleWhoseOperableHoursCouldNotBeRead_IsRefused()
        {
            Assert.That(
                SAM.Analytical.Tas.TPD.Query.ContinuousOperationRefusal(Type(global::TPD.tpdScheduleType.tpdScheduleYearly), null),
                Is.Not.Null);
        }

        [Test]
        public void AFunctionSchedule_IsRefused_EvenThoughItCanHappenToRunEveryHour()
        {
            //The shipped MV.json "Occupancy Schedule": tpdScheduleFunctionAllZonesLoad on occupant-sensible
            //load. It ran continuously on the acceptance fixture only because some room was occupied in
            //every hour; it is demand-driven, which is not the frozen factor 1.0.
            string refusal = SAM.Analytical.Tas.TPD.Query.ContinuousOperationRefusal(Type(global::TPD.tpdScheduleType.tpdScheduleFunction), null);

            Assert.That(refusal, Is.Not.Null);
            Assert.That(refusal, Does.Contain("demand"));
        }

        [Test]
        public void AFunctionSchedule_IsRefused_WhateverOperableHoursIsClaimedForIt()
        {
            Assert.That(
                SAM.Analytical.Tas.TPD.Query.ContinuousOperationRefusal(Type(global::TPD.tpdScheduleType.tpdScheduleFunction), 8760),
                Is.Not.Null);
        }

        [Test]
        public void AnHourlyDayTypeSchedule_IsRefused_BecauseItsContinuityCannotBeReadBack()
        {
            Assert.That(
                SAM.Analytical.Tas.TPD.Query.ContinuousOperationRefusal(Type(global::TPD.tpdScheduleType.tpdScheduleHourly), 8760),
                Is.Not.Null);
        }

        [Test]
        public void NoScheduleAtAll_IsRefused()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SAM.Analytical.Tas.TPD.Query.ContinuousOperationRefusal(null, null), Is.Not.Null);
                Assert.That(SAM.Analytical.Tas.TPD.Query.ContinuousOperationRefusal(Type(global::TPD.tpdScheduleType.tpdScheduleNone), null), Is.Not.Null);
            });
        }
    }
}
