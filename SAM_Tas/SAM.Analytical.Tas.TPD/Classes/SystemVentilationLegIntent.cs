// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// What one PR1 ventilation leg is <b>intended</b> to become in TAS: which two endpoints, which
    /// duty, and which component is to carry that duty.
    /// <para>
    /// <b>Where a duty can live, measured on licensed TAS.</b> A <c>Duct</c> declares no GUID and no
    /// design-flow property at all - only <c>GetFlowRate(int hour)</c>, an hourly <i>result</i> - and a
    /// <c>Junction</c> declares no members whatsoever. So a leg's duty cannot live on the topology; it
    /// lives on a flow-controlling component placed in the leg. That is why:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Supply</b> rides on the room's own <c>SystemZone.FlowRate</c> /
    /// <c>.FreshAir</c>, which is where TAS itself puts a zone's supply duty. No damper is needed and
    /// none is invented, so <see cref="Guid_DutyCarrier"/> is <see cref="Guid.Empty"/> for a supply
    /// leg.</description></item>
    /// <item><description><b>Extract</b> and <b>transfer</b> ride on an in-line <c>Damper</c> -
    /// <c>DesignFlowRate</c> with <c>DesignFlowType = tpdFlowRateValue</c>, measured to round-trip
    /// exactly through save and reopen, including 31 l/s supply and 19 l/s extract coexisting on one
    /// room and two branching transfer legs of 11 and 7 l/s off one source.</description></item>
    /// </list>
    /// <para>
    /// <b>PR1's graph carries no extract or transfer damper</b>, so PR2 materialises one per leg into
    /// its own working copy of the graph - never into PR1's - and <see cref="Guid_DutyCarrier"/> names
    /// it. A branching transfer is therefore two legs off one source room, each owning its own damper
    /// and its own duty; the junction, where the template has one, is topology and carries nothing.
    /// </para>
    /// <para>Free of TAS COM types.</para>
    /// </summary>
    public class SystemVentilationLegIntent
    {
        public SystemVentilationLegIntent(
            SystemVentilationConnectionType connectionType,
            Guid guid_SystemConnection,
            Guid guid_AirSystem,
            Guid guid_SystemSpace_From,
            Guid guid_SystemSpace_To,
            Guid guid_SpaceAirMovement,
            double designFlowRate_Lps,
            Guid guid_DutyCarrier)
        {
            ConnectionType = connectionType;
            Guid_SystemConnection = guid_SystemConnection;
            Guid_AirSystem = guid_AirSystem;
            Guid_SystemSpace_From = guid_SystemSpace_From;
            Guid_SystemSpace_To = guid_SystemSpace_To;
            Guid_SpaceAirMovement = guid_SpaceAirMovement;
            DesignFlowRate_Lps = designFlowRate_Lps;
            Guid_DutyCarrier = guid_DutyCarrier;
        }

        /// <summary>Supply, extract or transfer. <c>Undefined</c> is always a refusal.</summary>
        public SystemVentilationConnectionType ConnectionType { get; }

        /// <summary>PR1's <c>SystemConnection</c>. This row's identity.</summary>
        public Guid Guid_SystemConnection { get; }

        /// <summary>Which air handling unit the leg belongs to.</summary>
        public Guid Guid_AirSystem { get; }

        /// <summary>
        /// The upstream endpoint as a <c>SystemSpace</c>, or <see cref="Guid.Empty"/> for a supply leg,
        /// whose air comes from the unit rather than from a room.
        /// </summary>
        public Guid Guid_SystemSpace_From { get; }

        /// <summary>
        /// The downstream endpoint as a <c>SystemSpace</c>, or <see cref="Guid.Empty"/> for an extract
        /// leg, whose air goes to the unit rather than to a room.
        /// </summary>
        public Guid Guid_SystemSpace_To { get; }

        /// <summary>
        /// The analytical <c>SpaceAirMovement</c> a transfer leg came from. <see cref="Guid.Empty"/> on
        /// supply and extract.
        /// </summary>
        public Guid Guid_SpaceAirMovement { get; }

        /// <summary>
        /// PR1's <c>SystemConnectionParameter.DesignFlowRate</c>, in l/s, verbatim.
        /// <para>
        /// <b>This is DesignAirFlow and nothing else.</b> Not <c>PartFRequiredAirFlow</c>, not a selected
        /// equipment capacity, not an operating airflow - PR1 states them separately and conflating them
        /// is the Part O invariant this route exists to keep.
        /// </para>
        /// </summary>
        public double DesignFlowRate_Lps { get; }

        /// <summary>
        /// The <c>SystemDamper</c> in PR2's working copy that is to carry this leg's duty, or
        /// <see cref="Guid.Empty"/> for a supply leg, whose duty rides on the room's zone.
        /// </summary>
        public Guid Guid_DutyCarrier { get; }

        /// <summary>Whether this leg needs a damper materialised for it.</summary>
        public bool RequiresDutyCarrier
        {
            get
            {
                return ConnectionType == SystemVentilationConnectionType.Extract
                    || ConnectionType == SystemVentilationConnectionType.Transfer;
            }
        }

        /// <summary>Whether the intent states a usable leg.</summary>
        public bool IsComplete
        {
            get
            {
                if (ConnectionType == SystemVentilationConnectionType.Undefined)
                {
                    return false;
                }

                if (Guid_SystemConnection == Guid.Empty || Guid_AirSystem == Guid.Empty)
                {
                    return false;
                }

                if (double.IsNaN(DesignFlowRate_Lps) || double.IsInfinity(DesignFlowRate_Lps) || DesignFlowRate_Lps < 0.0)
                {
                    return false;
                }

                if (RequiresDutyCarrier && Guid_DutyCarrier == Guid.Empty)
                {
                    return false;
                }

                switch (ConnectionType)
                {
                    case SystemVentilationConnectionType.Transfer:
                        return Guid_SystemSpace_From != Guid.Empty
                            && Guid_SystemSpace_To != Guid.Empty
                            && Guid_SpaceAirMovement != Guid.Empty;

                    case SystemVentilationConnectionType.Supply:
                        return Guid_SystemSpace_To != Guid.Empty && Guid_DutyCarrier == Guid.Empty;

                    case SystemVentilationConnectionType.Extract:
                        return Guid_SystemSpace_From != Guid.Empty;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// A copy of this intent naming the duty carrier that has now been materialised for it. Intents
        /// are immutable, so the working-copy materialisation replaces rather than mutates.
        /// </summary>
        public SystemVentilationLegIntent WithDutyCarrier(Guid guid_DutyCarrier)
        {
            return new SystemVentilationLegIntent(
                ConnectionType,
                Guid_SystemConnection,
                Guid_AirSystem,
                Guid_SystemSpace_From,
                Guid_SystemSpace_To,
                Guid_SpaceAirMovement,
                DesignFlowRate_Lps,
                guid_DutyCarrier);
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1} l/s: {2} -> {3} (connection {4}, carrier {5})",
                ConnectionType,
                DesignFlowRate_Lps,
                Guid_SystemSpace_From == Guid.Empty ? "AHU" : Guid_SystemSpace_From.ToString(),
                Guid_SystemSpace_To == Guid.Empty ? "AHU" : Guid_SystemSpace_To.ToString(),
                Guid_SystemConnection,
                Guid_DutyCarrier == Guid.Empty ? "zone" : Guid_DutyCarrier.ToString());
        }
    }
}
