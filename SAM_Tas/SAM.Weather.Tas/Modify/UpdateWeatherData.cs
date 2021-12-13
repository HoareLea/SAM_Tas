using SAM.Core.Tas;
using System.Linq;

namespace SAM.Weather.Tas
{
    public static partial class Modify
    {
        public static bool UpdateWeatherData(string path_TBD, WeatherData weatherData)
        {
            if(string.IsNullOrWhiteSpace(path_TBD) || weatherData == null)
            {
                return false;
            }

            bool result = false;
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateWeatherData(sAMTBDDocument, weatherData);
                if (result)
                {
                    sAMTBDDocument.Save();
                }
            }

            return result;
        }

        public static bool UpdateWeatherData(this SAMTBDDocument sAMTBDDocument, WeatherData weatherData)
        {
            if(sAMTBDDocument == null || weatherData == null)
            {
                return false;
            }

            return UpdateWeatherData(sAMTBDDocument.TBDDocument, weatherData);
        }

        public static bool UpdateWeatherData(this TBD.TBDDocument tBDDocument, WeatherData weatherData)
        {
            if(tBDDocument == null || weatherData == null)
            {
                return false;
            }

            return UpdateWeatherData(tBDDocument.Building, weatherData);
        }

        public static bool UpdateWeatherData(this TBD.Building building, WeatherData weatherData)
        {
            if (building == null || weatherData == null)
            {
                return false;
            }

            TBD.WeatherYear weatherYear_TBD = building.GetWeatherYear();
            if (weatherYear_TBD == null)
            {
                weatherYear_TBD = building.AddWeatherYear();
            }

            weatherYear_TBD.altitude = System.Convert.ToSingle(weatherData.Latitude);
            weatherYear_TBD.longitude = System.Convert.ToSingle(weatherData.Longitude);

            return Update(weatherData.WeatherYears?.FirstOrDefault(), weatherYear_TBD);
        }
    }
}