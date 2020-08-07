using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static tsdZoneArray? TsdZoneArray(this SpaceDataType spaceDataType)
        {
            if (spaceDataType == Tas.SpaceDataType.Undefined)
                return null;

            return (tsdZoneArray)(int)spaceDataType;
        }
    }
}