// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>The COM-free half of the gbXML aperture canonicalisation: which reusable definition each
        /// physical aperture part asks for.</b>
        /// <para>
        /// One <see cref="ApertureDefinitionBinding"/> per aperture per part that physically exists, in the
        /// same part order the direct export uses (frame, then pane), each carrying the
        /// <see cref="Tas.ConstructionDefinition"/> and <see cref="Tas.BuildingElementDefinition"/> produced
        /// by the SAME factories <c>Modify.ResolveApertureDefinition</c> resolves against. There is no second
        /// set of equality rules here - this decides nothing about reuse, it states what each part asks for,
        /// and the definitions' own <c>Equals</c> decides how many distinct answers that is.
        /// </para>
        /// <para>
        /// <b>A part exists when it has thickness</b>, exactly as the direct export decides it
        /// (<c>aperture.GetThickness(part) &gt; 0</c>). That is what makes this a faithful mirror of the
        /// direct route rather than an approximation of it: the two must agree on how many definitions one
        /// model needs, because the direct route is the behavioural reference.
        /// </para>
        /// <para>
        /// <b>Pane and frame can never collapse into one definition</b>, however identical their layer
        /// content: <see cref="Analytical.AperturePart"/> is a field of both definitions and
        /// <see cref="BEType(Analytical.AperturePart)"/> differs between them.
        /// </para>
        /// </summary>
        /// <param name="apertures">The physical SAM apertures.</param>
        /// <param name="materialLibrary">The model's material library, for the layer content.</param>
        /// <param name="dayTypeNames">The day types an aperture control is assigned to - <c>BuildingReuseCache.DayTypeNames</c>.</param>
        /// <returns>One binding per aperture per existing part, never null.</returns>
        public static List<ApertureDefinitionBinding> ApertureDefinitionBindings(IEnumerable<Aperture> apertures, Core.MaterialLibrary materialLibrary, IEnumerable<string> dayTypeNames)
        {
            List<ApertureDefinitionBinding> result = new List<ApertureDefinitionBinding>();

            if (apertures == null)
            {
                return result;
            }

            List<string> dayTypeNames_Temp = dayTypeNames == null ? new List<string>() : dayTypeNames.ToList();

            foreach (Aperture aperture in apertures)
            {
                if (aperture == null)
                {
                    continue;
                }

                //Frame before pane, matching the direct export's own ordering so a name budget consumed in
                //one route is consumed in the same order in the other.
                foreach (Analytical.AperturePart aperturePart in new Analytical.AperturePart[] { Analytical.AperturePart.Frame, Analytical.AperturePart.Pane })
                {
                    if (!ApertureHasPart(aperture, aperturePart))
                    {
                        continue;
                    }

                    result.Add(ApertureDefinitionBinding(aperture, aperturePart, materialLibrary, dayTypeNames_Temp));
                }
            }

            return result;
        }

        /// <summary>
        /// The single-part case of
        /// <see cref="ApertureDefinitionBindings(IEnumerable{Aperture}, Core.MaterialLibrary, IEnumerable{string})"/>,
        /// used by the COM pass, which knows from the physical stamps which parts really exist in the TBD and
        /// so does not consult the thickness rule.
        /// </summary>
        public static ApertureDefinitionBinding ApertureDefinitionBinding(Aperture aperture, Analytical.AperturePart aperturePart, Core.MaterialLibrary materialLibrary, IEnumerable<string> dayTypeNames)
        {
            if (aperture == null)
            {
                return null;
            }

            ConstructionDefinition constructionDefinition = aperture.ApertureConstruction.ConstructionDefinition(aperturePart, materialLibrary, out string refusal_Construction);
            BuildingElementDefinition buildingElementDefinition = aperture.BuildingElementDefinition(aperturePart, constructionDefinition, dayTypeNames, out string refusal_BuildingElement);

            //The construction refusal is reported first: an element refusal that follows one is a consequence
            //of it, not an independent finding.
            string refusal = constructionDefinition == null ? refusal_Construction : (buildingElementDefinition == null ? refusal_BuildingElement : null);

            return new ApertureDefinitionBinding(aperture.Guid, aperturePart, constructionDefinition, buildingElementDefinition, refusal);
        }

        /// <summary>
        /// Whether <paramref name="aperture"/> physically has <paramref name="aperturePart"/> - the direct
        /// export's own test, stated once so both routes ask it the same way.
        /// </summary>
        public static bool ApertureHasPart(this Aperture aperture, Analytical.AperturePart aperturePart)
        {
            if (aperture == null || aperturePart == Analytical.AperturePart.Undefined)
            {
                return false;
            }

            double thickness = aperture.GetThickness(aperturePart);

            return !double.IsNaN(thickness) && thickness > 0;
        }

        /// <summary>
        /// The distinct reusable element definitions a set of bindings asks for - the count a caller asserts
        /// against. Unproven definitions are excluded: they are created and named but never shared, so they
        /// are not part of the reusable set.
        /// </summary>
        public static List<BuildingElementDefinition> DistinctBuildingElementDefinitions(this IEnumerable<ApertureDefinitionBinding> apertureDefinitionBindings)
        {
            List<BuildingElementDefinition> result = new List<BuildingElementDefinition>();

            if (apertureDefinitionBindings == null)
            {
                return result;
            }

            foreach (ApertureDefinitionBinding apertureDefinitionBinding in apertureDefinitionBindings)
            {
                if (apertureDefinitionBinding == null || !apertureDefinitionBinding.Shareable)
                {
                    continue;
                }

                if (!result.Exists(x => x.Equals(apertureDefinitionBinding.BuildingElementDefinition)))
                {
                    result.Add(apertureDefinitionBinding.BuildingElementDefinition);
                }
            }

            return result;
        }

        /// <summary>
        /// The distinct reusable constructions a set of bindings asks for. Pane and frame stay separate
        /// however identical their layers.
        /// </summary>
        public static List<ConstructionDefinition> DistinctConstructionDefinitions(this IEnumerable<ApertureDefinitionBinding> apertureDefinitionBindings)
        {
            List<ConstructionDefinition> result = new List<ConstructionDefinition>();

            if (apertureDefinitionBindings == null)
            {
                return result;
            }

            foreach (ApertureDefinitionBinding apertureDefinitionBinding in apertureDefinitionBindings)
            {
                ConstructionDefinition constructionDefinition = apertureDefinitionBinding?.ConstructionDefinition;
                if (constructionDefinition == null || !constructionDefinition.Proven)
                {
                    continue;
                }

                if (!result.Exists(x => x.Equals(constructionDefinition)))
                {
                    result.Add(constructionDefinition);
                }
            }

            return result;
        }
    }
}
