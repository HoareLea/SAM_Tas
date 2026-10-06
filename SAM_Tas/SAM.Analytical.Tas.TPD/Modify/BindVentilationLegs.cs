// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// Writes each leg's design airflow onto its native carrier, reads back what TAS then holds, and
        /// records one <see cref="SystemVentilationConnectionBinding"/> per PR1 connection.
        /// <para>
        /// <b>The two paths differ, and the difference matters when reading the reconciliation.</b>
        /// </para>
        /// <list type="bullet">
        /// <item><description>A <b>supply</b> leg is a genuine read-back: the duty was written at the
        /// zone pairing point in <c>Convert.ToTPD(DisplaySystemSpace, …)</c>, and what is reported here
        /// is what TAS holds now. Intent and state are two independent
        /// statements.</description></item>
        /// <item><description>An <b>extract</b> or <b>transfer</b> leg is <b>written here</b>. This is
        /// the authoritative write for the damper's absolute duty and for both of its type switches,
        /// and the check that follows is a post-write read of TAS's own storage - it catches a native
        /// object that <i>declined</i> what was just written, not a conversion that never wrote. The
        /// reconciliation's independent statement for these legs is the acceptance's separate walk of
        /// the saved document, not this method.</description></item>
        /// </list>
        /// <para>
        /// The write is deliberate rather than an assertion because
        /// <c>Modify.Update(SizedFlowVariable, SizedFlowValue, EnergyCentre)</c> writes <c>Type</c> and
        /// <c>Method</c> only for a <c>DesignConditionSizedFlowValue</c>. A plain <c>SizedFlowValue</c>
        /// reaching <c>Convert.ToTPD(DisplaySystemDamper, …)</c> would leave TAS's defaults -
        /// <c>Type = tpdSizedVariableSize</c>, <c>Method = tpdSizeFlowACH</c> - and the litres per
        /// second would be discarded in favour of sizing the leg by air changes per hour. Setting both
        /// switches here makes that unreachable instead of merely reported.
        /// </para>
        /// <para>
        /// <b>Where each type's duty lives</b>, measured on licensed TAS and not inferred:
        /// </para>
        /// <list type="bullet">
        /// <item><description><b>Supply</b> - the room's own <c>SystemZone.FlowRate</c>, already written
        /// at the zone pairing point. Read back here so the leg reconciles against the same carrier the
        /// room does. No damper: a duct has no design flow and a supply damper would be an invention.
        /// </description></item>
        /// <item><description><b>Extract</b> and <b>transfer</b> - the in-line <c>Damper</c> PR2
        /// materialised for the leg: <c>DesignFlowRate</c> in l/s with
        /// <c>DesignFlowRate.Type = tpdSizedVariableValue</c> and
        /// <c>DesignFlowType = tpdFlowRateValue</c>. Both are checked, because either left at a
        /// derivation rule would let TAS compute a duty from a neighbouring zone instead of using the
        /// design's.</description></item>
        /// </list>
        /// <para>
        /// <b>Linear.</b> Every native object is reached by <c>System.GetComponentByGUID</c> from an
        /// identity recorded at the pairing point. Nothing walks the system per leg.
        /// </para>
        /// </summary>
        public static bool BindVentilationLegs(
            SystemVentilationConversionContext systemVentilationConversionContext,
            Guid guid_AirSystem,
            global::TPD.System system)
        {
            if (systemVentilationConversionContext == null)
            {
                return false;
            }

            if (system == null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Air system {0} produced no native TAS system, so none of its legs could be bound.",
                    guid_AirSystem));

                return false;
            }

            //Indexed by air system rather than filtered out of the whole collection: this runs once per
            //unit, and a filter would make it systems x legs.
            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents_AirSystem(guid_AirSystem))
            {
                double? designFlowRate_Lps;
                string reference_FlowController;

                if (systemVentilationLegIntent.RequiresDutyCarrier)
                {
                    if (!TryBindDamper(systemVentilationConversionContext, systemVentilationLegIntent, system, out designFlowRate_Lps, out reference_FlowController))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!TryReadZoneSupply(systemVentilationConversionContext, systemVentilationLegIntent, system, out designFlowRate_Lps))
                    {
                        continue;
                    }

                    //A supply leg's carrier is the room's zone, which the room binding already names.
                    //Nothing is invented here: a duct has no guid to report.
                    reference_FlowController = null;
                }

                systemVentilationConversionContext.Record(new SystemVentilationConnectionBinding(
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SpaceAirMovement,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    systemVentilationLegIntent.Guid_AirSystem,
                    systemVentilationLegIntent.Guid_SystemSpace_From,
                    systemVentilationLegIntent.Guid_SystemSpace_To,
                    designFlowRate_Lps.GetValueOrDefault(double.NaN),
                    reference_FlowController));
            }

            return systemVentilationConversionContext.Refusals.Count == 0;
        }

        private static bool TryBindDamper(
            SystemVentilationConversionContext systemVentilationConversionContext,
            SystemVentilationLegIntent systemVentilationLegIntent,
            global::TPD.System system,
            out double? designFlowRate_Lps,
            out string reference_FlowController)
        {
            designFlowRate_Lps = null;
            reference_FlowController = null;

            reference_FlowController = systemVentilationConversionContext.Reference(systemVentilationLegIntent.Guid_DutyCarrier);

            if (string.IsNullOrWhiteSpace(reference_FlowController))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1}: the damper materialised for it never reached TAS, so its {2} l/s has no carrier.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    systemVentilationLegIntent.DesignFlowRate_Lps));

                return false;
            }

            Damper damper;

            try
            {
                damper = system.GetComponentByGUID(reference_FlowController) as Damper;
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1}: resolving its native damper {2} threw {3}: {4}.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    reference_FlowController,
                    exception.GetType().Name,
                    exception.Message));

                return false;
            }

            if (damper == null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1}: native damper {2} does not resolve back through its own TAS system.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    reference_FlowController));

                return false;
            }

            SizedFlowVariable sizedFlowVariable = damper.DesignFlowRate;

            if (sizedFlowVariable == null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1}: native damper {2} exposes no design flow rate, so its {3} l/s was dropped.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    reference_FlowController,
                    systemVentilationLegIntent.DesignFlowRate_Lps));

                return false;
            }

            //The authoritative write for this leg's duty. The ordinary damper conversion has already
            //run from the working copy, but it only carries Type and Method for a
            //DesignConditionSizedFlowValue - so the value and both type switches are (re)written here
            //unconditionally rather than checked, and the read-back below then reports a native object
            //that declined them.
            try
            {
                sizedFlowVariable.Value = systemVentilationLegIntent.DesignFlowRate_Lps;
                sizedFlowVariable.Type = tpdSizedVariable.tpdSizedVariableValue;
                damper.DesignFlowType = tpdFlowRateType.tpdFlowRateValue;
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1}: writing {2} l/s onto native damper {3} threw {4}: {5}.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    systemVentilationLegIntent.DesignFlowRate_Lps,
                    reference_FlowController,
                    exception.GetType().Name,
                    exception.Message));

                return false;
            }

            if (damper.DesignFlowRate == null || damper.DesignFlowRate.Type != tpdSizedVariable.tpdSizedVariableValue)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1}: native damper {2} declined the absolute flow type just written to it, so TAS "
                    + "would size the leg rather than use the {3} l/s the design states.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    reference_FlowController,
                    systemVentilationLegIntent.DesignFlowRate_Lps));

                return false;
            }

            if (damper.DesignFlowType != tpdFlowRateType.tpdFlowRateValue)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "{0} leg {1}: native damper {2} declined the absolute flow type just written to it and still "
                    + "reports DesignFlowType {3}, so its duty would be derived from a neighbouring zone rather "
                    + "than taken from the design.",
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    reference_FlowController,
                    damper.DesignFlowType));

                return false;
            }

            designFlowRate_Lps = damper.DesignFlowRate.Value;

            return true;
        }

        private static bool TryReadZoneSupply(
            SystemVentilationConversionContext systemVentilationConversionContext,
            SystemVentilationLegIntent systemVentilationLegIntent,
            global::TPD.System system,
            out double? designFlowRate_Lps)
        {
            designFlowRate_Lps = null;

            string reference_SystemZone = systemVentilationConversionContext.Reference(systemVentilationLegIntent.Guid_SystemSpace_To);

            if (string.IsNullOrWhiteSpace(reference_SystemZone))
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Supply leg {0} delivers to system space {1}, which no native zone was paired with.",
                    systemVentilationLegIntent.Guid_SystemConnection,
                    systemVentilationLegIntent.Guid_SystemSpace_To));

                return false;
            }

            SystemZone systemZone;

            try
            {
                systemZone = system.GetComponentByGUID(reference_SystemZone) as SystemZone;
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Supply leg {0}: resolving native zone {1} threw {2}: {3}.",
                    systemVentilationLegIntent.Guid_SystemConnection,
                    reference_SystemZone,
                    exception.GetType().Name,
                    exception.Message));

                return false;
            }

            if (systemZone == null || systemZone.FlowRate == null)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Supply leg {0}: native zone {1} does not resolve back through its own TAS system, or exposes "
                    + "no flow carrier.",
                    systemVentilationLegIntent.Guid_SystemConnection,
                    reference_SystemZone));

                return false;
            }

            designFlowRate_Lps = systemZone.FlowRate.Value;

            return true;
        }
    }
}
