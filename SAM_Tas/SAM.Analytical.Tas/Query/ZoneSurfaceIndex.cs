// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <c>{ZoneGuid, SurfaceNumber}</c> -> the physical <c>zoneSurface</c>, over every zone in the
        /// building - the resolution a rebind needs to turn a <see cref="Core.Tas.ZoneSurfaceReference"/>
        /// stamp back into the real TBD object.
        /// <para>
        /// Keyed by <see cref="ZoneSurfaceKey"/> rather than a formatted string, so this index and every other
        /// physical comparison in the codebase agree about what one surface is - including that two spellings
        /// of one zone GUID are one zone.
        /// </para>
        /// <para>
        /// Shared by <c>Modify.UpdateBuildingElements</c> and <c>Modify.UpdateApertureDefinitions</c>: both
        /// rebind physical surfaces, and they must resolve a stamp to the same object.
        /// </para>
        /// </summary>
        public static Dictionary<ZoneSurfaceKey, TBD.IZoneSurface> ZoneSurfaceIndex(this TBD.Building building)
        {
            Dictionary<ZoneSurfaceKey, TBD.IZoneSurface> result = new Dictionary<ZoneSurfaceKey, TBD.IZoneSurface>();

            List<TBD.zone> zones = building?.Zones();
            if (zones == null)
            {
                return result;
            }

            foreach (TBD.zone zone in zones)
            {
                if (zone == null)
                {
                    continue;
                }

                List<TBD.IZoneSurface> zoneSurfaces = zone.ZoneSurfaces();
                if (zoneSurfaces == null)
                {
                    continue;
                }

                foreach (TBD.IZoneSurface zoneSurface in zoneSurfaces)
                {
                    if (zoneSurface == null)
                    {
                        continue;
                    }

                    ZoneSurfaceKey zoneSurfaceKey = ZoneSurfaceKey(zone.GUID, zoneSurface.number);
                    if (zoneSurfaceKey != null)
                    {
                        result[zoneSurfaceKey] = zoneSurface;
                    }
                }
            }

            return result;
        }
    }
}
