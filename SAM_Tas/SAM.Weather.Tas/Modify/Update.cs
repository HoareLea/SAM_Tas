using System.Collections.Generic;

namespace SAM.Weather.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Updates TBD Weather Day from SAM WeatherDay
        /// </summary>
        /// <param name="weatherDay_TBD">Destination TBD WeatherDay</param>
        /// <param name="weatherDay">Source SAM WeatherDay </param>
        /// <returns>True if data updated</returns>
        public static bool Update(this TBD.WeatherDay weatherDay_TBD, WeatherDay weatherDay)
        {
            if(weatherDay_TBD == null || weatherDay == null)
            {
                return false;
            }

            for(int i = 1; i <= 24; i++)
            {
                double value = double.NaN;

                if(weatherDay.TryGetValue(WeatherDataType.CloudCover, i - 1, out value))
                {
                    weatherDay_TBD.cloudCover[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.DryBulbTemperature, i - 1, out value))
                {
                    weatherDay_TBD.dryBulb[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.WindSpeed, i - 1, out value))
                {
                    weatherDay_TBD.windSpeed[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.DiffuseSolarRadiation, i - 1, out value))
                {
                    weatherDay_TBD.diffuseRadiation[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.GlobalSolarRadiation, i - 1, out value))
                {
                    weatherDay_TBD.globalRadiation[i] = System.Convert.ToSingle(value);
                }
                else
                {
                    value = weatherDay.CalculatedGlobalRadiation(i - 1);
                    weatherDay_TBD.globalRadiation[i] = System.Convert.ToSingle(value);
                }

                if (weatherDay.TryGetValue(WeatherDataType.RelativeHumidity, i - 1, out value))
                {
                    weatherDay_TBD.humidity[i] = System.Convert.ToSingle(value);
                }


                if (weatherDay.TryGetValue(WeatherDataType.WindDirection, i - 1, out value))
                {
                    weatherDay_TBD.windDirection[i] = System.Convert.ToSingle(value);
                }
            }

            return true;

        }

        /// <summary>
        /// Updates SAM WeatherDay from TBD WeatherDay
        /// </summary>
        /// <param name="weatherDay">Destination SAM WeatherDay</param>
        /// <param name="weatherDay_TBD">Source TBD WeatherDay</param>
        /// <returns>True if data Updated</returns>
        public static bool Update(this WeatherDay weatherDay, TBD.WeatherDay weatherDay_TBD)
        {
            if (weatherDay_TBD == null || weatherDay == null)
            {
                return false;
            }

            for (int i = 1; i <= 24; i++)
            {
                weatherDay[WeatherDataType.CloudCover, i - 1] = weatherDay_TBD.cloudCover[i];
                weatherDay[WeatherDataType.DryBulbTemperature, i - 1] = weatherDay_TBD.dryBulb[i];
                weatherDay[WeatherDataType.WindSpeed, i - 1] = weatherDay_TBD.windSpeed[i];
                weatherDay[WeatherDataType.DiffuseSolarRadiation, i - 1] = weatherDay_TBD.diffuseRadiation[i];
                weatherDay[WeatherDataType.GlobalSolarRadiation, i - 1] = weatherDay_TBD.globalRadiation[i];
                weatherDay[WeatherDataType.RelativeHumidity, i - 1] = weatherDay_TBD.humidity[i];
                weatherDay[WeatherDataType.WindDirection, i - 1] = weatherDay_TBD.windDirection[i];
            }

            return true;
        }

        /// <summary>
        /// Updates SAM WeatherYear from TBD WeatherYear
        /// </summary>
        /// <param name="weatherYear">Destination SAM WeatherYear</param>
        /// <param name="weatherYear_TBD">Source TBD WeatherYear</param>
        /// <returns>True if data Updated</returns>
        public static bool Update(this WeatherYear weatherYear, TBD.WeatherYear weatherYear_TBD)
        {
            if(weatherYear == null || weatherYear_TBD == null)
            {
                return false;
            }

            weatherYear.Year = weatherYear_TBD.year;

            List<TBD.WeatherDay> weatherDays_TBD = weatherYear_TBD.WeatherDays();
            for(int i =0; i < weatherDays_TBD.Count; i++)
            {
                WeatherDay weatherDay = weatherYear[i];
                if(weatherDay == null)
                {
                    weatherDay = new WeatherDay();
                }

                Update(weatherDay, weatherDays_TBD[i]);

                weatherYear[i] = weatherDay;
            }

            return true;
        }

        /// <summary>
        /// Updates TBD WeatherYear from SAM WeatherYear
        /// </summary>
        /// <param name="weatherYear_TBD">Destination TBD WeatherYear</param>
        /// <param name="weatherYear">Source SAM WeatherYear</param>
        /// <returns>True if data Updated</returns>
        public static bool Update(this TBD.WeatherYear weatherYear_TBD, WeatherYear weatherYear)
        {
            if (weatherYear_TBD == null || weatherYear == null)
            {
                return false;
            }

            List<TBD.WeatherDay> weatherDays_TBD =  weatherYear_TBD.WeatherDays();
            if(weatherDays_TBD == null)
            {
                return false;
            }

            for(int i = 0; i < weatherDays_TBD.Count; i++)
            {
                Update(weatherDays_TBD[i], weatherYear[i]);
            }

            return true;
        }
    }
}