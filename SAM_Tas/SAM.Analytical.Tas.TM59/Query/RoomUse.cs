using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Query
    {
        public static RoomUse RoomUse(this TM59Manager tM59Manager, Space space)
        {
            if(space == null || tM59Manager == null)
            {
                return Tas.RoomUse.Undefined;
            }

            List<TM59SpaceApplication> tM59SpaceApplications = tM59Manager.TM59SpaceApplications(space);
            if(tM59SpaceApplications == null || tM59SpaceApplications.Count == 0)
            {
                return Tas.RoomUse.Undefined;
            }

            if(tM59SpaceApplications.Contains(TM59SpaceApplication.Living) || tM59SpaceApplications.Contains(TM59SpaceApplication.Cooking))
            {
                return Tas.RoomUse.LivingRoomOrKitchen;
            }

            if (tM59SpaceApplications.Contains(TM59SpaceApplication.Sleeping))
            {
                return Tas.RoomUse.Bedroom;
            }

            return Tas.RoomUse.Other;
        }
    }
}