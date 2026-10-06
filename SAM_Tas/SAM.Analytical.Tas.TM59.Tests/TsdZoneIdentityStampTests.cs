// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Regression for the TSD zone identity stamp</b> - <c>Convert.ToSAM(TSD.ZoneData)</c> now writes
    /// <c>SpaceParameter.ZoneGuid</c> onto the simulated space explicitly, the way
    /// <c>Convert.ToSAM(TBD.zone)</c> always has.
    /// <para>
    /// <b>What was wrong.</b> The TSD converter left the stamp to the generic
    /// <c>Create.ParameterSet_Space</c> / <c>TypeMap</c> path. That path is registered
    /// (<c>ActiveSetting</c> maps <c>SpaceParameter.ZoneGuid</c> to <c>TSD.ZoneData."zoneGUID"</c>) but cannot
    /// fire: <c>SAM.Core.Create.ParameterSet</c> reads the source property using the SAM-side parameter name
    /// - <c>"Zone Guid"</c>, with a space - instead of the TAS-side <c>"zoneGUID"</c>, so the value is
    /// silently never carried across. A live Part O run showed it directly: every design space had a
    /// <c>ZoneGuid</c>, every simulated space had none, and all nine spaces resolved by unique name.
    /// </para>
    /// <para>
    /// <b>Why the name fallback succeeding was not good enough.</b> It only holds while every room name is
    /// unique across the whole model. The real validation shape is three flats each containing a room called
    /// exactly <c>"Bedroom 2"</c>, where the name is ambiguous by construction and
    /// <c>SimulationSpaceMap</c> refuses rather than guess - see
    /// <see cref="DuplicateRoomNames_ResolveByZoneGuidWhereTheyPreviouslyRefused"/>, which fails without the
    /// stamp.
    /// </para>
    /// <para>
    /// <b>No TAS COM here.</b> <c>TSD.ZoneData</c> is a COM interop type and cannot be constructed without an
    /// installed TAS, so these tests stamp spaces exactly as the fixed converter stamps them - the same
    /// approach <c>GbXMLRouteIdentityAcceptanceTests</c> takes. What they pin is the contract either side of
    /// the converter: that <c>Query.SimulationSpaceKey</c> reads what it writes, that association then
    /// resolves by key, and that a genuinely absent guid still falls back to the name.
    /// </para>
    /// </summary>
    [TestFixture]
    public class TsdZoneIdentityStampTests
    {
        //Qualified deliberately: inside this namespace SAM.Analytical.Tas has its own SpaceParameter and
        //Query, each shadowing the SAM.Analytical one.
        private const string zoneGuid_Bedroom2_Flat1 = "6f1b0f2e-0000-4000-8000-000000000001";
        private const string zoneGuid_Bedroom2_Flat2 = "6f1b0f2e-0000-4000-8000-000000000002";
        private const string zoneGuid_Corridor = "6f1b0f2e-0000-4000-8000-00000000000c";

        /// <summary>
        /// The stamp the converter writes is the value <c>Query.SimulationSpaceKey</c> reads back. This is the
        /// join between the two halves of the fix: the converter writes <c>SpaceParameter.ZoneGuid</c>, and
        /// that query is what <c>SimulationSpaceMap</c> is handed as its key selector.
        /// </summary>
        [Test]
        public void ASpaceStampedAsTheConverterStampsIt_YieldsThatZoneGuidAsItsSimulationSpaceKey()
        {
            Space space = Simulated("Bedroom 2", zoneGuid_Bedroom2_Flat1);

            Assert.That(Tas.Query.SimulationSpaceKey(space), Is.EqualTo(zoneGuid_Bedroom2_Flat1));
        }

        /// <summary>
        /// A simulated space carrying the guid resolves to its design space by that guid, not by its name -
        /// proved by giving the simulated side a DIFFERENT name. A rename must not break the association.
        /// </summary>
        [Test]
        public void ASimulatedSpaceCarryingTheZoneGuid_ResolvesByStableKeyAndNotByName()
        {
            Space space_Design = Design("Bedroom 2", zoneGuid_Bedroom2_Flat1);
            Space space_Simulation = Simulated("Renamed After The Simulation", zoneGuid_Bedroom2_Flat1);

            SimulationSpaceMap simulationSpaceMap = Create.SimulationSpaceMap([space_Design], [space_Simulation]);

            Assert.Multiple(() =>
            {
                Assert.That(simulationSpaceMap.IsComplete, Is.True);
                Assert.That(simulationSpaceMap.Design(space_Simulation)?.Guid, Is.EqualTo(space_Design.Guid));
            });
        }

        /// <summary>
        /// <b>The test this fix exists for.</b> Two flats each with a room named exactly "Bedroom 2". With the
        /// simulated side stamped, each resolves to its own flat by guid. With it blank - the state the live
        /// run was in - the name is ambiguous on both sides and the map refuses, which is the correct
        /// behaviour for a blank key and exactly why the unique-name fallback was never a substitute for the
        /// stamp.
        /// </summary>
        [Test]
        public void DuplicateRoomNames_ResolveByZoneGuidWhereTheyPreviouslyRefused()
        {
            Space space_Design_Flat1 = Design("Bedroom 2", zoneGuid_Bedroom2_Flat1);
            Space space_Design_Flat2 = Design("Bedroom 2", zoneGuid_Bedroom2_Flat2);

            Space space_Simulation_Flat1 = Simulated("Bedroom 2", zoneGuid_Bedroom2_Flat1);
            Space space_Simulation_Flat2 = Simulated("Bedroom 2", zoneGuid_Bedroom2_Flat2);

            SimulationSpaceMap simulationSpaceMap = Create.SimulationSpaceMap(
                [space_Design_Flat1, space_Design_Flat2],
                [space_Simulation_Flat1, space_Simulation_Flat2]);

            Assert.Multiple(() =>
            {
                Assert.That(simulationSpaceMap.IsComplete, Is.True);
                Assert.That(simulationSpaceMap.Design(space_Simulation_Flat1)?.Guid, Is.EqualTo(space_Design_Flat1.Guid));
                Assert.That(simulationSpaceMap.Design(space_Simulation_Flat2)?.Guid, Is.EqualTo(space_Design_Flat2.Guid));
            });

            //The same two rooms without the stamp - what the converter produced before this fix.
            Space space_Simulation_Flat1_Unstamped = Simulated("Bedroom 2", null);
            Space space_Simulation_Flat2_Unstamped = Simulated("Bedroom 2", null);

            SimulationSpaceMap simulationSpaceMap_Unstamped = Create.SimulationSpaceMap(
                [space_Design_Flat1, space_Design_Flat2],
                [space_Simulation_Flat1_Unstamped, space_Simulation_Flat2_Unstamped]);

            Assert.Multiple(() =>
            {
                Assert.That(simulationSpaceMap_Unstamped.IsComplete, Is.False);
                Assert.That(simulationSpaceMap_Unstamped.Design(space_Simulation_Flat1_Unstamped), Is.Null);
                Assert.That(simulationSpaceMap_Unstamped.Design(space_Simulation_Flat2_Unstamped), Is.Null);
            });
        }

        /// <summary>
        /// <b>The fallback is untouched.</b> Where the source zone genuinely has no guid the converter stamps
        /// nothing, the space states no identity, and the documented unique-name rule still resolves it. This
        /// is the behaviour every model that predates the stamp depends on.
        /// </summary>
        [Test]
        public void ASimulatedSpaceWithNoZoneGuid_StillResolvesByUniqueName()
        {
            Space space_Design = Design("Corridor", zoneGuid_Corridor);
            Space space_Simulation = Simulated("Corridor", null);

            SimulationSpaceMap simulationSpaceMap = Create.SimulationSpaceMap([space_Design], [space_Simulation]);

            Assert.Multiple(() =>
            {
                Assert.That(Tas.Query.SimulationSpaceKey(space_Simulation), Is.Null);
                Assert.That(simulationSpaceMap.IsComplete, Is.True);
                Assert.That(simulationSpaceMap.Design(space_Simulation)?.Guid, Is.EqualTo(space_Design.Guid));
            });
        }

        /// <summary>
        /// The converter guards on <c>string.IsNullOrWhiteSpace</c> rather than stamping whatever TAS returned,
        /// so a blank guid leaves the space with no parameter at all. This pins that the guard and the
        /// alternative - stamping the blank - are behaviourally the same to the map, which is why the guard is
        /// safe: both fall back to the name, neither states an empty identity.
        /// </summary>
        [Test]
        public void ABlankZoneGuid_IsNotAStatedIdentityAndFallsBackToTheName()
        {
            Space space_Design = Design("Corridor", zoneGuid_Corridor);

            Space space_Simulation = new Space("Corridor");
            space_Simulation.SetValue(SpaceParameter.ZoneGuid, "   ");

            SimulationSpaceMap simulationSpaceMap = Create.SimulationSpaceMap([space_Design], [space_Simulation]);

            Assert.Multiple(() =>
            {
                Assert.That(simulationSpaceMap.IsComplete, Is.True);
                Assert.That(simulationSpaceMap.Design(space_Simulation)?.Guid, Is.EqualTo(space_Design.Guid));
            });
        }

        /// <summary>
        /// A design space as the workflow leaves it - <c>Modify.UpdateIds</c> stamps the TBD zone guid.
        /// </summary>
        private static Space Design(string name, string zoneGuid)
        {
            Space result = new Space(name);
            result.SetValue(SpaceParameter.ZoneGuid, zoneGuid);

            return result;
        }

        /// <summary>
        /// A simulated space as the fixed <c>Convert.ToSAM(TSD.ZoneData)</c> leaves it - a fresh space with a
        /// fresh guid, no internal condition, and the zone guid stamped where the source stated one.
        /// </summary>
        private static Space Simulated(string name, string zoneGuid)
        {
            Space result = new Space(name);

            if (!string.IsNullOrWhiteSpace(zoneGuid))
            {
                result.SetValue(SpaceParameter.ZoneGuid, zoneGuid);
            }

            return result;
        }
    }
}
