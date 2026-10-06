// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        /// <summary>
        /// Reads the PR1 mechanical-ventilation graph and states, by identity alone, what TAS is to be
        /// asked to build.
        /// <para>
        /// <b>Where each fact comes from, and why none of them is a name.</b>
        /// </para>
        /// <list type="bullet">
        /// <item><description>Which room is which, and which unit serves it: PR1's own lineage rows -
        /// <c>MechanicalVentilationBindingType.SystemSpace</c> carries <c>Space.Guid -&gt;
        /// SystemSpace.Guid</c> and <c>AirSystem</c> carries <c>AirHandlingUnit.Guid -&gt;
        /// AirSystem.Guid</c>.</description></item>
        /// <item><description>Which leg is which: the <b>topology</b> of the materialised graph.
        /// A <c>SystemSpace</c>'s connector 0 is its supply inlet and connector 1 its extract outlet, so
        /// a connection touching one space at index 0 is a supply leg, one touching a space at index 1
        /// is an extract leg, and one touching two spaces is a transfer running from the index-1 end to
        /// the index-0 end. That is PR1's stated contract, not an inference from
        /// component names.</description></item>
        /// <item><description>Which duty: <c>SystemConnectionParameter.DesignFlowRate</c>, in l/s,
        /// verbatim. <b>DesignAirFlow only</b> - never a Part F required airflow, a selected equipment
        /// capacity or an operating airflow, which PR1 states separately.</description></item>
        /// <item><description>Which zone load: the analytical space's TAS zone guid, supplied by the
        /// caller from <c>SpaceParameter.ZoneGuid</c>. Measured on licensed TAS to be the same
        /// identifier the TSD's <c>ZoneLoad.GUID</c> answers.</description></item>
        /// <item><description>Which air movement a transfer came from: PR1's
        /// <c>TransferConnection</c> row.</description></item>
        /// </list>
        /// <para>
        /// <b>Linear.</b> One pass over the plant room's spaces builds the endpoint index; after that
        /// every connection resolves its two endpoints by dictionary probe. Nothing scans the graph per
        /// room and nothing scans it per leg.
        /// </para>
        /// </summary>
        /// <param name="systemEnergyCentre">PR1's materialised graph. Read, never written.</param>
        /// <param name="mechanicalVentilationBindings">PR1's analytical-source lineage.</param>
        /// <param name="dictionary_ZoneReference">
        /// Analytical <c>Space.Guid</c> to TAS zone guid, from <c>SpaceParameter.ZoneGuid</c>. A room
        /// missing from here is refused rather than resolved by name.
        /// </param>
        /// <param name="fanHeatGainPolicy">
        /// PR5A (SAM#111 plan §D/§K.3): what <c>Modify.GroundVentilationFans</c> does with a fan's native
        /// <c>HeatGainFactor</c>. <c>ClearToZero</c>, the B0 control, by default.
        /// </param>
        /// <param name="mechanicalVentilationRecirculationCoolings">
        /// PR5B (SAM#111): the recirculation cooling branches SAM_Systems materialised into the graph, or
        /// null for none (B0). Their connections touch the rooms' own connectors, so without them each would
        /// read as a second supply or extract leg of its room; they are left out of the leg intent by
        /// identity, and the ventilation intent is exactly the one the same graph states without them.
        /// </param>
        public static SystemVentilationConversionContext SystemVentilationConversionContext(
            this Core.Systems.SystemEnergyCentre systemEnergyCentre,
            IEnumerable<MechanicalVentilationBinding> mechanicalVentilationBindings,
            IDictionary<Guid, string> dictionary_ZoneReference,
            SystemVentilationFanHeatGainPolicy fanHeatGainPolicy = SystemVentilationFanHeatGainPolicy.ClearToZero,
            IEnumerable<MechanicalVentilationRecirculationCooling> mechanicalVentilationRecirculationCoolings = null,
            IEnumerable<MechanicalVentilationGuidanceCooling> mechanicalVentilationGuidanceCoolings = null)
        {
            SystemVentilationConversionContext result = new SystemVentilationConversionContext
            {
                FanHeatGainPolicy = fanHeatGainPolicy
            };

            if (mechanicalVentilationRecirculationCoolings != null)
            {
                foreach (MechanicalVentilationRecirculationCooling mechanicalVentilationRecirculationCooling in mechanicalVentilationRecirculationCoolings)
                {
                    result.Add(mechanicalVentilationRecirculationCooling);
                }
            }

            //SAM#123: the manufacturer-guidance cooling units. Their only new connections join the exchanger,
            //the supply coil and the supply fan, so they touch no room connector and change no leg intent.
            if (mechanicalVentilationGuidanceCoolings != null)
            {
                foreach (MechanicalVentilationGuidanceCooling mechanicalVentilationGuidanceCooling in mechanicalVentilationGuidanceCoolings)
                {
                    result.Add(mechanicalVentilationGuidanceCooling);
                }
            }

            if (systemEnergyCentre == null)
            {
                result.Refuse("No materialised systems graph was supplied, so there is nothing to convert.");
                return result;
            }

            if (mechanicalVentilationBindings == null)
            {
                result.Refuse("No PR1 lineage was supplied, so no room could be tied to its analytical space by identity.");
                return result;
            }

            //-------------------------------------------------------------------------------------------
            //PR1's lineage. Three of its five row types matter here; the terminal rows are an N:1
            //aggregation whose result - the leg's duty - is already on the connection itself.
            //-------------------------------------------------------------------------------------------

            Dictionary<Guid, Guid> space_By_SystemSpace = new Dictionary<Guid, Guid>();
            Dictionary<Guid, Guid> spaceAirMovement_By_Connection = new Dictionary<Guid, Guid>();
            Dictionary<Guid, SystemVentilationConnectionType> statedType_By_Connection = new Dictionary<Guid, SystemVentilationConnectionType>();

            foreach (MechanicalVentilationBinding mechanicalVentilationBinding in mechanicalVentilationBindings)
            {
                if (mechanicalVentilationBinding == null)
                {
                    continue;
                }

                switch (mechanicalVentilationBinding.BindingType)
                {
                    case MechanicalVentilationBindingType.AirSystem:
                        result.Add(mechanicalVentilationBinding.Guid_Analytical, mechanicalVentilationBinding.Guid_Systems);
                        break;

                    case MechanicalVentilationBindingType.SystemSpace:
                        if (space_By_SystemSpace.TryGetValue(mechanicalVentilationBinding.Guid_Systems, out Guid guid_Space_Existing)
                            && guid_Space_Existing != mechanicalVentilationBinding.Guid_Analytical)
                        {
                            result.Refuse(string.Format(
                                "System space {0} is stated to have come from two analytical rooms, {1} and {2}.",
                                mechanicalVentilationBinding.Guid_Systems,
                                guid_Space_Existing,
                                mechanicalVentilationBinding.Guid_Analytical));
                        }

                        space_By_SystemSpace[mechanicalVentilationBinding.Guid_Systems] = mechanicalVentilationBinding.Guid_Analytical;
                        break;

                    case MechanicalVentilationBindingType.SupplyConnection:
                        statedType_By_Connection[mechanicalVentilationBinding.Guid_Systems] = SystemVentilationConnectionType.Supply;
                        break;

                    case MechanicalVentilationBindingType.ExtractConnection:
                        statedType_By_Connection[mechanicalVentilationBinding.Guid_Systems] = SystemVentilationConnectionType.Extract;
                        break;

                    case MechanicalVentilationBindingType.TransferConnection:
                        statedType_By_Connection[mechanicalVentilationBinding.Guid_Systems] = SystemVentilationConnectionType.Transfer;
                        spaceAirMovement_By_Connection[mechanicalVentilationBinding.Guid_Systems] = mechanicalVentilationBinding.Guid_Analytical;
                        break;
                }
            }

            //-------------------------------------------------------------------------------------------
            //The graph itself.
            //-------------------------------------------------------------------------------------------

            Dictionary<Guid, Guid> airSystem_By_SystemSpace = new Dictionary<Guid, Guid>();
            List<SystemVentilationLegIntent> legs = new List<SystemVentilationLegIntent>();

            List<SystemPlantRoom> systemPlantRooms = systemEnergyCentre.GetSystemPlantRooms();
            if (systemPlantRooms == null || systemPlantRooms.Count == 0)
            {
                result.Refuse("The materialised graph holds no plant room.");
                return result;
            }

            foreach (SystemPlantRoom systemPlantRoom in systemPlantRooms)
            {
                if (systemPlantRoom == null)
                {
                    continue;
                }

                //One pass: every endpoint a leg can name, keyed by the reference a SystemConnection
                //stores. Keyed on the guid rather than on the whole ObjectReference so that a display
                //subclass and its base cannot fail to match on type name alone.
                Dictionary<string, Guid> systemSpace_By_Reference = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

                List<SystemSpace> systemSpaces = systemPlantRoom.GetSystemComponents<SystemSpace>();
                if (systemSpaces != null)
                {
                    foreach (SystemSpace systemSpace in systemSpaces)
                    {
                        string reference = ReferenceKey(systemSpace);
                        if (reference != null)
                        {
                            systemSpace_By_Reference[reference] = systemSpace.Guid;
                        }
                    }
                }

                List<AirSystem> airSystems = systemPlantRoom.GetSystems<AirSystem>();
                if (airSystems == null)
                {
                    continue;
                }

                airSystems.Sort((x, y) => x.Guid.CompareTo(y.Guid));

                foreach (AirSystem airSystem in airSystems)
                {
                    List<SystemSpace> systemSpaces_AirSystem = systemPlantRoom.GetRelatedObjects<SystemSpace>(airSystem);
                    if (systemSpaces_AirSystem != null)
                    {
                        foreach (SystemSpace systemSpace in systemSpaces_AirSystem)
                        {
                            if (airSystem_By_SystemSpace.TryGetValue(systemSpace.Guid, out Guid guid_AirSystem_Existing)
                                && guid_AirSystem_Existing != airSystem.Guid)
                            {
                                result.Refuse(string.Format(
                                    "System space {0} belongs to air systems {1} and {2}, so which unit serves the room is ambiguous.",
                                    systemSpace.Guid,
                                    guid_AirSystem_Existing,
                                    airSystem.Guid));

                                continue;
                            }

                            airSystem_By_SystemSpace[systemSpace.Guid] = airSystem.Guid;
                        }
                    }

                    List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemComponents<ISystemConnection>(airSystem);
                    if (systemConnections == null)
                    {
                        continue;
                    }

                    systemConnections.Sort((x, y) => x.Guid.CompareTo(y.Guid));

                    foreach (ISystemConnection systemConnection in systemConnections)
                    {
                        //PR5B: the cooling loop's own connections reach the rooms' connectors too, and are
                        //not ventilation legs - left out by identity, never by what they look like.
                        if (result.IsRecirculationConnection(systemConnection.Guid))
                        {
                            continue;
                        }

                        SystemVentilationLegIntent systemVentilationLegIntent = LegIntent(
                            result,
                            systemConnection,
                            airSystem.Guid,
                            systemSpace_By_Reference,
                            spaceAirMovement_By_Connection,
                            statedType_By_Connection);

                        if (systemVentilationLegIntent != null)
                        {
                            legs.Add(systemVentilationLegIntent);
                        }
                    }
                }
            }

            //-------------------------------------------------------------------------------------------
            //The rooms, with the duties their own legs state. A room's supply duty is its supply leg's
            //DesignFlowRate and nothing else - not the template prototype's flow, not a sizing rule.
            //-------------------------------------------------------------------------------------------

            Dictionary<Guid, double> supply_By_SystemSpace = new Dictionary<Guid, double>();
            Dictionary<Guid, double> extract_By_SystemSpace = new Dictionary<Guid, double>();

            legs.Sort((x, y) => x.Guid_SystemConnection.CompareTo(y.Guid_SystemConnection));

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in legs)
            {
                result.Add(systemVentilationLegIntent);

                if (systemVentilationLegIntent.ConnectionType == SystemVentilationConnectionType.Supply)
                {
                    if (supply_By_SystemSpace.ContainsKey(systemVentilationLegIntent.Guid_SystemSpace_To))
                    {
                        result.Refuse(string.Format(
                            "System space {0} carries two supply legs, so its design supply duty is ambiguous.",
                            systemVentilationLegIntent.Guid_SystemSpace_To));

                        continue;
                    }

                    supply_By_SystemSpace[systemVentilationLegIntent.Guid_SystemSpace_To] = systemVentilationLegIntent.DesignFlowRate_Lps;
                }
                else if (systemVentilationLegIntent.ConnectionType == SystemVentilationConnectionType.Extract)
                {
                    if (extract_By_SystemSpace.ContainsKey(systemVentilationLegIntent.Guid_SystemSpace_From))
                    {
                        result.Refuse(string.Format(
                            "System space {0} carries two extract legs, so its design extract duty is ambiguous.",
                            systemVentilationLegIntent.Guid_SystemSpace_From));

                        continue;
                    }

                    extract_By_SystemSpace[systemVentilationLegIntent.Guid_SystemSpace_From] = systemVentilationLegIntent.DesignFlowRate_Lps;
                }
            }

            List<Guid> guids_SystemSpace = new List<Guid>(space_By_SystemSpace.Keys);
            guids_SystemSpace.Sort();

            foreach (Guid guid_SystemSpace in guids_SystemSpace)
            {
                Guid guid_Space = space_By_SystemSpace[guid_SystemSpace];

                if (!airSystem_By_SystemSpace.TryGetValue(guid_SystemSpace, out Guid guid_AirSystem))
                {
                    result.Refuse(string.Format(
                        "Room {0} was materialised as system space {1}, which no air system serves.",
                        guid_Space,
                        guid_SystemSpace));

                    continue;
                }

                string reference_ZoneLoad = null;
                if (dictionary_ZoneReference == null || !dictionary_ZoneReference.TryGetValue(guid_Space, out reference_ZoneLoad))
                {
                    reference_ZoneLoad = null;
                }

                result.Add(new SystemVentilationRoomIntent(
                    guid_Space,
                    guid_SystemSpace,
                    guid_AirSystem,
                    reference_ZoneLoad,
                    supply_By_SystemSpace.TryGetValue(guid_SystemSpace, out double supply_Lps) ? (double?)supply_Lps : null,
                    extract_By_SystemSpace.TryGetValue(guid_SystemSpace, out double extract_Lps) ? (double?)extract_Lps : null));
            }

            //PR5B: a recirculation cooling branch has to sit inside its own unit's one air system and serve
            //only that air system's existing rooms - never a second system, never a room of another unit.
            Dictionary<Guid, Guid> airSystem_By_AirHandlingUnit = result.AirSystemByAirHandlingUnit;
            foreach (MechanicalVentilationRecirculationCooling mechanicalVentilationRecirculationCooling in result.RecirculationCoolings)
            {
                if (!airSystem_By_AirHandlingUnit.TryGetValue(mechanicalVentilationRecirculationCooling.Guid_AirHandlingUnit, out Guid guid_AirSystem_Unit)
                    || guid_AirSystem_Unit != mechanicalVentilationRecirculationCooling.Guid_AirSystem)
                {
                    result.Refuse(string.Format(
                        "The recirculation cooling branch of air handling unit {0} names air system {1}, which is not that unit's own air system.",
                        mechanicalVentilationRecirculationCooling.Guid_AirHandlingUnit,
                        mechanicalVentilationRecirculationCooling.Guid_AirSystem));

                    continue;
                }

                foreach (MechanicalVentilationRecirculationRoom mechanicalVentilationRecirculationRoom in mechanicalVentilationRecirculationCooling.Rooms)
                {
                    SystemVentilationRoomIntent systemVentilationRoomIntent = result.RoomIntent(mechanicalVentilationRecirculationRoom.Guid_SystemSpace);

                    if (systemVentilationRoomIntent == null
                        || systemVentilationRoomIntent.Guid_Space != mechanicalVentilationRecirculationRoom.Guid_Space
                        || systemVentilationRoomIntent.Guid_AirSystem != mechanicalVentilationRecirculationCooling.Guid_AirSystem)
                    {
                        result.Refuse(string.Format(
                            "The recirculation cooling branch of air system {0} serves system space {1}, which is not an existing room of that air system.",
                            mechanicalVentilationRecirculationCooling.Guid_AirSystem,
                            mechanicalVentilationRecirculationRoom.Guid_SystemSpace));
                    }
                }
            }

            //A leg whose endpoint is not one of PR1's materialised rooms is a leg into somewhere the
            //lineage does not describe. It is refused rather than dropped.
            foreach (SystemVentilationLegIntent systemVentilationLegIntent in legs)
            {
                RefuseUnknownEndpoint(result, space_By_SystemSpace, systemVentilationLegIntent, systemVentilationLegIntent.Guid_SystemSpace_From);
                RefuseUnknownEndpoint(result, space_By_SystemSpace, systemVentilationLegIntent, systemVentilationLegIntent.Guid_SystemSpace_To);
            }

            return result;
        }

        private static void RefuseUnknownEndpoint(
            SystemVentilationConversionContext systemVentilationConversionContext,
            Dictionary<Guid, Guid> space_By_SystemSpace,
            SystemVentilationLegIntent systemVentilationLegIntent,
            Guid guid_SystemSpace)
        {
            if (guid_SystemSpace == Guid.Empty || space_By_SystemSpace.ContainsKey(guid_SystemSpace))
            {
                return;
            }

            systemVentilationConversionContext.Refuse(string.Format(
                "{0} leg {1} names system space {2}, which PR1's lineage does not tie to any analytical room.",
                systemVentilationLegIntent.ConnectionType,
                systemVentilationLegIntent.Guid_SystemConnection,
                guid_SystemSpace));
        }

        /// <summary>
        /// Classifies one connection from its endpoints, or answers null when it touches no room at all -
        /// which is what the template's own fan-to-junction wiring is, and which is not a ventilation leg.
        /// </summary>
        private static SystemVentilationLegIntent LegIntent(
            SystemVentilationConversionContext systemVentilationConversionContext,
            ISystemConnection systemConnection,
            Guid guid_AirSystem,
            Dictionary<string, Guid> systemSpace_By_Reference,
            Dictionary<Guid, Guid> spaceAirMovement_By_Connection,
            Dictionary<Guid, SystemVentilationConnectionType> statedType_By_Connection)
        {
            List<ObjectReference> objectReferences = systemConnection?.ObjectReferences;
            if (objectReferences == null || objectReferences.Count == 0)
            {
                return null;
            }

            Guid guid_SystemSpace_From = Guid.Empty;
            Guid guid_SystemSpace_To = Guid.Empty;
            int count_Rooms = 0;

            foreach (ObjectReference objectReference in objectReferences)
            {
                string reference = ReferenceKey(objectReference);
                if (reference == null || !systemSpace_By_Reference.TryGetValue(reference, out Guid guid_SystemSpace))
                {
                    continue;
                }

                count_Rooms++;

                if (!systemConnection.TryGetIndex(objectReference, out int index))
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Connection {0} names system space {1} without a connector index, so which way the air runs is not stated.",
                        systemConnection.Guid,
                        guid_SystemSpace));

                    return null;
                }

                //SystemSpace connector 0 is the supply inlet and connector 1 the extract outlet. That is
                //PR1's contract, declared by the space's own SystemConnectorManager.
                if (index == 0)
                {
                    guid_SystemSpace_To = guid_SystemSpace;
                }
                else if (index == 1)
                {
                    guid_SystemSpace_From = guid_SystemSpace;
                }
                else
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Connection {0} attaches to system space {1} on connector {2}, which is not an air connector.",
                        systemConnection.Guid,
                        guid_SystemSpace,
                        index));

                    return null;
                }
            }

            if (count_Rooms == 0)
            {
                return null;
            }

            SystemVentilationConnectionType systemVentilationConnectionType;

            if (guid_SystemSpace_From != Guid.Empty && guid_SystemSpace_To != Guid.Empty)
            {
                systemVentilationConnectionType = SystemVentilationConnectionType.Transfer;
            }
            else if (guid_SystemSpace_To != Guid.Empty)
            {
                systemVentilationConnectionType = SystemVentilationConnectionType.Supply;
            }
            else
            {
                systemVentilationConnectionType = SystemVentilationConnectionType.Extract;
            }

            //PR1 states the type independently, through its lineage rows. Where it does, the two must
            //agree: a leg silently remapped from one direction to another is exactly what this route
            //exists to make impossible.
            if (statedType_By_Connection.TryGetValue(systemConnection.Guid, out SystemVentilationConnectionType systemVentilationConnectionType_Stated)
                && systemVentilationConnectionType_Stated != systemVentilationConnectionType)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Connection {0} is stated by PR1 as a {1} leg and its endpoints make it a {2} leg.",
                    systemConnection.Guid,
                    systemVentilationConnectionType_Stated,
                    systemVentilationConnectionType));

                return null;
            }

            Guid guid_SpaceAirMovement = Guid.Empty;
            if (systemVentilationConnectionType == SystemVentilationConnectionType.Transfer
                && !spaceAirMovement_By_Connection.TryGetValue(systemConnection.Guid, out guid_SpaceAirMovement))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Transfer leg {0} runs between two rooms and PR1's lineage names no air movement it came from.",
                    systemConnection.Guid));

                return null;
            }

            if (!(systemConnection is SAMObject sAMObject) || !sAMObject.TryGetValue(SystemConnectionParameter.DesignFlowRate, out double designFlowRate_Lps))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1} states no design flow rate.",
                    systemVentilationConnectionType,
                    systemConnection.Guid));

                return null;
            }

            return new SystemVentilationLegIntent(
                systemVentilationConnectionType,
                systemConnection.Guid,
                guid_AirSystem,
                guid_SystemSpace_From,
                guid_SystemSpace_To,
                guid_SpaceAirMovement,
                designFlowRate_Lps,
                Guid.Empty);
        }

        private static string ReferenceKey(SAMObject sAMObject)
        {
            return sAMObject == null ? null : sAMObject.Guid.ToString("N");
        }

        private static string ReferenceKey(ObjectReference objectReference)
        {
            Core.Reference? reference = objectReference?.Reference;

            return reference == null || !reference.HasValue ? null : reference.Value.ToString();
        }
    }
}
