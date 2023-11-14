using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string UniqueId(this TCD.IMaterial material)
        {
            if(material == null)
            {
                return null;
            }

            List<string> values = new List<string>();
            values.Add(material.name == null ? string.Empty : material.name);
            values.Add(material.description == null ? string.Empty : material.description);
            values.Add(material.width.ToString());
            values.Add(material.externalSolarReflectance.ToString());
            values.Add(material.internalSolarReflectance.ToString());
            values.Add(material.solarTransmittance.ToString());
            values.Add(material.externalEmissivity.ToString());
            values.Add(material.internalEmissivity.ToString());
            values.Add(material.lightTransmittance.ToString());
            values.Add(material.externalLightReflectance.ToString());
            values.Add(material.internalLightReflectance.ToString());
            values.Add(material.isBlind.ToString());
            values.Add(material.conductivity.ToString());
            values.Add(material.density.ToString());
            values.Add(material.specificHeat.ToString());
            values.Add(material.vapourDiffusionFactor.ToString());
            values.Add(material.convectionCoefficient.ToString());
            values.Add(material.type.ToString());
            values.Add(material.dynamicViscosity.ToString());

            return string.Join("_", values);
        }
    }
}