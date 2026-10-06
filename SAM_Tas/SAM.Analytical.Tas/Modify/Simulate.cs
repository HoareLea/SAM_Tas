// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using SAM.Core.Tas;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool Simulate(string path_TBD, string path_TSD, int day_First, int day_Last)
        {
            return Simulate(path_TBD, path_TSD, day_First, day_Last, SizingType.Undefined, null);
        }

        public static bool Simulate(string path_TBD, string path_TSD, int day_First, int day_Last, SizingType sizingType, IEnumerable<SurfaceOutputSpec> surfaceOutputSpecs)
        {
            if (string.IsNullOrWhiteSpace(path_TBD) || string.IsNullOrWhiteSpace(path_TSD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new (path_TBD))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument?.TBDDocument;
                if (tBDDocument == null)
                    return false;

                bool dirty = false;

                if (sizingType != SizingType.Undefined)
                {
                    SetSizingTypes(tBDDocument.Building, sizingType);
                    dirty = true;
                }

                SurfaceOutputSpec firstSpec = null;
                if (surfaceOutputSpecs != null)
                {
                    foreach (SurfaceOutputSpec spec in surfaceOutputSpecs)
                    {
                        if (spec == null)
                            continue;
                        if (firstSpec == null)
                            firstSpec = spec;
                    }
                }

                if (firstSpec != null)
                {
                    Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs);
                    Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, firstSpec.Name);
                    dirty = true;
                }

                if (dirty)
                {
                    sAMTBDDocument.Save();
                }

                result = Simulate(tBDDocument, path_TSD, day_First, day_Last);

                if (result)
                {
                    sAMTBDDocument.Save();
                }
            }

            return result;
        }

        public static bool Simulate(SAMTBDDocument sAMTBDDocument, string path_TSD, int day_First, int day_Last)
        {
            return Simulate(sAMTBDDocument?.TBDDocument, path_TSD, day_First, day_Last);
        }

        public static bool Simulate(TBD.TBDDocument tBDDocument, string path_TSD, int day_First, int day_Last)
        {
            if (tBDDocument == null || string.IsNullOrWhiteSpace(path_TSD))
                return false;

            tBDDocument.simulate(day_First, day_Last, 0, 1, 0, 0, path_TSD, 1, 0);

            return Core.Query.WaitToUnlock(path_TSD);
        }

        /// <summary>
        /// Simulates a TBD to a <b>separate</b> TSD and reports what the run actually left behind.
        /// <para>
        /// The overloads above answer <c>Core.Query.WaitToUnlock(path_TSD)</c>, which returns true as soon as
        /// an <i>already existing</i> file is unlocked. A leftover TSD from an earlier run therefore made a
        /// failed simulation report success - and TAS reports a rejected model by writing
        /// <c>&lt;basename&gt;_error_log.txt</c> and returning normally from the COM call, which nothing read.
        /// </para>
        /// <para>
        /// Here <c>WaitToUnlock</c> is demoted to what it is - a wait - and success is decided by evidence:
        /// the expected TSD was deleted first, no error log is attributable to this run, and the file that
        /// now exists post-dates the run and is not the ~22-byte stub TAS writes when no weather data is
        /// installed.
        /// </para>
        /// </summary>
        /// <param name="simulationEvidence">What the run left behind. Never null.</param>
        public static bool Simulate(string path_TBD, string path_TSD, int day_First, int day_Last, out SimulationEvidence simulationEvidence)
        {
            simulationEvidence = new SimulationEvidence(SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            if (string.IsNullOrWhiteSpace(path_TBD) || string.IsNullOrWhiteSpace(path_TSD))
            {
                simulationEvidence.Refuse("Both a TBD and a TSD path are required.");
                return false;
            }

            if (!System.IO.File.Exists(path_TBD))
            {
                simulationEvidence.Refuse(string.Concat("The TBD does not exist: ", path_TBD));
                return false;
            }

            simulationEvidence.Prepare(System.DateTime.UtcNow);

            if (simulationEvidence.Refusals.Count != 0)
            {
                return false;
            }

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument?.TBDDocument;

                if (tBDDocument == null)
                {
                    simulationEvidence.RecordCallFailed(string.Concat("the document could not be opened: ", path_TBD));
                    return false;
                }

                if (tBDDocument.Building == null)
                {
                    simulationEvidence.RecordCallFailed(string.Concat("the document carries no building: ", path_TBD));
                    return false;
                }

                try
                {
                    tBDDocument.simulate(day_First, day_Last, 0, 1, 0, 0, path_TSD, 1, 0);

                    // A wait, not a verdict: the file may still be being written when the call returns.
                    Core.Query.WaitToUnlock(path_TSD);

                    simulationEvidence.RecordCallReturned();
                    sAMTBDDocument.Save();
                }
                catch (System.Exception exception)
                {
                    simulationEvidence.RecordCallFailed(
                        string.Format("{0}: {1}", exception.GetType().Name, exception.Message));
                    return false;
                }
            }

            simulationEvidence.Conclude();

            return simulationEvidence.Completed;
        }
    }
}