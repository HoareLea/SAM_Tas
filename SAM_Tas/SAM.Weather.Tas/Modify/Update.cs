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
            if (weatherYear == null || weatherYear_TBD == null)
            {
                return false;
            }

            weatherYear.Year = weatherYear_TBD.year;


            //TODO: TAS MEETING Implement new way of inserting data 
            //1.global radiation
            //2.difuse radiation
            //3.Cloud cover
            //4.Dry Bulb
            //5.Humidity
            //6.Wind speed
            //7.Wind direction

            List<WeatherDay> weatherDays = new List<WeatherDay>();
            for (int i = 0; i < 365; i++)
            {
                weatherDays.Add(new WeatherDay());
            }

            float[] values_GlobalSolarRadiation = weatherYear_TBD.GetAnnualParameter(2);
            float[] values_DiffuseSolarRadiation = weatherYear_TBD.GetAnnualParameter(3);
            float[] values_CloudCover = weatherYear_TBD.GetAnnualParameter(4);
            float[] values_DryBulbTemperature = weatherYear_TBD.GetAnnualParameter(0);
            float[] values_RelativeHumidity = weatherYear_TBD.GetAnnualParameter(1);
            float[] values_WindSpeed = weatherYear_TBD.GetAnnualParameter(5);
            float[] values_WindDirection = weatherYear_TBD.GetAnnualParameter(6);

            //Parallel.For(0, 365, (int i) =>
            for (int i = 0; i < 365; i++)
            {
                WeatherDay weatherDay = weatherDays[i];

                int hourIndex_Start = i * 24;
                int hourIndex_End = hourIndex_Start + 24;

                for (int hourIndex = hourIndex_Start; i < hourIndex_End; i++)
                {
                    int index = hourIndex - hourIndex_Start;

                    weatherDay[WeatherDataType.GlobalSolarRadiation, index] = values_GlobalSolarRadiation[hourIndex];
                    weatherDay[WeatherDataType.DiffuseSolarRadiation, index] = values_DiffuseSolarRadiation[hourIndex];
                    weatherDay[WeatherDataType.CloudCover, index] = values_CloudCover[hourIndex];
                    weatherDay[WeatherDataType.DryBulbTemperature, index] = values_DryBulbTemperature[hourIndex];
                    weatherDay[WeatherDataType.RelativeHumidity, index] = values_RelativeHumidity[hourIndex];
                    weatherDay[WeatherDataType.WindSpeed, index] = values_WindSpeed[hourIndex];
                    weatherDay[WeatherDataType.WindDirection, index] = values_WindDirection[hourIndex];
                }

            };//);

            for (int i = 0; i < weatherDays.Count; i++)
            {
                weatherYear[i] = weatherDays[i];
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

            List<float> values_GlobalSolarRadiation = new List<float>() { 0 };
            List<float> values_DiffuseSolarRadiation = new List<float>() { 0 };
            List<float> values_CloudCover = new List<float>() { 0 };
            List<float> values_DryBulbTemperature = new List<float>() { 0 };
            List<float> values_RelativeHumidity = new List<float>() { 0 };
            List<float> values_WindSpeed = new List<float>() { 0 };
            List<float> values_WindDirection = new List<float>() { 0 };

            for (int i = 0; i < 365; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    values_GlobalSolarRadiation.Add(System.Convert.ToSingle(weatherYear[i][WeatherDataType.GlobalSolarRadiation, j]));
                    values_DiffuseSolarRadiation.Add(System.Convert.ToSingle(weatherYear[i][WeatherDataType.DiffuseSolarRadiation, j]));

                    double cloudCover = weatherYear[i][WeatherDataType.CloudCover, j];
                    values_CloudCover.Add(System.Convert.ToSingle(cloudCover));
                    
                    values_DryBulbTemperature.Add(System.Convert.ToSingle(weatherYear[i][WeatherDataType.DryBulbTemperature, j]));
                    values_RelativeHumidity.Add(System.Convert.ToSingle(weatherYear[i][WeatherDataType.RelativeHumidity, j]));
                    values_WindSpeed.Add(System.Convert.ToSingle(weatherYear[i][WeatherDataType.WindSpeed, j]));
                    values_WindDirection.Add(System.Convert.ToSingle(weatherYear[i][WeatherDataType.WindDirection, j]));
                }
            }

            weatherYear_TBD.SetAnnualParameter(values_GlobalSolarRadiation.ToArray(), 2);
            weatherYear_TBD.SetAnnualParameter(values_DiffuseSolarRadiation.ToArray(), 3);
            weatherYear_TBD.SetAnnualParameter(values_CloudCover.ToArray(), 4);
            weatherYear_TBD.SetAnnualParameter(values_DryBulbTemperature.ToArray(), 0);
            weatherYear_TBD.SetAnnualParameter(values_RelativeHumidity.ToArray(), 1);
            weatherYear_TBD.SetAnnualParameter(values_WindSpeed.ToArray(), 5);
            weatherYear_TBD.SetAnnualParameter(values_WindDirection.ToArray(), 6);

            return true;
        }
    }
}