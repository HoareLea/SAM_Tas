// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        /// <summary>
        /// Runs the temporary ResultantTemperature thermostat bridge for a complete Systems route.
        /// <para>The stages, each refusing before the next is reached:</para>
        /// <list type="number">
        /// <item><description><b>The plan</b> (<see cref="ThermostatBridgePlan"/>): the route's own no-IZAM
        /// source, every bound room exactly once by guid, and a complete finite annual achieved zone
        /// temperature for each.</description></item>
        /// <item><description><b>The paths</b>: the copy may not land on the source's TBD or TSD or on the
        /// route's TPD.</description></item>
        /// <item><description><b>The copy</b>: a stale copy is deleted first, the source is hashed, copied and
        /// the copy's hash must equal it. The source is never opened.</description></item>
        /// <item><description><b>The thermostats</b>: both limits of every internal condition of each room's
        /// zone receive the room's achieved series, read back slot by slot
        /// (<see cref="Modify.WriteThermostatBridge(TBD.Building, ThermostatBridgePlan, List{string})"/>).
        /// Nothing is simulated when any room could not be written.</description></item>
        /// <item><description><b>The second simulation</b>, days 1..365, evidenced by
        /// <see cref="SimulationEvidence"/>: its stale TSD deleted first, no error log attributable to it, a
        /// fresh non-trivial TSD afterwards.</description></item>
        /// <item><description><b>The results</b>: every room's resultant temperature by zone guid, and the
        /// check that the copy held the air at the imposed temperature
        /// (<see cref="Query.ReadThermostatBridge"/>).</description></item>
        /// <item><description><b>The source, again</b>: its hash must not have moved.</description></item>
        /// </list>
        /// </summary>
        /// <param name="systemVentilationRoute">A complete Systems route.</param>
        /// <param name="path_TBD">Where the copy is written; its TSD goes beside it.</param>
        /// <param name="achievedAirTemperatureTolerance">See <see cref="TPD.ThermostatBridge.DefaultAchievedAirTemperatureTolerance"/>.</param>
        public static ThermostatBridge ThermostatBridge(
            SystemVentilationRoute systemVentilationRoute,
            string path_TBD,
            double achievedAirTemperatureTolerance = TPD.ThermostatBridge.DefaultAchievedAirTemperatureTolerance)
        {
            //Where the bridge's time goes, beside the bridge TBD as <name>.timing.csv. Observation only.
            TPDProfiler profiler = new TPDProfiler();
            IDisposable activation = profiler.Activate();

            try
            {
                return ThermostatBridge(systemVentilationRoute, path_TBD, achievedAirTemperatureTolerance, profiler);
            }
            finally
            {
                activation.Dispose();
                profiler.WriteCsv(path_TBD, ".timing.csv");
            }
        }

        private static ThermostatBridge ThermostatBridge(
            SystemVentilationRoute systemVentilationRoute,
            string path_TBD,
            double achievedAirTemperatureTolerance,
            TPDProfiler profiler)
        {
            profiler.Step("Bridge: plan and guard");

            List<string> refusals = new List<string>();
            List<string> notes = new List<string>();

            //-------------------------------------------------------------------------------------------
            //1. The plan.
            //-------------------------------------------------------------------------------------------
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(systemVentilationRoute);

            ThermostatBridge result = new ThermostatBridge(thermostatBridgePlan, null)
            {
                AchievedAirTemperatureTolerance = achievedAirTemperatureTolerance,
                Path_TPD = systemVentilationRoute?.Path_TPD,
                Path_TBD_Source = thermostatBridgePlan.NoIzamThermalSource?.Path_TBD,
                Path_TSD_Source = thermostatBridgePlan.NoIzamThermalSource?.Path_TSD,
            };

            if (!thermostatBridgePlan.IsValid)
            {
                refusals.AddRange(thermostatBridgePlan.Refusals);
                return Refused(result, refusals, notes);
            }

            //-------------------------------------------------------------------------------------------
            //2. The paths.
            //-------------------------------------------------------------------------------------------
            string path_TSD = TryGuard_ThermostatBridge(result, path_TBD, achievedAirTemperatureTolerance, refusals);

            if (path_TSD == null)
            {
                return Refused(result, refusals, notes);
            }

            //-------------------------------------------------------------------------------------------
            //3. The copy.
            //-------------------------------------------------------------------------------------------
            profiler.Step("Bridge: hash source and copy TBD");

            result.Hash_TBD_Source_Before = Sha256(result.Path_TBD_Source);
            result.Hash_TSD_Source_Before = Sha256(result.Path_TSD_Source);

            if (result.Hash_TBD_Source_Before == null || result.Hash_TSD_Source_Before == null)
            {
                refusals.Add("The no-IZAM source's TBD or TSD could not be read, so the bridge has nothing to copy.");
                return Refused(result, refusals, notes);
            }

            try
            {
                //A copy left by an earlier run is deleted rather than overwritten in place: what exists after this
                //line can only be this run's copy of this source.
                if (File.Exists(path_TBD))
                {
                    File.Delete(path_TBD);
                }

                File.Copy(result.Path_TBD_Source, path_TBD);
            }
            catch (Exception exception)
            {
                refusals.Add(string.Format("The no-IZAM TBD could not be copied to '{0}': {1}", path_TBD, exception.Message));
                return Refused(result, refusals, notes);
            }

            result.Path_TBD = path_TBD;
            result.Path_TSD = path_TSD;
            result.Hash_TBD_Copied = Sha256(path_TBD);

            if (!string.Equals(result.Hash_TBD_Copied, result.Hash_TBD_Source_Before, StringComparison.Ordinal))
            {
                refusals.Add(string.Format("The copy '{0}' is not byte-identical to the no-IZAM source it was copied from.", path_TBD));
                return Refused(result, refusals, notes);
            }

            notes.Add(string.Format("No-IZAM source {0} (sha256 {1}) copied to {2}.", result.Path_TBD_Source, result.Hash_TBD_Source_Before, path_TBD));

            SimulationEvidence simulationEvidence = new SimulationEvidence(SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);
            result.SimulationEvidence = simulationEvidence;

            simulationEvidence.Prepare(DateTime.UtcNow);

            if (simulationEvidence.Refusals.Count != 0)
            {
                refusals.AddRange(simulationEvidence.Refusals);
                return Refused(result, refusals, notes);
            }

            //-------------------------------------------------------------------------------------------
            //4 and 5. The thermostats, then the second simulation - one document session.
            //-------------------------------------------------------------------------------------------
            List<ThermostatBridgeRoom> thermostatBridgeRooms = null;

            try
            {
                profiler.Step("Bridge: open TBD");

                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    profiler.Step("Bridge: write thermostats (write + read back)");

                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;
                    TBD.Building building = tBDDocument?.Building;

                    if (building == null)
                    {
                        simulationEvidence.RecordCallFailed(string.Concat("the copy could not be opened: ", path_TBD));
                    }
                    else if (building.GetIZAM(0) != null)
                    {
                        //The copy is byte-identical to the source, so this is the source carrying an IZAM - the
                        //no-IZAM contract is broken and the bridge would reconstruct a doubly ventilated building.
                        refusals.Add("The no-IZAM source carries an IZAM, so it is not the building the Systems route stood on.");
                    }
                    else
                    {
                        List<string> refusals_Write = new List<string>();

                        thermostatBridgeRooms = building.WriteThermostatBridge(thermostatBridgePlan, refusals_Write);

                        if (refusals_Write.Count != 0 || thermostatBridgeRooms.Count != thermostatBridgePlan.Count)
                        {
                            refusals.AddRange(refusals_Write);

                            if (refusals_Write.Count == 0)
                            {
                                refusals.Add("Not every planned room reached a thermostat, and the writer said nothing about why.");
                            }
                        }
                        else
                        {
                            notes.Add(string.Format(
                                "Heating and cooling thermostats written and read back for {0} room(s), {1} hour(s) each.",
                                thermostatBridgeRooms.Count,
                                ThermostatBridgePlan.HoursPerYear));

                            profiler.Step("Bridge: save TBD (before simulation)");

                            sAMTBDDocument.Save();

                            profiler.Step("Bridge: TAS building simulation (full year)");

                            tBDDocument.simulate(ThermostatBridgePlan.FirstDay, ThermostatBridgePlan.LastDay, 0, 1, 0, 0, path_TSD, 1, 0);

                            profiler.Step("Bridge: wait for TSD unlock");

                            //A wait, not a verdict: the evidence below decides.
                            Core.Query.WaitToUnlock(path_TSD);

                            simulationEvidence.RecordCallReturned();

                            profiler.Step("Bridge: save TBD (after simulation)");

                            sAMTBDDocument.Save();

                            profiler.Step("Bridge: close TBD");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                simulationEvidence.RecordCallFailed(string.Format("{0}: {1}", exception.GetType().Name, exception.Message));
            }

            result = WithRooms(result, thermostatBridgeRooms);

            if (refusals.Count != 0 || !simulationEvidence.CallReturned)
            {
                refusals.AddRange(simulationEvidence.Refusals);
                return Refused(result, refusals, notes);
            }

            simulationEvidence.Conclude();

            notes.AddRange(simulationEvidence.Notes);

            if (!simulationEvidence.Completed)
            {
                refusals.AddRange(simulationEvidence.Refusals);
                return Refused(result, refusals, notes);
            }

            //-------------------------------------------------------------------------------------------
            //6. The results.
            //-------------------------------------------------------------------------------------------
            profiler.Step("Bridge: read resultant temperature from TSD");

            List<ResultantTemperatureResult> resultantTemperatureResults = null;

            try
            {
                using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
                {
                    TSD.SimulationData simulationData = sAMTSDDocument.TSDDocument?.SimulationData;

                    if (simulationData?.GetBuildingData() == null)
                    {
                        refusals.Add(string.Concat("The second TSD could not be read: ", path_TSD));
                    }
                    else
                    {
                        //The SimulationData overload: it refuses a TSD that does not hold days 1..365 before
                        //reading anything - which no length check on the series can detect.
                        resultantTemperatureResults = simulationData.ReadThermostatBridge(thermostatBridgePlan, thermostatBridgeRooms, achievedAirTemperatureTolerance, refusals);
                    }
                }
            }
            catch (Exception exception)
            {
                refusals.Add(string.Format("Reading the second TSD threw {0}: {1}", exception.GetType().Name, exception.Message));
            }

            //-------------------------------------------------------------------------------------------
            //7. The source, again, and the lineage.
            //-------------------------------------------------------------------------------------------
            profiler.Step("Bridge: lineage hashes");

            result.Hash_TBD_Source_After = Sha256(result.Path_TBD_Source);
            result.Hash_TSD_Source_After = Sha256(result.Path_TSD_Source);
            result.Hash_TBD = Sha256(path_TBD);
            result.Hash_TSD = Sha256(path_TSD);

            if (!string.Equals(result.Hash_TBD_Source_Before, result.Hash_TBD_Source_After, StringComparison.Ordinal)
                || !string.Equals(result.Hash_TSD_Source_Before, result.Hash_TSD_Source_After, StringComparison.Ordinal))
            {
                refusals.Add("The no-IZAM source changed while the bridge ran, so the bridge's lineage back to it is broken.");
            }

            foreach (ThermostatBridgeRoom thermostatBridgeRoom in result.Rooms)
            {
                notes.Add(thermostatBridgeRoom.ToString());
            }

            notes.Add(string.Format(
                "Lineage: route TPD {0}; source TBD {1} sha256 {2} (after {3}); bridge TBD {4} sha256 {5}; bridge TSD {6} sha256 {7}.",
                result.Path_TPD,
                result.Path_TBD_Source,
                result.Hash_TBD_Source_Before,
                result.Hash_TBD_Source_After,
                path_TBD,
                result.Hash_TBD,
                path_TSD,
                result.Hash_TSD));

            if (refusals.Count == 0)
            {
                simulationEvidence.RecordResultsReconciled(thermostatBridgePlan.Count, ThermostatBridgePlan.StartHour, ThermostatBridgePlan.EndHour);
            }
            else
            {
                simulationEvidence.RecordResultsNotReconciled(string.Format("{0} refusal(s) on the bridge's results.", refusals.Count));
            }

            result.ResultantTemperatureResults = new ResultantTemperatureResults(
                TPD.ThermostatBridge.Method,
                ThermostatBridgePlan.StartHour,
                ThermostatBridgePlan.EndHour,
                Guids_Space(thermostatBridgePlan),
                resultantTemperatureResults,
                path_TSD,
                simulationEvidence,
                refusals,
                notes);

            return result;
        }

        /// <summary>
        /// Everything about the paths that must hold before anything is copied. Returns the bridge TSD path, or
        /// null having refused.
        /// </summary>
        private static string TryGuard_ThermostatBridge(ThermostatBridge thermostatBridge, string path_TBD, double achievedAirTemperatureTolerance, List<string> refusals)
        {
            if (double.IsNaN(achievedAirTemperatureTolerance) || double.IsInfinity(achievedAirTemperatureTolerance) || achievedAirTemperatureTolerance < 0)
            {
                refusals.Add(string.Format("The achieved-air tolerance {0} is not a temperature difference.", achievedAirTemperatureTolerance));
                return null;
            }

            if (string.IsNullOrWhiteSpace(path_TBD))
            {
                refusals.Add("No path was given for the bridge's copy of the no-IZAM TBD.");
                return null;
            }

            string path_TSD;

            try
            {
                if (!string.Equals(Path.GetExtension(path_TBD), ".tbd", StringComparison.OrdinalIgnoreCase))
                {
                    refusals.Add(string.Format("The bridge's copy '{0}' is not a .tbd path.", path_TBD));
                    return null;
                }

                path_TSD = Path.ChangeExtension(path_TBD, ".tsd");
            }
            catch (Exception exception)
            {
                refusals.Add(string.Format("'{0}' is not a usable path for the bridge's copy: {1}", path_TBD, exception.Message));
                return null;
            }

            //The copy deletes whatever is at its path, and the simulation deletes whatever is at its TSD. Either
            //landing on the source or the Systems document would destroy the input the bridge stands on.
            foreach (string path in new string[] { thermostatBridge.Path_TBD_Source, thermostatBridge.Path_TSD_Source, thermostatBridge.Path_TPD })
            {
                if (SamePath(path_TBD, path) || SamePath(path_TSD, path))
                {
                    refusals.Add(string.Format(
                        "The bridge's copy '{0}' or its TSD '{1}' would be written over '{2}', an input the bridge stands on.",
                        path_TBD,
                        path_TSD,
                        path));

                    return null;
                }
            }

            string directory = Path.GetDirectoryName(path_TBD);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                refusals.Add(string.Concat("The directory the bridge's copy would be written to does not exist: ", directory));
                return null;
            }

            return path_TSD;
        }

        private static ThermostatBridge Refused(ThermostatBridge thermostatBridge, List<string> refusals, List<string> notes)
        {
            if (refusals.Count == 0)
            {
                refusals.Add("The thermostat bridge stopped and no stage said why. This is itself a defect.");
            }

            thermostatBridge.ResultantTemperatureResults = new ResultantTemperatureResults(
                TPD.ThermostatBridge.Method,
                ThermostatBridgePlan.StartHour,
                ThermostatBridgePlan.EndHour,
                Guids_Space(thermostatBridge.ThermostatBridgePlan),
                null,
                null,
                thermostatBridge.SimulationEvidence,
                refusals,
                notes);

            return thermostatBridge;
        }

        private static ThermostatBridge WithRooms(ThermostatBridge thermostatBridge, IEnumerable<ThermostatBridgeRoom> thermostatBridgeRooms)
        {
            return new ThermostatBridge(thermostatBridge.ThermostatBridgePlan, thermostatBridgeRooms)
            {
                AchievedAirTemperatureTolerance = thermostatBridge.AchievedAirTemperatureTolerance,
                Path_TPD = thermostatBridge.Path_TPD,
                Path_TBD_Source = thermostatBridge.Path_TBD_Source,
                Path_TSD_Source = thermostatBridge.Path_TSD_Source,
                Hash_TBD_Source_Before = thermostatBridge.Hash_TBD_Source_Before,
                Hash_TSD_Source_Before = thermostatBridge.Hash_TSD_Source_Before,
                Path_TBD = thermostatBridge.Path_TBD,
                Path_TSD = thermostatBridge.Path_TSD,
                Hash_TBD_Copied = thermostatBridge.Hash_TBD_Copied,
                SimulationEvidence = thermostatBridge.SimulationEvidence,
            };
        }

        private static List<Guid> Guids_Space(ThermostatBridgePlan thermostatBridgePlan)
        {
            List<Guid> result = new List<Guid>();

            if (thermostatBridgePlan?.SystemVentilationRoute == null)
            {
                return result;
            }

            //The rooms the ROUTE bound, not only the ones the plan accepted: a refused plan must still report
            //against every room that needed an answer.
            foreach (SystemVentilationBinding systemVentilationBinding in thermostatBridgePlan.SystemVentilationRoute.Bindings)
            {
                result.Add(systemVentilationBinding.Guid_Space);
            }

            return result;
        }

        private static string Sha256(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                using (SHA256 sHA256 = SHA256.Create())
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return BitConverter.ToString(sHA256.ComputeHash(fileStream)).Replace("-", string.Empty);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
