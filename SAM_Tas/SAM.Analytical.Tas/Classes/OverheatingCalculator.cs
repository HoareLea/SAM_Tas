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

        public List<TM52ExtendedResult> Calculate(IEnumerable<Space> spaces)
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
                    if(i < StartHourOfYear || i > EndHourOfYear)
                    {
                        continue;
                    }

                    if (Core.Query.TryConvert(jArray_ResultantTemperature[i], out double resultantTemperature) || double.IsNaN(resultantTemperature))
                    {
                        continue;
                    }

                    maxAcceptableTemperatures.Add(i, maxIndoorComfortTemperatures[i]);
                    minAcceptableTemperatures.Add(i, minAcceptableTemperatures[i]);
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

                TM52ExtendedResult tM52ExtendedResult = new TM52ExtendedResult(space_Temp.Name, Query.Source(), space.Guid.ToString(), occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures);
                result.Add(tM52ExtendedResult);
            }

            return result;
        }

        public IndexedDoubles GetMaxIndoorComfortTemperatures(Period period = Period.Hourly)
        {
            return GetMaxIndoorComfortTemperatures(Core.Query.DayOfYear(StartHourOfYear), Core.Query.DayOfYear(EndHourOfYear), period);
        }

        public IndexedDoubles GetMaxIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
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
            return GetMinIndoorComfortTemperatures(Core.Query.DayOfYear(StartHourOfYear), Core.Query.DayOfYear(EndHourOfYear), period);
        }

        public IndexedDoubles GetMinIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
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
