// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// Draws one explicit air system readably: every room on its own row, its transfer and extract
        /// dampers beside it, laid out by <see cref="Query.VentilationLayout"/>.
        /// <para>
        /// <b>Presentation only, and measured to be.</b> Only <c>SetPosition</c> and <c>SetDirection</c>
        /// are called. On licensed TAS, moving every one of an accepted document's 42 components to new
        /// positions and directions left all 9 zones' 8760 hourly ZoneTemperature values unchanged - a
        /// maximum difference of exactly 0 K - with both systems still answering "Done". No duct, duty,
        /// guid, schedule or group is touched, and no <c>ComponentGroup</c> is involved.
        /// </para>
        /// <para>
        /// It runs <b>before</b> the ducts are created because a duct's bend nodes can only be given at
        /// creation (<c>IDuct.AddNode</c>; TAS exposes no way to remove one), and
        /// <see cref="Create.Ducts"/> routes each duct from the positions set here. A failure to place
        /// something is noted and never refused: a drawing cannot make a design wrong.
        /// </para>
        /// </summary>
        public static void LayOutVentilationSystem(
            SystemVentilationConversionContext systemVentilationConversionContext,
            Guid guid_AirSystem,
            Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponent)
        {
            if (systemVentilationConversionContext == null || dictionary_SystemComponent == null)
            {
                return;
            }

            List<SystemVentilationRoomIntent> roomIntents = systemVentilationConversionContext.RoomIntents.FindAll(x => x.Guid_AirSystem == guid_AirSystem);
            List<SystemVentilationLegIntent> legIntents = systemVentilationConversionContext.LegIntents_AirSystem(guid_AirSystem);

            HashSet<Guid> guids_LaidOut = new HashSet<Guid>();
            foreach (SystemVentilationRoomIntent roomIntent in roomIntents)
            {
                guids_LaidOut.Add(roomIntent.Guid_SystemSpace);
            }

            foreach (SystemVentilationLegIntent legIntent in legIntents)
            {
                if (legIntent.RequiresDutyCarrier)
                {
                    guids_LaidOut.Add(legIntent.Guid_DutyCarrier);
                }
            }

            //The trunk is everything else this system holds - fans, the supply damper, the template's own
            //junctions - and it stays exactly where the template draws it.
            List<VentilationLayoutRectangle> trunk = new List<VentilationLayoutRectangle>();
            foreach (KeyValuePair<Guid, global::TPD.ISystemComponent> keyValuePair in dictionary_SystemComponent)
            {
                if (!guids_LaidOut.Contains(keyValuePair.Key))
                {
                    trunk.Add(NativeRectangle(keyValuePair.Value));
                }
            }

            VentilationLayoutRectangle zone = FirstNativeRectangle(roomIntents.ConvertAll(x => x.Guid_SystemSpace), dictionary_SystemComponent);
            VentilationLayoutRectangle damper = FirstNativeRectangle(legIntents.FindAll(x => x.RequiresDutyCarrier).ConvertAll(x => x.Guid_DutyCarrier), dictionary_SystemComponent);

            VentilationLayout ventilationLayout = Query.VentilationLayout(
                roomIntents,
                legIntents,
                VentilationLayoutRectangle.Union(trunk),
                zone?.Width ?? 60,
                zone?.Height ?? 60,
                damper?.Width ?? 40,
                damper?.Height ?? 40);

            int count = 0;
            List<string> failures = new List<string>();

            foreach (SystemVentilationRoomIntent roomIntent in roomIntents)
            {
                count += Place(dictionary_SystemComponent, roomIntent.Guid_SystemSpace, ventilationLayout.Room(roomIntent.Guid_SystemSpace), failures) ? 1 : 0;
            }

            foreach (SystemVentilationLegIntent legIntent in legIntents)
            {
                if (legIntent.RequiresDutyCarrier)
                {
                    count += Place(dictionary_SystemComponent, legIntent.Guid_DutyCarrier, ventilationLayout.Leg(legIntent.Guid_SystemConnection), failures) ? 1 : 0;
                }
            }

            systemVentilationConversionContext.Note(string.Format(
                "Air system {0}: {1} room(s) and duty-carrying damper(s) drawn one room per row, transfer dampers beneath "
                + "each room and extract dampers beside it; the trunk is left where the template draws it. Presentation "
                + "only - no duct, duty, guid or schedule is changed.{2}",
                guid_AirSystem,
                count,
                failures.Count == 0 ? string.Empty : " Not placed: " + string.Join("; ", failures) + "."));
        }

        private static bool Place(
            Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponent,
            Guid guid,
            VentilationLayoutRectangle ventilationLayoutRectangle,
            List<string> failures)
        {
            if (ventilationLayoutRectangle == null || !dictionary_SystemComponent.TryGetValue(guid, out global::TPD.ISystemComponent systemComponent) || systemComponent == null)
            {
                return false;
            }

            try
            {
                ((dynamic)systemComponent).SetPosition(ventilationLayoutRectangle.X, ventilationLayoutRectangle.Y);
                ((dynamic)systemComponent).SetDirection(global::TPD.tpdDirection.tpdLeftRight);
                return true;
            }
            catch (Exception exception)
            {
                failures.Add(string.Format("{0} ({1})", guid, exception.Message));
                return false;
            }
        }

        private static VentilationLayoutRectangle FirstNativeRectangle(
            List<Guid> guids,
            Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponent)
        {
            foreach (Guid guid in guids)
            {
                if (dictionary_SystemComponent.TryGetValue(guid, out global::TPD.ISystemComponent systemComponent))
                {
                    VentilationLayoutRectangle result = NativeRectangle(systemComponent);
                    if (result != null && result.Width > 0 && result.Height > 0)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// A component's drawn box, read late-bound: <c>GetPosition()</c> is the top-left corner and
        /// <c>GetBaseWidth()</c>/<c>GetBaseHeight()</c> its size. Null when TAS will not say.
        /// </summary>
        internal static VentilationLayoutRectangle NativeRectangle(object systemComponent)
        {
            if (systemComponent == null)
            {
                return null;
            }

            try
            {
                dynamic position = ((dynamic)systemComponent).GetPosition();
                return new VentilationLayoutRectangle(
                    (int)position.x,
                    (int)position.y,
                    (int)((dynamic)systemComponent).GetBaseWidth(),
                    (int)((dynamic)systemComponent).GetBaseHeight());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Whether TAS draws the component with its inlet on the left - every direction but right-to-left.</summary>
        internal static bool NativeLeftToRight(object systemComponent)
        {
            try
            {
                return (int)((dynamic)systemComponent).GetDirection() != (int)global::TPD.tpdDirection.tpdRightLeft;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>Draws a branch junction beside the component it serves, facing the same way.</summary>
        internal static void PlaceVentilationJunction(object junction, object systemComponent, bool outlet)
        {
            VentilationLayoutRectangle component = NativeRectangle(systemComponent);
            VentilationLayoutRectangle size = NativeRectangle(junction);
            if (component == null || size == null)
            {
                return;
            }

            bool leftToRight = NativeLeftToRight(systemComponent);

            VentilationLayoutRectangle ventilationLayoutRectangle = Query.VentilationJunctionRectangle(component, leftToRight, outlet, size.Width, size.Height);

            try
            {
                ((dynamic)junction).SetPosition(ventilationLayoutRectangle.X, ventilationLayoutRectangle.Y);
                ((dynamic)junction).SetDirection(leftToRight ? global::TPD.tpdDirection.tpdLeftRight : global::TPD.tpdDirection.tpdRightLeft);
            }
            catch
            {
            }
        }

        /// <summary>The bend nodes for a duct between two already-drawn components; see <see cref="Query.VentilationDuctRoute"/>.</summary>
        internal static List<int[]> VentilationDuctNodes(object upstream, object downstream, bool downstreamIsJunction)
        {
            return Query.VentilationDuctRoute(
                NativeRectangle(upstream),
                NativeLeftToRight(upstream),
                NativeRectangle(downstream),
                NativeLeftToRight(downstream),
                downstreamIsJunction);
        }
    }
}
