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
                return new LayerThicknessCalculationResult(Query.Source(), layerThicknessCalculationData.ConstructionName, -1, double.NaN, thermalTransmittance, double.NaN);
            }

            TCD.Construction construction_TCD = construction.ToTCD(document, ConstructionManager);
            if (construction_TCD == null)
            {
                return new LayerThicknessCalculationResult(Query.Source(), layerThicknessCalculationData.ConstructionName, -1, double.NaN, thermalTransmittance, double.NaN);
            }

            LayerThicknessCalculationResult layerThicknessCalculationResult = null;

            List<TCD.material> materials = construction_TCD.Materials();
            if(materials == null || materials.Count <= layerThicknessCalculationData.LayerIndex)
            {
                return new LayerThicknessCalculationResult(Query.Source(), layerThicknessCalculationData.ConstructionName, -1, double.NaN, thermalTransmittance, double.NaN);
            }

            TCD.material material = materials[layerThicknessCalculationData.LayerIndex];
            if(material == null)
            {
                return new LayerThicknessCalculationResult(Query.Source(),layerThicknessCalculationData.ConstructionName, -1, double.NaN, thermalTransmittance,double.NaN);
            }

            HeatFlowDirection heatFlowDirection = layerThicknessCalculationData.HeatFlowDirection;
            bool external = layerThicknessCalculationData.External;

            Func<double, double> func = new Func<double, double>(thickness_Temp => 
            {
                material.width = System.Convert.ToSingle(thickness_Temp);
                return Query.ThermalTransmittance(construction_TCD, heatFlowDirection, external, Tolerance);
            });

            double thickness = Core.Query.Calculate_ByDivision(func, thermalTransmittance, thicknessRange.Min, thicknessRange.Max);

            double calculatedThermalTransmittance = func.Invoke(thickness);

            return new LayerThicknessCalculationResult(Query.Source(), layerThicknessCalculationData.ConstructionName, layerThicknessCalculationData.LayerIndex, thickness, thermalTransmittance, calculatedThermalTransmittance);
        }

        public List<LayerThicknessCalculationResult> Calculate(IEnumerable<LayerThicknessCalculationData> layerThicknessCalculationDatas)
        {
            if(ConstructionManager == null || layerThicknessCalculationDatas == null || layerThicknessCalculationDatas.Count() == 0)
            {
                return null;
            }

            string directory = System.IO.Path.GetTempPath();
            if(!System.IO.Directory.Exists(directory))
            {
                return null;
            }

            directory = System.IO.Path.Combine(directory, "SAM");
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            string fileName = Guid.NewGuid().ToString("N") + ".tcd";

            string path = System.IO.Path.Combine(directory, fileName);

            List<LayerThicknessCalculationResult> result = new List<LayerThicknessCalculationResult>();
            
            try
            {
                using (SAMTCDDocument sAMTCDDocument = new SAMTCDDocument())
                {
                    if(sAMTCDDocument.Create(path))
                    {
                        TCD.Document document = sAMTCDDocument.Document;
                        foreach (LayerThicknessCalculationData layerThicknessCalculationData in layerThicknessCalculationDatas)
                        {
                            LayerThicknessCalculationResult layerThicknessCalculationResult = Calculate(layerThicknessCalculationData, document);
                            result.Add(layerThicknessCalculationResult);

                        }
                    }
                }
            }
            catch
            {

            }
            finally
            {
                if(System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            return result;
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
