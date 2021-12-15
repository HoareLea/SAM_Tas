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

            weatherYear_TBD.latitude = System.Convert.ToSingle(weatherData.Latitude);
            weatherYear_TBD.longitude = System.Convert.ToSingle(weatherData.Longitude);
            weatherYear_TBD.name = weatherData.Name;
            weatherYear_TBD.description = weatherData.Description;
            weatherYear_TBD.altitude = System.Convert.ToSingle(weatherData.Elevtion);
            
            if(weatherData.TryGetValue(WeatherDataParameter.TimeZone, out string timeZone))
            {
                double @double = Core.Query.Double(Core.Query.UTC(timeZone));
                if(!double.IsNaN(@double))
                {
                    weatherYear_TBD.timeZone = System.Convert.ToSingle(@double);
                }
            }

            if (weatherData.TryGetValue(WeatherDataParameter.GroundTemperatures, out Core.SAMCollection<GroundTemperature> groundTemperatures) && groundTemperatures != null && groundTemperatures.Count != 0)
            {
                GroundTemperature groundTemperature = groundTemperatures.ToList().Find(x => System.Math.Abs(x.Depth - 2.0) < Core.Tolerance.MacroDistance);
                if(groundTemperature == null)
                {
                    groundTemperature = groundTemperatures.FirstOrDefault();
                }

                if(groundTemperature != null)
                {
                    weatherYear_TBD.groundTemperature = System.Convert.ToSingle(groundTemperature.Temperatures.Sum() / 12.0);
                }
            }

            return Update(weatherYear_TBD, weatherData.WeatherYears?.FirstOrDefault());
        }
    }
}