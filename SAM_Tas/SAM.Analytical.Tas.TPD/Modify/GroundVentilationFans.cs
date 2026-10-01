// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// Settles what every fan of an explicit ventilation system contributes and when it runs, and
        /// refuses rather than carrying an operating profile the analytical model does not state.
        /// <para>
        /// <b>Fan heat gain follows <see cref="SystemVentilationConversionContext.FanHeatGainPolicy"/></b>
        /// (PR5A, SAM#111 plan §D/§K.3). <b><c>ClearToZero</c>, the default:</b> <c>HeatGainFactor = 0</c>,
        /// no part of the fan's motor power is added to the air stream. The shipped <c>MV.json</c>
        /// prototype states <c>1.0</c>, and carrying that through is a real thermal statement, not a
        /// formality - measured on the acceptance fixture, zeroing it moved <c>ZoneTemperature</c> by up
        /// to <b>2.80 K</b> (worst hour, the transfer-fed corridor) and 0.13 K in the annual mean. So it is
        /// set here deliberately and read back, not left to whatever the template happened to say. This is
        /// B0's control, and every B-variant is paired against a B0 run using it. <b><c>FromSystemsGraph</c>,
        /// the manufacturer-aware route:</b> nothing is written - the value SAM_Systems already resolved
        /// onto the graph (a declared fan-heat fraction, plan §C/§F) is left exactly as
        /// <c>Convert.ToTPD(SystemFan, …)</c> stated it, and only read back.
        /// </para>
        /// <para>
        /// SAM's own replicated routes are split on this: <c>TPD_CAV</c>, <c>TPD_EOL</c> and
        /// <c>TPD_EOC</c> zero it, while <c>TPD_MV</c>, <c>TPD_VAV</c> and <c>TPD_MVRE</c> leave it at 1.
        /// <c>ClearToZero</c> follows the former. <b>It is a deviation from <c>TPD_MV</c>, stated as one.</b>
        /// </para>
        /// <para>
        /// <b>The duty itself is never authored.</b> Measured on a TAS-authored file, a fan set to
        /// <c>tpdFlowRateAllAttachedZonesFreshAir</c> answered <c>37.9420166015625</c>, exactly its
        /// zone's <c>FreshAir.Value</c>, and one set to <c>tpdFlowRateAllAttachedZonesFlowRate</c>
        /// answered <c>3517.4283114346595</c>, exactly its zone's <c>FlowRate.Value</c>. So a fan's
        /// duty follows the zones attached to it, and this route <b>reports</b> that rather than
        /// writing a fan flow of its own - writing one would state a design the analytical model does
        /// not contain.
        /// </para>
        /// <para>
        /// <b>Operation is continuous at factor 1.0, and that is read back rather than assumed.</b> The
        /// frozen #111 parity configuration is supplied as an ordinary 8760-hour constant-1.0
        /// <c>YearlySchedule</c> through PR1's <c>MechanicalVentilationSettings.Schedule</c>: PR1 names it
        /// on every fan, <c>Modify.Add(EnergyCentre, ISchedule)</c> writes it as a
        /// <c>tpdScheduleYearly</c> table and <c>Convert.ToTPD(SystemFan, …)</c> attaches it by name. A
        /// TAS plant schedule is an on/off table, so factor 1.0 is that table on in all 8760 hours - which
        /// TAS reports as <c>GetNumOperableHours() == 8760</c>, read here off each fan's own schedule.
        /// </para>
        /// <para>
        /// Anything else is refused - see <see cref="Query.ContinuousOperationRefusal"/>. That includes
        /// the shipped <c>MV.json</c> <c>"Occupancy Schedule"</c> every fan keeps when no schedule is
        /// supplied: a <c>tpdScheduleFunctionAllZonesLoad</c> function schedule that switches the fan with
        /// the attached zones' demand. The route used to accept it, and to refuse the frozen yearly
        /// schedule; that was a defect against the parity contract, measured and recorded in
        /// the SAM_Tas PR record.
        /// </para>
        /// <para>
        /// Every damper and every zone of the produced system answers <c>GetSchedule() == null</c>, 23 of
        /// 23 on the acceptance fixture, so the fan schedule is the <b>only</b> operation carrier in the
        /// air side. <c>PartLoad</c> is a part-load performance table and <c>OverallEfficiency.Value = 1</c>
        /// with no modifier - neither is an operating profile.
        /// </para>
        /// <para>
        /// <b>Every read here is late-bound.</b> The typed
        /// <c>((global::TPD.SystemComponent)fan).GetSchedule()</c> throws
        /// <c>DISP_E_MEMBERNOTFOUND</c> on a fan - see <see cref="Schedule"/>.
        /// </para>
        /// <para>
        /// <b>What the simulated document proves.</b> A fan answers exactly one hourly results series,
        /// <c>GetResultsData</c> variable 9 - its Load, <c>Q x dp / eta</c> - and every other variable
        /// <c>0..24</c> answers "Failed to get the results series". Delivered flow is therefore read as
        /// <c>Load x eta / dp</c>; the licensed acceptance holds it against the derived duty in every
        /// hour. That read belongs to the acceptance, not to this conversion step, which runs before
        /// anything is simulated.
        /// </para>
        /// </summary>
        public static bool GroundVentilationFans(
            SystemVentilationConversionContext systemVentilationConversionContext,
            global::TPD.System system)
        {
            if (systemVentilationConversionContext == null)
            {
                return false;
            }

            if (system == null)
            {
                systemVentilationConversionContext.Refuse(
                    "An air system produced no native TAS system, so its fans could not be grounded.");

                return false;
            }

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(system);
            if (systemComponents == null)
            {
                return true;
            }

            List<string> notes = new List<string>();

            foreach (global::TPD.SystemComponent systemComponent in systemComponents)
            {
                if (!(systemComponent is global::TPD.Fan fan))
                {
                    continue;
                }

                string reference_Fan = Query.NativeReference(fan) ?? "<no identifier>";

                if (!TryGroundHeatGain(systemVentilationConversionContext, fan, reference_Fan, out string heatGainNote))
                {
                    continue;
                }

                if (!TryCheckOperation(systemVentilationConversionContext, fan, reference_Fan, out string operation))
                {
                    continue;
                }

                //No litres per second here on purpose. The switch below means TAS derives the duty
                //from the attached zones, and it has not settled at this point in the conversion - the
                //carrier still reads the template prototype's figure (277.33 l/s on the acceptance
                //fixture, against the 44 l/s the saved document answers). Reporting that number would
                //state a duty this route neither authored nor believes.
                notes.Add(string.Format(
                    "Fan {0} derives its duty from the attached zones ({1}), {2} and runs continuously at "
                    + "factor 1.0 - {3}.",
                    reference_Fan,
                    fan.DesignFlowType,
                    heatGainNote,
                    operation));
            }

            notes.Sort(StringComparer.Ordinal);

            foreach (string note in notes)
            {
                systemVentilationConversionContext.Note(note);
            }

            return systemVentilationConversionContext.Refusals.Count == 0;
        }

        /// <summary>
        /// Grounds a fan's native <c>HeatGainFactor</c> according to
        /// <see cref="SystemVentilationConversionContext.FanHeatGainPolicy"/> - PR5A (SAM#111 plan
        /// §D/§K.3) - and always reads it back.
        /// <para>
        /// <c>ClearToZero</c> (the B0 control, and the default) behaves exactly as this method always has:
        /// it forces 0 and refuses if TAS did not keep it. <c>FromSystemsGraph</c> writes nothing - the
        /// value is whatever <c>Convert.ToTPD(SystemFan, …)</c> already stated, which may be a resolved
        /// manufacturer figure - and only reads it back, so a silent native change of a value this route
        /// never touched is still caught.
        /// </para>
        /// </summary>
        private static bool TryGroundHeatGain(
            SystemVentilationConversionContext systemVentilationConversionContext,
            global::TPD.Fan fan,
            string reference_Fan,
            out string note)
        {
            note = null;

            bool clearToZero = systemVentilationConversionContext.FanHeatGainPolicy == SystemVentilationFanHeatGainPolicy.ClearToZero;

            if (clearToZero)
            {
                try
                {
                    fan.HeatGainFactor = 0;
                }
                catch (Exception exception)
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Fan {0}: clearing its heat gain factor threw {1}: {2}.",
                        reference_Fan,
                        exception.GetType().Name,
                        exception.Message));

                    return false;
                }
            }

            //Read back off the native object either way. A write TAS silently declined would otherwise
            //leave the template's 1.0 in place and put the fan's motor power into the air stream
            //unannounced; a FromSystemsGraph value TAS silently changed would otherwise go unnoticed too.
            double heatGainFactor = fan.HeatGainFactor;

            if (clearToZero && heatGainFactor != 0)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Fan {0}: TAS did not keep the cleared heat gain factor - it reports {1}, so the fan "
                    + "would still add its motor power to the air stream.",
                    reference_Fan,
                    heatGainFactor));

                return false;
            }

            note = clearToZero
                ? string.Format("adds no heat to the air stream (HeatGainFactor {0})", heatGainFactor)
                : string.Format("carries the systems graph's own heat gain factor (HeatGainFactor {0})", heatGainFactor);

            return true;
        }

        /// <summary>
        /// Reads the schedule TAS holds for a fan, <b>late-bound</b>.
        /// <para>
        /// The typed call <c>((global::TPD.SystemComponent)fan).GetSchedule()</c> throws
        /// <c>COMException: Member not found. (0x80020003 DISP_E_MEMBERNOTFOUND)</c> on every fan of a
        /// produced system - measured, on all four fans of the acceptance document. That is the same
        /// split checkpoint 1 recorded for <c>ISystemComponent.GUID</c> and <c>.Name</c>: on this
        /// interop the typed <c>ISystemComponent</c> accessors are unreliable and the late-bound read
        /// is the correct one. Replacing this with a typed call - which would look like a tidy-up -
        /// turns the whole route into a refusal.
        /// </para>
        /// </summary>
        private static object Schedule(global::TPD.Fan fan)
        {
            return ((dynamic)fan).GetSchedule();
        }

        private static bool TryCheckOperation(
            SystemVentilationConversionContext systemVentilationConversionContext,
            global::TPD.Fan fan,
            string reference_Fan,
            out string operation)
        {
            operation = null;

            object plantSchedule;

            try
            {
                plantSchedule = Schedule(fan);
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Fan {0}: reading its schedule threw {1}: {2}, so its operation could not be settled.",
                    reference_Fan,
                    exception.GetType().Name,
                    exception.Message));

                return false;
            }

            int? type = null;
            int? operableHours = null;
            string name = null;

            if (plantSchedule != null)
            {
                try
                {
                    type = (int)((dynamic)plantSchedule).Type;
                    name = ((dynamic)plantSchedule).Name as string;
                }
                catch (Exception exception)
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Fan {0}: reading the schedule attached to it threw {1}: {2}, so its operation could "
                        + "not be settled.",
                        reference_Fan,
                        exception.GetType().Name,
                        exception.Message));

                    return false;
                }

                //Only a yearly table answers this. On a function schedule TAS throws "Not a Yearly
                //Schedule" - measured - and an unreadable count is refused below, never assumed.
                if (type == (int)tpdScheduleType.tpdScheduleYearly)
                {
                    try
                    {
                        operableHours = (int)((dynamic)plantSchedule).GetNumOperableHours();
                    }
                    catch
                    {
                        operableHours = null;
                    }
                }
            }

            string refusal = Query.ContinuousOperationRefusal(type, operableHours);

            if (refusal != null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Fan {0}{1} {2}",
                    reference_Fan,
                    name == null ? string.Empty : string.Format(" (schedule \"{0}\")", name),
                    refusal));

                return false;
            }

            operation = string.Format(
                "its operation carrier is the yearly schedule \"{0}\", operable in {1} of {2} hours",
                name,
                operableHours,
                Query.HoursPerYear);

            return true;
        }
    }
}
