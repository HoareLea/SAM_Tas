using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Core.IMaterial ToSAM(this TBD.material material)
        {
            if(material == null)
            {
                return null;
            }

            Core.IMaterial result = null;
            switch((TBD.MaterialTypes)material.type)
            {
                case TBD.MaterialTypes.tcdGasLayer:
                    result = Analytical.Create.GasMaterial(
                        material.name, 
                        string.Empty, 
                        material.name, 
                        material.description, 
                        material.width, 
                        material.vapourDiffusionFactor, 
                        double.NaN);
                    break;

                case TBD.MaterialTypes.tcdOpaqueLayer:
                    result = Analytical.Create.OpaqueMaterial(
                        material.name, 
                        string.Empty, 
                        material.name, 
                        material.description, 
                        material.conductivity, 
                        material.specificHeat, 
                        material.density, 
                        material.width, 
                        material.vapourDiffusionFactor, 
                        material.externalSolarReflectance, 
                        material.internalSolarReflectance, 
                        material.externalLightReflectance, 
                        material.externalLightReflectance, 
                        material.externalEmissivity, 
                        material.internalEmissivity, 
                        false);
                    break;

                case TBD.MaterialTypes.tcdOpaqueMaterial:
                    result = Analytical.Create.OpaqueMaterial(
                        material.name,
                        string.Empty,
                        material.name,
                        material.description,
                        material.conductivity,
                        material.specificHeat,
                        material.density,
                        material.width,
                        material.vapourDiffusionFactor,
                        material.externalSolarReflectance,
                        material.internalSolarReflectance,
                        material.externalLightReflectance,
                        material.externalLightReflectance,
                        material.externalEmissivity,
                        material.internalEmissivity,
                        false);
                    break;

                case TBD.MaterialTypes.tcdTransparentLayer:
                    result = Analytical.Create.TransparentMaterial(
                        material.name,
                        string.Empty,
                        material.name,
                        material.description,
                        material.conductivity,
                        material.width,
                        material.vapourDiffusionFactor,
                        material.solarTransmittance,
                        material.lightTransmittance,
                        material.externalSolarReflectance,
                        material.internalSolarReflectance,
                        material.externalLightReflectance,
                        material.internalLightReflectance,
                        material.externalEmissivity,
                        material.internalEmissivity,
                        material.isBlind == 1);
                    break;
            }


            return result;
        }
    }
}
