using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateMaterial(this TBD.material material_TBD, IMaterial material)
        {
            if (material == null || material_TBD == null)
                return false;

            return UpdateMaterial(material_TBD, material as dynamic);
        }

        public static bool UpdateMaterial(this TBD.material material, GasMaterial gasMaterial)
        {
            if (!string.IsNullOrWhiteSpace(gasMaterial.Name) && !gasMaterial.Name.Equals(material.name))
                material.name = gasMaterial.Name;

            material.type = (int)TBD.MaterialTypes.tcdGasLayer;

            material.description = gasMaterial.Description;
            material.width = System.Convert.ToSingle(gasMaterial.GetValue<double>(Core.MaterialParameter.DefaultThickness));
            material.convectionCoefficient = System.Convert.ToSingle(gasMaterial.GetValue<double>(GasMaterialParameter.HeatTransferCoefficient));
            material.vapourDiffusionFactor = System.Convert.ToSingle(gasMaterial.GetValue<double>(MaterialParameter.VapourDiffusionFactor));

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
            material.vapourDiffusionFactor = System.Convert.ToSingle(transparentMaterial.GetValue<double>(MaterialParameter.VapourDiffusionFactor));
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
            material.vapourDiffusionFactor = System.Convert.ToSingle(opaqueMaterial.GetValue<double>(MaterialParameter.VapourDiffusionFactor));
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