using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static tsdSurfaceArray? TsdSurfaceArray(this PanelDataType panelDataType)
        {
            if (panelDataType == Tas.PanelDataType.Undefined)
                return null;

            return (tsdSurfaceArray)(int)panelDataType;
        }
    }
}