// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Step 7. What the conversion is asked to build, read out of a <b>real</b> PR1 graph.
    /// <para>
    /// Every fixture here is materialised by the real
    /// <c>SAM.Analytical.Systems.Create.MechanicalVentilation</c> over the real shipped <c>MV.json</c>,
    /// and the intent is then built by the real
    /// <c>SAM.Analytical.Tas.TPD.Create.SystemVentilationConversionContext</c>. Nothing is mocked, and
    /// no TAS licence is involved: the intent is deliberately free of COM types precisely so that this
    /// is possible.
    /// </para>
    /// <para>
    /// The topologies the brief requires proof of - supply-only, extract-only, supply and extract with
    /// unequal duties, transfer-only, branching transfer, multiple air handling units - are each
    /// asserted here against the graph PR1 actually produced.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SystemVentilationIntentTests
    {
        private static SystemVentilationConversionContext Context(AdjacencyCluster adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation)
        {
            mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            Assert.That(
                mechanicalVentilationMaterialisation.IsMaterialised,
                Is.True,
                string.Join(" | ", mechanicalVentilationMaterialisation.Refusals));

            return TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster));
        }

        private static SystemVentilationLegIntent Leg(SystemVentilationConversionContext systemVentilationConversionContext, SystemVentilationConnectionType systemVentilationConnectionType, Guid guid_SystemSpace_From, Guid guid_SystemSpace_To)
        {
            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                if (systemVentilationLegIntent.ConnectionType != systemVentilationConnectionType)
                {
                    continue;
                }

                if (systemVentilationLegIntent.Guid_SystemSpace_From == guid_SystemSpace_From
                    && systemVentilationLegIntent.Guid_SystemSpace_To == guid_SystemSpace_To)
                {
                    return systemVentilationLegIntent;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------------------ the whole shape

        [Test]
        public void Dwelling_StatesEveryRoomAndEveryLegExactlyOnce()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.Refusals, Is.Empty);

                //Five rooms: two with supply, two with extract, and the hall, which has no terminal of
                //its own and is reached only through the transfers.
                Assert.That(systemVentilationConversionContext.RoomIntents.Count, Is.EqualTo(5));

                Assert.That(systemVentilationConversionContext.Count(SystemVentilationConnectionType.Supply), Is.EqualTo(2));
                Assert.That(systemVentilationConversionContext.Count(SystemVentilationConnectionType.Extract), Is.EqualTo(2));
                Assert.That(systemVentilationConversionContext.Count(SystemVentilationConnectionType.Transfer), Is.EqualTo(4));

                Assert.That(systemVentilationConversionContext.AirSystemByAirHandlingUnit.Count, Is.EqualTo(1));
                Assert.That(systemVentilationConversionContext.AirSystemByAirHandlingUnit.ContainsKey(airHandlingUnit.Guid), Is.True);
            });
        }

        [Test]
        public void EveryRoomIntentNamesItsAnalyticalSpaceAndItsTasZone()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            foreach (SystemVentilationRoomIntent systemVentilationRoomIntent in systemVentilationConversionContext.RoomIntents)
            {
                Assert.That(systemVentilationRoomIntent.IsComplete, Is.True, systemVentilationRoomIntent.ToString());

                Assert.That(
                    systemVentilationRoomIntent.Reference_ZoneLoad,
                    Is.EqualTo(SystemVentilationFixture.ZoneReference(systemVentilationRoomIntent.Guid_Space)),
                    "the zone load must be named by the room's own stamped TAS zone guid");
            }
        }

        // ------------------------------------------------------------------------------------ SUPPLY

        [Test]
        public void SupplyOnlyRoom_CarriesItsSupplyDutyAndNoExtract()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Space space = SystemVentilationFixture.Space(adjacencyCluster, "Bedroom", true);
            Guid guid_SystemSpace = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space);

            SystemVentilationRoomIntent systemVentilationRoomIntent = systemVentilationConversionContext.RoomIntent(guid_SystemSpace);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationRoomIntent, Is.Not.Null);
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Supply_Lps, Is.EqualTo(13.0));
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Extract_Lps, Is.Null, "a supply-only room has no extract duty, not a zero one");
            });
        }

        [Test]
        public void SupplyLeg_HasNoDutyCarrier_BecauseTheDutyBelongsOnTheZone()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                if (systemVentilationLegIntent.ConnectionType != SystemVentilationConnectionType.Supply)
                {
                    continue;
                }

                Assert.Multiple(() =>
                {
                    Assert.That(systemVentilationLegIntent.RequiresDutyCarrier, Is.False);
                    Assert.That(systemVentilationLegIntent.Guid_DutyCarrier, Is.EqualTo(Guid.Empty));
                    Assert.That(systemVentilationLegIntent.Guid_SystemSpace_From, Is.EqualTo(Guid.Empty), "supply air comes from the unit, not from a room");
                    Assert.That(systemVentilationLegIntent.Guid_SystemSpace_To, Is.Not.EqualTo(Guid.Empty));
                });
            }
        }

        // ----------------------------------------------------------------------------------- EXTRACT

        [Test]
        public void ExtractOnlyRoom_CarriesItsExtractDutyAndNoSupply()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Space space = SystemVentilationFixture.Space(adjacencyCluster, "Bathroom", true);
            Guid guid_SystemSpace = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space);

            SystemVentilationRoomIntent systemVentilationRoomIntent = systemVentilationConversionContext.RoomIntent(guid_SystemSpace);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationRoomIntent, Is.Not.Null);
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Supply_Lps, Is.Null);
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Extract_Lps, Is.EqualTo(15.0));
            });
        }

        [Test]
        public void ExtractLeg_RequiresItsOwnDutyCarrier()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            List<SystemVentilationLegIntent> legs = systemVentilationConversionContext.LegIntents
                .FindAll(x => x.ConnectionType == SystemVentilationConnectionType.Extract);

            Assert.That(legs.Count, Is.EqualTo(2));

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in legs)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(systemVentilationLegIntent.RequiresDutyCarrier, Is.True);
                    Assert.That(systemVentilationLegIntent.Guid_SystemSpace_From, Is.Not.EqualTo(Guid.Empty));
                    Assert.That(systemVentilationLegIntent.Guid_SystemSpace_To, Is.EqualTo(Guid.Empty), "extract air goes to the unit, not to a room");
                });
            }
        }

        // --------------------------------------------------------------- SUPPLY AND EXTRACT, UNEQUAL

        [Test]
        public void OneRoomCarriesUnequalSupplyAndExtractIndependently()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.SupplyAndExtract(out Space space, out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Guid guid_SystemSpace = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space);

            SystemVentilationRoomIntent systemVentilationRoomIntent = systemVentilationConversionContext.RoomIntent(guid_SystemSpace);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Supply_Lps, Is.EqualTo(31.0));
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Extract_Lps, Is.EqualTo(19.0));

                Assert.That(Leg(systemVentilationConversionContext, SystemVentilationConnectionType.Supply, Guid.Empty, guid_SystemSpace).DesignFlowRate_Lps, Is.EqualTo(31.0));
                Assert.That(Leg(systemVentilationConversionContext, SystemVentilationConnectionType.Extract, guid_SystemSpace, Guid.Empty).DesignFlowRate_Lps, Is.EqualTo(19.0));
            });
        }

        // ---------------------------------------------------------------------------------- TRANSFER

        [Test]
        public void TransferOnlyRoom_IsStatedWithNoSupplyAndNoExtract()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Space space_Hall = SystemVentilationFixture.Space(adjacencyCluster, "Hall", true);
            Guid guid_SystemSpace_Hall = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space_Hall);

            SystemVentilationRoomIntent systemVentilationRoomIntent = systemVentilationConversionContext.RoomIntent(guid_SystemSpace_Hall);

            Assert.Multiple(() =>
            {
                Assert.That(guid_SystemSpace_Hall, Is.Not.EqualTo(Guid.Empty), "a room reached only through transfers is still a room");
                Assert.That(systemVentilationRoomIntent, Is.Not.Null);
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Supply_Lps, Is.Null);
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Extract_Lps, Is.Null);
                Assert.That(systemVentilationRoomIntent.IsComplete, Is.True);
            });
        }

        [Test]
        public void BranchingTransfer_IsTwoLegsWithTwoIndependentDuties()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Guid guid_Hall = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, SystemVentilationFixture.Space(adjacencyCluster, "Hall", true));
            Guid guid_Bathroom = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, SystemVentilationFixture.Space(adjacencyCluster, "Bathroom", true));
            Guid guid_Kitchen = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, SystemVentilationFixture.Space(adjacencyCluster, "Kitchen", true));

            SystemVentilationLegIntent leg_Bathroom = Leg(systemVentilationConversionContext, SystemVentilationConnectionType.Transfer, guid_Hall, guid_Bathroom);
            SystemVentilationLegIntent leg_Kitchen = Leg(systemVentilationConversionContext, SystemVentilationConnectionType.Transfer, guid_Hall, guid_Kitchen);

            Assert.Multiple(() =>
            {
                //Two legs off the same source room, each with its own duty and its own air movement.
                Assert.That(leg_Bathroom, Is.Not.Null);
                Assert.That(leg_Kitchen, Is.Not.Null);

                Assert.That(leg_Bathroom.Guid_SystemConnection, Is.Not.EqualTo(leg_Kitchen.Guid_SystemConnection));
                Assert.That(leg_Bathroom.Guid_SpaceAirMovement, Is.Not.EqualTo(leg_Kitchen.Guid_SpaceAirMovement));

                //0.0075 m3/s and 0.005 m3/s, stated by PR1 in l/s.
                Assert.That(leg_Bathroom.DesignFlowRate_Lps, Is.EqualTo(7.5).Within(1e-9));
                Assert.That(leg_Kitchen.DesignFlowRate_Lps, Is.EqualTo(5.0).Within(1e-9));

                Assert.That(leg_Bathroom.RequiresDutyCarrier, Is.True);
                Assert.That(leg_Kitchen.RequiresDutyCarrier, Is.True);
            });
        }

        [Test]
        public void EveryTransferLegNamesTheAirMovementItCameFrom()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            HashSet<Guid> guids_SpaceAirMovement = new HashSet<Guid>();

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                if (systemVentilationLegIntent.ConnectionType != SystemVentilationConnectionType.Transfer)
                {
                    Assert.That(systemVentilationLegIntent.Guid_SpaceAirMovement, Is.EqualTo(Guid.Empty));
                    continue;
                }

                Assert.That(systemVentilationLegIntent.Guid_SpaceAirMovement, Is.Not.EqualTo(Guid.Empty));
                Assert.That(guids_SpaceAirMovement.Add(systemVentilationLegIntent.Guid_SpaceAirMovement), Is.True, "one air movement cannot become two legs");
            }

            Assert.That(guids_SpaceAirMovement.Count, Is.EqualTo(4));
        }

        // ------------------------------------------------------------------------------ MULTIPLE AHUs

        [Test]
        public void TwoAirHandlingUnits_StayTwoAirSystemsWithDisjointRooms()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.TwoUnits(out AirHandlingUnit airHandlingUnit_1, out AirHandlingUnit airHandlingUnit_2);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Dictionary<Guid, Guid> dictionary = systemVentilationConversionContext.AirSystemByAirHandlingUnit;

            Assert.Multiple(() =>
            {
                Assert.That(dictionary.Count, Is.EqualTo(2));
                Assert.That(dictionary[airHandlingUnit_1.Guid], Is.Not.EqualTo(dictionary[airHandlingUnit_2.Guid]), "two physical units must not collapse into one air system");

                Assert.That(systemVentilationConversionContext.RoomIntents.Count, Is.EqualTo(10));
            });

            Dictionary<Guid, int> count_By_AirSystem = new Dictionary<Guid, int>();

            foreach (SystemVentilationRoomIntent systemVentilationRoomIntent in systemVentilationConversionContext.RoomIntents)
            {
                count_By_AirSystem.TryGetValue(systemVentilationRoomIntent.Guid_AirSystem, out int count);
                count_By_AirSystem[systemVentilationRoomIntent.Guid_AirSystem] = count + 1;
            }

            Assert.That(count_By_AirSystem.Count, Is.EqualTo(2));
            Assert.That(count_By_AirSystem.Values, Is.All.EqualTo(5));
        }

        [Test]
        public void EveryLegBelongsToTheSameAirSystemAsBothItsRooms()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.TwoUnits(out AirHandlingUnit airHandlingUnit_1, out AirHandlingUnit airHandlingUnit_2);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                foreach (Guid guid_SystemSpace in new[] { systemVentilationLegIntent.Guid_SystemSpace_From, systemVentilationLegIntent.Guid_SystemSpace_To })
                {
                    if (guid_SystemSpace == Guid.Empty)
                    {
                        continue;
                    }

                    Assert.That(
                        systemVentilationConversionContext.RoomIntent(guid_SystemSpace).Guid_AirSystem,
                        Is.EqualTo(systemVentilationLegIntent.Guid_AirSystem),
                        "a leg cannot cross air handling units");
                }
            }
        }

        // ---------------------------------------------------------------------- IDENTITY, NOT NAMES

        [Test]
        public void ThreeRoomsSharingOneDisplayNameStayThreeRooms()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.DuplicateNames(out AirHandlingUnit airHandlingUnit);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            List<SystemVentilationRoomIntent> systemVentilationRoomIntents = systemVentilationConversionContext.RoomIntents;

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.Refusals, Is.Empty);
                Assert.That(systemVentilationRoomIntents.Count, Is.EqualTo(3));

                //Three distinct analytical rooms, three distinct system spaces, three distinct zone
                //identities - from a model in which every display name is the same string.
                Assert.That(systemVentilationRoomIntents.Select(x => x.Guid_Space).Distinct().Count(), Is.EqualTo(3));
                Assert.That(systemVentilationRoomIntents.Select(x => x.Guid_SystemSpace).Distinct().Count(), Is.EqualTo(3));
                Assert.That(systemVentilationRoomIntents.Select(x => x.Reference_ZoneLoad).Distinct().Count(), Is.EqualTo(3));

                //And the duties did not swap: 11 and 12 supply, 13 extract, each on its own room.
                Assert.That(systemVentilationRoomIntents.Select(x => x.DesignFlowRate_Supply_Lps).Where(x => x.HasValue).Select(x => x.Value).OrderBy(x => x), Is.EqualTo(new[] { 11.0, 12.0 }));
                Assert.That(systemVentilationRoomIntents.Select(x => x.DesignFlowRate_Extract_Lps).Where(x => x.HasValue).Select(x => x.Value), Is.EqualTo(new[] { 13.0 }));
            });
        }

        [Test]
        public void ARoomWithNoStampedTasZoneIsRefusedByName_NotResolvedByOne()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit airHandlingUnit);

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            Space space = SystemVentilationFixture.Space(adjacencyCluster, "Kitchen", true);

            SystemVentilationConversionContext systemVentilationConversionContext = TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster, space.Guid));

            Guid guid_SystemSpace = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.RoomIntent(guid_SystemSpace).IsComplete, Is.False);

                //The intent alone does not refuse - the reconciliation does, because that is where the
                //room can be named against what was actually built. It refuses this room by IDENTITY,
                //rather than falling back to the display name it shares with nothing in the TSD.
                Assert.That(systemVentilationConversionContext.Reconcile(), Is.False);
                Assert.That(
                    systemVentilationConversionContext.Refusals.Exists(x => x.Contains(space.Guid.ToString()) && x.Contains("does not state the TAS zone guid")),
                    Is.True,
                    string.Join(" | ", systemVentilationConversionContext.Refusals));
                Assert.That(
                    systemVentilationConversionContext.Refusals.Exists(x => x.Contains(space.Name)),
                    Is.False,
                    "a refusal must identify the room, and the room's display name is not its identity");
            });
        }

        // ------------------------------------------------------------------------- ORDER INDEPENDENCE

        [Test]
        public void TheIntentIsTheSameWhicheverOrderTheLineageIsSuppliedIn()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.TwoUnits(out AirHandlingUnit airHandlingUnit_1, out AirHandlingUnit airHandlingUnit_2);

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            List<MechanicalVentilationBinding> bindings = mechanicalVentilationMaterialisation.Bindings;

            List<MechanicalVentilationBinding> bindings_Reversed = new List<MechanicalVentilationBinding>(bindings);
            bindings_Reversed.Reverse();

            Dictionary<Guid, string> zoneReferences = SystemVentilationFixture.ZoneReferences(adjacencyCluster);

            string first = Describe(TPD.Create.SystemVentilationConversionContext(mechanicalVentilationMaterialisation.SystemEnergyCentre, bindings, zoneReferences));
            string second = Describe(TPD.Create.SystemVentilationConversionContext(mechanicalVentilationMaterialisation.SystemEnergyCentre, bindings_Reversed, zoneReferences));

            Assert.That(second, Is.EqualTo(first));
        }

        private static string Describe(SystemVentilationConversionContext systemVentilationConversionContext)
        {
            List<string> lines = new List<string>();

            foreach (SystemVentilationRoomIntent systemVentilationRoomIntent in systemVentilationConversionContext.RoomIntents)
            {
                lines.Add(systemVentilationRoomIntent.ToString());
            }

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                lines.Add(systemVentilationLegIntent.ToString());
            }

            return string.Join(Environment.NewLine, lines);
        }

        // ----------------------------------------------------- DESIGN AIRFLOW, AND NOTHING ELSE

        [Test]
        public void TheDutyIsTheConnectionsDesignFlowRate_NotTheTemplatePrototypesFlow()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.SupplyAndExtract(out Space space, out AirHandlingUnit airHandlingUnit, 7.0, 3.0);

            SystemVentilationConversionContext systemVentilationConversionContext = Context(adjacencyCluster, out MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation);

            Guid guid_SystemSpace = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space);

            SystemVentilationRoomIntent systemVentilationRoomIntent = systemVentilationConversionContext.RoomIntent(guid_SystemSpace);

            //MV.json's prototype space states 128 l/s. If any of that leaked through, these would not be
            //7 and 3.
            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Supply_Lps, Is.EqualTo(7.0));
                Assert.That(systemVentilationRoomIntent.DesignFlowRate_Extract_Lps, Is.EqualTo(3.0));
            });
        }
    }
}
