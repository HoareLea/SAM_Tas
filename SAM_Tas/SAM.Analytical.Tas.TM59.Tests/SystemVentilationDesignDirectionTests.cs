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
    /// The regression for the acceptance-fixture defect a manual TAS review of the PR2 closeout exposed.
    /// <para>
    /// <b>What went wrong.</b> The licensed acceptance harness authored its own ventilation design
    /// instead of consuming the one the analytical model states: it cleared the model's inherited
    /// ventilation objects and then handed out supply / extract / transfer roles by <b>ascending space
    /// guid</b>. The route converted that synthetic design faithfully, so every native check passed - and
    /// the accepted document still contained transfers running the opposite way round to the design
    /// Iteration 1a produces (<c>Kitchen -&gt; Bedroom</c> where the real design states
    /// <c>Bedroom -&gt; Kitchen</c>). The defect was in the fixture, not in the conversion, and no
    /// automated test could see it because no automated test stated a design whose direction disagreed
    /// with guid order.
    /// </para>
    /// <para>
    /// <b>What this pins.</b> A design whose transfer runs from the HIGHER-guid room to the LOWER-guid
    /// one - deliberately against ascending guid order, whatever guids the run happens to draw - must
    /// reach the conversion in exactly that direction, with the roles the design's own terminals state.
    /// Anything that derived a role or a direction from guid order, enumeration order or a display name
    /// would report the mirror image and fail here.
    /// </para>
    /// <para>
    /// COM-free, like every other PR2 conversion test: the graph is materialised by the real
    /// <c>Create.MechanicalVentilation</c> over the real shipped <c>MV.json</c>, and the intent by the
    /// real <c>Create.SystemVentilationConversionContext</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SystemVentilationDesignDirectionTests
    {
        /// <summary>
        /// A two-room dwelling whose transfer runs AGAINST ascending guid order: the supplied room is
        /// whichever of the two spaces drew the higher guid, and the extracted room the lower.
        /// </summary>
        private static AdjacencyCluster Dwelling(out Space space_Supplied, out Space space_Extracted)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            Space space_1 = SystemVentilationFixture.Space(adjacencyCluster, "Bedroom");
            Space space_2 = SystemVentilationFixture.Space(adjacencyCluster, "Kitchen");

            //The one decision this fixture makes, and it is made from the guids themselves so the design
            //is against guid order on EVERY run rather than on the runs where the dice fell that way.
            bool firstIsHigher = space_1.Guid.CompareTo(space_2.Guid) > 0;

            space_Supplied = firstIsHigher ? space_1 : space_2;
            space_Extracted = firstIsHigher ? space_2 : space_1;

            VentilationSystem ventilationSystem = SystemVentilationFixture.VentilationSystem(adjacencyCluster, "MVHR-01", out AirHandlingUnit airHandlingUnit);

            SystemVentilationFixture.Serve(adjacencyCluster, ventilationSystem, space_Supplied);
            SystemVentilationFixture.Serve(adjacencyCluster, ventilationSystem, space_Extracted);

            SystemVentilationFixture.Terminal(adjacencyCluster, ventilationSystem, space_Supplied, FlowClassification.Supply, 30.0);
            SystemVentilationFixture.Terminal(adjacencyCluster, ventilationSystem, space_Extracted, FlowClassification.Extract, 30.0);

            //30 l/s, the duty that closes both rooms: the supplied room passes on everything it receives
            //and the extracted room extracts everything it draws in.
            SystemVentilationFixture.Transfer(adjacencyCluster, space_Supplied, space_Extracted, 0.030);

            return adjacencyCluster;
        }

        [Test]
        public void ATransferStatedAgainstGuidOrder_ReachesTheConversionInTheDirectionTheDesignStates()
        {
            AdjacencyCluster adjacencyCluster = Dwelling(out Space space_Supplied, out Space space_Extracted);

            Assert.That(
                space_Supplied.Guid.CompareTo(space_Extracted.Guid),
                Is.GreaterThan(0),
                "the fixture only proves anything while the design runs against ascending guid order");

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            Assert.That(
                mechanicalVentilationMaterialisation.IsMaterialised,
                Is.True,
                string.Join(" | ", mechanicalVentilationMaterialisation.Refusals));

            SystemVentilationConversionContext systemVentilationConversionContext = TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster));

            Guid guid_Supplied = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space_Supplied);
            Guid guid_Extracted = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space_Extracted);

            SystemVentilationLegIntent leg_Transfer = null;
            int count_Transfer = 0;

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                if (systemVentilationLegIntent.ConnectionType != SystemVentilationConnectionType.Transfer)
                {
                    continue;
                }

                count_Transfer++;
                leg_Transfer = systemVentilationLegIntent;
            }

            SystemVentilationRoomIntent room_Supplied = systemVentilationConversionContext.RoomIntent(guid_Supplied);
            SystemVentilationRoomIntent room_Extracted = systemVentilationConversionContext.RoomIntent(guid_Extracted);

            Assert.Multiple(() =>
            {
                Assert.That(systemVentilationConversionContext.Refusals, Is.Empty);

                Assert.That(count_Transfer, Is.EqualTo(1), "the design states one transfer, so one leg and no mirror of it");

                Assert.That(leg_Transfer, Is.Not.Null);
                Assert.That(leg_Transfer.Guid_SystemSpace_From, Is.EqualTo(guid_Supplied), "the transfer leaves the room the design says it leaves");
                Assert.That(leg_Transfer.Guid_SystemSpace_To, Is.EqualTo(guid_Extracted), "and arrives at the room the design says it arrives at");
                Assert.That(leg_Transfer.DesignFlowRate_Lps, Is.EqualTo(30.0).Within(1e-9));

                //And the roles, which the same defect would have swapped: the higher-guid room is the
                //supplied one because its own terminal says so, not because of where it sorts.
                Assert.That(room_Supplied, Is.Not.Null);
                Assert.That(room_Supplied.DesignFlowRate_Supply_Lps, Is.EqualTo(30.0).Within(1e-9));
                Assert.That(room_Supplied.DesignFlowRate_Extract_Lps, Is.Null);

                Assert.That(room_Extracted, Is.Not.Null);
                Assert.That(room_Extracted.DesignFlowRate_Supply_Lps, Is.Null);
                Assert.That(room_Extracted.DesignFlowRate_Extract_Lps, Is.EqualTo(30.0).Within(1e-9));
            });
        }

        /// <summary>
        /// The same design stated the other way round has to produce the mirror image and nothing else -
        /// so the test above cannot be passing because the conversion happens to prefer one direction.
        /// </summary>
        [Test]
        public void ReversingTheDesignReversesTheLeg_AndNothingElseChanges()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            Space space_1 = SystemVentilationFixture.Space(adjacencyCluster, "Bedroom");
            Space space_2 = SystemVentilationFixture.Space(adjacencyCluster, "Kitchen");

            //Deliberately WITH ascending guid order this time.
            bool firstIsLower = space_1.Guid.CompareTo(space_2.Guid) < 0;

            Space space_Supplied = firstIsLower ? space_1 : space_2;
            Space space_Extracted = firstIsLower ? space_2 : space_1;

            VentilationSystem ventilationSystem = SystemVentilationFixture.VentilationSystem(adjacencyCluster, "MVHR-01", out AirHandlingUnit airHandlingUnit);

            SystemVentilationFixture.Serve(adjacencyCluster, ventilationSystem, space_Supplied);
            SystemVentilationFixture.Serve(adjacencyCluster, ventilationSystem, space_Extracted);

            SystemVentilationFixture.Terminal(adjacencyCluster, ventilationSystem, space_Supplied, FlowClassification.Supply, 30.0);
            SystemVentilationFixture.Terminal(adjacencyCluster, ventilationSystem, space_Extracted, FlowClassification.Extract, 30.0);

            SystemVentilationFixture.Transfer(adjacencyCluster, space_Supplied, space_Extracted, 0.030);

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            Assert.That(
                mechanicalVentilationMaterialisation.IsMaterialised,
                Is.True,
                string.Join(" | ", mechanicalVentilationMaterialisation.Refusals));

            SystemVentilationConversionContext systemVentilationConversionContext = TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster));

            Guid guid_Supplied = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space_Supplied);
            Guid guid_Extracted = SystemVentilationFixture.SystemSpaceGuid(mechanicalVentilationMaterialisation, space_Extracted);

            List<SystemVentilationLegIntent> legs_Transfer = new List<SystemVentilationLegIntent>();

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                if (systemVentilationLegIntent.ConnectionType == SystemVentilationConnectionType.Transfer)
                {
                    legs_Transfer.Add(systemVentilationLegIntent);
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(space_Supplied.Guid.CompareTo(space_Extracted.Guid), Is.LessThan(0));
                Assert.That(legs_Transfer, Has.Count.EqualTo(1));
                Assert.That(legs_Transfer[0].Guid_SystemSpace_From, Is.EqualTo(guid_Supplied));
                Assert.That(legs_Transfer[0].Guid_SystemSpace_To, Is.EqualTo(guid_Extracted));
            });
        }
    }
}
