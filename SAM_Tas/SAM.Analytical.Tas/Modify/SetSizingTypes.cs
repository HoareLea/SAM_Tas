using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void SetSizingTypes(this Building building, SizingType sizingType)
        {
            if(sizingType == SizingType.Undefined)
            {
                return;
            }

            List<zone> zones = building?.Zones();
            if(zones == null || zones.Count == 0)
            {
                return;
            }

            foreach (zone zone in zones)
            {
                switch (sizingType)
                {
                    case SizingType.Sizing:
                        zone.sizeHeating = (int)TBD.SizingType.tbdSizing;
                        zone.sizeCooling = (int)TBD.SizingType.tbdSizing;
                        break;

                    case SizingType.NoSizing:
                        zone.sizeHeating = (int)TBD.SizingType.tbdNoSizing;
                        zone.sizeCooling = (int)TBD.SizingType.tbdNoSizing;
                        break;

                    case SizingType.DesignSizingOnly:
                        zone.sizeHeating = (int)TBD.SizingType.tbdDesignSizingOnly;
                        zone.sizeCooling = (int)TBD.SizingType.tbdDesignSizingOnly;
                        break;
                }
            }
        }
    }
}