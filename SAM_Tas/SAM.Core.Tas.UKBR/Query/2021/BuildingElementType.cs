namespace SAM.Core.Tas.UKBR.v2021
{
    public static partial class Query
    {
        public static BuildingElementType? BuildingElementType(this BuildingElement buildingElement)
        {
            return Core.Query.Enum<BuildingElementType>(buildingElement?.BETypeIndex);
        }
    }
}