using System.Collections.Generic;

namespace SAM.Weather.Tas
{
    public static partial class Query
    {
        public static List<TWD.WeatherYear> WeatherYears(this TWD.WeatherFolder weatherFolder)
        {
            if (weatherFolder == null)
            {
                return null;
            }

            List<TWD.WeatherYear> result = new List<TWD.WeatherYear>();

            int index = 1;
            TWD.WeatherYear weatherYear = weatherFolder.weatherYears(index);
            while (weatherYear != null)
            {
                result.Add(weatherYear);
                index++;

                weatherYear = weatherFolder.weatherYears(index);
            }

            return result;

        }
    }
}