using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Weather;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public class OverheatingCalculator
    {
        private TextMap textMap = Analytical.Query.DefaultInternalConditionTextMap_TM59();

        public TM52BuildingCategory TM52BuildingCategory { get; set; } = TM52BuildingCategory.CategoryII;
        
        public AnalyticalModel AnalyticalModel { get; set; } = null;

        public string Source
        {
            get
            {
                string result = AnalyticalModel?.Name;
                if (string.IsNullOrWhiteSpace(result))
                {
                    result = Query.Source();
                }

                return result;
            }
        }

        public TextMap TextMap
        {
            get
            {
                return textMap;
            }

            set
            {
                textMap = value;
            }
        }

        public OverheatingCalculator(AnalyticalModel analyticalModel)
        {
            AnalyticalModel = analyticalModel;
        }

        public List<TM52ExtendedResult> Calculate_TM52(IEnumerable<Space> spaces, int startHourOfYear = 2880, int endHourOfYear = 6528)
        {
            if (AnalyticalModel == null || spaces == null)
            {
                return null;
            }

            IndexedDoubles maxIndoorComfortTemperatures = GetMaxIndoorComfortTemperatures();
            IndexedDoubles minIndoorComfortTemperatures = GetMinIndoorComfortTemperatures();

            List<TM52ExtendedResult> result = new List<TM52ExtendedResult>();
            foreach (Space space in spaces)
            {
                Space space_Temp = AnalyticalModel.GetSpaces()?.Find(x => x.Guid == space.Guid);
                if (space_Temp == null)
                {
                    continue;
                }

                if (!Core.Query.TryGetValue(space_Temp, SpaceDataType.OccupantSensibleGain.Text(), out JArray jArray_OccupantSensibleGain) || jArray_OccupantSensibleGain == null)
                {
                    continue;
                }

                if (!Core.Query.TryGetValue(space_Temp, SpaceDataType.ResultantTemperature.Text(), out JArray jArray_ResultantTemperature) || jArray_ResultantTemperature == null)
                {
                    continue;
                }

                HashSet<int> occupiedHourIndices = new HashSet<int>();
                IndexedDoubles maxAcceptableTemperatures = new IndexedDoubles();
                IndexedDoubles minAcceptableTemperatures = new IndexedDoubles();
                IndexedDoubles operativeTemperatures = new IndexedDoubles();

                for (int i = 0; i < jArray_OccupantSensibleGain.Count; i++)
                {
                    if(i < startHourOfYear || i > endHourOfYear)
                    {
                        continue;
                    }

                    if (!Core.Query.TryConvert(jArray_ResultantTemperature[i], out double resultantTemperature) || double.IsNaN(resultantTemperature))
                    {
                        continue;
                    }

                    maxAcceptableTemperatures.Add(i, maxIndoorComfortTemperatures[i]);
                    minAcceptableTemperatures.Add(i, minIndoorComfortTemperatures[i]);
                    operativeTemperatures.Add(i, resultantTemperature);


                    if (!Core.Query.TryConvert(jArray_OccupantSensibleGain[i], out double occupantSensibleGain) || double.IsNaN(occupantSensibleGain))
                    {
                        continue;
                    }

                    if(occupantSensibleGain <= 0)
                    {
                        continue;
                    }

                    occupiedHourIndices.Add(i);
                }

                TM52ExtendedResult tM52ExtendedResult = new TM52ExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory,occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures);
                result.Add(tM52ExtendedResult);
            }

            return result;
        }

        public List<TM59ExtendedResult> Calculate_TM59(IEnumerable<Space> spaces)
        {
            if (AnalyticalModel == null || spaces == null || textMap == null)
            {
                return null;
            }

            TM59Manager TM59Manager = new TM59Manager(textMap);

            IndexedDoubles maxIndoorComfortTemperatures = GetMaxIndoorComfortTemperatures();
            IndexedDoubles minIndoorComfortTemperatures = GetMinIndoorComfortTemperatures();

            AdjacencyCluster adjacencyCluster = AnalyticalModel.AdjacencyCluster;

            List<TM59ExtendedResult> result = new List<TM59ExtendedResult>();
            foreach (Space space in spaces)
            {
                Space space_Temp = adjacencyCluster?.GetSpaces()?.Find(x => x.Guid == space.Guid);
                if (space_Temp == null)
                {
                    continue;
                }

                string systemTypeName = space_Temp?.InternalCondition?.GetSystemTypeName<VentilationSystemType>()?.ToUpper();
                if(string.IsNullOrWhiteSpace(systemTypeName))
                {
                    SystemTypeLibrary systemTypeLibrary = Analytical.Query.DefaultSystemTypeLibrary();
                    
                    List<Zone> zones = adjacencyCluster.GetRelatedObjects<Zone>(space_Temp);
                    if(zones != null)
                    {
                        foreach(Zone zone_Temp in zones)
                        {
                            VentilationSystemType ventilationSystemType = systemTypeLibrary.GetSystemTypes<VentilationSystemType>(zone_Temp.Name, TextComparisonType.Equals, true)?.FirstOrDefault();
                            if(ventilationSystemType != null)
                            {
                                systemTypeName = ventilationSystemType.Name.ToUpper().Trim();
                                break;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(systemTypeName))
                        {
                            foreach (Zone zone_Temp in zones)
                            {
                                VentilationSystemType ventilationSystemType = systemTypeLibrary.GetSystemTypes<VentilationSystemType>(zone_Temp.Name, TextComparisonType.StartsWith, false)?.FirstOrDefault();
                                if (ventilationSystemType != null)
                                {
                                    systemTypeName = ventilationSystemType.Name.ToUpper().Trim();
                                    break;
                                }
                            }
                        }
                    }
                }

                if(string.IsNullOrWhiteSpace(systemTypeName))
                {
                    systemTypeName = "NV";
                }

                List<TM59SpaceApplication> tM59SpaceApplications = TM59Manager.TM59SpaceApplications(space?.InternalCondition);
                if (tM59SpaceApplications == null || tM59SpaceApplications.Count == 0)
                {
                    tM59SpaceApplications = TM59Manager.TM59SpaceApplications(space);
                }

                if (!Core.Query.TryGetValue(space_Temp, SpaceDataType.OccupantSensibleGain.Text(), out JArray jArray_OccupantSensibleGain) || jArray_OccupantSensibleGain == null)
                {
                    continue;
                }

                if (!Core.Query.TryGetValue(space_Temp, SpaceDataType.ResultantTemperature.Text(), out JArray jArray_ResultantTemperature) || jArray_ResultantTemperature == null)
                {
                    continue;
                }

                HashSet<int> occupiedHourIndices = new HashSet<int>();
                IndexedDoubles maxAcceptableTemperatures = new IndexedDoubles();
                IndexedDoubles minAcceptableTemperatures = new IndexedDoubles();
                IndexedDoubles operativeTemperatures = new IndexedDoubles();

                for (int i = 0; i < jArray_OccupantSensibleGain.Count; i++)
                {
                    if (!Core.Query.TryConvert(jArray_ResultantTemperature[i], out double resultantTemperature) || double.IsNaN(resultantTemperature))
                    {
                        continue;
                    }

                    maxAcceptableTemperatures.Add(i, maxIndoorComfortTemperatures[i]);
                    minAcceptableTemperatures.Add(i, minIndoorComfortTemperatures[i]);
                    operativeTemperatures.Add(i, resultantTemperature);


                    if (!Core.Query.TryConvert(jArray_OccupantSensibleGain[i], out double occupantSensibleGain) || double.IsNaN(occupantSensibleGain))
                    {
                        continue;
                    }

                    if (occupantSensibleGain <= 0)
                    {
                        continue;
                    }

                    occupiedHourIndices.Add(i);
                }

                TM59ExtendedResult tM59ExtendedResult = null;
                if (tM59SpaceApplications == null || tM59SpaceApplications.Count == 0 || (!string.IsNullOrWhiteSpace(systemTypeName) && systemTypeName.Equals("UV")))
                {
                    tM59ExtendedResult = new TM59CorridorExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(systemTypeName) && systemTypeName.Equals("NV"))
                    {
                        if (tM59SpaceApplications.Contains(TM59SpaceApplication.Sleeping))
                        {
                            tM59ExtendedResult = new TM59NaturalVentilationBedroomExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures);
                        }
                        else
                        {
                            tM59ExtendedResult = new TM59NaturalVentilationExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures, tM59SpaceApplications?.ToArray());
                        }
                    }
                    else
                    {
                        tM59ExtendedResult = new TM59MechanicalVentilationExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures, tM59SpaceApplications?.ToArray());
                    }
                }

                if(tM59ExtendedResult == null)
                {
                    continue;
                }

                result.Add(tM59ExtendedResult);
            }

            return result;
        }

        public IndexedDoubles GetMaxIndoorComfortTemperatures(Period period = Period.Hourly)
        {
            if (!AnalyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData) || weatherData == null)
            {
                return null;
            }

            WeatherYear weatherYear = weatherData?.WeatherYears?.FirstOrDefault();
            if (weatherYear == null)
            {
                return null;
            }

            List<double> values = Analytical.Query.MaxIndoorComfortTemperatures(weatherYear, TM52BuildingCategory);
            if (values == null || values.Count == 0)
            {
                return null;
            }

            IndexedDoubles result = new IndexedDoubles(values);

            return result.Repeat(period, Period.Daily);
        }

        public IndexedDoubles GetMaxIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
        {
            if (!AnalyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData) || weatherData == null)
            {
                return null;
            }

            WeatherYear weatherYear = weatherData?.WeatherYears?.FirstOrDefault();
            if (weatherYear == null)
            {
                return null;
            }

            List<double> values = Analytical.Query.MaxIndoorComfortTemperatures(weatherYear, TM52BuildingCategory, startDayIndex, endDayIndex);
            if (values == null || values.Count == 0)
            {
                return null;
            }

            IndexedDoubles result = new IndexedDoubles(values, startDayIndex);

            return result.Repeat(period, Period.Daily);
        }

        public IndexedDoubles GetMinIndoorComfortTemperatures(Period period = Period.Hourly)
        {
            if (!AnalyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData) || weatherData == null)
            {
                return null;
            }

            WeatherYear weatherYear = weatherData?.WeatherYears?.FirstOrDefault();
            if (weatherYear == null)
            {
                return null;
            }

            List<double> values = Analytical.Query.MinIndoorComfortTemperatures(weatherYear, TM52BuildingCategory);
            if (values == null || values.Count == 0)
            {
                return null;
            }

            IndexedDoubles result = new IndexedDoubles(values);

            return result.Repeat(period, Period.Daily);
        }

        public IndexedDoubles GetMinIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
        {
            if (!AnalyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData) || weatherData == null)
            {
                return null;
            }

            WeatherYear weatherYear = weatherData?.WeatherYears?.FirstOrDefault();
            if (weatherYear == null)
            {
                return null;
            }

            List<double> values = Analytical.Query.MinIndoorComfortTemperatures(weatherYear, TM52BuildingCategory, startDayIndex, endDayIndex);
            if (values == null || values.Count == 0)
            {
                return null;
            }

            IndexedDoubles result = new IndexedDoubles(values, startDayIndex);

            return result.Repeat(period, Period.Daily);
        }
    }
}
