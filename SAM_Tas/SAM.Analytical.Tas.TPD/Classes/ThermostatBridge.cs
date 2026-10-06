// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// The whole record of one run of the temporary ResultantTemperature thermostat bridge: what it stood on,
    /// what it wrote, what TAS did with it, and the resultant temperature that came out.
    /// <para>
    /// <b>The route it records.</b>
    /// </para>
    /// <code>
    /// Systems ZoneTemperature  -&gt;  COPY of the route's own no-IZAM TBD
    ///                          -&gt;  heating AND cooling thermostat := achieved room-air temperature, hour by hour
    ///                          -&gt;  second TSD  -&gt;  ResultantTemperature
    /// </code>
    /// <para>
    /// <b>Lineage.</b> The source TBD is hashed before the bridge copies it and again at the end, and must be
    /// unchanged; the copy is hashed as copied and must equal the source; the bridge TBD and TSD are hashed as
    /// left. Together with the route's own <c>Path_TPD</c> and each room's guid that is the chain from the
    /// Systems result to every resultant temperature.
    /// </para>
    /// <para>
    /// <b>Refused means empty.</b> <see cref="ResultantTemperatureResults"/> is never null, but on any refusal
    /// it carries no series and no result path. The per-room measurements and the simulation evidence are kept
    /// on a refusal, because they are the diagnosis. Free of TAS COM types.
    /// </para>
    /// </summary>
    public class ThermostatBridge
    {
        /// <summary>
        /// How far the copied building's simulated air temperature may stand from the imposed achieved zone
        /// temperature, in any hour, before the bridge is refused as not having reconstructed the Systems
        /// state, in K.
        /// <para>
        /// <b>Set against licensed measurements, not guessed</b> (PR3 evidence): on the real Iteration 1a chain
        /// every bridged room held its air within <b>0.0010 K</b> of the imposed series in every hour. In the
        /// self-imposition control on a different model the room zones held within <b>0.22 K</b>, while a
        /// corridor zone with a partly radiant emitter stood up to 2.23 K off in its worst hour. 0.5 K keeps a
        /// factor of two over the room zones and stays inside the 1 K resolution of the TM59 criteria the
        /// resultant temperature feeds - so a zone whose thermostat genuinely could not hold the air is
        /// refused, and TAS's own control precision is not.
        /// </para>
        /// </summary>
        public const double DefaultAchievedAirTemperatureTolerance = 0.5;

        /// <summary>What <see cref="TPD.ResultantTemperatureResults.Method"/> says for a bridged result.</summary>
        public const string Method = "TAS Systems ZoneTemperature -> thermostat bridge on a copy of the no-IZAM TBD -> TSD ResultantTemperature";

        private readonly List<ThermostatBridgeRoom> rooms = new List<ThermostatBridgeRoom>();

        internal ThermostatBridge(ThermostatBridgePlan thermostatBridgePlan, IEnumerable<ThermostatBridgeRoom> rooms)
        {
            ThermostatBridgePlan = thermostatBridgePlan;

            if (rooms != null)
            {
                this.rooms.AddRange(rooms);
                this.rooms.Sort((x, y) => x.Guid_Space.CompareTo(y.Guid_Space));
            }
        }

        /// <summary>What the bridge set out to do, and on which source.</summary>
        public ThermostatBridgePlan ThermostatBridgePlan { get; }

        /// <summary>The route's Systems document, recorded for lineage.</summary>
        public string Path_TPD { get; internal set; }

        /// <summary>The no-IZAM TBD the bridge copied. Read, never written.</summary>
        public string Path_TBD_Source { get; internal set; }

        /// <summary>SHA-256 of the source TBD before the bridge touched anything.</summary>
        public string Hash_TBD_Source_Before { get; internal set; }

        /// <summary>SHA-256 of the source TBD after the bridge finished. Must equal the value before.</summary>
        public string Hash_TBD_Source_After { get; internal set; }

        /// <summary>The source's paired TSD, recorded for lineage.</summary>
        public string Path_TSD_Source { get; internal set; }

        /// <summary>SHA-256 of the source TSD before the bridge ran.</summary>
        public string Hash_TSD_Source_Before { get; internal set; }

        /// <summary>SHA-256 of the source TSD after the bridge finished. Must equal the value before.</summary>
        public string Hash_TSD_Source_After { get; internal set; }

        /// <summary>The copy the bridge wrote thermostats into and simulated.</summary>
        public string Path_TBD { get; internal set; }

        /// <summary>SHA-256 of the copy the moment it was made. Must equal the source's.</summary>
        public string Hash_TBD_Copied { get; internal set; }

        /// <summary>SHA-256 of the copy as the bridge left it - thermostats written, simulated, saved.</summary>
        public string Hash_TBD { get; internal set; }

        /// <summary>The second TSD, and the only source of the resultant temperature.</summary>
        public string Path_TSD { get; internal set; }

        /// <summary>SHA-256 of the second TSD.</summary>
        public string Hash_TSD { get; internal set; }

        /// <summary>What the second simulation left behind.</summary>
        public SimulationEvidence SimulationEvidence { get; internal set; }

        /// <summary>The largest achieved-air deviation the bridge accepts. See <see cref="DefaultAchievedAirTemperatureTolerance"/>.</summary>
        public double AchievedAirTemperatureTolerance { get; internal set; }

        /// <summary>What was measured per room, ordered by <c>Space.Guid</c>.</summary>
        public List<ThermostatBridgeRoom> Rooms
        {
            get { return new List<ThermostatBridgeRoom>(rooms); }
        }

        /// <summary>The answer. Never null; empty and refused unless every stage passed.</summary>
        public ResultantTemperatureResults ResultantTemperatureResults { get; internal set; }

        /// <summary>Whether the bridge produced a complete resultant temperature for every room it planned.</summary>
        public bool IsComplete
        {
            get { return ResultantTemperatureResults != null && ResultantTemperatureResults.IsComplete; }
        }

        /// <summary>Every reason the bridge is not usable.</summary>
        public List<string> Refusals
        {
            get { return ResultantTemperatureResults == null ? new List<string>() : ResultantTemperatureResults.Refusals; }
        }

        public override string ToString()
        {
            return IsComplete
                ? string.Format("Thermostat bridge: {0} room(s), {1}", rooms.Count, Path_TSD)
                : string.Format("Thermostat bridge REFUSED ({0} reason(s)).", Refusals.Count);
        }
    }
}
