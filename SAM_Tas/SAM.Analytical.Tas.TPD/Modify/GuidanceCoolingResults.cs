// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// SAM#123: reads back, hour by hour, what each manufacturer-guidance cooling unit did - intake,
        /// stat-room air, extract, exchanger leaving, supply, supply and extract airflow and the coil's
        /// sensible and latent duty - from a plant-room pass with duct data on a copy of the route's TPD (the
        /// same method as the PR5B evidence). A read that cannot be made refuses; what the unit did is reported,
        /// not judged pass/fail here.
        /// </summary>
        public static GuidanceCoolingResults GuidanceCoolingResults(
            string path_TPD,
            SystemVentilationConversionContext systemVentilationConversionContext,
            int startHour,
            int endHour)
        {
            GuidanceCoolingResults result = new GuidanceCoolingResults(
                startHour,
                endHour,
                "TAS plant-room SimulateEx (duct data) on a copy of the route's TPD: exchanger inlet ducts, DX coil inlet/outlet ducts, stat-room zone air (series 9), DX sensible/latent (series 10/11).");

            List<MechanicalVentilationGuidanceCooling> guidanceCoolings = systemVentilationConversionContext?.GuidanceCoolings;
            if (guidanceCoolings == null || guidanceCoolings.Count == 0)
            {
                result.Refuse("No manufacturer-guidance cooling unit was stated, so there is nothing to read.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(path_TPD) || !File.Exists(path_TPD) || endHour < startHour)
            {
                result.Refuse(string.Format("The route's TPD '{0}' could not be read for manufacturer-guidance evidence over hours {1}..{2}.", path_TPD, startHour, endHour));
                return result;
            }

            int count = endHour - startHour + 1;

            Dictionary<Guid, string> reference_System_By_AirSystem = new Dictionary<Guid, string>();
            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationConversionContext.Bindings)
            {
                reference_System_By_AirSystem[systemVentilationBinding.Guid_AirSystem] = systemVentilationBinding.Reference_System;
            }

            string path_Copy = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(path_TPD)) ?? string.Empty,
                string.Format("{0}.guidance-evidence-{1:N}.tpd", Path.GetFileNameWithoutExtension(path_TPD), Guid.NewGuid()));

            try
            {
                using (TPDProfiler.Current?.Measure("Guidance evidence: copy TPD"))
                {
                    File.Copy(path_TPD, path_Copy, true);
                }

                IDisposable measure_Open = TPDProfiler.Current?.Measure("Guidance evidence: open TPD copy");

                using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_Copy))
                {
                    measure_Open?.Dispose();

                    EnergyCentre energyCentre = sAMTPDDocument.TPDDocument?.EnergyCentre;
                    if (energyCentre == null)
                    {
                        result.Refuse(string.Format("The copy '{0}' of the route's TPD could not be opened for manufacturer-guidance evidence.", path_Copy));
                        return result;
                    }

                    double externalPollutant = (double)((dynamic)energyCentre).ExternalPollutant.Value;
                    Dictionary<string, global::TPD.System> system_By_Reference = new Dictionary<string, global::TPD.System>(StringComparer.OrdinalIgnoreCase);

                    for (int i = 1; i <= energyCentre.GetPlantRoomCount(); i++)
                    {
                        PlantRoom plantRoom = energyCentre.GetPlantRoom(i);
                        if (plantRoom == null)
                        {
                            continue;
                        }

                        string diagnostic;
                        using (TPDProfiler.Current?.Measure("Guidance evidence: TAS plant-room SimulateEx (duct data)"))
                        {
                            diagnostic = (string)((dynamic)plantRoom).SimulateEx(startHour + 1, endHour + 1, 0, externalPollutant, 10.0, RecirculationCooling_SimulationData, 1, 0);
                        }
                        result.Note(string.Format("Manufacturer-guidance evidence: plant room {0} answered \"{1}\".", i, (diagnostic ?? string.Empty).Trim()));

                        if (SimulationDiagnostic.IsFailure(diagnostic))
                        {
                            result.Refuse(string.Format("The plant-room pass that reads the manufacturer-guidance evidence failed: \"{0}\".", (diagnostic ?? string.Empty).Trim()));
                            return result;
                        }

                        for (int j = 1; j <= plantRoom.GetSystemCount(); j++)
                        {
                            global::TPD.System system = plantRoom.GetSystem(j);
                            string reference = system == null ? null : Query.NativeReference(system);
                            if (reference != null)
                            {
                                system_By_Reference[reference] = system;
                            }
                        }
                    }

                    IDisposable measure_Read = TPDProfiler.Current?.Measure("Guidance evidence: read hourly series (all units)");

                    foreach (MechanicalVentilationGuidanceCooling guidanceCooling in guidanceCoolings)
                    {
                        string label = string.Format("Manufacturer-guidance cooling of air system {0}", guidanceCooling.Guid_AirSystem);

                        if (!TryGetGuidanceRecipe(guidanceCooling, out GuidanceRecipe recipe, out string refusal_Recipe))
                        {
                            result.Refuse(string.Format("{0}: {1}", label, refusal_Recipe));
                            continue;
                        }

                        if (!reference_System_By_AirSystem.TryGetValue(guidanceCooling.Guid_AirSystem, out string reference_System) || !system_By_Reference.TryGetValue(reference_System, out global::TPD.System system))
                        {
                            result.Refuse(string.Format("{0}: its native air system was not found in the evidence copy.", label));
                            continue;
                        }

                        ISystemComponent exchanger = Component(system, systemVentilationConversionContext.Reference(guidanceCooling.Guid_Exchanger));
                        ISystemComponent dXCoil = Component(system, systemVentilationConversionContext.Reference(guidanceCooling.Guid_DXCoil));
                        MechanicalVentilationGuidanceRoom room_Stat = guidanceCooling.Rooms.Find(x => x.Guid_Space == guidanceCooling.Guid_Space_Stat);
                        ISystemComponent systemZone_Stat = room_Stat == null ? null : Component(system, systemVentilationConversionContext.Reference(room_Stat.Guid_SystemSpace));

                        Duct duct_Intake = null, duct_Extract = null;
                        foreach (Duct duct in (exchanger == null ? null : Query.Ducts(exchanger, Direction.In)) ?? new List<Duct>())
                        {
                            int port = (int)((dynamic)duct).GetDownstreamComponentPort();
                            if (port == 1)
                            {
                                duct_Intake = duct;
                            }
                            else if (port == 2)
                            {
                                duct_Extract = duct;
                            }
                        }

                        List<Duct> ducts_In = dXCoil == null ? null : Query.Ducts(dXCoil, Direction.In);
                        List<Duct> ducts_Out = dXCoil == null ? null : Query.Ducts(dXCoil, Direction.Out);

                        if (duct_Intake == null || duct_Extract == null || systemZone_Stat == null || ducts_In == null || ducts_In.Count != 1 || ducts_Out == null || ducts_Out.Count != 1)
                        {
                            result.Refuse(string.Format("{0}: the exchanger's intake/extract ducts, the coil's single inlet and outlet duct or the stat room's zone were not found in the evidence copy.", label));
                            continue;
                        }

                        List<double>[] series =
                        {
                            Series(duct_Intake, 3, startHour, count),
                            Series(systemZone_Stat, 9, startHour, count),
                            Series(duct_Extract, 3, startHour, count),
                            Series(ducts_In[0], 3, startHour, count),
                            Series(ducts_Out[0], 3, startHour, count),
                            Series(ducts_In[0], 1, startHour, count),
                            Series(duct_Extract, 1, startHour, count),
                            Series(dXCoil, 10, startHour, count),
                            Series(dXCoil, 11, startHour, count),
                        };

                        if (Array.Exists(series, x => x == null))
                        {
                            result.Refuse(string.Format("{0}: TAS answered no complete hourly series for one of its read-backs.", label));
                            continue;
                        }

                        GuidanceCoolingResult guidanceCoolingResult = new GuidanceCoolingResult(
                            guidanceCooling.Guid_AirSystem,
                            (string)((dynamic)system).Name,
                            recipe.DesignSupply_Lps,
                            recipe.DesignExtract_Lps,
                            recipe.Elevated_Lps,
                            recipe.CoolingExtractFraction,
                            recipe.CoilNetDrop_K,
                            recipe.MinimumSupply_C,
                            recipe.BypassMinimumIntake_C,
                            recipe.BypassMinimumExtract_C,
                            recipe.CoolingDuty_W,
                            recipe.ActivationTemperature_C,
                            series[0], series[1], series[2], series[3], series[4], series[5], series[6], series[7], series[8])
                        {
                            StartHour = startHour,
                        };

                        result.Add(guidanceCoolingResult);
                        result.Note(guidanceCoolingResult.Summary());
                    }

                    measure_Read?.Dispose();
                }
            }
            catch (Exception exception)
            {
                result.Refuse(string.Format("Reading the manufacturer-guidance evidence threw {0}: {1}", exception.GetType().Name, exception.Message));
            }
            finally
            {
                try
                {
                    if (File.Exists(path_Copy))
                    {
                        File.Delete(path_Copy);
                    }
                }
                catch
                {
                }
            }

            return result;
        }
    }
}
