// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>simdata of the plant-room pass: loads, ducts and the rest the duct read-back needs (measured, SAM#111 PR5A Phase 0).</summary>
        private const int RecirculationCooling_SimulationData = 47;

        /// <summary>
        /// PR5B (SAM#111): the evidence of what every recirculation cooling branch of a converted and
        /// simulated route document did, hour by hour - and whether that is what the branch was built to do.
        /// <para>
        /// <b>Why a second pass, and on a copy.</b> The route simulates each air system with
        /// <c>ISystem.Simulate</c>, which answers the room temperatures and nothing a duct carries. A coil's
        /// inlet and outlet state and a duct's airflow answer only after a plant-room <c>SimulateEx</c> with a
        /// duct data mask (measured, SAM#111 PR5A Phase 0) - the same physics, per the same measurement - so
        /// that pass is made on a disposable copy beside the document, never on the route's own file, and
        /// its room temperatures are compared with the route's as a consistency figure.
        /// </para>
        /// <para>
        /// <b>What is read, all by identity:</b> the coil's single inlet duct (the mixed return: temperature
        /// and airflow - the branch's OperatingAirFlow) and single outlet duct (its supply temperature); every
        /// ventilation duty carrier's outlet flow against its leg's design; every ventilation fan's outlet
        /// flow against its unit's design supply or extract total; and the outdoor dry bulb from the thermal
        /// source's own weather. The judgement is <see cref="Create.RecirculationCoolingResult"/>'s.
        /// </para>
        /// </summary>
        public static RecirculationCoolingResults RecirculationCoolingResults(
            string path_TPD,
            string path_TSD,
            SystemVentilationConversionContext systemVentilationConversionContext,
            SystemZoneTemperatureResults systemZoneTemperatureResults,
            int startHour,
            int endHour)
        {
            RecirculationCoolingResults result = new RecirculationCoolingResults(
                startHour,
                endHour,
                "TAS plant-room SimulateEx (duct data) on a copy of the route's TPD: coil inlet/outlet ducts, ventilation carrier and fan ducts, outdoor dry bulb from the thermal source TSD.");

            List<MechanicalVentilationRecirculationCooling> recirculationCoolings = systemVentilationConversionContext?.RecirculationCoolings;
            if (recirculationCoolings == null || recirculationCoolings.Count == 0)
            {
                result.Refuse("No recirculation cooling branch was stated, so there is no cooling evidence to read.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(path_TPD) || !File.Exists(path_TPD) || endHour < startHour)
            {
                result.Refuse(string.Format("The route's TPD '{0}' could not be read for recirculation cooling evidence over hours {1}..{2}.", path_TPD, startHour, endHour));
                return result;
            }

            int count = endHour - startHour + 1;

            List<double> outdoorTemperature_C = OutdoorTemperature(path_TSD, startHour, endHour, out string refusal_Outdoor);
            if (refusal_Outdoor != null)
            {
                result.Refuse(refusal_Outdoor);
                return result;
            }

            //Each unit's bindings: its native system and its design totals.
            Dictionary<Guid, List<SystemVentilationBinding>> bindings_By_AirSystem = new Dictionary<Guid, List<SystemVentilationBinding>>();
            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationConversionContext.Bindings)
            {
                if (!bindings_By_AirSystem.TryGetValue(systemVentilationBinding.Guid_AirSystem, out List<SystemVentilationBinding> bindings))
                {
                    bindings = new List<SystemVentilationBinding>();
                    bindings_By_AirSystem[systemVentilationBinding.Guid_AirSystem] = bindings;
                }

                bindings.Add(systemVentilationBinding);
            }

            string path_Copy = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(path_TPD)) ?? string.Empty,
                string.Format("{0}.cooling-evidence-{1:N}.tpd", Path.GetFileNameWithoutExtension(path_TPD), Guid.NewGuid()));

            try
            {
                File.Copy(path_TPD, path_Copy, true);

                using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_Copy))
                {
                    TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;
                    EnergyCentre energyCentre = tPDDoc?.EnergyCentre;

                    if (energyCentre == null)
                    {
                        result.Refuse(string.Format("The copy '{0}' of the route's TPD could not be opened for recirculation cooling evidence.", path_Copy));
                        return result;
                    }

                    double externalPollutant = (double)((dynamic)energyCentre).ExternalPollutant.Value;

                    Dictionary<string, global::TPD.System> system_By_Reference = new Dictionary<string, global::TPD.System>(StringComparer.OrdinalIgnoreCase);

                    int count_PlantRoom = energyCentre.GetPlantRoomCount();
                    for (int i = 1; i <= count_PlantRoom; i++)
                    {
                        PlantRoom plantRoom = energyCentre.GetPlantRoom(i);
                        if (plantRoom == null)
                        {
                            continue;
                        }

                        string diagnostic = (string)((dynamic)plantRoom).SimulateEx(startHour + 1, endHour + 1, 0, externalPollutant, 10.0, RecirculationCooling_SimulationData, 1, 0);
                        result.Note(string.Format("Recirculation cooling evidence: plant room {0} answered \"{1}\".", i, (diagnostic ?? string.Empty).Trim()));

                        if (SimulationDiagnostic.IsFailure(diagnostic))
                        {
                            result.Refuse(string.Format("The plant-room pass that reads the recirculation cooling evidence failed: \"{0}\".", (diagnostic ?? string.Empty).Trim()));
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

                    double maximumZoneTemperatureDifference = 0;

                    foreach (MechanicalVentilationRecirculationCooling recirculationCooling in recirculationCoolings)
                    {
                        string label = string.Format("Recirculation cooling of air system {0}", recirculationCooling.Guid_AirSystem);

                        if (!bindings_By_AirSystem.TryGetValue(recirculationCooling.Guid_AirSystem, out List<SystemVentilationBinding> bindings)
                            || bindings.Count == 0
                            || !system_By_Reference.TryGetValue(bindings[0].Reference_System, out global::TPD.System system))
                        {
                            result.Refuse(string.Format("{0}: its native air system was not found in the evidence copy.", label));
                            continue;
                        }

                        ISystemComponent dXCoil = Component(system, systemVentilationConversionContext.Reference(recirculationCooling.Guid_DXCoil));
                        ISystemComponent fan_Recirculation = Component(system, systemVentilationConversionContext.Reference(recirculationCooling.Guid_Fan));

                        List<Duct> ducts_In = dXCoil == null ? null : Query.Ducts(dXCoil, Direction.In);
                        List<Duct> ducts_Out = dXCoil == null ? null : Query.Ducts(dXCoil, Direction.Out);

                        if (dXCoil == null || fan_Recirculation == null || ducts_In == null || ducts_In.Count != 1 || ducts_Out == null || ducts_Out.Count != 1)
                        {
                            result.Refuse(string.Format("{0}: its coil, fan or the coil's single inlet and outlet duct were not found in the evidence copy.", label));
                            continue;
                        }

                        List<double> mixedReturnTemperature_C = Series(ducts_In[0], 3, startHour, count);
                        List<double> operatingAirFlow_Lps = Series(ducts_In[0], 1, startHour, count);
                        List<double> supplyTemperature_C = Series(ducts_Out[0], 3, startHour, count);

                        List<double> canonicalDeviation_Lps = CanonicalDeviation(systemVentilationConversionContext, system, bindings, Query.NativeReference(fan_Recirculation), startHour, count, out string refusal_Canonical);
                        if (refusal_Canonical != null)
                        {
                            result.Refuse(string.Format("{0}: {1}", label, refusal_Canonical));
                        }

                        result.Add(Create.RecirculationCoolingResult(
                            recirculationCooling,
                            startHour,
                            outdoorTemperature_C,
                            mixedReturnTemperature_C,
                            operatingAirFlow_Lps,
                            supplyTemperature_C,
                            canonicalDeviation_Lps));

                        //The same document, twice: the rooms should read what the route read.
                        foreach (SystemVentilationBinding systemVentilationBinding in bindings)
                        {
                            List<double> zoneTemperature_C = Series(Component(system, systemVentilationBinding.Reference_SystemZone), 9, startHour, count);
                            SystemZoneTemperatureResult systemZoneTemperatureResult = systemZoneTemperatureResults?.Result(systemVentilationBinding.Guid_Space);

                            for (int h = 0; zoneTemperature_C != null && systemZoneTemperatureResult != null && h < count; h++)
                            {
                                if (systemZoneTemperatureResult.TryGetValue(startHour + h, out double value))
                                {
                                    maximumZoneTemperatureDifference = System.Math.Max(maximumZoneTemperatureDifference, System.Math.Abs(zoneTemperature_C[h] - value));
                                }
                            }
                        }
                    }

                    result.MaximumZoneTemperatureDifference_K = maximumZoneTemperatureDifference;
                    result.Note(string.Format("Recirculation cooling evidence: the plant-room pass reproduces the route's room temperatures to {0:0.######} K.", maximumZoneTemperatureDifference));
                }
            }
            catch (Exception exception)
            {
                result.Refuse(string.Format("Reading the recirculation cooling evidence threw {0}: {1}", exception.GetType().Name, exception.Message));
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
                    //A leftover evidence copy is an untidy file, not a wrong answer; the evidence above
                    //was read in full or refused.
                }
            }

            return result;
        }

        private static ISystemComponent Component(global::TPD.System system, string reference)
        {
            if (system == null || string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            try
            {
                return system.GetComponentByGUID(reference) as ISystemComponent;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>One hourly result series of a native duct or component, or null where TAS answered none of the right length.</summary>
        private static List<double> Series(object target, int variable, int startHour, int count)
        {
            if (target == null)
            {
                return null;
            }

            object @object;
            try
            {
                @object = ((dynamic)target).GetResultsData(tpdResultsPeriod.tpdResultsPeriodHourly, tpdCombinerType.tpdCombinerTypeMax, variable, startHour + 1, count);
            }
            catch
            {
                return null;
            }

            if (!(@object is IEnumerable enumerable))
            {
                return null;
            }

            List<double> result = new List<double>(count);
            foreach (object value in enumerable)
            {
                result.Add(System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
            }

            return result.Count == count ? result : null;
        }

        /// <summary>
        /// Each hour's largest departure of any ventilation flow of one air system from its design: every duty
        /// carrier against its leg's duty, and every ventilation fan against the unit's design supply or
        /// extract total. The recirculation fan is the cooling loop's and is not counted.
        /// </summary>
        private static List<double> CanonicalDeviation(
            SystemVentilationConversionContext systemVentilationConversionContext,
            global::TPD.System system,
            List<SystemVentilationBinding> bindings,
            string reference_Fan_Recirculation,
            int startHour,
            int count,
            out string refusal)
        {
            refusal = null;

            double[] result = new double[count];

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents_AirSystem(bindings[0].Guid_AirSystem))
            {
                if (systemVentilationLegIntent.Guid_DutyCarrier == Guid.Empty)
                {
                    continue;
                }

                ISystemComponent damper = Component(system, systemVentilationConversionContext.Reference(systemVentilationLegIntent.Guid_DutyCarrier));
                List<double> flow = FlowOut(damper, startHour, count);
                if (flow == null)
                {
                    refusal = string.Format("the flow of ventilation duty carrier {0} was not answered.", systemVentilationLegIntent.Guid_DutyCarrier);
                    return null;
                }

                for (int h = 0; h < count; h++)
                {
                    result[h] = System.Math.Max(result[h], System.Math.Abs(flow[h] - systemVentilationLegIntent.DesignFlowRate_Lps));
                }
            }

            double supply_Lps = 0, extract_Lps = 0;
            foreach (SystemVentilationBinding systemVentilationBinding in bindings)
            {
                supply_Lps += systemVentilationBinding.DesignFlowRate_Supply_Lps ?? 0;
                extract_Lps += systemVentilationBinding.DesignFlowRate_Extract_Lps ?? 0;
            }

            int count_Fan = 0;
            foreach (global::TPD.SystemComponent systemComponent in Query.SystemComponents<global::TPD.SystemComponent>(system) ?? new List<global::TPD.SystemComponent>())
            {
                if (!(systemComponent is global::TPD.Fan fan))
                {
                    continue;
                }

                if (string.Equals(Query.NativeReference(fan), reference_Fan_Recirculation, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                List<double> flow = FlowOut((ISystemComponent)fan, startHour, count);
                if (flow == null)
                {
                    refusal = string.Format("the flow of ventilation fan {0} was not answered.", Query.NativeReference(fan));
                    return null;
                }

                count_Fan++;
                for (int h = 0; h < count; h++)
                {
                    result[h] = System.Math.Max(result[h], System.Math.Min(System.Math.Abs(flow[h] - supply_Lps), System.Math.Abs(flow[h] - extract_Lps)));
                }
            }

            if (count_Fan == 0)
            {
                refusal = "no ventilation fan was found beside the recirculation fan.";
                return null;
            }

            return new List<double>(result);
        }

        /// <summary>A component's total outlet airflow [l/s], hour by hour: the sum over its outlet ducts.</summary>
        private static List<double> FlowOut(ISystemComponent systemComponent, int startHour, int count)
        {
            List<Duct> ducts = systemComponent == null ? null : Query.Ducts(systemComponent, Direction.Out);
            if (ducts == null || ducts.Count == 0)
            {
                return null;
            }

            double[] result = new double[count];
            foreach (Duct duct in ducts)
            {
                List<double> flow = Series(duct, 1, startHour, count);
                if (flow == null)
                {
                    return null;
                }

                for (int h = 0; h < count; h++)
                {
                    result[h] += flow[h];
                }
            }

            return new List<double>(result);
        }

        /// <summary>The outdoor dry bulb [degC] of each hour, from the thermal source's own TSD.</summary>
        private static List<double> OutdoorTemperature(string path_TSD, int startHour, int endHour, out string refusal)
        {
            refusal = null;

            if (string.IsNullOrWhiteSpace(path_TSD) || !File.Exists(path_TSD))
            {
                refusal = string.Format("The thermal source TSD '{0}' is not there, so the outdoor dry bulb the cooling table is read at is unknown.", path_TSD);
                return null;
            }

            List<double> result = new List<double>(endHour - startHour + 1);

            try
            {
                using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
                {
                    TSD.BuildingData buildingData = sAMTSDDocument.TSDDocument?.SimulationData?.GetBuildingData();
                    if (buildingData == null)
                    {
                        refusal = string.Format("The thermal source TSD '{0}' carries no building results, so the outdoor dry bulb is unknown.", path_TSD);
                        return null;
                    }

                    for (int hour = startHour; hour <= endHour; hour++)
                    {
                        result.Add(buildingData.GetHourlyBuildingResult(hour + 1, (int)TSD.tsdBuildingArray.externalTemperature));
                    }
                }
            }
            catch (Exception exception)
            {
                refusal = string.Format("Reading the outdoor dry bulb from '{0}' threw {1}: {2}", path_TSD, exception.GetType().Name, exception.Message);
                return null;
            }

            return result;
        }
    }
}
