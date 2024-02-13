using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static bool Sizing(this string path_TBD, SizingSettings sizingSettings, AnalyticalModel analyticalModel = null)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
            {
                return false;
            }

            if (!Sizing_PrepareDocument(path_TBD, sizingSettings, analyticalModel))
            {
                return false;
            }

            string directory = global::System.IO.Path.GetDirectoryName(path_TBD);

            if(sizingSettings.GenerateUncappedFile)
            {
                string path_TBD_Uncapped = global::System.IO.Path.Combine(directory, global::System.IO.Path.GetFileNameWithoutExtension(path_TBD) + "_Uncapped" + global::System.IO.Path.GetExtension(path_TBD));
                global::System.IO.File.Copy(path_TBD, path_TBD_Uncapped, true);
                Sizing_ApplyAirGlass(path_TBD, sizingSettings, analyticalModel);
            }

            if(sizingSettings.GenerateHDDCDDFile)
            {
                string path_TBD_HDDCDD = global::System.IO.Path.Combine(directory, global::System.IO.Path.GetFileNameWithoutExtension(path_TBD) + "_HDDCDD" + global::System.IO.Path.GetExtension(path_TBD));
                global::System.IO.File.Copy(path_TBD, path_TBD_HDDCDD, true);
                Sizing_ApplyOversizingFactors(path_TBD, sizingSettings, analyticalModel);
            }

            return true;
        }

        private static bool Sizing_PrepareDocument(string path_TBD, SizingSettings sizingSettings, AnalyticalModel analyticalModel = null)
        {
            if (string.IsNullOrWhiteSpace(path_TBD) || !global::System.IO.File.Exists(path_TBD))
            {
                return false;
            }

            if(sizingSettings == null)
            {
                sizingSettings = new SizingSettings();
            }

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;
                Building building = tBDDocument?.Building;
                if (building != null)
                {
                    TBD.SizingType sizingType = TBD.SizingType.tbdSizing;

                    List<zone> zones = building.Zones();
                    foreach (zone zone in zones)
                    {
                        if (sizingSettings.SystemSizingMethod && Modify.ApplySystemSizingMethod(zone, adjacencyCluster))
                        {
                            continue;
                        }

                        zone.sizeCooling = (int)sizingType;
                        zone.sizeHeating = (int)sizingType;
                        zone.maxCoolingLoad = 0;
                        zone.maxHeatingLoad = 0;
                    }

                    if (sizingSettings.ExcludeOutdoorAir)
                    {
                        List<TBD.InternalCondition> internalConditions = building.InternalConditions();
                        for (int i = internalConditions.Count - 1; i >= 0; i--)
                        {
                            TBD.InternalCondition internalCondition = building.GetIC(i);
                            if (internalCondition.name.EndsWith("HDD"))
                            {
                                profile profile = internalCondition.GetInternalGain()?.GetProfile((int)Profiles.ticV);
                                if (profile != null)
                                    profile.factor = 0;
                            }
                        }
                    }

                    tBDDocument.sizing(0);// new 24.01.2024
                    sAMTBDDocument.Save();
                    result = true;
                }
            }

            return result;
        }

        private static bool Sizing_ApplyAirGlass(string path_TBD, SizingSettings sizingSettings, AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(path_TBD) || !global::System.IO.File.Exists(path_TBD))
            {
                return false;
            }

            if(sizingSettings == null)
            {
                sizingSettings = new SizingSettings();
            }

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;
                Building building = tBDDocument?.Building;
                if (building != null)
                {
                    TBD.Construction construction = building.GetConstructionByName("Air_Glass");
                    if (construction == null)
                    {
                        construction = building.AddConstruction(null);
                        construction.name = "Air_Glass";
                        construction.type = TBD.ConstructionTypes.tcdTransparentConstruction;
                        material material = construction.AddMaterial();
                        material.name = "Air_Glass";
                        material.description = "Special for HDD sizing";
                        material.type = (int)MaterialTypes.tcdTransparentLayer;
                        material.width = global::System.Convert.ToSingle(0.02 / 1000);
                        material.conductivity = 1;
                        material.vapourDiffusionFactor = 1;
                        material.solarTransmittance = 0.999999f;
                        material.externalEmissivity = 0.00001f;
                        material.internalEmissivity = 0.00001f;
                    }

                    List<buildingElement> buildingElements = building.BuildingElements();
                    foreach (buildingElement buildingElement in buildingElements)
                    {
                        if (!buildingElement.name.Contains("_AIR"))
                            continue;

                        buildingElement.ghost = 0;
                        buildingElement.AssignConstruction(construction);
                    }

                    

                    if (sizingSettings.ExcludePositiveInternalGains)
                    {
                        Sizing_ExcludePositiveInternalGains(tBDDocument, sizingSettings, analyticalModel);
                    }
                    else
                    {
                        AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;

                        TBD.SizingType sizingType = TBD.SizingType.tbdDesignSizingOnly;

                        List<zone> zones = building.Zones();
                        foreach (zone zone in zones)
                        {
                            if (sizingSettings.SystemSizingMethod && Modify.ApplySystemSizingMethod(zone, adjacencyCluster))
                            {
                                continue;
                            }

                            zone.sizeCooling = (int)sizingType;
                            zone.sizeHeating = (int)sizingType;
                        }

                        tBDDocument.sizing(0);
                    }

                    sAMTBDDocument.Save();
                    result = true;
                }
            }

            return result;
        }

        private static bool Sizing_ApplyOversizingFactors(string path_TBD, SizingSettings sizingSettings, AnalyticalModel analyticalModel = null)
        {
            if (string.IsNullOrWhiteSpace(path_TBD) || !global::System.IO.File.Exists(path_TBD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;
                Building building = tBDDocument?.Building;
                if (building != null)
                {
                    Modify.UpdateSizingFactors(building, analyticalModel);

                    List<buildingElement> buildingElements = building.BuildingElements();
                    foreach (buildingElement buildingElement in buildingElements)
                    {
                        if (!buildingElement.name.Contains("_AIR"))
                            continue;

                        buildingElement.ghost = 1;
                        buildingElement.AssignConstruction(null);
                    }

                    sAMTBDDocument.Save();
                    result = true;
                }
            }

            return result;
        }

        private static bool Sizing_ExcludePositiveInternalGains(this TBDDocument tBDDocument, SizingSettings sizingSettings, AnalyticalModel analyticalModel = null)
        {
            if (tBDDocument == null)
                return false;

            List<zone> zones = tBDDocument?.Building?.Zones();
            if (zones == null)
                return false;

            TBD.SizingType sizingType = TBD.SizingType.tbdDesignSizingOnly;

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;

            List<Tuple<zone, TBD.InternalCondition, double>> tuples = new List<Tuple<zone, TBD.InternalCondition, double>>();
            foreach (zone zone in zones)
            {
                if (sizingSettings.SystemSizingMethod && Modify.ApplySystemSizingMethod(zone, adjacencyCluster))
                {
                    continue;
                }

                zone.sizeHeating = (int)sizingType;
                zone.sizeCooling = (int)sizingType;

                List<TBD.InternalCondition> internalConditions = zone.InternalConditions();
                if (internalConditions == null || internalConditions.Count == 0)
                {
                    tuples.Add(new Tuple<zone, TBD.InternalCondition, double>(zone, null, -50));
                    continue;
                }

                TBD.InternalCondition internalCondition = internalConditions.Find(x => x.name.EndsWith(" - HDD"));
                if (internalCondition == null)
                {
                    tuples.Add(new Tuple<zone, TBD.InternalCondition, double>(zone, null, -50));
                    continue;
                }

                tuples.Add(new Tuple<zone, TBD.InternalCondition, double>(zone, internalCondition, internalCondition.GetLowerLimit()));
            }

            List<double> temperatures_Unique = tuples.ConvertAll(x => x.Item3).Distinct().ToList();
            temperatures_Unique.Sort();

            if (temperatures_Unique.Count == 0)
                return false;

            Dictionary<zone, double> dictionary = new Dictionary<zone, double>();
            foreach (double tempearture in temperatures_Unique)
            {
                if (tempearture <= -50)
                    continue;

                //Here we filter room that have higher temperature than current set point
                List<Tuple<zone, TBD.InternalCondition, double>> tuples_Temperature = tuples.FindAll(x => x.Item3 >= tempearture);

                foreach (Tuple<zone, TBD.InternalCondition, double> tuple in tuples_Temperature)
                {
                    Thermostat thermostat = tuple.Item2?.GetThermostat();
                    if (thermostat == null)
                        continue;

                    profile profile = thermostat.GetProfile((int)Profiles.ticLL);
                    if (profile == null)
                        continue;

                    profile.value = global::System.Convert.ToSingle(tempearture);
                }

                tBDDocument.save();
                tBDDocument.sizing(0);

                tuples_Temperature = tuples_Temperature.FindAll(x => x.Item3 == tempearture);
                foreach (Tuple<zone, TBD.InternalCondition, double> tuple in tuples_Temperature)
                    if (!dictionary.ContainsKey(tuple.Item1))
                        dictionary[tuple.Item1] = tuple.Item1.maxHeatingLoad;
            }

            sizingType = TBD.SizingType.tbdNoSizing;
            foreach (KeyValuePair<zone, double> keyValuePair in dictionary)
            {
                zone zone = keyValuePair.Key;

                zone.sizeCooling = (int)sizingType;
                zone.sizeHeating = (int)sizingType;

                zone.maxHeatingLoad = global::System.Convert.ToSingle(keyValuePair.Value);
            }

            return true;
        }
    }
}