// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// The models the PR2 ventilation tests work from, and the walk-up to the shipped <c>MV.json</c>.
    /// <para>
    /// <b>These tests convert a REAL PR1 graph.</b> Every fixture here builds an ordinary
    /// <c>AdjacencyCluster</c> from <c>SAM.Analytical</c>'s own public constructors and then runs the
    /// real <c>SAM.Analytical.Systems.Create.MechanicalVentilation</c> over the real shipped template.
    /// A hand-written stand-in for that graph would let PR2 be tested against PR2's idea of PR1 -
    /// exactly the disagreement the conversion is supposed to catch. Nothing here touches TAS: no COM
    /// object is created and no licence is involved.
    /// </para>
    /// <para>
    /// The dwelling shape is the one PR1's own tests use, because it is the one that exercises
    /// everything at once: two habitable rooms with supply, a hall with no terminals, two wet rooms with
    /// extract, and four transfers routing <c>habitable -&gt; hall -&gt; wet room</c> - so the hall
    /// branches two in and two out.
    /// </para>
    /// </summary>
    internal static class SystemVentilationFixture
    {
        internal const double Area = 12.0;
        internal const double Volume = 30.0;

        /// <summary>
        /// The shipped <c>MV.json</c>, copied next to the test assembly from SAM_Systems' own resources.
        /// </summary>
        internal static SystemEnergyCentre Template()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Resources", "MV.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The shipped MV.json was not copied to the test output.", path);
            }

            return Analytical.Systems.Query.SystemEnergyCentre(path);
        }

        /// <summary>Runs the real PR1 materialisation over the shipped template.</summary>
        internal static MechanicalVentilationMaterialisation Materialise(AdjacencyCluster adjacencyCluster)
        {
            return adjacencyCluster.MechanicalVentilation(Template());
        }

        internal static Space Space(AdjacencyCluster adjacencyCluster, string name)
        {
            Space result = new Space(name, new Geometry.Spatial.Point3D(0, 0, 0));

            result.SetValue(Analytical.SpaceParameter.Area, Area);
            result.SetValue(Analytical.SpaceParameter.Volume, Volume);

            adjacencyCluster.AddObject(result);

            return result;
        }

        internal static VentilationSystem VentilationSystem(AdjacencyCluster adjacencyCluster, string name_AirHandlingUnit, out AirHandlingUnit airHandlingUnit)
        {
            airHandlingUnit = Analytical.Create.AirHandlingUnit(name_AirHandlingUnit);
            airHandlingUnit.SummerSupplyTemperature = double.NaN;
            airHandlingUnit.WinterSupplyTemperature = double.NaN;

            adjacencyCluster.AddObject(airHandlingUnit);

            VentilationSystemType ventilationSystemType = Analytical.Create.VentilationSystemType(
                Guid.NewGuid(),
                "MVHR",
                "Continuous mechanical supply and extract with heat recovery.");

            VentilationSystem result = Analytical.Create.MechanicalSystem(ventilationSystemType, null, 1) as VentilationSystem;

            result.SetValue(VentilationSystemParameter.SupplyUnitName, name_AirHandlingUnit);
            result.SetValue(VentilationSystemParameter.ExhaustUnitName, name_AirHandlingUnit);

            adjacencyCluster.AddObject(result);

            return result;
        }

        internal static void Serve(AdjacencyCluster adjacencyCluster, VentilationSystem ventilationSystem, Space space)
        {
            adjacencyCluster.AddRelation(ventilationSystem, space);
        }

        internal static VentilationTerminal Terminal(AdjacencyCluster adjacencyCluster, VentilationSystem ventilationSystem, Space space, FlowClassification flowClassification, double? designFlowRate_Lps)
        {
            VentilationTerminal result = new VentilationTerminal(
                string.Format("{0} {1}", space.Name, flowClassification),
                flowClassification,
                designFlowRate_Lps);

            adjacencyCluster.AddObject(result);
            adjacencyCluster.AddRelation(ventilationSystem, result);
            adjacencyCluster.AddRelation(result, space);

            return result;
        }

        internal static SpaceAirMovement Transfer(AdjacencyCluster adjacencyCluster, Space space_From, Space space_To, double airFlow_M3S)
        {
            SpaceAirMovement result = new SpaceAirMovement(
                string.Format("{0} -> {1}", space_From.Name, space_To.Name),
                airFlow_M3S,
                new ObjectReference(space_From).ToString(),
                new ObjectReference(space_To).ToString());

            adjacencyCluster.AddObject(result);
            adjacencyCluster.AddRelation(result, space_To);

            return result;
        }

        /// <summary>
        /// The canonical single-unit dwelling: supply into two habitable rooms, extract out of two wet
        /// rooms, a hall with no terminals at all, and four transfers - so one model carries supply-only
        /// rooms, extract-only rooms, a transfer-only room and a branching transfer at once.
        /// </summary>
        internal static AdjacencyCluster Dwelling(out AirHandlingUnit airHandlingUnit, string suffix = "", AdjacencyCluster adjacencyCluster = null)
        {
            adjacencyCluster = adjacencyCluster ?? new AdjacencyCluster();

            VentilationSystem ventilationSystem = VentilationSystem(adjacencyCluster, "AHU" + suffix, out airHandlingUnit);

            Space space_Bedroom = Space(adjacencyCluster, "Bedroom" + suffix);
            Space space_Living = Space(adjacencyCluster, "Living" + suffix);
            Space space_Hall = Space(adjacencyCluster, "Hall" + suffix);
            Space space_Bathroom = Space(adjacencyCluster, "Bathroom" + suffix);
            Space space_Kitchen = Space(adjacencyCluster, "Kitchen" + suffix);

            Serve(adjacencyCluster, ventilationSystem, space_Bedroom);
            Serve(adjacencyCluster, ventilationSystem, space_Living);
            Serve(adjacencyCluster, ventilationSystem, space_Bathroom);
            Serve(adjacencyCluster, ventilationSystem, space_Kitchen);

            Terminal(adjacencyCluster, ventilationSystem, space_Bedroom, FlowClassification.Supply, 13.0);
            Terminal(adjacencyCluster, ventilationSystem, space_Living, FlowClassification.Supply, 12.0);
            Terminal(adjacencyCluster, ventilationSystem, space_Bathroom, FlowClassification.Extract, 15.0);
            Terminal(adjacencyCluster, ventilationSystem, space_Kitchen, FlowClassification.Extract, 10.0);

            Transfer(adjacencyCluster, space_Bedroom, space_Hall, 0.0065);
            Transfer(adjacencyCluster, space_Living, space_Hall, 0.006);
            Transfer(adjacencyCluster, space_Hall, space_Bathroom, 0.0075);
            Transfer(adjacencyCluster, space_Hall, space_Kitchen, 0.005);

            return adjacencyCluster;
        }

        /// <summary>
        /// One room carrying <b>both</b> a supply and an extract terminal, with deliberately unequal
        /// duties - the case that decides whether the two are ever conflated. 31 and 19 are the values
        /// the licensed checkpoint authored and read back independently.
        /// </summary>
        internal static AdjacencyCluster SupplyAndExtract(out Space space, out AirHandlingUnit airHandlingUnit, double supply_Lps = 31.0, double extract_Lps = 19.0)
        {
            AdjacencyCluster result = new AdjacencyCluster();

            VentilationSystem ventilationSystem = VentilationSystem(result, "AHU", out airHandlingUnit);

            space = Space(result, "Studio");

            Serve(result, ventilationSystem, space);

            Terminal(result, ventilationSystem, space, FlowClassification.Supply, supply_Lps);
            Terminal(result, ventilationSystem, space, FlowClassification.Extract, extract_Lps);

            return result;
        }

        /// <summary>
        /// Two dwellings on two separate physical units - the model that proves two analytical air
        /// handling units stay two TAS systems and neither room set leaks into the other.
        /// </summary>
        internal static AdjacencyCluster TwoUnits(out AirHandlingUnit airHandlingUnit_1, out AirHandlingUnit airHandlingUnit_2)
        {
            AdjacencyCluster result = Dwelling(out airHandlingUnit_1, " 1");

            Dwelling(out airHandlingUnit_2, " 2", result);

            return result;
        }

        /// <summary>
        /// The same dwelling with <b>every space, terminal and unit sharing one display name</b>. If any
        /// step of the conversion or the reconciliation resolved by name, this model would alias its
        /// rooms onto one another and the assertions built on it would fail.
        /// </summary>
        internal static AdjacencyCluster DuplicateNames(out AirHandlingUnit airHandlingUnit)
        {
            AdjacencyCluster result = new AdjacencyCluster();

            VentilationSystem ventilationSystem = VentilationSystem(result, "AHU", out airHandlingUnit);

            Space space_1 = Space(result, "Bedroom 2");
            Space space_2 = Space(result, "Bedroom 2");
            Space space_3 = Space(result, "Bedroom 2");

            Serve(result, ventilationSystem, space_1);
            Serve(result, ventilationSystem, space_2);
            Serve(result, ventilationSystem, space_3);

            Terminal(result, ventilationSystem, space_1, FlowClassification.Supply, 11.0);
            Terminal(result, ventilationSystem, space_2, FlowClassification.Supply, 12.0);
            Terminal(result, ventilationSystem, space_3, FlowClassification.Extract, 13.0);

            Transfer(result, space_1, space_3, 0.004);

            return result;
        }

        /// <summary>
        /// A multi-dwelling model at a stated size, several dwellings per physical unit - which is what a
        /// real scheme looks like, and what the scaling evidence has to stay linear in.
        /// </summary>
        internal static AdjacencyCluster Scaled(int dwellings, int dwellingsPerUnit, out int count_Spaces, out int count_AirHandlingUnits)
        {
            count_Spaces = dwellings * 5;
            count_AirHandlingUnits = (dwellings + dwellingsPerUnit - 1) / dwellingsPerUnit;

            AdjacencyCluster result = new AdjacencyCluster();

            VentilationSystem ventilationSystem = null;

            for (int i = 0; i < dwellings; i++)
            {
                if (i % dwellingsPerUnit == 0)
                {
                    ventilationSystem = VentilationSystem(result, string.Format("AHU {0}", i / dwellingsPerUnit), out AirHandlingUnit airHandlingUnit);
                }

                Space space_Bedroom = Space(result, string.Format("Bedroom {0}", i));
                Space space_Living = Space(result, string.Format("Living {0}", i));
                Space space_Hall = Space(result, string.Format("Hall {0}", i));
                Space space_Bathroom = Space(result, string.Format("Bathroom {0}", i));
                Space space_Kitchen = Space(result, string.Format("Kitchen {0}", i));

                Serve(result, ventilationSystem, space_Bedroom);
                Serve(result, ventilationSystem, space_Living);
                Serve(result, ventilationSystem, space_Bathroom);
                Serve(result, ventilationSystem, space_Kitchen);

                Terminal(result, ventilationSystem, space_Bedroom, FlowClassification.Supply, 13.0);
                Terminal(result, ventilationSystem, space_Living, FlowClassification.Supply, 12.0);
                Terminal(result, ventilationSystem, space_Bathroom, FlowClassification.Extract, 15.0);
                Terminal(result, ventilationSystem, space_Kitchen, FlowClassification.Extract, 10.0);

                Transfer(result, space_Bedroom, space_Hall, 0.0065);
                Transfer(result, space_Living, space_Hall, 0.006);
                Transfer(result, space_Hall, space_Bathroom, 0.0075);
                Transfer(result, space_Hall, space_Kitchen, 0.005);
            }

            return result;
        }

        /// <summary>
        /// A stand-in for what the TBD workflow stamps: every space's <c>SpaceParameter.ZoneGuid</c>.
        /// A test needs a plausible TAS zone guid per room and never a real one - what matters is that
        /// the value is per-room, opaque and not a name.
        /// </summary>
        internal static Dictionary<Guid, string> ZoneReferences(AdjacencyCluster adjacencyCluster, params Guid[] omit)
        {
            HashSet<Guid> omitted = new HashSet<Guid>(omit ?? new Guid[0]);

            Dictionary<Guid, string> result = new Dictionary<Guid, string>();

            foreach (Space space in adjacencyCluster.GetSpaces() ?? new List<Space>())
            {
                if (omitted.Contains(space.Guid))
                {
                    continue;
                }

                result[space.Guid] = ZoneReference(space.Guid);
            }

            return result;
        }

        /// <summary>The TAS-shaped zone guid a room is stamped with, derived so a test can predict it.</summary>
        internal static string ZoneReference(Guid guid_Space)
        {
            return string.Concat("{", guid_Space.ToString("D").ToUpperInvariant(), "}");
        }

        internal static SystemPlantRoom PlantRoom(MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation)
        {
            List<SystemPlantRoom> systemPlantRooms = mechanicalVentilationMaterialisation.SystemEnergyCentre.GetSystemPlantRooms();

            return systemPlantRooms.Count == 1
                ? systemPlantRooms[0]
                : throw new InvalidOperationException(string.Format("{0} plant rooms.", systemPlantRooms.Count));
        }

        /// <summary>The materialised room of one analytical space, found through PR1's lineage.</summary>
        internal static Guid SystemSpaceGuid(MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation, Space space)
        {
            foreach (MechanicalVentilationBinding mechanicalVentilationBinding in mechanicalVentilationMaterialisation.Bindings)
            {
                if (mechanicalVentilationBinding.BindingType == MechanicalVentilationBindingType.SystemSpace
                    && mechanicalVentilationBinding.Guid_Analytical == space.Guid)
                {
                    return mechanicalVentilationBinding.Guid_Systems;
                }
            }

            return Guid.Empty;
        }

        /// <summary>Every space of a cluster, by name. Only for a fixture whose names ARE unique.</summary>
        internal static Space Space(AdjacencyCluster adjacencyCluster, string name, bool find)
        {
            foreach (Space space in adjacencyCluster.GetSpaces() ?? new List<Space>())
            {
                if (space.Name == name)
                {
                    return space;
                }
            }

            return null;
        }
    }
}
