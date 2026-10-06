// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// One leg's lineage: which leg, between which rooms, carrying which design flow, and what native
    /// evidence exists for it.
    /// <para>
    /// <b>Why this is separate from <see cref="SystemVentilationBinding"/>.</b> A room binding answers
    /// "which TAS zone is this room, and what is its result". A connection binding answers "which leg
    /// carries what flow, between which two rooms". Collapsing them would lose PR1's
    /// <c>SpaceAirMovement.Guid -> TransferConnection.Guid</c> lineage - which is exactly what proves
    /// transfer-only and branching-transfer topology, and what PR4 needs in order to present it without
    /// re-reading the TPD.
    /// </para>
    /// <para>
    /// <b>The native carrier, measured on licensed TAS.</b> A leg's duty does not live on the duct: a
    /// TPD <c>Duct</c> declares no GUID and no design-flow property at all, only hourly result
    /// accessors, and a <c>Junction</c> declares no members whatsoever. An absolute per-leg duty lives on
    /// an in-line <c>Damper</c> - <c>DesignFlowRate</c> with <c>DesignFlowType = tpdFlowRateValue</c> -
    /// which was authored, saved, reopened and read back exactly, including 31 l/s supply and 19 l/s
    /// extract coexisting on one room, and two branching transfer legs of 11 and 7 l/s off one source.
    /// </para>
    /// <para>
    /// <see cref="Reference_FlowController"/> is therefore the damper, when one was created. It is never
    /// invented: a duct guid does not exist, and a null here is honest - PR1's connection identity plus
    /// the two explicit endpoints already state the topology.
    /// </para>
    /// </summary>
    public class SystemVentilationConnectionBinding
    {
        /// <summary>
        /// Creates a leg binding. As with the room binding, nothing is inferred.
        /// </summary>
        public SystemVentilationConnectionBinding(
            SystemVentilationConnectionType connectionType,
            Guid guid_SpaceAirMovement,
            Guid guid_SystemConnection,
            Guid guid_AirSystem,
            Guid guid_SystemSpace_From,
            Guid guid_SystemSpace_To,
            double designFlowRate_Lps,
            string reference_FlowController)
        {
            ConnectionType = connectionType;
            Guid_SpaceAirMovement = guid_SpaceAirMovement;
            Guid_SystemConnection = guid_SystemConnection;
            Guid_AirSystem = guid_AirSystem;
            Guid_SystemSpace_From = guid_SystemSpace_From;
            Guid_SystemSpace_To = guid_SystemSpace_To;
            DesignFlowRate_Lps = designFlowRate_Lps;
            Reference_FlowController = reference_FlowController;
        }

        /// <summary>Supply, extract or transfer. <c>Undefined</c> is always a refusal.</summary>
        public SystemVentilationConnectionType ConnectionType { get; }

        /// <summary>
        /// The analytical <c>SpaceAirMovement</c> a <see cref="SystemVentilationConnectionType.Transfer"/>
        /// row came from. <c>Guid.Empty</c> for supply and extract, whose analytical sources are terminals
        /// and are already carried by PR1's own bindings.
        /// </summary>
        public Guid Guid_SpaceAirMovement { get; }

        /// <summary>PR1's <c>SystemConnection</c>. Always present - this is the row's identity.</summary>
        public Guid Guid_SystemConnection { get; }

        /// <summary>Which air handling unit the leg belongs to.</summary>
        public Guid Guid_AirSystem { get; }

        /// <summary>
        /// The upstream endpoint as a <c>SystemSpace</c>. For a supply leg this is <c>Guid.Empty</c>: the
        /// air comes from the unit, not from a room.
        /// </summary>
        public Guid Guid_SystemSpace_From { get; }

        /// <summary>
        /// The downstream endpoint as a <c>SystemSpace</c>. For an extract leg this is <c>Guid.Empty</c>:
        /// the air goes to the unit, not to a room.
        /// </summary>
        public Guid Guid_SystemSpace_To { get; }

        /// <summary>PR1's <c>SystemConnectionParameter.DesignFlowRate</c>, in l/s, verbatim.</summary>
        public double DesignFlowRate_Lps { get; }

        /// <summary>
        /// The native guid of the in-line component carrying this leg's duty - the <c>Damper</c> - or
        /// <c>null</c> where none was created. <b>Never invented</b>: ducts have no guid of their own.
        /// </summary>
        public string Reference_FlowController { get; }

        /// <summary>
        /// Whether the row states a usable leg. A transfer row must name both rooms and its originating
        /// air movement; a supply or extract row must name the one room it touches.
        /// </summary>
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

                switch (ConnectionType)
                {
                    case SystemVentilationConnectionType.Transfer:
                        // Both endpoints are rooms, and the analytical movement it came from must be named,
                        // or a branching transfer cannot be told from two unrelated legs.
                        return Guid_SystemSpace_From != Guid.Empty
                            && Guid_SystemSpace_To != Guid.Empty
                            && Guid_SpaceAirMovement != Guid.Empty;

                    case SystemVentilationConnectionType.Supply:
                        return Guid_SystemSpace_To != Guid.Empty;

                    case SystemVentilationConnectionType.Extract:
                        return Guid_SystemSpace_From != Guid.Empty;

                    default:
                        return false;
                }
            }
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1} l/s: {2} -> {3} (connection {4}{5})",
                ConnectionType,
                DesignFlowRate_Lps,
                Guid_SystemSpace_From == Guid.Empty ? "AHU" : Guid_SystemSpace_From.ToString(),
                Guid_SystemSpace_To == Guid.Empty ? "AHU" : Guid_SystemSpace_To.ToString(),
                Guid_SystemConnection,
                Guid_SpaceAirMovement == Guid.Empty
                    ? string.Empty
                    : string.Concat(", movement ", Guid_SpaceAirMovement));
        }
    }
}
