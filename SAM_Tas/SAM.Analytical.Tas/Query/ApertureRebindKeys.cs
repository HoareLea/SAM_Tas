// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Produces the complete, COM-free physical-surface plan for rebinding one aperture part.
        /// <para>
        /// Every key must resolve uniquely back to the requested aperture and part, exist in the TBD-side
        /// binding index, and still point to <paramref name="buildingElementGuid_From"/>. Any failure refuses
        /// the whole plan, so a caller can validate here before it creates or mutates a replacement definition.
        /// </para>
        /// </summary>
        public static List<ZoneSurfaceKey> ApertureRebindKeys(
            AperturePhysicalIdentity aperturePhysicalIdentity,
            AperturePart aperturePart,
            AperturePhysicalIndex aperturePhysicalIndex,
            IReadOnlyDictionary<ZoneSurfaceKey, string> buildingElementGuidsBySurface,
            string buildingElementGuid_From,
            out string refusal)
        {
            refusal = null;

            if (aperturePhysicalIdentity == null || aperturePhysicalIndex == null || buildingElementGuidsBySurface == null || string.IsNullOrWhiteSpace(buildingElementGuid_From))
            {
                refusal = "The physical rebind plan is incomplete.";
                return null;
            }

            if (!aperturePhysicalIdentity.SurfaceSetComplete(aperturePart))
            {
                refusal = string.Format("SAM aperture '{0}' has representative {1} side stamps but no preserved complete physical surface set; it must be restamped by export, import or UpdateIds before it can be rebound safely.",
                    aperturePhysicalIdentity.ApertureGuid,
                    aperturePart);
                return null;
            }

            List<KeyValuePair<int, ZoneSurfaceKey>> allKeys = aperturePhysicalIdentity.AllKeys(aperturePart);
            if (allKeys.Any(x => x.Key < 1 || x.Key > 2))
            {
                refusal = string.Format("SAM aperture '{0}' states {1} surfaces in more than two zones.", aperturePhysicalIdentity.ApertureGuid, aperturePart);
                return null;
            }

            List<ZoneSurfaceKey> keys = allKeys
                .Select(x => x.Value)
                .Where(x => x != null && x.IsValid)
                .Distinct()
                .ToList();

            keys.Sort(CompareZoneSurfaceKeys);

            if (keys.Count == 0)
            {
                refusal = string.Format("SAM aperture '{0}' states no physical {1} surfaces.", aperturePhysicalIdentity.ApertureGuid, aperturePart);
                return null;
            }

            foreach (ZoneSurfaceKey key in keys)
            {
                if (!aperturePhysicalIndex.TryResolve(key, out System.Guid apertureGuid_Owner, out AperturePart aperturePart_Owner, out int _, out string refusal_Owner)
                    || apertureGuid_Owner != aperturePhysicalIdentity.ApertureGuid
                    || aperturePart_Owner != aperturePart)
                {
                    refusal = string.Format("Physical surface {0} does not resolve uniquely back to SAM aperture '{1}' as its {2}{3}.",
                        key,
                        aperturePhysicalIdentity.ApertureGuid,
                        aperturePart,
                        refusal_Owner == null ? string.Empty : " - " + refusal_Owner);
                    return null;
                }

                if (!buildingElementGuidsBySurface.TryGetValue(key, out string buildingElementGuid_Current))
                {
                    refusal = string.Format("Physical surface {0} could not be found in the TBD.", key);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(buildingElementGuid_Current) || buildingElementGuid_Current != buildingElementGuid_From)
                {
                    refusal = string.Format("Physical surface {0} is not currently bound to the element the aperture stamp claims.", key);
                    return null;
                }
            }

            return keys;
        }
    }
}
