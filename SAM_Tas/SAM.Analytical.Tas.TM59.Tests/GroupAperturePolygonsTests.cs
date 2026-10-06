// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Stage 3 (S3-C2) - the aperture import's polygon grouping, pulled out as its own pure function and
    /// exercised directly, COM-free.</b>
    /// <para>
    /// Pins the two import bugs the extraction fixed: a lone pane (no coincident frame ring) used to end up
    /// in an EMPTY group - <see cref="LonePane_NoCoincidentPartner_IsAOneMemberGroupWithItsOwnKey"/> - and
    /// the seed polygon that DID find a partner was paired with whatever key happened to be first in the
    /// shrinking remainder list, not its own -
    /// <see cref="Seed_PairedWithAPartner_KeepsItsOwnKeyNotTheRemainderS"/>.
    /// </para>
    /// </summary>
    [TestFixture]
    public class GroupAperturePolygonsTests
    {
        private static Polygon3D Rect(double x, double y, double width, double height)
        {
            return new Polygon3D(new List<Point3D>
            {
                new Point3D(x, y, 0),
                new Point3D(x + width, y, 0),
                new Point3D(x + width, y, height),
                new Point3D(x, y, height)
            });
        }

        private static Tuple<Polygon3D, string> Tuple_(Polygon3D polygon3D, string key)
        {
            return new Tuple<Polygon3D, string>(polygon3D, key);
        }

        [Test]
        public void LonePane_NoCoincidentPartner_IsAOneMemberGroupWithItsOwnKey()
        {
            // A single, isolated polygon - the ordinary case for a sealed window whose frame ring TAS
            // never wrote as a separate surface. Before the fix this produced an EMPTY group, so the
            // caller's downstream stamp/OpeningProperties block never ran at all.
            Tuple<Polygon3D, string> pane = Tuple_(Rect(0, 0, 1, 1.2), "pane-key");

            List<List<Tuple<Polygon3D, string>>> groups = TasQuery.GroupAperturePolygons(new List<Tuple<Polygon3D, string>> { pane });

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Count, Is.EqualTo(1), "a lone polygon must still produce a group - one member, not zero");
            Assert.That(groups[0][0].Item2, Is.EqualTo("pane-key"), "the lone polygon's own key must travel with it");
        }

        [Test]
        public void Seed_PairedWithAPartner_KeepsItsOwnKeyNotTheRemainderS()
        {
            // The frame (larger, sorted first - it becomes the SEED) and its inset pane (smaller, coincident
            // - internal point falls inside the frame). Before the fix, the seed's own key was discarded
            // before the branch that paired it back in, and it picked up whatever tuple was first in the
            // ALREADY-SHRUNK remainder instead - silently wrong the moment more than one group exists in the
            // same pass, because the "first remaining" tuple belongs to a DIFFERENT aperture's polygon.
            Tuple<Polygon3D, string> frame = Tuple_(Rect(0, 0, 1.2, 1.4), "frame-key");
            Tuple<Polygon3D, string> pane = Tuple_(Rect(0.1, 0, 1.0, 1.2), "pane-key");

            // A second, unrelated aperture's polygon - large enough to be evaluated before the real pane
            // if the seed's own key were (wrongly) read back off "whatever remains first".
            Tuple<Polygon3D, string> other_Frame = Tuple_(Rect(10, 0, 5, 5), "other-frame-key");

            List<List<Tuple<Polygon3D, string>>> groups = TasQuery.GroupAperturePolygons(new List<Tuple<Polygon3D, string>> { pane, other_Frame, frame });

            List<Tuple<Polygon3D, string>> group_Frame = groups.Find(x => x.Any(t => t.Item2 == "frame-key"));
            Assert.That(group_Frame, Is.Not.Null);
            Assert.That(group_Frame.Count, Is.EqualTo(2), "the frame and its coincident pane belong in the SAME group");

            List<string> keys = group_Frame.ConvertAll(x => x.Item2);
            Assert.That(keys, Does.Contain("frame-key"));
            Assert.That(keys, Does.Contain("pane-key"));
            Assert.That(keys, Does.Not.Contain("other-frame-key"), "the seed's group must never pick up a different aperture's key");
        }

        [Test]
        public void ManyDisjointApertures_EachStaysItsOwnGroup()
        {
            // 50 non-overlapping windows on one construction, as the plan's own S3-C2 test list names -
            // every polygon is its own group, none bleed into each other.
            List<Tuple<Polygon3D, string>> tuples = new List<Tuple<Polygon3D, string>>();
            for (int i = 0; i < 50; i++)
            {
                tuples.Add(Tuple_(Rect(i * 2, 0, 1, 1.2), "window-" + i));
            }

            List<List<Tuple<Polygon3D, string>>> groups = TasQuery.GroupAperturePolygons(tuples);

            Assert.That(groups.Count, Is.EqualTo(50));
            Assert.That(groups.All(x => x.Count == 1), Is.True, "50 disjoint polygons must produce 50 one-member groups");

            HashSet<string> keys = new HashSet<string>(groups.SelectMany(x => x.ConvertAll(t => t.Item2)));
            Assert.That(keys.Count, Is.EqualTo(50), "every window's own key must appear exactly once, in its own group");
        }

        [Test]
        public void NestedAndAdjacentWindows_SeparateCorrectlyByContainment()
        {
            // Two SEPARATE windows, each with its own frame+pane pair, sitting side by side - containment
            // must not let one window's frame swallow the NEIGHBOUR's pane just because they are adjacent.
            Tuple<Polygon3D, string> frame_1 = Tuple_(Rect(0, 0, 1.2, 1.4), "frame-1");
            Tuple<Polygon3D, string> pane_1 = Tuple_(Rect(0.1, 0, 1.0, 1.2), "pane-1");

            Tuple<Polygon3D, string> frame_2 = Tuple_(Rect(2, 0, 1.2, 1.4), "frame-2");
            Tuple<Polygon3D, string> pane_2 = Tuple_(Rect(2.1, 0, 1.0, 1.2), "pane-2");

            List<List<Tuple<Polygon3D, string>>> groups = TasQuery.GroupAperturePolygons(new List<Tuple<Polygon3D, string>> { pane_1, frame_2, pane_2, frame_1 });

            Assert.That(groups.Count, Is.EqualTo(2));

            List<Tuple<Polygon3D, string>> group_1 = groups.Find(x => x.Any(t => t.Item2 == "frame-1"));
            List<Tuple<Polygon3D, string>> group_2 = groups.Find(x => x.Any(t => t.Item2 == "frame-2"));

            Assert.That(group_1.ConvertAll(x => x.Item2), Is.EquivalentTo(new[] { "frame-1", "pane-1" }));
            Assert.That(group_2.ConvertAll(x => x.Item2), Is.EquivalentTo(new[] { "frame-2", "pane-2" }));
        }

        [Test]
        public void EmptyInput_ReturnsNoGroups()
        {
            List<List<Tuple<Polygon3D, string>>> groups = TasQuery.GroupAperturePolygons(new List<Tuple<Polygon3D, string>>());

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void NullInput_ReturnsNoGroups()
        {
            List<List<Tuple<Polygon3D, string>>> groups = TasQuery.GroupAperturePolygons<string>(null);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }
    }
}
