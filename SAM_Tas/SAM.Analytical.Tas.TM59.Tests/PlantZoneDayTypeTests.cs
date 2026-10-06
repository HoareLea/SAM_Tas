// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;

using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The generated plant zone is deliberately inactive on the design daytypes, and stays that way.</b>
    /// <para>
    /// <c>Modify.UpdateIZAMs</c> builds one small TAS zone per air handling unit - the unit's own plant
    /// zone, named after the unit ("MVHR-01" and so on) - and assigns it an internal condition on every
    /// daytype except <b>HDD</b> and <b>CDD</b>. TAS's pre-simulation check notices and warns: <i>"Zone
    /// 'MVHR-01' is missing internal conditions on some daytypes."</i>
    /// </para>
    /// <para>
    /// That warning is <b>expected and correct</b>. The generated zone is a duct volume standing in for a
    /// unit rather than a room, and it is not wanted active in the heating and cooling design-day sizing
    /// runs. Adding HDD and CDD internal conditions to silence the message would put it into those runs,
    /// which is the thing the exclusion exists to prevent - so this pins the exclusion, in the direction of
    /// "do not helpfully add them back".
    /// </para>
    /// <para>
    /// Pinned at <c>Query.DayType_PlantZoneInternalCondition</c>, the pure predicate <c>UpdateIZAMs</c>
    /// filters the calendar's daytypes through. No TBD or COM type appears here, so this runs with no TAS
    /// licence, install or COM server - the same rule the rest of this project follows.
    /// </para>
    /// </summary>
    [TestFixture]
    public class PlantZoneDayTypeTests
    {
        /// <summary>The heating design day gets no internal condition on the plant zone. Intentionally.</summary>
        [Test]
        public void HDD_GetsNoPlantZoneInternalCondition()
        {
            Assert.That(TasQuery.DayType_PlantZoneInternalCondition("HDD"), Is.False,
                "HDD must stay excluded: the generated plant zone is not wanted active in the heating design-day run, and TAS's 'missing internal conditions on some daytypes' warning is the expected consequence.");
        }

        /// <summary>And so does the cooling design day.</summary>
        [Test]
        public void CDD_GetsNoPlantZoneInternalCondition()
        {
            Assert.That(TasQuery.DayType_PlantZoneInternalCondition("CDD"), Is.False,
                "CDD must stay excluded, for the same reason as HDD.");
        }

        /// <summary>
        /// Every ordinary daytype does get one - the exclusion is the two design daytypes and nothing else,
        /// so the plant zone still runs through the whole simulated year.
        /// </summary>
        [TestCase("Weekday")]
        [TestCase("Weekend")]
        [TestCase("Holiday")]
        [TestCase("Monday")]
        [TestCase("Summer Weekday")]
        public void AnOrdinaryDayType_GetsAPlantZoneInternalCondition(string dayTypeName)
        {
            Assert.That(TasQuery.DayType_PlantZoneInternalCondition(dayTypeName), Is.True);
        }

        /// <summary>
        /// The names are matched exactly, not loosely. A daytype merely CONTAINING "HDD" is a different
        /// daytype and keeps its internal condition; dropping it would silently shrink the schedule the
        /// plant zone runs on.
        /// </summary>
        [TestCase("HDDX")]
        [TestCase("hdd")]
        [TestCase("CDD Weekday")]
        [TestCase(" HDD")]
        public void ADayTypeThatIsMerelySimilar_StillGetsOne(string dayTypeName)
        {
            Assert.That(TasQuery.DayType_PlantZoneInternalCondition(dayTypeName), Is.True);
        }

        /// <summary>
        /// An unnamed daytype is kept, and asking does not throw. A TBD calendar can carry one, and the
        /// predicate answers one question only - whether this is one of the two excluded design daytypes.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void AnUnnamedDayType_StillGetsOne(string dayTypeName)
        {
            Assert.That(TasQuery.DayType_PlantZoneInternalCondition(dayTypeName), Is.True);
        }
    }
}
