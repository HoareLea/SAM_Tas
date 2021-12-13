using System.Collections.Generic;

namespace SAM.Weather.Tas
{
    public static partial class Query
    {
        public static List<TBD.WeatherDay> WeatherDays(this TBD.WeatherYear weatherYear)
        {
            if (weatherYear == null)
            {
                return null;
            }

            List<TBD.WeatherDay> result = new List<TBD.WeatherDay>();

            int index = 1;
            TBD.WeatherDay weatherDay = weatherYear.weatherDays(index);
            while (weatherDay != null)
            {
                result.Add(weatherDay);
                index++;

                weatherDay = weatherYear.weatherDays(index);
            }

            return result;

        }
    }
}