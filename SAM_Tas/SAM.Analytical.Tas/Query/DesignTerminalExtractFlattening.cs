// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Which room extract movements this export must write as leaving the building <b>directly from the
        /// room</b>, and which air handling units must therefore not be given an exhaust.
        ///
        /// <para><b>Why the export differs from the model</b></para>
        /// <para>
        /// The SAM model states the physical MVHR airflow topology, and it is right:
        /// <c>Outside -&gt; unit</c>, <c>unit -&gt; supply rooms</c>, <c>extract rooms -&gt; unit</c>,
        /// <c>unit -&gt; Outside</c>. The extract air really does pass through the unit.
        /// </para>
        /// <para>
        /// TAS cannot hold that. <c>Modify.UpdateIZAMs</c> represents an air handling unit as a
        /// <b>thermal zone</b>, and a TAS thermal zone is a single well-mixed air node - it has no notion of
        /// a supply airstream and an extract airstream passing through the same box without meeting. So a
        /// unit zone given the outside intake and the whole extract duty at once mixes them, and the supply
        /// it then delivers leaves at the mixed temperature. That is a sensible heat exchanger nobody
        /// specified, and a licensed A/B measured it at about 50% effectiveness on the acceptance dwelling -
        /// 3.79 K on the unit's own zone in the annual mean, and up to 1.14 K in every supplied room. See
        /// <c>documentation/PartO-TAS-VALIDATION.md</c>.
        /// </para>
        /// <para>
        /// Flattening <c>room -&gt; unit</c> plus <c>unit -&gt; Outside</c> into <c>room -&gt; Outside</c>
        /// removes the meeting point and nothing else. Every room still loses exactly its design extract,
        /// the unit still draws and delivers exactly its design supply, and every node still conserves. The
        /// unit's supply is outside air, which is what a configuration stating no heat recovery means.
        /// </para>
        ///
        /// <para><b>The scope, and why it is this one</b></para>
        /// <para>
        /// A movement qualifies only where its source is a <see cref="Space"/> carrying design
        /// <see cref="VentilationTerminal"/>s and its destination is an <see cref="AirHandlingUnit"/>. That
        /// is not a heuristic and it reads no name: it is <b>the same authority</b>
        /// <c>Modify.AddAirMovementObjects</c> uses to choose its design-terminal branch, which is the only
        /// code in SAM that produces a movement INTO an air handling unit at all. The generic branch - the
        /// one every model without design terminals reaches - already writes each space's outward movement
        /// straight to outside and routes nothing into the unit, so no generic MEP export can match this and
        /// none is changed. Design terminals are realized only on the Approved Document O MVHR route.
        /// </para>
        /// <para>
        /// The legacy <c>Create.IZAM</c> / <c>Modify.UpdateIZAMsBySpaceParameter</c> route builds its
        /// movements from hand-authored space parameters and never reaches this method.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">The model being exported. <b>Not modified.</b></param>
        /// <param name="guids_AirHandlingUnit">
        /// The air handling units that lose their exhaust, by guid - exactly those at the receiving end of a
        /// flattened movement. Empty where nothing is flattened. An exhaust left beside a flattened extract
        /// would take the same air out of the building twice.
        /// </param>
        /// <returns>
        /// The extract movements to write as leaving from the room, by guid. <b>Empty, never null</b>, so a
        /// caller can use it unconditionally.
        /// </returns>
        public static HashSet<Guid> DesignTerminalExtractFlattening(this AdjacencyCluster adjacencyCluster, out HashSet<Guid> guids_AirHandlingUnit)
        {
            HashSet<Guid> result = new HashSet<Guid>();
            guids_AirHandlingUnit = new HashSet<Guid>();

            if (adjacencyCluster == null)
            {
                return result;
            }

            List<AirHandlingUnit> airHandlingUnits = adjacencyCluster.GetObjects<AirHandlingUnit>();
            if (airHandlingUnits == null || airHandlingUnits.Count == 0)
            {
                return result;
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (spaces == null)
            {
                return result;
            }

            foreach (Space space in spaces)
            {
                if (space == null)
                {
                    continue;
                }

                //The design-terminal branch's own gate, asked the same way it asks it. A space with no
                //design terminal never produced a movement into the unit, so it cannot contribute one here.
                List<VentilationTerminal> ventilationTerminals = adjacencyCluster.VentilationTerminals(space);
                if (ventilationTerminals == null || ventilationTerminals.Count == 0)
                {
                    continue;
                }

                ObjectReference objectReference_Space = new ObjectReference(space);

                foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(space) ?? new List<SpaceAirMovement>())
                {
                    if (spaceAirMovement == null || string.IsNullOrWhiteSpace(spaceAirMovement.To))
                    {
                        continue;
                    }

                    //Out of THIS room, not merely related to it: a supply movement is related to the room
                    //too, and it runs the other way.
                    if (objectReference_Space != Core.Convert.ComplexReference<ObjectReference>(spaceAirMovement.From))
                    {
                        continue;
                    }

                    ObjectReference objectReference_To = Core.Convert.ComplexReference<ObjectReference>(spaceAirMovement.To);

                    AirHandlingUnit airHandlingUnit = airHandlingUnits.Find(x => x != null && new ObjectReference(x) == objectReference_To);
                    if (airHandlingUnit == null)
                    {
                        continue;
                    }

                    result.Add(spaceAirMovement.Guid);
                    guids_AirHandlingUnit.Add(airHandlingUnit.Guid);
                }
            }

            return result;
        }
    }
}
