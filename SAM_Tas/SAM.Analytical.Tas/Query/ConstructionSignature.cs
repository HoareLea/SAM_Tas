// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// A deterministic, build-stable fingerprint of a <see cref="ConstructionMaterialDefinition"/>. Every
        /// float is carried as its exact IEEE-754 bit pattern (see <c>SingleBitsHex</c>), never as rounded
        /// display text, so two materials equality keeps apart can never collide here.
        /// <para>
        /// Used only to generate and collision-resolve NAMES and to derive a build-stable hash code - never
        /// to decide reuse, which is full field equality.
        /// </para>
        /// </summary>
        public static string ConstructionMaterialSignature(this ConstructionMaterialDefinition constructionMaterialDefinition)
        {
            if (constructionMaterialDefinition == null)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();

            //The name and description are hashed rather than embedded: this string is a fingerprint, and a
            //construction with a dozen layers would otherwise carry a dozen free-text names.
            stringBuilder.AppendFormat("N{0}", Fnv1aHex(constructionMaterialDefinition.Name ?? string.Empty));
            stringBuilder.AppendFormat(" T{0}", constructionMaterialDefinition.Type);
            stringBuilder.AppendFormat(" D{0}", Fnv1aHex(constructionMaterialDefinition.Description ?? string.Empty));
            stringBuilder.AppendFormat(" B{0}", constructionMaterialDefinition.IsBlind);

            //Order fixed here and never changed: the same definition must fingerprint the same on every
            //export, so that a repeated export resolves a collision to the same name it did last time.
            float[] values =
            {
                constructionMaterialDefinition.Conductivity,
                constructionMaterialDefinition.SpecificHeat,
                constructionMaterialDefinition.Density,
                constructionMaterialDefinition.VapourDiffusionFactor,
                constructionMaterialDefinition.ExternalSolarReflectance,
                constructionMaterialDefinition.InternalSolarReflectance,
                constructionMaterialDefinition.ExternalLightReflectance,
                constructionMaterialDefinition.InternalLightReflectance,
                constructionMaterialDefinition.ExternalEmissivity,
                constructionMaterialDefinition.InternalEmissivity,
                constructionMaterialDefinition.SolarTransmittance,
                constructionMaterialDefinition.LightTransmittance,
                constructionMaterialDefinition.DynamicViscosity,
                constructionMaterialDefinition.ConvectionCoefficient
            };

            stringBuilder.Append(" P");
            foreach (float value in values)
            {
                stringBuilder.Append(SingleBitsHex(value));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// A deterministic fingerprint of one construction layer: its two widths as exact bit patterns, and
        /// its material's fingerprint. An unresolved material contributes a fixed marker, so an unproven
        /// layer still fingerprints stably without pretending to know what it holds.
        /// </summary>
        public static string ConstructionLayerSignature(this ConstructionLayerDefinition constructionLayerDefinition)
        {
            if (constructionLayerDefinition == null)
            {
                return null;
            }

            return string.Format(
                "W{0} MW{1} M[{2}]",
                SingleBitsHex(constructionLayerDefinition.Width),
                SingleBitsHex(constructionLayerDefinition.MaterialWidth),
                ConstructionMaterialSignature(constructionLayerDefinition.Material) ?? "unresolved");
        }

        /// <summary>
        /// A deterministic, build-stable fingerprint of a <see cref="ConstructionDefinition"/>, used to
        /// generate and collision-resolve TBD construction NAMES - never to decide reuse.
        /// <para>
        /// Shape: <c>&lt;part&gt; T&lt;type&gt; AHT&lt;bits&gt; D&lt;hash&gt; L&lt;count&gt;
        /// [&lt;layer signature&gt;…]</c>. The layer count is present as well as the layers so that a
        /// definition's fingerprint can never be a prefix of a longer one's, and the layer signatures are
        /// concatenated in TBD order because a construction IS its layer sequence.
        /// </para>
        /// </summary>
        public static string ConstructionSignature(this ConstructionDefinition constructionDefinition)
        {
            if (constructionDefinition == null)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(constructionDefinition.AperturePart.ToString());
            stringBuilder.AppendFormat(" T{0}", constructionDefinition.Type);
            stringBuilder.AppendFormat(" AHT{0}", SingleBitsHex(constructionDefinition.AdditionalHeatTransfer));
            stringBuilder.AppendFormat(" D{0}", Fnv1aHex(constructionDefinition.Description ?? string.Empty));
            stringBuilder.AppendFormat(" L{0}", constructionDefinition.LayerCount);

            foreach (ConstructionLayerDefinition constructionLayerDefinition in constructionDefinition.Layers)
            {
                stringBuilder.AppendFormat(" [{0}]", ConstructionLayerSignature(constructionLayerDefinition) ?? "unresolved");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// The eight-hex-digit discriminator appended to a preferred construction name when that name is
        /// already taken by a DIFFERENT definition. Derived from the full signature - which carries the exact
        /// float bit patterns - so the same definition resolves to the same discriminator on every repeated
        /// export.
        /// </summary>
        public static string ConstructionSignatureHash(this ConstructionDefinition constructionDefinition)
        {
            string signature = ConstructionSignature(constructionDefinition);

            return signature == null ? null : Fnv1aHex(signature);
        }

        /// <summary>
        /// A deterministic fingerprint of one opening on a building element: its ordinal, and the aperture
        /// control it states. Reuses <see cref="ApertureTypeSignature(ApertureTypeDefinition)"/> so one
        /// definition of "what makes two controls the same control" serves both stages.
        /// </summary>
        public static string ApertureTypeAssignmentSignature(this ApertureTypeAssignment apertureTypeAssignment)
        {
            if (apertureTypeAssignment == null)
            {
                return null;
            }

            return string.Format(
                "O{0} A[{1}]",
                apertureTypeAssignment.Ordinal,
                ApertureTypeSignature(apertureTypeAssignment.ApertureTypeDefinition) ?? "unresolved");
        }

        /// <summary>
        /// A deterministic, build-stable fingerprint of a <see cref="BuildingElementDefinition"/>, used to
        /// generate and collision-resolve aperture building-element NAMES - never to decide reuse.
        /// <para>
        /// Shape: <c>&lt;apertureType&gt; &lt;part&gt; BE&lt;bEType&gt; C&lt;colour&gt;
        /// K[&lt;construction signature&gt;] A&lt;count&gt; [&lt;opening signature&gt;…]</c>. The opening
        /// count is present as well as the openings, so a bare element can never fingerprint as a prefix of
        /// one that carries openings.
        /// </para>
        /// </summary>
        public static string BuildingElementSignature(this BuildingElementDefinition buildingElementDefinition)
        {
            if (buildingElementDefinition == null)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(buildingElementDefinition.ApertureType.ToString());
            stringBuilder.AppendFormat(" {0}", buildingElementDefinition.AperturePart.ToString());
            stringBuilder.AppendFormat(" BE{0}", buildingElementDefinition.BEType);
            stringBuilder.AppendFormat(" C{0:X8}", buildingElementDefinition.Colour);
            stringBuilder.AppendFormat(" K[{0}]", ConstructionSignature(buildingElementDefinition.ConstructionDefinition) ?? "unresolved");
            stringBuilder.AppendFormat(" A{0}", buildingElementDefinition.ApertureTypeCount);

            foreach (ApertureTypeAssignment apertureTypeAssignment in buildingElementDefinition.ApertureTypes)
            {
                stringBuilder.AppendFormat(" [{0}]", ApertureTypeAssignmentSignature(apertureTypeAssignment) ?? "unresolved");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// The eight-hex-digit discriminator appended to a preferred aperture building-element name when that
        /// name is already taken by a DIFFERENT definition.
        /// </summary>
        public static string BuildingElementSignatureHash(this BuildingElementDefinition buildingElementDefinition)
        {
            string signature = BuildingElementSignature(buildingElementDefinition);

            return signature == null ? null : Fnv1aHex(signature);
        }
    }
}
