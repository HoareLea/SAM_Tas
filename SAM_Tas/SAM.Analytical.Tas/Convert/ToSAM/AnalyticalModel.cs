// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using SAM.Geometry.SolarCalculator;
using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static AnalyticalModel ToSAM(string path_TSD, TSDConversionSettings tSDConversionSettings)
        {
            return ToSAM(path_TSD, tSDConversionSettings, out _);
        }

        /// <summary>
        /// The model a TSD holds, opened READ-ONLY (the conversion never writes to it).
        /// </summary>
        /// <param name="refusal">
        /// Why no model was returned when <see cref="TSDConversionSettings.RequireFullYear"/> refused the file,
        /// otherwise null.
        /// </param>
        public static AnalyticalModel ToSAM(string path_TSD, TSDConversionSettings tSDConversionSettings, out string refusal)
        {
            AnalyticalModel result = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
            {
                result = ToSAM(sAMTSDDocument.TSDDocument, tSDConversionSettings, out refusal);
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
            return ToSAM(tSDDocument, tSDConversionSettings, out _);
        }

        /// <param name="refusal">
        /// Why no model was returned when <see cref="TSDConversionSettings.RequireFullYear"/> refused the file,
        /// otherwise null.
        /// </param>
        public static AnalyticalModel ToSAM(this TSD.TSDDocument tSDDocument, TSDConversionSettings tSDConversionSettings, out string refusal)
        {
            refusal = null;

            TSD.BuildingData buildingData = tSDDocument?.SimulationData?.GetBuildingData();
            if (buildingData == null)
            {
                return null;
            }

            if(tSDConversionSettings == null)
            {
                tSDConversionSettings = new TSDConversionSettings();
            }

            //Opt-in, for a caller whose assessment is defined over a whole year (Part O's TM59). Checked on the
            //simulation's stated day range BEFORE any result is read, because nothing read afterwards can tell: TSD
            //answers 8760 hours for any simulation and pads the days it did not simulate with -1.
            if (tSDConversionSettings.RequireFullYear)
            {
                refusal = Query.FullYearRefusal(tSDDocument.SimulationData);
                if (refusal != null)
                {
                    return null;
                }
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

        /// <summary>
        /// Backwards-compatible 2-arg overload — forwards to the 3-arg variant with
        /// <paramref name="importSurfaceShades"/> defaulted to <c>false</c> so callers
        /// compiled against the previous signature keep working.
        /// </summary>
        public static AnalyticalModel ToSAM(string path_TBD, bool importUnused)
        {
            return ToSAM(path_TBD, importUnused, false);
        }

        public static AnalyticalModel ToSAM(string path_TBD, bool importUnused, bool importSurfaceShades)
        {
            // Shared polygon3D cache across AdjacencyCluster build + Create.SolarModel.
            // Saves redundant COM polygon conversions for the exposed surfaces that get
            // processed by both phases (~50 polygons, ~400 ms on a real building).
            Dictionary<string, Polygon3D> polygonCache = [];

            AnalyticalModel result = null;
            using (SAMTBDDocument sAMTBDDocument = new (path_TBD))
            {
                if (!importUnused)
                {
                    Modify.RemoveUnusedInternalConditions(sAMTBDDocument?.TBDDocument?.Building);
                }

                TBD.Building building = sAMTBDDocument?.TBDDocument?.Building;
                result = ToSAM_AnalyticalModel(building, polygonCache, importUnused);

                if(result is not null)
                {
                    Weather.WeatherData weatherData = Weather.Tas.Convert.ToSAM_WeatherData(building);
                    if(weatherData is not null)
                    {
                        List<DesignDay> coolingDesignDays = null;
                        List<DesignDay> heatingDesignDays = null;

                        if(weatherData.CoolingDesignDay() is DesignDay coolingDesignDay)
                        {
                            coolingDesignDays = [coolingDesignDay];
                        }

                        if (weatherData.HeatingDesignDay() is DesignDay heatingDesignDay)
                        {
                            heatingDesignDays = [heatingDesignDay];
                        }

                        result.UpdateWeather(weatherData, coolingDesignDays, heatingDesignDays);
                    }
                    
                    if (importSurfaceShades)
                    {
                        SolarModel solarModel = Create.SolarModel(building, polygonCache);
                        if (solarModel != null)
                        {
                            result = result.CopyResults(solarModel);
                            result.SetValue(Analytical.AnalyticalModelParameter.SolarModel, solarModel);
                        }
                    }
                }
            }

            return result;
        }

        public static AnalyticalModel ToSAM_AnalyticalModel(this SAMTBDDocument sAMTBDDocument)
        {
            if (sAMTBDDocument == null)
            {
                return null;
            }

            return ToSAM_AnalyticalModel(sAMTBDDocument.TBDDocument);

        }

        public static AnalyticalModel ToSAM_AnalyticalModel(this TBD.TBDDocument tBDDocument)
        {
            if (tBDDocument == null)
            {
                return null;
            }

            return ToSAM_AnalyticalModel(tBDDocument.Building);
        }

        public static AnalyticalModel ToSAM_AnalyticalModel(this TBD.Building building)
        {
            return ToSAM_AnalyticalModel(building, null);
        }

        /// <summary>
        /// AnalyticalModel build with optional shared polygon3D cache. Pass a non-null
        /// <paramref name="polygonCache"/> so that subsequent calls (e.g. <c>Create.SolarModel</c>)
        /// can reuse the already-converted Polygon3D objects instead of re-marshaling them
        /// over the TBD COM boundary.
        /// </summary>
        public static AnalyticalModel ToSAM_AnalyticalModel(this TBD.Building building, Dictionary<string, Polygon3D> polygonCache)
        {
            return ToSAM_AnalyticalModel(building, polygonCache, false);
        }

        /// <summary>
        /// AnalyticalModel build with optional shared polygon3D cache. Pass a non-null
        /// <paramref name="polygonCache"/> so that subsequent calls (e.g. <c>Create.SolarModel</c>)
        /// can reuse the already-converted Polygon3D objects instead of re-marshaling them
        /// over the TBD COM boundary.
        /// <para/>
        /// With <paramref name="importUnused"/> = true the building's full construction library is
        /// imported — including constructions not attached to any surface (e.g. the transparent
        /// <c>Air_Glass</c> sizing placeholder or the Null building element's construction) — as
        /// standalone templates, so the library round-trips back out on export. The geometry-driven
        /// import alone only captures constructions referenced by a zone surface. Internal conditions
        /// not assigned to any zone are likewise added as templates (model-side only — see
        /// <see cref="Modify.AddUnusedInternalConditions(AdjacencyCluster, TBD.Building, ProfileReuseIndex)"/> for the export caveat).
        /// </summary>
        public static AnalyticalModel ToSAM_AnalyticalModel(this TBD.Building building, Dictionary<string, Polygon3D> polygonCache, bool importUnused)
        {
            if (building == null)
            {
                return null;
            }

            //One index for the whole conversion: the library below, every zone-owned internal condition inside
            //ToSAM(building, ...), and the unused-condition templates all resolve their profile references
            //through it, so the model cannot disagree with itself about what a reference names.
            ProfileReuseIndex profileReuseIndex = building.ProfileReuseIndex();

            ProfileLibrary profileLibrary = building.ToSAM_ProfileLibrary(profileReuseIndex);
            Core.MaterialLibrary materialLibrary = building.ToSAM_MaterialLibrary();

            Core.Location location = new Core.Location(building.name, building.longitude, building.latitude, 0);
            location.SetValue(Core.LocationParameter.TimeZone, Core.Query.Description(Core.Query.UTC(building.timeZone)));

            Core.Address address = new (null, null, null, Core.CountryCode.Undefined);

            AdjacencyCluster adjacencyCluster = ToSAM(building, polygonCache, profileReuseIndex);

            if (importUnused)
            {
                adjacencyCluster.AddUnusedConstructions(building);
                adjacencyCluster.AddUnusedInternalConditions(building, profileReuseIndex);
            }

            return new AnalyticalModel(building.name, null, location, address, adjacencyCluster, materialLibrary, profileLibrary);
        }

        public static SolarModel ToSAM_SolarModel(this SAMTBDDocument sAMTBDDocument)
        {
            if(sAMTBDDocument == null)
            {
                return null;
            }

            return ToSAM_SolarModel(sAMTBDDocument.TBDDocument);

        }

        public static SolarModel ToSAM_SolarModel(this TBD.TBDDocument tBDDocument)
        {
            if(tBDDocument == null)
            {
                return null;
            }

            return ToSAM_SolarModel(tBDDocument.Building);
        }
        
        public static SolarModel ToSAM_SolarModel(this TBD.Building building)
        {
            if (building == null)
            {
                return null;
            }

            return Create.SolarModel(building);
        }
    }
}
