using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.CoolingDesignDay> CoolingDesignDays(this TBD.Building building)
        {
            List<TBD.CoolingDesignDay> result = new List<TBD.CoolingDesignDay>();

            int index = 0;
            TBD.CoolingDesignDay coolingDesignDay = building?.GetCoolingDesignDay(index);
            while (coolingDesignDay != null)
            {
                result.Add(coolingDesignDay);
                index++;

                coolingDesignDay = building?.GetCoolingDesignDay(index);
            }

            return result;
        }
    }
}