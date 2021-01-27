﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<SpaceSimulationResultParameter, object> Overheating(TSD.ZoneData zoneData, int index_Start, int index_End)
        {
            if (zoneData == null)
                return null;

            Dictionary<TSD.tsdZoneArray, float[]> dictionary = new Dictionary<TSD.tsdZoneArray, float[]>();
            dictionary[TSD.tsdZoneArray.occupantSensibleGain] = new float[8760];
            dictionary[TSD.tsdZoneArray.resultantTemp] = new float[8760];
            dictionary[TSD.tsdZoneArray.dryBulbTemp] = new float[8760];

            for (int i = index_Start; i <= index_End; i++)
            {
                foreach (KeyValuePair<TSD.tsdZoneArray, float[]> keyValuePair in dictionary)
                {
                    float[] yearlyValues = keyValuePair.Value;
                    float[] dailyValues = (zoneData.GetDailyZoneResult(i, (short)keyValuePair.Key) as IEnumerable).Cast<float>().ToArray();
                    int startHour = (i * 24) - 24;
                    int counter = 0;
                    for (int n = startHour; n <= startHour + 23; n++)
                    {
                        yearlyValues[n] = dailyValues[counter];
                        counter += 1;
                    }
                    dictionary[keyValuePair.Key] = yearlyValues;
                }
            }

            float[] occupancySensibleGains = dictionary[TSD.tsdZoneArray.occupantSensibleGain];
            float[] resultantTemperatures = dictionary[TSD.tsdZoneArray.resultantTemp];
            float[] dryBulbTemperatures = dictionary[TSD.tsdZoneArray.dryBulbTemp];

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
                    //We are taking temperature data for to cases: greater than 25 and greated than 28
                    for (int n = 0; n < temperatures.Length; n++)
                    {
                        if (resultantTemperatures[i] > temperatures[n])
                            temperatures_Count[n]++;
                    }

                    occupiedHours++;
                }

            }

            Dictionary<SpaceSimulationResultParameter, object> result = new Dictionary<SpaceSimulationResultParameter, object>();
            result[SpaceSimulationResultParameter.OccupiedHours] = occupiedHours;
            result[SpaceSimulationResultParameter.OccupiedHours25] = temperatures_Count[0];
            result[SpaceSimulationResultParameter.OccupiedHours28] = temperatures_Count[1];
            result[SpaceSimulationResultParameter.MaxDryBulbTemperatureIndex] = temperature_Max_Index;
            result[SpaceSimulationResultParameter.MinDryBulbTemperatureIndex] = temperature_Min_Index;
            result[SpaceSimulationResultParameter.MaxDryBulbTemperature] = temperature_Max;
            result[SpaceSimulationResultParameter.MinDryBulbTemperature] = temperature_Min;

            return result;
        }
    }
}