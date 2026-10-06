// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// The coarse progress callback of <c>Create.SystemVentilationRoute</c> (Part O progress UI): optional,
    /// safe when a subscriber throws, silent where the route never starts, and a count only where the route
    /// counted real air systems. COM-free - the licensed route itself is exercised by the Part O acceptance runs.
    /// </summary>
    [TestFixture]
    public class SystemVentilationRouteProgressTests
    {
        [TestCase(1, 3, true)]
        [TestCase(3, 3, true)]
        [TestCase(0, 0, false)]
        [TestCase(2, 0, false)]
        [TestCase(0, 3, false)]
        [TestCase(4, 3, false)]
        public void ACountIsRealOnlyForAnItemInsideTheTotal(int current, int total, bool expected)
        {
            SystemVentilationRouteProgress progress = new SystemVentilationRouteProgress(SystemVentilationRouteStage.SimulatingAirSystems, current, total);

            Assert.That(progress.HasCount, Is.EqualTo(expected));
            Assert.That(progress.Current, Is.EqualTo(current));
            Assert.That(progress.Total, Is.EqualTo(total));
        }

        private static void Report(Action<SystemVentilationRouteProgress> progress)
        {
            MethodInfo methodInfo = typeof(SystemVentilationConversionContext).GetMethod(
                "ReportProgress",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(Action<SystemVentilationRouteProgress>), typeof(SystemVentilationRouteStage), typeof(int), typeof(int) },
                null);

            Assert.That(methodInfo, Is.Not.Null);

            methodInfo.Invoke(null, new object[] { progress, SystemVentilationRouteStage.ConvertingAirSystems, 2, 3 });
        }

        [Test]
        public void AThrowingSubscriberCanNeverStopTheWorkAndNoSubscriberIsHarmless()
        {
            Assert.DoesNotThrow(() => Report(null));
            Assert.DoesNotThrow(() => Report(x => throw new InvalidOperationException("a progress window that fell over")));

            List<SystemVentilationRouteProgress> received = new List<SystemVentilationRouteProgress>();

            Report(received.Add);

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].Stage, Is.EqualTo(SystemVentilationRouteStage.ConvertingAirSystems));
            Assert.That(received[0].HasCount, Is.True);
        }

        [Test]
        public void ASimulationThatCannotStartReportsNothingAndRefusesExactlyAsBefore()
        {
            List<SystemVentilationRouteProgress> received = new List<SystemVentilationRouteProgress>();

            bool result_Without = TPD.Modify.SimulateSystems(null, null, 0, 23, out SimulationEvidence evidence_Without, out SystemZoneTemperatureResults _);
            bool result_With = TPD.Modify.SimulateSystems(null, null, 0, 23, out SimulationEvidence evidence_With, out SystemZoneTemperatureResults _, received.Add);

            Assert.That(result_Without, Is.False);
            Assert.That(result_With, Is.False);
            Assert.That(received, Is.Empty);
            Assert.That(evidence_With.Refusals, Is.EqualTo(evidence_Without.Refusals));
        }

        [Test]
        public void ARouteRefusedByItsGuardReportsNothing()
        {
            string directory = Path.Combine(Path.GetTempPath(), "sam-route-progress-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                List<SystemVentilationRouteProgress> received = new List<SystemVentilationRouteProgress>();

                SystemVentilationRoute route = TPD.Create.SystemVentilationRoute(null, null, Path.Combine(directory, "Guarded.tpd"), 0, 23, SystemVentilationFanHeatGainPolicy.ClearToZero, received.Add);

                Assert.That(route.IsComplete, Is.False);
                Assert.That(route.Refusals, Is.Not.Empty);
                Assert.That(received, Is.Empty);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
