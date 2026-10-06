// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>What makes two TBD construction layers the same layer.</b> An immutable, COM-free value object
    /// over exactly the fields this export writes onto a <c>TBD.material</c> - one per property
    /// <c>TBD.IMaterial</c> exposes, minus <c>width</c>, which belongs to the layer rather than the material
    /// (see <see cref="ConstructionLayerDefinition"/>).
    /// <para>
    /// <b>Why the physics and not just the name.</b> Reuse of a construction this export authored could be
    /// decided on material NAMES alone: within one export the mapping name -&gt; material comes from one
    /// <c>MaterialLibrary</c>, so equal names mean equal materials by construction. A construction that was
    /// already in the TBD before this export is a different matter - nothing proves the material behind its
    /// "Glass 6mm" layer is the "Glass 6mm" the model states. Comparing the stored physics is what turns
    /// "same name" into "same layer", and it is the only way a definition read back off a seeded TBD can be
    /// proven equal to one this export is about to write.
    /// </para>
    /// <para>
    /// <b>Both sides are built by the same field set.</b> <see cref="Query.ConstructionMaterialDefinition(Core.IMaterial)"/>
    /// mirrors what <c>Modify.UpdateMaterial</c> writes; <c>Query.ConstructionMaterialDefinitionTBD</c> reads
    /// the same fields back. A field the writer leaves alone is carried here as the value a freshly added
    /// TBD material holds for it - zero, or an empty description. That makes the mirror's failure mode
    /// one-directional and safe: a field this class gets wrong can only stop a SEEDED construction from
    /// being recognised (under-reuse), never merge two constructions the model states as different, because
    /// two layers created in one export run through the same mirror on identical input.
    /// </para>
    /// <para>
    /// <b>Float comparison is exact, with NaN equal to NaN.</b> Both sides have been through the same
    /// <c>Convert.ToSingle</c>, so a tolerance would only ever merge two layers the model states as
    /// different. <see cref="float.Equals(object)"/> semantics are used deliberately rather than
    /// <c>==</c>: a material that states no conductivity stores NaN, and under <c>==</c> that layer would
    /// never equal itself, so every window carrying it would get its own construction. Signed zero and NaN
    /// are both normalised on the way in so the deterministic signature - which hashes IEEE-754 bit
    /// patterns - agrees with equality.
    /// </para>
    /// <para><b>Instances are immutable.</b> A shared definition is never rewritten.</para>
    /// </summary>
    public sealed class ConstructionMaterialDefinition : IEquatable<ConstructionMaterialDefinition>
    {
        /// <summary>
        /// The raw-values constructor. Named arguments are strongly preferred at call sites - the field list
        /// is long and positional.
        /// </summary>
        public ConstructionMaterialDefinition(
            string name,
            int type,
            string description,
            float conductivity,
            float specificHeat,
            float density,
            float vapourDiffusionFactor,
            float externalSolarReflectance,
            float internalSolarReflectance,
            float externalLightReflectance,
            float internalLightReflectance,
            float externalEmissivity,
            float internalEmissivity,
            float solarTransmittance,
            float lightTransmittance,
            float dynamicViscosity,
            float convectionCoefficient,
            int isBlind)
        {
            Name = Normalize(name);
            Type = type;
            Description = Normalize(description);
            Conductivity = NormalizeSingle(conductivity);
            SpecificHeat = NormalizeSingle(specificHeat);
            Density = NormalizeSingle(density);
            VapourDiffusionFactor = NormalizeSingle(vapourDiffusionFactor);
            ExternalSolarReflectance = NormalizeSingle(externalSolarReflectance);
            InternalSolarReflectance = NormalizeSingle(internalSolarReflectance);
            ExternalLightReflectance = NormalizeSingle(externalLightReflectance);
            InternalLightReflectance = NormalizeSingle(internalLightReflectance);
            ExternalEmissivity = NormalizeSingle(externalEmissivity);
            InternalEmissivity = NormalizeSingle(internalEmissivity);
            SolarTransmittance = NormalizeSingle(solarTransmittance);
            LightTransmittance = NormalizeSingle(lightTransmittance);
            DynamicViscosity = NormalizeSingle(dynamicViscosity);
            ConvectionCoefficient = NormalizeSingle(convectionCoefficient);
            IsBlind = isBlind;
        }

        /// <summary>The material's name, as written to <c>material.name</c>. Empty and absent normalise to null.</summary>
        public string Name { get; }

        /// <summary>The <c>TBD.MaterialTypes</c> value written to <c>material.type</c>.</summary>
        public int Type { get; }

        /// <summary>The description, normalised so empty and absent are the same thing.</summary>
        public string Description { get; }

        /// <summary>Thermal conductivity, W/mK.</summary>
        public float Conductivity { get; }

        /// <summary>Specific heat capacity. Left at the TBD default by the transparent and gas writes.</summary>
        public float SpecificHeat { get; }

        /// <summary>Density.</summary>
        public float Density { get; }

        /// <summary>Vapour diffusion factor.</summary>
        public float VapourDiffusionFactor { get; }

        /// <summary>External solar reflectance.</summary>
        public float ExternalSolarReflectance { get; }

        /// <summary>Internal solar reflectance.</summary>
        public float InternalSolarReflectance { get; }

        /// <summary>External light reflectance.</summary>
        public float ExternalLightReflectance { get; }

        /// <summary>Internal light reflectance.</summary>
        public float InternalLightReflectance { get; }

        /// <summary>External emissivity.</summary>
        public float ExternalEmissivity { get; }

        /// <summary>Internal emissivity.</summary>
        public float InternalEmissivity { get; }

        /// <summary>Solar transmittance. Written by the transparent path only.</summary>
        public float SolarTransmittance { get; }

        /// <summary>Light transmittance. Written by the transparent path only.</summary>
        public float LightTransmittance { get; }

        /// <summary>Dynamic viscosity. Written by the gas path only.</summary>
        public float DynamicViscosity { get; }

        /// <summary>Convection coefficient. Written by the gas path only.</summary>
        public float ConvectionCoefficient { get; }

        /// <summary>The blind flag, as the <c>int</c> TBD stores.</summary>
        public int IsBlind { get; }

        public bool Equals(ConstructionMaterialDefinition other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (!string.Equals(Name, other.Name, StringComparison.Ordinal) || Type != other.Type)
            {
                return false;
            }

            if (!string.Equals(Description, other.Description, StringComparison.Ordinal) || IsBlind != other.IsBlind)
            {
                return false;
            }

            return Conductivity.Equals(other.Conductivity)
                && SpecificHeat.Equals(other.SpecificHeat)
                && Density.Equals(other.Density)
                && VapourDiffusionFactor.Equals(other.VapourDiffusionFactor)
                && ExternalSolarReflectance.Equals(other.ExternalSolarReflectance)
                && InternalSolarReflectance.Equals(other.InternalSolarReflectance)
                && ExternalLightReflectance.Equals(other.ExternalLightReflectance)
                && InternalLightReflectance.Equals(other.InternalLightReflectance)
                && ExternalEmissivity.Equals(other.ExternalEmissivity)
                && InternalEmissivity.Equals(other.InternalEmissivity)
                && SolarTransmittance.Equals(other.SolarTransmittance)
                && LightTransmittance.Equals(other.LightTransmittance)
                && DynamicViscosity.Equals(other.DynamicViscosity)
                && ConvectionCoefficient.Equals(other.ConvectionCoefficient);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConstructionMaterialDefinition);
        }

        /// <summary>
        /// Consistent with <see cref="Equals(ConstructionMaterialDefinition)"/> and derived from the same
        /// deterministic signature the naming uses, never from <c>string.GetHashCode</c> - so a definition
        /// used as a dictionary key behaves the same on every runtime and build. Reuse itself never depends
        /// on this: the lookup is a full equality scan.
        /// </summary>
        public override int GetHashCode()
        {
            return unchecked((int)Query.Fnv1a(Query.ConstructionMaterialSignature(this) ?? string.Empty));
        }

        public override string ToString()
        {
            return Query.ConstructionMaterialSignature(this) ?? "ConstructionMaterialDefinition";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Signed zero to positive zero, and any NaN to the canonical <see cref="float.NaN"/>. Both keep the
        /// bit-pattern signature in agreement with an equality that treats those values as equal.
        /// </summary>
        internal static float NormalizeSingle(float value)
        {
            if (value == 0)
            {
                return 0f;
            }

            return float.IsNaN(value) ? float.NaN : value;
        }
    }
}
