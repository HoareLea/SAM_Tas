// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// D-1. <c>Query.ZoneLoads(SystemComponent)</c> must enumerate a zone's loads, not repeat the first one.
    /// <para>
    /// The defect being pinned: the loop advanced <c>i</c> but read <c>GetZoneLoad(index)</c> with
    /// <c>index</c> fixed at <c>1</c>, so a <c>SystemZone</c> carrying N loads answered <b>the first load N
    /// times</b>. Its two <c>TSDData</c> siblings in the same file were always correct - they read
    /// <c>GetZoneLoad(i)</c> and skip nulls - so this fixture also holds the <c>SystemComponent</c> overload to
    /// that same standard.
    /// </para>
    /// <para>
    /// Where it bit: <c>Modify.UpdateSpaceAirflows</c> searches these loads by name, so a zone whose second
    /// load is the requested room was never found and its airflow was silently not written. The Part O
    /// Iteration 3 route reaches multi-load zones through <c>Modify.AssignZones</c> and the
    /// <c>SetMultiplicity</c> group path, which is why this is a PR2 blocker rather than latent debt.
    /// </para>
    /// <para>
    /// The production method is reached through <see cref="TpdReflection"/> - see the note there on CS1769 and
    /// why the production interop embedding is deliberately left alone. No TAS licence, install or COM server
    /// is involved: the loads below are managed fakes.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ZoneLoadEnumerationTests
    {
        private static FakeSystemComponent SystemComponent(params string[] names)
        {
            FakeSystemComponent result = new FakeSystemComponent();

            foreach (string name in names)
            {
                result.Add(new FakeZoneLoad { Name = name, GUID = string.Concat("guid-", name) });
            }

            return result;
        }

        [Test]
        public void ZoneLoads_ThreeLoadZone_ReturnsThreeDistinctLoads()
        {
            FakeSystemComponent systemComponent = SystemComponent("Bedroom 1", "Bedroom 2", "Bathroom");

            List<object> zoneLoads = TpdReflection.ZoneLoads_SystemComponent(systemComponent);

            Assert.That(zoneLoads, Is.Not.Null, "A three-load zone must not enumerate to null.");
            Assert.That(zoneLoads.Count, Is.EqualTo(3), "A three-load zone must report three loads.");

            List<string> names = zoneLoads.Select(x => TpdReflection.Name(x)).ToList();

            Assert.That(
                names,
                Is.EqualTo(new List<string> { "Bedroom 1", "Bedroom 2", "Bathroom" }),
                "The loads must come back in TAS's own order, each exactly once. Repeating the first load is "
                + "the fixed-index defect.");

            Assert.That(
                names.Distinct().Count(),
                Is.EqualTo(3),
                "The three loads must be distinct. Three copies of load 1 is the defect this test exists for.");
        }

        [Test]
        public void ZoneLoads_ThreeLoadZone_RequestsEveryIndexOnce()
        {
            FakeSystemComponent systemComponent = SystemComponent("A", "B", "C");

            TpdReflection.ZoneLoads_SystemComponent(systemComponent);

            Assert.That(
                systemComponent.RequestedIndexes,
                Is.EqualTo(new List<int> { 1, 2, 3 }),
                "Every 1-based index must be requested exactly once. Requesting index 1 three times is the "
                + "defect; this asserts on the COM traffic itself so the fix cannot be faked by post-filtering.");
        }

        [Test]
        public void ZoneLoads_SingleLoadZone_IsUnchanged()
        {
            FakeSystemComponent systemComponent = SystemComponent("Only");

            List<object> zoneLoads = TpdReflection.ZoneLoads_SystemComponent(systemComponent);

            Assert.That(zoneLoads.Count, Is.EqualTo(1));
            Assert.That(TpdReflection.Name(zoneLoads[0]), Is.EqualTo("Only"));
        }

        [Test]
        public void ZoneLoads_EmptyZone_ReturnsEmptyNotNull()
        {
            FakeSystemComponent systemComponent = SystemComponent();

            List<object> zoneLoads = TpdReflection.ZoneLoads_SystemComponent(systemComponent);

            Assert.That(zoneLoads, Is.Not.Null, "A zone with no loads returns an empty list, not null.");
            Assert.That(zoneLoads.Count, Is.EqualTo(0));
        }

        [Test]
        public void ZoneLoads_NullComponent_ReturnsNull()
        {
            List<object> zoneLoads = TpdReflection.ZoneLoads_SystemComponent(null);

            Assert.That(zoneLoads, Is.Null, "A null component answers null, as it always has.");
        }

        [Test]
        public void ZoneLoads_NullLoadInTheMiddle_IsSkippedNotReturned()
        {
            // The two TSDData overloads in the same file skip nulls; the SystemComponent overload did not,
            // so a hole in the collection surfaced as a null element for every caller to trip over.
            FakeSystemComponent systemComponent = new FakeSystemComponent();
            systemComponent.Add(new FakeZoneLoad { Name = "First", GUID = "guid-First" });
            systemComponent.Add(null);
            systemComponent.Add(new FakeZoneLoad { Name = "Third", GUID = "guid-Third" });

            List<object> zoneLoads = TpdReflection.ZoneLoads_SystemComponent(systemComponent);

            Assert.That(
                zoneLoads.Any(x => x == null),
                Is.False,
                "A null zone load must be skipped, matching the TSDData overloads, not handed to the caller.");

            Assert.That(
                zoneLoads.Select(x => TpdReflection.Name(x)).ToList(),
                Is.EqualTo(new List<string> { "First", "Third" }));
        }
    }
}
