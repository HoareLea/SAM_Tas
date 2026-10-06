// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// One physical aperture's stamps, read off a SAM <see cref="Aperture"/> and into values.
    /// <para>
    /// <b>Physical identity is the four <c>ZoneSurfaceReference</c>s and nothing else</b> - the pane's on each
    /// side and the frame's on each side, each a <see cref="ZoneSurfaceKey"/>. The two
    /// <c>BuildingElementGuid</c> stamps are carried here too, but only as DEFINITION BINDINGS: after Stage 2
    /// a great many physical apertures legitimately stamp the same one, so this type never lets a caller ask
    /// "which aperture is building element X?" - see <see cref="AperturePhysicalIndex"/>, which answers
    /// "which aperture is surface <c>{ZoneGuid, SurfaceNumber}</c>?" instead, and reports the element GUID
    /// only for verifying a binding it already resolved physically.
    /// </para>
    /// <para>COM-free: built from a SAM aperture, which is where the stamps live.</para>
    /// </summary>
    public sealed class AperturePhysicalIdentity
    {
        private readonly ZoneSurfaceKey[] keys_Pane;
        private readonly ZoneSurfaceKey[] keys_Frame;
        private readonly List<KeyValuePair<int, ZoneSurfaceKey>> allKeys_Pane;
        private readonly List<KeyValuePair<int, ZoneSurfaceKey>> allKeys_Frame;

        /// <param name="apertureGuid">The SAM aperture this identity belongs to.</param>
        /// <param name="paneKey_1">The pane's side-1 surface, or null when the aperture states none.</param>
        /// <param name="paneKey_2">The pane's side-2 surface, or null. Only an aperture between two zones has one.</param>
        /// <param name="frameKey_1">The frame's side-1 surface, or null when the aperture has no frame.</param>
        /// <param name="frameKey_2">The frame's side-2 surface, or null.</param>
        /// <param name="paneBuildingElementGuid">The reusable pane definition this aperture is bound to. A binding, never an identity.</param>
        /// <param name="frameBuildingElementGuid">The reusable frame definition this aperture is bound to. A binding, never an identity.</param>
        public AperturePhysicalIdentity(Guid apertureGuid, ZoneSurfaceKey paneKey_1, ZoneSurfaceKey paneKey_2, ZoneSurfaceKey frameKey_1, ZoneSurfaceKey frameKey_2, string paneBuildingElementGuid, string frameBuildingElementGuid)
            : this(apertureGuid, paneKey_1, paneKey_2, frameKey_1, frameKey_2, paneBuildingElementGuid, frameBuildingElementGuid, null, null, false, false)
        {
        }

        /// <param name="paneKeys_All">Every physical pane surface, including any extra faces on one side.</param>
        /// <param name="frameKeys_All">Every physical frame surface, including any extra faces on one side.</param>
        public AperturePhysicalIdentity(Guid apertureGuid, ZoneSurfaceKey paneKey_1, ZoneSurfaceKey paneKey_2, ZoneSurfaceKey frameKey_1, ZoneSurfaceKey frameKey_2, string paneBuildingElementGuid, string frameBuildingElementGuid, IEnumerable<ZoneSurfaceKey> paneKeys_All, IEnumerable<ZoneSurfaceKey> frameKeys_All, bool paneSurfaceSetComplete = true, bool frameSurfaceSetComplete = true)
        {
            ApertureGuid = apertureGuid;
            keys_Pane = new ZoneSurfaceKey[] { paneKey_1, paneKey_2 };
            keys_Frame = new ZoneSurfaceKey[] { frameKey_1, frameKey_2 };
            allKeys_Pane = BuildAllKeys(keys_Pane, paneKeys_All);
            allKeys_Frame = BuildAllKeys(keys_Frame, frameKeys_All);
            PaneSurfaceSetComplete = paneSurfaceSetComplete;
            FrameSurfaceSetComplete = frameSurfaceSetComplete;
            PaneBuildingElementGuid = paneBuildingElementGuid;
            FrameBuildingElementGuid = frameBuildingElementGuid;
        }

        public Guid ApertureGuid { get; }

        /// <summary>The reusable pane <c>TBD.buildingElement</c> GUID. Shared with every equivalent aperture.</summary>
        public string PaneBuildingElementGuid { get; }

        /// <summary>The reusable frame <c>TBD.buildingElement</c> GUID. Shared with every equivalent aperture.</summary>
        public string FrameBuildingElementGuid { get; }

        /// <summary>Whether the pane's full physical set, rather than representative legacy slots only, was present.</summary>
        public bool PaneSurfaceSetComplete { get; }

        /// <summary>Whether the frame's full physical set, rather than representative legacy slots only, was present.</summary>
        public bool FrameSurfaceSetComplete { get; }

        /// <summary>
        /// The stamp in the <c>_1</c> or <c>_2</c> slot of a part, or null when that slot is empty.
        /// </summary>
        /// <param name="aperturePart">Pane or frame. Anything else returns null.</param>
        /// <param name="side">1 or 2. Anything else returns null.</param>
        public ZoneSurfaceKey Key(AperturePart aperturePart, int side)
        {
            if (side != 1 && side != 2)
            {
                return null;
            }

            switch (aperturePart)
            {
                case AperturePart.Pane:
                    return keys_Pane[side - 1];

                case AperturePart.Frame:
                    return keys_Frame[side - 1];
            }

            return null;
        }

        /// <summary>The occupied slots of a part, in slot order.</summary>
        public List<KeyValuePair<int, ZoneSurfaceKey>> Keys(AperturePart aperturePart)
        {
            List<KeyValuePair<int, ZoneSurfaceKey>> result = new List<KeyValuePair<int, ZoneSurfaceKey>>();

            for (int side = 1; side <= 2; side++)
            {
                ZoneSurfaceKey zoneSurfaceKey = Key(aperturePart, side);
                if (zoneSurfaceKey != null)
                {
                    result.Add(new KeyValuePair<int, ZoneSurfaceKey>(side, zoneSurfaceKey));
                }
            }

            return result;
        }

        /// <summary>
        /// Every physical key of a part. Several keys may have the same side number when TAS split one pane or
        /// frame side into several faces; <see cref="Keys(AperturePart)"/> remains one representative per side.
        /// </summary>
        public List<KeyValuePair<int, ZoneSurfaceKey>> AllKeys(AperturePart aperturePart)
        {
            switch (aperturePart)
            {
                case AperturePart.Pane:
                    return new List<KeyValuePair<int, ZoneSurfaceKey>>(allKeys_Pane);

                case AperturePart.Frame:
                    return new List<KeyValuePair<int, ZoneSurfaceKey>>(allKeys_Frame);
            }

            return new List<KeyValuePair<int, ZoneSurfaceKey>>();
        }

        /// <summary>
        /// Whether <see cref="AllKeys(AperturePart)"/> came from an explicitly preserved complete set. A legacy
        /// aperture that has representative slots only cannot prove that another same-side face was not lost.
        /// </summary>
        public bool SurfaceSetComplete(AperturePart aperturePart)
        {
            return aperturePart == AperturePart.Frame ? FrameSurfaceSetComplete : aperturePart == AperturePart.Pane && PaneSurfaceSetComplete;
        }

        /// <summary>Every occupied slot, pane and frame.</summary>
        public List<Tuple<AperturePart, int, ZoneSurfaceKey>> Stamps()
        {
            List<Tuple<AperturePart, int, ZoneSurfaceKey>> result = new List<Tuple<AperturePart, int, ZoneSurfaceKey>>();

            foreach (AperturePart aperturePart in new AperturePart[] { AperturePart.Pane, AperturePart.Frame })
            {
                foreach (KeyValuePair<int, ZoneSurfaceKey> keyValuePair in AllKeys(aperturePart))
                {
                    result.Add(new Tuple<AperturePart, int, ZoneSurfaceKey>(aperturePart, keyValuePair.Key, keyValuePair.Value));
                }
            }

            return result;
        }

        /// <summary>The reusable definition GUID bound to a part. A binding, never an identity.</summary>
        public string BuildingElementGuid(AperturePart aperturePart)
        {
            switch (aperturePart)
            {
                case AperturePart.Pane:
                    return PaneBuildingElementGuid;

                case AperturePart.Frame:
                    return FrameBuildingElementGuid;
            }

            return null;
        }

        /// <summary>Whether this aperture states any physical surface at all. A stamp-less aperture cannot be resolved physically.</summary>
        public bool HasStamps
        {
            get { return allKeys_Pane.Count != 0 || allKeys_Frame.Count != 0; }
        }

        public override string ToString()
        {
            return string.Format("{0}: pane {1}/{2}, frame {3}/{4}",
                ApertureGuid,
                keys_Pane[0] == null ? "-" : keys_Pane[0].ToString(),
                keys_Pane[1] == null ? "-" : keys_Pane[1].ToString(),
                keys_Frame[0] == null ? "-" : keys_Frame[0].ToString(),
                keys_Frame[1] == null ? "-" : keys_Frame[1].ToString());
        }

        private static List<KeyValuePair<int, ZoneSurfaceKey>> BuildAllKeys(ZoneSurfaceKey[] representatives, IEnumerable<ZoneSurfaceKey> allKeys)
        {
            List<ZoneSurfaceKey> keys = new List<ZoneSurfaceKey>();

            if (allKeys != null)
            {
                foreach (ZoneSurfaceKey key in allKeys)
                {
                    if (key != null && key.IsValid && !keys.Contains(key))
                    {
                        keys.Add(key);
                    }
                }
            }

            foreach (ZoneSurfaceKey representative in representatives)
            {
                if (representative != null && representative.IsValid && !keys.Contains(representative))
                {
                    keys.Add(representative);
                }
            }

            keys.Sort(Query.CompareZoneSurfaceKeys);

            List<string> zones = keys.Select(x => x.ZoneGuid).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            return keys.ConvertAll(x => new KeyValuePair<int, ZoneSurfaceKey>(zones.IndexOf(x.ZoneGuid) + 1, x));
        }
    }
}
