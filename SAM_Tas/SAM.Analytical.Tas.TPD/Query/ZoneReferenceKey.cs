// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        /// <summary>
        /// The one canonical form of a TAS building-zone guid, so that the same zone compares equal however
        /// a given API spelled it.
        /// <para>
        /// The same identifier reaches the thermostat bridge from four places - <c>SpaceParameter.ZoneGuid</c>
        /// on the analytical room, the TPD <c>ZoneLoad.GUID</c> the Systems route bound, <c>zone.GUID</c> in
        /// the TBD copy and <c>ZoneData.zoneGUID</c> in the second TSD - and TAS answers it braced
        /// (<c>{C0B7E340-...}</c>) while a bare form is equally accepted by
        /// <c>TSDData.GetZoneLoadForGuid</c>. A guid is compared as a guid; anything that does not parse is
        /// compared as its trimmed, upper-cased text, never loosened further.
        /// </para>
        /// </summary>
        /// <returns>The canonical key, or null for a blank reference.</returns>
        public static string ZoneReferenceKey(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            string trimmed = reference.Trim();

            return Guid.TryParse(trimmed, out Guid guid) ? guid.ToString("D") : trimmed.ToUpperInvariant();
        }
    }
}
