// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Step 8. The conversion reconciliation - the check that stands between "TAS accepted the calls"
    /// and "TAS now holds the graph the design states".
    /// <para>
    /// <b>Method.</b> Each test builds the intent from a real PR1 graph, then plays back what a
    /// conversion would have recorded - correctly, or with exactly one thing wrong - and asserts what
    /// the reconciliation says. Playing it back rather than running TAS is the point: a licensed run
    /// can show that the correct case passes, and only an injected fault can show that the incorrect
    /// case is caught rather than tolerated.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SystemVentilationReconciliationTests
    {
        private const string Prefix_Zone = "{ZONE-";
        private const string Prefix_System = "{SYS-";
        private const string Prefix_Damper = "{DMP-";

        private static string Reference(string prefix, Guid guid)
        {
            return string.Concat(prefix, guid.ToString("D").ToUpperInvariant(), "}");
        }

        private static SystemVentilationConversionContext Intent(AdjacencyCluster adjacencyCluster)
        {
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            Assert.That(mechanicalVentilationMaterialisation.IsMaterialised, Is.True, string.Join(" | ", mechanicalVentilationMaterialisation.Refusals));

            SystemVentilationConversionContext result = TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster));

            //The duty carriers the working copy would have gained, assigned through the production
            //derivation so the identities are the ones a real run would produce.
            foreach (SystemVentilationLegIntent systemVentilationLegIntent in result.LegIntents)
            {
                if (!systemVentilationLegIntent.RequiresDutyCarrier)
                {
                    continue;
                }

                result.SetDutyCarrier(
                    systemVentilationLegIntent.Guid_SystemConnection,
                    TPD.Query.SystemVentilationGuid("DutyCarrier", TPD.Query.SystemVentilationGuidComponent(systemVentilationLegIntent.Guid_SystemConnection)));
            }

            return result;
        }

        /// <summary>
        /// Plays back a conversion that did everything right, subject to the supplied faults.
        /// </summary>
        private static SystemVentilationConversionContext Converted(
            AdjacencyCluster adjacencyCluster,
            Func<SystemVentilationRoomIntent, string> reference_SystemZone = null,
            Func<SystemVentilationRoomIntent, string> reference_ZoneLoad = null,
            Func<SystemVentilationRoomIntent, double?> supply_Lps = null,
            Func<SystemVentilationLegIntent, double> leg_Lps = null,
            Func<SystemVentilationLegIntent, string> reference_FlowController = null,
            Func<Guid, string> reference_System = null,
            Func<Guid, string> reference_System_Room = null,
            HashSet<Guid> omit_Rooms = null,
            HashSet<Guid> omit_Legs = null,
            int extra_NativeSystems = 0)
        {
            SystemVentilationConversionContext result = Intent(adjacencyCluster);

            reference_SystemZone = reference_SystemZone ?? (x => Reference(Prefix_Zone, x.Guid_SystemSpace));
            reference_ZoneLoad = reference_ZoneLoad ?? (x => x.Reference_ZoneLoad);
            supply_Lps = supply_Lps ?? (x => x.DesignFlowRate_Supply_Lps);
            leg_Lps = leg_Lps ?? (x => x.DesignFlowRate_Lps);
            reference_FlowController = reference_FlowController ?? (x => x.RequiresDutyCarrier ? Reference(Prefix_Damper, x.Guid_DutyCarrier) : null);
            reference_System = reference_System ?? (x => Reference(Prefix_System, x));
            reference_System_Room = reference_System_Room ?? reference_System;

            foreach (KeyValuePair<Guid, Guid> keyValuePair in result.AirSystemByAirHandlingUnit)
            {
                result.RecordAirSystem(keyValuePair.Value, reference_System(keyValuePair.Value));
                result.RecordNativeSystem();
            }

            for (int i = 0; i < extra_NativeSystems; i++)
            {
                result.RecordNativeSystem();
            }

            foreach (SystemVentilationRoomIntent systemVentilationRoomIntent in result.RoomIntents)
            {
                if (omit_Rooms != null && omit_Rooms.Contains(systemVentilationRoomIntent.Guid_SystemSpace))
                {
                    continue;
                }

                result.RecordPairing(systemVentilationRoomIntent.Guid_SystemSpace, reference_SystemZone(systemVentilationRoomIntent));

                result.RecordRoomPairing(
                    systemVentilationRoomIntent.Guid_SystemSpace,
                    reference_SystemZone(systemVentilationRoomIntent),
                    reference_ZoneLoad(systemVentilationRoomIntent),
                    reference_System_Room(systemVentilationRoomIntent.Guid_AirSystem),
                    supply_Lps(systemVentilationRoomIntent));
            }

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in result.LegIntents)
            {
                if (omit_Legs != null && omit_Legs.Contains(systemVentilationLegIntent.Guid_SystemConnection))
                {
                    continue;
                }

                result.Record(new SystemVentilationConnectionBinding(
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SpaceAirMovement,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    systemVentilationLegIntent.Guid_AirSystem,
                    systemVentilationLegIntent.Guid_SystemSpace_From,
                    systemVentilationLegIntent.Guid_SystemSpace_To,
                    leg_Lps(systemVentilationLegIntent),
                    reference_FlowController(systemVentilationLegIntent)));
            }

            result.CompleteRoomBindings();

            return result;
        }

        private static AdjacencyCluster Dwelling()
        {
            return SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);
        }

        private static void AssertRefused(SystemVentilationConversionContext systemVentilationConversionContext, string fragment)
        {
            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.Reconcile(), Is.False);
                Assert.That(systemVentilationConversionContext.IsReconciled, Is.False);
                Assert.That(
                    systemVentilationConversionContext.Refusals.Exists(x => x.Contains(fragment)),
                    Is.True,
                    string.Join(" | ", systemVentilationConversionContext.Refusals));
            });
        }

        // -------------------------------------------------------------------------------- the good case

        [Test]
        public void AFaithfulConversionReconciles()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(Dwelling());

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.Reconcile(), Is.True, string.Join(" | ", systemVentilationConversionContext.Refusals));
                Assert.That(systemVentilationConversionContext.IsReconciled, Is.True);
                Assert.That(systemVentilationConversionContext.Bindings.Count, Is.EqualTo(5));
                Assert.That(systemVentilationConversionContext.ConnectionBindings.Count, Is.EqualTo(8));
            });
        }

        [Test]
        public void NothingIsReconciledUntilReconcileHasRun()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(Dwelling());

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.ReconcileAttempted, Is.False);
                Assert.That(systemVentilationConversionContext.IsReconciled, Is.False, "an unchecked conversion is never a reconciled one");
            });
        }

        // ------------------------------------------------------------------------------- air systems

        [Test]
        public void AnAhuThatProducedNoTasSystemIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Intent(Dwelling());

            //Nothing recorded at all: the unit was to become a system and none appeared.
            AssertRefused(systemVentilationConversionContext, "no native TAS system was produced for it");
        }

        [Test]
        public void TwoAhusCollapsingOntoOneTasSystemIsRefused()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.TwoUnits(out AirHandlingUnit airHandlingUnit_1, out AirHandlingUnit airHandlingUnit_2);

            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                adjacencyCluster,
                reference_System: x => "{SYS-ONE}");

            AssertRefused(systemVentilationConversionContext, "collapsed into one");
        }

        [Test]
        public void AnExtraTasSystemNobodyAskedForIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(Dwelling(), extra_NativeSystems: 1);

            AssertRefused(systemVentilationConversionContext, "native TAS air system(s)");
        }

        // ------------------------------------------------------------------------------------- rooms

        [Test]
        public void ARoomThatWasNeverBoundIsRefused()
        {
            //One cluster, materialised twice: PR1's identities are derived from the analytical guids, so
            //the room chosen from the intent is the same room the playback then omits.
            AdjacencyCluster adjacencyCluster = Dwelling();

            HashSet<Guid> omitted = new HashSet<Guid> { Intent(adjacencyCluster).RoomIntents[0].Guid_SystemSpace };

            AssertRefused(Converted(adjacencyCluster, omit_Rooms: omitted), "no native zone was bound for it");
        }

        [Test]
        public void ARoomBoundToAZoneLoadOtherThanItsOwnIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                reference_ZoneLoad: x => "{LOAD-SOMEBODY-ELSE}");

            AssertRefused(systemVentilationConversionContext, "and its analytical space names TAS zone");
        }

        [Test]
        public void TwoRoomsBoundToOneNativeZoneIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                reference_SystemZone: x => "{ZONE-SHARED}");

            AssertRefused(systemVentilationConversionContext, "were both bound to native zone");
        }

        [Test]
        public void TwoRoomsSharingOneZoneLoadIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                reference_ZoneLoad: x => "{LOAD-SHARED}");

            AssertRefused(systemVentilationConversionContext, "were both bound to zone load");
        }

        [Test]
        public void ARoomBoundIntoTheWrongTasSystemIsRefused()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.TwoUnits(out AirHandlingUnit airHandlingUnit_1, out AirHandlingUnit airHandlingUnit_2);

            SystemVentilationConversionContext systemVentilationConversionContext = Intent(adjacencyCluster);

            List<Guid> guids_AirSystem = new List<Guid>(systemVentilationConversionContext.AirSystemByAirHandlingUnit.Values);
            guids_AirSystem.Sort();

            //The two air systems are recorded correctly, so nothing collapsed; only the ROOMS report the
            //first unit's native system, whichever unit they actually belong to.
            SystemVentilationConversionContext context = Converted(
                adjacencyCluster,
                reference_System_Room: x => Reference(Prefix_System, guids_AirSystem[0]));

            AssertRefused(context, "was bound into native TAS system");
        }

        // -------------------------------------------------------------------------------------- flows

        [Test]
        public void ASupplyDutyThatDoesNotSurviveOntoTheZoneIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                supply_Lps: x => x.DesignFlowRate_Supply_Lps.HasValue ? (double?)(x.DesignFlowRate_Supply_Lps.Value + 1.0) : null);

            AssertRefused(systemVentilationConversionContext, "supply: the source graph states");
        }

        [Test]
        public void ALegDutyThatDoesNotSurviveOntoItsCarrierIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                leg_Lps: x => x.DesignFlowRate_Lps * 2.0);

            AssertRefused(systemVentilationConversionContext, "the native carrier holds");
        }

        [Test]
        public void SinglePrecisionRoundTripIsNotTreatedAsAMismatch()
        {
            //An authored 128.0 reads back from TAS as 128.00001525878906. That is the storage, not a
            //disagreement, and refusing it would refuse every correct conversion.
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                leg_Lps: x => (double)(float)x.DesignFlowRate_Lps,
                supply_Lps: x => x.DesignFlowRate_Supply_Lps.HasValue ? (double?)(double)(float)x.DesignFlowRate_Supply_Lps.Value : null);

            Assert.That(systemVentilationConversionContext.Reconcile(), Is.True, string.Join(" | ", systemVentilationConversionContext.Refusals));
        }

        [Test]
        public void ANonFiniteNativeFlowIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                leg_Lps: x => double.NaN);

            AssertRefused(systemVentilationConversionContext, "produced an incomplete binding");
        }

        // --------------------------------------------------------------------------------------- legs

        [Test]
        public void ALegThatWasNeverMaterialisedIsRefused()
        {
            AdjacencyCluster adjacencyCluster = Dwelling();

            SystemVentilationConversionContext systemVentilationConversionContext = Intent(adjacencyCluster);

            HashSet<Guid> omitted = new HashSet<Guid> { systemVentilationConversionContext.LegIntents[0].Guid_SystemConnection };

            AssertRefused(Converted(adjacencyCluster, omit_Legs: omitted), "nothing was materialised for it");
        }

        [Test]
        public void AnExtractLegWithNoNativeDamperIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                reference_FlowController: x => null);

            AssertRefused(systemVentilationConversionContext, "no native damper was materialised to hold it");
        }

        [Test]
        public void TwoLegsSharingOneNativeDamperIsRefused()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(
                Dwelling(),
                reference_FlowController: x => x.RequiresDutyCarrier ? "{DMP-SHARED}" : null);

            AssertRefused(systemVentilationConversionContext, "share native damper");
        }

        // ----------------------------------------------------------------------- order independence

        [Test]
        public void ReconciliationDoesNotDependOnTheOrderThingsWereRecordedIn()
        {
            AdjacencyCluster adjacencyCluster = Dwelling();

            SystemVentilationConversionContext first = Converted(adjacencyCluster);
            SystemVentilationConversionContext second = Converted(adjacencyCluster);

            Assert.Multiple(() =>
            {
                Assert.That(first.Reconcile(), Is.True);
                Assert.That(second.Reconcile(), Is.True);

                Assert.That(
                    string.Join("|", second.Bindings.ConvertAll(x => x.ToString())),
                    Is.EqualTo(string.Join("|", first.Bindings.ConvertAll(x => x.ToString()))));

                Assert.That(
                    string.Join("|", second.ConnectionBindings.ConvertAll(x => x.ToString())),
                    Is.EqualTo(string.Join("|", first.ConnectionBindings.ConvertAll(x => x.ToString()))));
            });
        }

        [Test]
        public void DuplicateDisplayNamesDoNotChangeTheOutcome()
        {
            SystemVentilationConversionContext systemVentilationConversionContext = Converted(SystemVentilationFixture.DuplicateNames(out AirHandlingUnit airHandlingUnit));

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.Reconcile(), Is.True, string.Join(" | ", systemVentilationConversionContext.Refusals));
                Assert.That(systemVentilationConversionContext.Bindings.Count, Is.EqualTo(3));
            });
        }
    }
}
