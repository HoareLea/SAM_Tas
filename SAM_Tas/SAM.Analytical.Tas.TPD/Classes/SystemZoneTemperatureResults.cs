// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// Every room's <c>ZoneTemperature</c> series for one simulated period, together with the check that
    /// decides whether the route ran at all.
    /// <para>
    /// <b>This is the gate.</b> A measured <c>"Done"</c> from TAS is positive evidence and nothing more:
    /// the run that produced it could still have written no result for a room, or the right number of
    /// results against the wrong zones. What settles it is that every room the source graph names has a
    /// complete, finite series, read off the exact native zone and zone load its own binding states -
    /// and that no series came back for a room nobody asked about.
    /// </para>
    /// <para>
    /// <b>Only <c>ZoneTemperature</c> is requested.</b> TAS exposes eleven space result series; fetching
    /// all of them per room would multiply the COM work by eleven for ten thousand values nobody reads.
    /// The one series is requested by its own enum, and the store is keyed by
    /// <c>SpaceDataType.ZoneTemperature.ToString()</c> - not by the enum indexer, which resolves through
    /// <c>Description()</c> ("Zone Temperature") and would answer nothing at all for a store written
    /// under <c>ToString()</c> ("ZoneTemperature").
    /// </para>
    /// <para>Free of TAS COM types, so the whole gate is testable without a licence.</para>
    /// </summary>
    public class SystemZoneTemperatureResults
    {
        private readonly Dictionary<Guid, SystemZoneTemperatureResult> results = new Dictionary<Guid, SystemZoneTemperatureResult>();
        private readonly List<string> refusals = new List<string>();
        private readonly List<string> notes = new List<string>();

        private bool validated;

        public SystemZoneTemperatureResults(int startHour, int endHour)
        {
            StartHour = startHour;
            EndHour = endHour;
        }

        /// <summary>First hour of the requested period, 0-based.</summary>
        public int StartHour { get; }

        /// <summary>Last hour of the requested period, 0-based and inclusive.</summary>
        public int EndHour { get; }

        /// <summary>How many hourly values each room must have.</summary>
        public int ExpectedCount
        {
            get { return EndHour - StartHour + 1; }
        }

        /// <summary>Every series, ordered by <c>Space.Guid</c> - never by the order they were read.</summary>
        public List<SystemZoneTemperatureResult> Results
        {
            get
            {
                List<SystemZoneTemperatureResult> result = new List<SystemZoneTemperatureResult>(results.Values);
                result.Sort((x, y) => x.Guid_Space.CompareTo(y.Guid_Space));
                return result;
            }
        }

        /// <summary>Every reason the result set is not usable.</summary>
        public List<string> Refusals
        {
            get { return new List<string>(refusals); }
        }

        /// <summary>What was checked.</summary>
        public List<string> Notes
        {
            get { return new List<string>(notes); }
        }

        /// <summary>
        /// True only once <see cref="Validate"/> has run and every check passed. False until then: an
        /// unvalidated result set is never a usable one.
        /// </summary>
        public bool IsComplete
        {
            get { return validated && refusals.Count == 0; }
        }

        /// <summary>One room's series, by analytical room guid. Null when there is none.</summary>
        public SystemZoneTemperatureResult Result(Guid guid_Space)
        {
            return results.TryGetValue(guid_Space, out SystemZoneTemperatureResult result) ? result : null;
        }

        /// <summary>Records a refusal.</summary>
        public void Refuse(string refusal)
        {
            if (!string.IsNullOrWhiteSpace(refusal))
            {
                refusals.Add(refusal);
                validated = false;
            }
        }

        /// <summary>Records a check that passed.</summary>
        public void Note(string note)
        {
            if (!string.IsNullOrWhiteSpace(note))
            {
                notes.Add(note);
            }
        }

        /// <summary>
        /// Adds one room's series. A second series for one room is a refusal: two native zones claiming
        /// the same room is exactly the aliasing this route exists to make impossible.
        /// </summary>
        public bool Add(SystemZoneTemperatureResult systemZoneTemperatureResult)
        {
            if (systemZoneTemperatureResult == null)
            {
                return false;
            }

            if (results.ContainsKey(systemZoneTemperatureResult.Guid_Space))
            {
                Refuse(string.Format(
                    "Room {0} produced two zone temperature series.",
                    systemZoneTemperatureResult.Guid_Space));

                return false;
            }

            results[systemZoneTemperatureResult.Guid_Space] = systemZoneTemperatureResult;
            return true;
        }

        /// <summary>
        /// Checks the result set against the room bindings the conversion produced, and refuses on every
        /// disagreement.
        /// <para>What is checked:</para>
        /// <list type="number">
        /// <item><description>every expected room has a series;</description></item>
        /// <item><description>each series was read off the exact native zone and zone load its room's
        /// binding names;</description></item>
        /// <item><description>each series has one finite value for every hour of the requested period,
        /// with no missing index;</description></item>
        /// <item><description>no two rooms share a native zone or a native zone load;</description></item>
        /// <item><description>no series belongs to a room nobody asked about.</description></item>
        /// </list>
        /// </summary>
        public bool Validate(IEnumerable<SystemVentilationBinding> systemVentilationBindings)
        {
            validated = false;

            if (systemVentilationBindings == null)
            {
                Refuse("No room bindings were supplied, so there is nothing to check the results against.");
                return false;
            }

            Dictionary<Guid, SystemVentilationBinding> dictionary = new Dictionary<Guid, SystemVentilationBinding>();

            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationBindings)
            {
                if (systemVentilationBinding == null)
                {
                    continue;
                }

                if (dictionary.ContainsKey(systemVentilationBinding.Guid_Space))
                {
                    Refuse(string.Format(
                        "Room {0} is named by two bindings, so which native zone owns it is not stated.",
                        systemVentilationBinding.Guid_Space));

                    continue;
                }

                dictionary[systemVentilationBinding.Guid_Space] = systemVentilationBinding;
            }

            if (dictionary.Count == 0)
            {
                Refuse("The conversion bound no room, so no zone temperature could belong to one.");
                return false;
            }

            if (ExpectedCount <= 0)
            {
                Refuse(string.Format("The requested period {0}..{1} contains no hours.", StartHour, EndHour));
                return false;
            }

            Dictionary<string, Guid> space_By_Zone = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Guid> space_By_Load = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            List<Guid> guids = new List<Guid>(dictionary.Keys);
            guids.Sort();

            int count_Complete = 0;

            foreach (Guid guid_Space in guids)
            {
                SystemVentilationBinding systemVentilationBinding = dictionary[guid_Space];

                if (!results.TryGetValue(guid_Space, out SystemZoneTemperatureResult systemZoneTemperatureResult) || systemZoneTemperatureResult == null)
                {
                    Refuse(string.Format(
                        "Room {0} is in the converted graph and no zone temperature series came back for it.",
                        guid_Space));

                    continue;
                }

                if (!string.Equals(systemZoneTemperatureResult.Reference_SystemZone, systemVentilationBinding.Reference_SystemZone, StringComparison.OrdinalIgnoreCase))
                {
                    Refuse(string.Format(
                        "Room {0}: its series was read off native zone {1} and its binding names zone {2}.",
                        guid_Space,
                        systemZoneTemperatureResult.Reference_SystemZone ?? "<none>",
                        systemVentilationBinding.Reference_SystemZone ?? "<none>"));
                }

                if (!string.Equals(systemZoneTemperatureResult.Reference_ZoneLoad, systemVentilationBinding.Reference_ZoneLoad, StringComparison.OrdinalIgnoreCase))
                {
                    Refuse(string.Format(
                        "Room {0}: its series came from zone load {1} and its binding names load {2}.",
                        guid_Space,
                        systemZoneTemperatureResult.Reference_ZoneLoad ?? "<none>",
                        systemVentilationBinding.Reference_ZoneLoad ?? "<none>"));
                }

                if (systemZoneTemperatureResult.StartHour != StartHour || systemZoneTemperatureResult.EndHour != EndHour)
                {
                    Refuse(string.Format(
                        "Room {0}: its series covers hours {1}..{2} and the run requested {3}..{4}.",
                        guid_Space,
                        systemZoneTemperatureResult.StartHour,
                        systemZoneTemperatureResult.EndHour,
                        StartHour,
                        EndHour));
                }

                string refusal = systemZoneTemperatureResult.Refusal();
                if (refusal != null)
                {
                    Refuse(refusal);
                }
                else
                {
                    count_Complete++;
                }

                if (!string.IsNullOrWhiteSpace(systemZoneTemperatureResult.Reference_SystemZone))
                {
                    if (space_By_Zone.TryGetValue(systemZoneTemperatureResult.Reference_SystemZone, out Guid guid_Other))
                    {
                        Refuse(string.Format(
                            "Rooms {0} and {1} both took their series from native zone {2}.",
                            guid_Other,
                            guid_Space,
                            systemZoneTemperatureResult.Reference_SystemZone));
                    }
                    else
                    {
                        space_By_Zone[systemZoneTemperatureResult.Reference_SystemZone] = guid_Space;
                    }
                }

                if (!string.IsNullOrWhiteSpace(systemZoneTemperatureResult.Reference_ZoneLoad))
                {
                    if (space_By_Load.TryGetValue(systemZoneTemperatureResult.Reference_ZoneLoad, out Guid guid_Other))
                    {
                        Refuse(string.Format(
                            "Rooms {0} and {1} both took their series from zone load {2}.",
                            guid_Other,
                            guid_Space,
                            systemZoneTemperatureResult.Reference_ZoneLoad));
                    }
                    else
                    {
                        space_By_Load[systemZoneTemperatureResult.Reference_ZoneLoad] = guid_Space;
                    }
                }
            }

            foreach (SystemZoneTemperatureResult systemZoneTemperatureResult in Results)
            {
                if (!dictionary.ContainsKey(systemZoneTemperatureResult.Guid_Space))
                {
                    Refuse(string.Format(
                        "A zone temperature series came back for room {0}, which the converted graph does not contain.",
                        systemZoneTemperatureResult.Guid_Space));
                }
            }

            validated = refusals.Count == 0;

            if (validated)
            {
                Note(string.Format(
                    "Zone temperature complete for {0} of {0} room(s), {1} finite value(s) each over hours {2}..{3}, "
                    + "each read off the native zone and zone load its own binding names.",
                    count_Complete,
                    ExpectedCount,
                    StartHour,
                    EndHour));
            }

            return validated;
        }
    }
}
