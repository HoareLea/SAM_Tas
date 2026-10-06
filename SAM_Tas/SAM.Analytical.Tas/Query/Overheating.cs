// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>The zone arrays <see cref="Overheating(float[], float[], float[], double)"/> is calculated from.</summary>
        public static readonly TSD.tsdZoneArray[] OverheatingZoneArrays = new TSD.tsdZoneArray[] { TSD.tsdZoneArray.occupantSensibleGain, TSD.tsdZoneArray.resultantTemp, TSD.tsdZoneArray.dryBulbTemp };

        public static Dictionary<Analytical.SpaceSimulationResultParameter, object> Overheating(TSD.ZoneData zoneData, int index_Start, int index_End, double tolerance = 0.01)
        {
            if (zoneData == null)
                return null;

            Dictionary<TSD.tsdZoneArray, float[]> dictionary = ZoneResultSeries(new TSD.ZoneData[] { zoneData }, index_Start, index_End, OverheatingZoneArrays)[0];

            return Overheating(dictionary[TSD.tsdZoneArray.occupantSensibleGain], dictionary[TSD.tsdZoneArray.resultantTemp], dictionary[TSD.tsdZoneArray.dryBulbTemp], tolerance);
        }

        /// <summary>
        /// The overheating values from a zone's 8760-hour series, laid out as <see cref="ZoneResultSeries"/> returns them.
        /// The series are not modified.
        /// </summary>
        public static Dictionary<Analytical.SpaceSimulationResultParameter, object> Overheating(float[] occupancySensibleGains, float[] resultantTemperatures, float[] dryBulbTemperatures, double tolerance = 0.01)
        {
            if (occupancySensibleGains == null || resultantTemperatures == null || dryBulbTemperatures == null)
                return null;

            dryBulbTemperatures = (float[])dryBulbTemperatures.Clone();
            for(int i =0; i < dryBulbTemperatures.Length; i++)
            {
                dryBulbTemperatures[i] = Core.Query.Round(dryBulbTemperatures[i], (float)tolerance);
            }

            float temperature_Max = float.MinValue;
            int temperature_Max_Index = -1;
            float temperature_Min = float.MaxValue;
            int temperature_Min_Index = -1;

            //item1 resultantTemp > 25 
            //item2 resultantTemp > 28
            int[] temperatures_Count = new int[] { 0, 0 };
            float[] temperatures = new float[] { 25, 28 };
            int occupiedHours = 0;
            for (int i = 0; i < 8760; i++)
            {
                //Max and Min Temp
                float aTemp = dryBulbTemperatures[i];
                if (aTemp > temperature_Max)
                {
                    temperature_Max = aTemp;
                    temperature_Max_Index = i;
                }

                if (aTemp < temperature_Min)
                {
                    temperature_Min = aTemp;
                    temperature_Min_Index = i;
                }

                // does the zone have occupancy
                if (occupancySensibleGains[i] > 0)
                {
                    //We are taking temperature data for to cases: greater than 25 and greated than 28 resultantTemperature
                    for (int n = 0; n < temperatures.Length; n++)
                    {
                        if (resultantTemperatures[i] > temperatures[n])
                            temperatures_Count[n]++;
                    }

                    occupiedHours++;
                }

            }

            Dictionary<Analytical.SpaceSimulationResultParameter, object> result = new Dictionary<Analytical.SpaceSimulationResultParameter, object>();
            result[Analytical.SpaceSimulationResultParameter.OccupiedHours] = occupiedHours;
            result[Analytical.SpaceSimulationResultParameter.OccupiedHours25] = temperatures_Count[0];
            result[Analytical.SpaceSimulationResultParameter.OccupiedHours28] = temperatures_Count[1];
            result[Analytical.SpaceSimulationResultParameter.MaxDryBulbTemperatureIndex] = temperature_Max_Index;
            result[Analytical.SpaceSimulationResultParameter.MinDryBulbTemperatureIndex] = temperature_Min_Index;
            result[Analytical.SpaceSimulationResultParameter.MaxDryBulbTemperature] = temperature_Max;
            result[Analytical.SpaceSimulationResultParameter.MinDryBulbTemperature] = temperature_Min;

            return result;
        }
    }
}