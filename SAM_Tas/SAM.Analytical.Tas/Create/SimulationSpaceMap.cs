// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        /// <summary>
        /// A <see cref="Analytical.SimulationSpaceMap"/> that ties a TAS-simulated model's spaces back to the
        /// design model's, <b>using the identity TAS itself preserves across the round trip</b>.
        /// <para>
        /// <b>Why this lives in <c>SAM_Tas</c>.</b> <c>SimulationSpaceMap</c> deliberately does not name the
        /// identity it matches on - it takes a <c>Func&lt;Space, string&gt;</c> - because which value survives a
        /// simulation is engine-specific and <c>SAM.Analytical</c> must not know about any engine. For TAS it is
        /// the zone guid stamped onto a space as <c>SpaceParameter.ZoneGuid</c>: the workflow writes it onto the
        /// design spaces, and <c>Convert.ToSAM</c> writes it again onto the fresh spaces it builds from a TSD.
        /// That makes it the one thing common to both sides of the round trip.
        /// </para>
        /// <para>
        /// <b>Verified on the real run, not assumed</b>: 9 spaces resolved to 9 distinct TAS zone guids,
        /// matching the DomOv XML - unique per SPACE, not per zone.
        /// </para>
        /// <para>
        /// <b>Where the stamp is absent the map falls back to unique names and refuses duplicates</b>, which is
        /// <c>SimulationSpaceMap</c>'s own behaviour and the reason a model that predates the stamp is not simply
        /// unusable. It is a weaker identity, not a silent one.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel_Design">The design model - the identities results must land on.</param>
        /// <param name="analyticalModel_Simulation">The model read back from the simulation.</param>
        public static Analytical.SimulationSpaceMap SimulationSpaceMap(this AnalyticalModel analyticalModel_Design, AnalyticalModel analyticalModel_Simulation)
        {
            return new Analytical.SimulationSpaceMap(analyticalModel_Design?.GetSpaces(), analyticalModel_Simulation?.GetSpaces(), Query.SimulationSpaceKey);
        }

        /// <summary>
        /// The same map over space lists a caller already holds.
        /// </summary>
        public static Analytical.SimulationSpaceMap SimulationSpaceMap(IEnumerable<Space> spaces_Design, IEnumerable<Space> spaces_Simulation)
        {
            return new Analytical.SimulationSpaceMap(spaces_Design, spaces_Simulation, Query.SimulationSpaceKey);
        }
    }
}
