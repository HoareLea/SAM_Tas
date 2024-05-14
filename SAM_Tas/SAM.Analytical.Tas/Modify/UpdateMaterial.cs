using SAM.Core;
using TCD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateMaterial(this TBD.material material_TBD, Core.IMaterial material)
        {
            if (material == null || material_TBD == null)
                return false;

            return UpdateMaterial(material_TBD, material as dynamic);
        }

        public static bool UpdateMaterial(this material material_TCD, Core.IMaterial material)
        {
            if (material == null || material_TCD == null)
            {
                return false;
            }

            material_TCD.name = material.Name;
            if (material is Material)
            {
                if (material is OpaqueMaterial)
                {
                    OpaqueMaterial opaqueMaterial = (OpaqueMaterial)material;

                    material_TCD.type = (int)MaterialTypes.tcdOpaqueLayer;
                    material_TCD.conductivity = System.Convert.ToSingle(opaqueMaterial.ThermalConductivity);
                    material_TCD.description = opaqueMaterial.Description;
                    material_TCD.specificHeat = System.Convert.ToSingle(opaqueMaterial.SpecificHeatCapacity);
                    material_TCD.density = System.Convert.ToSingle(opaqueMaterial.Density);

                    if (opaqueMaterial.TryGetValue(OpaqueMaterialParameter.ExternalSolarReflectance, out double externalSolarReflectance) && !double.IsNaN(externalSolarReflectance))
                    {
                        material_TCD.externalSolarReflectance = System.Convert.ToSingle(externalSolarReflectance);
                    }

                    if (opaqueMaterial.TryGetValue(OpaqueMaterialParameter.InternalSolarReflectance, out double internalSolarReflectance) && !double.IsNaN(internalSolarReflectance))
                    {
                        material_TCD.internalSolarReflectance = System.Convert.ToSingle(internalSolarReflectance);
                    }

                    if (opaqueMaterial.TryGetValue(OpaqueMaterialParameter.ExternalLightReflectance, out double externalLightReflectance) && !double.IsNaN(externalLightReflectance))
                    {
                        material_TCD.externalLightReflectance = System.Convert.ToSingle(externalLightReflectance);
                    }

                    if (opaqueMaterial.TryGetValue(OpaqueMaterialParameter.InternalLightReflectance, out double internalLightReflectance) && !double.IsNaN(internalLightReflectance))
                    {
                        material_TCD.internalLightReflectance = System.Convert.ToSingle(internalLightReflectance);
                    }

                    if (opaqueMaterial.TryGetValue(OpaqueMaterialParameter.ExternalEmissivity, out double externalEmissivity) && !double.IsNaN(externalEmissivity))
                    {
                        material_TCD.externalEmissivity = System.Convert.ToSingle(externalEmissivity);
                    }

                    if (opaqueMaterial.TryGetValue(OpaqueMaterialParameter.InternalEmissivity, out double internalEmissivity) && !double.IsNaN(internalEmissivity))
                    {
                        material_TCD.internalEmissivity = System.Convert.ToSingle(internalEmissivity);
                    }

                    if (opaqueMaterial.TryGetValue(OpaqueMaterialParameter.IgnoreThermalTransmittanceCalculations, out bool ignoreThermalTransmittanceCalculations))
                    {
                        material_TCD.isBlind = ignoreThermalTransmittanceCalculations;
                    }
                }
                else if (material is TransparentMaterial)
                {
                    TransparentMaterial transparentMaterial = (TransparentMaterial)material;

                    material_TCD.type = (int)MaterialTypes.tcdTransparentLayer;
                    material_TCD.conductivity = System.Convert.ToSingle(transparentMaterial.ThermalConductivity);
                    material_TCD.description = transparentMaterial.Description;
                    material_TCD.specificHeat = System.Convert.ToSingle(transparentMaterial.SpecificHeatCapacity);
                    material_TCD.density = System.Convert.ToSingle(transparentMaterial.Density);

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.SolarTransmittance, out double solarTransmittance) && !double.IsNaN(solarTransmittance))
                    {
                        material_TCD.solarTransmittance = System.Convert.ToSingle(solarTransmittance);
                    }

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.ExternalSolarReflectance, out double externalSolarReflectance) && !double.IsNaN(externalSolarReflectance))
                    {
                        material_TCD.externalSolarReflectance = System.Convert.ToSingle(externalSolarReflectance);
                    }

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.InternalSolarReflectance, out double internalSolarReflectance) && !double.IsNaN(internalSolarReflectance))
                    {
                        material_TCD.internalSolarReflectance = System.Convert.ToSingle(internalSolarReflectance);
                    }

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.LightTransmittance, out double lightTransmittance) && !double.IsNaN(lightTransmittance))
                    {
                        material_TCD.lightTransmittance = System.Convert.ToSingle(lightTransmittance);
                    }

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.ExternalLightReflectance, out double externalLightReflectance) && !double.IsNaN(externalLightReflectance))
                    {
                        material_TCD.externalLightReflectance = System.Convert.ToSingle(externalLightReflectance);
                    }

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.InternalLightReflectance, out double internalLightReflectance) && !double.IsNaN(internalLightReflectance))
                    {
                        material_TCD.internalLightReflectance = System.Convert.ToSingle(internalLightReflectance);
                    }

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.ExternalEmissivity, out double externalEmissivity) && !double.IsNaN(externalEmissivity))
                    {
                        material_TCD.externalEmissivity = System.Convert.ToSingle(externalEmissivity);
                    }

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.InternalEmissivity, out double internalEmissivity) && !double.IsNaN(internalEmissivity))
                    {
                        material_TCD.internalEmissivity = System.Convert.ToSingle(internalEmissivity);
                    }

                    if (transparentMaterial.TryGetValue(TransparentMaterialParameter.IsBlind, out bool isBlind))
                    {
                        material_TCD.isBlind = isBlind;
                    }

                }
                else if (material is GasMaterial)
                {
                    GasMaterial gasMaterial = (GasMaterial)material;

                    material_TCD.type = (int)MaterialTypes.tcdGasLayer;

                    float thermalConductivity = System.Convert.ToSingle(gasMaterial.ThermalConductivity);
                    material_TCD.conductivity = thermalConductivity < 0 || double.IsNaN(thermalConductivity) ? 0 : thermalConductivity;

                    float specificHeatCapacity = System.Convert.ToSingle(gasMaterial.SpecificHeatCapacity);
                    material_TCD.specificHeat = specificHeatCapacity < 0 || double.IsNaN(specificHeatCapacity) ? 0 : specificHeatCapacity;

                    material_TCD.description = gasMaterial.Description;
                    material_TCD.density = System.Convert.ToSingle(gasMaterial.Density);
                    material_TCD.dynamicViscosity = System.Convert.ToSingle(gasMaterial.DynamicViscosity);

                    if (gasMaterial.TryGetValue(GasMaterialParameter.HeatTransferCoefficient, out double heatTransferCoefficient) && !double.IsNaN(heatTransferCoefficient))
                    {
                        material_TCD.convectionCoefficient = System.Convert.ToSingle(heatTransferCoefficient);
                    }
                }
                else
                {
                    return false;
                }

                if (material.TryGetValue(Core.MaterialParameter.DefaultThickness, out double thickness) && !double.IsNaN(thickness))
                {
                    material_TCD.width = System.Convert.ToSingle(thickness);
                }

                if (material.TryGetValue(Analytical.MaterialParameter.VapourDiffusionFactor, out double vapourDiffusionFactor) && !double.IsNaN(vapourDiffusionFactor))
                {
                    material_TCD.vapourDiffusionFactor = System.Convert.ToSingle(vapourDiffusionFactor);
                }
            }

            return true;
        }

        public static bool UpdateMaterial(this TBD.material material, GasMaterial gasMaterial)
        {
            if (!string.IsNullOrWhiteSpace(gasMaterial.Name) && !gasMaterial.Name.Equals(material.name))
                material.name = gasMaterial.Name;

            material.type = (int)TBD.MaterialTypes.tcdGasLayer;

            material.description = gasMaterial.Description;
            material.width = System.Convert.ToSingle(gasMaterial.GetValue<double>(Core.MaterialParameter.DefaultThickness));
            material.convectionCoefficient = System.Convert.ToSingle(gasMaterial.GetValue<double>(GasMaterialParameter.HeatTransferCoefficient));
            material.vapourDiffusionFactor = System.Convert.ToSingle(gasMaterial.GetValue<double>(Analytical.MaterialParameter.VapourDiffusionFactor));

            float thermalConductivity = System.Convert.ToSingle(gasMaterial.ThermalConductivity);
            material.conductivity = thermalConductivity < 0 || double.IsNaN(thermalConductivity) ? 0 : thermalConductivity;

            float specificHeatCapacity = System.Convert.ToSingle(gasMaterial.SpecificHeatCapacity);
            material.specificHeat = specificHeatCapacity < 0 || double.IsNaN(specificHeatCapacity) ? 0 : specificHeatCapacity;

            material.density = System.Convert.ToSingle(gasMaterial.Density);
            material.dynamicViscosity = System.Convert.ToSingle(gasMaterial.DynamicViscosity);

            return true;
        }

        public static bool UpdateMaterial(this TBD.material material, TransparentMaterial transparentMaterial)
        {
            if (!string.IsNullOrWhiteSpace(transparentMaterial.Name) && !transparentMaterial.Name.Equals(material.name))
                material.name = transparentMaterial.Name;

            material.type = (int)TBD.MaterialTypes.tcdTransparentLayer;

            material.description = transparentMaterial.Description;
            material.width = System.Convert.ToSingle(transparentMaterial.GetValue<double>(Core.MaterialParameter.DefaultThickness));
            material.conductivity = System.Convert.ToSingle(transparentMaterial.ThermalConductivity);
            material.vapourDiffusionFactor = System.Convert.ToSingle(transparentMaterial.GetValue<double>(Analytical.MaterialParameter.VapourDiffusionFactor));
            material.solarTransmittance = System.Convert.ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.SolarTransmittance));
            material.externalSolarReflectance = System.Convert.ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.ExternalSolarReflectance));
            material.internalSolarReflectance = System.Convert.ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.InternalSolarReflectance));
            material.lightTransmittance = System.Convert.ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.LightTransmittance));
            material.externalLightReflectance = System.Convert.ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.ExternalLightReflectance));
            material.internalLightReflectance = System.Convert.ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.InternalLightReflectance));
            material.externalEmissivity = System.Convert.ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.ExternalEmissivity));
            material.internalEmissivity = System.Convert.ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.InternalEmissivity));

            if (transparentMaterial.GetValue<bool>(TransparentMaterialParameter.IsBlind))
                material.isBlind = 1;
            else
                material.isBlind = 0;

            return true;
        }

        public static bool UpdateMaterial(this TBD.material material, OpaqueMaterial opaqueMaterial)
        {
            if (!string.IsNullOrWhiteSpace(opaqueMaterial.Name) && !opaqueMaterial.Name.Equals(material.name))
                material.name = opaqueMaterial.Name;

            material.type = (int)TBD.MaterialTypes.tcdOpaqueMaterial;

            material.description = opaqueMaterial.Description;
            material.width = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(Core.MaterialParameter.DefaultThickness));
            material.conductivity = System.Convert.ToSingle(opaqueMaterial.ThermalConductivity);
            material.specificHeat = System.Convert.ToSingle(opaqueMaterial.SpecificHeatCapacity);
            material.density = System.Convert.ToSingle(opaqueMaterial.Density);
            material.vapourDiffusionFactor = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(Analytical.MaterialParameter.VapourDiffusionFactor));
            material.externalSolarReflectance = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.ExternalSolarReflectance));
            material.internalSolarReflectance = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.InternalSolarReflectance));
            material.externalLightReflectance = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.ExternalLightReflectance));
            material.internalLightReflectance = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.InternalLightReflectance));
            material.externalEmissivity = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.ExternalEmissivity));
            material.internalEmissivity = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.InternalEmissivity));

            return true;
        }
    }
}