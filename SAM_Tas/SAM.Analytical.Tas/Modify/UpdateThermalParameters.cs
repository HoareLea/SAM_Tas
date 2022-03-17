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

                List<Aperture> apertures = panel.Apertures;
                if(apertures != null && apertures.Count > 0)
                {
                    foreach(Aperture aperture in apertures)
                    {
                        if(aperture == null)
                        {
                            continue;
                        }

                        string name_Aperture = aperture.ApertureConstruction?.Name;
                        if(string.IsNullOrWhiteSpace(name_Aperture))
                        {
                            continue;
                        }

                        TBD.Construction construction_TBD_Aperture = constructions_TBD.Find(x => x.name == name_Aperture);
                        if (construction_TBD_Aperture == null)
                        {
                            name_Aperture = string.Format("{0} -pane", name_Aperture);
                            construction_TBD_Aperture = constructions_TBD.Find(x => x.name == name_Aperture);
                        }

                        if (construction_TBD_Aperture == null)
                        {
                            continue;
                        }

                        thermalTransmittance = Query.ThermalTransmittance(construction_TBD_Aperture, PanelType.CurtainWall);
                        if (!double.IsNaN(thermalTransmittance))
                        {
                            aperture.SetValue(ApertureParameter.ThermalTransmittance, thermalTransmittance);
                        }

                        Query.GlazingValues(construction_TBD_Aperture,
                            out lightTransmittance,
                            out lightReflectance,
                            out directSolarEnergyTransmittance,
                            out directSolarEnergyReflectance,
                            out directSolarEnergyAbsorptance,
                            out totalSolarEnergyTransmittance,
                            out pilkingtonShortWavelengthCoefficient,
                            out pilkingtonLongWavelengthCoefficient);

                        if (!double.IsNaN(lightTransmittance))
                        {
                            aperture.SetValue(ApertureParameter.LightTransmittance, lightTransmittance);
                        }

                        if (!double.IsNaN(lightReflectance))
                        {
                            aperture.SetValue(ApertureParameter.LightReflectance, lightReflectance);
                        }

                        if (!double.IsNaN(directSolarEnergyTransmittance))
                        {
                            aperture.SetValue(ApertureParameter.DirectSolarEnergyTransmittance, directSolarEnergyTransmittance);
                        }

                        if (!double.IsNaN(directSolarEnergyReflectance))
                        {
                            aperture.SetValue(ApertureParameter.DirectSolarEnergyReflectance, directSolarEnergyReflectance);
                        }

                        if (!double.IsNaN(directSolarEnergyAbsorptance))
                        {
                            aperture.SetValue(ApertureParameter.DirectSolarEnergyAbsorptance, directSolarEnergyAbsorptance);
                        }

                        if (!double.IsNaN(totalSolarEnergyTransmittance))
                        {
                            aperture.SetValue(ApertureParameter.TotalSolarEnergyTransmittance, totalSolarEnergyTransmittance);
                        }

                        if (!double.IsNaN(pilkingtonShortWavelengthCoefficient))
                        {
                            aperture.SetValue(ApertureParameter.PilkingtonShadingShortWavelengthCoefficient, pilkingtonShortWavelengthCoefficient);
                        }

                        if (!double.IsNaN(pilkingtonLongWavelengthCoefficient))
                        {
                            aperture.SetValue(ApertureParameter.PilkingtonShadingLongWavelengthCoefficient, pilkingtonLongWavelengthCoefficient);
                        }

                        panel.RemoveAperture(aperture.Guid);
                        panel.AddAperture(aperture);
                    }
                }


                adjacencyCluster.AddObject(panel);
                result[panel] = thermalTransmittance;
            }

            return result;
        }
    }
}