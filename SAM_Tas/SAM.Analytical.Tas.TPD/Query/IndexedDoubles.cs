using SAM.Core;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static IndexedDoubles IndexedDoubles(this ISystemEquipmentResult systemEquipmentResult, SystemSpaceDataType systemSpaceDataType)
        {
            if(systemEquipmentResult == null)
            {
                return null;
            }

            SystemIndexedResult systemIndexedResult = systemEquipmentResult as SystemIndexedResult;
            if(systemIndexedResult == null)
            {
                return null;
            }

            if(systemEquipmentResult is SystemChilledBeamResult)
            {
                switch(systemSpaceDataType)
                {
                    case SystemSpaceDataType.Condensation:
                        return systemIndexedResult[ChilledBeamDataType.Condensation.ToString()];

                    case SystemSpaceDataType.CoolingSensibleLoad:
                        return systemIndexedResult[ChilledBeamDataType.CoolingSensibleLoad.ToString()];

                    case SystemSpaceDataType.CoolingLatentLoad:
                        return systemIndexedResult[ChilledBeamDataType.CoolingLatentLoad.ToString()];

                    default:
                        return null;
                }
            }

            if (systemEquipmentResult is SystemDXCoilUnitResult)
            {
                switch (systemSpaceDataType)
                {
                    case SystemSpaceDataType.Condensation:
                        return systemIndexedResult[DXCoilUnitDataType.Condensation.ToString()];

                    case SystemSpaceDataType.CoolingSensibleLoad:
                        return systemIndexedResult[DXCoilUnitDataType.CoolingSensibleLoad.ToString()];

                    case SystemSpaceDataType.CoolingLatentLoad:
                        return systemIndexedResult[DXCoilUnitDataType.CoolingLatentLoad.ToString()];

                    case SystemSpaceDataType.HeatingLoad:
                        return systemIndexedResult[DXCoilUnitDataType.HeatingLoad.ToString()];

                    case SystemSpaceDataType.ElectricalLoad:
                        return systemIndexedResult[DXCoilUnitDataType.ElectricalLoad.ToString()];

                    default:
                        return null;
                }
            }

            if (systemEquipmentResult is SystemFanCoilUnitResult)
            {
                switch (systemSpaceDataType)
                {
                    case SystemSpaceDataType.Condensation:
                        return systemIndexedResult[FanCoilUnitDataType.Condensation.ToString()];

                    case SystemSpaceDataType.CoolingSensibleLoad:
                        return systemIndexedResult[FanCoilUnitDataType.CoolingSensibleLoad.ToString()];

                    case SystemSpaceDataType.CoolingLatentLoad:
                        return systemIndexedResult[FanCoilUnitDataType.CoolingLatentLoad.ToString()];

                    case SystemSpaceDataType.HeatingLoad:
                        return systemIndexedResult[FanCoilUnitDataType.HeatingLoad.ToString()];

                    case SystemSpaceDataType.ElectricalLoad:
                        return systemIndexedResult[FanCoilUnitDataType.ElectricalLoad.ToString()];

                    default:
                        return null;
                }
            }

            if (systemEquipmentResult is SystemRadiatorResult)
            {
                switch (systemSpaceDataType)
                {
                    case SystemSpaceDataType.HeatingLoad:
                        return systemIndexedResult[RadiatorDataType.HeatingLoad.ToString()];

                    default:
                        return null;
                }
            }

            return null;
        }
    }
}