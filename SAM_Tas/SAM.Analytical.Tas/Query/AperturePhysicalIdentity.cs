// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// One SAM aperture read into its physical identity - the four <c>ZoneSurfaceReference</c> stamps and
        /// the two building-element bindings - or <c>null</c> for an aperture that is not one.
        /// <para>
        /// A half-populated stamp (no zone GUID, or the <c>-1</c> sentinel surface number) is dropped rather
        /// than carried as a partial key: see <see cref="ZoneSurfaceKey(Core.Tas.ZoneSurfaceReference)"/>.
        /// </para>
        /// </summary>
        public static AperturePhysicalIdentity AperturePhysicalIdentity(this Aperture aperture)
        {
            if (aperture == null)
            {
                return null;
            }

            List<ZoneSurfaceKey> paneKeys_All = ApertureZoneSurfaceReferences(aperture, global::SAM.Analytical.AperturePart.Pane).ConvertAll(ZoneSurfaceKey).FindAll(x => x != null);
            List<ZoneSurfaceKey> frameKeys_All = ApertureZoneSurfaceReferences(aperture, global::SAM.Analytical.AperturePart.Frame).ConvertAll(ZoneSurfaceKey).FindAll(x => x != null);

            return new AperturePhysicalIdentity(
                aperture.Guid,
                ApertureZoneSurfaceKey(aperture, ApertureParameter.PaneZoneSurfaceReference_1),
                ApertureZoneSurfaceKey(aperture, ApertureParameter.PaneZoneSurfaceReference_2),
                ApertureZoneSurfaceKey(aperture, ApertureParameter.FrameZoneSurfaceReference_1),
                ApertureZoneSurfaceKey(aperture, ApertureParameter.FrameZoneSurfaceReference_2),
                ApertureBuildingElementGuid(aperture, ApertureParameter.PaneBuildingElementGuid),
                ApertureBuildingElementGuid(aperture, ApertureParameter.FrameBuildingElementGuid),
                paneKeys_All,
                frameKeys_All,
                aperture.HasValue(ApertureZoneSurfaceReferencesParameter(global::SAM.Analytical.AperturePart.Pane)),
                aperture.HasValue(ApertureZoneSurfaceReferencesParameter(global::SAM.Analytical.AperturePart.Frame)));
        }

        /// <summary>
        /// The physical index over a set of SAM apertures.
        /// <para>
        /// Pass the WHOLE model rather than one panel's apertures: an ambiguity is only visible to an index
        /// that can see both claimants, and an index built from a subset would answer confidently where the
        /// full one refuses.
        /// </para>
        /// </summary>
        public static AperturePhysicalIndex AperturePhysicalIndex(IEnumerable<Aperture> apertures)
        {
            List<AperturePhysicalIdentity> aperturePhysicalIdentities = new List<AperturePhysicalIdentity>();

            if (apertures != null)
            {
                foreach (Aperture aperture in apertures)
                {
                    AperturePhysicalIdentity aperturePhysicalIdentity = AperturePhysicalIdentity(aperture);
                    if (aperturePhysicalIdentity != null)
                    {
                        aperturePhysicalIdentities.Add(aperturePhysicalIdentity);
                    }
                }
            }

            return new AperturePhysicalIndex(aperturePhysicalIdentities);
        }

        private static ZoneSurfaceKey ApertureZoneSurfaceKey(Aperture aperture, ApertureParameter apertureParameter)
        {
            if (!aperture.TryGetValue(apertureParameter, out Core.Tas.ZoneSurfaceReference zoneSurfaceReference) || zoneSurfaceReference == null)
            {
                return null;
            }

            return ZoneSurfaceKey(zoneSurfaceReference);
        }

        private static string ApertureBuildingElementGuid(Aperture aperture, ApertureParameter apertureParameter)
        {
            if (!aperture.TryGetValue(apertureParameter, out string buildingElementGuid) || string.IsNullOrWhiteSpace(buildingElementGuid))
            {
                return null;
            }

            return buildingElementGuid;
        }
    }
}
