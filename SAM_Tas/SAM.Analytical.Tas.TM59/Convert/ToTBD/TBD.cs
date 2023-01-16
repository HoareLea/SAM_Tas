using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Convert
    {
        public static bool ToTBD(this AnalyticalModel analyticalModel, string path, TextMap textMap, Weather.WeatherData weatherData = null, IEnumerable<DesignDay> coolingDesignDays = null, IEnumerable<DesignDay> heatingDesignDays = null)
        {
            bool converted = Tas.Convert.ToTBD(analyticalModel, path, weatherData, coolingDesignDays, heatingDesignDays, true);
            if (!converted)
            {
                return false;
            }

            string directory_TM59 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "Report XMLs");
            if (!System.IO.Directory.Exists(directory_TM59))
            {
                System.IO.Directory.CreateDirectory(directory_TM59);
            }

            string path_TM59 = System.IO.Path.Combine(directory_TM59, System.IO.Path.GetFileNameWithoutExtension(path) + "DomOv.xml");

            return ToXml(analyticalModel, path_TM59, new TM59Manager(textMap));
        }
    }
}
