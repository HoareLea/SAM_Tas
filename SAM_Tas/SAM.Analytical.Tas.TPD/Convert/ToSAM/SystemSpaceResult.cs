using TPD;
using System.Linq;
using System.Collections.Generic;
using SAM.Core;
using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSpaceResult ToSAM_SpaceSystemResult(this SystemZone systemZone, SystemPlantRoom systemPlantRoom, int start, int end, params SpaceDataType[] systemSpaceDataTypes)
        {
            if (systemZone == null)
            {
                return null;
            }

            string reference = (systemZone as dynamic).GUID;

            ZoneLoad zoneLoad = systemZone.ZoneLoads()?.FirstOrDefault();
            if (zoneLoad == null)
            {
                return null;
            }

            IEnumerable<SpaceDataType> systemSpaceDataTypes_Temp = systemSpaceDataTypes == null || systemSpaceDataTypes.Length == 0 ? Enum.GetValues(typeof(SpaceDataType)).Cast<SpaceDataType>() : systemSpaceDataTypes;

            Dictionary<SpaceDataType, IndexedDoubles> dictionary = new Dictionary<SpaceDataType, IndexedDoubles>();
            foreach (SpaceDataType spaceDataType in systemSpaceDataTypes_Temp)
            {

                //object @object = ((global::TPD.SystemComponent)systemZone as dynamic).GetResultsData(tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType.tpdCombinerTypeMax, 2, 1, 8760);

                IndexedDoubles indexedDoubles = Create.IndexedDoubles((global::TPD.SystemComponent)systemZone, spaceDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(spaceDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[spaceDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }

            SystemSpaceResult result = new SystemSpaceResult(reference, zoneLoad.Name, Query.Source(), zoneLoad.FloorArea, zoneLoad.Volume, dictionary);

            return result;
        }

        //public static SystemSpaceResult ToSAM_SpaceSystemResult(this SystemZone systemZone, SystemPlantRoom systemPlantRoom, int start, int end, params SystemSpaceDataType[] systemSpaceDataTypes)
        //{
        //    if (systemZone == null)
        //    {
        //        return null;
        //    }

        //    string reference = (systemZone as dynamic).GUID;

        //    ZoneLoad zoneLoad = systemZone.ZoneLoads()?.FirstOrDefault();
        //    if (zoneLoad == null)
        //    {
        //        return null;
        //    }

        //    Dictionary<SystemSpaceDataType, IndexedDoubles> dictionary = new Dictionary<SystemSpaceDataType, IndexedDoubles>();

        //    SystemSpace systemSpace = systemPlantRoom.Find<SystemSpace>(x => x.Reference() == reference);
        //    if(systemSpace != null)
        //    {
        //        List<ISystemSpaceComponent> systemSpaceComponents = systemPlantRoom.GetSystemSpaceComponents<ISystemSpaceComponent>(systemSpace);
        //        if(systemSpaceComponents != null)
        //        {
        //            IEnumerable<SystemSpaceDataType> SystemSpaceDataTypes_Temp = systemSpaceDataTypes == null || systemSpaceDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SystemSpaceDataType)).Cast<SystemSpaceDataType>() : systemSpaceDataTypes;

        //            dictionary = new Dictionary<SystemSpaceDataType, IndexedDoubles>();

        //            foreach(ISystemSpaceComponent systemSpaceComponent in systemSpaceComponents)
        //            {
        //                List<ISystemComponentResult> systemComponentResults = systemPlantRoom.GetSystemResults<ISystemComponentResult>(systemSpaceComponent);
        //                if(systemComponentResults != null && systemComponentResults.Count != 0)
        //                {
        //                    foreach(ISystemComponentResult systemComponentResult in systemComponentResults)
        //                    {
        //                        foreach (SystemSpaceDataType systemSpaceDataType in SystemSpaceDataTypes_Temp)
        //                        {
        //                            IndexedDoubles indexedDoubles = Systems.Query.IndexedDoubles(systemComponentResult, systemSpaceDataType);
        //                            if(indexedDoubles != null)
        //                            {
        //                                if(!dictionary.TryGetValue(systemSpaceDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
        //                                {
        //                                    dictionary[systemSpaceDataType] = indexedDoubles;
        //                                }
        //                                else
        //                                {
        //                                    indexedDoubles_Temp.Sum(indexedDoubles);
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    SystemSpaceResult result = new SystemSpaceResult(reference, zoneLoad.Name, Query.Source(), zoneLoad.FloorArea, zoneLoad.Volume, dictionary);
        //    return result;
        //}
    }
}
