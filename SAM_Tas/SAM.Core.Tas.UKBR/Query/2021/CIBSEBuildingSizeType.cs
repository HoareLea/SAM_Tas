namespace SAM.Core.Tas.UKBR.v2021
{
    public static partial class Query
    {
        public static CIBSEBuildingSizeType? CIBSEBuildingSizeType(this Building building)
        {
            return Core.Query.Enum<CIBSEBuildingSizeType>(building.CIBSEBuildingSizeIndex);
        }
    }
}