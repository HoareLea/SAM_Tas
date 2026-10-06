// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using System.IO;
using TAS3D;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static bool ToTBD(this string path_T3D, string path_TBD, int day_First, int day_Last, int step, bool autoAssignConstructions, bool removeExisting)
        {
            if (string.IsNullOrWhiteSpace(path_T3D) || string.IsNullOrWhiteSpace(path_TBD))
                return false;

            bool result = false;
            using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
            {
                result = ToTBD(sAMT3DDocument, path_TBD, day_First, day_Last, step, autoAssignConstructions, removeExisting);
            }

            return result;
        }

        public static bool ToTBD(this SAMT3DDocument sAMT3DDocument, string path_TBD, int day_First, int day_Last, int step, bool autoAssignConstructions, bool removeExisting)
        {
            if (sAMT3DDocument == null)
                return false;

            return ToTBD(sAMT3DDocument.T3DDocument, path_TBD, day_First, day_Last, step, autoAssignConstructions, removeExisting);
        }

        public static bool ToTBD(this T3DDocument t3DDocument, string path_TBD, int day_First, int day_Last, int step, bool autoAssignConstructions, bool removeExisting)
        {
            if (t3DDocument == null || string.IsNullOrWhiteSpace(path_TBD))
            {
                return false;
            }

            if(removeExisting)
            {
                if (File.Exists(path_TBD))
                {
                    File.Delete(path_TBD);
                }
            }

            int int_autoAssignConstructions = 0;
            if (autoAssignConstructions)
            {
                int_autoAssignConstructions = 1;
            }

            if (path_TBD != null && File.Exists(path_TBD) && !string.IsNullOrWhiteSpace(t3DDocument?.Building?.GUID))
            {
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    TBD.Building building = sAMTBDDocument?.TBDDocument?.Building;
                    if(building != null)
                    {
                        if(building.GUID != t3DDocument.Building.GUID)
                        {
                            building.GUID = t3DDocument.Building.GUID;
                            sAMTBDDocument.Save();
                        }
                    }
                }
            }

            return t3DDocument.ExportNew(day_First, day_Last, step, 1, 1, 1, path_TBD, int_autoAssignConstructions, 0, 0);
        }

        public static bool ToTBD(this AnalyticalModel analyticalModel, string path, Weather.WeatherData weatherData = null, IEnumerable<DesignDay> coolingDesignDays = null, IEnumerable<DesignDay> heatingDesignDays = null, bool updateGuids = false)
        {
            if(analyticalModel == null || string.IsNullOrWhiteSpace(path))
                {
                return false;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            Weather.WeatherData weatherData_Temp = weatherData;
            if(weatherData_Temp == null)
            {
                if (!analyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.WeatherData, out weatherData_Temp, true))
                {
                    weatherData_Temp = null;
                }
            }
            
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                if (weatherData_Temp != null)
                {
                    double buildingHeight = Analytical.Query.BuildingHeight(analyticalModel.AdjacencyCluster);
                    buildingHeight = double.IsNaN(buildingHeight) || buildingHeight < 0 ? 0 : buildingHeight;

                    Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData_Temp, buildingHeight);
                }

                TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                List<TBD.dayType> dayTypes = Query.DayTypes(calendar);
                if (dayTypes.Find(x => x.name == "HDD") == null)
                {
                    TBD.dayType dayType = calendar.AddDayType();
                    dayType.name = "HDD";
                }

                if (dayTypes.Find(x => x.name == "CDD") == null)
                {
                    TBD.dayType dayType = calendar.AddDayType();
                    dayType.name = "CDD";
                }

                ToTBD(analyticalModel, tBDDocument, updateGuids);

                // ToTBD only writes constructions referenced by a panel/aperture; add the unused
                // library templates (e.g. Air_Glass, the Null element's construction) so they
                // round-trip on this export path too. (UpdateZones below handles the IC templates.)
                Modify.AddUnusedConstructions(tBDDocument.Building, analyticalModel.AdjacencyCluster, analyticalModel.MaterialLibrary);

                Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                // Same rule as WorkflowCalculator: a caller who states a weather makes that weather
                // authoritative over the design days it did not state outright, so a re-export with a new
                // weather cannot size on the design days the model brought back from the old one.
                Query.DesignDays_Authoritative(
                    analyticalModel,
                    weatherData,
                    coolingDesignDays,
                    heatingDesignDays,
                    out List<DesignDay> coolingDesignDays_Authoritative,
                    out List<DesignDay> heatingDesignDays_Authoritative);

                if (coolingDesignDays_Authoritative != null || heatingDesignDays_Authoritative != null)
                {
                    Modify.AddDesignDays(tBDDocument, coolingDesignDays_Authoritative, heatingDesignDays_Authoritative, 30);
                }

                Modify.UpdateShading(tBDDocument.Building, analyticalModel);

                sAMTBDDocument.Save();
            }

            // Diagnostic only (SAM_DEBUG): reopen the saved TBD and compare the coverage read back
            // (the FromTBD path) against the SAM source coverage, so the option-2 round-trip drift is
            // captured on the ToTBD side. No-op unless _debug_ is on; never throws.
            Modify.LogShadeRoundTrip(path, analyticalModel);

            return true;
        }
    }
}
