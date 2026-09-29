// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>Proportional band [K] of the cooling-stat: off at the activation temperature, full 0.1 K above it.</summary>
        public const double GuidanceCoolingStatBand_K = 0.1;

        /// <summary>
        /// SAM#123: grounds one air system's selected product operated to its manufacturer's guidance, in
        /// native TAS - the recipe the Stage 11 prototype proved (2026-09-24, PASS) - and reads every written
        /// value back, refusing on any disagreement.
        /// <list type="bullet">
        /// <item><description><b>Cooling-stat.</b> A normal temperature controller sensing the stat room's
        /// <c>SystemZone</c> (<c>SensorArc1</c> assigned), proportional over
        /// <see cref="GuidanceCoolingStatBand_K"/> above the activation temperature, drives the supply DX
        /// coil.</description></item>
        /// <item><description><b>Elevated airflow.</b> Two more controllers on the same sensor and band drive the
        /// variable-speed supply and extract fans (design = the elevated total) with a minimum signal of
        /// (design / elevated)^2 - a controlled fan's airflow goes as the square root of its signal - so the unit
        /// moves its design airflow with the stat satisfied and the elevated airflow calling. Every damper states
        /// its elevated share as an absolute value and stays uncontrolled, so the airflow keeps the design
        /// proportions: controlling the dampers instead does not converge where a supplied room's only outlet is
        /// a transfer (Stage 11b).</description></item>
        /// <item><description><b>Exchanger.</b> Uncontrolled - a control arc would scale its efficiency by the
        /// signal. Efficiency = an equality table over (intake, extract, own airflow): the unit's own bypass
        /// decision at both airflows (0 where its conditions hold - independent of the cooling-stat), otherwise
        /// the background recovery fraction at the design airflow and the cooling rule's fraction at the
        /// elevated airflow. The exchanger's own airflow carries the controller signal because TAS refuses a
        /// <c>ControlSignal</c> axis on <c>SensibleEfficiency</c>.</description></item>
        /// <item><description><b>DX coil.</b> No enable gates, and <c>MinimumOffcoil</c> = a table over the
        /// coil's own entering temperature: <c>max(minimum, entering - (coil drop - fan rise)(elevated))</c>,
        /// which a controlled coil holds as its leaving temperature. The fans' own heat stays cleared - the
        /// stated fan rise is carried in the net drop instead. The finite cooling duty TAS requires is
        /// <see cref="GuidanceCoolingDuty_W"/>, a numerical value large enough that the law, not the duty, sets the
        /// leaving temperature whenever the stat calls. No product capacity figure is used: none is published as
        /// a DX total duty.</description></item>
        /// </list>
        /// <para>
        /// <b>Manufacturer guidance, not certified performance.</b> Nothing here is written as a certified
        /// efficiency, specific fan power or capacity.
        /// </para>
        /// </summary>
        /// <returns>True where the unit is grounded, or where the air system carries none.</returns>
        public static bool GroundGuidanceCooling(
            SystemVentilationConversionContext systemVentilationConversionContext,
            Guid guid_AirSystem,
            global::TPD.System system,
            Dictionary<Guid, ISystemComponent> dictionary_SystemComponent)
        {
            if (systemVentilationConversionContext == null)
            {
                return false;
            }

            MechanicalVentilationGuidanceCooling guidanceCooling = systemVentilationConversionContext.GuidanceCooling(guid_AirSystem);
            if (guidanceCooling == null)
            {
                return true;
            }

            string label = string.Format("Manufacturer-guidance cooling of air system {0}", guid_AirSystem);

            if (system == null || dictionary_SystemComponent == null)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0} could not be grounded: no native system.", label));
                return false;
            }

            //---------------------------------------------------------------------------------------------
            //The strategy: only the carriers the prototype proved are accepted.
            //---------------------------------------------------------------------------------------------

            if (!TryGetGuidanceRecipe(guidanceCooling, out GuidanceRecipe recipe, out string refusal_Recipe))
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: {1}", label, refusal_Recipe));
                return false;
            }

            //---------------------------------------------------------------------------------------------
            //The native objects, by identity.
            //---------------------------------------------------------------------------------------------

            if (!(Native(dictionary_SystemComponent, guidanceCooling.Guid_Exchanger) is Exchanger exchanger)
                || !(Native(dictionary_SystemComponent, guidanceCooling.Guid_DXCoil) is DXCoil dXCoil)
                || !(Native(dictionary_SystemComponent, guidanceCooling.Guid_Fan_Supply) is global::TPD.Fan fan_Supply)
                || !(Native(dictionary_SystemComponent, guidanceCooling.Guid_Fan_Extract) is global::TPD.Fan fan_Extract))
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the exchanger, supply DX coil or a fan produced no native component.", label));
                return false;
            }

            MechanicalVentilationGuidanceRoom room_Stat = guidanceCooling.Rooms.Find(x => x.Guid_Space == guidanceCooling.Guid_Space_Stat);
            if (room_Stat == null || !(Native(dictionary_SystemComponent, room_Stat.Guid_SystemSpace) is SystemZone systemZone_Stat))
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the cooling-stat's room produced no native zone to sense.", label));
                return false;
            }

            //The one supply damper every supplied zone hangs off (supply duty lives on the zones).
            Dictionary<string, Damper> dampers_Supply = new Dictionary<string, Damper>();
            foreach (MechanicalVentilationGuidanceRoom room in guidanceCooling.Rooms)
            {
                if (room.DesignSupply_Lps <= 0)
                {
                    continue;
                }

                if (!(Native(dictionary_SystemComponent, room.Guid_SystemSpace) is SystemZone systemZone))
                {
                    systemVentilationConversionContext.Refuse(string.Format("{0}: supplied room {1} produced no native zone.", label, room.Guid_Space));
                    return false;
                }

                foreach (Damper damper in UpstreamDampers((ISystemComponent)systemZone))
                {
                    dampers_Supply[Query.NativeReference(damper)] = damper;
                }
            }

            if (dampers_Supply.Count != 1)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: the supplied zones hang off {1} supply damper(s); the elevated supply is carried by exactly one shared supply damper.", label, dampers_Supply.Count));
                return false;
            }

            Damper damper_Supply = new List<Damper>(dampers_Supply.Values)[0];

            //Every extract and transfer duty carrier of this air system, at its design duty.
            List<Damper> dampers_Extract = new List<Damper>();
            List<double> designs_Extract = new List<double>();
            double design_ExtractLegs_Lps = 0;

            foreach (SystemVentilationLegIntent legIntent in systemVentilationConversionContext.LegIntents_AirSystem(guid_AirSystem) ?? new List<SystemVentilationLegIntent>())
            {
                if (legIntent.ConnectionType != SystemVentilationConnectionType.Extract && legIntent.ConnectionType != SystemVentilationConnectionType.Transfer)
                {
                    continue;
                }

                if (!(Native(dictionary_SystemComponent, legIntent.Guid_DutyCarrier) is Damper damper))
                {
                    systemVentilationConversionContext.Refuse(string.Format("{0}: the {1} leg {2} has no native duty-carrier damper.", label, legIntent.ConnectionType, legIntent.Guid_SystemConnection));
                    return false;
                }

                dampers_Extract.Add(damper);
                designs_Extract.Add(legIntent.DesignFlowRate_Lps);

                if (legIntent.ConnectionType == SystemVentilationConnectionType.Extract)
                {
                    design_ExtractLegs_Lps += legIntent.DesignFlowRate_Lps;
                }
            }

            if (dampers_Extract.Count == 0 || System.Math.Abs(design_ExtractLegs_Lps - recipe.DesignExtract_Lps) > System.Math.Max(SystemVentilationConversionContext.FlowRateTolerance_Absolute, SystemVentilationConversionContext.FlowRateTolerance_Relative * recipe.DesignExtract_Lps))
            {
                systemVentilationConversionContext.Refuse(string.Format(CultureInfo.InvariantCulture, "{0}: the extract duty carriers state {1:0.###} l/s where the unit's design extract is {2:0.###} l/s.", label, design_ExtractLegs_Lps, recipe.DesignExtract_Lps));
                return false;
            }

            //---------------------------------------------------------------------------------------------
            //Write.
            //---------------------------------------------------------------------------------------------

            int count_Refusal = systemVentilationConversionContext.Refusals.Count;

            try
            {
                //Fans: variable speed, following their dampers, the elevated total as design.
                foreach (global::TPD.Fan fan in new[] { fan_Supply, fan_Extract })
                {
                    fan.ControlType = tpdFanControlType.tpdFanControlVariableSpeed;
                    fan.DesignFlowType = tpdFlowRateType.tpdFlowRateValue;
                    fan.DesignFlowRate.Type = tpdSizedVariable.tpdSizedVariableValue;
                    ((dynamic)fan.DesignFlowRate).Value = recipe.Elevated_Lps;
                }

                //Dampers: the elevated flow as an absolute value.
                WriteAbsoluteFlow(damper_Supply, recipe.Elevated_Lps);
                for (int i = 0; i < dampers_Extract.Count; i++)
                {
                    WriteAbsoluteFlow(dampers_Extract[i], designs_Extract[i] * recipe.Elevated_Lps / recipe.DesignExtract_Lps);
                }

                //Exchanger: uncontrolled, efficiency x state table.
                exchanger.SetpointMethod = tpdSetpointMethod.tpdSetpointNone;
                dynamic sensibleEfficiency = exchanger.SensibleEfficiency;
                sensibleEfficiency.ClearModifiers();
                sensibleEfficiency.Value = recipe.ExtractFraction;
                dynamic latentEfficiency = exchanger.LatentEfficiency;
                latentEfficiency.ClearModifiers();
                latentEfficiency.Value = 0.0;
                using (TPDProfiler.Current?.Measure("Guidance: write exchanger state table"))
                {
                    WriteExchangerStateTable(sensibleEfficiency.AddModifierTable(), recipe);
                }

                TPDProfiler.Current?.Count("Guidance: exchanger state table cells", 2L * recipe.Intakes_C.Length * recipe.Extracts_C.Length);

                //DX coil: finite duty, no gates, supply law on the off-coil floor.
                dXCoil.ControlMethod = tpdCoolingControlMethod.tpdCoolingControlNormal;
                dXCoil.CoolingDuty.Type = tpdSizedVariable.tpdSizedVariableValue;
                ((dynamic)dXCoil.CoolingDuty).Value = recipe.CoolingDuty_W;
                dXCoil.HeatingDuty.Type = tpdSizedVariable.tpdSizedVariableValue;
                ((dynamic)dXCoil.HeatingDuty).Value = 0.0;

                dynamic coolingSetpoint = dXCoil.CoolingSetpoint;
                coolingSetpoint.ClearModifiers();
                coolingSetpoint.Value = GuidanceCoolingInertSetpoint_C;

                dynamic heatingSetpoint = dXCoil.HeatingSetpoint;
                heatingSetpoint.ClearModifiers();
                heatingSetpoint.Value = GuidanceCoolingInertSetpoint_C;

                dynamic minimumOffcoil = dXCoil.MinimumOffcoil;
                minimumOffcoil.ClearModifiers();
                minimumOffcoil.Value = 0.0;
                using (TPDProfiler.Current?.Measure("Guidance: write coil supply-law table"))
                {
                    WriteSupplyLawTable(minimumOffcoil.AddModifierTable(), recipe);
                }

                //Controllers, all on the stat room's zone, all day types.
                List<PlantDayType> plantDayTypes = new List<PlantDayType>();
                PlantCalendar plantCalendar = system.GetPlantRoom()?.GetEnergyCentre()?.GetCalendar();
                if (plantCalendar != null)
                {
                    for (int i = 1; i <= plantCalendar.GetDayTypeCount(); i++)
                    {
                        PlantDayType plantDayType = plantCalendar.GetDayType(i);
                        if (plantDayType != null)
                        {
                            plantDayTypes.Add(plantDayType);
                        }
                    }
                }

                if (plantDayTypes.Count == 0)
                {
                    systemVentilationConversionContext.Refuse(string.Format("{0}: the plant calendar has no day types for the controllers.", label));
                    return false;
                }

                using (TPDProfiler.Current?.Measure("Guidance: add controllers"))
                {
                    AddStatController(system, systemZone_Stat, recipe, 0.0, plantDayTypes, "Manufacturer guidance cooling-stat (room) - DX", new ISystemComponent[] { (ISystemComponent)dXCoil });
                    AddStatController(system, systemZone_Stat, recipe, FanSignal(recipe.DesignSupply_Lps, recipe.Elevated_Lps), plantDayTypes, "Manufacturer guidance elevated supply fan (room)", new ISystemComponent[] { (ISystemComponent)fan_Supply });
                    AddStatController(system, systemZone_Stat, recipe, FanSignal(recipe.DesignExtract_Lps, recipe.Elevated_Lps), plantDayTypes, "Manufacturer guidance elevated extract fan (room)", new ISystemComponent[] { (ISystemComponent)fan_Extract });
                }
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: writing it threw {1}: {2}.", label, exception.GetType().Name, exception.Message));
                return false;
            }

            //---------------------------------------------------------------------------------------------
            //Read back. Everything written, off the native objects.
            //---------------------------------------------------------------------------------------------

            List<string> disagreements = new List<string>();

            foreach (global::TPD.Fan fan in new[] { fan_Supply, fan_Extract })
            {
                if (fan.ControlType != tpdFanControlType.tpdFanControlVariableSpeed || !IsAbsolute(fan.DesignFlowType, fan.DesignFlowRate, recipe.Elevated_Lps))
                {
                    disagreements.Add(string.Format("fan {0} is not variable speed at {1} l/s", Query.NativeReference(fan), recipe.Elevated_Lps));
                }
            }

            if (!IsAbsolute(damper_Supply.DesignFlowType, damper_Supply.DesignFlowRate, recipe.Elevated_Lps))
            {
                disagreements.Add("the supply damper does not state the elevated supply");
            }

            for (int i = 0; i < dampers_Extract.Count; i++)
            {
                if (!IsAbsolute(dampers_Extract[i].DesignFlowType, dampers_Extract[i].DesignFlowRate, designs_Extract[i] * recipe.Elevated_Lps / recipe.DesignExtract_Lps))
                {
                    disagreements.Add(string.Format("extract/transfer damper {0} does not state its elevated share", Query.NativeReference(dampers_Extract[i])));
                }
            }

            if (exchanger.SetpointMethod != tpdSetpointMethod.tpdSetpointNone)
            {
                disagreements.Add("the exchanger still states a setpoint");
            }
            else
            {
                using (TPDProfiler.Current?.Measure("Guidance: read back exchanger state table"))
                {
                    if (!ReadBackExchangerStateTable(exchanger, recipe, out string exchangerDisagreement))
                    {
                        disagreements.Add("the exchanger " + exchangerDisagreement);
                    }
                }
            }

            using (TPDProfiler.Current?.Measure("Guidance: read back coil"))
            {
                if (!ReadBackCoil(dXCoil, recipe, out string coilDisagreement))
                {
                    disagreements.Add("the DX coil " + coilDisagreement);
                }
            }

            using (TPDProfiler.Current?.Measure("Guidance: read back controllers"))
            {
                if (!ReadBackControllers(system, systemZone_Stat, recipe, dXCoil, fan_Supply, fan_Extract, dampers_Extract, damper_Supply, out string controllerDisagreement))
                {
                    disagreements.Add(controllerDisagreement);
                }
            }

            if (disagreements.Count != 0)
            {
                systemVentilationConversionContext.Refuse(string.Format("{0}: TAS does not hold what was written - {1}.", label, string.Join("; ", disagreements)));
                return false;
            }

            if (systemVentilationConversionContext.Refusals.Count != count_Refusal)
            {
                return false;
            }

            systemVentilationConversionContext.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0} grounded (MANUFACTURER GUIDANCE, not certified performance): cooling-stat in zone {1} at {2:0.###} C (+{3:0.###} K band); supply {4:0.###} -> {5:0.###} l/s and extract {6:0.###} -> {5:0.###} l/s while cooling ({7} extract/transfer damper(s)); exchanger bypass (intake >= {11:0.###} C, extract > intake and >= {12:0.###} C) else recovery {8:0.###} at design / {13:0.####} at {5:0.###} l/s; DX supply = coil entering - {9:0.###} K{14}, whenever the stat calls (numerical duty {10:0} W, not a rating); read back.",
                label,
                Query.NativeReference(systemZone_Stat),
                recipe.ActivationTemperature_C,
                GuidanceCoolingStatBand_K,
                recipe.DesignSupply_Lps,
                recipe.Elevated_Lps,
                recipe.DesignExtract_Lps,
                dampers_Extract.Count,
                recipe.ExtractFraction,
                recipe.CoilNetDrop_K,
                recipe.CoolingDuty_W,
                recipe.BypassMinimumIntake_C,
                recipe.BypassMinimumExtract_C,
                recipe.CoolingExtractFraction,
                double.IsNaN(recipe.MinimumSupply_C) ? " (no minimum stated)" : string.Format(CultureInfo.InvariantCulture, ", not below {0:0.###} C", recipe.MinimumSupply_C)));

            return true;
        }

        /// <summary>An inert coil setpoint [&#176;C]: a controlled coil ignores it, and it can never gate cooling.</summary>
        public const double GuidanceCoolingInertSetpoint_C = -100.0;

        /// <summary>
        /// The DX coil's cooling duty [W] - a numerical value, <b>not a rating and not a constraint</b>. TAS needs a
        /// finite duty for a controlled coil, and a controlled coil delivers its signal times that duty until the
        /// off-coil law stops it. A duty this large makes any stat signal reach the stated law, which is the
        /// manufacturer's on/off cooling at the room stat; the product's published 2.2 kW is a combined coolth
        /// recovery + sensible figure, not a DX total duty, and used here it made the coil modulate proportionally in
        /// part-signal hours (Stage 13: 424 of 794 July-August part-flow hours short of the law).
        /// </summary>
        public const double GuidanceCoolingDuty_W = 100000.0;

        /// <summary>Everything the grounding writes, resolved and checked once from the unit's strategy.</summary>
        public class GuidanceRecipe
        {
            public double ActivationTemperature_C;
            public double BypassMinimumIntake_C;
            public double BypassMinimumExtract_C;

            /// <summary>The background (design-airflow) recovery fraction.</summary>
            public double ExtractFraction;

            /// <summary>The exchanger's recovery fraction at the elevated airflow, from the cooling rule.</summary>
            public double CoolingExtractFraction;

            /// <summary>What the coil takes off the air at the elevated airflow [K]: its drop less the fan motor heat's rise.</summary>
            public double CoilNetDrop_K;

            /// <summary>The lowest temperature the coil delivers [&#176;C], or NaN where the rule states none.</summary>
            public double MinimumSupply_C;

            public double Elevated_Lps;
            public double DesignSupply_Lps;
            public double DesignExtract_Lps;
            public double CoolingDuty_W;
            public double[] Intakes_C;
            public double[] Extracts_C;

            /// <summary>
            /// The unit's own bypass decision - the same at every airflow, independent of the cooling-stat, inclusive at
            /// both minimums and strict on extract above intake, exactly as
            /// <see cref="VentilationUnitOperatingStrategy.ExchangerBypassed"/> states it.
            /// </summary>
            public bool Bypass(double intake_C, double extract_C)
            {
                return intake_C >= BypassMinimumIntake_C && extract_C > intake_C && extract_C >= BypassMinimumExtract_C;
            }

            /// <summary>The background (design-airflow) exchanger state: 0 in bypass, the recovery fraction otherwise.</summary>
            public double BackgroundEfficiency(double intake_C, double extract_C)
            {
                return Bypass(intake_C, extract_C) ? 0.0 : ExtractFraction;
            }

            /// <summary>
            /// The cooling (elevated-airflow) exchanger state: 0 in bypass, otherwise heat/coolth recovery at the
            /// fraction the cooling rule states for the elevated airflow.
            /// </summary>
            public double CoolingEfficiency(double intake_C, double extract_C)
            {
                return Bypass(intake_C, extract_C) ? 0.0 : CoolingExtractFraction;
            }

            /// <summary>
            /// The coil's leaving-temperature floor for an entering temperature: the entering temperature less the net
            /// drop, never below the stated minimum. A coil only cools, so where the entering air is already below the
            /// minimum TAS simply passes it through (the heating duty is zero).
            /// </summary>
            public double SupplyLaw_C(double entering_C)
            {
                double result = entering_C - CoilNetDrop_K;
                return double.IsNaN(MinimumSupply_C) ? result : System.Math.Max(result, MinimumSupply_C);
            }

            /// <summary>The coil entering-temperature breakpoints of the supply-law table, with the floor's kink on the grid.</summary>
            public double[] SupplyLawEntering_C
            {
                get
                {
                    return double.IsNaN(MinimumSupply_C)
                        ? new double[] { SupplyLawBound_C[0], SupplyLawBound_C[1] }
                        : new double[] { SupplyLawBound_C[0], MinimumSupply_C + CoilNetDrop_K, SupplyLawBound_C[1] };
                }
            }
        }

        /// <summary>
        /// The coil entering temperatures [&#176;C] the supply-law table spans; TAS never extrapolates beyond them. The law
        /// is linear above the floor's kink, so the upper bound is set well above any coil inlet the exchanger table can
        /// produce (its extract axis runs to 100 &#176;C) rather than holding the target at the edge.
        /// </summary>
        private static readonly double[] SupplyLawBound_C = { -50.0, 150.0 };

        /// <summary>
        /// Resolves and checks the unit's strategy into what the grounding writes, or says why it cannot be.
        /// </summary>
        public static bool TryGetGuidanceRecipe(MechanicalVentilationGuidanceCooling guidanceCooling, out GuidanceRecipe recipe, out string refusal)
        {
            recipe = null;
            refusal = null;

            VentilationUnitOperatingStrategy strategy = guidanceCooling?.Settings?.OperatingStrategy;
            if (strategy == null || strategy.Refusal() != null)
            {
                refusal = "states no usable operating strategy" + (strategy?.Refusal() == null ? "." : ": " + strategy.Refusal());
                return false;
            }

            if (strategy.CoolingActivationSignal != CoolingActivationSignal.RoomTemperature)
            {
                refusal = "switches cooling on the extract; only a room cooling-stat has been proven in native TAS.";
                return false;
            }

            if (strategy.SummerBypassSupplyTemperatureRule?.SupplyTemperatureRuleType != SupplyTemperatureRuleType.OutdoorAir
                || strategy.HeatCoolthRecoverySupplyTemperatureRule?.SupplyTemperatureRuleType != SupplyTemperatureRuleType.LinearBlend
                || strategy.CoolingSupplyTemperatureRule?.SupplyTemperatureRuleType != SupplyTemperatureRuleType.ExchangerThenCoil)
            {
                refusal = "states supply rules other than intake air (bypass), a linear blend (recovery) and an exchanger then coil (cooling) - the only carriers proven in native TAS.";
                return false;
            }

            double extractFraction = strategy.HeatCoolthRecoverySupplyTemperatureRule.ExtractFraction;
            if (!(extractFraction > 0 && extractFraction <= 1))
            {
                refusal = string.Format(CultureInfo.InvariantCulture, "states a recovery blend fraction of {0}; the exchanger carries a fraction in (0, 1].", extractFraction);
                return false;
            }

            SupplyTemperatureRule rule_Cooling = strategy.CoolingSupplyTemperatureRule;
            double elevated_Lps = strategy.ElevatedAirFlow_Lps;
            double coolingExtractFraction = rule_Cooling.ExchangerExtractFraction(elevated_Lps);
            double coilNetDrop_K = rule_Cooling.CoilNetTemperatureDrop_K(elevated_Lps);
            if (!(coolingExtractFraction >= 0 && coolingExtractFraction <= 1) || double.IsNaN(coilNetDrop_K) || double.IsInfinity(coilNetDrop_K))
            {
                refusal = string.Format(CultureInfo.InvariantCulture, "states no exchanger and coil figures at the elevated airflow of {0} l/s ({1})", elevated_Lps, rule_Cooling.AirFlowDomainCondition(elevated_Lps) ?? "the rule refuses");
                return false;
            }

            //The coil carries no heating duty, so a law that asks it to heat could not be held natively. The rule's own
            //refusal already prevents this at every stated airflow; this guards the grounding on its own terms.
            if (coilNetDrop_K < 0)
            {
                refusal = string.Format(CultureInfo.InvariantCulture, "states a net coil drop of {0} K at {1} l/s; the coil carries no heating duty, so it cannot deliver a fan rise larger than its drop.", coilNetDrop_K, elevated_Lps);
                return false;
            }

            //Stage 12 (2026-09-24) proved the floor natively as a kink in the off-coil table over the coil's entering
            //temperature; any finite floor below the table's upper bound is carried the same way.
            double minimumSupply_C = rule_Cooling.MinimumSupplyTemperature_C;
            if (!double.IsNaN(minimumSupply_C) && !(minimumSupply_C + coilNetDrop_K > SupplyLawBound_C[0] && minimumSupply_C + coilNetDrop_K < SupplyLawBound_C[1]))
            {
                refusal = string.Format(CultureInfo.InvariantCulture, "states a minimum cooling supply temperature of {0} degC, outside what the off-coil table spans.", minimumSupply_C);
                return false;
            }

            double designSupply_Lps = guidanceCooling.DesignSupply_Lps;
            double designExtract_Lps = guidanceCooling.DesignExtract_Lps;
            if (!(designSupply_Lps > 0) || !(designExtract_Lps > 0) || !(elevated_Lps > designSupply_Lps) || !(elevated_Lps > designExtract_Lps))
            {
                refusal = string.Format(CultureInfo.InvariantCulture, "states an elevated airflow of {0} l/s that is not above the design supply {1} l/s and extract {2} l/s, so nothing is elevated while cooling.", elevated_Lps, designSupply_Lps, designExtract_Lps);
                return false;
            }

            recipe = new GuidanceRecipe
            {
                ActivationTemperature_C = strategy.CoolingActivationTemperature_C,
                BypassMinimumIntake_C = strategy.BypassMinimumIntakeTemperature_C,
                BypassMinimumExtract_C = strategy.BypassMinimumExtractTemperature_C,
                ExtractFraction = extractFraction,
                CoolingExtractFraction = coolingExtractFraction,
                CoilNetDrop_K = coilNetDrop_K,
                MinimumSupply_C = minimumSupply_C,
                Elevated_Lps = elevated_Lps,
                DesignSupply_Lps = designSupply_Lps,
                DesignExtract_Lps = designExtract_Lps,
                CoolingDuty_W = GuidanceCoolingDuty_W,
            };

            //Breakpoints at 0.1 K through the thresholds up to 45 C on both axes, with each (inclusive) bypass minimum on
            //the grid and a 0.01 K step just below it, so neither switch smears (Stage 7 / Stage 11). The bypass diagonal
            //(extract = intake) smears over at most 0.1 K anywhere up to 45 C; coarser breakpoints above 35 C left 17
            //heatwave hours partly recovering on the MG run (Stage 13). Beyond 45 C the extract axis takes one more 0.1 K
            //step (so the diagonal smear stays 0.1 K at the last intake step) and then continues coarsely:
            //for any intake below 45 C every such cell is on the same side of the diagonal, so a hot (e.g. displacement-
            //vent) extract keeps the exact state. An intake above 45 C is held at 45 C - outside any design weather used
            //here (the DSY1 2050s peak is 40.3 C) - where an extract between 45 C and the intake would read as bypass.
            recipe.Intakes_C = Axis(new double[] { -20, -5, 5 }, System.Math.Min(recipe.BypassMinimumIntake_C, 12.0), 45.0, new double[0], new double[] { recipe.BypassMinimumIntake_C - 0.01 });
            recipe.Extracts_C = Axis(new double[] { 5, 12 }, System.Math.Min(recipe.BypassMinimumExtract_C, 18.0) - 0.1, 45.0, new double[] { 45.1, 50, 60, 80, 100 }, new double[] { recipe.BypassMinimumExtract_C - 0.01, recipe.ActivationTemperature_C + 0.01 });

            return true;
        }

        private static double[] Axis(double[] low, double from, double to, double[] high, double[] extra)
        {
            SortedSet<double> values = new SortedSet<double>();
            foreach (double value in low)
            {
                if (value < from - 1e-9)
                {
                    values.Add(value);
                }
            }

            int steps = (int)System.Math.Round((to - from) / 0.1);
            for (int i = 0; i <= steps; i++)
            {
                values.Add(System.Math.Round(from + (0.1 * i), 4));
            }

            foreach (double value in high)
            {
                values.Add(value);
            }

            foreach (double value in extra ?? new double[0])
            {
                values.Add(System.Math.Round(value, 4));
            }

            return new List<double>(values).ToArray();
        }

        /// <summary>
        /// The dampers feeding a component, walking upstream through junctions only - the route inserts a
        /// native branch junction wherever one damper feeds several zones.
        /// </summary>
        private static List<Damper> UpstreamDampers(ISystemComponent systemComponent)
        {
            List<Damper> result = new List<Damper>();
            Queue<ISystemComponent> queue = new Queue<ISystemComponent>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(systemComponent);

            while (queue.Count != 0)
            {
                foreach (Duct duct in Query.Ducts(queue.Dequeue(), Direction.In) ?? new List<Duct>())
                {
                    object upstream = duct?.GetUpstreamComponent();
                    if (upstream == null || !visited.Add(Query.NativeReference(upstream)))
                    {
                        continue;
                    }

                    if (upstream is Damper damper)
                    {
                        result.Add(damper);
                    }
                    else if (upstream is Junction junction)
                    {
                        queue.Enqueue((ISystemComponent)junction);
                    }
                }
            }

            return result;
        }

        private static void WriteAbsoluteFlow(Damper damper, double value_Lps)
        {
            damper.DesignFlowType = tpdFlowRateType.tpdFlowRateValue;
            damper.DesignFlowRate.Type = tpdSizedVariable.tpdSizedVariableValue;
            ((dynamic)damper.DesignFlowRate).Value = value_Lps;
            damper.MinimumFlowType = tpdFlowRateType.tpdFlowRateNone;
        }

        private static bool IsAbsolute(tpdFlowRateType tpdFlowRateType, SizedFlowVariable sizedFlowVariable, double expected_Lps)
        {
            if (tpdFlowRateType != tpdFlowRateType.tpdFlowRateValue || sizedFlowVariable == null || sizedFlowVariable.Type != tpdSizedVariable.tpdSizedVariableValue)
            {
                return false;
            }

            double tolerance = System.Math.Max(SystemVentilationConversionContext.FlowRateTolerance_Absolute, SystemVentilationConversionContext.FlowRateTolerance_Relative * System.Math.Abs(expected_Lps));
            return System.Math.Abs(sizedFlowVariable.Value - expected_Lps) <= tolerance;
        }

        private static void WriteExchangerStateTable(dynamic table, GuidanceRecipe recipe)
        {
            table.Name = "Manufacturer guidance exchanger state: bypass 0 at both airflows, else recovery at the design-airflow / elevated-airflow fraction";
            table.SetVariable(1, tpdProfileDataVariableType.tpdProfileDataVariableODB);
            table.SetVariable(2, tpdProfileDataVariableType.tpdProfileDataVariableEDB2);
            table.SetVariable(3, tpdProfileDataVariableType.tpdProfileDataVariableEFlow);
            table.SetSize(recipe.Intakes_C.Length, recipe.Extracts_C.Length, 2);
            table.Extrapolate = false;
            table.Multiplier = tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;

            for (int i = 0; i < recipe.Intakes_C.Length; i++)
            {
                table.SetAxisValue(1, i + 1, recipe.Intakes_C[i]);
            }

            for (int j = 0; j < recipe.Extracts_C.Length; j++)
            {
                table.SetAxisValue(2, j + 1, recipe.Extracts_C[j]);
            }

            table.SetAxisValue(3, 1, recipe.DesignSupply_Lps);
            table.SetAxisValue(3, 2, recipe.Elevated_Lps);

            for (int i = 0; i < recipe.Intakes_C.Length; i++)
            {
                for (int j = 0; j < recipe.Extracts_C.Length; j++)
                {
                    table.SetDataValue(i + 1, j + 1, 1, recipe.BackgroundEfficiency(recipe.Intakes_C[i], recipe.Extracts_C[j]));
                    table.SetDataValue(i + 1, j + 1, 2, recipe.CoolingEfficiency(recipe.Intakes_C[i], recipe.Extracts_C[j]));
                }
            }
        }

        private static bool ReadBackExchangerStateTable(Exchanger exchanger, GuidanceRecipe recipe, out string disagreement)
        {
            disagreement = null;
            dynamic sensibleEfficiency = exchanger.SensibleEfficiency;

            if (System.Math.Abs((double)sensibleEfficiency.Value - recipe.ExtractFraction) > 1e-9 || (int)sensibleEfficiency.GetModifierCount() != 1)
            {
                disagreement = "efficiency is not the blend fraction with exactly one table";
                return false;
            }

            dynamic table = sensibleEfficiency.GetModifier(1);
            if ((bool)table.Extrapolate || (int)table.GetAxisSize(1) != recipe.Intakes_C.Length || (int)table.GetAxisSize(2) != recipe.Extracts_C.Length || (int)table.GetAxisSize(3) != 2)
            {
                disagreement = "state table has the wrong size or extrapolates";
                return false;
            }

            if (System.Math.Abs((double)table.GetAxisValue(3, 1) - recipe.DesignSupply_Lps) > 1e-6 || System.Math.Abs((double)table.GetAxisValue(3, 2) - recipe.Elevated_Lps) > 1e-6)
            {
                disagreement = "state table airflow axis is not design / elevated";
                return false;
            }

            for (int i = 0; i < recipe.Intakes_C.Length; i++)
            {
                double intake_C = (double)table.GetAxisValue(1, i + 1);
                for (int j = 0; j < recipe.Extracts_C.Length; j++)
                {
                    double extract_C = (double)table.GetAxisValue(2, j + 1);
                    if (System.Math.Abs(intake_C - recipe.Intakes_C[i]) > 1e-6 || System.Math.Abs(extract_C - recipe.Extracts_C[j]) > 1e-6
                        || System.Math.Abs((double)table.GetDataValue(i + 1, j + 1, 1) - recipe.BackgroundEfficiency(intake_C, extract_C)) > 1e-9
                        || System.Math.Abs((double)table.GetDataValue(i + 1, j + 1, 2) - recipe.CoolingEfficiency(intake_C, extract_C)) > 1e-9)
                    {
                        disagreement = string.Format(CultureInfo.InvariantCulture, "state table cell ({0}, {1}) is not the stated rule", intake_C, extract_C);
                        return false;
                    }
                }
            }

            dynamic latentEfficiency = exchanger.LatentEfficiency;
            if (System.Math.Abs((double)latentEfficiency.Value) > 1e-12 || (int)latentEfficiency.GetModifierCount() != 0)
            {
                disagreement = "recovers latent heat, which the guidance rule does not state";
                return false;
            }

            return true;
        }

        /// <summary>
        /// The coil's off-coil floor as a table over its OWN entering dry bulb (<c>EDB</c>), so the coil takes the
        /// stated net drop off whatever the explicit exchanger delivers - bypass, heat or coolth recovery - and never
        /// goes below the stated minimum. Proven natively at Stage 12 (2026-09-24): exact in every full-flow hour.
        /// </summary>
        private static void WriteSupplyLawTable(dynamic table, GuidanceRecipe recipe)
        {
            table.Name = double.IsNaN(recipe.MinimumSupply_C)
                ? string.Format(CultureInfo.InvariantCulture, "Manufacturer guidance cooling supply = coil entering - {0:0.###} K (at the elevated airflow)", recipe.CoilNetDrop_K)
                : string.Format(CultureInfo.InvariantCulture, "Manufacturer guidance cooling supply = max({1:0.###}, coil entering - {0:0.###} K) (at the elevated airflow)", recipe.CoilNetDrop_K, recipe.MinimumSupply_C);
            table.SetVariable(1, tpdProfileDataVariableType.tpdProfileDataVariableEDB);

            double[] entering_C = recipe.SupplyLawEntering_C;
            table.SetSize(entering_C.Length, 0, 0);
            table.Extrapolate = false;
            table.Multiplier = tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;

            for (int i = 0; i < entering_C.Length; i++)
            {
                table.SetAxisValue(1, i + 1, entering_C[i]);
                table.SetDataValue(i + 1, 1, 1, recipe.SupplyLaw_C(entering_C[i]));
            }
        }

        private static bool ReadBackCoil(DXCoil dXCoil, GuidanceRecipe recipe, out string disagreement)
        {
            disagreement = null;

            if (dXCoil.ControlMethod != tpdCoolingControlMethod.tpdCoolingControlNormal)
            {
                disagreement = "is not under normal control";
                return false;
            }

            if (dXCoil.CoolingDuty.Type != tpdSizedVariable.tpdSizedVariableValue || System.Math.Abs(((dynamic)dXCoil.CoolingDuty).Value - recipe.CoolingDuty_W) > 1e-6)
            {
                disagreement = string.Format(CultureInfo.InvariantCulture, "cooling duty is not the stated {0:0} W", recipe.CoolingDuty_W);
                return false;
            }

            if (dXCoil.HeatingDuty.Type != tpdSizedVariable.tpdSizedVariableValue || ((dynamic)dXCoil.HeatingDuty).Value != 0.0)
            {
                disagreement = "heating duty is not an absolute zero";
                return false;
            }

            foreach (dynamic profileData in new object[] { dXCoil.CoolingSetpoint, dXCoil.HeatingSetpoint })
            {
                if ((double)profileData.Value != GuidanceCoolingInertSetpoint_C || (int)profileData.GetModifierCount() != 0)
                {
                    disagreement = "still carries a setpoint gate";
                    return false;
                }
            }

            dynamic minimumOffcoil = dXCoil.MinimumOffcoil;
            if ((int)minimumOffcoil.GetModifierCount() != 1)
            {
                disagreement = "off-coil floor carries no single supply-law table";
                return false;
            }

            dynamic table = minimumOffcoil.GetModifier(1);
            double[] entering_C = recipe.SupplyLawEntering_C;
            if ((bool)table.Extrapolate || (int)table.GetAxisSize(1) != entering_C.Length || (int)table.GetVariable(1) != (int)tpdProfileDataVariableType.tpdProfileDataVariableEDB)
            {
                disagreement = "supply-law table has the wrong size or variable, or extrapolates";
                return false;
            }

            for (int i = 0; i < entering_C.Length; i++)
            {
                if (System.Math.Abs((double)table.GetAxisValue(1, i + 1) - entering_C[i]) > 1e-9 || System.Math.Abs((double)table.GetDataValue(i + 1, 1, 1) - recipe.SupplyLaw_C(entering_C[i])) > 1e-9)
                {
                    disagreement = "supply-law table is not the coil entering temperature less the net drop, floored";
                    return false;
                }
            }

            return true;
        }

        private static void AddStatController(global::TPD.System system, SystemZone systemZone_Stat, GuidanceRecipe recipe, double minimum, List<PlantDayType> plantDayTypes, string name, ISystemComponent[] systemComponents)
        {
            Controller controller = system.AddController();
            controller.ControlType = tpdControlType.tpdControlNormal;
            controller.SensorType = tpdSensorType.tpdTempSensor;
            controller.SensorArc1 = controller.AddSensorArcToComponent((global::TPD.SystemComponent)systemZone_Stat, 1);
            controller.Setpoint = recipe.ActivationTemperature_C + (GuidanceCoolingStatBand_K / 2.0);
            controller.Band = GuidanceCoolingStatBand_K;
            controller.Gradient = 1;
            controller.Min = minimum;
            controller.Max = 1;

            ((dynamic)controller).Name = name;
            controller.Description = "SAM#123 manufacturer guidance (provisional, not certified performance): wall cooling-stat analogue on the room's air; proportional from the activation temperature over the band.";

            foreach (PlantDayType plantDayType in plantDayTypes)
            {
                controller.AddDayType(plantDayType);
            }

            foreach (ISystemComponent systemComponent in systemComponents)
            {
                controller.AddControlArc((global::TPD.SystemComponent)systemComponent);
            }
        }

        /// <summary>
        /// The fan controller's minimum signal for a design airflow: a controlled variable-speed fan's airflow goes
        /// as the square root of its signal (measured, SAM#123 Stage 11b: signal 0.7875 gave 71.0 of 80 l/s, 0.375
        /// gave 49.0), so the signal that holds the design airflow is (design / elevated)^2.
        /// </summary>
        public static double FanSignal(double design_Lps, double elevated_Lps)
        {
            double ratio = design_Lps / elevated_Lps;
            return ratio * ratio;
        }

        private static bool ReadBackControllers(global::TPD.System system, SystemZone systemZone_Stat, GuidanceRecipe recipe, DXCoil dXCoil, global::TPD.Fan fan_Supply, global::TPD.Fan fan_Extract, List<Damper> dampers_Uncontrolled, Damper damper_Supply, out string disagreement)
        {
            disagreement = null;

            string reference_Zone = Query.NativeReference(systemZone_Stat);

            Dictionary<string, double> minimum_By_Target = new Dictionary<string, double>
            {
                [Query.NativeReference(dXCoil)] = 0.0,
                [Query.NativeReference(fan_Supply)] = FanSignal(recipe.DesignSupply_Lps, recipe.Elevated_Lps),
                [Query.NativeReference(fan_Extract)] = FanSignal(recipe.DesignExtract_Lps, recipe.Elevated_Lps),
            };

            //The dampers carry the elevated design proportions uncontrolled: a controlled damper in series with
            //another (a supplied room whose only outlet is a transfer) never converges (Stage 11b).
            HashSet<string> dampers_MustBeUncontrolled = new HashSet<string> { Query.NativeReference(damper_Supply) };
            dampers_Uncontrolled.ForEach(x => dampers_MustBeUncontrolled.Add(Query.NativeReference(x)));

            HashSet<string> targets_Found = new HashSet<string>();

            for (int i = 1; i <= system.GetControllerCount(); i++)
            {
                Controller controller = system.GetController(i);
                if (controller == null)
                {
                    continue;
                }

                dynamic sensorArc = controller.SensorArc1;
                object component_Sensed = null;
                try
                {
                    component_Sensed = sensorArc?.GetComponent();
                }
                catch
                {
                }

                if (component_Sensed == null || Query.NativeReference(component_Sensed) != reference_Zone)
                {
                    continue;
                }

                for (int j = 1; j <= controller.GetControlArcCount(); j++)
                {
                    string reference_Target = Query.NativeReference(controller.GetControlArc(j).GetComponent());
                    if (dampers_MustBeUncontrolled.Contains(reference_Target))
                    {
                        disagreement = string.Format("damper {0} is controlled; the elevated flow is carried by the fans with the dampers uncontrolled", reference_Target);
                        return false;
                    }

                    if (!minimum_By_Target.TryGetValue(reference_Target, out double minimum))
                    {
                        continue;
                    }

                    if (controller.ControlType != tpdControlType.tpdControlNormal
                        || controller.SensorType != tpdSensorType.tpdTempSensor
                        || System.Math.Abs(controller.Setpoint - (recipe.ActivationTemperature_C + (GuidanceCoolingStatBand_K / 2.0))) > 1e-6
                        || System.Math.Abs(controller.Band - GuidanceCoolingStatBand_K) > 1e-6
                        || System.Math.Abs(controller.Min - minimum) > 1e-6
                        || System.Math.Abs(controller.Max - 1.0) > 1e-9
                        || controller.GetDayTypeCount() < 1)
                    {
                        disagreement = string.Format(CultureInfo.InvariantCulture, "the controller on {0} does not read back as the stated cooling-stat (setpoint {1}, band {2}, min {3})", reference_Target, controller.Setpoint, controller.Band, controller.Min);
                        return false;
                    }

                    targets_Found.Add(reference_Target);
                }
            }

            if (targets_Found.Count != minimum_By_Target.Count)
            {
                disagreement = string.Format("only {0} of {1} controlled components are driven by the stat room's controllers", targets_Found.Count, minimum_By_Target.Count);
                return false;
            }

            return true;
        }
    }
}
