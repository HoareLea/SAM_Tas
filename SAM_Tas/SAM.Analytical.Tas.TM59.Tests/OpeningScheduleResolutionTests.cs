// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The SAM -&gt; TBD availability-schedule seam: value comparison, deterministic naming, and source
    /// precedence.</b>
    /// <para>
    /// A TBD schedule is building-level and shared by apertures across every zone, so the decision that
    /// governs whether a repeated export creates a duplicate is <b>value equality</b>, never the name. These
    /// tests exercise that decision, the collision-safe naming that follows it, and the
    /// <c>Schedule</c>-before-legacy-<c>Profile</c> precedence, all directly.
    /// </para>
    /// <para>
    /// <b>Why these tests need no installed TAS.</b> The pieces under test -
    /// <c>TasQuery.ScheduleValues</c>, <c>TasQuery.ScheduleValuesEqual</c>, <c>TasQuery.ScheduleSignature</c>,
    /// <c>TasQuery.ScheduleIndex</c>, <c>TasQuery.ScheduleName</c> and
    /// <c>Query.TryGetOpeningScheduleSource</c> (an extension method) - name no TAS COM type, which is what lets the whole
    /// resolve/reuse/name algorithm be verified without a TBD. What genuinely needs COM is the write itself
    /// (<c>Modify.SetScheduleValues</c>) and its assignment to a <c>TBD.profile</c>: those carry their own
    /// mandatory 24-value read-back and are covered by the manual TAS acceptance run, following the same
    /// precedent as <c>TsdZoneIdentityStampTests</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    public class OpeningScheduleResolutionTests
    {
        private const string PartOName = "PartO_DayOpen_08_23";

        private static bool[] BoolWindow(int from, int to)
        {
            bool[] values = new bool[24];
            for (int hour = 0; hour < 24; hour++)
            {
                values[hour] = from <= to ? (hour >= from && hour < to) : (hour >= from || hour < to);
            }

            return values;
        }

        private static int[] IntWindow(int from, int to)
        {
            return BoolWindow(from, to).Select(x => x ? 1 : 0).ToArray();
        }

        private static DailyAvailabilitySchedule Schedule(string name, int from, int to)
        {
            return new DailyAvailabilitySchedule(name, BoolWindow(from, to));
        }

        private static Profile LegacyProfile(string name, int from, int to)
        {
            return new Profile(name, ProfileGroup.Ventilation, IntWindow(from, to).Select(x => (double)x).ToArray());
        }

        // -------------------------------------------------------------------------------------------------
        // DailyAvailabilitySchedule -> int[24]
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void ScheduleValues_FromAvailabilitySchedule_IsTwentyFourZerosAndOnes()
        {
            int[] values = Schedule(PartOName, 8, 23).ScheduleValues();

            Assert.That(values, Is.Not.Null);
            Assert.That(values.Length, Is.EqualTo(24));
            for (int hour = 0; hour < 24; hour++)
            {
                Assert.That(values[hour], Is.EqualTo(hour >= 8 && hour < 23 ? 1 : 0), $"hour {hour}");
            }
        }

        [Test]
        public void ScheduleValues_FromNullSchedule_IsNull()
        {
            Assert.That(((DailyAvailabilitySchedule)null).ScheduleValues(), Is.Null);
        }

        [Test]
        public void ScheduleHourCount_Is24()
        {
            Assert.That(TasQuery.ScheduleHourCount, Is.EqualTo(24));
        }

        // -------------------------------------------------------------------------------------------------
        // Legacy Profile -> int[24], unchanged conversion
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// The legacy route must keep writing exactly what it wrote before <c>DailyAvailabilitySchedule</c>
        /// existed: <c>GetDailyValues()</c> then <c>System.Convert.ToInt32</c> per hour.
        /// </summary>
        [Test]
        public void ScheduleValues_FromLegacyProfile_MatchesTheOldConversion()
        {
            Profile profile = LegacyProfile("Legacy", 8, 23);

            int[] values = profile.ScheduleValues();
            double[] daily = profile.GetDailyValues();

            Assert.That(daily, Is.Not.Null);
            Assert.That(daily.Length, Is.EqualTo(24));
            Assert.That(values, Is.Not.Null);
            for (int hour = 0; hour < 24; hour++)
            {
                Assert.That(values[hour], Is.EqualTo(System.Convert.ToInt32(daily[hour])), $"hour {hour}");
            }
        }

        /// <summary>
        /// A general-valued legacy profile is carried through the int seam with the identical rounding the
        /// previous code applied - including <c>Convert.ToInt32</c>'s banker's rounding, which is why the
        /// legacy route is not re-expressed as a binary schedule.
        /// </summary>
        [Test]
        public void ScheduleValues_FromNonBinaryLegacyProfile_PreservesConvertToInt32Semantics()
        {
            double[] source = new double[24];
            for (int hour = 0; hour < 24; hour++)
            {
                source[hour] = hour % 4 == 0 ? 0.5 : (hour % 4 == 1 ? 1.5 : (hour % 4 == 2 ? 2.0 : 0.0));
            }

            int[] values = new Profile("NonBinary", ProfileGroup.Ventilation, source).ScheduleValues();

            Assert.That(values, Is.Not.Null);
            for (int hour = 0; hour < 24; hour++)
            {
                Assert.That(values[hour], Is.EqualTo(System.Convert.ToInt32(source[hour])), $"hour {hour}");
            }
        }

        /// <summary>
        /// A profile carrying no values is the one case <c>GetDailyValues()</c> returns null for. It must be
        /// reported as "no usable source", not skipped after a schedule has already been created.
        /// </summary>
        [Test]
        public void ScheduleValues_FromValuelessProfile_IsNull()
        {
            Assert.That(new Profile("Empty", ProfileGroup.Ventilation.Text()).ScheduleValues(), Is.Null);
        }

        // -------------------------------------------------------------------------------------------------
        // TBD COM boundary: hour index and value encoding
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// A stand-in for a <c>TBD.schedule</c>'s value store, addressed exactly as TAS addresses it: slots
        /// 1..24, where slot n is the hour ENDING at n:00. Slot 0 exists only so the array can be indexed
        /// without an offset of its own - production must never write it, and these tests assert that.
        /// <para>
        /// An ON hour is handed back the way TAS hands it back: the <c>VARIANT_BOOL</c> TRUE bit pattern, -1.
        /// </para>
        /// </summary>
        private sealed class FakeTBDSchedule
        {
            private readonly int[] slots = new int[25];

            public void SetValue(int index_TBD, int value)
            {
                slots[index_TBD] = value;
            }

            public int GetValue(int index_TBD)
            {
                //TAS returns TRUE as the VARIANT_BOOL bit pattern, not as the 1 that was written.
                return slots[index_TBD] == 0 ? 0 : -1;
            }

            /// <summary>Whether the hour ending at <paramref name="hourEnding"/>:00 is ON.</summary>
            public bool IsOnForHourEnding(int hourEnding)
            {
                return slots[hourEnding] != 0;
            }

            public bool Slot0Untouched
            {
                get { return slots[0] == 0; }
            }
        }

        /// <summary>What <c>Modify.SetScheduleValues</c> does, minus the COM object.</summary>
        private static FakeTBDSchedule Write(int[] values)
        {
            FakeTBDSchedule schedule = new FakeTBDSchedule();
            for (int hour = 0; hour < values.Length; hour++)
            {
                schedule.SetValue(TasQuery.ScheduleIndexTBD(hour), values[hour]);
            }

            return schedule;
        }

        /// <summary>What <c>Query.HourlyValues</c> does, minus the COM object.</summary>
        private static int[] Read(FakeTBDSchedule schedule)
        {
            int[] result = new int[24];
            for (int hour = 0; hour < 24; hour++)
            {
                result[hour] = TasQuery.ScheduleValueFromTBD(schedule.GetValue(TasQuery.ScheduleIndexTBD(hour)));
            }

            return result;
        }

        // --- value encoding ---

        [Test]
        public void ScheduleValueFromTBD_ZeroAndOne_AreUnchanged()
        {
            Assert.That(TasQuery.ScheduleValueFromTBD(0), Is.EqualTo(0));
            Assert.That(TasQuery.ScheduleValueFromTBD(1), Is.EqualTo(1));
        }

        /// <summary>
        /// TAS hands an ON hour back as the <c>VARIANT_BOOL</c> TRUE bit pattern, -1, not as the 1 that was
        /// written. Comparing the raw value against the SAM value refused every schedule the export created.
        /// </summary>
        [Test]
        public void ScheduleValueFromTBD_VariantBoolTrue_IsOne()
        {
            Assert.That(TasQuery.ScheduleValueFromTBD(-1), Is.EqualTo(1));
        }

        /// <summary>
        /// Only -1 is folded onto 1. Anything else passes through so it still fails the caller's comparison -
        /// an unexpected read-back must refuse, not be quietly taken for "true".
        /// </summary>
        [Test]
        public void ScheduleValueFromTBD_UnexpectedValue_IsPassedThroughNotTreatedAsTrue()
        {
            foreach (int value in new[] { 2, 3, 7, -2, -255, int.MaxValue, int.MinValue })
            {
                Assert.That(TasQuery.ScheduleValueFromTBD(value), Is.EqualTo(value), $"value {value}");
            }
        }

        // --- hour index ---

        /// <summary>
        /// TBD's 24-slot hourly indexed properties are 1-based hour-ending: SAM hour h (h:00 to (h+1):00) is
        /// TBD slot h + 1, the hour ending at (h+1):00. Writing 0-based put every hour one cell early.
        /// </summary>
        [Test]
        public void ScheduleIndexTBD_MapsSamHourToTheOneBasedHourEndingSlot()
        {
            Assert.That(TasQuery.ScheduleIndexTBD(0), Is.EqualTo(1), "00:00-01:00 is the hour ending 01:00");
            Assert.That(TasQuery.ScheduleIndexTBD(8), Is.EqualTo(9), "08:00-09:00 is the hour ending 09:00");
            Assert.That(TasQuery.ScheduleIndexTBD(22), Is.EqualTo(23), "22:00-23:00 is the hour ending 23:00");
            Assert.That(TasQuery.ScheduleIndexTBD(23), Is.EqualTo(24), "23:00-24:00 is the hour ending 24:00");
        }

        [Test]
        public void ScheduleHourFromTBD_IsTheInverseOfScheduleIndexTBD()
        {
            for (int hour = 0; hour < 24; hour++)
            {
                Assert.That(TasQuery.ScheduleHourFromTBD(TasQuery.ScheduleIndexTBD(hour)), Is.EqualTo(hour), $"hour {hour}");
            }
        }

        /// <summary>
        /// The whole SAM day occupies TBD slots 1..24. Slot 0 is not a TAS hour and must never be written -
        /// writing it is what left 23:00-24:00 (slot 24) unwritten at the other end of the day.
        /// </summary>
        [Test]
        public void WrittenSlots_AreOneToTwentyFour_AndSlotZeroIsNeverTouched()
        {
            FakeTBDSchedule schedule = Write(Enumerable.Repeat(1, 24).ToArray());

            Assert.That(schedule.Slot0Untouched, Is.True, "slot 0 is not a TAS hour");
            for (int hourEnding = 1; hourEnding <= 24; hourEnding++)
            {
                Assert.That(schedule.IsOnForHourEnding(hourEnding), Is.True, $"hour ending {hourEnding}:00");
            }
        }

        // --- the two conventions together ---

        /// <summary>
        /// <b>The regression this boundary exists for.</b> PartO_DayOpen_08_23 must land in TAS as
        /// OFF 00:00-08:00, ON 08:00-23:00, OFF 23:00-24:00 - 15 ON cells, the first being the hour ending
        /// 09:00 and the last the hour ending 23:00. Before the fix the block sat one cell early, at the
        /// hours ending 08:00 to 22:00, which is what a licensed TAS displayed as 07:00-08:00 to 21:00-22:00.
        /// </summary>
        [Test]
        public void PartODayOpen_LandsOnTheIntendedTasHours()
        {
            FakeTBDSchedule schedule = Write(Schedule(PartOName, 8, 23).ScheduleValues());

            for (int hourEnding = 1; hourEnding <= 8; hourEnding++)
            {
                Assert.That(schedule.IsOnForHourEnding(hourEnding), Is.False, $"00:00-08:00 must be OFF; hour ending {hourEnding}:00");
            }

            for (int hourEnding = 9; hourEnding <= 23; hourEnding++)
            {
                Assert.That(schedule.IsOnForHourEnding(hourEnding), Is.True, $"08:00-23:00 must be ON; hour ending {hourEnding}:00");
            }

            Assert.That(schedule.IsOnForHourEnding(24), Is.False, "23:00-24:00 must be OFF");

            int count_On = Enumerable.Range(1, 24).Count(schedule.IsOnForHourEnding);
            Assert.That(count_On, Is.EqualTo(15), "an 08-23 window is 15 whole hours");
        }

        /// <summary>
        /// Read-back inverts both conventions, so SAM sees its canonical 24-value 0/1 representation again:
        /// same values, same signature, domain model still binary.
        /// </summary>
        [Test]
        public void ReadBack_RestoresTheCanonicalSamSchedule()
        {
            DailyAvailabilitySchedule source = Schedule(PartOName, 8, 23);
            int[] written = source.ScheduleValues();

            int[] readBack = Read(Write(written));

            Assert.That(readBack, Is.EqualTo(written));
            Assert.That(readBack.All(x => x == 0 || x == 1), Is.True);
            Assert.That(TasQuery.ScheduleValuesEqual(readBack, written), Is.True);
            Assert.That(TasQuery.ScheduleSignature(readBack), Is.EqualTo(source.Signature));
            Assert.That(TasQuery.ScheduleSignature(readBack), Is.EqualTo("00FFFE"));
        }

        /// <summary>
        /// Neither conversion may rescue a schedule that genuinely did not persist. An all-zero read-back is
        /// the exact corruption the write verification was added for, and it still refuses; so does a
        /// different window, and so does an unexpected raw value.
        /// </summary>
        [Test]
        public void ReadBack_StillRefusesAGenuineMismatch()
        {
            int[] written = IntWindow(8, 23);

            Assert.That(TasQuery.ScheduleValuesEqual(Read(Write(new int[24])), written), Is.False, "the values were lost");
            Assert.That(TasQuery.ScheduleValuesEqual(Read(Write(IntWindow(9, 21))), written), Is.False, "a different window");

            int[] unexpected = Read(Write(written));
            unexpected[8] = TasQuery.ScheduleValueFromTBD(7);
            Assert.That(TasQuery.ScheduleValuesEqual(unexpected, written), Is.False, "an unexpected raw value is not taken for true");
        }

        // -------------------------------------------------------------------------------------------------
        // Value equality
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void ScheduleValuesEqual_IdenticalValues_AreEqual()
        {
            Assert.That(TasQuery.ScheduleValuesEqual(IntWindow(8, 23), IntWindow(8, 23)), Is.True);
        }

        [Test]
        public void ScheduleValuesEqual_AnySingleDifferingHour_IsNotEqual()
        {
            for (int hour = 0; hour < 24; hour++)
            {
                int[] other = IntWindow(8, 23);
                other[hour] = other[hour] == 1 ? 0 : 1;

                Assert.That(TasQuery.ScheduleValuesEqual(IntWindow(8, 23), other), Is.False, $"hour {hour}");
            }
        }

        [Test]
        public void ScheduleValuesEqual_WrongLengthOrNull_IsNotEqual()
        {
            Assert.That(TasQuery.ScheduleValuesEqual(IntWindow(8, 23), new int[23]), Is.False);
            Assert.That(TasQuery.ScheduleValuesEqual(IntWindow(8, 23), null), Is.False);
            Assert.That(TasQuery.ScheduleValuesEqual(null, IntWindow(8, 23)), Is.False);
        }

        // -------------------------------------------------------------------------------------------------
        // Signature
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void ScheduleSignature_BinaryValues_IsTheDomainObjectsSixHexMask()
        {
            Assert.That(TasQuery.ScheduleSignature(IntWindow(8, 23)), Is.EqualTo("00FFFE"));
            Assert.That(TasQuery.ScheduleSignature(IntWindow(8, 23)), Is.EqualTo(Schedule(PartOName, 8, 23).Signature));
            Assert.That(TasQuery.ScheduleSignature(new int[24]), Is.EqualTo("000000"));
        }

        /// <summary>
        /// A non-binary schedule can only reach TAS through the legacy profile route. Its signature is
        /// deliberately a different shape so it can never be confused with, or collide with, a binary one -
        /// and it must be arithmetic, never <c>GetHashCode</c>, because it can end up in a persisted TBD name.
        /// </summary>
        [Test]
        public void ScheduleSignature_NonBinaryValues_IsADistinctStableFingerprint()
        {
            int[] values = IntWindow(8, 23);
            values[9] = 3;

            string signature = TasQuery.ScheduleSignature(values);

            Assert.That(signature, Does.StartWith("X"));
            Assert.That(signature.Length, Is.EqualTo(9));
            Assert.That(signature, Is.Not.EqualTo(TasQuery.ScheduleSignature(IntWindow(8, 23))));
            Assert.That(TasQuery.ScheduleSignature(values), Is.EqualTo(signature));
        }

        [Test]
        public void ScheduleSignature_WrongLength_IsNull()
        {
            Assert.That(TasQuery.ScheduleSignature(new int[23]), Is.Null);
            Assert.That(TasQuery.ScheduleSignature(null), Is.Null);
        }

        // -------------------------------------------------------------------------------------------------
        // Reuse by value
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void ScheduleIndex_IdenticalValuesExist_ReturnsThatIndex()
        {
            List<int[]> existing = new List<int[]> { new int[24], IntWindow(9, 21), IntWindow(8, 23) };

            Assert.That(TasQuery.ScheduleIndex(existing, IntWindow(8, 23)), Is.EqualTo(2));
        }

        [Test]
        public void ScheduleIndex_NoIdenticalValues_ReturnsMinusOne()
        {
            List<int[]> existing = new List<int[]> { new int[24], IntWindow(9, 21) };

            Assert.That(TasQuery.ScheduleIndex(existing, IntWindow(8, 23)), Is.EqualTo(-1));
        }

        [Test]
        public void ScheduleIndex_FirstMatchWins_SoRepeatedExportIsIdempotent()
        {
            List<int[]> existing = new List<int[]> { IntWindow(8, 23), IntWindow(8, 23) };

            Assert.That(TasQuery.ScheduleIndex(existing, IntWindow(8, 23)), Is.EqualTo(0));
            Assert.That(TasQuery.ScheduleIndex(existing, IntWindow(8, 23)), Is.EqualTo(0));
        }

        /// <summary>
        /// The §7 example: three openings with quite different reasons for the same availability window
        /// resolve to one schedule.
        /// </summary>
        [Test]
        public void ScheduleIndex_DifferentlyNamedSourcesWithTheSameValues_ResolveToOneSchedule()
        {
            int[] existingValues = Schedule("Whatever this building already called it", 8, 23).ScheduleValues();
            List<int[]> existing = new List<int[]> { existingValues };

            Assert.That(TasQuery.ScheduleIndex(existing, Schedule(PartOName, 8, 23).ScheduleValues()), Is.EqualTo(0));
            Assert.That(TasQuery.ScheduleIndex(existing, Schedule("Internal_Door_Availability", 8, 23).ScheduleValues()), Is.EqualTo(0));
            Assert.That(TasQuery.ScheduleIndex(existing, Schedule("Security_Restricted", 8, 23).ScheduleValues()), Is.EqualTo(0));
        }

        // -------------------------------------------------------------------------------------------------
        // Naming and collisions
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void ScheduleName_FreeRequestedName_IsUsedAsIs()
        {
            string name = TasQuery.ScheduleName(new[] { "Something else" }, PartOName, IntWindow(8, 23), out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(name, Is.EqualTo(PartOName));
        }

        [Test]
        public void ScheduleName_NoRequestedName_IsGeneratedFromTheSignature()
        {
            string name = TasQuery.ScheduleName(new string[0], null, IntWindow(8, 23), out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(name, Is.EqualTo(TasQuery.ScheduleNamePrefix + "00FFFE"));
        }

        /// <summary>
        /// A taken name is only ever reached when the value search has already failed, so the existing
        /// schedule necessarily holds different values. The suffix is the requested values' signature - a
        /// deterministic name, not a <c>(1)</c>/<c>(2)</c> counter, so the same values resolve identically on
        /// every repeated export.
        /// </summary>
        [Test]
        public void ScheduleName_TakenByDifferentValues_GetsADeterministicSignatureSuffix()
        {
            string name = TasQuery.ScheduleName(new[] { PartOName }, PartOName, IntWindow(8, 23), out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(name, Is.EqualTo(PartOName + "_00FFFE"));
            Assert.That(TasQuery.ScheduleName(new[] { PartOName }, PartOName, IntWindow(8, 23), out string _), Is.EqualTo(name));
        }

        [Test]
        public void ScheduleName_BothPreferredAndQualifiedTaken_Refuses()
        {
            string name = TasQuery.ScheduleName(new[] { PartOName, PartOName + "_00FFFE" }, PartOName, IntWindow(8, 23), out string refusal);

            Assert.That(name, Is.Null);
            Assert.That(refusal, Is.Not.Null);
            Assert.That(refusal, Does.Contain(PartOName));
            Assert.That(refusal, Does.Contain("_00FFFE"));
        }

        [Test]
        public void ScheduleName_InvalidValueCount_RefusesWithoutNaming()
        {
            string name = TasQuery.ScheduleName(new string[0], PartOName, new int[23], out string refusal);

            Assert.That(name, Is.Null);
            Assert.That(refusal, Is.Not.Null);
            Assert.That(refusal, Does.Contain("24"));
        }

        // -------------------------------------------------------------------------------------------------
        // Source precedence
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void OpeningScheduleSource_PartOUnrestricted_HasNoSource()
        {
            bool result = new PartOOpeningProperties(1.2, 1.0, 30.0).TryGetOpeningScheduleSource(out string name, out int[] values, out string refusal);

            Assert.That(result, Is.False);
            Assert.That(name, Is.Null);
            Assert.That(values, Is.Null);
            Assert.That(refusal, Is.Null);
        }

        [Test]
        public void OpeningScheduleSource_PartOAlwaysClosed_HasNoSource()
        {
            bool result = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.AlwaysClosed).TryGetOpeningScheduleSource(out string _, out int[] values, out string refusal);

            Assert.That(result, Is.False);
            Assert.That(values, Is.Null);
            Assert.That(refusal, Is.Null);
        }

        [Test]
        public void OpeningScheduleSource_PartONightClosed_IsTheAvailabilityWindow()
        {
            bool result = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed).TryGetOpeningScheduleSource(out string name, out int[] values, out string refusal);

            Assert.That(result, Is.True);
            Assert.That(refusal, Is.Null);
            Assert.That(name, Is.EqualTo(PartOName));
            Assert.That(TasQuery.ScheduleValuesEqual(values, IntWindow(8, 23)), Is.True);
        }

        /// <summary>The documented precedence: the explicit Schedule wins whenever present.</summary>
        [Test]
        public void OpeningScheduleSource_ScheduleWinsOverLegacyProfile()
        {
            ProfileOpeningProperties profileOpeningProperties = new ProfileOpeningProperties(0.6, LegacyProfile("Legacy", 9, 21), Schedule(PartOName, 8, 23));

            bool result = profileOpeningProperties.TryGetOpeningScheduleSource(out string name, out int[] values, out string refusal);

            Assert.That(result, Is.True);
            Assert.That(refusal, Is.Null);
            Assert.That(name, Is.EqualTo(PartOName));
            Assert.That(TasQuery.ScheduleValuesEqual(values, IntWindow(8, 23)), Is.True);
            Assert.That(TasQuery.ScheduleValuesEqual(values, IntWindow(9, 21)), Is.False);
        }

        /// <summary>Compatibility: with no Schedule, the legacy Profile governs exactly as it used to.</summary>
        [Test]
        public void OpeningScheduleSource_NoSchedule_FallsBackToLegacyProfile()
        {
            ProfileOpeningProperties profileOpeningProperties = new ProfileOpeningProperties(0.6, LegacyProfile("Legacy_Availability", 9, 21));

            bool result = profileOpeningProperties.TryGetOpeningScheduleSource(out string name, out int[] values, out string refusal);

            Assert.That(result, Is.True);
            Assert.That(refusal, Is.Null);
            Assert.That(name, Is.EqualTo("Legacy_Availability"));
            Assert.That(TasQuery.ScheduleValuesEqual(values, IntWindow(9, 21)), Is.True);
        }

        [Test]
        public void OpeningScheduleSource_NeitherCarrier_HasNoSourceAndNoRefusal()
        {
            bool result = new ProfileOpeningProperties(0.6).TryGetOpeningScheduleSource(out string name, out int[] values, out string refusal);

            Assert.That(result, Is.False);
            Assert.That(name, Is.Null);
            Assert.That(values, Is.Null);
            Assert.That(refusal, Is.Null);
        }

        /// <summary>
        /// The failure that previously left a named 24-zero schedule in a real TBD: a stated schedule source
        /// that cannot supply 24 values. It must be REPORTED, and it must be reported before anything is
        /// created - <c>TBD.Building</c> has no <c>RemoveSchedule</c>, so a schedule created in error could
        /// never be withdrawn.
        /// </summary>
        [Test]
        public void OpeningScheduleSource_StatedButUnusableProfile_Refuses()
        {
            ProfileOpeningProperties profileOpeningProperties = new ProfileOpeningProperties(0.6, new Profile("Empty", ProfileGroup.Ventilation.Text()));

            bool result = profileOpeningProperties.TryGetOpeningScheduleSource(out string name, out int[] values, out string refusal);

            Assert.That(result, Is.False);
            Assert.That(values, Is.Null);
            Assert.That(name, Is.Null);
            Assert.That(refusal, Is.Not.Null);
            Assert.That(refusal, Does.Contain("Empty"));
        }

        [Test]
        public void OpeningScheduleSource_PlainOpeningProperties_HasNoSource()
        {
            bool result = new OpeningProperties(0.6).TryGetOpeningScheduleSource(out string _, out int[] values, out string refusal);

            Assert.That(result, Is.False);
            Assert.That(values, Is.Null);
            Assert.That(refusal, Is.Null);
        }

        [Test]
        public void OpeningScheduleSource_Null_HasNoSource()
        {
            bool result = ((ISingleOpeningProperties)null).TryGetOpeningScheduleSource(out string _, out int[] values, out string refusal);

            Assert.That(result, Is.False);
            Assert.That(values, Is.Null);
            Assert.That(refusal, Is.Null);
        }

        // -------------------------------------------------------------------------------------------------
        // End-to-end resolution, without COM
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// The whole decision a repeated export makes: first pass finds nothing to reuse and names a new
        /// schedule; second pass, with that schedule now in the building, reuses it and names nothing. This is
        /// what stops <c>PartO_DayOpen_08_23 (1)</c>, <c>(2)</c>, ... from accumulating.
        /// </summary>
        [Test]
        public void RepeatedExport_ResolvesToOneSchedule()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed);
            partOOpeningProperties.TryGetOpeningScheduleSource(out string name, out int[] values, out string _);

            //Pass 1: an empty building.
            List<string> names = new List<string>();
            List<int[]> existing = new List<int[]>();

            Assert.That(TasQuery.ScheduleIndex(existing, values), Is.EqualTo(-1));
            string created = TasQuery.ScheduleName(names, name, values, out string refusal_1);
            Assert.That(refusal_1, Is.Null);
            Assert.That(created, Is.EqualTo(PartOName));

            names.Add(created);
            existing.Add(values);

            //Pass 2: the same values are requested again - reused, and no second name derived.
            Assert.That(TasQuery.ScheduleIndex(existing, values), Is.EqualTo(0));
            Assert.That(existing.Count, Is.EqualTo(1));
            Assert.That(names.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// A different window is a different schedule, so it must NOT reuse the existing one - and it gets
        /// its own name without disturbing the first.
        /// </summary>
        [Test]
        public void DifferentValues_CreateASecondSchedule()
        {
            int[] first = IntWindow(8, 23);
            int[] second = IntWindow(9, 21);

            List<string> names = new List<string> { PartOName };
            List<int[]> existing = new List<int[]> { first };

            Assert.That(TasQuery.ScheduleIndex(existing, second), Is.EqualTo(-1));

            string created = TasQuery.ScheduleName(names, "PartO_DayOpen_09_21", second, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(created, Is.EqualTo("PartO_DayOpen_09_21"));
            Assert.That(created, Is.Not.EqualTo(PartOName));
        }
    }
}
