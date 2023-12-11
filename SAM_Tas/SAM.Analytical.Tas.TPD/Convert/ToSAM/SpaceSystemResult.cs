using TPD;
using System.Linq;
using System.Collections.Generic;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSpaceResult ToSAM_SpaceSystemResult(this SystemZone systemZone, SystemModel systemModel, int start, int end, params SystemSpaceDataType[] systemSpaceDataTypes)
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

            SystemSpace systemSpace = systemModel.Find<SystemSpace>(x => x.Reference() == zoneLoad.GUID);
            if(systemSpace != null)
            {
                List<ISystemEquipment> systemEquipments = systemModel.GetSystemEquipments<ISystemEquipment>(systemSpace);
                if(systemEquipments != null)
                {
                    IEnumerable<SystemSpaceDataType> SystemSpaceDataTypes_Temp = systemSpaceDataTypes == null || systemSpaceDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SystemSpaceDataType)).Cast<SystemSpaceDataType>() : systemSpaceDataTypes;

                    dictionary = new Dictionary<SystemSpaceDataType, IndexedDoubles>();

                    foreach(ISystemEquipment systemEquipment in systemEquipments)
                    {
                        List<ISystemEquipmentResult> systemEquipmentResults = systemModel.GetSystemResults<ISystemEquipmentResult>(systemEquipment);
                        if(systemEquipmentResults != null && systemEquipmentResults.Count != 0)
                        {
                            foreach(ISystemEquipmentResult systemEquipmentResult in systemEquipmentResults)
                            {
                                foreach (SystemSpaceDataType systemSpaceDataType in SystemSpaceDataTypes_Temp)
                                {
                                    IndexedDoubles indexedDoubles = Query.IndexedDoubles(systemEquipmentResult, systemSpaceDataType);
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
