// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>The aperture import's own polygon grouping, pulled out as a pure, COM-free function.</b>
        /// Groups a set of (polygon, key) pairs the way one physical aperture's coincident surfaces are
        /// grouped on import - a frame ring and its inset pane, or several overlapping polygons the same
        /// window contributed - into one list per aperture: sorted largest-first, each group SEEDED by its
        /// own largest remaining polygon, gathering every OTHER polygon whose internal point falls inside
        /// the seed's face.
        /// <para>
        /// <b>The seed always joins its own group.</b> A polygon with no coincident partner - a lone pane
        /// with no matching frame ring, the ordinary case for a sealed or frameless opening - is still a
        /// ONE-MEMBER group, never an empty one. Before this existed as its own function, the aperture
        /// import paired the seed's polygon with whatever tuple happened to be first in the REMAINING list
        /// after the seed was removed - a different surface's key entirely when a genuine partner existed,
        /// and no key at all when it did not, so a lone pane got no <c>ZoneSurfaceReference</c> stamp, no
        /// <c>BuildingElementGuid</c> stamp and no <c>OpeningProperties</c> import. Both bugs shared the one
        /// root cause - the seed's own key was discarded before the branch that needed it - and this fixes
        /// both by construction: the seed's own (polygon, key) pair is captured before anything is removed,
        /// and added to the group unconditionally.
        /// </para>
        /// <para>
        /// <b>Generic over the key</b> so this needs no TBD type to be exercised - the real import passes
        /// <c>TBD.IZoneSurface</c>, a test can pass anything at all. The GROUPING only ever looks at the
        /// polygons; the key travels along for the caller to read back out of the returned groups.
        /// </para>
        /// </summary>
        /// <param name="tuples">Every (polygon, key) pair to group. A null or key-less entry is dropped.</param>
        /// <param name="tolerance">The containment tolerance used to decide whether a polygon's internal point falls inside a seed's face.</param>
        /// <returns>One group per aperture, largest seed first within each group; never null.</returns>
        public static List<List<Tuple<Polygon3D, T>>> GroupAperturePolygons<T>(IEnumerable<Tuple<Polygon3D, T>> tuples, double tolerance = Core.Tolerance.MacroDistance)
        {
            List<List<Tuple<Polygon3D, T>>> result = new List<List<Tuple<Polygon3D, T>>>();

            if (tuples == null)
            {
                return result;
            }

            List<Tuple<Polygon3D, T>> remaining = tuples.Where(x => x != null && x.Item1 != null).ToList();
            remaining.Sort((x, y) => y.Item1.GetArea().CompareTo(x.Item1.GetArea()));

            while (remaining.Count > 0)
            {
                Tuple<Polygon3D, T> seed = remaining[0];
                remaining.RemoveAt(0);

                Face3D face3D_Seed = new Face3D(seed.Item1);

                List<Tuple<Polygon3D, T>> group = remaining.FindAll(x => face3D_Seed.InRange(x.Item1.InternalPoint3D(), tolerance));

                //Unconditional - a group with no coincident partner is a group of exactly one, not zero.
                group.Add(seed);

                group.ForEach(x => remaining.Remove(x));

                //Largest first within the group too, so a caller taking group[0] as "the seed" (as the
                //import does, for the lone-member case) gets the same polygon this function seeded with.
                group.Sort((x, y) => y.Item1.GetArea().CompareTo(x.Item1.GetArea()));

                result.Add(group);
            }

            return result;
        }
    }
}
