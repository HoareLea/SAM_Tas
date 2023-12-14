using TPD;
using System.Linq;
using System.Collections.Generic;
using SAM.Core;
using SAM.Analytical.Systems;
using SAM.Core.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSpaceResult ToSAM_SpaceSystemResult(this SystemZone systemZone, SystemPlantRoom systemPlantRoom, int start, int end, params SystemSpaceDataType[] systemSpaceDataTypes)
        {
            if (systemZone == null)
            {
                return null;
            }

            ZoneLoad zoneLoad = systemZone.ZoneLoads()?.FirstOrDefault();
            if (zoneLoad == null)
            {
                return null;
            }

            Dictionary<SystemSpaceDataType, IndexedDoubles> dictionary = new Dictionary<SystemSpaceDataType, IndexedDoubles>();

            SystemSpace systemSpace = systemPlantRoom.Find<SystemSpace>(x => x.Reference() == zoneLoad.GUID);
            if(systemSpace != null)
            {
                List<ISystemSpaceComponent> systemSpaceComponents = systemPlantRoom.GetSystemSpaceComponents<ISystemSpaceComponent>(systemSpace);
                if(systemSpaceComponents != null)
                {
                    IEnumerable<SystemSpaceDataType> SystemSpaceDataTypes_Temp = systemSpaceDataTypes == null || systemSpaceDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SystemSpaceDataType)).Cast<SystemSpaceDataType>() : systemSpaceDataTypes;

                    dictionary = new Dictionary<SystemSpaceDataType, IndexedDoubles>();

                    foreach(ISystemSpaceComponent systemSpaceComponent in systemSpaceComponents)
                    {
                        List<ISystemComponentResult> systemComponentResults = systemPlantRoom.GetSystemResults<ISystemComponentResult>(systemSpaceComponent);
                        if(systemComponentResults != null && systemComponentResults.Count != 0)
                        {
                            foreach(ISystemComponentResult systemComponentResult in systemComponentResults)
                            {
                                foreach (SystemSpaceDataType systemSpaceDataType in SystemSpaceDataTypes_Temp)
                                {
                                    IndexedDoubles indexedDoubles = Systems.Query.IndexedDoubles(systemComponentResult, systemSpaceDataType);
                                    if(indexedDoubles != null)
                                    {
                                        if(!dictionary.TryGetValue(systemSpaceDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                                        {
                                            dictionary[systemSpaceDataType] = indexedDoubles;
                                        }
                                        else
                                        {
                                            indexedDoubles_Temp.Sum(indexedDoubles);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            SystemSpaceResult result = new SystemSpaceResult(zoneLoad.GUID, zoneLoad.Name, Query.Source(), zoneLoad.FloorArea, zoneLoad.Volume, dictionary);
            return result;
        }
    }
}
