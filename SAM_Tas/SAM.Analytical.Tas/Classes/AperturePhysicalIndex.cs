// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>The one place a physical TAS surface is turned back into the SAM aperture that owns it.</b>
    /// <c>{ ZoneGuid, SurfaceNumber }</c> in, <c>(aperture, part, side)</c> out - or a refusal.
    /// <para>
    /// <b>Why a whole type rather than a scan.</b> Two things have to be true of an answer for it to be safe
    /// to write through: exactly one aperture claims that surface, and it claims it in exactly one slot.
    /// Neither is guaranteed - a model that has been round-tripped through a broken pairing, or hand edited,
    /// can carry the same stamp on two apertures - and a scan that returns the first hit turns that into a
    /// silent cross-bind. This index detects the collision when it is BUILT and answers <c>false</c> for that
    /// key for ever after, with a reason. <b>Refusing is the correct outcome</b>: the alternative is updating
    /// a window the user did not change.
    /// </para>
    /// <para>
    /// <b>What it deliberately cannot do</b> is find an aperture from a building-element GUID. After Stage 2
    /// that GUID is a shared definition binding - two hundred identical windows carry the same one - so a
    /// lookup by it has no single answer. <see cref="ApertureGuids(string)"/> exists only so a caller that has
    /// ALREADY resolved a surface physically can check which apertures share the definition it is bound to; it
    /// returns the whole set, and never pretends the set has one member.
    /// </para>
    /// <para>COM-free. Built from <see cref="AperturePhysicalIdentity"/> values.</para>
    /// </summary>
    public sealed class AperturePhysicalIndex
    {
        private sealed class Entry
        {
            public Guid ApertureGuid;
            public AperturePart AperturePart;
            public int Side;
            public string Refusal;
        }

        private readonly Dictionary<ZoneSurfaceKey, Entry> entries = new Dictionary<ZoneSurfaceKey, Entry>();

        private readonly Dictionary<string, List<Guid>> apertureGuidsByBuildingElementGuid = new Dictionary<string, List<Guid>>();

        private readonly List<AperturePhysicalIdentity> identities = new List<AperturePhysicalIdentity>();

        /// <param name="aperturePhysicalIdentities">
        /// Every physical aperture in the model. Passing a SUBSET narrows what can be resolved but never makes
        /// a wrong answer right, so callers should pass the whole model: an ambiguity between a surface in the
        /// subset and one outside it would otherwise go undetected.
        /// </param>
        public AperturePhysicalIndex(IEnumerable<AperturePhysicalIdentity> aperturePhysicalIdentities)
        {
            if (aperturePhysicalIdentities == null)
            {
                return;
            }

            foreach (AperturePhysicalIdentity aperturePhysicalIdentity in aperturePhysicalIdentities)
            {
                if (aperturePhysicalIdentity == null)
                {
                    continue;
                }

                identities.Add(aperturePhysicalIdentity);

                foreach (Tuple<AperturePart, int, ZoneSurfaceKey> stamp in aperturePhysicalIdentity.Stamps())
                {
                    ZoneSurfaceKey zoneSurfaceKey = stamp.Item3;
                    if (zoneSurfaceKey == null || !zoneSurfaceKey.IsValid)
                    {
                        continue;
                    }

                    Entry entry;
                    if (!entries.TryGetValue(zoneSurfaceKey, out entry))
                    {
                        entries[zoneSurfaceKey] = new Entry
                        {
                            ApertureGuid = aperturePhysicalIdentity.ApertureGuid,
                            AperturePart = stamp.Item1,
                            Side = stamp.Item2,
                            Refusal = null
                        };

                        continue;
                    }

                    //A second claim on one physical surface. The first claim is NOT kept and preferred - that
                    //would make the answer depend on enumeration order, and one of the two claims is wrong.
                    //Both lose, and the key refuses from here on.
                    if (entry.Refusal == null)
                    {
                        entry.Refusal = string.Format("Physical surface {0} is claimed by more than one aperture stamp ({1} {2} side {3}, and {4} {5} side {6}); it cannot identify either, so it resolves to none.",
                            zoneSurfaceKey,
                            entry.ApertureGuid,
                            entry.AperturePart,
                            entry.Side,
                            aperturePhysicalIdentity.ApertureGuid,
                            stamp.Item1,
                            stamp.Item2);
                    }
                }

                foreach (AperturePart aperturePart in new AperturePart[] { AperturePart.Pane, AperturePart.Frame })
                {
                    string buildingElementGuid = aperturePhysicalIdentity.BuildingElementGuid(aperturePart);
                    if (string.IsNullOrWhiteSpace(buildingElementGuid))
                    {
                        continue;
                    }

                    List<Guid> apertureGuids;
                    if (!apertureGuidsByBuildingElementGuid.TryGetValue(buildingElementGuid, out apertureGuids))
                    {
                        apertureGuids = new List<Guid>();
                        apertureGuidsByBuildingElementGuid[buildingElementGuid] = apertureGuids;
                    }

                    if (!apertureGuids.Contains(aperturePhysicalIdentity.ApertureGuid))
                    {
                        apertureGuids.Add(aperturePhysicalIdentity.ApertureGuid);
                    }
                }
            }
        }

        /// <summary>Every identity this index was built from.</summary>
        public List<AperturePhysicalIdentity> Identities
        {
            get { return new List<AperturePhysicalIdentity>(identities); }
        }

        /// <summary>How many physical surfaces resolve. Excludes the keys that refuse.</summary>
        public int ResolvableCount
        {
            get { return entries.Count(x => x.Value.Refusal == null); }
        }

        /// <summary>Every physical surface more than one aperture claims, with the reason.</summary>
        public List<KeyValuePair<ZoneSurfaceKey, string>> Ambiguities()
        {
            return entries.Where(x => x.Value.Refusal != null).Select(x => new KeyValuePair<ZoneSurfaceKey, string>(x.Key, x.Value.Refusal)).ToList();
        }

        /// <summary>
        /// The aperture, part and side that own one physical surface.
        /// <para>
        /// <c>false</c> for an unknown key - a surface no SAM aperture stamps, which is what a foreign or
        /// native TBD looks like and is not an error - and <c>false</c> WITH a <paramref name="refusal"/> for a
        /// key two apertures claim. A caller must treat both the same way: do not write.
        /// </para>
        /// </summary>
        public bool TryResolve(ZoneSurfaceKey zoneSurfaceKey, out Guid apertureGuid, out AperturePart aperturePart, out int side, out string refusal)
        {
            apertureGuid = Guid.Empty;
            aperturePart = AperturePart.Undefined;
            side = 0;
            refusal = null;

            if (zoneSurfaceKey == null || !zoneSurfaceKey.IsValid)
            {
                return false;
            }

            Entry entry;
            if (!entries.TryGetValue(zoneSurfaceKey, out entry))
            {
                return false;
            }

            if (entry.Refusal != null)
            {
                refusal = entry.Refusal;
                return false;
            }

            apertureGuid = entry.ApertureGuid;
            aperturePart = entry.AperturePart;
            side = entry.Side;

            return true;
        }

        /// <summary>
        /// Every aperture bound to one reusable building element.
        /// <para>
        /// <b>For verification only.</b> A count above one is the NORMAL, intended Stage 2 state, not a fault;
        /// this exists so a caller that resolved a surface physically can ask whether the definition it points
        /// at is exclusively that aperture, which decides whether per-aperture data may be written onto the
        /// definition at all.
        /// </para>
        /// </summary>
        public List<Guid> ApertureGuids(string buildingElementGuid)
        {
            List<Guid> result;
            if (string.IsNullOrWhiteSpace(buildingElementGuid) || !apertureGuidsByBuildingElementGuid.TryGetValue(buildingElementGuid, out result))
            {
                return new List<Guid>();
            }

            return new List<Guid>(result);
        }
    }
}
