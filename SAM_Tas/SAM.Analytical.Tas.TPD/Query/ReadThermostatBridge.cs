// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        /// <summary>
        /// Reads the thermostat bridge's second TSD: each planned room's <c>ResultantTemperature</c>, and the
        /// check that the copied building actually held the room's air at the imposed achieved temperature.
        /// <para>
        /// <b>By guid, once.</b> The TSD's zones are walked a single time into a <c>zoneGUID</c> index and each
        /// room is then one probe; two zones answering one guid are refused.
        /// </para>
        /// <para>
        /// <b>The achieved-air check is the bridge's own acceptance.</b> Imposing the Systems air temperature
        /// on both thermostat limits only reconstructs the Systems state if TAS then holds the air there. The
        /// simulated dry bulb is compared with the imposed series hour by hour, and a room whose largest
        /// deviation exceeds <paramref name="achievedAirTemperatureTolerance"/> is refused - its resultant
        /// temperature would belong to a building the Systems simulation did not have.
        /// </para>
        /// <para>
        /// Hour <c>k</c> of TSD's annual array is 0-based hour <c>k</c>, the hour written into thermostat slot
        /// <c>k + 1</c>: measured on licensed TAS, see the PR3 evidence.
        /// </para>
        /// </summary>
        /// <param name="simulationData">The second TSD's simulation data.</param>
        /// <param name="thermostatBridgePlan">The plan the copy was written from.</param>
        /// <param name="thermostatBridgeRooms">The writer's measurements, completed here by room guid.</param>
        /// <param name="achievedAirTemperatureTolerance">Largest accepted |dry bulb - imposed| in any hour, K.</param>
        /// <param name="refusals">Receives every reason the results are not the bridge's answer.</param>
        public static List<ResultantTemperatureResult> ReadThermostatBridge(
            this TSD.SimulationData simulationData,
            ThermostatBridgePlan thermostatBridgePlan,
            IEnumerable<ThermostatBridgeRoom> thermostatBridgeRooms,
            double achievedAirTemperatureTolerance,
            List<string> refusals)
        {
            if (simulationData == null)
            {
                refusals.Add("No second TSD was given to the thermostat bridge reader.");
                return new List<ResultantTemperatureResult>();
            }

            string refusal_Year = FullYearRefusal(simulationData.firstDay, simulationData.lastDay);
            if (refusal_Year != null)
            {
                refusals.Add(string.Concat("The second TSD ", refusal_Year));
                return new List<ResultantTemperatureResult>();
            }

            return ReadThermostatBridge(simulationData.GetBuildingData(), thermostatBridgePlan, thermostatBridgeRooms, achievedAirTemperatureTolerance, refusals);
        }

        /// <summary>
        /// Why a TSD's simulated day range is not the bridge's whole year, or null when it is.
        /// <para>
        /// <b>The day range is the only signal.</b> TSD pads every day it did not simulate with -1 and still answers
        /// 8760 hours - measured, SAM_Tas#72 follow-up record - so the length checks the reader applies cannot see a
        /// part-year file: without this a days-1-to-2 simulation reads as 48 real hours followed by 8712 hours of
        /// -1 degC. Part-year TSDs remain legitimate elsewhere; this is the bridge's own rule, because the bridge
        /// imposes and compares a whole year.
        /// </para>
        /// </summary>
        public static string FullYearRefusal(int firstDay, int lastDay)
        {
            if (firstDay == ThermostatBridgePlan.FirstDay && lastDay == ThermostatBridgePlan.LastDay)
            {
                return null;
            }

            return string.Format(
                "holds days {0}..{1}, not the full year {2}..{3} the thermostat bridge imposes and compares, so its results are refused rather than read (TSD pads the days it did not simulate with -1).",
                firstDay,
                lastDay,
                ThermostatBridgePlan.FirstDay,
                ThermostatBridgePlan.LastDay);
        }

        /// <summary>
        /// Reads the thermostat bridge's second TSD from its building data - with NO check of the simulated day
        /// range, which building data does not carry. The bridge itself calls the
        /// <see cref="ReadThermostatBridge(TSD.SimulationData, ThermostatBridgePlan, IEnumerable{ThermostatBridgeRoom}, double, List{string})"/>
        /// overload, which refuses a part-year file first.
        /// </summary>
        public static List<ResultantTemperatureResult> ReadThermostatBridge(
            this TSD.BuildingData buildingData,
            ThermostatBridgePlan thermostatBridgePlan,
            IEnumerable<ThermostatBridgeRoom> thermostatBridgeRooms,
            double achievedAirTemperatureTolerance,
            List<string> refusals)
        {
            List<ResultantTemperatureResult> result = new List<ResultantTemperatureResult>();

            if (buildingData == null || thermostatBridgePlan == null || !thermostatBridgePlan.IsValid)
            {
                refusals.Add("No second TSD or no valid plan was given to the thermostat bridge reader.");
                return result;
            }

            Dictionary<Guid, ThermostatBridgeRoom> room_By_Space = new Dictionary<Guid, ThermostatBridgeRoom>();
            if (thermostatBridgeRooms != null)
            {
                foreach (ThermostatBridgeRoom thermostatBridgeRoom in thermostatBridgeRooms)
                {
                    if (thermostatBridgeRoom != null)
                    {
                        room_By_Space[thermostatBridgeRoom.Guid_Space] = thermostatBridgeRoom;
                    }
                }
            }

            Dictionary<string, TSD.ZoneData> zoneData_By_Key = new Dictionary<string, TSD.ZoneData>(StringComparer.Ordinal);
            HashSet<string> keys_Duplicate = new HashSet<string>(StringComparer.Ordinal);

            //TSD's zone accessor is 1-based.
            int index = 1;
            TSD.ZoneData zoneData;

            while ((zoneData = buildingData.GetZoneData(index)) != null)
            {
                index++;

                string key = ZoneReferenceKey(zoneData.zoneGUID);
                if (key == null)
                {
                    continue;
                }

                if (zoneData_By_Key.ContainsKey(key))
                {
                    keys_Duplicate.Add(key);
                    continue;
                }

                zoneData_By_Key[key] = zoneData;
            }

            //Every bridged zone's two series, read in ONE day-by-day pass over the TSD (BridgeSeries) before any
            //room is judged. The zones are exactly the ones the loop below reads, in plan order, each once.
            List<TSD.ZoneData> zoneDatas_Read = new List<TSD.ZoneData>();
            Dictionary<string, int> index_By_Key = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ThermostatBridgeTransfer thermostatBridgeTransfer in thermostatBridgePlan.Transfers)
            {
                if (keys_Duplicate.Contains(thermostatBridgeTransfer.Key) || index_By_Key.ContainsKey(thermostatBridgeTransfer.Key)
                    || !zoneData_By_Key.TryGetValue(thermostatBridgeTransfer.Key, out zoneData))
                {
                    continue;
                }

                index_By_Key[thermostatBridgeTransfer.Key] = zoneDatas_Read.Count;
                zoneDatas_Read.Add(zoneData);
            }

            List<List<double>[]> series = null;
            string refusal_Read = null;
            try
            {
                series = BridgeSeries(zoneDatas_Read);
            }
            catch (Exception exception)
            {
                refusal_Read = string.Format("the second TSD's hourly results could not be read ({0}: {1}).", exception.GetType().Name, exception.Message);
            }

            foreach (ThermostatBridgeTransfer thermostatBridgeTransfer in thermostatBridgePlan.Transfers)
            {
                room_By_Space.TryGetValue(thermostatBridgeTransfer.Guid_Space, out ThermostatBridgeRoom thermostatBridgeRoom);

                if (keys_Duplicate.Contains(thermostatBridgeTransfer.Key))
                {
                    result.Add(Refused(thermostatBridgeTransfer, string.Format("more than one zone in the second TSD answers guid {0}.", thermostatBridgeTransfer.Reference_Zone)));
                    continue;
                }

                if (!zoneData_By_Key.TryGetValue(thermostatBridgeTransfer.Key, out zoneData))
                {
                    result.Add(Refused(thermostatBridgeTransfer, string.Format("the second TSD has no zone {0}.", thermostatBridgeTransfer.Reference_Zone)));
                    continue;
                }

                if (refusal_Read != null)
                {
                    refusals.Add(string.Format("Room {0}: {1}", thermostatBridgeTransfer.Guid_Space, refusal_Read));
                    result.Add(Refused(thermostatBridgeTransfer, refusal_Read));
                    continue;
                }

                List<double>[] series_Zone = series[index_By_Key[thermostatBridgeTransfer.Key]];
                List<double> resultantTemperatures = series_Zone[0];
                List<double> dryBulbTemperatures = series_Zone[1];

                IndexedDoubles indexedDoubles = new IndexedDoubles();

                int count_Finite = 0;

                for (int i = 0; i < resultantTemperatures.Count; i++)
                {
                    double value = resultantTemperatures[i];

                    indexedDoubles[thermostatBridgeTransfer.StartHour + i] = value;

                    if (!double.IsNaN(value) && !double.IsInfinity(value))
                    {
                        count_Finite++;
                    }
                }

                if (thermostatBridgeRoom != null)
                {
                    thermostatBridgeRoom.Count_ResultantTemperature = resultantTemperatures.Count;
                    thermostatBridgeRoom.Count_ResultantTemperature_Finite = count_Finite;
                }

                string refusal_AchievedAir = AchievedAirTemperatureRefusal(thermostatBridgeTransfer, dryBulbTemperatures, achievedAirTemperatureTolerance, out double max, out double mean);

                if (thermostatBridgeRoom != null)
                {
                    thermostatBridgeRoom.MaxAchievedAirTemperatureDeviation = max;
                    thermostatBridgeRoom.MeanAchievedAirTemperatureDeviation = mean;
                }

                if (refusal_AchievedAir != null)
                {
                    refusals.Add(string.Format("Room {0}: {1}", thermostatBridgeTransfer.Guid_Space, refusal_AchievedAir));
                }

                result.Add(new ResultantTemperatureResult(
                    thermostatBridgeTransfer.Guid_Space,
                    thermostatBridgeTransfer.Reference_Zone,
                    ThermostatBridgePlan.StartHour,
                    ThermostatBridgePlan.EndHour,
                    indexedDoubles,
                    null));
            }

            return result;
        }

        /// <summary>
        /// Why the copied building's simulated air temperature does not follow the imposed achieved series
        /// closely enough, or null when it does. Compares hour by hour against the Systems value itself, not
        /// against its single-precision copy.
        /// </summary>
        /// <param name="max">Largest |dry bulb - imposed|, K; NaN when the series cannot be compared.</param>
        /// <param name="mean">Mean |dry bulb - imposed|, K; NaN when the series cannot be compared.</param>
        public static string AchievedAirTemperatureRefusal(
            ThermostatBridgeTransfer thermostatBridgeTransfer,
            IList<double> dryBulbTemperatures,
            double achievedAirTemperatureTolerance,
            out double max,
            out double mean)
        {
            max = double.NaN;
            mean = double.NaN;

            if (thermostatBridgeTransfer == null)
            {
                return "no transfer to compare the simulated air temperature against.";
            }

            if (double.IsNaN(achievedAirTemperatureTolerance) || double.IsInfinity(achievedAirTemperatureTolerance) || achievedAirTemperatureTolerance < 0)
            {
                return string.Format("the achieved-air tolerance {0} is not a temperature difference.", achievedAirTemperatureTolerance);
            }

            if (dryBulbTemperatures == null || dryBulbTemperatures.Count != thermostatBridgeTransfer.Count)
            {
                return string.Format(
                    "the second simulation answered {0} air temperature(s) for {1} imposed hour(s), so whether the air was held at the "
                    + "achieved temperature cannot be shown.",
                    dryBulbTemperatures == null ? 0 : dryBulbTemperatures.Count,
                    thermostatBridgeTransfer.Count);
            }

            double sum = 0;
            double max_Temp = 0;
            int hour_Max = -1;

            for (int i = 0; i < dryBulbTemperatures.Count; i++)
            {
                double delta = global::System.Math.Abs(dryBulbTemperatures[i] - thermostatBridgeTransfer.ZoneTemperature(i));

                if (double.IsNaN(delta) || double.IsInfinity(delta))
                {
                    return string.Format("the simulated air temperature at hour {0} is {1}.", thermostatBridgeTransfer.StartHour + i, dryBulbTemperatures[i]);
                }

                sum += delta;

                if (delta > max_Temp)
                {
                    max_Temp = delta;
                    hour_Max = thermostatBridgeTransfer.StartHour + i;
                }
            }

            max = max_Temp;
            mean = dryBulbTemperatures.Count == 0 ? 0 : sum / dryBulbTemperatures.Count;

            if (max_Temp > achievedAirTemperatureTolerance)
            {
                return string.Format(
                    "the copied building's air temperature stands up to {0:0.0000} K from the imposed achieved temperature (hour {1}), "
                    + "beyond the {2} K the bridge accepts, so its resultant temperature is not the Systems building's.",
                    max_Temp,
                    hour_Max,
                    achievedAirTemperatureTolerance);
            }

            return null;
        }

        private static ResultantTemperatureResult Refused(ThermostatBridgeTransfer thermostatBridgeTransfer, string diagnostic)
        {
            return new ResultantTemperatureResult(
                thermostatBridgeTransfer.Guid_Space,
                thermostatBridgeTransfer.Reference_Zone,
                ThermostatBridgePlan.StartHour,
                ThermostatBridgePlan.EndHour,
                null,
                diagnostic);
        }

        /// <summary>
        /// The bridge's two series - [0] resultant temperature, [1] dry bulb - for every zone, read DAY BY DAY across
        /// all of them over the bridge's whole year (<see cref="ThermostatBridgePlan.FirstDay"/>..<see cref="ThermostatBridgePlan.LastDay"/>):
        /// every zone's two arrays for day 1, then day 2, and so on - the order of
        /// <c>SAM.Analytical.Tas.Query.ZoneResultSeries</c>. Not that method itself: this assembly does not reference
        /// SAM.Analytical.Tas, and referencing it would put that assembly's same-named <c>Create</c>, <c>Query</c>,
        /// <c>SpaceParameter</c> and <c>AnalyticalModelParameter</c> in scope of every file here.
        /// <para>
        /// <b>Why not one <c>GetAnnualZoneResult</c> per zone and array.</b> TSD.exe decodes results a simulated day
        /// at a time into a cache of roughly 550 MB; on a file whose decoded year does not fit, every zone-by-zone
        /// annual read decodes the whole year again (~8 s per series on a 483 MB TSD, ~52 min for the x30 bridge),
        /// while a day-major pass decodes each day once (the SAM_Tas PR record).
        /// The 365 daily answers joined in order are, bit for bit, the annual answer - measured on real TSDs - and
        /// each value goes through the same <see cref="AnnualSeries"/> conversion as before, so a null or
        /// unconvertible element is still NaN and a daily answer of the wrong length still changes the count the
        /// completeness checks refuse on.
        /// </para>
        /// <para>
        /// <b>Not a completeness check.</b> TSD answers every day 1..365 even for a part-year simulation, padding the
        /// days it did not simulate with -1, so neither this read nor the annual one it replaced can tell a part year
        /// from a full one by length. The simulation's stated day range does that, in
        /// <see cref="ReadThermostatBridge(TSD.SimulationData, ThermostatBridgePlan, IEnumerable{ThermostatBridgeRoom}, double, List{string})"/>.
        /// </para>
        /// </summary>
        private static List<List<double>[]> BridgeSeries(IList<TSD.ZoneData> zoneDatas)
        {
            short[] tsdZoneArrays = new short[] { (short)TSD.tsdZoneArray.resultantTemp, (short)TSD.tsdZoneArray.dryBulbTemp };

            List<List<double>[]> result = new List<List<double>[]>(zoneDatas.Count);
            foreach (TSD.ZoneData zoneData in zoneDatas)
            {
                List<double>[] series_Zone = new List<double>[tsdZoneArrays.Length];
                for (int i = 0; i < tsdZoneArrays.Length; i++)
                {
                    series_Zone[i] = new List<double>(ThermostatBridgePlan.HoursPerYear);
                }

                result.Add(series_Zone);
            }

            for (int day = ThermostatBridgePlan.FirstDay; day <= ThermostatBridgePlan.LastDay; day++)
            {
                for (int j = 0; j < zoneDatas.Count; j++)
                {
                    for (int i = 0; i < tsdZoneArrays.Length; i++)
                    {
                        result[j][i].AddRange(AnnualSeries(zoneDatas[j].GetDailyZoneResult(day, tsdZoneArrays[i])));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// A TSD annual result as doubles, in the order TSD returned it. A null or unconvertible element becomes
        /// NaN - which the completeness check then refuses - never a plausible zero.
        /// </summary>
        private static List<double> AnnualSeries(object value)
        {
            List<double> result = new List<double>();

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                return result;
            }

            foreach (object @object in enumerable)
            {
                double value_Temp = double.NaN;

                if (@object != null)
                {
                    try
                    {
                        value_Temp = global::System.Convert.ToDouble(@object, global::System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        value_Temp = double.NaN;
                    }
                }

                result.Add(value_Temp);
            }

            return result;
        }
    }
}
