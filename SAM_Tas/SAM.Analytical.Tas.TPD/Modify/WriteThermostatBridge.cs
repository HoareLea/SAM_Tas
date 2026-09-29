// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// Writes every planned room's achieved zone temperature into both thermostat limits of its own zone in
        /// an <b>already-copied</b> TBD, and reads every value back.
        /// <para>
        /// <b>Zones are found by guid, once.</b> The building's zones are walked a single time into a guid
        /// index, and each room is then one probe. Enumeration order, display name and position play no part;
        /// two zones answering the same guid are refused, because either could be the room.
        /// </para>
        /// <para>
        /// Nothing is saved or simulated here. The caller must not simulate when any refusal was added.
        /// </para>
        /// </summary>
        /// <param name="building">The copy's building. Never the source's.</param>
        /// <param name="thermostatBridgePlan">A valid plan.</param>
        /// <param name="refusals">Receives every reason the copy is not a faithful bridge.</param>
        /// <returns>One measurement per planned room that resolved to a zone, ordered by <c>Space.Guid</c>.</returns>
        public static List<ThermostatBridgeRoom> WriteThermostatBridge(this TBD.Building building, ThermostatBridgePlan thermostatBridgePlan, List<string> refusals)
        {
            List<ThermostatBridgeRoom> result = new List<ThermostatBridgeRoom>();

            if (building == null || thermostatBridgePlan == null || !thermostatBridgePlan.IsValid)
            {
                refusals?.Add("No building or no valid plan was given to the thermostat bridge writer.");
                return result;
            }

            Dictionary<string, TBD.zone> zone_By_Key = new Dictionary<string, TBD.zone>(StringComparer.Ordinal);
            HashSet<string> keys_Duplicate = new HashSet<string>(StringComparer.Ordinal);

            int index = 0;
            TBD.zone zone;

            while ((zone = building.GetZone(index)) != null)
            {
                index++;

                string key = Query.ZoneReferenceKey(zone.GUID);
                if (key == null)
                {
                    continue;
                }

                if (zone_By_Key.ContainsKey(key))
                {
                    keys_Duplicate.Add(key);
                    continue;
                }

                zone_By_Key[key] = zone;
            }

            foreach (ThermostatBridgeTransfer thermostatBridgeTransfer in thermostatBridgePlan.Transfers)
            {
                if (keys_Duplicate.Contains(thermostatBridgeTransfer.Key))
                {
                    refusals.Add(string.Format(
                        "Room {0}: more than one zone in the TBD copy answers guid {1}, so which one is the room is ambiguous.",
                        thermostatBridgeTransfer.Guid_Space,
                        thermostatBridgeTransfer.Reference_Zone));

                    continue;
                }

                if (!zone_By_Key.TryGetValue(thermostatBridgeTransfer.Key, out zone))
                {
                    refusals.Add(string.Format(
                        "Room {0}: the TBD copy has no zone {1}, so there is no thermostat to bridge its temperature into.",
                        thermostatBridgeTransfer.Guid_Space,
                        thermostatBridgeTransfer.Reference_Zone));

                    continue;
                }

                result.Add(WriteThermostatBridge(zone, thermostatBridgeTransfer, refusals));
            }

            return result;
        }

        /// <summary>
        /// Writes one room's series into both thermostat limits of <b>every</b> internal condition the zone
        /// carries, so whichever of them TAS applies on a given calendar day holds the zone at the achieved
        /// temperature.
        /// <para>
        /// <b>Refused rather than written:</b>
        /// </para>
        /// <list type="bullet">
        /// <item><description>a zone with no internal condition, or one with no thermostat;</description></item>
        /// <item><description>an internal condition also assigned to another zone - writing this room's series
        /// into it would hand the other zone this room's temperature;</description></item>
        /// <item><description>a thermostat that senses radiant temperature or controls proportionally: it
        /// would not hold the <i>air</i> at the imposed value, which is the whole premise of the bridge. The
        /// bridge does not reconfigure the thermostat; it refuses.</description></item>
        /// </list>
        /// </summary>
        public static ThermostatBridgeRoom WriteThermostatBridge(this TBD.zone zone, ThermostatBridgeTransfer thermostatBridgeTransfer, List<string> refusals)
        {
            ThermostatBridgeRoom result = new ThermostatBridgeRoom(thermostatBridgeTransfer.Guid_Space, thermostatBridgeTransfer.Reference_Zone, thermostatBridgeTransfer.Count);

            int count_Heating = int.MaxValue;
            int count_Cooling = int.MaxValue;
            double maxTransferDelta = 0;

            int count_InternalCondition = 0;
            TBD.InternalCondition internalCondition;

            while ((internalCondition = zone.GetIC(count_InternalCondition)) != null)
            {
                count_InternalCondition++;

                string name = internalCondition.name;

                int index = 0;
                TBD.zone zone_Assigned;

                bool shared = false;

                while ((zone_Assigned = internalCondition.GetZone(index)) != null)
                {
                    index++;

                    if (!string.Equals(Query.ZoneReferenceKey(zone_Assigned.GUID), thermostatBridgeTransfer.Key, StringComparison.Ordinal))
                    {
                        shared = true;
                    }
                }

                if (shared)
                {
                    refusals.Add(string.Format(
                        "Room {0}: internal condition '{1}' of zone {2} is also assigned to another zone, so its thermostat cannot hold "
                        + "this room's temperature without imposing it on the other zone too.",
                        thermostatBridgeTransfer.Guid_Space,
                        name,
                        thermostatBridgeTransfer.Reference_Zone));

                    continue;
                }

                TBD.Thermostat thermostat = internalCondition.GetThermostat();

                if (thermostat == null)
                {
                    refusals.Add(string.Format("Room {0}: internal condition '{1}' has no thermostat.", thermostatBridgeTransfer.Guid_Space, name));
                    continue;
                }

                if (thermostat.radiantProportion != 0 || thermostat.proportionalControl != 0)
                {
                    refusals.Add(string.Format(
                        "Room {0}: the thermostat of internal condition '{1}' senses {2:R} radiant and controls {3}, so it would not "
                        + "hold the air at the imposed temperature.",
                        thermostatBridgeTransfer.Guid_Space,
                        name,
                        thermostat.radiantProportion,
                        thermostat.proportionalControl != 0 ? "proportionally" : "on/off"));

                    continue;
                }

                //Cooling is the upper limit, heating the lower. Both receive the SAME achieved series: the zone is
                //held at it, not merely bounded by it.
                string refusal_Cooling = WriteThermostatBridge(thermostat.GetProfile((int)TBD.Profiles.ticUL), thermostatBridgeTransfer, out int count_UpperLimit, out double delta_UpperLimit);
                string refusal_Heating = WriteThermostatBridge(thermostat.GetProfile((int)TBD.Profiles.ticLL), thermostatBridgeTransfer, out int count_LowerLimit, out double delta_LowerLimit);

                if (refusal_Cooling != null)
                {
                    refusals.Add(string.Format("Room {0}, internal condition '{1}', cooling thermostat: {2}", thermostatBridgeTransfer.Guid_Space, name, refusal_Cooling));
                }

                if (refusal_Heating != null)
                {
                    refusals.Add(string.Format("Room {0}, internal condition '{1}', heating thermostat: {2}", thermostatBridgeTransfer.Guid_Space, name, refusal_Heating));
                }

                count_Cooling = global::System.Math.Min(count_Cooling, count_UpperLimit);
                count_Heating = global::System.Math.Min(count_Heating, count_LowerLimit);
                maxTransferDelta = global::System.Math.Max(maxTransferDelta, global::System.Math.Max(delta_UpperLimit, delta_LowerLimit));
            }

            if (count_InternalCondition == 0)
            {
                refusals.Add(string.Format(
                    "Room {0}: zone {1} carries no internal condition, so it has no thermostat to bridge into.",
                    thermostatBridgeTransfer.Guid_Space,
                    thermostatBridgeTransfer.Reference_Zone));
            }

            result.Count_InternalConditions = count_InternalCondition;
            result.Count_Heating = count_InternalCondition == 0 ? 0 : count_Heating;
            result.Count_Cooling = count_InternalCondition == 0 ? 0 : count_Cooling;
            result.MaxTransferDelta = maxTransferDelta;

            return result;
        }

        /// <summary>
        /// Writes one thermostat profile as a yearly profile holding the room's series, then reads every slot
        /// back.
        /// <para>
        /// <b>1-based slots: measured, not assumed.</b> On licensed TAS the bulk <c>SetYearlyValues(float[])</c>
        /// silently <b>ignores element 0</b> and stores element <c>i</c> in slot <c>i</c>: a natural 0-based
        /// <c>float[8760]</c> shifts every hour by one and repeats the last value, and nothing reports it.
        /// <c>GetYearlyValues()</c> answers a <c>Single[*]</c> bounded 1..8760, confirming the base. Slot
        /// <c>h</c> therefore receives hour <c>h - 1</c>, and the licensed self-imposition control measured the
        /// simulated air temperature aligned at exactly that hour (PR3 evidence).
        /// </para>
        /// <para>
        /// <b>One bulk write and one bulk read (2026-09-29).</b> The profile used to be written and read back one
        /// slot at a time - 17,520 cross-process calls per profile, two profiles per internal condition - which
        /// dominated the bridge. It is now written as a 1-based <c>float[8761]</c> with element 0 unused (the
        /// shape <c>Modify.UpdateYearlyValues</c> has written since 2026-09-11) and read back with
        /// <c>GetYearlyValues()</c>; every one of the 8760 slots is still compared exactly. So that a bulk read
        /// that disagreed with the slot accessor about the base could not pass unnoticed, the first, a middle and
        /// the last slot are also read through <c>yearlyValues[h]</c> and must agree with it.
        /// </para>
        /// </summary>
        /// <param name="count_Matched">Slots that read back exactly the single-precision value written.</param>
        /// <param name="maxTransferDelta">Largest |read-back - Systems zone temperature| over the slots.</param>
        /// <returns>Why the profile is not a faithful copy of the series, or null.</returns>
        public static string WriteThermostatBridge(this TBD.profile profile, ThermostatBridgeTransfer thermostatBridgeTransfer, out int count_Matched, out double maxTransferDelta)
        {
            count_Matched = 0;
            maxTransferDelta = 0;

            if (profile == null)
            {
                return "the thermostat has no such profile.";
            }

            if (thermostatBridgeTransfer == null || thermostatBridgeTransfer.StartHour != ThermostatBridgePlan.StartHour || thermostatBridgeTransfer.Count != ThermostatBridgePlan.HoursPerYear)
            {
                return string.Format("the transfer does not carry exactly the {0} hours of the year.", ThermostatBridgePlan.HoursPerYear);
            }

            profile.type = TBD.ProfileTypes.ticYearlyProfile;
            profile.factor = 1;

            //Slot h = hour h - 1; element 0 is never read by TAS.
            float[] slots = new float[ThermostatBridgePlan.HoursPerYear + 1];
            for (int hour = 1; hour <= ThermostatBridgePlan.HoursPerYear; hour++)
            {
                slots[hour] = thermostatBridgeTransfer.Value(hour - 1);
            }

            using (TPDProfiler.Current?.Measure("Bridge: write one yearly profile"))
            {
                profile.SetYearlyValues(slots);
            }

            if (profile.type != TBD.ProfileTypes.ticYearlyProfile || profile.factor != 1)
            {
                return string.Format("TAS did not keep the profile as a yearly profile with factor 1 (type {0}, factor {1}).", profile.type, profile.factor);
            }

            IDisposable measure_ReadBack = TPDProfiler.Current?.Measure("Bridge: read back one yearly profile");

            if (!(profile.GetYearlyValues() is Array values) || values.Rank != 1 || values.GetLowerBound(0) != 1 || values.GetUpperBound(0) != ThermostatBridgePlan.HoursPerYear)
            {
                measure_ReadBack?.Dispose();
                return string.Format("TAS did not answer the profile's yearly values as slots 1..{0}.", ThermostatBridgePlan.HoursPerYear);
            }

            foreach (int hour in new[] { 1, ThermostatBridgePlan.HoursPerYear / 2, ThermostatBridgePlan.HoursPerYear })
            {
                if (!(values.GetValue(hour) is float bulk) || bulk != profile.yearlyValues[hour])
                {
                    measure_ReadBack?.Dispose();
                    return string.Format("the bulk read-back of slot {0} does not agree with the slot itself, so its base cannot be trusted.", hour);
                }
            }

            int hour_FirstMismatch = -1;

            for (int hour = 1; hour <= ThermostatBridgePlan.HoursPerYear; hour++)
            {
                float value = (float)values.GetValue(hour);

                if (value == thermostatBridgeTransfer.Value(hour - 1))
                {
                    count_Matched++;
                }
                else if (hour_FirstMismatch < 0)
                {
                    hour_FirstMismatch = hour - 1;
                }

                double delta = global::System.Math.Abs(value - thermostatBridgeTransfer.ZoneTemperature(hour - 1));
                if (double.IsNaN(delta) || delta > maxTransferDelta)
                {
                    maxTransferDelta = double.IsNaN(delta) ? double.PositiveInfinity : delta;
                }
            }

            measure_ReadBack?.Dispose();

            if (count_Matched != ThermostatBridgePlan.HoursPerYear)
            {
                return string.Format(
                    "{0} of {1} hour(s) read back as written; the first that did not is hour {2}.",
                    count_Matched,
                    ThermostatBridgePlan.HoursPerYear,
                    hour_FirstMismatch);
            }

            return null;
        }
    }
}
