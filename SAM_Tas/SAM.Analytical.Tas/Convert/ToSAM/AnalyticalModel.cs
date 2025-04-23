using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static AnalyticalModel ToSAM(string path_TSD, TSDConversionSettings tSDConversionSettings)
        {
            AnalyticalModel result = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD))
            {
                result = ToSAM(sAMTSDDocument, tSDConversionSettings);
            }

            return result;
        }

        public static AnalyticalModel ToSAM(this SAMTSDDocument sAMTSDDocument, TSDConversionSettings tSDConversionSettings)
        {
            if (sAMTSDDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTSDDocument.TSDDocument, tSDConversionSettings);

        }

        public static AnalyticalModel ToSAM(this TSD.TSDDocument tSDDocument, TSDConversionSettings tSDConversionSettings)
        {
            TSD.BuildingData buildingData = tSDDocument?.SimulationData?.GetBuildingData();
            if (buildingData == null)
            {
                return null;
            }

            if(tSDConversionSettings == null)
            {
                tSDConversionSettings = new TSDConversionSettings();
            }

            if(tSDConversionSettings.ZoneNames != null)
            {
                tSDConversionSettings = new TSDConversionSettings(tSDConversionSettings);

                if(tSDConversionSettings.SpaceNames == null)
                {
                    tSDConversionSettings.SpaceNames = new HashSet<string>();
                }

                List<TSD.ZoneDataGroup> zoneDataGroups = Query.ZoneDataGroups(tSDDocument.SimulationData);
                foreach(TSD.ZoneDataGroup zoneDataGroup in zoneDataGroups)
                {
                    if(!tSDConversionSettings.ZoneNames.Contains(zoneDataGroup.name))
                    {
                        continue;
                    }

                    List<TSD.ZoneData> zoneDatas = zoneDataGroup.ZoneDatas();
                    if(zoneDatas != null)
                    {
                        zoneDatas.ForEach(x => tSDConversionSettings.SpaceNames.Add(x.name));
                    }
                }
            }

            AdjacencyCluster adjacencyCluster = ToSAM_AdjacencyCluster(buildingData, tSDConversionSettings.SpaceDataTypes, tSDConversionSettings.PanelDataTypes, tSDConversionSettings.SpaceNames);

            if (tSDConversionSettings.ConvertZones)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces();
                
                List<TSD.ZoneDataGroup> zoneDataGroups = Query.ZoneDataGroups(tSDDocument.SimulationData);
                if(zoneDataGroups != null)
                {
                    foreach(TSD.ZoneDataGroup zoneDataGroup in zoneDataGroups)
                    {
                        Zone zone = new Zone(zoneDataGroup.name);
                        adjacencyCluster.AddObject(zone);

                        if(spaces != null)
                        {
                            List<TSD.ZoneData> zoneDatas = Query.ZoneDatas(zoneDataGroup);
                            if (zoneDatas != null)
                            {
                                foreach(TSD.ZoneData zoneData in zoneDatas)
                                {
                                    Space space = spaces.Find(x => x.Name == zoneData.name);
                                    if(space != null)
                                    {
                                        adjacencyCluster.AddRelation(zone, space);
                                    }
                                }
                            }
                        }
                    }
                }
            }


            AnalyticalModel result = new AnalyticalModel(buildingData.name, buildingData.description, null, null, adjacencyCluster);
            result.SetValue(SAM.Analytical.Tas.AnalyticalModelParameter.Path_TBD, tSDDocument?.SimulationData?.buildingPath);

            if(tSDConversionSettings.ConvertWeaterData)
            {
                Weather.WeatherData weatherData = Weather.Tas.Convert.ToSAM_WeatherData(tSDDocument.SimulationData);
                if (weatherData != null)
                {
                    result.SetValue(SAM.Analytical.AnalyticalModelParameter.WeatherData, weatherData);
                }
            }

            return result;
        }

        public static AnalyticalModel ToSAM(string path_TBD, bool importUnused)
        {
            AnalyticalModel result = null;
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                if (!importUnused)
                {
                    Modify.RemoveUnusedInternalConditions(sAMTBDDocument?.TBDDocument?.Building);
                }

                result = ToSAM(sAMTBDDocument);
            }

            return result;
        }

        public static AnalyticalModel ToSAM(this SAMTBDDocument sAMTBDDocument)
        {
            if(sAMTBDDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTBDDocument.TBDDocument);

        }

        public static AnalyticalModel ToSAM(this TBD.TBDDocument tBDDocument)
        {
            if(tBDDocument == null)
            {
                return null;
            }

            return ToSAM_AnalyticalModel(tBDDocument.Building);
        }

        public static AnalyticalModel ToSAM_AnalyticalModel(this TBD.Building building)
        {
            if (building == null)
            {
                return null;
            }

            ProfileLibrary profileLibrary = building.ToSAM_ProfileLibrary();
            Core.MaterialLibrary materialLibrary = building.ToSAM_MaterialLibrary();

            Core.Location location = new Core.Location(building.name, building.longitude, building.latitude, 0);
            location.SetValue(Core.LocationParameter.TimeZone, Core.Query.Description(Core.Query.UTC(building.timeZone)));

            Core.Address address = new Core.Address(null, null, null, Core.CountryCode.Undefined);

            return new AnalyticalModel(building.name, null, location, address, ToSAM(building), materialLibrary, profileLibrary);
        }
    }
}
