// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The TBD zone GUID each space is currently stamped with
        /// (<c>SpaceParameter.ZoneGuid</c>), keyed by the space's own GUID, captured in one pass.
        /// <para>
        /// <c>Modify.UpdateIds</c> clears the stamp from every space BEFORE it re-resolves the zone
        /// (a stale stamp must never survive a failed resolution - TAS need not have kept the same
        /// zone GUIDs), so the resolution below it must read the identity captured HERE, not the
        /// just-cleared parameter. Spaces stating no stamp are simply absent.
        /// </para>
        /// </summary>
        public static Dictionary<System.Guid, string> SpaceZoneGuids(this IEnumerable<Space> spaces)
        {
            Dictionary<System.Guid, string> result = new Dictionary<System.Guid, string>();
            if (spaces == null)
            {
                return result;
            }

            foreach (Space space in spaces)
            {
                if (space == null)
                {
                    continue;
                }

                if (space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) && !string.IsNullOrWhiteSpace(zoneGuid))
                {
                    result[space.Guid] = zoneGuid;
                }
            }

            return result;
        }
    }
}
