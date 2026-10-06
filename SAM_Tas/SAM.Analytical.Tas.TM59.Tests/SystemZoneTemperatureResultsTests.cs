// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Step 8, the results gate. A run is only successful when every room the conversion bound returns
    /// a complete, finite <c>ZoneTemperature</c> series off the exact native zone and zone load its own
    /// binding names.
    /// <para>
    /// <b>Why this is the gate and TAS's own answer is not.</b> Measured on licensed TAS,
    /// <c>ISystem.Simulate</c> answers <c>"Done"</c> on a successful run - and a document with zero
    /// plant rooms, zero systems and zero zones once answered <c>true</c> through the production entry
    /// point having produced nothing at all. A saved file, a returned string and a finished call are
    /// all things a failed run also produces. A complete series per room is not.
    /// </para>
    /// <para>
    /// The series here are supplied directly rather than read from TAS, so every failure mode - a
    /// missing room, a short series, a hole in the middle, a NaN, a series off the wrong zone, two rooms
    /// sharing one zone, a series for a room nobody asked about - can be exercised without a licence.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SystemZoneTemperatureResultsTests
    {
        private const int StartHour = 0;
        private const int EndHour = 23;

        private static Guid G(int seed)
        {
            return new Guid(seed, 0, 0, new byte[8]);
        }

        private static SystemVentilationBinding Binding(int room)
        {
            return new SystemVentilationBinding(
                G(room),
                G(room + 100),
                G(1000),
                string.Format("{{ZONE-{0}}}", room),
                string.Format("{{LOAD-{0}}}", room),
                "{SYS-1}",
                20.0,
                null);
        }

        private static IndexedDoubles Series(int startHour, int count, double value = 21.0)
        {
            IndexedDoubles result = new IndexedDoubles();

            for (int i = 0; i < count; i++)
            {
                result[startHour + i] = value;
            }

            return result;
        }

        private static SystemZoneTemperatureResult Result(
            SystemVentilationBinding systemVentilationBinding,
            IndexedDoubles indexedDoubles,
            string reference_SystemZone = null,
            string reference_ZoneLoad = null,
            string diagnostic = null,
            int startHour = StartHour,
            int endHour = EndHour)
        {
            return new SystemZoneTemperatureResult(
                systemVentilationBinding.Guid_Space,
                systemVentilationBinding.Guid_SystemSpace,
                reference_SystemZone ?? systemVentilationBinding.Reference_SystemZone,
                reference_ZoneLoad ?? systemVentilationBinding.Reference_ZoneLoad,
                startHour,
                endHour,
                indexedDoubles,
                diagnostic);
        }

        private static SystemZoneTemperatureResults Complete(int count_Rooms, out List<SystemVentilationBinding> bindings)
        {
            bindings = new List<SystemVentilationBinding>();

            SystemZoneTemperatureResults result = new SystemZoneTemperatureResults(StartHour, EndHour);

            for (int i = 1; i <= count_Rooms; i++)
            {
                SystemVentilationBinding systemVentilationBinding = Binding(i);

                bindings.Add(systemVentilationBinding);
                result.Add(Result(systemVentilationBinding, Series(StartHour, EndHour - StartHour + 1)));
            }

            return result;
        }

        // ------------------------------------------------------------------------------- the good case

        [Test]
        public void ACompleteResultSetValidates()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = Complete(3, out List<SystemVentilationBinding> bindings);

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(bindings), Is.True, string.Join(" | ", systemZoneTemperatureResults.Refusals));
                Assert.That(systemZoneTemperatureResults.IsComplete, Is.True);
                Assert.That(systemZoneTemperatureResults.ExpectedCount, Is.EqualTo(24));
                Assert.That(systemZoneTemperatureResults.Results.Count, Is.EqualTo(3));
            });
        }

        [Test]
        public void NothingIsCompleteUntilValidateHasRun()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = Complete(1, out List<SystemVentilationBinding> bindings);

            Assert.That(systemZoneTemperatureResults.IsComplete, Is.False);
        }

        [Test]
        public void ThePeriodIsWhateverWasRequested_Never8760ByAssumption()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(0, 8759);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, Series(0, 8760), startHour: 0, endHour: 8759));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.ExpectedCount, Is.EqualTo(8760));
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.True, string.Join(" | ", systemZoneTemperatureResults.Refusals));
            });
        }

        [Test]
        public void A24HourRequestAnsweredWith8760ValuesIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, Series(StartHour, 8760)));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("which is 24 hour(s)")), Is.True, string.Join(" | ", systemZoneTemperatureResults.Refusals));
            });
        }

        // --------------------------------------------------------------------------- the failure modes

        [Test]
        public void ARoomWithNoSeriesAtAllIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = Complete(2, out List<SystemVentilationBinding> bindings);

            bindings.Add(Binding(9));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(bindings), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("no zone temperature series came back for it")), Is.True);
            });
        }

        [Test]
        public void AShortSeriesIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, Series(StartHour, 12)));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("12 zone temperature value(s) came back")), Is.True);
            });
        }

        [Test]
        public void AHoleInTheMiddleOfTheSeriesIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            //The right NUMBER of values, with hour 7 missing and hour 24 present instead - which a count
            //check alone would wave through.
            IndexedDoubles indexedDoubles = Series(StartHour, 24);
            indexedDoubles.Remove(7);
            indexedDoubles[24] = 21.0;

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, indexedDoubles));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("no zone temperature at hour 7")), Is.True, string.Join(" | ", systemZoneTemperatureResults.Refusals));
            });
        }

        [Test]
        public void ANonFiniteValueIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            IndexedDoubles indexedDoubles = Series(StartHour, 24);
            indexedDoubles[11] = double.NaN;

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, indexedDoubles));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("the zone temperature at hour 11 is NaN")), Is.True, string.Join(" | ", systemZoneTemperatureResults.Refusals));
            });
        }

        [Test]
        public void ASeriesReadOffTheWrongNativeZoneIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, Series(StartHour, 24), reference_SystemZone: "{ZONE-SOMEBODY-ELSE}"));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("its series was read off native zone")), Is.True);
            });
        }

        [Test]
        public void ASeriesFromTheWrongZoneLoadIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, Series(StartHour, 24), reference_ZoneLoad: "{LOAD-SOMEBODY-ELSE}"));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("its series came from zone load")), Is.True);
            });
        }

        [Test]
        public void TwoRoomsTakingTheirSeriesFromOneNativeZoneIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding binding_1 = new SystemVentilationBinding(G(1), G(101), G(1000), "{ZONE-SHARED}", "{LOAD-1}", "{SYS-1}", 20.0, null);
            SystemVentilationBinding binding_2 = new SystemVentilationBinding(G(2), G(102), G(1000), "{ZONE-SHARED}", "{LOAD-2}", "{SYS-1}", 20.0, null);

            systemZoneTemperatureResults.Add(Result(binding_1, Series(StartHour, 24)));
            systemZoneTemperatureResults.Add(Result(binding_2, Series(StartHour, 24)));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { binding_1, binding_2 }), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("both took their series from native zone")), Is.True);
            });
        }

        [Test]
        public void ASeriesForARoomNobodyAskedAboutIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = Complete(2, out List<SystemVentilationBinding> bindings);

            systemZoneTemperatureResults.Add(Result(Binding(9), Series(StartHour, 24)));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(bindings), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("which the converted graph does not contain")), Is.True);
            });
        }

        [Test]
        public void TheReasonTasGaveIsKept_NotSwallowed()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, null, diagnostic: "GetResultsData threw COMException: Hour out of range"));

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("Hour out of range")), Is.True, "a swallowed COM message is a lost diagnosis");
            });
        }

        [Test]
        public void ARoomWhoseBindingCannotResolveResultsIsRefused()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            //No zone load: the binding names a zone but nothing the TSD can be asked for.
            SystemVentilationBinding systemVentilationBinding = new SystemVentilationBinding(G(1), G(101), G(1000), "{ZONE-1}", null, "{SYS-1}", 20.0, null);

            systemZoneTemperatureResults.Add(Result(systemVentilationBinding, null, diagnostic: "its binding does not name both a native zone and a zone load, so no series can be resolved for it."));

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationBinding.CanResolveResults, Is.False);
                Assert.That(systemZoneTemperatureResults.Validate(new List<SystemVentilationBinding> { systemVentilationBinding }), Is.False);
            });
        }

        // -------------------------------------------------------------------- identity, not names

        [Test]
        public void MappingIsByRoomGuid_SoEnumerationOrderCannotChangeIt()
        {
            SystemZoneTemperatureResults first = Complete(4, out List<SystemVentilationBinding> bindings);

            List<SystemVentilationBinding> reversed = new List<SystemVentilationBinding>(bindings);
            reversed.Reverse();

            SystemZoneTemperatureResults second = new SystemZoneTemperatureResults(StartHour, EndHour);

            foreach (SystemVentilationBinding systemVentilationBinding in reversed)
            {
                second.Add(Result(systemVentilationBinding, Series(StartHour, 24)));
            }

            Assert.Multiple(() =>
            {
                Assert.That(first.Validate(bindings), Is.True);
                Assert.That(second.Validate(reversed), Is.True);

                Assert.That(
                    string.Join("|", second.Results.ConvertAll(x => x.ToString())),
                    Is.EqualTo(string.Join("|", first.Results.ConvertAll(x => x.ToString()))),
                    "the answer is ordered by room identity, not by the order the series were read");
            });
        }

        [Test]
        public void OneRoomCannotProduceTwoSeries()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(StartHour, EndHour);

            SystemVentilationBinding systemVentilationBinding = Binding(1);

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Add(Result(systemVentilationBinding, Series(StartHour, 24))), Is.True);
                Assert.That(systemZoneTemperatureResults.Add(Result(systemVentilationBinding, Series(StartHour, 24))), Is.False);
                Assert.That(systemZoneTemperatureResults.Refusals.Exists(x => x.Contains("produced two zone temperature series")), Is.True);
            });
        }

        [Test]
        public void OneHourIsReadableWithoutCopyingTheSeries()
        {
            SystemVentilationBinding systemVentilationBinding = Binding(1);

            IndexedDoubles indexedDoubles = Series(StartHour, 24);
            indexedDoubles[5] = 19.5;

            SystemZoneTemperatureResult systemZoneTemperatureResult = Result(systemVentilationBinding, indexedDoubles);

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResult.TryGetValue(5, out double value), Is.True);
                Assert.That(value, Is.EqualTo(19.5));

                Assert.That(systemZoneTemperatureResult.TryGetValue(99, out double value_Absent), Is.False);
                Assert.That(double.IsNaN(value_Absent), Is.True, "an absent hour answers NaN, not a silent zero");
            });
        }

        [Test]
        public void TheSeriesHandedOutIsACopy_SoAPublishedResultCannotBeEdited()
        {
            SystemVentilationBinding systemVentilationBinding = Binding(1);

            SystemZoneTemperatureResult systemZoneTemperatureResult = Result(systemVentilationBinding, Series(StartHour, 24));

            IndexedDoubles taken = systemZoneTemperatureResult.Values;
            taken[3] = -999.0;

            //Values is defensive on every access, which is why walking a period must go through
            //TryGetValue: copying an annual series per hour is what turns a comparison into minutes.
            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResult.TryGetValue(3, out double value), Is.True);
                Assert.That(value, Is.EqualTo(21.0), "editing the copy must not reach the result");
                Assert.That(systemZoneTemperatureResult.IsComplete, Is.True);
            });
        }

        [Test]
        public void TheSeriesIsFoundByRoomGuid()
        {
            SystemZoneTemperatureResults systemZoneTemperatureResults = Complete(3, out List<SystemVentilationBinding> bindings);

            Assert.Multiple(() =>
            {
                Assert.That(systemZoneTemperatureResults.Result(G(2)), Is.Not.Null);
                Assert.That(systemZoneTemperatureResults.Result(G(2)).Reference_ZoneLoad, Is.EqualTo("{LOAD-2}"));
                Assert.That(systemZoneTemperatureResults.Result(G(99)), Is.Null);
            });
        }
    }
}
