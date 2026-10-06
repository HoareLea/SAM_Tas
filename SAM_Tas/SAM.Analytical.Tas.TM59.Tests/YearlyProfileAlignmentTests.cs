// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// A SAM yearly profile must land in a TBD yearly profile hour for hour: SAM's 0-based hour <c>k</c> in
    /// TAS's 1-based slot <c>k + 1</c>, which is the alignment the TSD annual results and the import both use.
    /// <para>
    /// Licensed TAS measured the export writing every yearly profile <b>one hour early</b> with hour 8760
    /// duplicated: <c>Modify.Update</c> handed <c>SetYearlyValues</c> a 0-based <c>float[8760]</c>, and TAS
    /// ignores element 0. The same run showed <c>Modify.UpdateACCI</c> shifted identically. These tests run
    /// the REAL writer and importer against <see cref="FakeProfile"/>, which models that measured behaviour -
    /// so they fail on the old writer, where the previous fake (copy from element 0) passed it.
    /// </para>
    /// </summary>
    [TestFixture]
    public class YearlyProfileAlignmentTests
    {
        private const int Hours = 8760;

        /// <summary>Every hour distinct, so any shift or duplicate is visible.</summary>
        private static List<double> Year()
        {
            return Enumerable.Range(0, Hours).Select(k => 1000.0 + k).ToList();
        }

        [Test]
        public void TheFakeBehavesAsLicensedTasWasMeasured()
        {
            //Pins the stand-in to the licensed measurement, so the tests below mean something.
            FakeProfile profile = new FakeProfile { type = TBD.ProfileTypes.ticYearlyProfile };

            profile.SetYearlyValues(Enumerable.Range(0, Hours).Select(i => 1000f + i).ToArray());
            Assert.That(profile.get_yearlyValues(1), Is.EqualTo(1001f), "element 0 is ignored");
            Assert.That(profile.get_yearlyValues(Hours - 1), Is.EqualTo(1000f + Hours - 1));
            Assert.That(profile.get_yearlyValues(Hours), Is.EqualTo(1000f + Hours - 1), "the last element is repeated");

            profile.SetYearlyValues(Enumerable.Range(0, Hours + 1).Select(i => 1000f + i).ToArray());
            Assert.That(profile.get_yearlyValues(1), Is.EqualTo(1001f));
            Assert.That(profile.get_yearlyValues(Hours), Is.EqualTo(1000f + Hours), "an 8761 array maps exactly");

            System.Array array = (System.Array)profile.GetYearlyValues();
            Assert.That(array.GetLowerBound(0), Is.EqualTo(1));
            Assert.That(array.GetUpperBound(0), Is.EqualTo(Hours));
        }

        [TestCase(TBD.Profiles.ticUL)]
        [TestCase(TBD.Profiles.ticOSG)]
        public void Export_WritesSamHourKIntoSlotKPlusOne_ForEveryHour(TBD.Profiles slot)
        {
            Profile profile = new Profile("Yearly", ProfileType.Cooling, Year());
            FakeProfile profile_TBD = new FakeProfile { profile = slot };

            Assert.That(SAM.Analytical.Tas.Modify.Update(profile_TBD, profile, 1), Is.True);

            Assert.That(profile_TBD.type, Is.EqualTo(TBD.ProfileTypes.ticYearlyProfile));
            List<int> misaligned = new List<int>();
            for (int hour = 1; hour <= Hours; hour++)
            {
                if (profile_TBD.get_yearlyValues(hour) != (float)(1000.0 + hour - 1))
                {
                    misaligned.Add(hour);
                }
            }

            Assert.That(misaligned, Is.Empty, string.Format("{0} of {1} slots misaligned; slot 1 holds {2}, slot {1} holds {3}.",
                misaligned.Count, Hours, profile_TBD.get_yearlyValues(1), profile_TBD.get_yearlyValues(Hours)));
        }

        [Test]
        public void ExportThenImport_IsAFixedPoint_OverThreeGenerations()
        {
            //The shift compounded: the import reads slot k + 1 back as hour k, correctly, so each export ->
            //import generation moved a yearly profile one more hour earlier.
            List<double> expected = Year();
            Profile profile = new Profile("Yearly", ProfileType.Cooling, expected);

            for (int generation = 1; generation <= 3; generation++)
            {
                FakeProfile profile_TBD = new FakeProfile();
                Assert.That(SAM.Analytical.Tas.Modify.Update(profile_TBD, profile, 1), Is.True);

                List<double> imported = SAM.Core.Tas.Query.Values(profile_TBD);
                Assert.That(imported, Is.EqualTo(expected).AsCollection, "generation " + generation);

                profile = new Profile("Yearly", ProfileType.Cooling, imported);
            }
        }

        [TestCase(0)]
        [TestCase(Hours - 1)]
        [TestCase(Hours + 1)]
        public void UpdateYearlyValues_RefusesAnythingButOneYear_AndWritesNothing(int count)
        {
            FakeProfile profile_TBD = new FakeProfile { type = TBD.ProfileTypes.ticYearlyProfile };
            profile_TBD.set_yearlyValues(1, 42f);

            Assert.That(SAM.Analytical.Tas.Modify.UpdateYearlyValues(profile_TBD, new float[count]), Is.False);
            Assert.That(profile_TBD.get_yearlyValues(1), Is.EqualTo(42f));
        }

        [Test]
        public void UpdateYearlyValues_WritesZeroBasedHourKIntoSlotKPlusOne()
        {
            FakeProfile profile_TBD = new FakeProfile { type = TBD.ProfileTypes.ticYearlyProfile };

            Assert.That(SAM.Analytical.Tas.Modify.UpdateYearlyValues(profile_TBD, Enumerable.Range(0, Hours).Select(k => 1000f + k).ToArray()), Is.True);

            for (int hour = 1; hour <= Hours; hour++)
            {
                Assert.That(profile_TBD.get_yearlyValues(hour), Is.EqualTo(1000f + hour - 1), "slot " + hour);
            }
        }
    }
}
