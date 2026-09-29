// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Mixed Part O strategies PR3B-3, COM-free: ONE TAS Systems document holding a cooled and an uncooled dwelling.
    /// The graph is the real SAM_Systems mixed materialisation (PR3B-2: the shipped MV topology, the shipped MVRE
    /// topology for the one unit carrying manufacturer guidance) and the conversion is SAM_Tas' own production
    /// context and grounding - cooling is bound to the air system whose unit carries <c>GuidanceCooling</c>, never to
    /// the document. Fixture values only.
    /// </summary>
    [TestFixture]
    public class MixedGuidanceCoolingTests
    {
        [Test]
        public void OneDocument_BindsGuidanceCoolingToTheCooledUnitsAirSystemOnly()
        {
            AdjacencyCluster adjacencyCluster = TwoDwellings(out AirHandlingUnit airHandlingUnit_Cooled, out AirHandlingUnit airHandlingUnit_Uncooled);

            MechanicalVentilationMaterialisation mixed = Mixed(adjacencyCluster, airHandlingUnit_Cooled);
            SystemVentilationConversionContext context = Context(mixed, adjacencyCluster);

            Assert.That(context.Refusals, Is.Empty, string.Join(" | ", context.Refusals));

            Dictionary<Guid, Guid> airSystem_By_AirHandlingUnit = context.AirSystemByAirHandlingUnit;
            Assert.That(airSystem_By_AirHandlingUnit.Count, Is.EqualTo(2), "One document, one air system per dwelling unit.");

            Guid guid_AirSystem_Cooled = airSystem_By_AirHandlingUnit[airHandlingUnit_Cooled.Guid];
            Guid guid_AirSystem_Uncooled = airSystem_By_AirHandlingUnit[airHandlingUnit_Uncooled.Guid];

            MechanicalVentilationGuidanceCooling guidanceCooling = context.GuidanceCoolings.Single();
            Assert.That(guidanceCooling.Guid_AirHandlingUnit, Is.EqualTo(airHandlingUnit_Cooled.Guid));
            Assert.That(context.GuidanceCooling(guid_AirSystem_Cooled), Is.SameAs(guidanceCooling));
            Assert.That(context.GuidanceCooling(guid_AirSystem_Uncooled), Is.Null, "Cooling elsewhere in the document does not cool this unit.");

            //The uncooled unit's air system is ordinary MV - no exchanger, no coil; the cooled one is the product's.
            SystemPlantRoom systemPlantRoom = SystemVentilationFixture.PlantRoom(mixed);
            Assert.That(Kinds(systemPlantRoom, guid_AirSystem_Uncooled), Is.EqualTo((0, 0)));
            Assert.That(Kinds(systemPlantRoom, guid_AirSystem_Cooled), Is.EqualTo((1, 1)));

            //Every room of each dwelling is bound to its own unit's air system.
            foreach (SystemVentilationRoomIntent systemVentilationRoomIntent in context.RoomIntents)
            {
                string name = adjacencyCluster.GetObject<Space>(systemVentilationRoomIntent.Guid_Space)?.Name;
                Guid expected = name.EndsWith(" A") ? guid_AirSystem_Cooled : guid_AirSystem_Uncooled;
                Assert.That(systemVentilationRoomIntent.Guid_AirSystem, Is.EqualTo(expected), name);
            }
        }

        [Test]
        public void TheVentilationIntent_IsExactlyTheUncooledDocuments()
        {
            AdjacencyCluster adjacencyCluster = TwoDwellings(out AirHandlingUnit airHandlingUnit_Cooled, out AirHandlingUnit _);

            SystemVentilationConversionContext context_Mixed = Context(Mixed(adjacencyCluster, airHandlingUnit_Cooled), adjacencyCluster);
            SystemVentilationConversionContext context_Uncooled = Context(adjacencyCluster.MechanicalVentilation(SystemVentilationFixture.Template()), adjacencyCluster);

            Assert.That(context_Mixed.Refusals, Is.Empty);
            Assert.That(context_Uncooled.Refusals, Is.Empty);
            Assert.That(context_Uncooled.GuidanceCoolings, Is.Empty, "No guidance record, no cooling anywhere.");

            Assert.That(context_Mixed.RoomIntents.Count, Is.EqualTo(context_Uncooled.RoomIntents.Count));
            foreach (SystemVentilationConnectionType systemVentilationConnectionType in new[] { SystemVentilationConnectionType.Supply, SystemVentilationConnectionType.Extract, SystemVentilationConnectionType.Transfer })
            {
                Assert.That(context_Mixed.Count(systemVentilationConnectionType), Is.EqualTo(context_Uncooled.Count(systemVentilationConnectionType)), systemVentilationConnectionType.ToString());
            }

            //The design duties are the terminals' - the cooling airflow is an operating airflow, never a design one.
            Assert.That(
                context_Mixed.RoomIntents.Select(x => (x.DesignFlowRate_Supply_Lps, x.DesignFlowRate_Extract_Lps)).OrderBy(x => x),
                Is.EqualTo(context_Uncooled.RoomIntents.Select(x => (x.DesignFlowRate_Supply_Lps, x.DesignFlowRate_Extract_Lps)).OrderBy(x => x)));
        }

        [Test]
        public void TheGuidanceCoolingGrounding_TakesTheCoolingPathForTheCooledAirSystemOnly()
        {
            AdjacencyCluster adjacencyCluster = TwoDwellings(out AirHandlingUnit airHandlingUnit_Cooled, out AirHandlingUnit airHandlingUnit_Uncooled);

            SystemVentilationConversionContext context = Context(Mixed(adjacencyCluster, airHandlingUnit_Cooled), adjacencyCluster);
            Assert.That(context.Refusals, Is.Empty);

            Guid guid_AirSystem_Cooled = context.AirSystemByAirHandlingUnit[airHandlingUnit_Cooled.Guid];
            Guid guid_AirSystem_Uncooled = context.AirSystemByAirHandlingUnit[airHandlingUnit_Uncooled.Guid];

            //The production grounding ToTPD calls for every air system. Without a native TAS system (COM-free) the
            //uncooled air system has nothing to ground and succeeds untouched; the cooled one enters the cooling path,
            //which needs the native system, and says so. Invoked by reflection only because its native parameters are
            //embedded interop types, which cannot be named across an assembly boundary.
            Assert.That(GroundGuidanceCooling(context, guid_AirSystem_Uncooled), Is.True);
            Assert.That(context.Refusals, Is.Empty, "The uncooled air system took the cooling path.");

            Assert.That(GroundGuidanceCooling(context, guid_AirSystem_Cooled), Is.False);
            Assert.That(context.Refusals.Single(), Does.Contain(guid_AirSystem_Cooled.ToString()).And.Contain("no native system"));
        }

        [Test]
        public void TheCooledUnit_OperatesAtSamsCoolingAirFlow_NotItsDesign()
        {
            AdjacencyCluster adjacencyCluster = TwoDwellings(out AirHandlingUnit airHandlingUnit_Cooled, out AirHandlingUnit _);

            MechanicalVentilationGuidanceCooling guidanceCooling = Context(Mixed(adjacencyCluster, airHandlingUnit_Cooled), adjacencyCluster).GuidanceCoolings.Single();

            //The dwelling's design is 25 l/s supply / 25 l/s extract; SAM's rule gives max(design, guidance 80) = 80.
            Assert.That(guidanceCooling.ElevatedAirFlow_Lps, Is.EqualTo(80.0).Within(1e-9));
            Assert.That(guidanceCooling.Rooms.Sum(x => x.DesignSupply_Lps), Is.EqualTo(25.0).Within(1e-9));
            Assert.That(guidanceCooling.Rooms.Sum(x => x.DesignExtract_Lps), Is.EqualTo(25.0).Within(1e-9));
        }

        // =====================================================================================================
        // Fixtures
        // =====================================================================================================

        private const double DesignDuty_Lps = 25.0;

        /// <summary>The real materialisation's guidance-cooling record for the cooled unit - for tests that need a production recipe.</summary>
        internal static MechanicalVentilationGuidanceCooling GuidanceCoolingForTests()
        {
            AdjacencyCluster adjacencyCluster = TwoDwellings(out AirHandlingUnit airHandlingUnit_Cooled, out AirHandlingUnit _);

            return Context(Mixed(adjacencyCluster, airHandlingUnit_Cooled), adjacencyCluster).GuidanceCoolings.Single();
        }

        private static AdjacencyCluster TwoDwellings(out AirHandlingUnit airHandlingUnit_A, out AirHandlingUnit airHandlingUnit_B)
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out airHandlingUnit_A, " A");
            return SystemVentilationFixture.Dwelling(out airHandlingUnit_B, " B", adjacencyCluster);
        }

        /// <summary>The PR3B-2 mixed call: the shipped MV topology, the shipped MVRE topology for the one unit with guidance.</summary>
        private static MechanicalVentilationMaterialisation Mixed(AdjacencyCluster adjacencyCluster, AirHandlingUnit airHandlingUnit_Cooled)
        {
            MechanicalVentilationGuidanceSettings guidanceSettings = Product().MechanicalVentilationGuidanceSettings(DesignDuty_Lps, DesignDuty_Lps, out string refusal);
            Assert.That(refusal, Is.Null);

            MechanicalVentilationMaterialisation result = adjacencyCluster.MechanicalVentilation(
                SystemVentilationFixture.Template(),
                new MechanicalVentilationSettings
                {
                    GuidanceTemplate = TemplateMVRE(),
                    GuidanceSettings = new Dictionary<Guid, MechanicalVentilationGuidanceSettings> { { airHandlingUnit_Cooled.Guid, guidanceSettings } },
                });

            Assert.That(result.IsMaterialised, Is.True, string.Join(" | ", result.Refusals));

            return result;
        }

        private static SystemVentilationConversionContext Context(MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation, AdjacencyCluster adjacencyCluster)
        {
            return TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster),
                SystemVentilationFanHeatGainPolicy.ClearToZero,
                mechanicalVentilationMaterialisation.RecirculationCoolings,
                mechanicalVentilationMaterialisation.GuidanceCoolings);
        }

        /// <summary>The shipped MVRE topology, linked from SAM_Systems' own resources like <c>MV.json</c>.</summary>
        private static SystemEnergyCentre TemplateMVRE()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Resources", "MVRE.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The shipped MVRE.json was not copied to the test output.", path);
            }

            return Analytical.Systems.Query.SystemEnergyCentre(path);
        }

        //A catalogue product: default 80, published 60-120 l/s, capacity 150 l/s - the Nuaire-shaped envelope.
        private static VentilationUnitTemplate Product()
        {
            VentilationUnitOperatingStrategy strategy = new VentilationUnitOperatingStrategy
            {
                Source = "Test Fixture, manufacturer modelling guidance, v.1 - not a real product",
                CoolingActivationTemperature_C = 22.0,
                CoolingActivationSignal = CoolingActivationSignal.RoomTemperature,
                MinimumCoolingActivationTemperature_C = 22.0,
                MaximumCoolingActivationTemperature_C = 25.0,
                BypassMinimumIntakeTemperature_C = 12.0,
                BypassMinimumExtractTemperature_C = 18.0,
                DefaultElevatedAirFlow_Lps = 80.0,
                MinimumElevatedAirFlow_Lps = 60.0,
                MaximumElevatedAirFlow_Lps = 120.0,
                SummerBypassSupplyTemperatureRule = SupplyTemperatureRule.OutdoorAir(),
                HeatCoolthRecoverySupplyTemperatureRule = SupplyTemperatureRule.LinearBlend(0.8),
                CoolingSupplyTemperatureRule = SupplyTemperatureRule.IntakeOffset(new[] { 60.0, 80.0, 100.0, 120.0 }, new[] { 15.0, 14.0, 13.5, 13.0 }),
            };

            return new VentilationUnitTemplate(new VentilationUnitReference("Fixture", "Unit", "U-1"), "Test fixture")
            {
                MaximumSupplyFlowRate_Lps = 150.0,
                MaximumExtractFlowRate_Lps = 150.0,
                OperatingStrategy = strategy,
            };
        }

        private static bool GroundGuidanceCooling(SystemVentilationConversionContext systemVentilationConversionContext, Guid guid_AirSystem)
        {
            System.Reflection.MethodInfo methodInfo = typeof(TPD.Modify).GetMethod("GroundGuidanceCooling");
            return (bool)methodInfo.Invoke(null, new object[] { systemVentilationConversionContext, guid_AirSystem, null, null });
        }

        private static (int Exchangers, int Coils) Kinds(SystemPlantRoom systemPlantRoom, Guid guid_AirSystem)
        {
            AirSystem airSystem = systemPlantRoom.GetSystems<AirSystem>().Single(x => x.Guid == guid_AirSystem);
            List<ISystemComponent> systemComponents = systemPlantRoom.GetSystemComponents<ISystemComponent>(airSystem) ?? new List<ISystemComponent>();
            return (systemComponents.Count(x => x is SystemExchanger), systemComponents.Count(x => x is SystemDXCoil));
        }
    }
}
