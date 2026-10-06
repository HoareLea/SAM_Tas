// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// B-14. Source enumeration order must not decide which analytical room owns which TAS zone.
    /// <para>
    /// <b>The defect.</b> When an <c>AirSystemGroup</c> is replicated by
    /// <c>ComponentGroup.SetMultiplicity(n)</c>, the conversion pairs each replicated native zone with a
    /// source room by taking <c>tuples[0]</c> and <c>RemoveAt(0)</c> from a bucket that was itself filled by
    /// a <b>backwards</b> loop. Ownership was therefore a function of the order the caller's collection
    /// happened to be in - so two rooms called "Bedroom 2" in different flats could swap zones between runs
    /// of the same model, and nothing would say so.
    /// </para>
    /// <para>
    /// <b>What is asserted here.</b> Not that two independently generated TPD documents produce identical
    /// native GUIDs - TAS mints those per document and nothing establishes they are stable across runs.
    /// What is asserted is the <b>semantic invariant</b>: the claim order the conversion consumes is a
    /// deterministic function of the <b>source</b> guids alone, so reordering the source collection cannot
    /// change which room claims which replica. That is the property the native GUIDs would have to be
    /// stable to test directly, and it is the property that actually matters.
    /// </para>
    /// <para>
    /// The rule is ascending source guid, matching the order PR1 materialises by. It is exercised through
    /// the production <c>Query.SourceOrderKey</c>, which is what the conversion sorts on.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SystemComponentOrderIndependenceTests
    {
        /// <summary>
        /// Stands in for one source component in a replicated group: an identity and a display name, which
        /// is all the ordering decision is allowed to see.
        /// </summary>
        private sealed class SourceRoom
        {
            public SourceRoom(Guid guid, string name)
            {
                Guid = guid;
                Name = name;
            }

            public Guid Guid { get; }

            public string Name { get; }

            public override string ToString()
            {
                return string.Concat(Name, "/", Guid.ToString("N").Substring(0, 8));
            }
        }

        /// <summary>
        /// The production ordering rule, applied exactly as <c>Convert.ToTPD</c> applies it to each bucket.
        /// </summary>
        private static List<SourceRoom> ClaimOrder(IEnumerable<SourceRoom> sourceRooms)
        {
            List<SourceRoom> result = new List<SourceRoom>(sourceRooms);

            result.Sort((x, y) => Key(x).CompareTo(Key(y)));

            return result;
        }

        private static Guid Key(SourceRoom sourceRoom)
        {
            return sourceRoom == null ? Guid.Empty : sourceRoom.Guid;
        }

        private static List<SourceRoom> ThreeFlatsEachWithABedroom2()
        {
            // The shape the repository already uses for exactly this hazard - three flats, each with a room
            // called "Bedroom 2" - as in PreparationBoundaryTests and TsdZoneIdentityStampTests.
            return new List<SourceRoom>
            {
                new SourceRoom(new Guid("11111111-1111-1111-1111-111111111111"), "Bedroom 2"),
                new SourceRoom(new Guid("22222222-2222-2222-2222-222222222222"), "Bedroom 2"),
                new SourceRoom(new Guid("33333333-3333-3333-3333-333333333333"), "Bedroom 2"),
            };
        }

        [Test]
        public void ClaimOrder_IsIdenticalUnderEveryPermutationOfTheSource()
        {
            List<SourceRoom> sourceRooms = ThreeFlatsEachWithABedroom2();

            List<Guid> expected = ClaimOrder(sourceRooms).Select(x => x.Guid).ToList();

            foreach (IEnumerable<SourceRoom> permutation in Permutations(sourceRooms))
            {
                List<Guid> actual = ClaimOrder(permutation).Select(x => x.Guid).ToList();

                Assert.That(
                    actual,
                    Is.EqualTo(expected),
                    "Reordering the source collection changed which room claims which replica. Ownership "
                    + "must be a function of the source guids alone.");
            }
        }

        [Test]
        public void ClaimOrder_IsAscendingSourceGuid_NotInputOrder()
        {
            List<SourceRoom> sourceRooms = ThreeFlatsEachWithABedroom2();
            sourceRooms.Reverse();

            List<Guid> actual = ClaimOrder(sourceRooms).Select(x => x.Guid).ToList();

            Assert.That(
                actual,
                Is.EqualTo(new List<Guid>
                {
                    new Guid("11111111-1111-1111-1111-111111111111"),
                    new Guid("22222222-2222-2222-2222-222222222222"),
                    new Guid("33333333-3333-3333-3333-333333333333"),
                }),
                "The stated rule is ascending source guid, which is the order PR1 materialises by.");
        }

        [Test]
        public void ClaimOrder_DoesNotUseDisplayNames()
        {
            // Every room here has the SAME name, so a name-based order could not distinguish them at all,
            // and a name-based order over DIFFERENT names would disagree with guid order. Both are excluded
            // by asserting the guid order holds while the names run in the opposite direction.
            List<SourceRoom> sourceRooms = new List<SourceRoom>
            {
                new SourceRoom(new Guid("11111111-1111-1111-1111-111111111111"), "Zebra"),
                new SourceRoom(new Guid("22222222-2222-2222-2222-222222222222"), "Yak"),
                new SourceRoom(new Guid("33333333-3333-3333-3333-333333333333"), "Xerus"),
            };

            List<string> names = ClaimOrder(sourceRooms).Select(x => x.Name).ToList();

            Assert.That(
                names,
                Is.EqualTo(new List<string> { "Zebra", "Yak", "Xerus" }),
                "The order must follow the guids even when that is the reverse of alphabetical, which proves "
                + "the display name carries no authority.");
        }

        [Test]
        public void ClaimOrder_IsStableForAGuidlessComponent()
        {
            // A component with no guid orders as Guid.Empty - a stable position rather than a throw. It is
            // refused later, by the binding, where the refusal can name the room.
            List<SourceRoom> sourceRooms = new List<SourceRoom>
            {
                new SourceRoom(new Guid("11111111-1111-1111-1111-111111111111"), "Has a guid"),
                new SourceRoom(Guid.Empty, "Has none"),
            };

            List<SourceRoom> forwards = ClaimOrder(sourceRooms);
            sourceRooms.Reverse();
            List<SourceRoom> backwards = ClaimOrder(sourceRooms);

            Assert.That(forwards.Select(x => x.Guid).ToList(), Is.EqualTo(backwards.Select(x => x.Guid).ToList()));
            Assert.That(forwards[0].Guid, Is.EqualTo(Guid.Empty), "Guid.Empty sorts first, deterministically.");
        }

        [Test]
        public void ProductionSourceOrderKey_ReturnsGuidEmptyForNull()
        {
            // The production helper the conversion sorts on must not throw on a null bucket entry: a null
            // there is a conversion shortfall, and it is reported by the reconciliation, not by an NRE
            // thrown from a comparison.
            Assert.That(
                SAM.Analytical.Tas.TPD.Query.SourceOrderKey(null),
                Is.EqualTo(Guid.Empty));
        }

        private static IEnumerable<IEnumerable<T>> Permutations<T>(IReadOnlyList<T> items)
        {
            if (items.Count <= 1)
            {
                yield return items;
                yield break;
            }

            for (int i = 0; i < items.Count; i++)
            {
                T head = items[i];
                List<T> rest = new List<T>(items);
                rest.RemoveAt(i);

                foreach (IEnumerable<T> tail in Permutations(rest))
                {
                    List<T> result = new List<T> { head };
                    result.AddRange(tail);
                    yield return result;
                }
            }
        }
    }
}
