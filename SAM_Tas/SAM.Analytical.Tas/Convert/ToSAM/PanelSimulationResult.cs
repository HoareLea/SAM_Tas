using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static SurfaceSimulationResult ToSAM(this SurfaceData surfaceData, int index)
        {
            if(surfaceData == null || index == -1)
            {
                return null;
            }

            SurfaceSimulationResult result = new SurfaceSimulationResult(surfaceData.BEName, Query.Source(), surfaceData.surfaceNumber.ToString());
            result.SetValue(Analytical.SurfaceSimulationResultParameter.Area, surfaceData.area);

            double value = double.NaN;

            value = surfaceData.HourlySurfaceResult(PanelDataType.ApertureFlowIn, index, double.NaN);
            if(!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ApertureFlowIn, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.ApertureFlowOut, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ApertureFlowOut, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.ApertureOpening, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ApertureOpening, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.ExternalCondensation, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ExternalCondensation, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.ExternalConduction, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ExternalConduction, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.ExternalConvection, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ExternalConvection, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.ExternalLongWave, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ExternalLongWave, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.ExternalSolarGain, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ExternalSolarGain, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.ExternalTemperature, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.ExternalTemperature, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.InternalCondensation, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.InternalCondensation, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.InternalConduction, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.InternalConduction, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.InternalConvection, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.InternalConvection, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.InternalLongWave, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.InternalLongWave, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.InternalSolarGain, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.InternalSolarGain, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.InternalTemperature, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.InternalTemperature, value);
            }

            value = surfaceData.HourlySurfaceResult(PanelDataType.InterstitialCondensation, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.SurfaceSimulationResultParameter.InterstitialCondensation, value);
            }

            return result;

        }
    }
}
