using System.Collections.Generic;

namespace SAM.Weather.Tas
{
    public static partial class Query
    {
        public static List<TWD.WeatherFolder> WeatherFolders(this TWD.WeatherFolder weatherFolder, bool recursive = false)
        {
            if (weatherFolder == null)
            {
                return null;
            }

            List<TWD.WeatherFolder> result = new List<TWD.WeatherFolder>();

            int index = 1;
            TWD.WeatherFolder weatherFolder_Temp = weatherFolder.childFolders(index);
            while (weatherFolder_Temp != null)
            {
                result.Add(weatherFolder_Temp);
                if(recursive)
                {
                    List<TWD.WeatherFolder> weatherFolders = WeatherFolders(weatherFolder_Temp, recursive);
                    if(weatherFolders != null && weatherFolders.Count != 0)
                    {
                        result.AddRange(weatherFolders);
                    }
                }

                index++;

                weatherFolder_Temp = weatherFolder.childFolders(index);
            }

            return result;

        }
    }
}