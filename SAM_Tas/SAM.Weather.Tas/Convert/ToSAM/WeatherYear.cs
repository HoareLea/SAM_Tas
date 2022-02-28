using System.Collections.Generic;

namespace SAM.Weather.Tas
{
    public static partial class Convert
    {
        public static WeatherYear ToSAM(this TWD.WeatherYear weatherYear)
        {
            if(weatherYear == null)
            {
                return null;
            }

            Dictionary<WeatherDataType, List<double>> dictionary = new Dictionary<WeatherDataType, List<double>>();
            dictionary[WeatherDataType.GlobalSolarRadiation] = weatherYear.AnnualParameter<double>(WeatherDataType.GlobalSolarRadiation);
            dictionary[WeatherDataType.DiffuseSolarRadiation] = weatherYear.AnnualParameter<double>(WeatherDataType.DiffuseSolarRadiation);
            dictionary[WeatherDataType.CloudCover] = weatherYear.AnnualParameter<double>(WeatherDataType.CloudCover);
            dictionary[WeatherDataType.DryBulbTemperature] = weatherYear.AnnualParameter<double>(WeatherDataType.DryBulbTemperature);
            dictionary[WeatherDataType.RelativeHumidity] = weatherYear.AnnualParameter<double>(WeatherDataType.RelativeHumidity);
            dictionary[WeatherDataType.WindSpeed] = weatherYear.AnnualParameter<double>(WeatherDataType.WindSpeed);
            dictionary[WeatherDataType.WindDirection] = weatherYear.AnnualParameter<double>(WeatherDataType.WindDirection);

            return Create.WeatherYear(weatherYear.year, dictionary);
        }
    }
}