// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// What the thermostat bridge measured for one room: how much of the achieved <c>ZoneTemperature</c> series
    /// reached the copied building's heating and cooling thermostats, and how closely the copied building then
    /// held its air at it.
    /// <para>
    /// Every count here is <b>read back from TAS</b>, not the number of values the bridge meant to write. A
    /// profile slot counts only when the value TAS returns for it is exactly the single-precision value
    /// written. Free of TAS COM types.
    /// </para>
    /// </summary>
    public class ThermostatBridgeRoom
    {
        internal ThermostatBridgeRoom(Guid guid_Space, string reference_Zone, int count_ZoneTemperature)
        {
            Guid_Space = guid_Space;
            Reference_Zone = reference_Zone;
            Count_ZoneTemperature = count_ZoneTemperature;
            MaxAchievedAirTemperatureDeviation = double.NaN;
            MeanAchievedAirTemperatureDeviation = double.NaN;
        }

        /// <summary>The analytical room.</summary>
        public Guid Guid_Space { get; }

        /// <summary>The TBD / TSD zone guid the room was bridged into.</summary>
        public string Reference_Zone { get; }

        /// <summary>How many achieved zone temperatures the Systems route supplied.</summary>
        public int Count_ZoneTemperature { get; }

        /// <summary>How many internal conditions of the zone had their thermostats written.</summary>
        public int Count_InternalConditions { get; internal set; }

        /// <summary>
        /// Hours whose heating (lower-limit, <c>ticLL</c>) thermostat value read back exactly as written - on
        /// every internal condition of the zone.
        /// </summary>
        public int Count_Heating { get; internal set; }

        /// <summary>
        /// Hours whose cooling (upper-limit, <c>ticUL</c>) thermostat value read back exactly as written - on
        /// every internal condition of the zone.
        /// </summary>
        public int Count_Cooling { get; internal set; }

        /// <summary>
        /// The largest difference between a Systems zone temperature and the thermostat value TAS reads back
        /// for the same hour, over both limits and every internal condition. Single-precision storage is the
        /// only thing that can make it non-zero.
        /// </summary>
        public double MaxTransferDelta { get; internal set; }

        /// <summary>How many resultant temperatures the second TSD answered for the zone.</summary>
        public int Count_ResultantTemperature { get; internal set; }

        /// <summary>How many of those are finite.</summary>
        public int Count_ResultantTemperature_Finite { get; internal set; }

        /// <summary>
        /// The largest difference between the copied building's simulated air (dry bulb) temperature and the
        /// imposed achieved zone temperature, over the period. NaN until the second TSD has been read.
        /// </summary>
        public double MaxAchievedAirTemperatureDeviation { get; internal set; }

        /// <summary>The mean of the same difference. NaN until the second TSD has been read.</summary>
        public double MeanAchievedAirTemperatureDeviation { get; internal set; }

        public override string ToString()
        {
            return string.Format(
                "Room {0} zone {1}: ZoneTemperature {2}, IC {3}, heating {4}, cooling {5}, max transfer delta {6:R}, "
                + "ResultantTemperature {7} ({8} finite), achieved air deviation max {9:0.0000} mean {10:0.0000} K",
                Guid_Space,
                Reference_Zone ?? "<none>",
                Count_ZoneTemperature,
                Count_InternalConditions,
                Count_Heating,
                Count_Cooling,
                MaxTransferDelta,
                Count_ResultantTemperature,
                Count_ResultantTemperature_Finite,
                MaxAchievedAirTemperatureDeviation,
                MeanAchievedAirTemperatureDeviation);
        }
    }
}
