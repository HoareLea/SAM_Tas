using SAM.Weather;
using System;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static DesignDay ToSAM(this TBD.DesignDay designDay, int year = 2018)
        {
            if(designDay == null)
            {
                return null;
            }

            int dayOfYear = designDay.yearDay;
            DateTime dateTime = new DateTime(year, 1, 1);
            dateTime.AddDays(dayOfYear);

            TBD.WeatherDay weatherDay_TBD = designDay.GetWeatherDay();

            DesignDay result = new DesignDay(designDay.name, System.Convert.ToInt16(dateTime.Year), System.Convert.ToByte(dateTime.Month), System.Convert.ToByte(dateTime.Day));

            for (int i = 1; i <= 24; i++)
            {
                result[WeatherDataType.CloudCover, i - 1] = weatherDay_TBD.cloudCover[i];
                result[WeatherDataType.DryBulbTemperature, i - 1] = weatherDay_TBD.dryBulb[i];
                result[WeatherDataType.WindSpeed, i - 1] = weatherDay_TBD.windSpeed[i];
                result[WeatherDataType.DiffuseSolarRadiation, i - 1] = weatherDay_TBD.diffuseRadiation[i];
                result[WeatherDataType.GlobalSolarRadiation, i - 1] = weatherDay_TBD.globalRadiation[i];
                result[WeatherDataType.RelativeHumidity, i - 1] = weatherDay_TBD.humidity[i];
                result[WeatherDataType.WindDirection, i - 1] = weatherDay_TBD.windDirection[i];
            }

            return result;
        }
    }
}
