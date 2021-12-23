using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static Dictionary<Panel, double> UpdateThermalParameters(this AdjacencyCluster adjacencyCluster, TBD.Building building)
        {
            List<TBD.Construction> constructions_TBD = building?.Constructions();
            if (constructions_TBD == null)
            {
                return null;
            }

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels == null)
            {
                return null;
            }

            Dictionary<Panel, double> result = new Dictionary<Panel, double>();
            foreach (Panel panel in panels)
            {
                Construction construction = panel?.Construction;
                if (construction == null)
                {
                    continue;
                }

                string name = construction.Name;

                TBD.Construction construction_TBD = constructions_TBD.Find(x => x.name == name);
                if (construction_TBD == null)
                {
                    name = Analytical.Query.UniqueName(panel.PanelType, name);
                    construction_TBD = constructions_TBD.Find(x => x.name == name);
                }

                if (construction_TBD == null)
                {
                    continue;
                }

                double thermalTransmittance = Query.ThermalTransmittance(construction_TBD, panel.PanelType);
                if (!double.IsNaN(thermalTransmittance))
                {
                    panel.SetValue(PanelParameter.ThermalTransmittance, thermalTransmittance);
                }

                Query.GlazingValues(construction_TBD, 
                    out double lightTransmittance, 
                    out double lightReflectance,
                    out double directSolarEnergyTransmittance,
                    out double directSolarEnergyReflectance,
                    out double directSolarEnergyAbsorptance,
                    out double totalSolarEnergyTransmittance,
                    out double pilkingtonShortWavelengthCoefficient,
                    out double pilkingtonLongWavelengthCoefficient);

                if (!double.IsNaN(lightTransmittance))
                {
                    panel.SetValue(PanelParameter.LightTransmittance, lightTransmittance);
                }

                if (!double.IsNaN(lightReflectance))
                {
                    panel.SetValue(PanelParameter.LightReflectance, lightReflectance);
                }

                if (!double.IsNaN(directSolarEnergyTransmittance))
                {
                    panel.SetValue(PanelParameter.DirectSolarEnergyTransmittance, directSolarEnergyTransmittance);
                }

                if (!double.IsNaN(directSolarEnergyReflectance))
                {
                    panel.SetValue(PanelParameter.DirectSolarEnergyReflectance, directSolarEnergyReflectance);
                }

                if (!double.IsNaN(directSolarEnergyAbsorptance))
                {
                    panel.SetValue(PanelParameter.DirectSolarEnergyAbsorptance, directSolarEnergyAbsorptance);
                }

                if (!double.IsNaN(totalSolarEnergyTransmittance))
                {
                    panel.SetValue(PanelParameter.TotalSolarEnergyTransmittance, totalSolarEnergyTransmittance);
                }

                if (!double.IsNaN(pilkingtonShortWavelengthCoefficient))
                {
                    panel.SetValue(PanelParameter.PilkingtonShadingShortWavelengthCoefficient, pilkingtonShortWavelengthCoefficient);
                }

                if (!double.IsNaN(pilkingtonLongWavelengthCoefficient))
                {
                    panel.SetValue(PanelParameter.PilkingtonShadingLongWavelengthCoefficient, pilkingtonLongWavelengthCoefficient);
                }


                adjacencyCluster.AddObject(panel);
                result[panel] = thermalTransmittance;
            }

            return result;
        }
    }
}