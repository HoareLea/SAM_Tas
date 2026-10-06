// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// An axis-aligned box in TAS Systems schematic coordinates: <c>X</c>/<c>Y</c> is the top-left corner
    /// TAS reports from <c>GetPosition()</c> and accepts in <c>SetPosition(x, y)</c>, and y grows downward.
    /// </summary>
    public sealed class VentilationLayoutRectangle
    {
        public VentilationLayoutRectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public int Right => X + Width;

        public int Bottom => Y + Height;

        public int CentreY => Y + Height / 2;

        public bool Overlaps(VentilationLayoutRectangle ventilationLayoutRectangle)
        {
            return ventilationLayoutRectangle != null
                && X < ventilationLayoutRectangle.Right && ventilationLayoutRectangle.X < Right
                && Y < ventilationLayoutRectangle.Bottom && ventilationLayoutRectangle.Y < Bottom;
        }

        public static VentilationLayoutRectangle Union(IEnumerable<VentilationLayoutRectangle> ventilationLayoutRectangles)
        {
            VentilationLayoutRectangle result = null;

            foreach (VentilationLayoutRectangle ventilationLayoutRectangle in ventilationLayoutRectangles ?? new VentilationLayoutRectangle[0])
            {
                if (ventilationLayoutRectangle == null)
                {
                    continue;
                }

                if (result == null)
                {
                    result = ventilationLayoutRectangle;
                    continue;
                }

                int x = global::System.Math.Min(result.X, ventilationLayoutRectangle.X);
                int y = global::System.Math.Min(result.Y, ventilationLayoutRectangle.Y);

                result = new VentilationLayoutRectangle(
                    x,
                    y,
                    global::System.Math.Max(result.Right, ventilationLayoutRectangle.Right) - x,
                    global::System.Math.Max(result.Bottom, ventilationLayoutRectangle.Bottom) - y);
            }

            return result;
        }
    }

    /// <summary>
    /// Where the explicit ventilation route draws one air system's rooms and duty-carrying dampers.
    /// <b>Presentation only</b>: nothing in the conversion, the reconciliation, the simulation or the
    /// results reads a position - measured on licensed TAS, moving every component of an accepted
    /// document leaves every zone's hourly ZoneTemperature unchanged.
    /// </summary>
    public sealed class VentilationLayout
    {
        private readonly Dictionary<Guid, VentilationLayoutRectangle> rooms = new Dictionary<Guid, VentilationLayoutRectangle>();
        private readonly Dictionary<Guid, VentilationLayoutRectangle> legs = new Dictionary<Guid, VentilationLayoutRectangle>();

        internal void AddRoom(Guid guid_SystemSpace, VentilationLayoutRectangle ventilationLayoutRectangle)
        {
            rooms[guid_SystemSpace] = ventilationLayoutRectangle;
        }

        internal void AddLeg(Guid guid_SystemConnection, VentilationLayoutRectangle ventilationLayoutRectangle)
        {
            legs[guid_SystemConnection] = ventilationLayoutRectangle;
        }

        /// <summary>The room's zone box, by PR1 system space guid; null when the room was not laid out.</summary>
        public VentilationLayoutRectangle Room(Guid guid_SystemSpace)
        {
            return rooms.TryGetValue(guid_SystemSpace, out VentilationLayoutRectangle result) ? result : null;
        }

        /// <summary>The leg's damper box, by PR1 connection guid; null for a supply leg, which has no damper.</summary>
        public VentilationLayoutRectangle Leg(Guid guid_SystemConnection)
        {
            return legs.TryGetValue(guid_SystemConnection, out VentilationLayoutRectangle result) ? result : null;
        }

        public IEnumerable<VentilationLayoutRectangle> Rectangles
        {
            get
            {
                foreach (VentilationLayoutRectangle ventilationLayoutRectangle in rooms.Values)
                {
                    yield return ventilationLayoutRectangle;
                }

                foreach (VentilationLayoutRectangle ventilationLayoutRectangle in legs.Values)
                {
                    yield return ventilationLayoutRectangle;
                }
            }
        }
    }

    public static partial class Query
    {
        //Gaps between the schematic's columns and rows, in TAS schematic units. A zone box is 60 and a
        //damper box 40 on licensed TAS; these leave a lane beside every column and between every row for
        //the duct routes of VentilationDuctRoute to run in.
        private const int VentilationLayoutTrunkGap = 160;
        private const int VentilationLayoutRowGap = 60;
        private const int VentilationLayoutStackGap = 10;
        private const int VentilationLayoutJunctionColumn = 100;
        private const int VentilationLayoutDamperColumnGap = 80;
        private const int VentilationLayoutExtractOnlyGap = 40;
        private const int VentilationLayoutLead = 20;

        /// <summary>
        /// Lays out one air system's rooms and duty-carrying dampers, <b>deterministically and by
        /// identity</b>: the same design produces the same drawing whatever order its rooms or legs are
        /// supplied in, and no display name is read.
        /// <list type="bullet">
        /// <item><description>every room is its own row, below the template's trunk, in air-path order:
        /// supply-only, supply and extract, transfer-only, extract-only, then by system space
        /// guid;</description></item>
        /// <item><description>a room's transfer dampers stack beneath its row in one column, its extract
        /// dampers level with it in a column further right - so supply arrives from the left, extract
        /// leaves to the right and transfers sit between the rooms they join;</description></item>
        /// <item><description>a room the air path ENDS at - extract with nothing passed on - stands in its
        /// own column, past the transfer dampers and clear of the occupied rooms, so the extract and
        /// return side reads separately and a transfer into it drops straight down. A room that extracts
        /// and still passes air on stays in the room column: it is in the middle of the path, not at the
        /// end of it;</description></item>
        /// <item><description>each row is tall enough for its own stack plus a free lane beneath it, which
        /// is where <see cref="VentilationDuctRoute"/> runs the ducts that travel back
        /// leftward.</description></item>
        /// </list>
        /// The trunk - fans, the supply damper and the template's own junctions - is left exactly where
        /// the template draws it.
        /// </summary>
        public static VentilationLayout VentilationLayout(
            IEnumerable<SystemVentilationRoomIntent> roomIntents,
            IEnumerable<SystemVentilationLegIntent> legIntents,
            VentilationLayoutRectangle trunk,
            int zoneWidth,
            int zoneHeight,
            int damperWidth,
            int damperHeight)
        {
            VentilationLayout result = new VentilationLayout();

            List<SystemVentilationRoomIntent> rooms = new List<SystemVentilationRoomIntent>();
            foreach (SystemVentilationRoomIntent roomIntent in roomIntents ?? new SystemVentilationRoomIntent[0])
            {
                if (roomIntent != null)
                {
                    rooms.Add(roomIntent);
                }
            }

            Dictionary<Guid, List<SystemVentilationLegIntent>> transfers = new Dictionary<Guid, List<SystemVentilationLegIntent>>();
            Dictionary<Guid, List<SystemVentilationLegIntent>> extracts = new Dictionary<Guid, List<SystemVentilationLegIntent>>();

            foreach (SystemVentilationLegIntent legIntent in legIntents ?? new SystemVentilationLegIntent[0])
            {
                if (legIntent == null || !legIntent.RequiresDutyCarrier)
                {
                    continue;
                }

                Dictionary<Guid, List<SystemVentilationLegIntent>> dictionary = legIntent.ConnectionType == SystemVentilationConnectionType.Transfer ? transfers : extracts;
                if (!dictionary.TryGetValue(legIntent.Guid_SystemSpace_From, out List<SystemVentilationLegIntent> list))
                {
                    list = new List<SystemVentilationLegIntent>();
                    dictionary[legIntent.Guid_SystemSpace_From] = list;
                }

                list.Add(legIntent);
            }

            //Built BEFORE the sort, because whether a room passes air ON decides which column it stands
            //in - and therefore its row order too.
            rooms.Sort((room_1, room_2) =>
            {
                int compare = VentilationLayoutOrder(room_1, transfers.ContainsKey(room_1.Guid_SystemSpace))
                    .CompareTo(VentilationLayoutOrder(room_2, transfers.ContainsKey(room_2.Guid_SystemSpace)));

                return compare != 0 ? compare : room_1.Guid_SystemSpace.CompareTo(room_2.Guid_SystemSpace);
            });

            //Columns, left to right: the occupied rooms, the transfer dampers beneath them, the extract-only
            //rooms the transfers feed, then the extract dampers - so the extract and return side stands
            //clear of the occupied rooms and a transfer into an extract-only room drops straight down.
            int x_Zone = (trunk?.Right ?? 0) + VentilationLayoutTrunkGap;
            int x_Transfer = x_Zone + zoneWidth + VentilationLayoutJunctionColumn;
            int x_ExtractOnly = x_Transfer + damperWidth + VentilationLayoutExtractOnlyGap;
            int x_Extract = x_ExtractOnly + zoneWidth + VentilationLayoutDamperColumnGap;
            int y = (trunk?.Bottom ?? 0) + VentilationLayoutTrunkGap - VentilationLayoutRowGap;

            foreach (SystemVentilationRoomIntent room in rooms)
            {
                int x_Room = VentilationLayoutOrder(room, transfers.ContainsKey(room.Guid_SystemSpace)) == 3 ? x_ExtractOnly : x_Zone;

                result.AddRoom(room.Guid_SystemSpace, new VentilationLayoutRectangle(x_Room, y, zoneWidth, zoneHeight));

                int bottom = y + zoneHeight;

                if (transfers.TryGetValue(room.Guid_SystemSpace, out List<SystemVentilationLegIntent> list_Transfer))
                {
                    list_Transfer.Sort((a, b) => a.Guid_SystemConnection.CompareTo(b.Guid_SystemConnection));

                    int y_Damper = y + zoneHeight + VentilationLayoutStackGap;
                    foreach (SystemVentilationLegIntent legIntent in list_Transfer)
                    {
                        result.AddLeg(legIntent.Guid_SystemConnection, new VentilationLayoutRectangle(x_Transfer, y_Damper, damperWidth, damperHeight));
                        bottom = global::System.Math.Max(bottom, y_Damper + damperHeight);
                        y_Damper += damperHeight + VentilationLayoutStackGap;
                    }
                }

                if (extracts.TryGetValue(room.Guid_SystemSpace, out List<SystemVentilationLegIntent> list_Extract))
                {
                    list_Extract.Sort((a, b) => a.Guid_SystemConnection.CompareTo(b.Guid_SystemConnection));

                    int y_Damper = y + (zoneHeight - damperHeight) / 2;
                    foreach (SystemVentilationLegIntent legIntent in list_Extract)
                    {
                        result.AddLeg(legIntent.Guid_SystemConnection, new VentilationLayoutRectangle(x_Extract, y_Damper, damperWidth, damperHeight));
                        bottom = global::System.Math.Max(bottom, y_Damper + damperHeight);
                        y_Damper += damperHeight + VentilationLayoutStackGap;
                    }
                }

                y = bottom + VentilationLayoutRowGap;
            }

            return result;
        }

        private static int VentilationLayoutOrder(SystemVentilationRoomIntent roomIntent, bool sendsTransfer)
        {
            bool supply = roomIntent.DesignFlowRate_Supply_Lps.HasValue;
            bool extract = roomIntent.DesignFlowRate_Extract_Lps.HasValue;

            //Order 3 - the extract-only column - is for a room the air path ENDS at. A room that extracts
            //and also passes air on to another room is not that, whatever its duties say: it is a room in
            //the middle of the path, and it belongs in the room column with its transfer damper to its
            //right, so the duct out of it runs forward.
            //
            //Real dwellings are full of them - a kitchen that extracts 55 l/s and still transfers 8 l/s on
            //to an ensuite - and standing one in the extract-only column, PAST the transfer column, forces
            //its own outgoing duct to double back through the room's own box.
            if (extract && !supply && sendsTransfer)
            {
                return 2;
            }

            return supply && !extract ? 0 : supply ? 1 : !extract ? 2 : 3;
        }

        /// <summary>
        /// The bend nodes of one explicit-route duct, from the upstream component's outlet to the
        /// downstream component's inlet, orthogonal and clear of every laid-out box.
        /// <para>
        /// A component drawn left-to-right has its inlet on its left and its outlet on its right; one
        /// drawn right-to-left (the template's return fan) the reverse. A duct that runs <b>forward</b> -
        /// its inlet lead lies beyond its outlet lead - turns once, just before the downstream
        /// component. One that runs <b>back</b> leaves by its outlet lead, climbs to the lane just above
        /// the downstream component, crosses in that lane and drops into the inlet: rows are laid out
        /// with that lane free by <see cref="VentilationLayout"/>. A junction's lane is taken above the
        /// row it sits in rather than above the junction itself.
        /// </para>
        /// </summary>
        /// <returns>The nodes as <c>{x, y}</c> pairs, in order; empty when the duct is straight.</returns>
        public static List<int[]> VentilationDuctRoute(
            VentilationLayoutRectangle upstream,
            bool upstreamLeftToRight,
            VentilationLayoutRectangle downstream,
            bool downstreamLeftToRight,
            bool downstreamIsJunction)
        {
            List<int[]> result = new List<int[]>();

            if (upstream == null || downstream == null)
            {
                return result;
            }

            int y_Out = upstream.CentreY;
            int y_In = downstream.CentreY;
            int x_Out = upstreamLeftToRight ? upstream.Right + VentilationLayoutLead : upstream.X - VentilationLayoutLead;
            int x_In = downstreamLeftToRight ? downstream.X - VentilationLayoutLead : downstream.Right + VentilationLayoutLead;

            bool forward = upstreamLeftToRight ? x_In >= x_Out : x_In <= x_Out;

            if (forward)
            {
                if (y_Out != y_In)
                {
                    result.Add(new[] { x_In, y_Out });
                    result.Add(new[] { x_In, y_In });
                }

                return result;
            }

            int y_Lane = downstream.Y - VentilationLayoutLead - (downstreamIsJunction ? VentilationLayoutLead : 0);

            result.Add(new[] { x_Out, y_Out });
            result.Add(new[] { x_Out, y_Lane });
            result.Add(new[] { x_In, y_Lane });
            result.Add(new[] { x_In, y_In });

            return result;
        }

        /// <summary>
        /// Where a branch junction is drawn: beside the component it serves, on the side the shared
        /// connector is on, level with it - an outlet junction just past the component's outlet, an inlet
        /// junction just before its inlet.
        /// </summary>
        public static VentilationLayoutRectangle VentilationJunctionRectangle(
            VentilationLayoutRectangle component,
            bool componentLeftToRight,
            bool outlet,
            int junctionWidth,
            int junctionHeight)
        {
            if (component == null)
            {
                return null;
            }

            bool right = componentLeftToRight == outlet;
            int gap = outlet ? VentilationLayoutLead : VentilationLayoutLead + VentilationLayoutStackGap;

            int x = right ? component.Right + gap : component.X - gap - junctionWidth;

            return new VentilationLayoutRectangle(x, component.CentreY - junctionHeight / 2, junctionWidth, junctionHeight);
        }
    }
}
