// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace SAM.Analytical.Tas.TPD
{
    // Lightweight per-call timing recorder, mirrors the WorkflowCalculator instrumentation pattern.
    // Each Step("name") closes the previous interval and starts a new one. WriteCsv() emits a CSV
    // next to the TPD file with one row per step plus a TOTAL row. Used to find hotspots inside
    // Convert.ToTPD on large models; safe to leave threaded through code with a null instance.
    //
    // Measure("name") is the finer, nested counter: it accumulates elapsed time AND a call count per
    // name, independently of the steps, and is written as indented " > name" rows after them (never
    // added to TOTAL, because a measure always sits inside a step). Code too deep to be handed a
    // profiler reaches the active one through Current, which is per thread and null outside a profiled
    // call - so an un-profiled caller pays one null check.
    internal sealed class TPDProfiler
    {
        [ThreadStatic]
        private static TPDProfiler current;

        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly List<string> stepOrder = new List<string>();
        private readonly Dictionary<string, double> totalsByStep = new Dictionary<string, double>();
        private readonly List<string> measureOrder = new List<string>();
        private readonly Dictionary<string, double> totalsByMeasure = new Dictionary<string, double>();
        private readonly Dictionary<string, long> countsByMeasure = new Dictionary<string, long>();
        private string currentStepName;

        /// <summary>The profiler of the call in progress on this thread, or null.</summary>
        public static TPDProfiler Current => current;

        /// <summary>Makes this the thread's <see cref="Current"/> until the returned scope is disposed.</summary>
        public IDisposable Activate()
        {
            TPDProfiler previous = current;
            current = this;
            return new Scope(() => current = previous);
        }

        /// <summary>Times one occurrence of <paramref name="name"/>; null-safe through <c>?.</c>.</summary>
        public IDisposable Measure(string name)
        {
            if (name == null)
            {
                return null;
            }

            if (!totalsByMeasure.ContainsKey(name))
            {
                measureOrder.Add(name);
                totalsByMeasure[name] = 0.0;
                countsByMeasure[name] = 0;
            }

            long start = Stopwatch.GetTimestamp();
            return new Scope(() =>
            {
                totalsByMeasure[name] += (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                countsByMeasure[name]++;
            });
        }

        /// <summary>Adds <paramref name="count"/> to a measure's count without timing anything (e.g. native calls made).</summary>
        public void Count(string name, long count)
        {
            if (name == null)
            {
                return;
            }

            if (!totalsByMeasure.ContainsKey(name))
            {
                measureOrder.Add(name);
                totalsByMeasure[name] = 0.0;
                countsByMeasure[name] = 0;
            }

            countsByMeasure[name] += count;
        }

        // Accumulates elapsed time per step name. Calling Step("Liquid systems") inside a loop with
        // multiple plantrooms sums up each pass under the same row, which is what we want for the CSV.
        public void Step(string description)
        {
            FinalizeCurrentStep();
            currentStepName = description;
            if (description != null && !totalsByStep.ContainsKey(description))
            {
                stepOrder.Add(description);
                totalsByStep[description] = 0.0;
            }
            stopwatch.Restart();
        }

        public void FinalizeCurrentStep()
        {
            if (currentStepName == null)
            {
                return;
            }

            stopwatch.Stop();
            totalsByStep[currentStepName] = totalsByStep[currentStepName] + stopwatch.Elapsed.TotalMilliseconds;
            currentStepName = null;
        }

        /// <summary>The CSV rows, without writing them: steps, TOTAL, then the measures with their counts.</summary>
        public List<string> Lines()
        {
            FinalizeCurrentStep();

            List<string> lines = new List<string> { "step,milliseconds,count" };
            double total = 0.0;
            foreach (string name in stepOrder)
            {
                double value = totalsByStep.TryGetValue(name, out double v) ? v : 0.0;
                lines.Add(string.Format(CultureInfo.InvariantCulture, "\"{0}\",{1:F1},", Escape(name), value));
                total += value;
            }
            lines.Add(string.Format(CultureInfo.InvariantCulture, "\"TOTAL\",{0:F1},", total));

            foreach (string name in measureOrder)
            {
                lines.Add(string.Format(CultureInfo.InvariantCulture, "\" > {0}\",{1:F1},{2}", Escape(name), totalsByMeasure[name], countsByMeasure[name]));
            }

            return lines;
        }

        public void WriteCsv(string path_TPD)
        {
            WriteCsv(path_TPD, ".timing.csv");
        }

        /// <summary>Writes <see cref="Lines"/> beside <paramref name="path"/> as <c>&lt;name&gt;&lt;suffix&gt;</c>.</summary>
        public void WriteCsv(string path, string suffix)
        {
            List<string> lines = Lines();

            if (stepOrder.Count == 0 && measureOrder.Count == 0 || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }

                File.WriteAllLines(Path.Combine(directory, fileName + suffix), lines);
            }
            catch
            {
                // Best-effort instrumentation: never break the workflow if writing the CSV fails.
            }
        }

        private static string Escape(string name)
        {
            return name?.Replace("\"", "\"\"") ?? string.Empty;
        }

        private sealed class Scope : IDisposable
        {
            private Action action;

            public Scope(Action action)
            {
                this.action = action;
            }

            public void Dispose()
            {
                action?.Invoke();
                action = null;
            }
        }
    }
}
