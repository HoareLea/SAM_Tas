// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>The identity of one physical TAS <c>zoneSurface</c>: <c>{ ZoneGuid, SurfaceNumber }</c>.</b>
    /// <para>
    /// This is the whole of physical aperture identity, and the reason it needs a type of its own is that
    /// neither half is sufficient. A TAS surface number is scoped to its ZONE - surface 7 exists in every
    /// zone in the building - so a number alone cross-binds zones. A zone GUID alone names a room. Only the
    /// pair locates a surface, and it is the pair the SAM side stamps as
    /// <c>Pane</c>/<c>FrameZoneSurfaceReference_1</c>/<c>_2</c>.
    /// </para>
    /// <para>
    /// <b>What must never be used instead.</b> After Stage 2 a <c>TBD.buildingElement</c> is a REUSABLE
    /// DEFINITION - two hundred identical windows legitimately share one, and so legitimately stamp the same
    /// <c>Pane</c>/<c>FrameBuildingElementGuid</c>. A building-element GUID, a construction GUID, an aperture
    /// type, a definition-derived name and a surface AREA are therefore all properties of the definition or
    /// of the shape, not of the instance, and none of them can tell two identical windows apart. This key
    /// can.
    /// </para>
    /// <para>
    /// Immutable, with value equality, and COM-free: it is built from two values already read out of COM, so
    /// every decision taken over it is testable without an installed TAS.
    /// </para>
    /// </summary>
    public sealed class ZoneSurfaceKey : IEquatable<ZoneSurfaceKey>
    {
        /// <summary>The surface number a <c>zoneSurface</c> reports, scoped to <see cref="ZoneGuid"/>.</summary>
        public int SurfaceNumber { get; }

        /// <summary>
        /// The owning zone's GUID, NORMALISED - see <see cref="Query.NormalizeZoneGuid(string)"/>. Held
        /// normalised rather than normalising on comparison so that equality and
        /// <see cref="GetHashCode"/> cannot disagree.
        /// </summary>
        public string ZoneGuid { get; }

        public ZoneSurfaceKey(string zoneGuid, int surfaceNumber)
        {
            ZoneGuid = Query.NormalizeZoneGuid(zoneGuid);
            SurfaceNumber = surfaceNumber;
        }

        /// <summary>
        /// A key with both halves present. A reference carrying no zone GUID, or the
        /// <see cref="Core.Tas.ZoneSurfaceReference"/> sentinel surface number <c>-1</c>, does not locate a
        /// surface and must not be allowed to match one - which is what makes a half-populated stamp a
        /// refusal rather than a wildcard.
        /// </summary>
        public bool IsValid
        {
            get { return !string.IsNullOrEmpty(ZoneGuid) && SurfaceNumber >= 0; }
        }

        public bool Equals(ZoneSurfaceKey other)
        {
            if (other == null)
            {
                return false;
            }

            return SurfaceNumber == other.SurfaceNumber && string.Equals(ZoneGuid, other.ZoneGuid, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ZoneSurfaceKey);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = 17;
                result = (result * 31) + SurfaceNumber;
                result = (result * 31) + (ZoneGuid == null ? 0 : ZoneGuid.GetHashCode());
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", ZoneGuid ?? "<no zone>", SurfaceNumber);
        }
    }
}
