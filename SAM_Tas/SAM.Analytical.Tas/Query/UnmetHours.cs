using SAM.Core.Tas;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, List<int>> UnmetHours(this BuildingData buildingData, tsdZoneArray tSDZoneArray, bool filterByExternalTemperature, bool workingHours, double temperatureLimit = 26, double margin = 0.5)
        {
            if (buildingData == null)
                return null;
            
            Dictionary<tsdZoneArray, Dictionary<string, double[]>> yearlyValues = YearlyValues(buildingData, new tsdZoneArray[] { tSDZoneArray, tsdZoneArray.occupantSensibleGain });
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
                    indexes.Add(i);
                }
            }

            if (filterByExternalTemperature)
            {
                foreach (KeyValuePair<string, List<int>> aKeyValuePair in result)
                {
                    List<int> indexes = new List<int>();
                    foreach (int index in result[aKeyValuePair.Key])
                    {
                        float temperature = buildingData.GetHourlyBuildingResult(index, (int)tsdBuildingArray.externalTemperature);
                        if (temperature < yearlyValues[tSDZoneArray][aKeyValuePair.Key][index])
                            indexes.Add(index);
                    }

                    result[aKeyValuePair.Key] = indexes;
                }
            }

            return result;
        }
    
        public static List<Core.Result> UnmetHours(string path_TSD, string path_TBD, double margin = 0.5)
        {
            if (string.IsNullOrWhiteSpace(path_TSD) || !File.Exists(path_TSD))
                return null;

            if (string.IsNullOrWhiteSpace(path_TBD) || !File.Exists(path_TBD))
                return null;

            Dictionary<TBD.Profiles, Dictionary<string, double[]>> temperatureSetPointDictionary = null;
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD, true))
            {
                temperatureSetPointDictionary = sAMTBDDocument.TBDDocument?.Building?.TemperatureSetPointDictionary(TBD.Profiles.ticLL, TBD.Profiles.ticUL);
                sAMTBDDocument.Close();
            }

            if (temperatureSetPointDictionary == null || temperatureSetPointDictionary.Count == 0)
                return null;

            Dictionary<tsdZoneArray, Dictionary<string, double[]>> yearlyValues = null;
            Dictionary<string, SpaceSimulationResult> dictionary_SpaceSimulationResult = null;
            AdjacencyClusterSimulationResult adjacencyClusterSimulationResult = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
            {
                BuildingData buildingData = sAMTSDDocument.TSDDocument?.SimulationData?.GetBuildingData();
                if(buildingData != null)
                {
                    adjacencyClusterSimulationResult = Create.AdjacencyClusterSimulationResult(buildingData);
                    yearlyValues = sAMTSDDocument.TSDDocument?.SimulationData?.GetBuildingData()?.YearlyValues(new tsdZoneArray[] { tsdZoneArray.dryBulbTemp, tsdZoneArray.occupantSensibleGain });

                    dictionary_SpaceSimulationResult = new Dictionary<string, SpaceSimulationResult>();
                    foreach (KeyValuePair<string, ZoneData> keyValuePair in buildingData.ZoneDataDictionary())
                        dictionary_SpaceSimulationResult[keyValuePair.Key] = Create.SpaceSimulationResult(keyValuePair.Value);
                }
            }

            Dictionary<string, bool[]> dictionary_Heating = null;
            Dictionary<string, bool[]> dictionary_Cooling = null;

            if (yearlyValues != null && yearlyValues.Count != 0)
            {
                dictionary_Heating = Compare(temperatureSetPointDictionary[TBD.Profiles.ticLL], yearlyValues[tsdZoneArray.dryBulbTemp], margin, LoadType.Heating);
                if (dictionary_Heating == null)
                    return null;

                dictionary_Cooling = Compare(temperatureSetPointDictionary[TBD.Profiles.ticUL], yearlyValues[tsdZoneArray.dryBulbTemp], margin, LoadType.Cooling);
                if (dictionary_Cooling == null)
                    return null;
            }

            Dictionary<string, bool[]> dictionary_OccupantSensibleGain = new Dictionary<string, bool[]>();
            foreach (KeyValuePair<string, double[]> aKeyValuePair in yearlyValues[tsdZoneArray.occupantSensibleGain])
                dictionary_OccupantSensibleGain.Add(aKeyValuePair.Key, aKeyValuePair.Value.ToList().ConvertAll(x => x > 0).ToArray());

            List<Core.Result> result = new List<Core.Result>();

            foreach (KeyValuePair<string, SpaceSimulationResult> keyValuePair in dictionary_SpaceSimulationResult)
            {
                SpaceSimulationResult spaceSimulationResult = keyValuePair.Value;
                if (spaceSimulationResult == null)
                    continue;

                if (dictionary_OccupantSensibleGain.TryGetValue(keyValuePair.Key, out bool[] occupantSensibleGain))
                {
                    //Heating
                    if (dictionary_Heating.TryGetValue(keyValuePair.Key, out bool[] heating))
                    {
                        List<bool> values = heating.ToList();

                        SpaceSimulationResult spaceSimulationResult_Heating = new SpaceSimulationResult(global::System.Guid.NewGuid(), spaceSimulationResult);
                        spaceSimulationResult_Heating.SetValue(Analytical.SpaceSimulationResultParameter.LoadType, LoadType.Heating.Text());
                        spaceSimulationResult_Heating.SetValue(Analytical.SpaceSimulationResultParameter.UnmetHours, values.Count(x => !x));
                        spaceSimulationResult_Heating.SetValue(Analytical.SpaceSimulationResultParameter.UnmetHourFirstIndex, values.IndexOf(false));

                        int count = 0;
                        for (int i = 0; i < values.Count; i++)
                            if (!values[i] && occupantSensibleGain[i])
                                count++;

                        spaceSimulationResult_Heating.SetValue(Analytical.SpaceSimulationResultParameter.OccupiedUnmetHours, count);
                        result.Add(spaceSimulationResult_Heating);
                    }

                    //Cooling
                    if (dictionary_Cooling.TryGetValue(keyValuePair.Key, out bool[] cooling))
                    {
                        List<bool> values = cooling.ToList();

                        SpaceSimulationResult spaceSimulationResult_Cooling = new SpaceSimulationResult(global::System.Guid.NewGuid(), spaceSimulationResult);
                        spaceSimulationResult_Cooling.SetValue(Analytical.SpaceSimulationResultParameter.LoadType, LoadType.Cooling.Text());
                        spaceSimulationResult_Cooling.SetValue(Analytical.SpaceSimulationResultParameter.UnmetHours, values.Count(x => !x));
                        spaceSimulationResult_Cooling.SetValue(Analytical.SpaceSimulationResultParameter.UnmetHourFirstIndex, values.IndexOf(false));

                        int count = 0;
                        for (int i = 0; i < values.Count; i++)
                            if (!values[i] && occupantSensibleGain[i])
                                count++;

                        spaceSimulationResult_Cooling.SetValue(Analytical.SpaceSimulationResultParameter.OccupiedUnmetHours, count);
                        result.Add(spaceSimulationResult_Cooling);
                    }
                }
            }

            if(adjacencyClusterSimulationResult != null)
            {
                bool[] combined = null;

                if(dictionary_Cooling != null)
                {
                    combined = Combine(dictionary_Cooling.Values);
                    if(combined != null)
                    {
                        AdjacencyClusterSimulationResult adjacencyClusterSimulationResult_Temp = new AdjacencyClusterSimulationResult(global::System.Guid.NewGuid(), adjacencyClusterSimulationResult);
                        adjacencyClusterSimulationResult_Temp.SetValue(AdjacencyClusterSimulationResultParameter.LoadType, LoadType.Cooling);
                        adjacencyClusterSimulationResult_Temp.SetValue(AdjacencyClusterSimulationResultParameter.UnmetHours, combined.Count(x => !x));
                        result.Add(adjacencyClusterSimulationResult_Temp);
                    }
                }

                if (dictionary_Heating != null)
                {
                    combined = Combine(dictionary_Heating.Values);
                    if (combined != null)
                    {
                        AdjacencyClusterSimulationResult adjacencyClusterSimulationResult_Temp = new AdjacencyClusterSimulationResult(global::System.Guid.NewGuid(), adjacencyClusterSimulationResult);
                        adjacencyClusterSimulationResult_Temp.SetValue(AdjacencyClusterSimulationResultParameter.LoadType, LoadType.Heating);
                        adjacencyClusterSimulationResult_Temp.SetValue(AdjacencyClusterSimulationResultParameter.UnmetHours, combined.Count(x => !x));
                        result.Add(adjacencyClusterSimulationResult_Temp);
                    }
                }
            }

            return result;
        }
    }
}