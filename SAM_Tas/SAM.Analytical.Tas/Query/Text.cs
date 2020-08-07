namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string Text(this ResultType resultType)
        {
            return Core.Query.Description(resultType);
        }
    }
}