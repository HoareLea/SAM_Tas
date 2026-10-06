// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The identity of a space that survives a TAS round trip: the zone guid TAS stamps on it.
        /// <para>
        /// <b>This is the value <c>SimulationSpaceMap</c> matches on for TAS</b>, and it is a string rather than
        /// a <c>Guid</c> because that is how TAS writes it and how <c>Convert.ToSAM</c> stores it. Blank or
        /// absent means "this space carries no engine identity", which the map treats as a fall back to unique
        /// names rather than as a match - two spaces with no stamp must not resolve to each other.
        /// </para>
        /// <para>
        /// <b>Not provenance.</b> A model's <c>Source</c>, the file it was read from, and whether the results
        /// came through a TSD or a TPD are all provenance and none of them identifies a room. This is an identity
        /// the engine preserves per space, which is a different kind of thing.
        /// </para>
        /// </summary>
        public static string SimulationSpaceKey(Space space)
        {
            return space != null && space.TryGetValue(SpaceParameter.ZoneGuid, out string result) ? result : null;
        }
    }
}
