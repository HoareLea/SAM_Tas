using System;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Convert
    {
        public static Zone ToTM59(this Space space, TM59Manager tM59Manager)
        {
            if (space == null || tM59Manager == null)
            {
                return null;
            }

            InternalCondition internalCondition = space.InternalCondition;
            if(internalCondition == null)
            {
                return null;
            }


            Guid guid = Guid.Empty; ;
            if(!space.TryGetValue(SpaceParameter.ZoneGuid, out guid) || guid == Guid.Empty)
            {
                guid = space.Guid;
            }

            SystemType systemType = SystemType.NaturalVentilation;
            if(internalCondition.TryGetValue(InternalConditionParameter.VentilationSystemTypeName, out string ventilationSystemTypeName))
            {
                if(ventilationSystemTypeName != "UU" && ventilationSystemTypeName != "NV")
                {
                    systemType = SystemType.MechanicalVentilation;
                }
            }

            return new Zone(guid, space.Name, 1, tM59Manager.RoomUse(space), systemType, true, 0.1);
        }
    }
}
