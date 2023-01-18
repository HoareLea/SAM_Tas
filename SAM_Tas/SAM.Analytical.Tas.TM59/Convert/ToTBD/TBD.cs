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

            if(!Modify.TryCreatePath(path, out string path_TM59))
            {
                return false;
            }

            return ToXml(analyticalModel, path_TM59, new TM59Manager(textMap));
        }
    }
}
