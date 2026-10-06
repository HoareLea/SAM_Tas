// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Step 6. The room and connection binding records - the identity chain PR2 is built on.
    /// <para>
    /// What is pinned here comes straight out of the licensed checkpoint:
    /// </para>
    /// <list type="bullet">
    /// <item><description>a room's native zone guid and its zone-load guid are <b>distinct</b> identifiers,
    /// and both are carried, because only the load guid resolves through
    /// <c>TSDData.GetZoneLoadForGuid</c>;</description></item>
    /// <item><description>supply and extract are separate duties that can differ on one room - measured at
    /// 31 l/s supply against 19 l/s extract - so a binding must never conflate them;</description></item>
    /// <item><description>a transfer leg names <b>both</b> rooms and the <c>SpaceAirMovement</c> it came
    /// from, which is what lets a branching transfer be told from two unrelated legs;</description></item>
    /// <item><description>no display name carries authority anywhere - a real TAS-authored file was
    /// observed with two different zones both named "System Zone 1".</description></item>
    /// </list>
    /// <para>Both records are free of TAS COM types, so all of this runs without a TAS licence.</para>
    /// </summary>
    [TestFixture]
    public class SystemVentilationBindingTests
    {
        private static Guid G(int seed)
        {
            return new Guid(seed, 0, 0, new byte[8]);
        }

        private static SystemVentilationBinding Room(
            int space = 1,
            int systemSpace = 2,
            int airSystem = 3,
            string zone = "{ZONE-1}",
            string load = "{LOAD-1}",
            string system = "{SYS-1}",
            double? supply = 31.0,
            double? extract = 19.0)
        {
            return new SystemVentilationBinding(
                space == 0 ? Guid.Empty : G(space),
                systemSpace == 0 ? Guid.Empty : G(systemSpace),
                airSystem == 0 ? Guid.Empty : G(airSystem),
                zone,
                load,
                system,
                supply,
                extract);
        }

        // ---------------------------------------------------------------- room bindings

        [Test]
        public void RoomBinding_CarriesTheWholeChain()
        {
            SystemVentilationBinding binding = Room();

            Assert.Multiple(() =>
            {
                Assert.That(binding.Guid_Space, Is.EqualTo(G(1)));
                Assert.That(binding.Guid_SystemSpace, Is.EqualTo(G(2)));
                Assert.That(binding.Guid_AirSystem, Is.EqualTo(G(3)));
                Assert.That(binding.Reference_SystemZone, Is.EqualTo("{ZONE-1}"));
                Assert.That(binding.Reference_ZoneLoad, Is.EqualTo("{LOAD-1}"));
                Assert.That(binding.IsComplete, Is.True);
                Assert.That(binding.CanResolveResults, Is.True);
            });
        }

        [Test]
        public void RoomBinding_ZoneAndLoadReferencesAreSeparateFields()
        {
            // Measured on licensed TAS: for a zone bound by AddZoneLoad, SystemZone.GUID was
            // {09EDFD23-...} while its ZoneLoad.GUID was {37FA3D5C-...}. Neither substitutes for the
            // other, so the record must have somewhere to put each.
            SystemVentilationBinding binding = Room(
                zone: "{09EDFD23-6F7A-41A0-A261-72C460997A6A}",
                load: "{37FA3D5C-27E0-41D5-8825-366F8DBD66AD}");

            Assert.That(binding.Reference_SystemZone, Is.Not.EqualTo(binding.Reference_ZoneLoad));
            Assert.That(binding.CanResolveResults, Is.True);
        }

        [Test]
        public void RoomBinding_WithoutAZoneLoad_CannotResolveResults()
        {
            SystemVentilationBinding binding = Room(load: null);

            Assert.That(binding.IsComplete, Is.True, "The room is still identified...");
            Assert.That(
                binding.CanResolveResults,
                Is.False,
                "...but with no zone-load guid there is nothing for GetZoneLoadForGuid to resolve, and "
                + "that must be visible rather than discovered as an empty series later.");
        }

        [TestCase(0, 2, 3, "{Z}", "{S}", TestName = "RoomBinding_refuses_without_Space")]
        [TestCase(1, 0, 3, "{Z}", "{S}", TestName = "RoomBinding_refuses_without_SystemSpace")]
        [TestCase(1, 2, 0, "{Z}", "{S}", TestName = "RoomBinding_refuses_without_AirSystem")]
        [TestCase(1, 2, 3, null, "{S}", TestName = "RoomBinding_refuses_without_zone_reference")]
        [TestCase(1, 2, 3, "{Z}", null, TestName = "RoomBinding_refuses_without_system_reference")]
        public void RoomBinding_IncompleteChain_Refuses(int space, int systemSpace, int airSystem, string zone, string system)
        {
            SystemVentilationBinding binding = Room(
                space: space, systemSpace: systemSpace, airSystem: airSystem, zone: zone, system: system);

            Assert.That(binding.IsComplete, Is.False, "A binding missing a link must refuse, not resolve by name.");
        }

        [Test]
        public void RoomBinding_SupplyAndExtractAreNeverConflated()
        {
            // The measured case: one room carrying 31 l/s supply and 19 l/s extract simultaneously.
            SystemVentilationBinding binding = Room(supply: 31.0, extract: 19.0);

            Assert.Multiple(() =>
            {
                Assert.That(binding.DesignFlowRate_Supply_Lps, Is.EqualTo(31.0));
                Assert.That(binding.DesignFlowRate_Extract_Lps, Is.EqualTo(19.0));
            });
        }

        [Test]
        public void RoomBinding_ExtractOnlyRoom_HasNoSupply()
        {
            // A wet room really has no supply. That is a stated absence, not a missing value, so it is
            // null rather than 0 - which would read as "a supply duty of zero was designed".
            SystemVentilationBinding binding = Room(supply: null, extract: 23.0);

            Assert.Multiple(() =>
            {
                Assert.That(binding.DesignFlowRate_Supply_Lps, Is.Null);
                Assert.That(binding.DesignFlowRate_Extract_Lps, Is.EqualTo(23.0));
                Assert.That(binding.IsComplete, Is.True, "A room with no supply is still a valid room.");
            });
        }

        [Test]
        public void RoomBindings_TwoRoomsSharingADisplayName_RemainDistinct()
        {
            // Three flats each with a "Bedroom 2" - the shape the repository already uses for this hazard,
            // and the shape a real TAS-authored file was observed to have (two zones both named
            // "System Zone 1"). The record carries no name at all, so aliasing is impossible by
            // construction; this asserts the identities stay distinct through a set.
            List<SystemVentilationBinding> bindings = new List<SystemVentilationBinding>
            {
                Room(space: 11, systemSpace: 21, zone: "{ZONE-A}", load: "{LOAD-A}"),
                Room(space: 12, systemSpace: 22, zone: "{ZONE-B}", load: "{LOAD-B}"),
                Room(space: 13, systemSpace: 23, zone: "{ZONE-C}", load: "{LOAD-C}"),
            };

            Assert.Multiple(() =>
            {
                Assert.That(bindings.Select(x => x.Guid_Space).Distinct().Count(), Is.EqualTo(3));
                Assert.That(bindings.Select(x => x.Reference_SystemZone).Distinct().Count(), Is.EqualTo(3));
                Assert.That(bindings.Select(x => x.Reference_ZoneLoad).Distinct().Count(), Is.EqualTo(3));
            });
        }

        // ---------------------------------------------------------------- connection bindings

        private static SystemVentilationConnectionBinding Leg(
            SystemVentilationConnectionType connectionType,
            int from,
            int to,
            double flow,
            int movement = 0,
            int connection = 99,
            string controller = "{DAMPER-1}")
        {
            return new SystemVentilationConnectionBinding(
                connectionType,
                movement == 0 ? Guid.Empty : G(movement),
                connection == 0 ? Guid.Empty : G(connection),
                G(3),
                from == 0 ? Guid.Empty : G(from),
                to == 0 ? Guid.Empty : G(to),
                flow,
                controller);
        }

        [Test]
        public void SupplyLeg_NamesTheRoomItServes()
        {
            SystemVentilationConnectionBinding leg = Leg(SystemVentilationConnectionType.Supply, 0, 21, 17.0);

            Assert.Multiple(() =>
            {
                Assert.That(leg.IsComplete, Is.True);
                Assert.That(leg.Guid_SystemSpace_From, Is.EqualTo(Guid.Empty), "Supply comes from the unit, not a room.");
                Assert.That(leg.Guid_SystemSpace_To, Is.EqualTo(G(21)));
                Assert.That(leg.DesignFlowRate_Lps, Is.EqualTo(17.0));
            });
        }

        [Test]
        public void ExtractLeg_NamesTheRoomItServes()
        {
            SystemVentilationConnectionBinding leg = Leg(SystemVentilationConnectionType.Extract, 22, 0, 23.0);

            Assert.Multiple(() =>
            {
                Assert.That(leg.IsComplete, Is.True);
                Assert.That(leg.Guid_SystemSpace_From, Is.EqualTo(G(22)));
                Assert.That(leg.Guid_SystemSpace_To, Is.EqualTo(Guid.Empty), "Extract goes to the unit, not a room.");
            });
        }

        [Test]
        public void TransferLeg_MustNameBothRoomsAndItsAirMovement()
        {
            SystemVentilationConnectionBinding leg = Leg(
                SystemVentilationConnectionType.Transfer, 21, 22, 11.0, movement: 50);

            Assert.That(leg.IsComplete, Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(
                    Leg(SystemVentilationConnectionType.Transfer, 0, 22, 11.0, movement: 50).IsComplete,
                    Is.False,
                    "A transfer leg with no source room is not a transfer.");

                Assert.That(
                    Leg(SystemVentilationConnectionType.Transfer, 21, 0, 11.0, movement: 50).IsComplete,
                    Is.False,
                    "A transfer leg with no destination room is not a transfer.");

                Assert.That(
                    Leg(SystemVentilationConnectionType.Transfer, 21, 22, 11.0, movement: 0).IsComplete,
                    Is.False,
                    "Without the originating SpaceAirMovement a branching transfer cannot be told from "
                    + "two unrelated legs.");
            });
        }

        [Test]
        public void BranchingTransfer_IsTwoRowsFromOneSource_EachWithItsOwnDuty()
        {
            // Measured on licensed TAS: two legs off one junction, 11 l/s and 7 l/s, both preserved.
            List<SystemVentilationConnectionBinding> legs = new List<SystemVentilationConnectionBinding>
            {
                Leg(SystemVentilationConnectionType.Transfer, 21, 22, 11.0, movement: 50, connection: 101),
                Leg(SystemVentilationConnectionType.Transfer, 21, 23, 7.0, movement: 51, connection: 102),
            };

            Assert.Multiple(() =>
            {
                Assert.That(legs.All(x => x.IsComplete), Is.True);
                Assert.That(legs.Select(x => x.Guid_SystemSpace_From).Distinct().Count(), Is.EqualTo(1), "One source.");
                Assert.That(legs.Select(x => x.Guid_SystemSpace_To).Distinct().Count(), Is.EqualTo(2), "Two destinations.");
                Assert.That(legs.Select(x => x.DesignFlowRate_Lps), Is.EquivalentTo(new[] { 11.0, 7.0 }));
                Assert.That(
                    legs.Select(x => x.Guid_SystemConnection).Distinct().Count(),
                    Is.EqualTo(2),
                    "Each leg reconciles against its own PR1 connection, exactly once.");
            });
        }

        [Test]
        public void UndefinedConnectionType_Refuses()
        {
            Assert.That(Leg(SystemVentilationConnectionType.Undefined, 21, 22, 11.0, movement: 50).IsComplete, Is.False);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-1.0)]
        public void NonFiniteOrNegativeDesignFlow_Refuses(double flow)
        {
            Assert.That(
                Leg(SystemVentilationConnectionType.Supply, 0, 21, flow).IsComplete,
                Is.False,
                "A design flow that is not a finite, non-negative number is not a design flow.");
        }

        [Test]
        public void NullFlowController_IsHonestNotFatal()
        {
            // A TPD duct has no guid of its own, so where no in-line damper was created there is simply
            // no native reference. That must be null and must not stop the row being usable - PR1's
            // connection identity plus the two endpoints already state the topology.
            SystemVentilationConnectionBinding leg = Leg(
                SystemVentilationConnectionType.Transfer, 21, 22, 11.0, movement: 50, controller: null);

            Assert.Multiple(() =>
            {
                Assert.That(leg.Reference_FlowController, Is.Null);
                Assert.That(leg.IsComplete, Is.True);
            });
        }

        [Test]
        public void ZeroDesignFlow_IsAllowed()
        {
            // Zero is a real authored duty (a room PR1 gave no supply to), not an error.
            Assert.That(Leg(SystemVentilationConnectionType.Supply, 0, 21, 0.0).IsComplete, Is.True);
        }
    }
}
