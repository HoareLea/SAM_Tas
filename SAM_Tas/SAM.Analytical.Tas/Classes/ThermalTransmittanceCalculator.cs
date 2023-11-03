using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public class ThermalTransmittanceCalculator
    {
        public ConstructionManager ConstructionManager { get; set; } = null;

        public double Tolerance { get; set; } = Core.Tolerance.MacroDistance;

        public ThermalTransmittanceCalculator(ConstructionManager constructionManager)
        {
            ConstructionManager = constructionManager;
        }
        
        private ApertureConstructionCalculationResult Calculate(ApertureConstructionCalculationData apertureConstructionCalculationData, TCD.Document document)
        {
            if(apertureConstructionCalculationData == null || document == null)
            {
                return null;
            }

            HeatFlowDirection heatFlowDirection = apertureConstructionCalculationData.HeatFlowDirection;
            if (heatFlowDirection == HeatFlowDirection.Undefined)
            {
                return null;
            }

            bool external = apertureConstructionCalculationData.External;

            Range<double> thicknessRange = apertureConstructionCalculationData.ThicknessRange;

            HashSet<string> apertureConstructionNames = apertureConstructionCalculationData.ApertureConstructionNames;
            if (apertureConstructionNames == null || apertureConstructionNames.Count == 0)
            {
                List<string> constructionNames_Temp = ConstructionManager.Constructions?.ConvertAll(x => x.Name);
                if (constructionNames_Temp != null)
                {
                    apertureConstructionNames = new HashSet<string>(constructionNames_Temp);
                }
            }

            if (apertureConstructionNames == null || apertureConstructionNames.Count == 0)
            {
                return null;
            }

            ApertureType apertureType = apertureConstructionCalculationData.ApertureType;

            string initialApertureConstructionName = apertureConstructionCalculationData.ApertureConstructionName;
            double initialPaneThermalTransmittance = double.NaN;
            double initialFrameThermalTransmittance = double.NaN;
            if (initialApertureConstructionName != null)
            {
                ApertureConstruction apertureConstruction = ConstructionManager.GetApertureConstructions(apertureType, initialApertureConstructionName)?.FirstOrDefault();
                if (apertureConstruction != null)
                {
                    List<TCD.Construction> constructions = apertureConstruction.ToTCD_Constructions(document, ConstructionManager);
                    if (constructions != null)
                    {
                        TCD.Construction construction = null;

                        construction = constructions.Find(x => x.name.EndsWith("-pane"));
                        if (construction != null)
                        {
                            initialPaneThermalTransmittance = Query.ThermalTransmittance(construction, heatFlowDirection, external, Tolerance);
                        }

                        construction = constructions.Find(x => x.name.EndsWith("-frame"));
                        if (construction != null)
                        {
                            initialFrameThermalTransmittance = Query.ThermalTransmittance(construction, heatFlowDirection, external, Tolerance);
                        }
                    }
                }
            }

            List<Tuple<ApertureConstruction, double, double, double>> tuples = new List<Tuple<ApertureConstruction, double, double, double>>();
            foreach (string apertureConstructionName_Temp in apertureConstructionNames)
            {
                ApertureConstruction apertureConstruction = ConstructionManager.GetApertureConstructions(apertureType, apertureConstructionName_Temp)?.FirstOrDefault();
                if(apertureConstruction == null)
                {
                    continue;
                }

                if (thicknessRange != null)
                {
                    double thickness = Math.Max(apertureConstruction.GetThickness(AperturePart.Pane), apertureConstruction.GetThickness(AperturePart.Frame));
                    if (!double.IsNaN(thickness))
                    {
                        if(!thicknessRange.In(thickness))
                        {
                            continue;
                        }
                    }
                }

                List<TCD.Construction> constructions = apertureConstruction.ToTCD_Constructions(document, ConstructionManager);
                if(constructions  == null || constructions.Count == 0)
                {
                    continue;
                }

                double[] calculatedThermalTransmittances = new double[] { 0, 0 };

                TCD.Construction construction_TCD = null;

                double calculatedPaneThermalTransmittance = double.NaN;
                double displayPaneThermalTransmittance = double.NaN;
                construction_TCD = constructions.Find(x => x.name.EndsWith("-pane"));
                if (construction_TCD != null)
                {
                    displayPaneThermalTransmittance = Query.ThermalTransmittance(construction_TCD, heatFlowDirection, external, Tolerance);
                    if (!double.IsNaN(displayPaneThermalTransmittance))
                    {
                        double paneThermalTransmittance = apertureConstructionCalculationData.PaneThermalTransmittance;
                        if (!double.IsNaN(paneThermalTransmittance))
                        {
                            calculatedPaneThermalTransmittance = displayPaneThermalTransmittance;
                            if (calculatedPaneThermalTransmittance > paneThermalTransmittance + Tolerance)
                            {
                                continue;
                            }
                            calculatedThermalTransmittances[0] = calculatedPaneThermalTransmittance;
                        }
                    }
                }

                double calculatedFrameThermalTransmittance = double.NaN;
                double displayFrameThermalTransmittance = double.NaN;
                construction_TCD = constructions.Find(x => x.name.EndsWith("-frame"));
                if (construction_TCD != null)
                {
                    displayFrameThermalTransmittance = Query.ThermalTransmittance(construction_TCD, heatFlowDirection, external, Tolerance);
                    if (!double.IsNaN(displayFrameThermalTransmittance))
                    {
                        double frameThermalTransmittance = apertureConstructionCalculationData.FrameThermalTransmittance;
                        if (!double.IsNaN(frameThermalTransmittance))
                        {
                            calculatedFrameThermalTransmittance = displayFrameThermalTransmittance;
                            if (calculatedFrameThermalTransmittance > frameThermalTransmittance + Tolerance)
                            {
                                continue;
                            }
                            calculatedThermalTransmittances[1] = calculatedFrameThermalTransmittance;
                        }
                    }
                }

                double calculatedThermalTransmittance = calculatedThermalTransmittances[0] > 0 && calculatedThermalTransmittances[1] > 0 ? (0.8 * calculatedThermalTransmittances[0]) + (0.2 * calculatedThermalTransmittances[1]) : calculatedThermalTransmittances[0] + calculatedThermalTransmittances[1];

                tuples.Add(new Tuple<ApertureConstruction, double, double, double>(apertureConstruction, displayPaneThermalTransmittance, displayFrameThermalTransmittance, calculatedThermalTransmittance));
            }

            if (tuples == null || tuples.Count == 0)
            {
                return null;
            }

            tuples.Sort((x, y) => x.Item3.CompareTo(y.Item3));

            return new ApertureConstructionCalculationResult(Query.Source(), apertureConstructionCalculationData.ApertureType, initialApertureConstructionName, initialPaneThermalTransmittance, initialFrameThermalTransmittance, tuples[0].Item1.Name, apertureConstructionCalculationData.PaneThermalTransmittance, apertureConstructionCalculationData.FrameThermalTransmittance, tuples[0].Item2, tuples[0].Item3);
        }
        
        private ConstructionCalculationResult Calculate(ConstructionCalculationData constructionCalculationData, TCD.Document document)
        {
            if(constructionCalculationData == null || document == null)
            {
                return null;
            }

            double thermalTransmittance = constructionCalculationData.ThermalTransmittance;
            if (double.IsNaN(thermalTransmittance))
            {
                return null;
            }

            HeatFlowDirection heatFlowDirection = constructionCalculationData.HeatFlowDirection;
            if(heatFlowDirection == HeatFlowDirection.Undefined)
            {
                return null;
            }

            bool external = constructionCalculationData.External;

            Range<double> thicknessRange = constructionCalculationData.ThicknessRange;

            HashSet<string> constructionNames = constructionCalculationData.ConstructionNames;
            if(constructionNames == null || constructionNames.Count == 0)
            {
                List<string> constructionNames_Temp = ConstructionManager.Constructions?.ConvertAll(x => x.Name);
                if(constructionNames_Temp != null)
                {
                    constructionNames = new HashSet<string>(constructionNames_Temp);
                }
            }

            if(constructionNames == null || constructionNames.Count == 0)
            {
                return null;
            }

            TCD.Construction construction_TCD = null;

            string initialConstructionName = constructionCalculationData.ConstructionName;
            double initialThermalTransmittance = double.NaN;
            if (initialConstructionName != null)
            {
                Construction construction = ConstructionManager.GetConstructions(initialConstructionName)?.FirstOrDefault();
                if (construction != null)
                {
                    construction_TCD = construction.ToTCD(document, ConstructionManager);
                    if (construction_TCD != null)
                    {
                        initialThermalTransmittance = Query.ThermalTransmittance(construction_TCD, heatFlowDirection, external, Tolerance);
                    }
                }
            }

            List<Tuple<TCD.Construction, double>> tuples = new List<Tuple<TCD.Construction, double>>();
            foreach(string constructionName_Temp in constructionNames)
            {
                Construction construction = ConstructionManager.GetConstructions(constructionName_Temp)?.FirstOrDefault();
                if(construction == null)
                {
                    continue;
                }
                
                if(thicknessRange != null)
                {
                    double thickness = construction.GetThickness();
                    if(!thicknessRange.In(thickness))
                    {
                        continue;
                    }
                }

                construction_TCD = construction.ToTCD(document, ConstructionManager);
                if(construction_TCD == null)
                {
                    continue;
                }

                double thermalTransmittance_Construction = Query.ThermalTransmittance(construction_TCD, heatFlowDirection, external, Tolerance);
                if(double.IsNaN(thermalTransmittance_Construction))
                {
                    continue;
                }

                tuples.Add(new Tuple<TCD.Construction, double>(construction_TCD, thermalTransmittance_Construction));
            }

            tuples.RemoveAll(x => x.Item2 > thermalTransmittance + Tolerance);

            if (tuples == null || tuples.Count == 0)
            {
                return new ConstructionCalculationResult(Query.Source(), initialConstructionName, initialThermalTransmittance, null, thermalTransmittance, double.NaN);
            }

            tuples.Sort((x, y) => x.Item2.CompareTo(y.Item2));

            string constructionName = tuples[0].Item1?.name;
            double calculatedThermalTransmittance = tuples[0].Item2;

            return new ConstructionCalculationResult(Query.Source(), initialConstructionName, initialThermalTransmittance, constructionName, thermalTransmittance, calculatedThermalTransmittance);
        }
        
        private LayerThicknessCalculationResult Calculate(LayerThicknessCalculationData layerThicknessCalculationData, TCD.Document document)
        {
            if(layerThicknessCalculationData == null || document == null)
            {
                return null;
            }

            Range<double> thicknessRange = layerThicknessCalculationData.ThicknessRange;
            if(thicknessRange == null || double.IsNaN(thicknessRange.Min) || double.IsNaN(thicknessRange.Max))
            {
                return null;
            }

            double thermalTransmittance = layerThicknessCalculationData.ThermalTransmittance;
            if(double.IsNaN(thermalTransmittance))
            {
                return null;
            }

            Construction construction = ConstructionManager.GetConstructions(layerThicknessCalculationData.ConstructionName, Core.TextComparisonType.Equals, true)?.FirstOrDefault();
            if (construction == null)
            {
                return new LayerThicknessCalculationResult(Query.Source(), layerThicknessCalculationData.ConstructionName, -1, double.NaN, double.NaN, thermalTransmittance, double.NaN);
            }

            TCD.Construction construction_TCD = construction.ToTCD(document, ConstructionManager);
            if (construction_TCD == null)
            {
                return new LayerThicknessCalculationResult(Query.Source(), layerThicknessCalculationData.ConstructionName, -1, double.NaN, double.NaN, thermalTransmittance, double.NaN);
            }

            HeatFlowDirection heatFlowDirection = layerThicknessCalculationData.HeatFlowDirection;
            bool external = layerThicknessCalculationData.External;

            double initialThermalTransmittance = Query.ThermalTransmittance(construction_TCD, heatFlowDirection, external, Tolerance);

            LayerThicknessCalculationResult layerThicknessCalculationResult = null;

            List<TCD.material> materials = construction_TCD.Materials();
            if(materials == null || materials.Count <= layerThicknessCalculationData.LayerIndex)
            {
                return new LayerThicknessCalculationResult(Query.Source(), layerThicknessCalculationData.ConstructionName, -1, double.NaN, initialThermalTransmittance, thermalTransmittance, double.NaN);
            }

            TCD.material material = materials[layerThicknessCalculationData.LayerIndex];
            if(material == null)
            {
                return new LayerThicknessCalculationResult(Query.Source(),layerThicknessCalculationData.ConstructionName, -1, double.NaN, initialThermalTransmittance, thermalTransmittance, double.NaN);
            }

            Func<double, double> func = new Func<double, double>(thickness_Temp => 
            {
                material.width = System.Convert.ToSingle(thickness_Temp);
                return Query.ThermalTransmittance(construction_TCD, heatFlowDirection, external, Tolerance);
            });

            double thickness = Core.Query.Calculate_ByDivision(func, thermalTransmittance, thicknessRange.Min, thicknessRange.Max);

            double calculatedThermalTransmittance = func.Invoke(thickness);

            return new LayerThicknessCalculationResult(Query.Source(), layerThicknessCalculationData.ConstructionName, layerThicknessCalculationData.LayerIndex, thickness, initialThermalTransmittance, thermalTransmittance, calculatedThermalTransmittance);
        }

        public List<LayerThicknessCalculationResult> Calculate(IEnumerable<LayerThicknessCalculationData> layerThicknessCalculationDatas)
        {
            if(ConstructionManager == null || layerThicknessCalculationDatas == null || layerThicknessCalculationDatas.Count() == 0)
            {
                return null;
            }

            List<LayerThicknessCalculationResult> result = new List<LayerThicknessCalculationResult>();

            Action<TCD.Document> action = new Action<TCD.Document>(x =>
            {

                foreach (LayerThicknessCalculationData layerThicknessCalculationData in layerThicknessCalculationDatas)
                {
                    LayerThicknessCalculationResult layerThicknessCalculationResult = Calculate(layerThicknessCalculationData, x);
                    result.Add(layerThicknessCalculationResult);

                }
            });

            Modify.Run(action);

            return result;
        }

        public List<ConstructionCalculationResult> Calculate(IEnumerable<ConstructionCalculationData> constructionCalculationDatas)
        {
            if (ConstructionManager == null || constructionCalculationDatas == null || constructionCalculationDatas.Count() == 0)
            {
                return null;
            }

            List<ConstructionCalculationResult> result = new List<ConstructionCalculationResult>();

            Action<TCD.Document> action = new Action<TCD.Document>(x =>
            {

                foreach (ConstructionCalculationData constructionCalculationData in constructionCalculationDatas)
                {
                    ConstructionCalculationResult constructionCalculationResult = Calculate(constructionCalculationData, x);
                    result.Add(constructionCalculationResult);

                }
            });

            Modify.Run(action);

            return result;
        }

        public List<IThermalTransmittanceCalculationResult> Calculate(IEnumerable<IThermalTransmittanceCalculationData> thermalTransmittanceCalculationDatas)
        {
            if (ConstructionManager == null || thermalTransmittanceCalculationDatas == null || thermalTransmittanceCalculationDatas.Count() == 0)
            {
                return null;
            }

            List<IThermalTransmittanceCalculationResult> result = new List<IThermalTransmittanceCalculationResult>();

            Action<TCD.Document> action = new Action<TCD.Document>(x =>
            {

                foreach (IThermalTransmittanceCalculationData thermalTransmittanceCalculationData in thermalTransmittanceCalculationDatas)
                {
                    if(thermalTransmittanceCalculationData is ConstructionCalculationData)
                    {
                        ConstructionCalculationResult constructionCalculationResult = Calculate((ConstructionCalculationData)thermalTransmittanceCalculationData, x);
                        result.Add(constructionCalculationResult);
                    }
                    else if(thermalTransmittanceCalculationData is LayerThicknessCalculationData)
                    {
                        LayerThicknessCalculationResult layerThicknessCalculationResult = Calculate((LayerThicknessCalculationData)thermalTransmittanceCalculationData, x);
                        result.Add(layerThicknessCalculationResult);
                    }
                    else if (thermalTransmittanceCalculationData is ApertureConstructionCalculationData)
                    {
                        ApertureConstructionCalculationResult apertureConstructionCalculationResult = Calculate((ApertureConstructionCalculationData)thermalTransmittanceCalculationData, x);
                        result.Add(apertureConstructionCalculationResult);
                    }
                }
            });

            Modify.Run(action);

            return result;
        }

        public IThermalTransmittanceCalculationResult Calculate(IThermalTransmittanceCalculationData thermalTransmittanceCalculationData)
        {
            if (thermalTransmittanceCalculationData == null)
            {
                return null;
            }

            return Calculate(new List<IThermalTransmittanceCalculationData>() { thermalTransmittanceCalculationData })?.FirstOrDefault();
        }

        public LayerThicknessCalculationResult Calculate(LayerThicknessCalculationData layerThicknessCalculationData)
        {
            if(layerThicknessCalculationData == null)
            {
                return null;
            }

            return Calculate(new LayerThicknessCalculationData[] { layerThicknessCalculationData })?.FirstOrDefault();
        }

    }
}
