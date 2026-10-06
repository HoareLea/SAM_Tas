// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.zone UpdateZone(this TBD.Building building, TBD.zone zone, Space space, ProfileLibrary profileLibrary, AdjacencyCluster adjacencyCluster = null)
        {
            return UpdateZone(building, zone, space, profileLibrary, null, adjacencyCluster);
        }

        // dayTypes_NonHDD threads a pre-computed list down to AddInternalCondition. See that method.
        public static TBD.zone UpdateZone(this TBD.Building building, TBD.zone zone, Space space, ProfileLibrary profileLibrary, IEnumerable<TBD.dayType> dayTypes_NonHDD, AdjacencyCluster adjacencyCluster = null)
        {
            if (space == null || profileLibrary == null || building == null || zone == null)
                return null;

            TBD.InternalCondition internalCondition_TBD = AddInternalCondition(building, space, profileLibrary, dayTypes_NonHDD, adjacencyCluster);
            if (internalCondition_TBD == null)
                return null;

            zone.AssignIC(internalCondition_TBD, true);

            if (!space.TryGetValue("Element Id", out string id))
                id = null;

            if (!space.TryGetValue(Analytical.SpaceParameter.LevelName, out string levelName))
                levelName = null;

            //The zone description is the SAM-only channel: [Id] and [LevelName] as before, and now the SAM
            //airflow REQUIREMENT the four authored bases state, which TAS has no field for. SAMZoneMetadata
            //owns the whole string - it rewrites what it manages, preserves anything it does not (a TAS user's
            //own note now survives an export, which the previous unconditional overwrite did not allow) and
            //appends its own section last. The metadata is built AFTER AddInternalCondition so it can
            //fingerprint the native TAS fields as that call actually left them.
            string description = SAMZoneMetadata.Compose(zone.description, id, levelName, Create.ZoneMetadata(space, internalCondition_TBD, profileLibrary));
            if (!string.IsNullOrEmpty(description))
                zone.description = description;

            return zone;
        }
    }
}
