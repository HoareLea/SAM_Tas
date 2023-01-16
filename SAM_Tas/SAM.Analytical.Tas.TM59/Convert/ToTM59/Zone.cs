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


            string guidString = null;
            if(!space.TryGetValue(SpaceParameter.ZoneGuid, out guidString) || string.IsNullOrWhiteSpace(guidString))
            {
                guidString = space.Guid.ToString();
            }

            return new Zone(System.Guid.Parse(guidString), space.Name, 1, tM59Manager.RoomUse(space), SystemType.NaturalVentilation, true, 0.1);
        }
    }
}
