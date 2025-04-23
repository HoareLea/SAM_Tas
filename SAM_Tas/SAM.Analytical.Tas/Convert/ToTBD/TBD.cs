using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using System.IO;
using TAS3D;

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
                Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                if(coolingDesignDays == null)
                {
                    if(analyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.CoolingDesignDays, out SAMCollection<DesignDay> designDays, true))
                    {
                        coolingDesignDays = designDays;
                    }
                }

                if (heatingDesignDays == null)
                {
                    if (analyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.HeatingDesignDays, out SAMCollection<DesignDay> designDays, true))
                    {
                        heatingDesignDays = designDays;
                    }
                }

                if (coolingDesignDays != null || heatingDesignDays != null)
                {
                    Modify.AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                }

                sAMTBDDocument.Save();
            }

            return true;
        }
    }
}
