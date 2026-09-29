// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Tas.TPD;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Where the three manufacturer-guidance controllers are drawn. Presentation only - but before 2026-09-29
    /// they were never positioned, so TAS drew all three on top of each other. Pinned: a row below the unit,
    /// each under the component it controls, no box on another, and the same drawing every time.
    /// <para>
    /// The fixture is the real 3-dwelling project's MVHR-01 as licensed TAS drew it (2026-09-29 layout dump):
    /// the MVRE trunk, the guidance coil between the exchanger and the supply fan, the rooms and dampers the
    /// ventilation layout placed below and to the right.
    /// </para>
    /// </summary>
    [TestFixture]
    public class GuidanceControllerLayoutTests
    {
        private const int Size = 40;

        private static readonly VentilationLayoutRectangle Exchanger = new VentilationLayoutRectangle(160, 100, 40, 120);
        private static readonly VentilationLayoutRectangle Coil = new VentilationLayoutRectangle(275, 100, 30, 40);
        private static readonly VentilationLayoutRectangle SupplyFan = new VentilationLayoutRectangle(390, 100, 60, 40);
        private static readonly VentilationLayoutRectangle ExtractFan = new VentilationLayoutRectangle(600, 240, 60, 40);

        private static List<VentilationLayoutRectangle> Drawn()
        {
            return new List<VentilationLayoutRectangle>
            {
                new VentilationLayoutRectangle(0, 110, 20, 20),
                Exchanger,
                Coil,
                new VentilationLayoutRectangle(0, 190, 20, 20),
                SupplyFan,
                new VentilationLayoutRectangle(530, 90, 40, 40),
                new VentilationLayoutRectangle(820, 380, 60, 60),
                new VentilationLayoutRectangle(1200, 390, 40, 40),
                new VentilationLayoutRectangle(980, 450, 40, 40),
                ExtractFan,
                new VentilationLayoutRectangle(1060, 550, 60, 60),
                new VentilationLayoutRectangle(240, 200, 20, 20),
                new VentilationLayoutRectangle(1200, 560, 40, 40),
                new VentilationLayoutRectangle(900, 400, 20, 20),
                new VentilationLayoutRectangle(690, 250, 20, 20),
            };
        }

        //In controller order: the DX coil's, the supply fan's, the extract fan's.
        private static List<VentilationLayoutRectangle> Controlled()
        {
            return new List<VentilationLayoutRectangle> { Coil, SupplyFan, ExtractFan };
        }

        private static void AssertNoOverlap(List<VentilationLayoutRectangle> controllers, List<VentilationLayoutRectangle> drawn)
        {
            for (int i = 0; i < controllers.Count; i++)
            {
                foreach (VentilationLayoutRectangle box in drawn)
                {
                    Assert.That(controllers[i].Overlaps(box), Is.False, $"controller {i} ({controllers[i].X}, {controllers[i].Y}) is drawn on a component at ({box.X}, {box.Y})");
                }

                for (int j = i + 1; j < controllers.Count; j++)
                {
                    Assert.That(controllers[i].Overlaps(controllers[j]), Is.False, $"controllers {i} and {j} are drawn on each other");
                }
            }
        }

        [Test]
        public void TheControllers_AreARowBelowTheUnit_ClearOfEveryComponentAndEachOther()
        {
            List<VentilationLayoutRectangle> controllers = SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(Controlled(), Drawn(), Size, Size);

            Assert.That(controllers, Has.Count.EqualTo(3));
            Assert.That(controllers, Has.None.Null);

            int bottom_Unit = Controlled().Max(x => x.Bottom);
            Assert.That(controllers.Select(x => x.Y).Distinct().Count(), Is.EqualTo(1), "one row");
            Assert.That(controllers[0].Y, Is.GreaterThan(bottom_Unit), "below the unit, the extract fan included");

            AssertNoOverlap(controllers, Drawn());
        }

        [Test]
        public void EachController_IsUnderTheComponentItControls_InTheSameLeftToRightOrder()
        {
            List<VentilationLayoutRectangle> controlled = Controlled();
            List<VentilationLayoutRectangle> controllers = SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(controlled, Drawn(), Size, Size);

            for (int i = 0; i < controlled.Count; i++)
            {
                int centre_Controlled = controlled[i].X + (controlled[i].Width / 2);
                Assert.That(controllers[i].X + (Size / 2), Is.EqualTo(centre_Controlled), $"controller {i} is centred under its component");
            }

            Assert.That(controllers[0].X, Is.LessThan(controllers[1].X));
            Assert.That(controllers[1].X, Is.LessThan(controllers[2].X));
        }

        [Test]
        public void TheDrawing_IsTheSameEveryTime_WhateverOrderTheComponentsAreGivenIn()
        {
            List<VentilationLayoutRectangle> first = SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(Controlled(), Drawn(), Size, Size);

            List<VentilationLayoutRectangle> drawn_Reversed = Drawn();
            drawn_Reversed.Reverse();
            List<VentilationLayoutRectangle> second = SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(Controlled(), drawn_Reversed, Size, Size);

            Assert.That(second.Select(x => (x.X, x.Y, x.Width, x.Height)), Is.EqualTo(first.Select(x => (x.X, x.Y, x.Width, x.Height))));
        }

        [Test]
        public void ControllersOfComponentsInOneColumn_ArePushedApartByALane()
        {
            //A unit drawn with the coil directly above the supply fan: both controllers would sit at one X.
            VentilationLayoutRectangle coil = new VentilationLayoutRectangle(390, 20, 60, 40);
            List<VentilationLayoutRectangle> controlled = new List<VentilationLayoutRectangle> { coil, SupplyFan, ExtractFan };

            List<VentilationLayoutRectangle> controllers = SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(controlled, controlled, Size, Size);

            AssertNoOverlap(controllers, controlled);
            Assert.That(controllers[1].X - controllers[0].Right, Is.GreaterThanOrEqualTo(Size), "a lane between neighbours for their control arcs");
        }

        [Test]
        public void AComponentInTheBand_MovesTheWholeRowDown_NotIntoIt()
        {
            List<VentilationLayoutRectangle> controllers_Free = SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(Controlled(), Drawn(), Size, Size);

            //A room drawn right where the row would go, under the supply fan.
            List<VentilationLayoutRectangle> drawn = Drawn();
            drawn.Add(new VentilationLayoutRectangle(controllers_Free[1].X, controllers_Free[1].Y, 60, 60));

            List<VentilationLayoutRectangle> controllers = SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(Controlled(), drawn, Size, Size);

            AssertNoOverlap(controllers, drawn);
            Assert.That(controllers[0].Y, Is.GreaterThan(controllers_Free[0].Y));
            Assert.That(controllers.Select(x => x.Y).Distinct().Count(), Is.EqualTo(1), "still one row");
        }

        [Test]
        public void AComponentTASWouldNotSize_LeavesOnlyItsControllerUnplaced()
        {
            List<VentilationLayoutRectangle> controlled = new List<VentilationLayoutRectangle> { Coil, null, ExtractFan };

            List<VentilationLayoutRectangle> controllers = SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(controlled, Drawn(), Size, Size);

            Assert.That(controllers, Has.Count.EqualTo(3));
            Assert.That(controllers[1], Is.Null);
            Assert.That(controllers[0], Is.Not.Null);
            Assert.That(controllers[2], Is.Not.Null);
            AssertNoOverlap(new List<VentilationLayoutRectangle> { controllers[0], controllers[2] }, Drawn());
        }

        [Test]
        public void NothingToPlace_IsNotAnError()
        {
            Assert.That(SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(null, Drawn(), Size, Size), Is.Empty);
            Assert.That(SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(new List<VentilationLayoutRectangle> { null }, Drawn(), Size, Size), Has.All.Null);
            Assert.That(SAM.Analytical.Tas.TPD.Query.GuidanceControllerLayout(Controlled(), null, Size, Size), Has.None.Null);
        }
    }
}
