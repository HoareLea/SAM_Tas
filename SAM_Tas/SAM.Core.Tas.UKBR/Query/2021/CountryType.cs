namespace SAM.Core.Tas.UKBR.v2021
{
    public static partial class Query
    {
        public static CountryType? CountryType(this Building building)
        {
            return Core.Query.Enum<CountryType>(building?.CountryIndex);
        }
    }
}