using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    { 
        public static T HourlySurfaceResult<T>(this SurfaceData surfaceData, tsdSurfaceArray tsdSurfaceArray, int index, T @default = default)
        {
            if(surfaceData == null || index == -1)
            {
                return @default;
            }

            float value = surfaceData.GetHourlySurfaceResult(index, (int)tsdSurfaceArray);
            if (!Core.Query.TryConvert(value, out T result))
            {
                return @default;
            }

            return result;
        }

        public static T HourlySurfaceResult<T>(this SurfaceData surfaceData, PanelDataType panelDataType, int index, T @default = default)
        {
            if (surfaceData == null || panelDataType == Tas.PanelDataType.Undefined)
            {
                return @default;
            }

            return HourlySurfaceResult<T>(surfaceData, panelDataType.TsdSurfaceArray().Value, index);
        }
    }
}