// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// The properties of a TCD material that the SAM import reads, captured with a single pass of
    /// COM calls. TCD.exe is an out-of-process server, so every property read is a cross-process
    /// call; keeping a snapshot lets the unique id and the SAM material both be derived without
    /// going back to TCD. The values keep their COM types (float / bool / int), so the unique id
    /// text and the numeric conversions are exactly those of reading the properties directly.
    /// </summary>
    internal sealed class TCDMaterialData
    {
        private readonly string name;
        private readonly string description;
        private readonly float width;
        private readonly float externalSolarReflectance;
        private readonly float internalSolarReflectance;
        private readonly float solarTransmittance;
        private readonly float externalEmissivity;
        private readonly float internalEmissivity;
        private readonly float lightTransmittance;
        private readonly float externalLightReflectance;
        private readonly float internalLightReflectance;
        private readonly bool isBlind;
        private readonly float conductivity;
        private readonly float density;
        private readonly float specificHeat;
        private readonly float vapourDiffusionFactor;
        private readonly float convectionCoefficient;
        private readonly int type;
        private readonly float dynamicViscosity;

        public TCDMaterialData(TCD.IMaterial material)
        {
            name = material.name;
            description = material.description;
            width = material.width;
            externalSolarReflectance = material.externalSolarReflectance;
            internalSolarReflectance = material.internalSolarReflectance;
            solarTransmittance = material.solarTransmittance;
            externalEmissivity = material.externalEmissivity;
            internalEmissivity = material.internalEmissivity;
            lightTransmittance = material.lightTransmittance;
            externalLightReflectance = material.externalLightReflectance;
            internalLightReflectance = material.internalLightReflectance;
            isBlind = material.isBlind;
            conductivity = material.conductivity;
            density = material.density;
            specificHeat = material.specificHeat;
            vapourDiffusionFactor = material.vapourDiffusionFactor;
            convectionCoefficient = material.convectionCoefficient;
            type = material.type;
            dynamicViscosity = material.dynamicViscosity;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public float Width
        {
            get
            {
                return width;
            }
        }

        public string UniqueId()
        {
            List<string> values = new List<string>();
            values.Add(name == null ? string.Empty : name);
            values.Add(description == null ? string.Empty : description);
            values.Add(width.ToString());
            values.Add(externalSolarReflectance.ToString());
            values.Add(internalSolarReflectance.ToString());
            values.Add(solarTransmittance.ToString());
            values.Add(externalEmissivity.ToString());
            values.Add(internalEmissivity.ToString());
            values.Add(lightTransmittance.ToString());
            values.Add(externalLightReflectance.ToString());
            values.Add(internalLightReflectance.ToString());
            values.Add(isBlind.ToString());
            values.Add(conductivity.ToString());
            values.Add(density.ToString());
            values.Add(specificHeat.ToString());
            values.Add(vapourDiffusionFactor.ToString());
            values.Add(convectionCoefficient.ToString());
            values.Add(type.ToString());
            values.Add(dynamicViscosity.ToString());

            return string.Join("_", values);
        }

        public Core.IMaterial ToSAM(string name = null)
        {
            Core.IMaterial result = null;
            switch ((TBD.MaterialTypes)type)
            {
                case TBD.MaterialTypes.tcdGasLayer:
                    result = Analytical.Create.GasMaterial(
                        name == null ? this.name : name,
                        string.Empty,
                        this.name,
                        description,
                        conductivity,
                        specificHeat,
                        density,
                        dynamicViscosity,
                        width,
                        vapourDiffusionFactor,
                        double.NaN);
                    result.SetValue(GasMaterialParameter.HeatTransferCoefficient, convectionCoefficient);
                    break;

                case TBD.MaterialTypes.tcdOpaqueLayer:
                    result = Analytical.Create.OpaqueMaterial(
                        name == null ? this.name : name,
                        string.Empty,
                        this.name,
                        description,
                        conductivity,
                        specificHeat,
                        density,
                        width,
                        vapourDiffusionFactor,
                        externalSolarReflectance,
                        internalSolarReflectance,
                        externalLightReflectance,
                        externalLightReflectance,
                        externalEmissivity,
                        internalEmissivity,
                        isBlind);
                    break;

                case TBD.MaterialTypes.tcdOpaqueMaterial:
                    result = Analytical.Create.OpaqueMaterial(
                        name == null ? this.name : name,
                        string.Empty,
                        this.name,
                        description,
                        conductivity,
                        specificHeat,
                        density,
                        width,
                        vapourDiffusionFactor,
                        externalSolarReflectance,
                        internalSolarReflectance,
                        externalLightReflectance,
                        externalLightReflectance,
                        externalEmissivity,
                        internalEmissivity,
                        false);
                    break;

                case TBD.MaterialTypes.tcdTransparentLayer:
                    result = Analytical.Create.TransparentMaterial(
                        name == null ? this.name : name,
                        string.Empty,
                        this.name,
                        description,
                        conductivity,
                        width,
                        vapourDiffusionFactor,
                        solarTransmittance,
                        lightTransmittance,
                        externalSolarReflectance,
                        internalSolarReflectance,
                        externalLightReflectance,
                        internalLightReflectance,
                        externalEmissivity,
                        internalEmissivity,
                        isBlind);
                    break;
            }

            return result;
        }
    }
}
