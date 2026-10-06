// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Query
    {
        // Canonical TM59 apartment condition names carry bedroom-count metadata ("1/2/3 Bed
        // Apt.") ahead of the actual room function, e.g. "1 Bed Apt. Kitchen". The generic fuzzy
        // matcher (SAM.Analytical.TM59Manager.IsSleeping/IsLiving/IsCooking) sees "Bed" and misreads
        // the whole name as Sleeping before it ever reaches the function suffix - and even the
        // bare suffix "Living Room" alone still fuzzy-matches Sleeping, because "Room" is a
        // substring of the Sleeping keyword "bedroom". Neither quirk is specific to these three
        // documented function names, so - rather than re-running the fuzzy matcher on a stripped
        // string - map the known canonical function suffixes directly to the RoomUse they've
        // always meant. Any other InternalCondition name (including "Studio", "Single/Double
        // Bedroom", and anything not matching this exact "<n> Bed Apt. <function>" shape) is
        // untouched and continues through the existing fuzzy/fallback path below.
        private static readonly Regex apartmentBedCountPrefix = new Regex(@"^\s*\d+\s*Bed\s+Apt\.\s*(?<function>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static RoomUse RoomUse(this TM59Manager tM59Manager, Space space)
        {
            if(space == null || tM59Manager == null)
            {
                return Tas.RoomUse.Undefined;
            }

            RoomUse? apartmentRoomUse = ApartmentRoomUse(space?.InternalCondition?.Name);
            if (apartmentRoomUse.HasValue)
            {
                return apartmentRoomUse.Value;
            }

            List<TM59SpaceApplication> tM59SpaceApplications = tM59Manager.TM59SpaceApplications(space?.InternalCondition);
            if (tM59SpaceApplications == null || tM59SpaceApplications.Count == 0)
            {
                tM59SpaceApplications = tM59Manager.TM59SpaceApplications(space);
            }

            if (tM59SpaceApplications == null || tM59SpaceApplications.Count == 0)
            {
                return Tas.RoomUse.Other;
            }

            if (tM59SpaceApplications.Contains(TM59SpaceApplication.Sleeping))
            {
                return Tas.RoomUse.Bedroom;
            }

            if (tM59SpaceApplications.Contains(TM59SpaceApplication.Living) || tM59SpaceApplications.Contains(TM59SpaceApplication.Cooking))
            {
                return Tas.RoomUse.LivingRoomOrKitchen;
            }

            return Tas.RoomUse.Other;
        }

        /// <summary>
        /// Maps a canonical TM59 apartment condition name ("1/2/3 Bed Apt. Kitchen" / "... Living
        /// Room" / "... Living Room/Kitchen") directly to its RoomUse, bypassing the fuzzy
        /// Sleeping/Living/Cooking matcher entirely for this exact, unambiguous pattern. Returns
        /// null for any other name, so the caller falls through to the existing logic unchanged.
        /// </summary>
        private static RoomUse? ApartmentRoomUse(string internalConditionName)
        {
            if (string.IsNullOrWhiteSpace(internalConditionName))
            {
                return null;
            }

            Match match = apartmentBedCountPrefix.Match(internalConditionName);
            if (!match.Success)
            {
                return null;
            }

            string function = match.Groups["function"].Value?.Trim();
            if (string.IsNullOrWhiteSpace(function))
            {
                return null;
            }

            if (function.Equals("Kitchen", StringComparison.OrdinalIgnoreCase)
                || function.Equals("Living Room", StringComparison.OrdinalIgnoreCase)
                || function.Equals("Living Room/Kitchen", StringComparison.OrdinalIgnoreCase))
            {
                return Tas.RoomUse.LivingRoomOrKitchen;
            }

            return null;
        }
    }
}
