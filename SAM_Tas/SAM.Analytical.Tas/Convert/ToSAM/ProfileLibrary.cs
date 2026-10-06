// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        /// <summary>
        /// The legacy library build: one SAM <see cref="Profile"/> per TBD internal-condition profile slot, named
        /// <c>"{internal condition} [{profile}]"</c>. Kept for callers that hold no reuse index; the import itself
        /// uses the overload below.
        /// </summary>
        public static ProfileLibrary ToSAM_ProfileLibrary(this TBD.Building building)
        {
            List<Profile> profiles = building?.ToSAM_Profiles();
            if(profiles == null)
            {
                return null;
            }

            ProfileLibrary result = new ProfileLibrary(building.name);
            profiles.ForEach(x => result.Add(x));

            return result;
        }

        /// <summary>
        /// The library the import builds: the value-deduplicated shared definitions
        /// <paramref name="profileReuseIndex"/> resolved, under their canonical names.
        /// <para>
        /// The SAME index must have been threaded through every internal-condition conversion in the model - see
        /// <see cref="ProfileReuseIndex"/> - or a condition converted without it will reference a name this
        /// library does not carry.
        /// </para>
        /// <para>A null index falls back to <see cref="ToSAM_ProfileLibrary(TBD.Building)"/>, i.e. today's behaviour.</para>
        /// </summary>
        public static ProfileLibrary ToSAM_ProfileLibrary(this TBD.Building building, ProfileReuseIndex profileReuseIndex)
        {
            if (building == null)
            {
                return null;
            }

            if (profileReuseIndex == null || !profileReuseIndex.Resolved)
            {
                return ToSAM_ProfileLibrary(building);
            }

            List<Profile> profiles = profileReuseIndex.Profiles;
            if (profiles.Count == 0)
            {
                //Nothing to share, so answer exactly as the legacy build does rather than inventing an empty
                //library where it returns null - a building with no internal conditions at all had no
                //ProfileLibrary before this change and must not gain one because of it.
                return ToSAM_ProfileLibrary(building);
            }

            ProfileLibrary result = new ProfileLibrary(building.name);
            profiles.ForEach(x => result.Add(x));

            return result;
        }
    }
}
