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
            material.width = System.Convert.ToSingle(gasMaterial.DefaultThickness());
            material.convectionCoefficient = System.Convert.ToSingle(Analytical.Query.HeatTransferCoefficient(gasMaterial));
            material.vapourDiffusionFactor = System.Convert.ToSingle(gasMaterial.VapurDiffusionFactor());

            return true;
        }

        public static bool UpdateMaterial(this TBD.material material, TransparentMaterial transparentMaterial)
        {
            if (!string.IsNullOrWhiteSpace(transparentMaterial.Name) && !transparentMaterial.Name.Equals(material.name))
                material.name = transparentMaterial.Name;

            material.type = (int)TBD.MaterialTypes.tcdTransparentLayer;

            material.description = transparentMaterial.Description;
            material.width = System.Convert.ToSingle(transparentMaterial.DefaultThickness());
            material.conductivity = System.Convert.ToSingle(transparentMaterial.ThermalConductivity);
            material.vapourDiffusionFactor = System.Convert.ToSingle(transparentMaterial.VapurDiffusionFactor());
            material.solarTransmittance = System.Convert.ToSingle(transparentMaterial.SolarTransmittance());
            material.externalSolarReflectance = System.Convert.ToSingle(transparentMaterial.ExternalSolarReflectance());
            material.internalSolarReflectance = System.Convert.ToSingle(transparentMaterial.InternalSolarReflectance());
            material.lightTransmittance = System.Convert.ToSingle(transparentMaterial.LightTransmittance());
            material.externalLightReflectance = System.Convert.ToSingle(transparentMaterial.ExternalLightReflectance());
            material.internalLightReflectance = System.Convert.ToSingle(transparentMaterial.InternalLightReflectance());
            material.externalEmissivity = System.Convert.ToSingle(transparentMaterial.ExternalEmissivity());
            material.internalEmissivity = System.Convert.ToSingle(transparentMaterial.InternalEmissivity());

            if (transparentMaterial.IsBlind())
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
            material.width = System.Convert.ToSingle(opaqueMaterial.DefaultThickness());
            material.conductivity = System.Convert.ToSingle(opaqueMaterial.ThermalConductivity);
            material.specificHeat = System.Convert.ToSingle(opaqueMaterial.SpecificHeatCapacity);
            material.density = System.Convert.ToSingle(opaqueMaterial.Density);
            material.vapourDiffusionFactor = System.Convert.ToSingle(opaqueMaterial.VapurDiffusionFactor());
            material.externalSolarReflectance = System.Convert.ToSingle(opaqueMaterial.ExternalSolarReflectance());
            material.internalSolarReflectance = System.Convert.ToSingle(opaqueMaterial.InternalSolarReflectance());
            material.externalLightReflectance = System.Convert.ToSingle(opaqueMaterial.ExternalLightReflectance());
            material.internalLightReflectance = System.Convert.ToSingle(opaqueMaterial.InternalLightReflectance());
            material.externalEmissivity = System.Convert.ToSingle(opaqueMaterial.ExternalEmissivity());
            material.internalEmissivity = System.Convert.ToSingle(opaqueMaterial.InternalEmissivity());

            return true;
        }
    }
}