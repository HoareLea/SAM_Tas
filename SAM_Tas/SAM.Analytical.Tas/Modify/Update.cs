﻿using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TCD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool Update(this TBD.profile profile_TBD, Profile profile, double factor)
        {
            if (profile_TBD == null || profile == null || profile.Count == -1)
                return false;

            profile_TBD.name = profile.Name;
            profile_TBD.description = profile.Name;

            if (profile.Count == 1)
            {
                profile_TBD.type = TBD.ProfileTypes.ticValueProfile;
                profile_TBD.factor = System.Convert.ToSingle(factor);
                profile_TBD.value = System.Convert.ToSingle(profile.GetValues()[0]);
                return true;
            }

            if(profile.Count <= 24)
            {
                profile_TBD.type = TBD.ProfileTypes.ticHourlyProfile;
                profile_TBD.factor = System.Convert.ToSingle(factor);

                for (int i = 0; i <= 23; i++)
                    profile_TBD.hourlyValues[i + 1] = System.Convert.ToSingle(profile[i]);

                return true;
            }

            profile_TBD.type = TBD.ProfileTypes.ticYearlyProfile;
            profile_TBD.factor = System.Convert.ToSingle(factor);

            //object yearlyValues_TBD = profile_TBD.GetYearlyValues();

            //float[] array = Query.Array<float>(yearlyValues_TBD);

            double[] yearlyValues =  profile.GetYearlyValues();
            float[] yearlyValues_float = new float[yearlyValues.Length];
            for (int i = 0; i < yearlyValues_float.Length; i++)
                yearlyValues_float[i] = System.Convert.ToSingle(yearlyValues[i]);

            profile_TBD.SetYearlyValues(yearlyValues_float);

            //for (int i = 0; i < 8759; i++)
            //    profile_TBD.yearlyValues[i] = System.Convert.ToSingle(profile[i]);

            return true;
        }

        public static bool Update(this TBD.CoolingDesignDay coolingDesignDay_TBD, DesignDay designDay, TBD.dayType dayType = null, int repetitions = 30)
        {
            if(coolingDesignDay_TBD == null || designDay == null)
            {
                return false;
            }

            coolingDesignDay_TBD.name = designDay.Name;
            foreach(TBD.DesignDay designDay_TBD in coolingDesignDay_TBD.DesignDays())
            {
                designDay_TBD?.Update(designDay, dayType, repetitions);
            }

            return true;
        }

        public static bool Update(this TBD.HeatingDesignDay heatingDesignDay_TBD, DesignDay designDay, TBD.dayType dayType = null, int repetitions = 30)
        {
            if (heatingDesignDay_TBD == null || designDay == null)
            {
                return false;
            }

            heatingDesignDay_TBD.name = designDay.Name;
            foreach (TBD.DesignDay designDay_TBD in heatingDesignDay_TBD.DesignDays())
            {
                designDay_TBD?.Update(designDay, dayType, repetitions);
            }

            return true;
        }

        public static bool Update(this TBD.DesignDay designDay_TBD, DesignDay designDay, TBD.dayType dayType = null, int repetitions = 30)
        {
            if(designDay_TBD == null)
            {
                return false;
            }

            designDay_TBD.yearDay = designDay.GetDateTime().DayOfYear;
            designDay_TBD.repetitions = repetitions;
            designDay_TBD.description = designDay.Description;

            if(dayType != null)
            {
                designDay_TBD.SetDayType(dayType);
            }

            //TODO: TAS MEETING: Discuss with Tas how to faster copy data
            return Weather.Tas.Modify.Update(designDay_TBD?.GetWeatherDay(), designDay);
        }

        public static bool Update(this ConstructionManager constructionManager, IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult)
        {
            if (constructionManager == null || thermalTransmittanceCalculationResult == null)
            {
                return false;
            }

            return Update(constructionManager, thermalTransmittanceCalculationResult as dynamic);
        }

        public static bool Update(this ConstructionManager constructionManager, LayerThicknessCalculationResult layerThicknessCalculationResult)
        {
            if(constructionManager == null || layerThicknessCalculationResult == null)
            {
                return false;
            }

            double thickness = layerThicknessCalculationResult.Thickness;
            if (double.IsNaN(thickness))
            {
                return false;
            }

            thickness = Core.Query.Round(thickness, Tolerance.MacroDistance);

            Construction construction = constructionManager.GetConstructions(layerThicknessCalculationResult.ConstructionName, Core.TextComparisonType.Equals, true)?.FirstOrDefault();
            if(construction == null)
            {
                return false;
            }

            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            if(constructionLayers == null || constructionLayers.Count == 0)
            {
                return false;
            } 

            int layerIndex = layerThicknessCalculationResult.LayerIndex;
            if(constructionLayers.Count <= layerIndex)
            {
                return false;
            }

            ConstructionLayer constructionLayer = constructionLayers[layerIndex];
            if(constructionLayer == null)
            {
                return false;
            }

            string materialName = string.Format("{0}_{1}m", constructionLayer.Name, thickness);
            Core.IMaterial material = constructionManager.GetMaterial(materialName);
            if(material == null)
            {
                material = constructionManager.GetMaterial(constructionLayer.Name);
                if (material == null)
                {
                    return false;
                }
            }

            if(!material.TryGetValue(Core.MaterialParameter.DefaultThickness, out double defaultThickness))
            {
                if(layerThicknessCalculationResult.Thickness == defaultThickness)
                {
                    return true;
                }
            }

            if(material.Name != materialName && material is Material)
            {
                string description =  ((Material)material).Description;

                material = Core.Create.Material((Material)material, materialName, materialName, description);
            }

            material.SetValue(Core.MaterialParameter.DefaultThickness, thickness);

            constructionLayers[layerIndex] = new ConstructionLayer(materialName, thickness);

            construction = new Construction(construction, constructionLayers);

            constructionManager.Add(construction);

            constructionManager.Add(material);

            return true;
        }

        public static bool Update(this ConstructionManager constructionManager, ConstructionCalculationResult constructionCalculationResult)
        {
            if(constructionManager == null || constructionCalculationResult == null)
            {
                return false;
            }

            string initialConstructionName = constructionCalculationResult.InitialConstructionName;
            if(initialConstructionName == null)
            {
                return false;
            }

            string constructionName = constructionCalculationResult.ConstructionName;
            if(constructionName == null)
            {
                return false;
            }

            Construction initialConstruction = constructionManager.GetConstructions(initialConstructionName)?.FirstOrDefault();
            if(initialConstruction == null)
            {
                return false;
            }

            Construction construction = constructionManager.GetConstructions(constructionName)?.FirstOrDefault();
            if(construction == null)
            {
                return false;
            }

            Construction construction_Updated = new Construction(initialConstruction.Guid, construction, initialConstruction.Name);

            return constructionManager.Add(construction_Updated);
        }

        public static bool Update(this ConstructionManager constructionManager, ApertureConstructionCalculationResult apertureConstructionCalculationResult)
        {
            if (constructionManager == null || apertureConstructionCalculationResult == null)
            {
                return false;
            }

            if (constructionManager == null || apertureConstructionCalculationResult == null)
            {
                return false;
            }

            string initialApertureConstructionName = apertureConstructionCalculationResult.InitialApertureConstructionName;
            if (initialApertureConstructionName == null)
            {
                return false;
            }

            string apertureConstructionName = apertureConstructionCalculationResult.ApertureConstructionName;
            if (apertureConstructionName == null)
            {
                return false;
            }

            ApertureConstruction initialApertureConstruction = constructionManager.GetApertureConstructions(apertureConstructionCalculationResult.ApertureType, initialApertureConstructionName)?.FirstOrDefault();
            if (initialApertureConstruction == null)
            {
                return false;
            }

            ApertureConstruction apertureConstruction = constructionManager.GetApertureConstructions(apertureConstructionCalculationResult.ApertureType, apertureConstructionName)?.FirstOrDefault();
            if (apertureConstruction == null)
            {
                return false;
            }

            ApertureConstruction apertureConstruction_Updated = new ApertureConstruction(initialApertureConstruction.Guid, apertureConstruction, initialApertureConstruction.Name);

            return constructionManager.Add(apertureConstruction_Updated);
        }

        public static bool Update(this ConstructionManager constructionManager, TCD.ConstructionFolder constructionFolder, Category category = null)
        {
            if(constructionManager == null || constructionFolder == null)
            {
                return false;
            }

            bool result = false;

            Category category_Temp = Core.Create.Category(constructionFolder.name, category);

            int index;

            index = 0;
            TCD.Construction construction_TCD = constructionFolder.constructions(index);
            while(construction_TCD != null)
            {
                Construction construction = construction_TCD.ToSAM();
                if(construction != null)
                {
                    List<Core.IMaterial> materials = construction_TCD.ToSAM_Materials();
                    if (materials != null)
                    {
                        materials.ForEach(x => constructionManager.Add(x));
                    }

                    constructionManager.Add(construction);
                    result = true;
                }

                construction.SetValue(AnalyticalObjectParameter.Category, category_Temp);

                index++;
                construction_TCD = constructionFolder.constructions(index);
            }

            index = 0;
            TCD.ConstructionFolder constructionFolder_Child = constructionFolder.childFolders(index);
            while(constructionFolder_Child != null)
            {
                Update(constructionManager, constructionFolder, category_Temp);
                index++;
                constructionFolder_Child = constructionFolder.childFolders(index);
            }

            return result;
        }

        public static bool Update(this ConstructionManager constructionManager, TCD.MaterialFolder materialFolder)
        {
            if (constructionManager == null || materialFolder == null)
            {
                return false;
            }

            bool result = false;

            return result;
        }
    }
}