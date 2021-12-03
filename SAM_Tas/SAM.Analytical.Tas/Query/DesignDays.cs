using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.DesignDay> DesignDays(this TBD.CoolingDesignDay coolingDesignDay)
        {
            List<TBD.DesignDay> result = new List<TBD.DesignDay>();

            int index = 0;
            TBD.DesignDay designDay = coolingDesignDay?.GetDesignDay(index);
            while (designDay != null)
            {
                result.Add(designDay);
                index++;

                designDay = coolingDesignDay?.GetDesignDay(index);
            }

            return result;
        }

        public static List<TBD.DesignDay> DesignDays(this TBD.HeatingDesignDay heatingDesignDay)
        {
            List<TBD.DesignDay> result = new List<TBD.DesignDay>();

            int index = 0;
            TBD.DesignDay designDay = heatingDesignDay?.GetDesignDay(index);
            while (designDay != null)
            {
                result.Add(designDay);
                index++;

                designDay = heatingDesignDay?.GetDesignDay(index);
            }

            return result;
        }
    }
}