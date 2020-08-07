using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static tsdSurfaceArray? TsdSurfaceArray(this ResultType resultType)
        {
            if (resultType == Tas.ResultType.Undefined)
                return null;

            return (tsdSurfaceArray)(int)resultType;
        }
    }
}