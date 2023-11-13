using SAM.Core;
using System.Collections.Generic;

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

            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            if (constructionLayers != null && constructionLayers.Count != 0)
            {
                double min = double.MaxValue;
                int index_Temp = -1;
                for (int i = 0; i < constructionLayers.Count; i++)
                {
                    Material material = materialLibrary?.GetMaterial(constructionLayers[i]?.Name) as Material;
                    if (material == null)
                    {
                        continue;
                    }

                    if (constructionLayers[i].Thickness < 0.01)
                    {
                        continue;
                    }

                    if (material.ThermalConductivity < min)
                    {
                        index_Temp = i;
                        min = material.ThermalConductivity;
                    }
                }

                if (index_Temp != -1)
                {
                    layerIndex = index_Temp;
                }
            }

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