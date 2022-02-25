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
            dictionary[WeatherDataType.GlobalSolarRadiation] = weatherYear.AnnualParameter<double>(2);
            dictionary[WeatherDataType.DiffuseSolarRadiation] = weatherYear.AnnualParameter<double>(3);
            dictionary[WeatherDataType.CloudCover] = weatherYear.AnnualParameter<double>(4);
            dictionary[WeatherDataType.DryBulbTemperature] = weatherYear.AnnualParameter<double>(0);
            dictionary[WeatherDataType.RelativeHumidity] = weatherYear.AnnualParameter<double>(1);
            dictionary[WeatherDataType.WindSpeed] = weatherYear.AnnualParameter<double>(5);
            dictionary[WeatherDataType.WindDirection] = weatherYear.AnnualParameter<double>(6);

            return Create.WeatherYear(weatherYear.year, dictionary);
        }
    }
}