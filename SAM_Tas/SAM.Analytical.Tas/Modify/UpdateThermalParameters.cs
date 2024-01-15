using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static Dictionary<Panel, double> UpdateThermalParameters(this AdjacencyCluster adjacencyCluster, TBD.Building building)
        {
            return UpdateThermalParameters(adjacencyCluster, building?.Constructions());
        }

        public static Dictionary<Panel, double> UpdateThermalParameters(this AdjacencyCluster adjacencyCluster, IEnumerable<TBD.Construction> constructions)
        {
            if(adjacencyCluster == null || constructions == null)
            {
                return null;
            }

            List<TBD.Construction> constructions_TBD = new List<TBD.Construction>(constructions);
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

                PanelType panelType = panel.PanelType;
                if (construction_TBD.type == TBD.ConstructionTypes.tcdTransparentConstruction)
                {
                    panelType = PanelType.CurtainWall;
                }

                double thermalTransmittance = Query.ThermalTransmittance(construction_TBD, panelType);
                if (!double.IsNaN(thermalTransmittance))
                {
                    panel.SetValue(Analytical.PanelParameter.ThermalTransmittance, thermalTransmittance);
                }

                Query.GlazingValues(construction_TBD,
                    out double lightTransmittance,
                    out double lightReflectance,
                    out double directSolarEnergyTransmittance,
                    out double directSolarEnergyReflectance,
                    out double directSolarEnergyAbsorptance,
                    out double totalSolarEnergyTransmittance,
                    out double pilkingtonShortWavelengthCoefficient,
                    out double pilkingtonLongWavelengthCoefficient,
                    0.0001);

                if (!double.IsNaN(lightTransmittance))
                {
                    panel.SetValue(Analytical.PanelParameter.LightTransmittance, lightTransmittance);
                }

                if (!double.IsNaN(lightReflectance))
                {
                    panel.SetValue(Analytical.PanelParameter.LightReflectance, lightReflectance);
                }

                if (!double.IsNaN(directSolarEnergyTransmittance))
                {
                    panel.SetValue(Analytical.PanelParameter.DirectSolarEnergyTransmittance, directSolarEnergyTransmittance);
                }

                if (!double.IsNaN(directSolarEnergyReflectance))
                {
                    panel.SetValue(Analytical.PanelParameter.DirectSolarEnergyReflectance, directSolarEnergyReflectance);
                }

                if (!double.IsNaN(directSolarEnergyAbsorptance))
                {
                    panel.SetValue(Analytical.PanelParameter.DirectSolarEnergyAbsorptance, directSolarEnergyAbsorptance);
                }

                if (!double.IsNaN(totalSolarEnergyTransmittance))
                {
                    panel.SetValue(Analytical.PanelParameter.TotalSolarEnergyTransmittance, totalSolarEnergyTransmittance);
                }

                if (!double.IsNaN(pilkingtonShortWavelengthCoefficient))
                {
                    panel.SetValue(Analytical.PanelParameter.PilkingtonShadingShortWavelengthCoefficient, pilkingtonShortWavelengthCoefficient);
                }

                if (!double.IsNaN(pilkingtonLongWavelengthCoefficient))
                {
                    panel.SetValue(Analytical.PanelParameter.PilkingtonShadingLongWavelengthCoefficient, pilkingtonLongWavelengthCoefficient);
                }

                List<Aperture> apertures = panel.Apertures;
                if (apertures != null && apertures.Count > 0)
                {
                    foreach (Aperture aperture in apertures)
                    {
                        if (aperture == null)
                        {
                            continue;
                        }

                        string name_Aperture = aperture.ApertureConstruction?.Name;
                        if (string.IsNullOrWhiteSpace(name_Aperture))
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
                            string prefix = Analytical.Query.UniqueNamePrefix(aperture.ApertureType);
                            construction_TBD_Aperture = constructions_TBD.Find(x => x.name.EndsWith(name_Aperture) && x.name.StartsWith(prefix));
                        }

                        if (construction_TBD_Aperture == null)
                        {
                            continue;
                        }

                        thermalTransmittance = Query.ThermalTransmittance(construction_TBD_Aperture, construction_TBD_Aperture.type == TBD.ConstructionTypes.tcdTransparentConstruction ? PanelType.CurtainWall : panel.PanelType);
                        if (!double.IsNaN(thermalTransmittance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.ThermalTransmittance, thermalTransmittance);
                        }

                        Query.GlazingValues(construction_TBD_Aperture,
                            out lightTransmittance,
                            out lightReflectance,
                            out directSolarEnergyTransmittance,
                            out directSolarEnergyReflectance,
                            out directSolarEnergyAbsorptance,
                            out totalSolarEnergyTransmittance,
                            out pilkingtonShortWavelengthCoefficient,
                            out pilkingtonLongWavelengthCoefficient,
                            0.0001);

                        if (!double.IsNaN(lightTransmittance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.LightTransmittance, lightTransmittance);
                        }

                        if (!double.IsNaN(lightReflectance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.LightReflectance, lightReflectance);
                        }

                        if (!double.IsNaN(directSolarEnergyTransmittance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.DirectSolarEnergyTransmittance, directSolarEnergyTransmittance);
                        }

                        if (!double.IsNaN(directSolarEnergyReflectance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.DirectSolarEnergyReflectance, directSolarEnergyReflectance);
                        }

                        if (!double.IsNaN(directSolarEnergyAbsorptance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.DirectSolarEnergyAbsorptance, directSolarEnergyAbsorptance);
                        }

                        if (!double.IsNaN(totalSolarEnergyTransmittance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.TotalSolarEnergyTransmittance, totalSolarEnergyTransmittance);
                        }

                        if (!double.IsNaN(pilkingtonShortWavelengthCoefficient))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.PilkingtonShadingShortWavelengthCoefficient, pilkingtonShortWavelengthCoefficient);
                        }

                        if (!double.IsNaN(pilkingtonLongWavelengthCoefficient))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.PilkingtonShadingLongWavelengthCoefficient, pilkingtonLongWavelengthCoefficient);
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

        public static Dictionary<Panel, double> UpdateThermalParameters(this AdjacencyCluster adjacencyCluster, IEnumerable<TCD.Construction> constructions)
        {
            if (adjacencyCluster == null || constructions == null)
            {
                return null;
            }

            Dictionary<Panel, double> result = UpdateThermalParameters(adjacencyCluster.GetPanels(), constructions);
            if(result != null)
            {
                foreach(Panel panel in result.Keys)
                {
                    adjacencyCluster.AddObject(panel);
                }
            }

            return result;
        }

        public static Dictionary<Panel, double> UpdateThermalParameters(this IEnumerable<Panel> panels, IEnumerable<TCD.Construction> constructions)
        {
            if (panels == null || constructions == null || panels.Count() == 0)
            {
                return null;
            }

            List<TCD.Construction> constructions_TCD = new List<TCD.Construction>(constructions);
            if (constructions_TCD == null)
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

                TCD.Construction construction_TCD = constructions_TCD.Find(x => x.name == name);
                if (construction_TCD == null)
                {
                    name = Analytical.Query.UniqueName(panel.PanelType, name);
                    construction_TCD = constructions_TCD.Find(x => x.name == name);
                }

                if (construction_TCD == null)
                {
                    continue;
                }

                PanelType panelType = panel.PanelType;
                if (construction_TCD.type == TCD.ConstructionTypes.tcdTransparentConstruction)
                {
                    panelType = PanelType.CurtainWall;
                }

                double thermalTransmittance = Query.ThermalTransmittance(construction_TCD, panelType);
                if (!double.IsNaN(thermalTransmittance))
                {
                    panel.SetValue(Analytical.PanelParameter.ThermalTransmittance, thermalTransmittance);
                }

                Query.GlazingValues(construction_TCD,
                    out double lightTransmittance,
                    out double lightReflectance,
                    out double directSolarEnergyTransmittance,
                    out double directSolarEnergyReflectance,
                    out double directSolarEnergyAbsorptance,
                    out double totalSolarEnergyTransmittance,
                    out double pilkingtonShortWavelengthCoefficient,
                    out double pilkingtonLongWavelengthCoefficient,
                    0.0001);

                if (!double.IsNaN(lightTransmittance))
                {
                    panel.SetValue(Analytical.PanelParameter.LightTransmittance, lightTransmittance);
                }

                if (!double.IsNaN(lightReflectance))
                {
                    panel.SetValue(Analytical.PanelParameter.LightReflectance, lightReflectance);
                }

                if (!double.IsNaN(directSolarEnergyTransmittance))
                {
                    panel.SetValue(Analytical.PanelParameter.DirectSolarEnergyTransmittance, directSolarEnergyTransmittance);
                }

                if (!double.IsNaN(directSolarEnergyReflectance))
                {
                    panel.SetValue(Analytical.PanelParameter.DirectSolarEnergyReflectance, directSolarEnergyReflectance);
                }

                if (!double.IsNaN(directSolarEnergyAbsorptance))
                {
                    panel.SetValue(Analytical.PanelParameter.DirectSolarEnergyAbsorptance, directSolarEnergyAbsorptance);
                }

                if (!double.IsNaN(totalSolarEnergyTransmittance))
                {
                    panel.SetValue(Analytical.PanelParameter.TotalSolarEnergyTransmittance, totalSolarEnergyTransmittance);
                }

                if (!double.IsNaN(pilkingtonShortWavelengthCoefficient))
                {
                    panel.SetValue(Analytical.PanelParameter.PilkingtonShadingShortWavelengthCoefficient, pilkingtonShortWavelengthCoefficient);
                }

                if (!double.IsNaN(pilkingtonLongWavelengthCoefficient))
                {
                    panel.SetValue(Analytical.PanelParameter.PilkingtonShadingLongWavelengthCoefficient, pilkingtonLongWavelengthCoefficient);
                }

                List<Aperture> apertures = panel.Apertures;
                if (apertures != null && apertures.Count > 0)
                {
                    foreach (Aperture aperture in apertures)
                    {
                        if (aperture == null)
                        {
                            continue;
                        }

                        string name_Aperture = aperture.ApertureConstruction?.Name;
                        if (string.IsNullOrWhiteSpace(name_Aperture))
                        {
                            continue;
                        }

                        TCD.Construction construction_TCD_Aperture = constructions_TCD.Find(x => x.name == name_Aperture);
                        if (construction_TCD_Aperture == null)
                        {
                            name_Aperture = string.Format("{0} -pane", name_Aperture);
                            construction_TCD_Aperture = constructions_TCD.Find(x => x.name == name_Aperture);
                        }

                        if (construction_TCD_Aperture == null)
                        {
                            string prefix = Analytical.Query.UniqueNamePrefix(aperture.ApertureType);
                            construction_TCD_Aperture = constructions_TCD.Find(x => x.name.EndsWith(name_Aperture) && x.name.StartsWith(prefix));
                        }

                        if (construction_TCD_Aperture == null)
                        {
                            continue;
                        }

                        thermalTransmittance = Query.ThermalTransmittance(construction_TCD_Aperture, construction_TCD_Aperture.type == TCD.ConstructionTypes.tcdTransparentConstruction ? PanelType.CurtainWall : panel.PanelType);
                        if (!double.IsNaN(thermalTransmittance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.ThermalTransmittance, thermalTransmittance);
                        }

                        Query.GlazingValues(construction_TCD_Aperture,
                            out lightTransmittance,
                            out lightReflectance,
                            out directSolarEnergyTransmittance,
                            out directSolarEnergyReflectance,
                            out directSolarEnergyAbsorptance,
                            out totalSolarEnergyTransmittance,
                            out pilkingtonShortWavelengthCoefficient,
                            out pilkingtonLongWavelengthCoefficient,
                            0.0001);

                        if (!double.IsNaN(lightTransmittance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.LightTransmittance, lightTransmittance);
                        }

                        if (!double.IsNaN(lightReflectance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.LightReflectance, lightReflectance);
                        }

                        if (!double.IsNaN(directSolarEnergyTransmittance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.DirectSolarEnergyTransmittance, directSolarEnergyTransmittance);
                        }

                        if (!double.IsNaN(directSolarEnergyReflectance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.DirectSolarEnergyReflectance, directSolarEnergyReflectance);
                        }

                        if (!double.IsNaN(directSolarEnergyAbsorptance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.DirectSolarEnergyAbsorptance, directSolarEnergyAbsorptance);
                        }

                        if (!double.IsNaN(totalSolarEnergyTransmittance))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.TotalSolarEnergyTransmittance, totalSolarEnergyTransmittance);
                        }

                        if (!double.IsNaN(pilkingtonShortWavelengthCoefficient))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.PilkingtonShadingShortWavelengthCoefficient, pilkingtonShortWavelengthCoefficient);
                        }

                        if (!double.IsNaN(pilkingtonLongWavelengthCoefficient))
                        {
                            aperture.SetValue(Analytical.ApertureParameter.PilkingtonShadingLongWavelengthCoefficient, pilkingtonLongWavelengthCoefficient);
                        }

                        panel.RemoveAperture(aperture.Guid);
                        panel.AddAperture(aperture);
                    }
                }

                result[panel] = thermalTransmittance;
            }

            return result;
        }

        public static Dictionary<Panel, double> UpdateThermalParameters(this AnalyticalModel analyticalModel)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            ConstructionLibrary constructionLibrary = analyticalModel.ConstructionLibrary;
            if(constructionLibrary == null || constructionLibrary.Count == 0)
            {
                return null;
            }

            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;
            if(materialLibrary == null || materialLibrary.Count == 0)
            {
                return null;
            }

            List<Panel> panels = analyticalModel.GetPanels();
            if(panels == null || panels.Count == 0)
            {
                return null;
            }

            Dictionary<Panel, double> result = null;

            Action<TCD.Document> action = new Action<TCD.Document>(x => 
            {
                List<TCD.Construction> constructions_TCD = new List<TCD.Construction>();
                foreach(Construction construction in constructionLibrary.GetConstructions())
                {
                    TCD.Construction constrcution_TCD = construction.ToTCD(x, analyticalModel);
                    if(constrcution_TCD == null)
                    {
                        continue;
                    }

                    constructions_TCD.Add(constrcution_TCD);
                }

                result = UpdateThermalParameters(panels, constructions_TCD);
                if(result != null && result.Count != 0)
                {
                    foreach(Panel panel in result.Keys)
                    {
                        analyticalModel.AddPanel(panel);
                    }
                }

            });

            Run(action);

            return result;
        }
    }
}