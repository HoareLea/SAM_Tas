// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// <b>The one aperture-definition resolver.</b> Given a SAM aperture and which half of it is wanted,
        /// returns the shared <c>TBD.Construction</c> and aperture <c>TBD.buildingElement</c> that state its
        /// content - reusing the ones already in the building where an equivalent exists, and creating them
        /// under a definition-derived name where none does.
        /// <para>
        /// This block used to live inline in <c>Modify.Update</c>. It is extracted here unchanged so that the
        /// gbXML/T3D route (<c>Modify.UpdateApertureDefinitions</c>) resolves definitions through the SAME
        /// code rather than through a second copy of the same rules. The two routes differ in how the
        /// PHYSICAL surfaces come to exist - the direct export creates them, TAS's own gbXML conversion
        /// creates them - and in nothing else.
        /// </para>
        /// <para>
        /// <b>A TBD Construction and an aperture buildingElement are both REUSABLE DEFINITIONS</b>, shared by
        /// every element and every surface that states the same thing - the same relationship a TBD
        /// ApertureType has, one level up. The physical windows are <c>zoneSurface</c>s, and they stay one per
        /// window whatever happens here; what is resolved below is how many DEFINITIONS those surfaces point
        /// at.
        /// </para>
        /// <para>
        /// <b>Identity is the DEFINITION and never the name.</b> Full content equality decides reuse, and a
        /// name taken by different content gets a deterministic collision suffix rather than being adopted.
        /// <b>A shared definition is IMMUTABLE</b> - on a hit nothing whatever is written to it, not even
        /// rewritten to the same value, because every other aperture referencing it would see the write.
        /// </para>
        /// </summary>
        /// <param name="building">The open TBD building.</param>
        /// <param name="buildingReuseCache">The open document's reuse cache. Definitions are found in, and registered back into, this.</param>
        /// <param name="aperture">The SAM aperture whose content is wanted.</param>
        /// <param name="aperturePart">Which half of it - pane or frame.</param>
        /// <param name="materialLibrary">The model's material library, for the layer content.</param>
        /// <param name="constructionsByName">Construction names this caller has created outside the cache (the panel constructions). Updated with anything created here. May be null.</param>
        /// <param name="buildingElementsByName">The same for building elements. May be null.</param>
        /// <param name="construction">The construction stating this aperture part's content, or null.</param>
        /// <param name="buildingElement">The aperture building element stating it, or null.</param>
        /// <param name="constructionDefinition">The construction definition that was resolved, or null.</param>
        /// <param name="buildingElementDefinition">The building element definition that was resolved, or null.</param>
        /// <param name="created_Construction">Whether <paramref name="construction"/> was created by this call rather than found by content. A caller comparing the created name against a preferred one - to decide whether a name was genuinely SUPERSEDED by foreign content, as opposed to an ordinary cross-family reuse hit under a different name - must gate on this: a reuse hit can legitimately carry any name, and treating that as superseded would rename a construction another aperture already correctly shares.</param>
        /// <param name="created_BuildingElement">Whether <paramref name="buildingElement"/> was created by this call rather than found.</param>
        /// <param name="refusal">Why no definition could be resolved, or null on success.</param>
        /// <returns>True when both a construction and a building element came back.</returns>
        public static bool ResolveApertureDefinition(
            this Building building,
            BuildingReuseCache buildingReuseCache,
            Aperture aperture,
            AperturePart aperturePart,
            MaterialLibrary materialLibrary,
            IDictionary<string, TBD.Construction> constructionsByName,
            IDictionary<string, buildingElement> buildingElementsByName,
            out TBD.Construction construction,
            out buildingElement buildingElement,
            out ConstructionDefinition constructionDefinition,
            out BuildingElementDefinition buildingElementDefinition,
            out bool created_Construction,
            out bool created_BuildingElement,
            out string refusal)
        {
            construction = null;
            buildingElement = null;
            constructionDefinition = null;
            buildingElementDefinition = null;
            created_Construction = false;
            created_BuildingElement = false;
            refusal = null;

            if (building == null || buildingReuseCache == null || aperture == null)
            {
                refusal = "No TBD building, reuse cache or SAM aperture to resolve an aperture definition from.";
                return false;
            }

            ApertureConstruction apertureConstruction = aperture.ApertureConstruction;

            constructionDefinition = apertureConstruction.ConstructionDefinition(aperturePart, materialLibrary, out string refusal_ConstructionDefinition);

            construction = buildingReuseCache.FindConstruction(constructionDefinition);
            if (construction == null && apertureConstruction != null)
            {
                //The whole namespace: the cache's own pass over the building, plus everything the caller has
                //created since - the panel constructions included, because they share it.
                IEnumerable<string> names_Construction = constructionsByName == null
                    ? buildingReuseCache.ConstructionNames()
                    : buildingReuseCache.ConstructionNames().Concat(constructionsByName.Keys);

                string constructionName = Query.ConstructionName(names_Construction, constructionDefinition, apertureConstruction.Name, out string refusal_ConstructionName);
                if (constructionName == null)
                {
                    refusal = refusal_ConstructionName ?? refusal_ConstructionDefinition;
                }
                else
                {
                    construction = building.AddConstruction(null);
                    construction.name = constructionName;
                    created_Construction = true;

                    //Reserved the moment the name exists in the TBD, BEFORE anything else is written: a
                    //creation whose write later fails is never withdrawn, so it must never become reusable -
                    //but its name still occupies the namespace.
                    buildingReuseCache.ReserveConstruction(construction);

                    if (apertureConstruction.Transparent(materialLibrary, aperturePart))
                    {
                        construction.type = TBD.ConstructionTypes.tcdTransparentConstruction;
                    }

                    List<ConstructionLayer> constructionLayers = apertureConstruction.GetConstructionLayers(aperturePart);
                    if (constructionLayers != null && constructionLayers.Count != 0)
                    {
                        int index = 1;
                        foreach (ConstructionLayer constructionLayer in constructionLayers)
                        {
                            Material material = materialLibrary?.GetMaterial(constructionLayer.Name) as Material;
                            if (material == null)
                            {
                                continue;
                            }

                            TBD.material material_TBD = construction.AddMaterial(material);
                            if (material_TBD != null)
                            {
                                material_TBD.width = System.Convert.ToSingle(constructionLayer.Thickness);
                                construction.materialWidth[index] = System.Convert.ToSingle(constructionLayer.Thickness);
                                index++;
                            }
                        }
                    }

                    if (constructionsByName != null && !string.IsNullOrEmpty(construction.name))
                    {
                        constructionsByName[construction.name] = construction;
                    }

                    //The write is complete, so the reservation becomes a reusable registration - unless the
                    //content could not be predicted, in which case the construction stands but is never shared.
                    if (constructionDefinition != null && constructionDefinition.Proven)
                    {
                        buildingReuseCache.RegisterConstruction(construction, constructionDefinition);
                    }
                }
            }

            if (construction == null)
            {
                if (refusal == null)
                {
                    refusal = string.Format("SAM aperture '{0}' ({1}) states no aperture construction for its {2}, so no TBD construction was resolved{3}.",
                        aperture.Name, aperture.Guid, aperturePart, refusal_ConstructionDefinition == null ? string.Empty : " - " + refusal_ConstructionDefinition);
                }

                return false;
            }

            buildingElementDefinition = aperture.BuildingElementDefinition(aperturePart, constructionDefinition, buildingReuseCache.DayTypeNames, out string refusal_BuildingElementDefinition);

            buildingElement = buildingReuseCache.FindApertureBuildingElement(buildingElementDefinition);
            if (buildingElement == null)
            {
                IEnumerable<string> names_BuildingElement = buildingElementsByName == null
                    ? buildingReuseCache.BuildingElementNames()
                    : buildingReuseCache.BuildingElementNames().Concat(buildingElementsByName.Keys);

                string buildingElementName = Query.BuildingElementName(names_BuildingElement, buildingElementDefinition, apertureConstruction?.Name, out string refusal_BuildingElementName);
                if (buildingElementName == null)
                {
                    refusal = refusal_BuildingElementName ?? refusal_BuildingElementDefinition;
                    return false;
                }

                buildingElement = building.AddBuildingElement();
                buildingElement.name = buildingElementName;

                buildingReuseCache.ReserveApertureBuildingElement(buildingElement);

                buildingElement.SetColor(aperture, aperturePart);

                buildingElement.BEType = Query.BEType(aperturePart);
                buildingElement.AssignConstruction(construction);
                created_BuildingElement = true;

                if (buildingElementsByName != null && !string.IsNullOrEmpty(buildingElement.name))
                {
                    buildingElementsByName[buildingElement.name] = buildingElement;
                }

                //The openings are written ONCE, onto the element that will now stand for every equivalent
                //aperture. Only a pane carries them, exactly as before.
                int count_ApertureTypes = 0;
                if (aperturePart == AperturePart.Pane && aperture.TryGetValue(Analytical.ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties))
                {
                    List<TBD.ApertureType> apertureTypes = SetApertureTypes(building, buildingElement, openingProperties, null, buildingReuseCache);
                    count_ApertureTypes = apertureTypes == null ? 0 : apertureTypes.Count;
                }

                //Shareable only once the element really carries what the definition says it carries. A partly
                //written opening set would otherwise be handed to the next aperture as though it were
                //complete - and a shared element is never written to again, so there would be no correcting it.
                if (buildingElementDefinition != null && buildingElementDefinition.Proven && count_ApertureTypes == buildingElementDefinition.ApertureTypeCount)
                {
                    buildingReuseCache.RegisterApertureBuildingElement(buildingElement, buildingElementDefinition);
                }
            }

            return buildingElement != null;
        }
    }
}
