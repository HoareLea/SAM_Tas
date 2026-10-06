// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Step 9. The atomic route: one object carrying everything a later stage needs, and <b>nothing</b>
    /// when any required stage failed.
    /// <para>
    /// <b>What is asserted here, and what is not.</b> These tests pin the fail-closed contract and the
    /// path guard, which are decided entirely in managed code and are therefore the part that can be
    /// proved without a TAS licence. That a complete route is produced from a real model is a licensed
    /// acceptance, recorded separately - and it is the fail-closed half that matters most here, because
    /// a route that quietly hands back half an answer is the failure nobody notices.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SystemVentilationRouteTests
    {
        private static Guid G(int seed)
        {
            return new Guid(seed, 0, 0, new byte[8]);
        }

        private static NoIzamThermalSource ThermalSource(string path_TBD = null, string path_TSD = null, bool removed = true, bool zoneReferences = true)
        {
            List<KeyValuePair<Guid, string>> pairs = new List<KeyValuePair<Guid, string>>();

            if (zoneReferences)
            {
                pairs.Add(new KeyValuePair<Guid, string>(G(1), "{ZONE-1}"));
            }

            return new NoIzamThermalSource(
                path_TBD,
                path_TSD,
                removed,
                removed,
                null,
                pairs,
                null,
                null);
        }

        private static SystemVentilationRoute Refused(params string[] refusals)
        {
            return new SystemVentilationRoute(
                ThermalSource(),
                @"C:\TasOut\route.tpd",
                null,
                new List<SystemVentilationBinding> { new SystemVentilationBinding(G(1), G(2), G(3), "{ZONE-1}", "{LOAD-1}", "{SYS-1}", 20.0, null) },
                new List<SystemVentilationConnectionBinding>(),
                new SystemZoneTemperatureResults(0, 23),
                refusals,
                null);
        }

        // -------------------------------------------------------------------------------- fail closed

        [Test]
        public void ARefusedRouteExposesNoPayloadAtAll()
        {
            SystemVentilationRoute systemVentilationRoute = Refused("the conversion did not reconcile");

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationRoute.IsComplete, Is.False);

                //Not a binding, not a result, and not the path of the document that failed. A caller
                //that reads the payload without reading the refusals cannot get half a route.
                Assert.That(systemVentilationRoute.Bindings, Is.Empty);
                Assert.That(systemVentilationRoute.ConnectionBindings, Is.Empty);
                Assert.That(systemVentilationRoute.SystemZoneTemperatureResults, Is.Null);
                Assert.That(systemVentilationRoute.Path_TPD, Is.Null);

                Assert.That(systemVentilationRoute.Refusals, Is.Not.Empty);
            });
        }

        [Test]
        public void ARouteThatDidNotCompleteAndSaidNothingSaysSoItself()
        {
            //No refusals, no evidence, no results: the stages disagreed with themselves. That is a
            //defect in the route, and it must not read as success.
            SystemVentilationRoute systemVentilationRoute = new SystemVentilationRoute(
                ThermalSource(),
                @"C:\TasOut\route.tpd",
                null,
                null,
                null,
                null,
                null,
                null);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationRoute.IsComplete, Is.False);
                Assert.That(systemVentilationRoute.Refusals.Count, Is.EqualTo(1));
                Assert.That(systemVentilationRoute.Refusals[0], Does.Contain("This is itself a defect"));
            });
        }

        [Test]
        public void TheThermalSourceAndTheDiagnosticSurviveARefusal()
        {
            SystemVentilationRoute systemVentilationRoute = Refused("something went wrong");

            //The payload is withheld; the diagnosis is not. Withholding the diagnosis would leave a
            //failed run with nothing to explain it.
            Assert.That(systemVentilationRoute.NoIzamThermalSource, Is.Not.Null);
        }

        // ---------------------------------------------------------------------------- the path guard

        [Test]
        public void WritingTheTpdOverTheThermalSourceIsRefusedBeforeAnythingRuns()
        {
            string path_TBD = Path.Combine(Path.GetTempPath(), "sam_pr2_guard.tbd");
            string path_TSD = Path.Combine(Path.GetTempPath(), "sam_pr2_guard.tsd");

            File.WriteAllText(path_TBD, "not really a TBD");
            File.WriteAllText(path_TSD, "not really a TSD");

            try
            {
                foreach (string path_TPD in new[] { path_TBD, path_TSD })
                {
                    SystemVentilationRoute systemVentilationRoute = TPD.Create.SystemVentilationRoute(
                        ThermalSource(path_TBD, path_TSD),
                        null,
                        path_TPD,
                        0,
                        23);

                    Assert.Multiple(() =>
                    {
                        Assert.That(systemVentilationRoute.IsComplete, Is.False);
                        Assert.That(File.Exists(path_TBD), Is.True, "the guard must run before anything deletes the output path");
                        Assert.That(File.Exists(path_TSD), Is.True);
                    });
                }
            }
            finally
            {
                File.Delete(path_TBD);
                File.Delete(path_TSD);
            }
        }

        [Test]
        public void AnIncompleteThermalSourceRefusesTheRoute()
        {
            SystemVentilationRoute systemVentilationRoute = TPD.Create.SystemVentilationRoute(
                ThermalSource(@"C:\TasOut\nowhere.tbd", @"C:\TasOut\nowhere.tsd"),
                null,
                @"C:\TasOut\route.tpd",
                0,
                23);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationRoute.IsComplete, Is.False);
                Assert.That(systemVentilationRoute.Refusals.Exists(x => x.Contains("thermal source is not complete")), Is.True, string.Join(" | ", systemVentilationRoute.Refusals));
            });
        }

        [Test]
        public void NoThermalSourceAndNoGraphAreBothRefusedByName()
        {
            SystemVentilationRoute systemVentilationRoute = TPD.Create.SystemVentilationRoute(null, null, @"C:\TasOut\route.tpd", 0, 23);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationRoute.IsComplete, Is.False);
                Assert.That(systemVentilationRoute.Refusals.Exists(x => x.Contains("No thermal source was supplied")), Is.True);
                Assert.That(systemVentilationRoute.Refusals.Exists(x => x.Contains("No materialised ventilation graph was supplied")), Is.True);
            });
        }

        [Test]
        public void APeriodThatEndsBeforeItStartsIsRefused()
        {
            SystemVentilationRoute systemVentilationRoute = TPD.Create.SystemVentilationRoute(null, null, @"C:\TasOut\route.tpd", 23, 0);

            Assert.That(systemVentilationRoute.Refusals.Exists(x => x.Contains("ends before it starts")), Is.True);
        }

        // ------------------------------------------------------------------- the thermal source record

        [Test]
        public void AThermalSourceWithOnlyOneCleanupAppliedIsNotComplete()
        {
            NoIzamThermalSource noIzamThermalSource = new NoIzamThermalSource(
                @"C:\TasOut\a.tbd",
                @"C:\TasOut\a.tsd",
                true,
                false,
                null,
                new[] { new KeyValuePair<Guid, string>(G(1), "{ZONE-1}") },
                null,
                null);

            Assert.Multiple(() =>
            {
                //Both cleanups matter: an IZAM-free building whose ticV gain is still there delivers the
                //same air twice, and no result check downstream could see it.
                Assert.That(noIzamThermalSource.RemovedIZAMs, Is.True);
                Assert.That(noIzamThermalSource.RemovedMechanicalVentilationGains, Is.False);
                Assert.That(noIzamThermalSource.IsComplete, Is.False);
            });
        }

        [Test]
        public void AThermalSourceStatesItsTsdPathRatherThanLeavingItToBeGuessed()
        {
            NoIzamThermalSource noIzamThermalSource = ThermalSource(@"C:\TasOut\model.tbd", @"C:\TasOut\model.tsd");

            Assert.Multiple(() =>
            {
                Assert.That(noIzamThermalSource.Path_TSD, Is.EqualTo(@"C:\TasOut\model.tsd"));
                Assert.That(Analytical.Tas.Query.Path_TSD(@"C:\TasOut\model.tbd"), Is.EqualTo(@"C:\TasOut\model.tsd"));
            });
        }

        [Test]
        public void AThermalSourceResolvesARoomsTasZoneByIdentity()
        {
            NoIzamThermalSource noIzamThermalSource = ThermalSource(@"C:\TasOut\model.tbd", @"C:\TasOut\model.tsd");

            Assert.Multiple(() =>
            {
                Assert.That(noIzamThermalSource.ZoneReference(G(1)), Is.EqualTo("{ZONE-1}"));
                Assert.That(noIzamThermalSource.ZoneReference(G(2)), Is.Null);
                Assert.That(noIzamThermalSource.Count_ZoneReferences, Is.EqualTo(1));
            });
        }
    }
}
