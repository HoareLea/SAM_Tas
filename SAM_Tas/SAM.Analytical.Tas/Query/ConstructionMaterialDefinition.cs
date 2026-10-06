// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>What <c>Modify.UpdateMaterial</c> will put on a fresh <c>TBD.material</c> for this SAM
        /// material</b> - resolved WITHOUT touching COM, so a construction's content can be identified,
        /// compared and named before a single TBD object is read or created.
        /// <para>
        /// This is a deliberate MIRROR of the three <c>UpdateMaterial(TBD.material, …)</c> overloads that the
        /// export's <c>construction.AddMaterial(material)</c> dispatches to, field for field and clamp for
        /// clamp - including the two places where the TBD write differs from its TCD sibling: the opaque
        /// write stores <c>tcdOpaqueMaterial</c> where the TCD one stores <c>tcdOpaqueLayer</c>, and the
        /// transparent write does not touch <c>specificHeat</c> or <c>density</c> at all. A field an overload
        /// leaves alone is reported as the value a freshly added TBD material holds - zero, or no
        /// description.
        /// </para>
        /// <para>
        /// <b>Why a mirror is safe.</b> Two layers created in one export run through this same function on
        /// the same input, so a mirror that disagreed with the writer could not merge two materials the model
        /// states as different - it would produce the same answer for both, and both would still get the same
        /// TBD material. What a disagreement WOULD cost is recognising a construction that was already in the
        /// TBD: its material reads back as what TBD really holds, and if that is not what this function
        /// predicts, the construction is simply not reused. Under-reuse, never unsafe sharing.
        /// </para>
        /// <para>
        /// <b><c>width</c> is deliberately absent.</b> Every overload writes it from the material's own
        /// default thickness and the export then overwrites it with the layer's stated thickness, so it
        /// belongs to the layer - see <see cref="ConstructionLayerDefinition"/>.
        /// </para>
        /// </summary>
        /// <returns>
        /// The material content, or null when <paramref name="material"/> is null or is not one of the three
        /// kinds the write knows how to store. An unrecognised kind makes the construction UNPROVEN rather
        /// than throwing: it is still created exactly as before, but it never takes part in reuse.
        /// </returns>
        /// <remarks>
        /// <b>Licensed-TAS-confirmed defaults for a freshly added <c>TBD.material</c>.</b> The opaque and
        /// transparent <c>UpdateMaterial</c> overloads never touch <c>dynamicViscosity</c> or
        /// <c>convectionCoefficient</c> - those are written only for a <see cref="GasMaterial"/> - so a
        /// fresh material's OWN TAS-native values stand, exactly as the comment on this method already
        /// said. What was wrong was the assumed value: confirmed on licensed TAS (a real construction
        /// exported with an opaque and a transparent layer, read back after save/reopen) that a fresh
        /// <c>TBD.material</c> reports <c>dynamicViscosity = 1E-05</c> and
        /// <c>convectionCoefficient = 0.001</c>, not zero. Reporting zero here made every repeat export
        /// see its own just-created opaque/transparent materials as different content from what was
        /// asked for - safe (under-reuse only, never unsafe sharing, per the class-level remarks above)
        /// but it meant a second export of the same model never satisfied "+0 Construction/BuildingElement",
        /// and a third export could then hit the double-name-collision refusal.
        /// </remarks>
        private const float FreshOpaqueOrTransparentDynamicViscosity = 1E-05f;
        private const float FreshOpaqueOrTransparentConvectionCoefficient = 0.001f;

        public static ConstructionMaterialDefinition ConstructionMaterialDefinition(this Core.IMaterial material)
        {
            if (material == null)
            {
                return null;
            }

            if (material is OpaqueMaterial)
            {
                OpaqueMaterial opaqueMaterial = (OpaqueMaterial)material;

                return new ConstructionMaterialDefinition(
                    name: MaterialName(opaqueMaterial),
                    type: (int)TBD.MaterialTypes.tcdOpaqueMaterial,
                    description: opaqueMaterial.Description,
                    conductivity: ToSingle(opaqueMaterial.ThermalConductivity),
                    specificHeat: ToSingle(opaqueMaterial.SpecificHeatCapacity),
                    density: ToSingle(opaqueMaterial.Density),
                    vapourDiffusionFactor: ToSingle(opaqueMaterial.GetValue<double>(Analytical.MaterialParameter.VapourDiffusionFactor)),
                    externalSolarReflectance: ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.ExternalSolarReflectance)),
                    internalSolarReflectance: ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.InternalSolarReflectance)),
                    externalLightReflectance: ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.ExternalLightReflectance)),
                    internalLightReflectance: ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.InternalLightReflectance)),
                    externalEmissivity: ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.ExternalEmissivity)),
                    internalEmissivity: ToSingle(opaqueMaterial.GetValue<double>(OpaqueMaterialParameter.InternalEmissivity)),

                    //Not written by the opaque overload - a fresh TBD material's own values stand.
                    solarTransmittance: 0,
                    lightTransmittance: 0,
                    dynamicViscosity: FreshOpaqueOrTransparentDynamicViscosity,
                    convectionCoefficient: FreshOpaqueOrTransparentConvectionCoefficient,
                    isBlind: 0);
            }

            if (material is TransparentMaterial)
            {
                TransparentMaterial transparentMaterial = (TransparentMaterial)material;

                return new ConstructionMaterialDefinition(
                    name: MaterialName(transparentMaterial),
                    type: (int)TBD.MaterialTypes.tcdTransparentLayer,
                    description: transparentMaterial.Description,
                    conductivity: ToSingle(transparentMaterial.ThermalConductivity),

                    //The TBD transparent overload writes neither, unlike its TCD sibling.
                    specificHeat: 0,
                    density: 0,

                    vapourDiffusionFactor: ToSingle(transparentMaterial.GetValue<double>(Analytical.MaterialParameter.VapourDiffusionFactor)),
                    externalSolarReflectance: ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.ExternalSolarReflectance)),
                    internalSolarReflectance: ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.InternalSolarReflectance)),
                    externalLightReflectance: ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.ExternalLightReflectance)),
                    internalLightReflectance: ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.InternalLightReflectance)),
                    externalEmissivity: ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.ExternalEmissivity)),
                    internalEmissivity: ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.InternalEmissivity)),
                    solarTransmittance: ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.SolarTransmittance)),
                    lightTransmittance: ToSingle(transparentMaterial.GetValue<double>(TransparentMaterialParameter.LightTransmittance)),

                    //Not written by the transparent overload either - same fresh-material defaults as opaque.
                    dynamicViscosity: FreshOpaqueOrTransparentDynamicViscosity,
                    convectionCoefficient: FreshOpaqueOrTransparentConvectionCoefficient,

                    //Written as the 1/0 TBD stores, not as a bool.
                    isBlind: transparentMaterial.GetValue<bool>(TransparentMaterialParameter.IsBlind) ? 1 : 0);
            }

            if (material is GasMaterial)
            {
                GasMaterial gasMaterial = (GasMaterial)material;

                return new ConstructionMaterialDefinition(
                    name: MaterialName(gasMaterial),
                    type: (int)TBD.MaterialTypes.tcdGasLayer,
                    description: gasMaterial.Description,

                    //The gas overload clamps: a negative or absent value is stored as zero.
                    conductivity: ClampNonNegative(ToSingle(gasMaterial.ThermalConductivity)),
                    specificHeat: ClampNonNegative(ToSingle(gasMaterial.SpecificHeatCapacity)),

                    density: ToSingle(gasMaterial.Density),
                    vapourDiffusionFactor: ToSingle(gasMaterial.GetValue<double>(Analytical.MaterialParameter.VapourDiffusionFactor)),

                    externalSolarReflectance: 0,
                    internalSolarReflectance: 0,
                    externalLightReflectance: 0,
                    internalLightReflectance: 0,
                    externalEmissivity: 0,
                    internalEmissivity: 0,
                    solarTransmittance: 0,
                    lightTransmittance: 0,

                    dynamicViscosity: ToSingle(gasMaterial.DynamicViscosity),
                    convectionCoefficient: ToSingle(gasMaterial.GetValue<double>(GasMaterialParameter.HeatTransferCoefficient)),
                    isBlind: 0);
            }

            //Some other kind of material. The write has no overload for it, so what TBD would end up holding
            //is not something this can predict - and an unpredictable layer must never be shared.
            return null;
        }

        /// <summary>
        /// The name the write puts on the TBD material: the SAM material's name, or nothing at all when the
        /// material states no usable name - the write's guard is
        /// <c>if (!string.IsNullOrWhiteSpace(name) &amp;&amp; !name.Equals(material.name))</c>, and a freshly
        /// added TBD material has no name for it to differ from.
        /// </summary>
        private static string MaterialName(Core.Material material)
        {
            return string.IsNullOrWhiteSpace(material.Name) ? null : material.Name;
        }

        private static float ToSingle(double value)
        {
            return global::System.Convert.ToSingle(value);
        }

        /// <summary>The gas write's own clamp: a negative or NaN value is stored as zero.</summary>
        private static float ClampNonNegative(float value)
        {
            return value < 0 || float.IsNaN(value) ? 0f : value;
        }
    }
}
