using SAM.Analytical.Systems;
using System.Collections.Generic;
using System.Threading;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool AssignZones(TPDDoc tPDDoc, AirSystemGroup airSystemGroup, List<Space> spaces)
        {
            if(tPDDoc == null || airSystemGroup == null)
            {
                return false;
            }

            EnergyCentre energyCentre = tPDDoc.EnergyCentre;
            if(energyCentre == null)
            {
                return false;
            }

            TSDData tSDData = energyCentre.GetTSDData(1);
            if (tSDData == null)
            {
                Thread.Sleep(2000);
                tSDData = energyCentre.GetTSDData(1);
                if (tSDData == null)
                {
                    return false;
                }
            }

            List<ComponentGroup> componentGroups = energyCentre.ComponentGroups();
            if(componentGroups == null)
            {
                return false;
            }

            string reference = airSystemGroup.Reference();

            ComponentGroup componentGroup = componentGroups.Find(x => x.Reference() == reference);
            if(componentGroup == null)
            {
                componentGroup = componentGroups.Find(x => ((dynamic)x).Name == airSystemGroup.Name);
            }

            if(componentGroup == null)
            {
                return false;
            }

            List<ZoneLoad> zoneLoads = null;

            List<ZoneLoad> zoneLoads_All = tSDData.ZoneLoads();
            if(zoneLoads_All != null)
            {
                zoneLoads = new List<ZoneLoad>();

                foreach (Space space in spaces)
                {
                    ZoneLoad zoneLoad = zoneLoads_All.Find(x => x.Name == space?.Name);
                    if(zoneLoad == null)
                    {
                        continue;
                    }

                    zoneLoads.Add(zoneLoad);
                }
            }

            if(zoneLoads.Count == 0)
            {
                return false;
            }

            componentGroup.SetMultiplicity(zoneLoads == null ? 0 : zoneLoads.Count);

            int index = 0;

            int count = componentGroup.GetComponentCount();

            for (int i = 1; i <= count; i++)
            {
                SystemComponent systemComponent = componentGroup.GetComponent(i);
                
                SystemZone systemZone = systemComponent as SystemZone;
                if(systemZone == null)
                {
                    continue;
                }

                ((dynamic)systemComponent).AddZoneLoad(zoneLoads[index]);

                index++;
            }

            return true;
        }
    }
}