// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// What one room is <b>intended</b> to become in TAS, stated entirely from the PR1 graph and the
    /// analytical model, <b>before</b> any native object exists.
    /// <para>
    /// <b>Why an intent is a separate record from <see cref="SystemVentilationBinding"/>.</b> A binding
    /// says what the conversion actually produced; an intent says what it was supposed to produce. Only
    /// two independent statements can be reconciled - a single record written by the conversion and read
    /// back by the conversion proves nothing at all.
    /// </para>
    /// <para>
    /// <b><see cref="Reference_ZoneLoad"/> is the key that makes this route name-free.</b> Measured on
    /// licensed TAS against fixture 1: the TBD's zones answer
    /// <c>zone[0] "Cell 1" GUID={37FA3D5C-27E0-41D5-8825-366F8DBD66AD}</c> and the TSD's zone loads,
    /// read through <c>TSDData.GetZoneLoad(i)</c>, answer <b>the same guid</b> for the same room - and
    /// <c>TSDData.GetZoneLoadForGuid</c> resolves it, braced or bare. SAM stamps that same guid onto the
    /// analytical space as <c>SpaceParameter.ZoneGuid</c> during the TBD workflow. So the chain
    /// </para>
    /// <code>
    /// Space.Guid -> SpaceParameter.ZoneGuid == ZoneLoad.GUID -> GetZoneLoadForGuid -> AddZoneLoad
    /// </code>
    /// <para>
    /// is identity end to end. The defect it replaces matched <c>SystemSpaceParameter.SpaceName</c>
    /// against <c>ZoneLoad.Name</c>, which on the shipped template compared "System Zone 1" with
    /// "Cell 1" and bound nothing at all - and which, when it did match, would alias two rooms that
    /// happened to share a display name.
    /// </para>
    /// <para>Free of TAS COM types, so an intent can be built and asserted without a TAS licence.</para>
    /// </summary>
    public class SystemVentilationRoomIntent
    {
        public SystemVentilationRoomIntent(
            Guid guid_Space,
            Guid guid_SystemSpace,
            Guid guid_AirSystem,
            string reference_ZoneLoad,
            double? designFlowRate_Supply_Lps,
            double? designFlowRate_Extract_Lps)
        {
            Guid_Space = guid_Space;
            Guid_SystemSpace = guid_SystemSpace;
            Guid_AirSystem = guid_AirSystem;
            Reference_ZoneLoad = reference_ZoneLoad;
            DesignFlowRate_Supply_Lps = designFlowRate_Supply_Lps;
            DesignFlowRate_Extract_Lps = designFlowRate_Extract_Lps;
        }

        /// <summary>The analytical room. PR1's <c>SystemSpace</c> lineage row states it.</summary>
        public Guid Guid_Space { get; }

        /// <summary>PR1's materialised <c>SystemSpace</c> - the key the conversion meets it by.</summary>
        public Guid Guid_SystemSpace { get; }

        /// <summary>Which physical air handling unit serves it - PR1's <c>AirSystem</c>.</summary>
        public Guid Guid_AirSystem { get; }

        /// <summary>
        /// The TAS zone guid, from <c>SpaceParameter.ZoneGuid</c>. Measured to be the same identifier as
        /// the TSD's <c>ZoneLoad.GUID</c>, which is what <c>TSDData.GetZoneLoadForGuid</c> takes.
        /// </summary>
        public string Reference_ZoneLoad { get; }

        /// <summary>The room's supply duty in l/s as PR1 authored it, or null where it has no supply.</summary>
        public double? DesignFlowRate_Supply_Lps { get; }

        /// <summary>The room's extract duty in l/s as PR1 authored it, or null where it has no extract.</summary>
        public double? DesignFlowRate_Extract_Lps { get; }

        /// <summary>
        /// Whether this intent names everything the conversion needs. A room whose analytical space was
        /// never stamped with a TAS zone guid cannot be bound by identity, and is refused rather than
        /// resolved by name.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return Guid_Space != Guid.Empty
                    && Guid_SystemSpace != Guid.Empty
                    && Guid_AirSystem != Guid.Empty
                    && !string.IsNullOrWhiteSpace(Reference_ZoneLoad);
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Space {0} -> SystemSpace {1} (AirSystem {2}, load {3}, supply {4}, extract {5})",
                Guid_Space,
                Guid_SystemSpace,
                Guid_AirSystem,
                Reference_ZoneLoad ?? "<none>",
                DesignFlowRate_Supply_Lps.HasValue ? DesignFlowRate_Supply_Lps.Value.ToString() : "none",
                DesignFlowRate_Extract_Lps.HasValue ? DesignFlowRate_Extract_Lps.Value.ToString() : "none");
        }
    }
}
