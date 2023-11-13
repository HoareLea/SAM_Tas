using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static ApertureConstructionCalculationData ApertureConstructionCalculationData(this ApertureConstruction apertureConstruction, ConstructionManager constructionManager)
        {
            if(apertureConstruction == null)
            {
                return null;
            }

            string apertureConstructionName = null;
            double paneThermalTransmittance = double.NaN;
            double frameThermalTransmittance = double.NaN;
            HeatFlowDirection heatFlowDirection = HeatFlowDirection.Undefined;
            bool external = true;
            HashSet<string> apertureConstructionNames = null;

            apertureConstructionName = apertureConstruction.Name;

            List<ApertureConstruction> apertureConstructions = constructionManager.ApertureConstructions?.FindAll(x => x.ApertureType == apertureConstruction.ApertureType);
            if (apertureConstructions != null && apertureConstructions.Count != 0)
            {
                apertureConstructionNames = new HashSet<string>();
                foreach(ApertureConstruction apertureConstruction_Temp in apertureConstructions)
                {
                    if(string.IsNullOrWhiteSpace(apertureConstruction_Temp?.Name))
                    {
                        continue;
                    }

                    apertureConstructionNames.Add(apertureConstruction_Temp.Name);
                }
            }

            PanelType panelType = PanelType.Undefined;
            if (apertureConstruction.TryGetValue(Analytical.ApertureConstructionParameter.DefaultPanelType, out string string_PanelType))
            {
                if (!Core.Query.TryGetEnum(string_PanelType, out panelType))
                {
                    panelType = PanelType.Undefined;
                }
            }

            paneThermalTransmittance = Query.ThermalTransmittance(panelType, out heatFlowDirection, out bool external_Temp);
            if (!double.IsNaN(paneThermalTransmittance))
            {
                external = external_Temp;
                frameThermalTransmittance = paneThermalTransmittance;
            }

            return new ApertureConstructionCalculationData(apertureConstruction.ApertureType, apertureConstructionName, apertureConstructionNames,paneThermalTransmittance, frameThermalTransmittance, heatFlowDirection, external);
        }
    }
}