using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static LayerThicknessCalculationData LayerThicknessCalculationData(this Construction construction, MaterialLibrary materialLibrary)
        {
            if(construction == null)
            {
                return null;
            }

            string constructionName = null;
            int layerIndex = -1;
            double thermalTransmittance = double.NaN;
            HeatFlowDirection heatFlowDirection = HeatFlowDirection.Undefined;
            bool external = true;

            constructionName = construction.Name;

            layerIndex = Query.AdjustableLayerIndex(construction, materialLibrary);

            PanelType panelType = PanelType.Undefined;
            if (construction.TryGetValue(Analytical.ConstructionParameter.DefaultPanelType, out string string_PanelType))
            {
                if (!Core.Query.TryGetEnum(string_PanelType, out panelType))
                {
                    panelType = PanelType.Undefined;
                }
            }

            thermalTransmittance = Query.ThermalTransmittance(panelType, out heatFlowDirection, out bool external_Temp);
            if (!double.IsNaN(thermalTransmittance))
            {
                external = external_Temp;
            }

            return new LayerThicknessCalculationData(constructionName, layerIndex, thermalTransmittance, heatFlowDirection, external);
        }
    }
}