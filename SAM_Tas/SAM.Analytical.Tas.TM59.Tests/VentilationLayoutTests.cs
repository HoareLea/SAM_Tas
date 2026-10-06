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
    /// The explicit route's TAS Systems drawing. Presentation only - measured on licensed TAS, moving
    /// every component leaves every ZoneTemperature unchanged - so what is pinned here is that the
    /// drawing is <b>inspectable</b>: deterministic, no box on another, rows in air-path order, and no
    /// routed duct passing through a box that is not one of its own two ends.
    /// <para>
    /// The fixture is the acceptance's five-room unit: a supply-only studio, an ensuite with supply AND
    /// extract, a hall reached only by transfer that branches to a bathroom and a bedroom, both
    /// extract-only. The trunk and box sizes are licensed TAS's own: zone 60, damper 40, junction 20,
    /// the template fans at x 390..660.
    /// </para>
    /// </summary>
    [TestFixture]
    public class VentilationLayoutTests
    {
        private static readonly Guid AirSystem = new Guid("0000000a-0000-0000-0000-000000000000");

        private static readonly Guid Studio = new Guid("00000001-0000-0000-0000-000000000000");
        private static readonly Guid Ensuite = new Guid("00000002-0000-0000-0000-000000000000");
        private static readonly Guid Hall = new Guid("00000003-0000-0000-0000-000000000000");
        private static readonly Guid Bathroom = new Guid("00000004-0000-0000-0000-000000000000");
        private static readonly Guid Bedroom = new Guid("00000005-0000-0000-0000-000000000000");

        //The template trunk as licensed TAS draws it, and its two branched ends: the supply damper's
        //outlet (left-to-right) and the return fan's inlet (the return fan is drawn right-to-left).
        private static readonly VentilationLayoutRectangle SupplyDamper = new VentilationLayoutRectangle(530, 90, 40, 40);
        private static readonly VentilationLayoutRectangle ReturnFan = new VentilationLayoutRectangle(600, 240, 60, 40);
        private static readonly VentilationLayoutRectangle FreshAirFan = new VentilationLayoutRectangle(390, 100, 60, 40);
        private static readonly VentilationLayoutRectangle Trunk = VentilationLayoutRectangle.Union(new[]
        {
            new VentilationLayoutRectangle(0, 110, 20, 20),
            new VentilationLayoutRectangle(0, 190, 20, 20),
            new VentilationLayoutRectangle(240, 200, 20, 20),
            ReturnFan,
            FreshAirFan,
            SupplyDamper,
        });

        private static SystemVentilationRoomIntent Room(Guid guid, double? supply, double? extract)
        {
            return new SystemVentilationRoomIntent(Guid.NewGuid(), guid, AirSystem, "load", supply, extract);
        }

        private static SystemVentilationLegIntent Leg(SystemVentilationConnectionType type, int index, Guid from, Guid to)
        {
            return new SystemVentilationLegIntent(type, new Guid(index, 0, 0, new byte[8]), AirSystem, from, to, Guid.NewGuid(), 10, Guid.NewGuid());
        }

        private static List<SystemVentilationRoomIntent> Rooms()
        {
            return new List<SystemVentilationRoomIntent>
            {
                Room(Studio, 13, null),
                Room(Ensuite, 31, 19),
                Room(Hall, null, null),
                Room(Bathroom, null, 15),
                Room(Bedroom, null, 10),
            };
        }

        private static List<SystemVentilationLegIntent> Legs()
        {
            return new List<SystemVentilationLegIntent>
            {
                Leg(SystemVentilationConnectionType.Supply, 1, Guid.Empty, Studio),
                Leg(SystemVentilationConnectionType.Supply, 2, Guid.Empty, Ensuite),
                Leg(SystemVentilationConnectionType.Extract, 3, Ensuite, Guid.Empty),
                Leg(SystemVentilationConnectionType.Extract, 4, Bathroom, Guid.Empty),
                Leg(SystemVentilationConnectionType.Extract, 5, Bedroom, Guid.Empty),
                Leg(SystemVentilationConnectionType.Transfer, 6, Studio, Hall),
                Leg(SystemVentilationConnectionType.Transfer, 7, Ensuite, Hall),
                Leg(SystemVentilationConnectionType.Transfer, 8, Hall, Bathroom),
                Leg(SystemVentilationConnectionType.Transfer, 9, Hall, Bedroom),
            };
        }

        private static VentilationLayout Plan(IEnumerable<SystemVentilationRoomIntent> rooms, IEnumerable<SystemVentilationLegIntent> legs)
        {
            return SAM.Analytical.Tas.TPD.Query.VentilationLayout(rooms, legs, Trunk, 60, 60, 40, 40);
        }

        private static string Describe(VentilationLayoutRectangle r) => r == null ? "<none>" : string.Format("{0},{1},{2},{3}", r.X, r.Y, r.Width, r.Height);

        [Test]
        public void TheDrawingIsTheSameWhateverOrderTheRoomsAndLegsArriveIn()
        {
            List<SystemVentilationRoomIntent> rooms = Rooms();
            List<SystemVentilationLegIntent> legs = Legs();

            VentilationLayout forward = Plan(rooms, legs);

            List<SystemVentilationRoomIntent> rooms_Reversed = Enumerable.Reverse(rooms).ToList();
            List<SystemVentilationLegIntent> legs_Reversed = Enumerable.Reverse(legs).ToList();
            VentilationLayout reversed = Plan(rooms_Reversed, legs_Reversed);

            foreach (SystemVentilationRoomIntent room in rooms)
            {
                Assert.That(Describe(reversed.Room(room.Guid_SystemSpace)), Is.EqualTo(Describe(forward.Room(room.Guid_SystemSpace))));
            }

            foreach (SystemVentilationLegIntent leg in legs)
            {
                Assert.That(Describe(reversed.Leg(leg.Guid_SystemConnection)), Is.EqualTo(Describe(forward.Leg(leg.Guid_SystemConnection))));
            }
        }

        [Test]
        public void NoBoxIsDrawnOnAnother_NorOnTheTrunk()
        {
            List<VentilationLayoutRectangle> boxes = Plan(Rooms(), Legs()).Rectangles.ToList();

            Assert.That(boxes.Count, Is.EqualTo(5 + 3 + 4), "five zones, three extract dampers, four transfer dampers - a supply leg has no damper");

            for (int i = 0; i < boxes.Count; i++)
            {
                Assert.That(boxes[i].Overlaps(Trunk), Is.False, Describe(boxes[i]) + " is drawn on the trunk");

                for (int j = i + 1; j < boxes.Count; j++)
                {
                    Assert.That(boxes[i].Overlaps(boxes[j]), Is.False, Describe(boxes[i]) + " overlaps " + Describe(boxes[j]));
                }
            }
        }

        [Test]
        public void RoomsAreRowsInAirPathOrder_SupplyThenBothThenTransferThenExtract()
        {
            VentilationLayout layout = Plan(Rooms(), Legs());

            Assert.Multiple(() =>
            {
                Assert.That(layout.Room(Studio).Y, Is.LessThan(layout.Room(Ensuite).Y));
                Assert.That(layout.Room(Ensuite).Y, Is.LessThan(layout.Room(Hall).Y));
                Assert.That(layout.Room(Hall).Y, Is.LessThan(layout.Room(Bathroom).Y));
                Assert.That(layout.Room(Bathroom).Y, Is.LessThan(layout.Room(Bedroom).Y), "extract-only rooms in system space guid order");
                Assert.That(new[] { Studio, Ensuite, Hall }.Select(x => layout.Room(x).X).Distinct().Count(), Is.EqualTo(1), "one column of occupied rooms");
                Assert.That(layout.Room(Bathroom).X, Is.EqualTo(layout.Room(Bedroom).X), "one column of extract-only rooms");
                Assert.That(
                    Legs().Where(x => x.ConnectionType == SystemVentilationConnectionType.Transfer).All(x => layout.Room(Bathroom).X > layout.Leg(x.Guid_SystemConnection).Right),
                    Is.True,
                    "extract-only rooms stand right of the occupied rooms and the transfer dampers beneath them");
            });
        }

        [Test]
        public void TransferDampersSitBeneathTheirRoom_ExtractDampersBesideIt()
        {
            VentilationLayout layout = Plan(Rooms(), Legs());

            foreach (SystemVentilationLegIntent leg in Legs())
            {
                VentilationLayoutRectangle damper = layout.Leg(leg.Guid_SystemConnection);

                if (leg.ConnectionType == SystemVentilationConnectionType.Supply)
                {
                    Assert.That(damper, Is.Null, "a supply leg's duty rides on its zone");
                    continue;
                }

                VentilationLayoutRectangle room = layout.Room(leg.Guid_SystemSpace_From);

                if (leg.ConnectionType == SystemVentilationConnectionType.Transfer)
                {
                    Assert.That(damper.Y, Is.GreaterThanOrEqualTo(room.Bottom));
                    Assert.That(damper.X, Is.GreaterThan(room.Right));
                }
                else
                {
                    Assert.That(damper.CentreY, Is.EqualTo(room.CentreY), "an extract damper is level with its room");
                }
            }
        }

        //------------------------------------------------------------------ routes

        private static List<int[]> Polyline(VentilationLayoutRectangle up, bool upLeftToRight, VentilationLayoutRectangle down, bool downLeftToRight, bool downIsJunction)
        {
            List<int[]> result = new List<int[]> { new[] { upLeftToRight ? up.Right : up.X, up.CentreY } };
            result.AddRange(SAM.Analytical.Tas.TPD.Query.VentilationDuctRoute(up, upLeftToRight, down, downLeftToRight, downIsJunction));
            result.Add(new[] { downLeftToRight ? down.X : down.Right, down.CentreY });
            return result;
        }

        private static bool Through(int[] a, int[] b, VentilationLayoutRectangle box)
        {
            if (a[1] == b[1])
            {
                int y = a[1];
                int x1 = global::System.Math.Min(a[0], b[0]), x2 = global::System.Math.Max(a[0], b[0]);
                return y > box.Y && y < box.Bottom && x1 < box.Right && x2 > box.X;
            }

            if (a[0] == b[0])
            {
                int x = a[0];
                int y1 = global::System.Math.Min(a[1], b[1]), y2 = global::System.Math.Max(a[1], b[1]);
                return x > box.X && x < box.Right && y1 < box.Bottom && y2 > box.Y;
            }

            Assert.Fail("a routed duct segment is not orthogonal: " + a[0] + "," + a[1] + " -> " + b[0] + "," + b[1]);
            return true;
        }

        private static void AssertClear(string what, List<int[]> polyline, IEnumerable<VentilationLayoutRectangle> others)
        {
            foreach (VentilationLayoutRectangle box in others)
            {
                for (int i = 0; i + 1 < polyline.Count; i++)
                {
                    Assert.That(Through(polyline[i], polyline[i + 1], box), Is.False, what + " passes through " + Describe(box));
                }
            }
        }

        [Test]
        public void EveryRoutedDuctStaysClearOfEveryBoxThatIsNotOneOfItsEnds()
        {
            VentilationLayout layout = Plan(Rooms(), Legs());

            //The branch junctions Create.Ducts adds, placed the way it places them.
            VentilationLayoutRectangle supplyJunction = SAM.Analytical.Tas.TPD.Query.VentilationJunctionRectangle(SupplyDamper, true, true, 20, 20);
            VentilationLayoutRectangle returnJunction = SAM.Analytical.Tas.TPD.Query.VentilationJunctionRectangle(ReturnFan, false, false, 20, 20);
            VentilationLayoutRectangle hallIn = SAM.Analytical.Tas.TPD.Query.VentilationJunctionRectangle(layout.Room(Hall), true, false, 20, 20);
            VentilationLayoutRectangle hallOut = SAM.Analytical.Tas.TPD.Query.VentilationJunctionRectangle(layout.Room(Hall), true, true, 20, 20);
            VentilationLayoutRectangle ensuiteOut = SAM.Analytical.Tas.TPD.Query.VentilationJunctionRectangle(layout.Room(Ensuite), true, true, 20, 20);

            List<VentilationLayoutRectangle> all = layout.Rectangles.ToList();
            all.AddRange(new[] { supplyJunction, returnJunction, hallIn, hallOut, ensuiteOut, ReturnFan, FreshAirFan, SupplyDamper });

            void Check(string what, VentilationLayoutRectangle up, bool upLeftToRight, VentilationLayoutRectangle down, bool downLeftToRight, bool downIsJunction)
            {
                AssertClear(what, Polyline(up, upLeftToRight, down, downLeftToRight, downIsJunction), all.Where(x => x != up && x != down));
            }

            List<SystemVentilationLegIntent> legs = Legs();
            VentilationLayoutRectangle D(int index) => layout.Leg(legs[index - 1].Guid_SystemConnection);

            Assert.Multiple(() =>
            {
                Check("supply to the studio", supplyJunction, true, layout.Room(Studio), true, false);
                Check("supply to the ensuite", supplyJunction, true, layout.Room(Ensuite), true, false);

                Check("studio to its transfer damper", layout.Room(Studio), true, D(6), true, false);
                Check("studio's transfer into the hall", D(6), true, hallIn, true, true);
                Check("ensuite's transfer into the hall", D(7), true, hallIn, true, true);
                Check("ensuite to its transfer damper", ensuiteOut, true, D(7), true, false);
                Check("ensuite to its extract damper", ensuiteOut, true, D(3), true, false);

                Check("hall to its first transfer damper", hallOut, true, D(8), true, false);
                Check("hall to its second transfer damper", hallOut, true, D(9), true, false);
                Check("hall's transfer into the bathroom", D(8), true, layout.Room(Bathroom), true, false);
                Check("hall's transfer into the bedroom", D(9), true, layout.Room(Bedroom), true, false);

                Check("bathroom to its extract damper", layout.Room(Bathroom), true, D(4), true, false);
                Check("bedroom to its extract damper", layout.Room(Bedroom), true, D(5), true, false);

                Check("ensuite's extract back to the return fan", D(3), true, returnJunction, false, true);
                Check("bathroom's extract back to the return fan", D(4), true, returnJunction, false, true);
                Check("bedroom's extract back to the return fan", D(5), true, returnJunction, false, true);
            });
        }

        /// <summary>
        /// The real-design case the synthetic fixture never produced: a room that EXTRACTS and still
        /// passes air on - a kitchen extracting 55 l/s that transfers 8 l/s to an ensuite. It is in the
        /// middle of the air path, not at the end of it, so it must stand in the room column with its
        /// transfer damper to its right; standing it in the extract-only column past the transfer column
        /// forced its own outgoing duct to double back through its own box.
        /// </summary>
        [Test]
        public void ARoomThatExtractsAndStillPassesAirOnStaysInTheRoomColumn()
        {
            Guid guid_Bedroom = new Guid("11111111-1111-1111-1111-111111111111");
            Guid guid_Kitchen = new Guid("22222222-2222-2222-2222-222222222222");
            Guid guid_Ensuite = new Guid("33333333-3333-3333-3333-333333333333");

            List<SystemVentilationRoomIntent> rooms = new List<SystemVentilationRoomIntent>
            {
                Room(guid_Bedroom, 63, null),
                Room(guid_Kitchen, null, 55),
                Room(guid_Ensuite, null, 8),
            };

            List<SystemVentilationLegIntent> legs = new List<SystemVentilationLegIntent>
            {
                Leg(SystemVentilationConnectionType.Supply, 101, Guid.Empty, guid_Bedroom),
                Leg(SystemVentilationConnectionType.Transfer, 102, guid_Bedroom, guid_Kitchen),
                Leg(SystemVentilationConnectionType.Transfer, 103, guid_Kitchen, guid_Ensuite),
                Leg(SystemVentilationConnectionType.Extract, 104, guid_Kitchen, Guid.Empty),
                Leg(SystemVentilationConnectionType.Extract, 105, guid_Ensuite, Guid.Empty),
            };

            VentilationLayout layout = Plan(rooms, legs);

            VentilationLayoutRectangle bedroom = layout.Room(guid_Bedroom);
            VentilationLayoutRectangle kitchen = layout.Room(guid_Kitchen);
            VentilationLayoutRectangle ensuite = layout.Room(guid_Ensuite);

            VentilationLayoutRectangle damper_Kitchen = layout.Leg(legs[2].Guid_SystemConnection);

            Assert.Multiple(() =>
            {
                Assert.That(kitchen.X, Is.EqualTo(bedroom.X), "a room that passes air on stands in the room column");
                Assert.That(ensuite.X, Is.GreaterThan(kitchen.X), "the room the path ENDS at stands in the extract-only column");

                //Air-path order down the drawing: supplied room, then the room it transfers into, then the
                //room that one transfers into.
                Assert.That(kitchen.Y, Is.GreaterThan(bedroom.Y));
                Assert.That(ensuite.Y, Is.GreaterThan(kitchen.Y));

                Assert.That(damper_Kitchen.X, Is.GreaterThan(kitchen.Right), "its transfer damper is to its right");
            });

            //And the duct out of the kitchen's own outlet junction into that damper is clear of every box
            //that is not one of its two ends - which is exactly what it was not.
            VentilationLayoutRectangle junction_Kitchen = SAM.Analytical.Tas.TPD.Query.VentilationJunctionRectangle(kitchen, true, true, 20, 20);

            List<VentilationLayoutRectangle> others = new List<VentilationLayoutRectangle> { bedroom, kitchen, ensuite };

            AssertClear(
                "the kitchen's outgoing transfer",
                Polyline(junction_Kitchen, true, damper_Kitchen, true, false),
                others);
        }

        [Test]
        public void AForwardDuctTurnsOnce_ABackwardDuctUsesTheLaneAboveItsTarget()
        {
            VentilationLayoutRectangle left = new VentilationLayoutRectangle(0, 0, 40, 40);
            VentilationLayoutRectangle right = new VentilationLayoutRectangle(200, 200, 60, 60);

            List<int[]> forward = SAM.Analytical.Tas.TPD.Query.VentilationDuctRoute(left, true, right, true, false);
            List<int[]> backward = SAM.Analytical.Tas.TPD.Query.VentilationDuctRoute(right, true, left, true, false);

            Assert.Multiple(() =>
            {
                Assert.That(forward.Count, Is.EqualTo(2));
                Assert.That(forward[0], Is.EqualTo(new[] { 180, 20 }));
                Assert.That(forward[1], Is.EqualTo(new[] { 180, 230 }));

                Assert.That(backward.Count, Is.EqualTo(4));
                Assert.That(backward[1][1], Is.EqualTo(-20), "the lane just above the target");
                Assert.That(backward[2][0], Is.EqualTo(-20), "the target's inlet lead");
            });
        }
    }
}
