// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// One existing TBD zone, reduced to the two strings <c>Modify.ResolvePlantZoneReuse</c> needs to decide
    /// whether it is the plant zone a given air handling unit already owns.
    /// <para>
    /// A plain carrier rather than the <c>TBD.zone</c> itself, so the decision is pure SAM.Analytical and can
    /// be unit-tested with no TAS licence, install or COM server - the same reason
    /// <c>Modify.ResolveAirHandlingUnitMovements</c> takes and returns only SAM types. The caller holds the
    /// COM objects and uses <see cref="Index"/> to get back to the one the resolution picked.
    /// </para>
    /// </summary>
    public class PlantZoneCandidate
    {
        /// <param name="index">The zone's position in the caller's own list, returned untouched.</param>
        /// <param name="name">As read from <c>zone.name</c>.</param>
        /// <param name="description">As read from <c>zone.description</c>.</param>
        public PlantZoneCandidate(int index, string name, string description)
        {
            Index = index;
            Name = name;
            Description = description;
        }

        /// <summary>The zone's position in the caller's list. The resolution never interprets it.</summary>
        public int Index { get; }

        /// <summary><c>zone.name</c>. Presentation only - never the primary identity. See <see cref="PlantZoneIdentity"/>.</summary>
        public string Name { get; }

        /// <summary><c>zone.description</c>, which is where <see cref="PlantZoneIdentity"/> states the owning unit.</summary>
        public string Description { get; }
    }
}
