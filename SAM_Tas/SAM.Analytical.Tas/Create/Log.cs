using SAM.Core;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static Log Log(this IThermalTransmittanceCalculationData thermalTransmittanceCalculationData, ConstructionManager constructionManager = null)
        {
            if (thermalTransmittanceCalculationData == null)
            {
                return null;
            }

            if(thermalTransmittanceCalculationData is ConstructionCalculationData)
            {
                return Log((ConstructionCalculationData)thermalTransmittanceCalculationData, constructionManager);
            }

            if (thermalTransmittanceCalculationData is ApertureConstructionCalculationData)
            {
                return Log((ApertureConstructionCalculationData)thermalTransmittanceCalculationData, constructionManager);
            }

            if (thermalTransmittanceCalculationData is LayerThicknessCalculationData)
            {
                return Log((LayerThicknessCalculationData)thermalTransmittanceCalculationData, constructionManager);
            }

            return null;
        }

        public static Log Log(this ConstructionCalculationData constructionCalculationData, ConstructionManager constructionManager = null)
        {
            if (constructionCalculationData == null)
            {
                return null;
            }

            Log result = new Log();

            if (string.IsNullOrWhiteSpace(constructionCalculationData.ConstructionName))
            {
                result.Add(string.Format("Source Construction name has not been provided"), LogRecordType.Message);
            }

            string constructionName = string.IsNullOrWhiteSpace(constructionCalculationData.ConstructionName) ? "???" : constructionCalculationData.ConstructionName;

            if (constructionCalculationData.ConstructionNames == null || constructionCalculationData.ConstructionNames.Count == 0)
            {
                result.Add(string.Format("Construction names have not been provided for construction {0}", constructionName), LogRecordType.Error);
            }

            if (double.IsNaN(constructionCalculationData.ThermalTransmittance) || constructionCalculationData.ThermalTransmittance == 0)
            {
                result.Add(string.Format("Thermal transmittance has invalid value for construction {0}", constructionName), LogRecordType.Error);
            }

            if(constructionManager != null)
            {
                if(!string.IsNullOrWhiteSpace(constructionCalculationData.ConstructionName))
                {
                    Construction construction = constructionManager.GetConstructions(constructionCalculationData.ConstructionName)?.FirstOrDefault();
                    if(construction == null)
                    {
                        result.Add(string.Format("Could not find construction {0} in ConstructionManager", constructionName), LogRecordType.Error);
                    }

                    Core.Modify.AddRange(result, construction?.Log(constructionManager.MaterialLibrary));
                }

                if(constructionCalculationData.ConstructionNames != null)
                {
                    foreach(string constructionName_Temp in constructionCalculationData.ConstructionNames)
                    {
                        Construction construction = constructionManager.GetConstructions(constructionName_Temp)?.FirstOrDefault();
                        if (construction == null)
                        {
                            result.Add(string.Format("Could not find construction {0} in ConstructionManager", constructionName_Temp), LogRecordType.Error);
                        }

                        Core.Modify.AddRange(result, construction?.Log(constructionManager.MaterialLibrary));
                    }
                }
            }

            return result;
        }

        public static Log Log(this ApertureConstructionCalculationData apertureConstructionCalculationData, ConstructionManager constructionManager = null)
        {
            if (apertureConstructionCalculationData == null)
            {
                return null;
            }

            Log result = new Log();

            if (string.IsNullOrWhiteSpace(apertureConstructionCalculationData.ApertureConstructionName))
            {
                result.Add(string.Format("Source ApertureConstruction name has not been provided"), LogRecordType.Message);
            }

            string apertureConstructionName = string.IsNullOrWhiteSpace(apertureConstructionCalculationData.ApertureConstructionName) ? "???" : apertureConstructionCalculationData.ApertureConstructionName;

            if (apertureConstructionCalculationData.ApertureConstructionNames == null || apertureConstructionCalculationData.ApertureConstructionNames.Count == 0)
            {
                result.Add(string.Format("ApertureConstruction names have not been provided for construction {0}", apertureConstructionName), LogRecordType.Error);
            }

            if ((double.IsNaN(apertureConstructionCalculationData.PaneThermalTransmittance) || apertureConstructionCalculationData.PaneThermalTransmittance == 0) && (double.IsNaN(apertureConstructionCalculationData.FrameThermalTransmittance) || apertureConstructionCalculationData.FrameThermalTransmittance == 0))
            {
                result.Add(string.Format("Thermal transmittances have invalid values for Aperture Construction {0}", apertureConstructionName), LogRecordType.Error);
            }

            if (constructionManager != null)
            {
                if (!string.IsNullOrWhiteSpace(apertureConstructionCalculationData.ApertureConstructionName))
                {
                    ApertureConstruction apertureConstruction = constructionManager.GetApertureConstructions(apertureConstructionCalculationData.ApertureType, apertureConstructionCalculationData.ApertureConstructionName)?.FirstOrDefault();
                    if (apertureConstruction == null)
                    {
                        result.Add(string.Format("Could not find Aperture Construction {0} in ConstructionManager", apertureConstructionName), LogRecordType.Error);
                    }

                    Core.Modify.AddRange(result, apertureConstruction?.Log(constructionManager.MaterialLibrary));
                }

                if (apertureConstructionCalculationData.ApertureConstructionNames != null)
                {
                    foreach (string apertureConstructionName_Temp in apertureConstructionCalculationData.ApertureConstructionNames)
                    {
                        ApertureConstruction apertureConstruction = constructionManager.GetApertureConstructions(apertureConstructionCalculationData.ApertureType, apertureConstructionName_Temp)?.FirstOrDefault();
                        if (apertureConstruction == null)
                        {
                            result.Add(string.Format("Could not find construction {0} in ConstructionManager", apertureConstructionName_Temp), LogRecordType.Error);
                        }

                        Core.Modify.AddRange(result, apertureConstruction?.Log(constructionManager.MaterialLibrary));
                    }
                }
            }

            return result;
        }

        public static Log Log(this LayerThicknessCalculationData layerThicknessCalculationData, ConstructionManager constructionManager = null)
        {
            if (layerThicknessCalculationData == null)
            {
                return null;
            }

            Log result = new Log();

            if (string.IsNullOrWhiteSpace(layerThicknessCalculationData.ConstructionName))
            {
                result.Add(string.Format("Source Construction name has not been provided"), LogRecordType.Message);
            }

            string constructionName = string.IsNullOrWhiteSpace(layerThicknessCalculationData.ConstructionName) ? "???" : layerThicknessCalculationData.ConstructionName;

            if (double.IsNaN(layerThicknessCalculationData.ThermalTransmittance) || layerThicknessCalculationData.ThermalTransmittance == 0)
            {
                result.Add(string.Format("Thermal transmittance has invalid value for construction {0}", constructionName), LogRecordType.Error);
            }

            if (constructionManager != null)
            {
                if (!string.IsNullOrWhiteSpace(layerThicknessCalculationData.ConstructionName))
                {
                    Construction construction = constructionManager.GetConstructions(layerThicknessCalculationData.ConstructionName)?.FirstOrDefault();
                    if (construction == null)
                    {
                        result.Add(string.Format("Could not find construction {0} in ConstructionManager", constructionName), LogRecordType.Error);
                    }

                    Core.Modify.AddRange(result, construction?.Log(constructionManager.MaterialLibrary));
                }
            }

            return result;
        }
    }
}