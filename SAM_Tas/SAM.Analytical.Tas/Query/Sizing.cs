using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static bool Sizing(this string path_TBD, AnalyticalModel analyticalModel = null, bool excludeOutdoorAir = false, bool excludePositiveInternalGains = false)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return false;

            if (!Sizing_PrepareDocument(path_TBD, excludeOutdoorAir))
                return false;

            string directory = System.IO.Path.GetDirectoryName(path_TBD);

            string path_TBD_Uncapped = System.IO.Path.Combine(directory, System.IO.Path.GetFileNameWithoutExtension(path_TBD) + "_Uncapped" + System.IO.Path.GetExtension(path_TBD));
            System.IO.File.Copy(path_TBD, path_TBD_Uncapped, true);
            Sizing_ApplyAirGlass(path_TBD, excludePositiveInternalGains);

            string path_TBD_HDDCDD = System.IO.Path.Combine(directory, System.IO.Path.GetFileNameWithoutExtension(path_TBD) + "_HDDCDD" + System.IO.Path.GetExtension(path_TBD));
            System.IO.File.Copy(path_TBD, path_TBD_HDDCDD, true);
            Sizing_ApplyOversizingFactors(path_TBD, analyticalModel);

            return true;
        }

        private static bool Sizing_PrepareDocument(string path_TBD, bool excludeOutdoorAir = true)
        {
            if (string.IsNullOrWhiteSpace(path_TBD) || !System.IO.File.Exists(path_TBD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;
                Building building = tBDDocument?.Building;
                if (building != null)
                {
                    SizingType sizingType = SizingType.tbdSizing;

                    List<zone> zones = building.Zones();
                    foreach (zone zone in zones)
                    {
                        zone.sizeCooling = (int)sizingType;
                        zone.sizeHeating = (int)sizingType;
                        zone.maxCoolingLoad = 0;
                        zone.maxHeatingLoad = 0;
                    }

                    List<TBD.InternalCondition> internalConditions = building.InternalConditions();
                    for (int i = internalConditions.Count - 1; i >= 0; i--)
                    {
                        TBD.InternalCondition internalCondition = building.GetIC(i);
                        if (internalCondition.name.EndsWith("HDD"))
                        {
                            if (excludeOutdoorAir)
                            {
                                profile profile = internalCondition.GetInternalGain()?.GetProfile((int)Profiles.ticV);
                                if (profile != null)
                                    profile.factor = 0;
                            }

                            //while (internalCondition.GetZone(0) != null)
                            //{
                            //    zone zone = internalCondition.GetZone(0);
                            //    zone.AssignIC(internalCondition, false);
                            //}
                        }
                    }

                    sAMTBDDocument.Save();
                    result = true;
                }
            }

            return result;
        }

        private static bool Sizing_ApplyAirGlass(string path_TBD, bool excludePositiveInternalGains = false)
        {
            if (string.IsNullOrWhiteSpace(path_TBD) || !System.IO.File.Exists(path_TBD))
                return false;

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
                        material.width = System.Convert.ToSingle(0.02 / 1000);
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

                    if (excludePositiveInternalGains)
                    {
                        Sizing_ExcludePositiveInternalGains(tBDDocument);
                    }
                    else
                    {
                        SizingType sizingType = SizingType.tbdDesignSizingOnly;

                        List<zone> zones = building.Zones();
                        foreach (zone zone in zones)
                        {
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

        private static bool Sizing_ApplyOversizingFactors(string path_TBD, AnalyticalModel analyticalModel = null)
        {
            if (string.IsNullOrWhiteSpace(path_TBD) || !System.IO.File.Exists(path_TBD))
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

        private static bool Sizing_ExcludePositiveInternalGains(this TBDDocument tBDDocument)
        {
            if (tBDDocument == null)
                return false;

            List<zone> zones = tBDDocument?.Building?.Zones();
            if (zones == null)
                return false;

            SizingType sizingType = SizingType.tbdDesignSizingOnly;

            List<Tuple<zone, TBD.InternalCondition, double>> tuples = new List<Tuple<zone, TBD.InternalCondition, double>>();
            foreach (zone zone in zones)
            {
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
                List<Tuple<zone, TBD.InternalCondition, double>> tuples_Temperature = tuples.FindAll(x => x.Item3 <= tempearture);

                if (tempearture > -50)
                {
                    foreach (Tuple<zone, TBD.InternalCondition, double> tuple in tuples_Temperature)
                    {
                        Thermostat thermostat = tuple.Item2?.GetThermostat();
                        if (thermostat == null)
                            continue;

                        profile profile = thermostat.GetProfile((int)Profiles.ticLL);
                        if (profile == null)
                            continue;

                        profile.value = System.Convert.ToSingle(tempearture);
                    }
                }

                tBDDocument.save();
                tBDDocument.sizing(0);

                tuples_Temperature = tuples_Temperature.FindAll(x => x.Item3 == tempearture);
                foreach (Tuple<zone, TBD.InternalCondition, double> tuple in tuples_Temperature)
                    if (!dictionary.ContainsKey(tuple.Item1))
                        dictionary[tuple.Item1] = tuple.Item1.maxHeatingLoad;
            }

            sizingType = SizingType.tbdNoSizing;
            foreach (KeyValuePair<zone, double> keyValuePair in dictionary)
            {
                zone zone = keyValuePair.Key;

                zone.sizeCooling = (int)sizingType;
                zone.sizeHeating = (int)sizingType;

                zone.maxHeatingLoad = System.Convert.ToSingle(keyValuePair.Value);
            }

            return true;
        }
    }
}