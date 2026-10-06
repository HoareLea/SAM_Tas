// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// PR5B (SAM#111): grounds one air system's recirculation cooling branch in native TAS - every
        /// property the graph states read back off the native objects, then the controller that turns the
        /// branch's law into flow.
        /// <para>
        /// <b>Read back, refused on any disagreement:</b> the coil's cooling setpoint is exactly the
        /// published equality table (every cell, extrapolation off); its heating setpoint is the
        /// cooling-enable temperature with no modifier and its heating duty is an absolute zero, so it never
        /// heats; the recirculation fan adds no heat, runs at variable speed and states the ceiling as an
        /// absolute value; every recirculation damper states its room's share as an absolute value.
        /// </para>
        /// <para>
        /// <b>The controller - why here, and why this one</b> (licensed evidence, SAM#111 PR5B). A normal
        /// temperature controller senses the mixed return entering the coil and acts on the recirculation
        /// DAMPERS, linearly, from the law's lower point to its upper point; the fan is left uncontrolled at
        /// variable speed and follows them. A controlled Value damper passes design x signal, so the signal
        /// is the flow fraction itself. Controlling the fan instead disturbed the ventilation legs. The
        /// sensor duct is the native branch junction's bridge into the coil, which the conversion creates for
        /// the many-to-one return and which no SAM connection names - so it is found here, by topology, and
        /// that is why the controller is not a SAM_Systems object. It is active on every plant day type: the
        /// coil's own gate, not a calendar, decides when it cools.
        /// </para>
        /// </summary>
        /// <returns>True where the branch is grounded, or where the air system carries none.</returns>
        public static bool GroundRecirculationCooling(
            SystemVentilationConversionContext systemVentilationConversionContext,
            Guid guid_AirSystem,
            global::TPD.System system,
            Dictionary<Guid, ISystemComponent> dictionary_SystemComponent)
        {
            if (systemVentilationConversionContext == null)
            {
                return false;
            }

            MechanicalVentilationRecirculationCooling recirculationCooling = systemVentilationConversionContext.RecirculationCooling(guid_AirSystem);
            if (recirculationCooling == null)
            {
                return true;
            }

            MechanicalVentilationCoolingSettings settings = recirculationCooling.Settings;
            string label = string.Format("Recirculation cooling of air system {0}", guid_AirSystem);

            if (system == null || dictionary_SystemComponent == null || settings == null)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0} could not be grounded: no native system or no settings.", label));
                return false;
            }

            if (!(Native(dictionary_SystemComponent, recirculationCooling.Guid_DXCoil) is DXCoil dXCoil))
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the cooling coil {1} produced no native DX coil.", label, recirculationCooling.Guid_DXCoil));
                return false;
            }

            if (!(Native(dictionary_SystemComponent, recirculationCooling.Guid_Fan) is global::TPD.Fan fan))
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the recirculation fan {1} produced no native fan.", label, recirculationCooling.Guid_Fan));
                return false;
            }

            List<Damper> dampers = new List<Damper>();
            List<double> shares = new List<double>();

            foreach (MechanicalVentilationRecirculationRoom room in recirculationCooling.Rooms)
            {
                foreach (Guid guid_Damper in new[] { room.Guid_Damper_Return, room.Guid_Damper_Supply })
                {
                    if (!(Native(dictionary_SystemComponent, guid_Damper) is Damper damper))
                    {
                        systemVentilationConversionContext.Refuse(string.Format("{0}: recirculation damper {1} of room {2} produced no native damper.", label, guid_Damper, room.Guid_Space));
                        return false;
                    }

                    dampers.Add(damper);
                    shares.Add(room.DesignFlowRate_Lps);
                }
            }

            int count_Refusal = systemVentilationConversionContext.Refusals.Count;

            ReadBackCoil(systemVentilationConversionContext, label, dXCoil, settings);
            ReadBackFan(systemVentilationConversionContext, label, fan, settings.MaximumOperatingAirFlow_Lps);

            for (int i = 0; i < dampers.Count; i++)
            {
                ReadBackAbsoluteFlow(systemVentilationConversionContext, string.Format("{0}: recirculation damper {1}", label, Query.NativeReference(dampers[i])), dampers[i].DesignFlowType, dampers[i].DesignFlowRate, shares[i]);
            }

            if (systemVentilationConversionContext.Refusals.Count != count_Refusal)
            {
                return false;
            }

            //---------------------------------------------------------------------------------------------
            //The controller.
            //---------------------------------------------------------------------------------------------

            List<Duct> ducts_In = Query.Ducts((ISystemComponent)dXCoil, Direction.In);
            if (ducts_In == null || ducts_In.Count != 1)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0}: the cooling coil has {1} inlet duct(s); the mixed return has to arrive on exactly one, where the controller senses it.",
                    label,
                    ducts_In == null ? 0 : ducts_In.Count));

                return false;
            }

            double[] controlTemperatures = settings.FlowFractionByControlTemperature.ControlTemperatures_C;
            double[] flowFractions = settings.FlowFractionByControlTemperature.FlowFractions;

            try
            {
                Controller controller = system.AddController();
                controller.ControlType = tpdControlType.tpdControlNormal;
                controller.SensorType = tpdSensorType.tpdTempSensor;
                controller.SensorArc1 = controller.AddSensorArc(ducts_In[0]);

                ControllerProfileData controllerProfileData = controller.GetProfile();
                controllerProfileData.Clear();
                controllerProfileData.AddPoint(controlTemperatures[0], flowFractions[0]);
                controllerProfileData.AddPoint(controlTemperatures[1], flowFractions[1]);

                controller.Gradient = 1;
                controller.Setpoint = controlTemperatures[1];
                controller.Band = controlTemperatures[1] - controlTemperatures[0];
                controller.Min = flowFractions[0];
                controller.Max = flowFractions[1];

                ((dynamic)controller).Name = "Recirculation cooling law (mixed return)";
                controller.Description = "PR5B: recirculation flow fraction against the mixed-return temperature, acting on the recirculation dampers.";

                int count_DayType = 0;
                PlantCalendar plantCalendar = system.GetPlantRoom()?.GetEnergyCentre()?.GetCalendar();
                if (plantCalendar != null)
                {
                    for (int i = 1; i <= plantCalendar.GetDayTypeCount(); i++)
                    {
                        PlantDayType plantDayType = plantCalendar.GetDayType(i);
                        if (plantDayType != null)
                        {
                            controller.AddDayType(plantDayType);
                            count_DayType++;
                        }
                    }
                }

                foreach (Damper damper in dampers)
                {
                    controller.AddControlArc((global::TPD.SystemComponent)damper);
                }

                int count_ControlArc = controller.GetControlArcCount();
                if (count_ControlArc != dampers.Count || count_DayType == 0 || controller.GetDayTypeCount() != count_DayType)
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "{0}: the controller holds {1} control arc(s) for {2} recirculation damper(s) and {3} of {4} day type(s).",
                        label,
                        count_ControlArc,
                        dampers.Count,
                        controller.GetDayTypeCount(),
                        count_DayType));

                    return false;
                }
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: creating its controller threw {1}: {2}.", label, exception.GetType().Name, exception.Message));
                return false;
            }

            systemVentilationConversionContext.Note(string.Format(
                "{0} grounded: {1} room(s), coil table {2} cell(s) with extrapolation off, cooling enabled from {3} C with no heating duty, recirculation {4:0.###}..{5:0.###} l/s by a controller on the mixed return acting on {6} damper(s).",
                label,
                recirculationCooling.Rooms.Count,
                settings.SupplyAirTemperatureTable.PointCount,
                settings.CoolingEnableTemperature_C,
                settings.MinimumOperatingAirFlow_Lps,
                settings.MaximumOperatingAirFlow_Lps,
                dampers.Count));

            return true;
        }

        private static object Native(Dictionary<Guid, ISystemComponent> dictionary_SystemComponent, Guid guid)
        {
            return dictionary_SystemComponent.TryGetValue(guid, out ISystemComponent systemComponent) ? systemComponent : null;
        }

        private static void ReadBackCoil(SystemVentilationConversionContext systemVentilationConversionContext, string label, DXCoil dXCoil, MechanicalVentilationCoolingSettings settings)
        {
            //The table, cell by cell.
            TableModifier tableModifier_Expected = settings.SupplyAirTemperatureTable.SupplyAirTemperatureModifier(out double _);

            ProfileData profileData_Cooling = dXCoil.CoolingSetpoint;
            int count_Modifier = (int)((dynamic)profileData_Cooling).GetModifierCount();

            TableModifier tableModifier_Native = count_Modifier == 1 ? Convert.ToSAM((ProfileDataModifier)profileData_Cooling.GetModifier(1)) as TableModifier : null;

            string refusal_Table = TableRefusal(tableModifier_Expected, tableModifier_Native);
            if (count_Modifier != 1 || refusal_Table != null)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the coil's native cooling setpoint is not the published table ({1} modifier(s); {2}).", label, count_Modifier, refusal_Table ?? "no table"));
            }

            //The gate.
            ProfileData profileData_Heating = dXCoil.HeatingSetpoint;
            int count_Modifier_Heating = (int)((dynamic)profileData_Heating).GetModifierCount();
            if (count_Modifier_Heating != 0 || System.Math.Abs(profileData_Heating.Value - settings.CoolingEnableTemperature_C) > 1e-4)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the coil's native heating setpoint (its cooling-enable temperature) reads {1} C with {2} modifier(s), not {3} C.", label, profileData_Heating.Value, count_Modifier_Heating, settings.CoolingEnableTemperature_C));
            }

            //No heating.
            SizedVariable sizedVariable_HeatingDuty = dXCoil.HeatingDuty;
            if (sizedVariable_HeatingDuty == null || sizedVariable_HeatingDuty.Type != tpdSizedVariable.tpdSizedVariableValue || ((dynamic)sizedVariable_HeatingDuty).Value != 0.0)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the coil's native heating duty is not an absolute zero, so it could heat.", label));
            }
        }

        private static void ReadBackFan(SystemVentilationConversionContext systemVentilationConversionContext, string label, global::TPD.Fan fan, double ceiling_Lps)
        {
            if (fan.HeatGainFactor != 0)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the recirculation fan states a heat gain factor of {1}; a pressure-flow surrogate adds no heat.", label, fan.HeatGainFactor));
            }

            if (fan.ControlType != tpdFanControlType.tpdFanControlVariableSpeed)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the recirculation fan is not variable speed ({1}), so it cannot follow its dampers.", label, fan.ControlType));
            }

            ReadBackAbsoluteFlow(systemVentilationConversionContext, string.Format("{0}: the recirculation fan", label), fan.DesignFlowType, fan.DesignFlowRate, ceiling_Lps);
        }

        private static void ReadBackAbsoluteFlow(SystemVentilationConversionContext systemVentilationConversionContext, string label, tpdFlowRateType tpdFlowRateType, SizedFlowVariable sizedFlowVariable, double expected_Lps)
        {
            double value = sizedFlowVariable == null ? double.NaN : sizedFlowVariable.Value;
            double tolerance = System.Math.Max(SystemVentilationConversionContext.FlowRateTolerance_Absolute, SystemVentilationConversionContext.FlowRateTolerance_Relative * System.Math.Abs(expected_Lps));

            if (tpdFlowRateType != tpdFlowRateType.tpdFlowRateValue
                || sizedFlowVariable == null
                || sizedFlowVariable.Type != tpdSizedVariable.tpdSizedVariableValue
                || double.IsNaN(value)
                || System.Math.Abs(value - expected_Lps) > tolerance)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0} reads {1} ({2}) where the graph states an absolute {3} l/s.", label, value, tpdFlowRateType, expected_Lps));
            }
        }

        /// <summary>
        /// Null where a table read back from TAS is the expected one - same variables in any column order,
        /// same multiplier, extrapolation off, every cell - and otherwise why not.
        /// </summary>
        public static string TableRefusal(TableModifier tableModifier_Expected, TableModifier tableModifier_Native)
        {
            if (tableModifier_Expected == null || tableModifier_Native == null)
            {
                return "no table read back";
            }

            if (tableModifier_Native.Extrapolate)
            {
                return "extrapolation is on";
            }

            if (tableModifier_Native.ArithmeticOperator != tableModifier_Expected.ArithmeticOperator)
            {
                return string.Format("the multiplier is {0}", tableModifier_Native.ArithmeticOperator);
            }

            List<string> headers_Expected = new List<string>(tableModifier_Expected.Headers);
            List<string> headers_Native = new List<string>(tableModifier_Native.Headers);

            if (headers_Expected.Count != headers_Native.Count)
            {
                return string.Format("{0} column(s) where {1} were written", headers_Native.Count, headers_Expected.Count);
            }

            //Native axis columns, by the variable each states - so a table read back in any column order still compares cell for cell.
            int count_Axis = headers_Expected.Count - 1;
            int[] columns = new int[count_Axis];

            for (int axis = 0; axis < count_Axis; axis++)
            {
                if (!TryGetVariableType(headers_Expected[axis], out tpdProfileDataVariableType variableType_Expected))
                {
                    return string.Format("the written axis '{0}' is not a TAS variable", headers_Expected[axis]);
                }

                columns[axis] = -1;
                for (int column = 0; column < count_Axis; column++)
                {
                    if (TryGetVariableType(headers_Native[column], out tpdProfileDataVariableType variableType_Native) && variableType_Native == variableType_Expected)
                    {
                        columns[axis] = column;
                    }
                }

                if (columns[axis] < 0)
                {
                    return string.Format("no native axis states {0}", variableType_Expected);
                }
            }

            if (tableModifier_Native.RowCount != tableModifier_Expected.RowCount)
            {
                return string.Format("{0} cell(s) where {1} were written", tableModifier_Native.RowCount, tableModifier_Expected.RowCount);
            }

            Dictionary<string, double> values_Native = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int row = 0; row < tableModifier_Native.RowCount; row++)
            {
                Dictionary<int, double> values = tableModifier_Native.GetDictionary(row);
                string[] key = new string[count_Axis];
                for (int axis = 0; axis < count_Axis; axis++)
                {
                    key[axis] = Key(values[columns[axis]]);
                }

                values_Native[string.Join("|", key)] = values[count_Axis];
            }

            for (int row = 0; row < tableModifier_Expected.RowCount; row++)
            {
                Dictionary<int, double> values = tableModifier_Expected.GetDictionary(row);
                string[] key = new string[count_Axis];
                for (int axis = 0; axis < count_Axis; axis++)
                {
                    key[axis] = Key(values[axis]);
                }

                //Single precision natively: a published 0.1 K value is exact to far better than 1e-4 K.
                if (!values_Native.TryGetValue(string.Join("|", key), out double value) || System.Math.Abs(value - values[count_Axis]) > 1e-4)
                {
                    return string.Format("the cell at ({0}) reads {1} where {2} was written", string.Join(", ", key), values_Native.ContainsKey(string.Join("|", key)) ? value.ToString() : "nothing", values[count_Axis]);
                }
            }

            return null;
        }

        private static string Key(double value)
        {
            //Axis values are published to a few decimals and stored single precision natively.
            return System.Math.Round(value, 4).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
