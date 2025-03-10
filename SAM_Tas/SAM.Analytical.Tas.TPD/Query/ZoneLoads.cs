using SAM.Analytical.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<ZoneLoad> ZoneLoads(this SystemZone systemZone)
        { 
            if(systemZone == null)
            {
                return null;
            }

            return ZoneLoads((SystemComponent)systemZone);
        }

        public static List<ZoneLoad> ZoneLoads(this SystemComponent systemComponent)
        {
            if (systemComponent == null)
            {
                return null;
            }

            int index = 1;

            dynamic @dynamic = systemComponent;

            List<ZoneLoad> result = new List<ZoneLoad>();

            int count = @dynamic.GetZoneLoadCount();
            for (int i = 1; i <= count; i++)
            {
                ZoneLoad zoneLoad = @dynamic.GetZoneLoad(index);
                result.Add(zoneLoad);
            }

            return result;
        }

        public static List<ZoneLoad> ZoneLoads(this TSDData tSDData)
        {
            if(tSDData == null)
            {
                return null;
            }

            List<ZoneLoad> result = new List<ZoneLoad>();
            for (int i = 1; i <= tSDData.GetZoneLoadCount(); i++)
            {
                ZoneLoad zoneLoad = tSDData.GetZoneLoad(i);
                if(zoneLoad == null)
                {
                    continue;
                }

                result.Add(zoneLoad);
            }

            return result;
        }

        public static List<ZoneLoad> ZoneLoads<T>(this TSDData tSDData, IEnumerable<T> systemSpaces) where T : SystemSpace
        {
            if (tSDData == null)
            {
                return null;
            }

            List<ZoneLoad> result = new List<ZoneLoad>();
            for (int i = 1; i <= tSDData.GetZoneLoadCount(); i++)
            {
                ZoneLoad zoneLoad = tSDData.GetZoneLoad(i);
                if (zoneLoad == null)
                {
                    continue;
                }

                if(systemSpaces != null)
                {
                    bool exists = false;
                    foreach (T systemSpace in systemSpaces)
                    {
                        string spaceName = systemSpace.GetValue<string>(SystemSpaceParameter.SpaceName);
                        if(spaceName != null)
                        {
                            if (spaceName == zoneLoad.Name)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (systemSpace.Name == zoneLoad.Name)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if(!exists)
                    {
                        continue;
                    }
                }

                result.Add(zoneLoad);
            }

            return result;
        }
    }
}