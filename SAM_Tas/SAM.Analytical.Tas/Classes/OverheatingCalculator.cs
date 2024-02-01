using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Weather;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public class OverheatingCalculator
    {
        public int StartHourOfYear { get; set; } = 2880;
        public int EndHourOfYear { get; set; } = 6528;
        public TM52BuildingCategory TM52BuildingCategory { get; set; } = TM52BuildingCategory.CategoryII;
        public AnalyticalModel AnalyticalModel { get; set; } = null;

        public OverheatingCalculator(AnalyticalModel analyticalModel)
        {
            AnalyticalModel = analyticalModel;
        }

        public List<SpaceTM52Result> Calculate(IEnumerable<Space> spaces)
        {
            if (AnalyticalModel == null || spaces == null)
            {
                return null;
            }

            IndexedDoubles indoorComfortTemperatures = GetIndoorComfortTemperatures();

            List<SpaceTM52Result> result = new List<SpaceTM52Result>();
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
                IndexedDoubles maximumAcceptableTemperatures = new IndexedDoubles();
                IndexedDoubles operativeTemperatures = new IndexedDoubles();

                for (int i = 0; i < jArray_OccupantSensibleGain.Count; i++)
                {
                    if(i < StartHourOfYear || i > EndHourOfYear)
                    {
                        continue;
                    }
                    
                    if (!Core.Query.TryConvert(jArray_OccupantSensibleGain[i], out double occupantSensibleGain) || double.IsNaN(occupantSensibleGain))
                    {
                        continue;
                    }

                    if(occupantSensibleGain <= 0)
                    {
                        continue;
                    }

                    if (!Core.Query.TryConvert(jArray_ResultantTemperature[i], out double resultantTemperature) || double.IsNaN(resultantTemperature))
                    {
                        continue;
                    }

                    occupiedHourIndices.Add(i);
                    maximumAcceptableTemperatures.Add(i, indoorComfortTemperatures[i]);
                    operativeTemperatures.Add(i, resultantTemperature);
                }

                SpaceTM52Result spaceTM52Result = new SpaceTM52Result(space_Temp.Name, Query.Source(), space.Guid.ToString(), occupiedHourIndices, maximumAcceptableTemperatures, operativeTemperatures);
                result.Add(spaceTM52Result);
            }

            return result;
        }

        public IndexedDoubles GetIndoorComfortTemperatures(Period period = Period.Hourly)
        {
            return GetIndoorComfortTemperatures(Core.Query.DayOfYear(StartHourOfYear), Core.Query.DayOfYear(EndHourOfYear), period);
        }

        public IndexedDoubles GetIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
        {
            if (!AnalyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out WeatherData weatherData) || weatherData == null)
            {
                return null;
            }

            WeatherYear weatherYear = weatherData?.WeatherYears?.FirstOrDefault();
            if (weatherYear == null)
            {
                return null;
            }

            List<double> values = Analytical.Query.IndoorComfortTemperatures(weatherYear, TM52BuildingCategory, startDayIndex, endDayIndex);
            if (values == null || values.Count == 0)
            {
                return null;
            }

            IndexedDoubles result = new IndexedDoubles(values, startDayIndex);

            return result.Repeat(period, Period.Daily);
        }
    }
}
