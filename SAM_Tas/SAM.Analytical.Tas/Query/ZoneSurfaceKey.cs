// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The comparable form of a TAS zone GUID. TAS reports a GUID as a string, and the same zone can be
        /// written braced or bare and in either case; comparing the raw strings would make two spellings of
        /// one zone two zones, which on this path means a physical surface silently failing to resolve.
        /// <para>
        /// Trimmed, unbraced and upper-cased, and parsed through <see cref="Guid"/> when it will parse so
        /// that any accepted spelling collapses to one. A string that is not a GUID at all is kept (trimmed
        /// and upper-cased) rather than discarded - a foreign TBD may use anything, and an opaque identifier
        /// still identifies a zone as long as both sides spell it the same way.
        /// </para>
        /// </summary>
        public static string NormalizeZoneGuid(string zoneGuid)
        {
            if (string.IsNullOrWhiteSpace(zoneGuid))
            {
                return null;
            }

            string result = zoneGuid.Trim();

            if (Guid.TryParse(result, out Guid guid))
            {
                return guid.ToString("D").ToUpperInvariant();
            }

            return result.ToUpperInvariant();
        }

        /// <summary>
        /// The physical key a SAM stamp names, or <c>null</c> when the stamp does not name a surface at all.
        /// <para>
        /// A reference with no zone GUID or the <c>-1</c> sentinel surface number is deliberately NOT turned
        /// into a key: a key that matched everything would cross-bind two windows, which is the one outcome
        /// this stage exists to prevent. Callers see <c>null</c> and refuse.
        /// </para>
        /// </summary>
        public static ZoneSurfaceKey ZoneSurfaceKey(this Core.Tas.ZoneSurfaceReference zoneSurfaceReference)
        {
            if (zoneSurfaceReference == null)
            {
                return null;
            }

            ZoneSurfaceKey result = new ZoneSurfaceKey(zoneSurfaceReference.ZoneGuid, zoneSurfaceReference.SurfaceNumber);

            return result.IsValid ? result : null;
        }

        /// <summary>The physical key of a zone surface, or <c>null</c> when the zone does not identify itself.</summary>
        public static ZoneSurfaceKey ZoneSurfaceKey(string zoneGuid, int surfaceNumber)
        {
            ZoneSurfaceKey result = new ZoneSurfaceKey(zoneGuid, surfaceNumber);

            return result.IsValid ? result : null;
        }
    }
}
