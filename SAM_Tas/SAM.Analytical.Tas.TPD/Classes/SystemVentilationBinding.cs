// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// One room's lineage, from the analytical model all the way to the TAS result, entirely by identity.
    /// <para>
    /// <b>The chain, and why every link is here.</b> Measured on licensed TAS against a real no-IZAM TSD:
    /// </para>
    /// <code>
    /// Space.Guid -> SystemSpace.Guid -> SystemZone.GUID  (the TPD component)
    ///                                -> ZoneLoad.GUID   (a DIFFERENT identifier)
    ///                                -> results
    /// </code>
    /// <para>
    /// <b>The two native identifiers are distinct and neither substitutes for the other.</b> For a zone
    /// bound with <c>AddZoneLoad</c>, <c>SystemZone.GUID</c> answered
    /// <c>{09EDFD23-6F7A-41A0-A261-72C460997A6A}</c> while its <c>ZoneLoad.GUID</c> answered
    /// <c>{37FA3D5C-27E0-41D5-8825-366F8DBD66AD}</c>. The component guid round-trips through
    /// <c>System.GetComponentByGUID</c>; the load guid resolves through
    /// <c>TSDData.GetZoneLoadForGuid</c>. Both are carried so the result side can use whichever is
    /// authoritative without the design changing.
    /// </para>
    /// <para>
    /// <b>No display name appears anywhere on this path.</b> That is not fastidiousness: a real
    /// TAS-authored file observed during the checkpoint had two different zones <i>both</i> named
    /// "System Zone 1", and in a block of flats every dwelling has a "Bedroom 2".
    /// </para>
    /// <para>
    /// Free of TAS COM types, so a binding can be built, asserted and refused without a TAS licence.
    /// </para>
    /// </summary>
    public class SystemVentilationBinding
    {
        /// <summary>
        /// Creates a room binding. Nothing is inferred: every identifier is supplied by the code that
        /// held both sides of the pairing at the moment it was made.
        /// </summary>
        public SystemVentilationBinding(
            Guid guid_Space,
            Guid guid_SystemSpace,
            Guid guid_AirSystem,
            string reference_SystemZone,
            string reference_ZoneLoad,
            string reference_System,
            double? designFlowRate_Supply_Lps,
            double? designFlowRate_Extract_Lps)
        {
            Guid_Space = guid_Space;
            Guid_SystemSpace = guid_SystemSpace;
            Guid_AirSystem = guid_AirSystem;
            Reference_SystemZone = reference_SystemZone;
            Reference_ZoneLoad = reference_ZoneLoad;
            Reference_System = reference_System;
            DesignFlowRate_Supply_Lps = designFlowRate_Supply_Lps;
            DesignFlowRate_Extract_Lps = designFlowRate_Extract_Lps;
        }

        /// <summary>The analytical room. This is the key every caller outside SAM_Tas uses.</summary>
        public Guid Guid_Space { get; }

        /// <summary>PR1's materialised <c>SystemSpace</c> for that room.</summary>
        public Guid Guid_SystemSpace { get; }

        /// <summary>Which physical air handling unit serves it - PR1's <c>AirSystem</c>.</summary>
        public Guid Guid_AirSystem { get; }

        /// <summary>
        /// The native TPD component guid of the zone, read late-bound through
        /// <c>Query.NativeReference</c> and round-tripped through <c>System.GetComponentByGUID</c>.
        /// </summary>
        public string Reference_SystemZone { get; }

        /// <summary>
        /// The native <c>ZoneLoad.GUID</c> the zone is bound to - a <b>different</b> identifier from
        /// <see cref="Reference_SystemZone"/>, and the one <c>TSDData.GetZoneLoadForGuid</c> resolves.
        /// Null when the zone carries no load, which the caller refuses rather than papering over.
        /// </summary>
        public string Reference_ZoneLoad { get; }

        /// <summary>The native TAS <c>System.GUID</c> the zone belongs to.</summary>
        public string Reference_System { get; }

        /// <summary>
        /// The room's supply duty in l/s, exactly as PR1 authored it. Null when the room has no supply -
        /// which is a real case (an extract-only wet room), not a missing value.
        /// </summary>
        public double? DesignFlowRate_Supply_Lps { get; }

        /// <summary>
        /// The room's extract duty in l/s, exactly as PR1 authored it. Null when the room has no extract.
        /// <b>Never conflated with supply</b>: measured on licensed TAS, a room can carry 31 l/s supply
        /// and 19 l/s extract simultaneously and independently.
        /// </summary>
        public double? DesignFlowRate_Extract_Lps { get; }

        /// <summary>
        /// Whether this binding names every identifier it needs to be usable. A binding that is not
        /// complete must be refused, never resolved by falling back to a name.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return Guid_Space != Guid.Empty
                    && Guid_SystemSpace != Guid.Empty
                    && Guid_AirSystem != Guid.Empty
                    && !string.IsNullOrWhiteSpace(Reference_SystemZone)
                    && !string.IsNullOrWhiteSpace(Reference_System);
            }
        }

        /// <summary>
        /// Whether the result side can be reached. The load guid is what
        /// <c>TSDData.GetZoneLoadForGuid</c> takes, so a binding without one cannot fetch a series.
        /// </summary>
        public bool CanResolveResults
        {
            get { return IsComplete && !string.IsNullOrWhiteSpace(Reference_ZoneLoad); }
        }

        public override string ToString()
        {
            return string.Format(
                "Space {0} -> SystemSpace {1} -> zone {2} / load {3} (supply {4}, extract {5})",
                Guid_Space,
                Guid_SystemSpace,
                Reference_SystemZone ?? "<none>",
                Reference_ZoneLoad ?? "<none>",
                DesignFlowRate_Supply_Lps.HasValue ? DesignFlowRate_Supply_Lps.Value.ToString() : "none",
                DesignFlowRate_Extract_Lps.HasValue ? DesignFlowRate_Extract_Lps.Value.ToString() : "none");
        }
    }
}
