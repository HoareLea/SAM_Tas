using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static PanelSimulationResult ToSAM(this SurfaceData surfaceData, int index)
        {
            if(surfaceData == null || index == -1)
            {
                return null;
            }

            PanelSimulationResult result = new PanelSimulationResult(surfaceData.BEName, Query.Source(), surfaceData.surfaceNumber.ToString());
            result.SetValue(Analytical.PanelSimulationResultParameter.Area, surfaceData.area);

            double value = double.NaN;

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ApertureFlowIn, index, double.NaN);
            if(!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ApertureFlowIn, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ApertureFlowOut, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ApertureFlowOut, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ApertureOpening, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ApertureOpening, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ExternalCondensation, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ExternalCondensation, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ExternalConduction, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ExternalConduction, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ExternalConvection, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ExternalConvection, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ExternalLongWave, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ExternalLongWave, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ExternalSolarGain, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ExternalSolarGain, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.ExternalTemperature, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.ExternalTemperature, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.InternalCondensation, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.InternalCondensation, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.InternalConduction, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.InternalConduction, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.InternalConvection, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.InternalConvection, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.InternalLongWave, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.InternalLongWave, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.InternalSolarGain, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.InternalSolarGain, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.InternalTemperature, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.InternalTemperature, value);
            }

            value = surfaceData.AnnualSurfaceResult(PanelDataType.InterstitialCondensation, index, double.NaN);
            if (!double.IsNaN(value))
            {
                result.SetValue(Analytical.PanelSimulationResultParameter.InterstitialCondensation, value);
            }

            return result;

        }
    }
}
