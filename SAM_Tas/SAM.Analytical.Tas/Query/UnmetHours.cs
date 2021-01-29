using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, List<int>> UnmetHours(this BuildingData buildingData, tsdZoneArray tSDZoneArray, bool filterByExternalTemperature, bool workingHours, double temperatureLimit = 26, double margin = 0.5)
        {
            if (buildingData == null)
                return null;
            
            Dictionary<tsdZoneArray, Dictionary<string, double[]>> yearlyValues = Query.YearlyValues(buildingData, new tsdZoneArray[] { tSDZoneArray, tsdZoneArray.occupantSensibleGain });
            if (yearlyValues == null)
                return null;

            Dictionary<string, List<int>> result = new Dictionary<string, List<int>>();

            foreach (KeyValuePair<string, double[]> keyValuePair in yearlyValues[tsdZoneArray.occupantSensibleGain])
            {
                for (int i = 0; i < keyValuePair.Value.Length; i++)
                {
                    if (workingHours && keyValuePair.Value[i] <= 0)
                        continue;

                    if (!workingHours && keyValuePair.Value[i] > 0)
                        continue;

                    double temperature = yearlyValues[tSDZoneArray][keyValuePair.Key][i];

                    if (temperature <= temperatureLimit + margin)
                        continue;

                    List<int> indexes = null;
                    if (!result.TryGetValue(keyValuePair.Key, out indexes))
                    {
                        indexes = new List<int>();
                        result[keyValuePair.Key] = indexes;
                    }
                    result[keyValuePair.Key].Add(i);
                }
            }

            if (filterByExternalTemperature)
            {
                foreach (KeyValuePair<string, List<int>> aKeyValuePair in result)
                {
                    List<int> indexes = new List<int>();
                    foreach (int aIndex in result[aKeyValuePair.Key])
                    {
                        float aTemperature = buildingData.GetHourlyBuildingResult(aIndex, (int)tsdBuildingArray.externalTemperature);
                        if (aTemperature < yearlyValues[tSDZoneArray][aKeyValuePair.Key][aIndex])
                            indexes.Add(aIndex);
                    }

                    result[aKeyValuePair.Key] = indexes;
                }
            }

            return result;
        }
    }
}