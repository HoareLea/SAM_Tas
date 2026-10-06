// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// Every recirculation cooling branch of one route, over one period - PR5B (SAM#111). Complete only when
    /// every branch answered every hour and every branch behaved as it was built to; otherwise the
    /// <see cref="Refusals"/> say why and the route that asked for it refuses. Free of TAS COM types.
    /// </summary>
    public class RecirculationCoolingResults
    {
        private readonly List<RecirculationCoolingResult> results = new List<RecirculationCoolingResult>();
        private readonly List<string> refusals = new List<string>();
        private readonly List<string> notes = new List<string>();

        public RecirculationCoolingResults(int startHour, int endHour, string method)
        {
            StartHour = startHour;
            EndHour = endHour;
            Method = method;
        }

        public int StartHour { get; }

        public int EndHour { get; }

        /// <summary>How the evidence was obtained, in words.</summary>
        public string Method { get; }

        /// <summary>
        /// The largest difference [K] between the room temperatures of the evidence pass and those of the
        /// route's own simulation - the two runs are of the same document.
        /// </summary>
        public double MaximumZoneTemperatureDifference_K { get; internal set; } = double.NaN;

        /// <summary>One result per branch, ordered by air system guid.</summary>
        public List<RecirculationCoolingResult> Results
        {
            get
            {
                List<RecirculationCoolingResult> result = new List<RecirculationCoolingResult>(results);
                result.Sort((x, y) => x.Guid_AirSystem.CompareTo(y.Guid_AirSystem));
                return result;
            }
        }

        public List<string> Refusals
        {
            get { return new List<string>(refusals); }
        }

        public List<string> Notes
        {
            get { return new List<string>(notes); }
        }

        public bool IsComplete
        {
            get { return refusals.Count == 0 && results.Count != 0; }
        }

        public RecirculationCoolingResult Result(Guid guid_AirSystem)
        {
            foreach (RecirculationCoolingResult result in results)
            {
                if (result.Guid_AirSystem == guid_AirSystem)
                {
                    return result;
                }
            }

            return null;
        }

        internal void Add(RecirculationCoolingResult recirculationCoolingResult)
        {
            if (recirculationCoolingResult == null)
            {
                return;
            }

            results.Add(recirculationCoolingResult);
            refusals.AddRange(recirculationCoolingResult.Refusals);
        }

        internal void Refuse(string refusal)
        {
            if (!string.IsNullOrWhiteSpace(refusal))
            {
                refusals.Add(refusal);
            }
        }

        internal void Note(string note)
        {
            if (!string.IsNullOrWhiteSpace(note))
            {
                notes.Add(note);
            }
        }
    }
}
