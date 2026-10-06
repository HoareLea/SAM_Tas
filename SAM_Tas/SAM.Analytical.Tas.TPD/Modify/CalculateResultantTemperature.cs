// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Core.Tas;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// The authoritative <b>TPD-full</b> route: simulate the actual system, carry the first pass's result
        /// into a <b>copy</b> of the TBD, simulate that copy, and leave a TSD that carries the
        /// <c>ResultantTemperature</c> series TM59 needs.
        /// <para>
        /// <b>The second simulation is deliberate. It is not duplicated work and must not be removed.</b> A TPD
        /// simulation models the real system but produces no resultant temperature; only a TBD run does. See
        /// <see cref="ResultantTemperaturePreparation"/> for the boundary this route draws and the transfer it
        /// can and cannot perform.
        /// </para>
        /// <para>
        /// Behaviour is unchanged from before the boundary was named: the default transfer is the one this method
        /// has always performed.
        /// </para>
        /// </summary>
        public static bool CalculateResultantTemperature(string path_TPD, out string path_TBD, out string path_TSD)
        {
            List<string> refusals;

            return CalculateResultantTemperature(path_TPD, ResultantTemperatureTransfer.ZoneTemperatureToThermostatLimits, out path_TBD, out path_TSD, out refusals);
        }

        /// <summary>
        /// The same route, stating which quantity crosses between the two passes and reporting why it refused.
        /// <para>
        /// <b>A refusal is final.</b> Where this returns false the caller must report
        /// <paramref name="refusals"/> and stop. It must not fall back to
        /// <see cref="ApproximateResultantTemperatureMap"/>, which synthesises a resultant temperature from a
        /// single pass: substituting that for a failed two-pass run would report an approximation as a simulated
        /// result. The two routes answer to different evidence and stay separately callable for that reason.
        /// </para>
        /// </summary>
        /// <param name="path_TPD">An already-simulated TPD, with its companion TBD beside it.</param>
        /// <param name="resultantTemperatureTransfer">Which first-pass quantity to carry into the TBD copy.</param>
        /// <param name="path_TBD">The copy that was modified and simulated - never the design TBD.</param>
        /// <param name="path_TSD">The second pass's TSD, the only source of <c>ResultantTemperature</c>.</param>
        /// <param name="refusals">Why the route stopped. Empty on success.</param>
        public static bool CalculateResultantTemperature(string path_TPD, ResultantTemperatureTransfer resultantTemperatureTransfer, out string path_TBD, out string path_TSD, out List<string> refusals)
        {
            path_TBD = null;
            path_TSD = null;

            ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(path_TPD, resultantTemperatureTransfer);

            refusals = resultantTemperaturePreparation.Refusals;

            //Refused before anything is read or written - notably before the design TBD is copied.
            if (!resultantTemperaturePreparation.IsSupported)
            {
                return false;
            }

            if (!System.IO.File.Exists(resultantTemperaturePreparation.Path_TPD))
            {
                refusals.Add(string.Format("The TPD '{0}' does not exist.", resultantTemperaturePreparation.Path_TPD));
                return false;
            }

            //Checked here as well as in TryBeginSecondPass, so a missing companion TBD refuses BEFORE the full COM
            //conversion below rather than after it. TryBeginSecondPass keeps its own check because it is the
            //guarantee that nothing is copied without one, and must hold for any caller.
            if (!System.IO.File.Exists(resultantTemperaturePreparation.Path_TBD_Design))
            {
                refusals.Add(string.Format("The TPD-full route needs the companion TBD '{0}' beside the TPD, and it does not exist.", resultantTemperaturePreparation.Path_TBD_Design));
                return false;
            }

            SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings()
            {
                //The TPD is read, never re-run: the first pass has already happened and its results are what
                //this route exists to carry forward.
                Simulate = false,
                IncludeComponentResults = true
            };

            SystemEnergyCentre systemEnergyCentre = Convert.ToSAM(resultantTemperaturePreparation.Path_TPD, systemEnergyCentreConversionSettings);
            if (systemEnergyCentre is null)
            {
                refusals.Add(string.Format("The TPD '{0}' could not be read.", resultantTemperaturePreparation.Path_TPD));
                return false;
            }

            List<SystemPlantRoom> systemPlantRooms = systemEnergyCentre.GetSystemPlantRooms();
            if (systemPlantRooms is null)
            {
                refusals.Add("The TPD carries no plant rooms, so it holds no first-pass results to transfer.");
                return false;
            }

            List<SystemSpaceResult> systemSpaceResults = new List<SystemSpaceResult>();
            foreach (SystemPlantRoom systemPlantRoom in systemPlantRooms)
            {
                List<SystemSpaceResult> systemSpaceResults_PlantRoom = systemPlantRoom.GetSystemResults<SystemSpaceResult>();
                if (systemSpaceResults_PlantRoom is null)
                {
                    continue;
                }

                systemSpaceResults.AddRange(systemSpaceResults_PlantRoom);
            }

            //Checks the companion TBD, takes the first pass's payload, and copies the design TBD - refusing
            //without writing anything if any of that fails. Only the copy is ever opened for writing below.
            Dictionary<string, IndexedDoubles> dictionary;
            List<string> refusals_SecondPass;
            if (!resultantTemperaturePreparation.TryBeginSecondPass(systemSpaceResults, out dictionary, out refusals_SecondPass))
            {
                refusals = refusals_SecondPass;
                return false;
            }

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(resultantTemperaturePreparation.Path_TBD_Simulation))
            {
                TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                Building building = tBDDocument.Building;

                int index_Zone = 0;

                while (building.GetZone(index_Zone) is zone zone)
                {
                    index_Zone++;

                    IndexedDoubles indexedDoubles;
                    if (!dictionary.TryGetValue(zone.name, out indexedDoubles) || indexedDoubles == null)
                    {
                        continue;
                    }

                    int index_IC = 0;
                    while (zone.GetIC(index_IC) is TBD.InternalCondition internalCondition)
                    {
                        index_IC++;

                        Thermostat thermostat = internalCondition.GetThermostat();
                        if (thermostat is null)
                        {
                            continue;
                        }

                        //Both limits, so the zone is held AT the first pass's temperature rather than merely
                        //bounded by it.
                        profile[] profiles = new profile[] { thermostat.GetProfile((int)Profiles.ticUL), thermostat.GetProfile((int)Profiles.ticLL) };

                        foreach (profile profile in profiles)
                        {
                            if (profile is null)
                            {
                                continue;
                            }

                            profile.type = ProfileTypes.ticYearlyProfile;
                            profile.factor = 1;

                            List<double> values = indexedDoubles.GetValues(new Range<int>(0, 8759));

                            float[] values_float = new float[values.Count + 1];
                            for (int i = 1; i < values_float.Length; i++)
                            {
                                values_float[i] = System.Convert.ToSingle(values[i - 1]);
                            }

                            profile.SetYearlyValues(values_float);
                        }

                    }
                }

                sAMTBDDocument.Save();

                tBDDocument.simulate(1, 365, 0, 1, 0, 0, resultantTemperaturePreparation.Path_TSD_Simulation, 1, 0);

                bool finished = Core.Query.WaitToUnlock(resultantTemperaturePreparation.Path_TSD_Simulation);
                if (finished)
                {
                    sAMTBDDocument.Save();
                }

                path_TSD = resultantTemperaturePreparation.Path_TSD_Simulation;
                path_TBD = resultantTemperaturePreparation.Path_TBD_Simulation;

                if (!finished)
                {
                    refusals.Add(string.Format("The second simulation did not finish, so '{0}' cannot be assessed.", resultantTemperaturePreparation.Path_TSD_Simulation));
                }

                return finished;
            }
        }
    }
}
