// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// SAM #113. TAS Systems sizing and simulation depend on the order components and ducts are created in;
    /// the explicit route used to order them by guid, so two identity sets for one network could size in one
    /// and fail (or never terminate) in the other. <c>Query.CreationOrder</c> must be a function of the graph
    /// alone: the same network under any identities, supplied in any order, is created in one order.
    /// </summary>
    [TestFixture]
    public class VentilationCreationOrderTests
    {
        /// <summary>
        /// One dwelling as the explicit route draws it, by role: a fresh-air source, an exchanger, a supply fan
        /// into two rooms, extract and transfer legs back through a return fan, and a same-zone recirculation
        /// loop (room -> return damper -> mixing junction -> coil -> fan -> split -> supply damper -> same room).
        /// </summary>
        private static readonly string[] Roles =
        {
            "source", "exchanger", "supplyFan", "supplySplit", "roomA", "roomB", "transferAB", "extractJunction",
            "extractB", "returnFan", "exhaust", "recircReturnA", "recircReturnB", "mix", "coil", "recircFan",
            "recircSplit", "recircSupplyA", "recircSupplyB",
        };

        private static readonly (string Up, int UpIndex, string Down, int DownIndex)[] Paths =
        {
            ("source", 0, "exchanger", 0), ("exchanger", 1, "supplyFan", 0), ("supplyFan", 1, "supplySplit", 0),
            ("supplySplit", 1, "roomA", 0), ("supplySplit", 1, "roomB", 0),
            ("roomA", 1, "transferAB", 0), ("transferAB", 1, "roomB", 0),
            ("roomB", 1, "extractB", 0), ("extractB", 1, "extractJunction", 0), ("extractJunction", 1, "returnFan", 0),
            ("returnFan", 1, "exchanger", 2), ("exchanger", 3, "exhaust", 0),
            ("roomA", 1, "recircReturnA", 0), ("roomB", 1, "recircReturnB", 0),
            ("recircReturnA", 1, "mix", 0), ("recircReturnB", 1, "mix", 0), ("mix", 1, "coil", 0), ("coil", 1, "recircFan", 0),
            ("recircFan", 1, "recircSplit", 0), ("recircSplit", 1, "recircSupplyA", 0), ("recircSplit", 1, "recircSupplyB", 0),
            ("recircSupplyA", 1, "roomA", 0), ("recircSupplyB", 1, "roomB", 0),
        };

        /// <summary>What a component IS: its kind, and for rooms the analytical space it stands for. Never a guid.</summary>
        private static string Identity(string role)
        {
            switch (role)
            {
                case "roomA": return "Space|analytical-A";
                case "roomB": return "Space|analytical-B";
                case "supplyFan": case "returnFan": case "recircFan": return "Fan";
                case "recircReturnA": case "recircReturnB": case "recircSupplyA": case "recircSupplyB": case "transferAB": case "extractB": return "Damper";
                case "supplySplit": case "extractJunction": case "mix": case "recircSplit": case "source": case "exhaust": return "Junction";
                default: return role;
            }
        }

        private static List<string> Order(int seed, bool shuffle)
        {
            Random random = new Random(seed);
            Dictionary<string, Guid> guid = Roles.ToDictionary(x => x, x => NewGuid(random));
            Dictionary<Guid, string> role = guid.ToDictionary(x => x.Value, x => x.Key);

            List<Guid> nodes = Roles.Select(x => guid[x]).ToList();
            List<CreationOrderEdge> edges = Paths.Select(x => new CreationOrderEdge(guid[x.Up], x.UpIndex, guid[x.Down], x.DownIndex)).ToList();
            if (shuffle)
            {
                nodes = nodes.OrderBy(x => random.Next()).ToList();
                edges = edges.OrderBy(x => random.Next()).ToList();
            }

            return TPD.Query.CreationOrder(nodes, edges, x => Identity(role[x])).Select(x => role[x]).ToList();
        }

        private static Guid NewGuid(Random random)
        {
            byte[] bytes = new byte[16];
            random.NextBytes(bytes);
            return new Guid(bytes);
        }

        [Test]
        public void CreationOrder_IsTheSameForEveryIdentitySet()
        {
            List<string> expected = Order(1, false);

            for (int seed = 2; seed < 200; seed++)
            {
                Assert.That(Order(seed, false), Is.EqualTo(expected), "Drawing different guids for the same network changed its native creation order (seed " + seed + ").");
            }
        }

        [Test]
        public void CreationOrder_IsTheSameForEveryEnumerationOrder()
        {
            List<string> expected = Order(1, false);

            for (int seed = 1; seed < 200; seed++)
            {
                Assert.That(Order(seed, true), Is.EqualTo(expected), "Supplying the same network in a different order changed its native creation order (seed " + seed + ").");
            }
        }

        [Test]
        public void CreationOrder_FollowsTheAirPathFromTheSource()
        {
            List<string> order = Order(7, true);

            Assert.That(order.Count, Is.EqualTo(Roles.Length), "Every component is created exactly once.");
            Assert.That(order.Distinct().Count(), Is.EqualTo(Roles.Length));
            Assert.That(order[0], Is.EqualTo("source"), "The only component nothing flows into comes first.");

            //breadth-first from the source: every component after the first is reached from one created before it
            HashSet<string> created = new HashSet<string>();
            foreach (string role in order)
            {
                if (created.Count != 0)
                {
                    Assert.That(Paths.Any(x => x.Down == role && created.Contains(x.Up)), Is.True, role + " was created before anything upstream of it.");
                }

                created.Add(role);
            }
        }

        [Test]
        public void CreationOrder_RoomsAreRankedByWhatTheyServe_NotByGuid()
        {
            //Two structurally identical rooms (source -> split -> room 1 | room 2 -> mix -> sink): only the analytical
            //identity each room stands for can tell them apart. Whichever room carries identity X must take the same
            //position under every identity set and whichever way round X and Y are assigned - the tie is broken by
            //what the room serves, never by its guid.
            int? position_X = null;

            for (int seed = 1; seed < 100; seed++)
            {
                foreach (bool swap in new[] { false, true })
                {
                    Random random = new Random(seed);
                    Guid source = NewGuid(random), split = NewGuid(random), room_1 = NewGuid(random), room_2 = NewGuid(random), mix = NewGuid(random), sink = NewGuid(random);
                    Guid room_X = swap ? room_2 : room_1;

                    Dictionary<Guid, string> identity = new Dictionary<Guid, string>
                    {
                        [source] = "Junction", [split] = "Junction", [mix] = "Junction", [sink] = "Junction",
                        [room_1] = swap ? "Space|analytical-Y" : "Space|analytical-X",
                        [room_2] = swap ? "Space|analytical-X" : "Space|analytical-Y",
                    };

                    List<CreationOrderEdge> edges = new List<CreationOrderEdge>
                    {
                        new CreationOrderEdge(source, 1, split, 0), new CreationOrderEdge(split, 1, room_1, 0), new CreationOrderEdge(split, 1, room_2, 0),
                        new CreationOrderEdge(room_1, 1, mix, 0), new CreationOrderEdge(room_2, 1, mix, 0), new CreationOrderEdge(mix, 1, sink, 0),
                    };

                    List<Guid> order = TPD.Query.CreationOrder(identity.Keys.OrderBy(x => random.Next()), edges.OrderBy(x => random.Next()), x => identity[x]);
                    int position = order.IndexOf(room_X);

                    position_X = position_X ?? position;
                    Assert.That(position, Is.EqualTo(position_X.Value), "The room serving X moved with its guid (seed " + seed + ", swap " + swap + ").");
                }
            }
        }

        [Test]
        public void CreationOrder_ToleratesNullAndDanglingInput()
        {
            Guid x = Guid.NewGuid(), y = Guid.NewGuid();

            Assert.That(TPD.Query.CreationOrder(null, null, null), Is.Empty);
            Assert.That(
                TPD.Query.CreationOrder(new[] { x, y, x }, new[] { null, new CreationOrderEdge(x, 0, Guid.NewGuid(), 0), new CreationOrderEdge(x, 0, y, 0) }, null),
                Is.EqualTo(new List<Guid> { x, y }));
        }
    }
}
