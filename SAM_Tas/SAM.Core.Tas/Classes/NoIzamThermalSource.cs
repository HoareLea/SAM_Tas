// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Core.Tas
{
    /// <summary>
    /// The thermal source a TAS Systems ventilation route runs on: a simulated building that carries
    /// <b>no</b> mechanical ventilation of its own, and the identity map that ties its zones back to the
    /// analytical rooms.
    /// <para>
    /// <b>Why "no IZAM" is the whole point.</b> On the Part O Iteration 3 route the ventilation is
    /// modelled explicitly in TAS Systems. If the building model <i>also</i> carried its mechanical
    /// ventilation - as IZAMs and as the <c>ticV</c> profile on every internal condition - the same air
    /// would be delivered twice and every zone temperature would be wrong in a way no result check could
    /// see. So the source is produced with <c>RemoveIZAMs</c> and
    /// <c>RemoveMechanicalVentilationGains</c>, and this record states that they were applied rather
    /// than leaving a later reader to assume it.
    /// </para>
    /// <para>
    /// <b>Both paths are stated, and the zone map with them.</b> The TSD path is derived from the TBD's
    /// basename inside the workflow, and a consumer that had to re-derive it would be guessing at a
    /// convention it does not own. The zone map - analytical <c>Space.Guid</c> to the TAS zone guid the
    /// workflow stamped as <c>SpaceParameter.ZoneGuid</c> - is what lets the Systems conversion bind
    /// each room to its own zone load by identity, with no name anywhere on the path.
    /// </para>
    /// <para>Free of TAS COM types.</para>
    /// </summary>
    public class NoIzamThermalSource
    {
        private readonly Dictionary<Guid, string> zoneReferences = new Dictionary<Guid, string>();
        private readonly List<string> refusals = new List<string>();
        private readonly List<string> notes = new List<string>();

        public NoIzamThermalSource(
            string path_TBD,
            string path_TSD,
            bool removedIZAMs,
            bool removedMechanicalVentilationGains,
            SimulationEvidence simulationEvidence,
            IEnumerable<KeyValuePair<Guid, string>> zoneReferences,
            IEnumerable<string> refusals,
            IEnumerable<string> notes)
        {
            Path_TBD = path_TBD;
            Path_TSD = path_TSD;
            RemovedIZAMs = removedIZAMs;
            RemovedMechanicalVentilationGains = removedMechanicalVentilationGains;
            SimulationEvidence = simulationEvidence;

            if (zoneReferences != null)
            {
                foreach (KeyValuePair<Guid, string> keyValuePair in zoneReferences)
                {
                    if (keyValuePair.Key == Guid.Empty || string.IsNullOrWhiteSpace(keyValuePair.Value))
                    {
                        continue;
                    }

                    this.zoneReferences[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            if (refusals != null)
            {
                this.refusals.AddRange(refusals);
            }

            if (notes != null)
            {
                this.notes.AddRange(notes);
            }
        }

        /// <summary>The simulated building. Stated, never derived by a consumer.</summary>
        public string Path_TBD { get; }

        /// <summary>The results the building produced. Stated, never derived from the TBD's basename.</summary>
        public string Path_TSD { get; }

        /// <summary>Whether the IZAM sweep was applied.</summary>
        public bool RemovedIZAMs { get; }

        /// <summary>Whether the mechanical ventilation gain (<c>ticV</c>) was zeroed.</summary>
        public bool RemovedMechanicalVentilationGains { get; }

        /// <summary>What the building simulation left behind. Null where none was run.</summary>
        public SimulationEvidence SimulationEvidence { get; }

        /// <summary>
        /// Analytical <c>Space.Guid</c> to TAS zone guid, from <c>SpaceParameter.ZoneGuid</c>. Measured
        /// on licensed TAS to be the same identifier a TSD zone load answers, which is what makes the
        /// Systems conversion name-free.
        /// </summary>
        public Dictionary<Guid, string> ZoneReferences
        {
            get { return new Dictionary<Guid, string>(zoneReferences); }
        }

        /// <summary>The TAS zone guid for one analytical room, or null.</summary>
        public string ZoneReference(Guid guid_Space)
        {
            return zoneReferences.TryGetValue(guid_Space, out string result) ? result : null;
        }

        /// <summary>How many rooms the source states a zone identity for.</summary>
        public int Count_ZoneReferences
        {
            get { return zoneReferences.Count; }
        }

        /// <summary>Every reason this source is not usable.</summary>
        public List<string> Refusals
        {
            get { return new List<string>(refusals); }
        }

        /// <summary>What was produced.</summary>
        public List<string> Notes
        {
            get { return new List<string>(notes); }
        }

        /// <summary>
        /// Whether the source is a thermal model a ventilation route may be run against: both cleanups
        /// applied, both files present, an evidenced simulation, and at least one room whose zone
        /// identity is stated.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return refusals.Count == 0
                    && RemovedIZAMs
                    && RemovedMechanicalVentilationGains
                    && !string.IsNullOrWhiteSpace(Path_TBD)
                    && !string.IsNullOrWhiteSpace(Path_TSD)
                    && System.IO.File.Exists(Path_TBD)
                    && System.IO.File.Exists(Path_TSD)
                    && SimulationEvidence != null
                    && SimulationEvidence.Completed
                    && zoneReferences.Count != 0;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "No-IZAM thermal source: {0} -> {1}, {2} room identity(ies){3}",
                Path_TBD ?? "<no TBD>",
                Path_TSD ?? "<no TSD>",
                zoneReferences.Count,
                IsComplete ? string.Empty : string.Concat(", INCOMPLETE (", refusals.Count, " refusal(s))"));
        }
    }
}
