// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// Puts a duty-carrying <c>SystemDamper</c> into every extract and transfer leg of a
        /// <b>working copy</b> of the PR1 graph, so that each leg has somewhere to hold its own absolute
        /// design airflow.
        /// <para>
        /// <b>Why anything has to be inserted at all.</b> Measured on licensed TAS: a <c>Duct</c>
        /// declares no GUID and no design-flow property, and a <c>Junction</c> declares no members at
        /// all. An absolute per-leg duty can only live on a flow-controlling component in the leg -
        /// <c>Damper.DesignFlowRate</c> with <c>DesignFlowType = tpdFlowRateValue</c>, which was
        /// authored, saved, reopened and read back exactly, including 31 l/s supply and 19 l/s extract
        /// coexisting on one room and two branching transfer legs of 11 and 7 l/s off one source.
        /// PR1's graph runs its extract legs straight into the unit's junction and its transfer legs
        /// room to room, so neither has a carrier until one is put there.
        /// </para>
        /// <para>
        /// <b>Supply legs are left exactly as PR1 built them.</b> A room's supply duty belongs on its
        /// own <c>SystemZone.FlowRate</c> / <c>.FreshAir</c>, which is where TAS itself puts it, so no
        /// damper is invented for a supply leg and none is needed.
        /// </para>
        /// <para>
        /// <b>One damper per leg, never one shared.</b> A branching transfer is two legs off one source
        /// room; each owns its own damper and its own duty, and the junction the template may place
        /// between them carries nothing. Sharing a carrier would make one number stand for two designs,
        /// which is refused by the reconciliation.
        /// </para>
        /// <para>
        /// <b>Identities are derived, not minted.</b> The damper and its two replacement connections get
        /// guids derived from the PR1 connection they belong to, so the same graph converted twice - in
        /// any enumeration order - produces the same identities. The original PR1 connection is removed
        /// from the working copy and replaced by the pair; its guid remains the leg's identity
        /// throughout, and the reconciliation still accounts for it exactly once.
        /// </para>
        /// <para>
        /// <b>PR1's graph is never touched.</b> The caller passes a copy; this mutates that copy.
        /// </para>
        /// </summary>
        /// <param name="systemEnergyCentre">A working copy of PR1's graph. Mutated in place.</param>
        /// <param name="systemVentilationConversionContext">
        /// The intent. Each extract and transfer leg is updated with the duty carrier materialised for
        /// it; refusals are recorded here.
        /// </param>
        /// <returns>True when every leg that needs a carrier has one.</returns>
        public static bool MaterialiseVentilationDutyCarriers(
            this Core.Systems.SystemEnergyCentre systemEnergyCentre,
            SystemVentilationConversionContext systemVentilationConversionContext)
        {
            if (systemVentilationConversionContext == null)
            {
                return false;
            }

            if (systemEnergyCentre == null)
            {
                systemVentilationConversionContext.Refuse("No working copy of the systems graph was supplied.");
                return false;
            }

            List<SystemVentilationLegIntent> systemVentilationLegIntents = systemVentilationConversionContext.LegIntents;

            int count = 0;

            List<SystemPlantRoom> systemPlantRooms = systemEnergyCentre.GetSystemPlantRooms();
            if (systemPlantRooms == null)
            {
                systemVentilationConversionContext.Refuse("The working copy holds no plant room.");
                return false;
            }

            foreach (SystemPlantRoom systemPlantRoom in systemPlantRooms)
            {
                if (systemPlantRoom == null)
                {
                    continue;
                }

                //One pass over the plant room, so nothing below scans it again per leg.
                Dictionary<Guid, ISystemConnection> dictionary_SystemConnection = new Dictionary<Guid, ISystemConnection>();
                List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemConnections();
                if (systemConnections != null)
                {
                    foreach (ISystemConnection systemConnection in systemConnections)
                    {
                        dictionary_SystemConnection[systemConnection.Guid] = systemConnection;
                    }
                }

                Dictionary<Guid, AirSystem> dictionary_AirSystem = new Dictionary<Guid, AirSystem>();
                List<AirSystem> airSystems = systemPlantRoom.GetSystems<AirSystem>();
                if (airSystems != null)
                {
                    foreach (AirSystem airSystem in airSystems)
                    {
                        dictionary_AirSystem[airSystem.Guid] = airSystem;
                    }
                }

                Dictionary<Guid, SystemSpace> dictionary_SystemSpace = new Dictionary<Guid, SystemSpace>();
                List<SystemSpace> systemSpaces = systemPlantRoom.GetSystemComponents<SystemSpace>();
                if (systemSpaces != null)
                {
                    foreach (SystemSpace systemSpace in systemSpaces)
                    {
                        dictionary_SystemSpace[systemSpace.Guid] = systemSpace;
                    }
                }

                DisplaySystemDamper displaySystemDamper_Prototype = null;
                List<DisplaySystemDamper> displaySystemDampers = systemPlantRoom.GetSystemComponents<DisplaySystemDamper>();

                //PR5B: a recirculation cooling branch's dampers are the cooling loop's, not the template's,
                //so they are never the prototype a ventilation duty carrier is copied from - B0's carriers
                //come out exactly as they would with no branch at all.
                displaySystemDampers?.RemoveAll(x => systemVentilationConversionContext.IsRecirculationComponent(x.Guid));

                if (displaySystemDampers != null && displaySystemDampers.Count != 0)
                {
                    //Ascending guid, so which damper is the prototype does not depend on enumeration order.
                    displaySystemDampers.Sort((x, y) => x.Guid.CompareTo(y.Guid));
                    displaySystemDamper_Prototype = displaySystemDampers[0];
                }

                bool changed = false;

                foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationLegIntents)
                {
                    if (!systemVentilationLegIntent.RequiresDutyCarrier)
                    {
                        continue;
                    }

                    if (!dictionary_SystemConnection.TryGetValue(systemVentilationLegIntent.Guid_SystemConnection, out ISystemConnection systemConnection))
                    {
                        //Another plant room's leg. Not this one's business.
                        continue;
                    }

                    if (displaySystemDamper_Prototype == null)
                    {
                        systemVentilationConversionContext.Refuse(string.Format(
                            "{0} leg {1} needs a damper to carry its {2} l/s and the template supplied none to copy, "
                            + "so there is no prototype to derive one from.",
                            systemVentilationLegIntent.ConnectionType,
                            systemVentilationLegIntent.Guid_SystemConnection,
                            systemVentilationLegIntent.DesignFlowRate_Lps));

                        continue;
                    }

                    if (Insert(systemPlantRoom, systemVentilationConversionContext, systemVentilationLegIntent, systemConnection, displaySystemDamper_Prototype, dictionary_AirSystem, dictionary_SystemSpace))
                    {
                        changed = true;
                        count++;
                    }
                }

                if (changed)
                {
                    //Everything read out of a SystemPlantRoom is a clone, so the mutated copy has to go
                    //back in. Add replaces by guid.
                    systemEnergyCentre.Add(systemPlantRoom);
                }
            }

            systemVentilationConversionContext.Note(string.Format(
                "{0} duty-carrying damper(s) materialised - one per extract and transfer leg. Supply duties "
                + "ride on the room's own zone and no damper was invented for them.",
                count));

            return systemVentilationConversionContext.Refusals.Count == 0;
        }

        private static bool Insert(
            SystemPlantRoom systemPlantRoom,
            SystemVentilationConversionContext systemVentilationConversionContext,
            SystemVentilationLegIntent systemVentilationLegIntent,
            ISystemConnection systemConnection,
            DisplaySystemDamper displaySystemDamper_Prototype,
            Dictionary<Guid, AirSystem> dictionary_AirSystem,
            Dictionary<Guid, SystemSpace> dictionary_SystemSpace)
        {
            //---------------------------------------------------------------------------------------------
            //The two ends of the leg, as PR1 stated them, with their connector indexes. Nothing here is
            //inferred from a component type or a name: the connection itself says which object sits on
            //which connector.
            //---------------------------------------------------------------------------------------------

            if (!TryGetEnds(
                systemPlantRoom,
                systemVentilationConversionContext,
                systemVentilationLegIntent,
                systemConnection,
                out ISystemComponent systemComponent_Upstream,
                out int index_Upstream,
                out ISystemComponent systemComponent_Downstream,
                out int index_Downstream))
            {
                return false;
            }

            if (!dictionary_AirSystem.TryGetValue(systemVentilationLegIntent.Guid_AirSystem, out AirSystem airSystem))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1} names air system {2}, which the working copy does not hold.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    systemVentilationLegIntent.Guid_AirSystem));

                return false;
            }

            //---------------------------------------------------------------------------------------------
            //The carrier.
            //---------------------------------------------------------------------------------------------

            string key_Leg = Query.SystemVentilationGuidComponent(systemVentilationLegIntent.Guid_SystemConnection);

            Guid guid_Damper = Query.SystemVentilationGuid("DutyCarrier", key_Leg);

            if (!(displaySystemDamper_Prototype.Duplicate(guid_Damper) is DisplaySystemDamper displaySystemDamper))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "The prototype damper could not be duplicated for {0} leg {1}.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection));

                return false;
            }

            //The duty. DesignConditionSizedFlowValue with SizingType.Value is what makes TAS write
            //tpdSizedVariableValue rather than leave the template's ACH sizing rule in place - a plain
            //SizedFlowValue would have its Value written and its Type ignored, and the litres per second
            //would then be replaced by whatever the sizing rule computed.
            displaySystemDamper.DesignFlowRate = new DesignConditionSizedFlowValue(
                systemVentilationLegIntent.DesignFlowRate_Lps,
                0.0,
                SizingType.Value,
                0.0,
                0.0,
                displaySystemDamper_Prototype.DesignFlowRate is DesignConditionSizedFlowValue designConditionSizedFlowValue_Prototype
                    ? designConditionSizedFlowValue_Prototype.SizedFlowMethod
                    : SizedFlowMethod.PerMeterSquared,
                null);

            displaySystemDamper.DesignFlowType = FlowRateType.Value;

            //A minimum flow of nothing, stated rather than inherited: the prototype's minimum belongs to
            //the template's leg and would otherwise silently floor this one.
            displaySystemDamper.MinimumFlowRate = new SizedFlowValue(0.0, 0.0);
            displaySystemDamper.MinimumFlowType = FlowRateType.None;
            displaySystemDamper.MinimumFlowFraction = 0.0;

            displaySystemDamper.Name = Name(systemVentilationLegIntent, dictionary_SystemSpace);
            displaySystemDamper.Description = string.Format(
                "Part O {0} duty carrier for connection {1}.",
                systemVentilationLegIntent.ConnectionType.ToString().ToLowerInvariant(),
                systemVentilationLegIntent.Guid_SystemConnection);

            Place(displaySystemDamper, systemVentilationLegIntent, dictionary_SystemSpace);

            systemPlantRoom.Add(displaySystemDamper);
            systemPlantRoom.Connect(airSystem, displaySystemDamper);

            //---------------------------------------------------------------------------------------------
            //The leg, rebuilt through the carrier. The original goes first, so no run of the conversion
            //can ever see both the direct duct and the one through the damper.
            //---------------------------------------------------------------------------------------------

            systemPlantRoom.Remove(systemConnection);

            SystemConnection systemConnection_In = (SystemConnection)new SystemConnection(
                systemConnection.SystemType,
                systemComponent_Upstream,
                index_Upstream,
                displaySystemDamper,
                Index_Damper_In).Duplicate(Query.SystemVentilationGuid("CarrierConnectionIn", key_Leg));

            SystemConnection systemConnection_Out = (SystemConnection)new SystemConnection(
                systemConnection.SystemType,
                displaySystemDamper,
                Index_Damper_Out,
                systemComponent_Downstream,
                index_Downstream).Duplicate(Query.SystemVentilationGuid("CarrierConnectionOut", key_Leg));

            Attach(systemPlantRoom, airSystem, systemConnection_In, systemComponent_Upstream, displaySystemDamper);
            Attach(systemPlantRoom, airSystem, systemConnection_Out, displaySystemDamper, systemComponent_Downstream);

            systemVentilationConversionContext.SetDutyCarrier(systemVentilationLegIntent.Guid_SystemConnection, guid_Damper);

            return true;
        }

        /// <summary>A <c>SystemDamper</c>'s air inlet connector.</summary>
        private const int Index_Damper_In = 0;

        /// <summary>A <c>SystemDamper</c>'s air outlet connector.</summary>
        private const int Index_Damper_Out = 1;

        /// <summary>
        /// Adds a connection and wires it the way PR1 does - both endpoints, the pair, and the system -
        /// rather than through <c>Connect(a, b, out connection)</c>, which refuses an occupied connector
        /// and would therefore reject the many-to-one the shipped template already produces.
        /// </summary>
        private static void Attach(
            SystemPlantRoom systemPlantRoom,
            AirSystem airSystem,
            SystemConnection systemConnection,
            ISystemComponent systemComponent_1,
            ISystemComponent systemComponent_2)
        {
            systemPlantRoom.Add(systemConnection);

            systemPlantRoom.Connect(systemConnection, systemComponent_1);
            systemPlantRoom.Connect(systemConnection, systemComponent_2);
            systemPlantRoom.Connect(systemComponent_1, systemComponent_2);

            systemPlantRoom.Connect(airSystem, systemConnection);
        }

        /// <summary>
        /// The upstream and downstream ends of a leg, read off the connection itself.
        /// <para>
        /// A <c>SystemSpace</c>'s connector 1 is its extract outlet, so the space on connector 1 is
        /// upstream; connector 0 is its supply inlet, so the space on connector 0 is downstream. For an
        /// extract leg the downstream end is whatever the template attaches - a junction, usually - and
        /// for a transfer leg it is the receiving room.
        /// </para>
        /// </summary>
        private static bool TryGetEnds(
            SystemPlantRoom systemPlantRoom,
            SystemVentilationConversionContext systemVentilationConversionContext,
            SystemVentilationLegIntent systemVentilationLegIntent,
            ISystemConnection systemConnection,
            out ISystemComponent systemComponent_Upstream,
            out int index_Upstream,
            out ISystemComponent systemComponent_Downstream,
            out int index_Downstream)
        {
            systemComponent_Upstream = null;
            index_Upstream = -1;
            systemComponent_Downstream = null;
            index_Downstream = -1;

            List<ObjectReference> objectReferences = systemConnection.ObjectReferences;

            if (objectReferences == null || objectReferences.Count != 2)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1} names {2} endpoint(s); a leg a damper can be put into has exactly two.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    objectReferences == null ? 0 : objectReferences.Count));

                return false;
            }

            List<Tuple<ISystemComponent, int>> tuples = new List<Tuple<ISystemComponent, int>>();

            foreach (ObjectReference objectReference in objectReferences)
            {
                ISystemComponent systemComponent = systemPlantRoom.GetSystemComponent<ISystemComponent>(objectReference);

                if (systemComponent == null || !systemConnection.TryGetIndex(objectReference, out int index))
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "{0} leg {1} names an endpoint the working copy cannot resolve: {2}.",
                        systemVentilationLegIntent.ConnectionType,
                        systemVentilationLegIntent.Guid_SystemConnection,
                        objectReference));

                    return false;
                }

                tuples.Add(new Tuple<ISystemComponent, int>(systemComponent, index));
            }

            //A room states its own direction: its connector 1 is its extract outlet, so it is upstream;
            //its connector 0 is its supply inlet, so it is downstream.
            foreach (Tuple<ISystemComponent, int> tuple in tuples)
            {
                if (!(tuple.Item1 is SystemSpace))
                {
                    continue;
                }

                if (tuple.Item2 == 1)
                {
                    systemComponent_Upstream = tuple.Item1;
                    index_Upstream = tuple.Item2;
                }
                else
                {
                    systemComponent_Downstream = tuple.Item1;
                    index_Downstream = tuple.Item2;
                }
            }

            //Whatever is left over is the unit-side end, and it takes the slot the room did not.
            foreach (Tuple<ISystemComponent, int> tuple in tuples)
            {
                if (tuple.Item1 is SystemSpace)
                {
                    continue;
                }

                if (systemComponent_Upstream == null)
                {
                    systemComponent_Upstream = tuple.Item1;
                    index_Upstream = tuple.Item2;
                }
                else if (systemComponent_Downstream == null)
                {
                    systemComponent_Downstream = tuple.Item1;
                    index_Downstream = tuple.Item2;
                }
            }

            if (systemComponent_Upstream == null || systemComponent_Downstream == null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1} does not state both ends, so a damper cannot be put into it.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection));

                return false;
            }

            return true;
        }

        /// <summary>
        /// A display name for the carrier. Deliberately derived from the room the air leaves, which means
        /// two rooms with the same name produce two dampers with the same name - and nothing anywhere
        /// reads one, which is the point.
        /// </summary>
        private static string Name(SystemVentilationLegIntent systemVentilationLegIntent, Dictionary<Guid, SystemSpace> dictionary_SystemSpace)
        {
            string name = dictionary_SystemSpace.TryGetValue(systemVentilationLegIntent.Guid_SystemSpace_From, out SystemSpace systemSpace) ? systemSpace?.Name : null;

            return string.Format(
                "{0} Damper{1}",
                systemVentilationLegIntent.ConnectionType,
                string.IsNullOrWhiteSpace(name) ? string.Empty : string.Concat(" ", name));
        }

        /// <summary>
        /// Puts the carrier beside the room it serves, so the TAS diagram remains readable. Position is
        /// presentation only - nothing in the conversion, the reconciliation or the results reads it.
        /// </summary>
        private static void Place(
            DisplaySystemDamper displaySystemDamper,
            SystemVentilationLegIntent systemVentilationLegIntent,
            Dictionary<Guid, SystemSpace> dictionary_SystemSpace)
        {
            Point2D point2D_Damper = displaySystemDamper.SystemGeometry?.CoordinateSystem2D?.Origin;
            if (point2D_Damper == null)
            {
                return;
            }

            if (!dictionary_SystemSpace.TryGetValue(systemVentilationLegIntent.Guid_SystemSpace_From, out SystemSpace systemSpace)
                || !(systemSpace is DisplaySystemSpace displaySystemSpace))
            {
                return;
            }

            Point2D point2D_Space = displaySystemSpace.SystemGeometry?.CoordinateSystem2D?.Origin;
            if (point2D_Space == null)
            {
                return;
            }

            double offset = systemVentilationLegIntent.ConnectionType == SystemVentilationConnectionType.Transfer ? -2.0 : 2.0;

            displaySystemDamper.Move(new Vector2D(
                point2D_Space.X + offset - point2D_Damper.X,
                point2D_Space.Y + offset - point2D_Damper.Y));
        }
    }
}
