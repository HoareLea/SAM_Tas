// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        /// <summary>
        /// The one place that reads a native TAS identifier off a TPD object.
        /// <para>
        /// <b>Why it is late-bound, and why that is not a style choice.</b> Measured on licensed TAS: the
        /// <i>typed</i> <c>ISystemComponent.GUID</c> and <c>.Name</c> accessors <b>throw</b>
        /// <c>InvalidCastException: OleAut reported a type mismatch</c> on a <c>SystemZone</c>, and on a
        /// <c>Fan</c> the typed <c>.GUID</c> answered the string <c>"0"</c>. The late-bound read answers a
        /// real guid in both cases. Rewriting these as typed property reads would look like a tidy-up and
        /// would break the route.
        /// </para>
        /// <para>
        /// It lives here, once, so that fact is stated in one place rather than spread as <c>dynamic</c>
        /// reads through the conversion - which is what the licensed checkpoint asked for.
        /// </para>
        /// </summary>
        /// <returns>The identifier, or <c>null</c> when the object has none or refuses to answer.</returns>
        public static string NativeReference(object tPDObject)
        {
            if (tPDObject == null)
            {
                return null;
            }

            try
            {
                object value = ((dynamic)tPDObject).GUID;

                string result = value as string;

                if (string.IsNullOrWhiteSpace(result))
                {
                    return null;
                }

                // A Fan answered the literal "0" through the typed accessor. Whatever produced that, it is
                // not an identifier, and it must never be allowed to become one.
                if (result.Trim() == "0")
                {
                    return null;
                }

                return result;
            }
            catch
            {
                // A component that will not answer at all is refused by the caller, which can name the room.
                return null;
            }
        }

        /// <summary>
        /// The native identifier of the <b>zone load</b> a system zone is bound to - a different identifier
        /// from the zone component's own, and the one the result side resolves through.
        /// <para>
        /// Measured: for a zone bound by <c>AddZoneLoad</c>, <c>SystemZone.GUID</c> and
        /// <c>ZoneLoad.GUID</c> are <b>distinct</b>, and only the latter resolves through
        /// <c>TSDData.GetZoneLoadForGuid</c>. Neither substitutes for the other.
        /// </para>
        /// </summary>
        public static string NativeReference_ZoneLoad(SystemZone systemZone)
        {
            if (systemZone == null)
            {
                return null;
            }

            try
            {
                ZoneLoad zoneLoad = systemZone.GetSystemZoneZoneLoad();

                return zoneLoad == null ? null : NativeReference(zoneLoad);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Whether a captured identifier still resolves to the same component through the system it was
        /// captured from. A reference that does not round-trip is not an identity and the caller refuses.
        /// </summary>
        public static bool RoundTrips(global::TPD.System system, string reference)
        {
            if (system == null || string.IsNullOrWhiteSpace(reference))
            {
                return false;
            }

            try
            {
                SystemComponent systemComponent = system.GetComponentByGUID(reference) as SystemComponent;

                return systemComponent != null
                    && string.Equals(NativeReference(systemComponent), reference, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
