// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using AperturePhysicalIndex = SAM.Analytical.Tas.AperturePhysicalIndex;
using TasModify = SAM.Analytical.Tas.Modify;
using TasQuery = SAM.Analytical.Tas.Query;
using ZoneSurfaceKey = SAM.Analytical.Tas.ZoneSurfaceKey;
using ZoneSurfaceReference = SAM.Core.Tas.ZoneSurfaceReference;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Stage 3 - what a PHYSICAL aperture is, and the two rules that keep two identical windows apart.</b>
    /// <para>
    /// Physical identity is <c>{ ZoneGuid, SurfaceNumber }</c> and nothing else. After Stage 2 a building
    /// element, a construction, an aperture type, a definition name and a surface area are all properties of a
    /// SHARED DEFINITION or of a shape - two hundred identical windows legitimately agree on every one of
    /// them - so any of them used as identity pairs the wrong window. These tests pin that the pair does
    /// identify, that a half of it does not, and that an ambiguous pair REFUSES rather than picking a
    /// claimant.
    /// </para>
    /// <para>
    /// The second rule is the <c>_1</c>/<c>_2</c> slots: a slot is a SIDE and a side is a ZONE. Every write
    /// path - the direct export, the TBD import and <c>UpdateIds</c> - goes through
    /// <see cref="TasModify.SetApertureZoneSurfaceReferences(Aperture, AperturePart, IEnumerable{ZoneSurfaceReference}, out string)"/>,
    /// so the tests here that fix the slot order fix it for all three at once. They also pin the two defects
    /// that made the slots unreliable: filling only an EMPTY slot left a previous run's stale stamp in place,
    /// and taking slots in arrival order let a repeated update swap the sides.
    /// </para>
    /// <para>
    /// COM-free throughout. <c>Aperture</c> and <c>ZoneSurfaceReference</c> are plain SAM value objects, and
    /// every decision under test is a pure function over them - which is the point: this is the behaviour
    /// whose failures are silent and expensive, so it must be checkable without an installed TAS.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ApertureInstanceIdentityTests
    {
        private const string ZoneA = "11111111-1111-1111-1111-111111111111";
        private const string ZoneB = "22222222-2222-2222-2222-222222222222";
        private const string ZoneC = "33333333-3333-3333-3333-333333333333";

        // =================================================================================================
        // Builders
        // =================================================================================================

        private static ApertureConstruction Glazing()
        {
            return new ApertureConstruction(
                System.Guid.NewGuid(),
                "SIM_EXT_GLZ",
                ApertureType.Window,
                new List<ConstructionLayer> { new ConstructionLayer("Glass 6mm", 0.006) },
                new List<ConstructionLayer> { new ConstructionLayer("Timber", 0.05) });
        }

        /// <summary>An aperture whose geometry is identical bar an offset - the collision case that matters.</summary>
        private static Aperture Window(double offset = 0)
        {
            return new Aperture(Glazing(), new Polygon3D(new List<Point3D>
            {
                new Point3D(offset, 0, 0),
                new Point3D(offset + 1, 0, 0),
                new Point3D(offset + 1, 0, 1),
                new Point3D(offset, 0, 1)
            }));
        }

        private static Aperture Stamped(AperturePart aperturePart, params ZoneSurfaceReference[] zoneSurfaceReferences)
        {
            Aperture aperture = Window();
            aperture.SetApertureZoneSurfaceReferences(aperturePart, zoneSurfaceReferences, out string _);
            return aperture;
        }

        private static ZoneSurfaceReference Reference(string zoneGuid, int surfaceNumber)
        {
            return new ZoneSurfaceReference(surfaceNumber, zoneGuid);
        }

        private static ZoneSurfaceReference Read(Aperture aperture, AperturePart aperturePart, int side)
        {
            aperture.TryGetValue(TasQuery.ApertureZoneSurfaceReferenceParameter(aperturePart, side), out ZoneSurfaceReference result);
            return result;
        }

        // =================================================================================================
        // ZoneSurfaceKey - the pair, and why neither half will do
        // =================================================================================================

        [Test]
        public void Key_SameZoneAndNumber_IsTheSameSurface()
        {
            Assert.That(new ZoneSurfaceKey(ZoneA, 5), Is.EqualTo(new ZoneSurfaceKey(ZoneA, 5)));
            Assert.That(new ZoneSurfaceKey(ZoneA, 5).GetHashCode(), Is.EqualTo(new ZoneSurfaceKey(ZoneA, 5).GetHashCode()));
        }

        [Test]
        public void Key_SameNumberDifferentZone_IsADifferentSurface()
        {
            //The whole reason the type exists: TAS numbers surfaces PER ZONE, so this pair of keys names two
            //different physical surfaces that happen to share a number.
            Assert.That(new ZoneSurfaceKey(ZoneA, 5), Is.Not.EqualTo(new ZoneSurfaceKey(ZoneB, 5)));
        }

        [Test]
        public void Key_SameZoneDifferentNumber_IsADifferentSurface()
        {
            Assert.That(new ZoneSurfaceKey(ZoneA, 5), Is.Not.EqualTo(new ZoneSurfaceKey(ZoneA, 6)));
        }

        [TestCase("{11111111-1111-1111-1111-111111111111}")]
        [TestCase("11111111-1111-1111-1111-111111111111")]
        [TestCase("11111111-1111-1111-1111-111111111111 ")]
        [TestCase("11111111-1111-1111-1111-111111111111")]
        [TestCase("{11111111-1111-1111-1111-111111111111} ")]
        public void Key_ZoneGuidSpelling_DoesNotMakeTwoZones(string zoneGuid)
        {
            //A stamp written by one path and read by another must not fail to resolve over braces or case.
            Assert.That(new ZoneSurfaceKey(zoneGuid, 5), Is.EqualTo(new ZoneSurfaceKey(ZoneA, 5)));
        }

        [Test]
        public void Key_NonGuidZoneIdentifier_IsKeptRatherThanDiscarded()
        {
            //A foreign TBD may identify a zone with anything. An opaque identifier still identifies, as long
            //as both sides spell it the same way.
            Assert.That(new ZoneSurfaceKey("zone-7", 5).IsValid, Is.True);
            Assert.That(new ZoneSurfaceKey("zone-7", 5), Is.EqualTo(new ZoneSurfaceKey("ZONE-7", 5)));
            Assert.That(new ZoneSurfaceKey("zone-7", 5), Is.Not.EqualTo(new ZoneSurfaceKey("zone-8", 5)));
        }

        [TestCase(null, 5)]
        [TestCase("", 5)]
        [TestCase("   ", 5)]
        [TestCase(ZoneA, -1)]
        public void Key_HalfPopulated_IsNotAKeyAtAll(string zoneGuid, int surfaceNumber)
        {
            //A half-populated stamp must NOT become a key that matches everything - that is precisely how two
            //windows would cross-bind. The factory answers null and callers refuse.
            Assert.That(new ZoneSurfaceKey(zoneGuid, surfaceNumber).IsValid, Is.False);
            Assert.That(TasQuery.ZoneSurfaceKey(zoneGuid, surfaceNumber), Is.Null);
            Assert.That(TasQuery.ZoneSurfaceKey(Reference(zoneGuid, surfaceNumber)), Is.Null);
        }

        // =================================================================================================
        // AperturePhysicalIndex - resolves exactly one, or refuses
        // =================================================================================================

        [Test]
        public void Index_ValidKey_ResolvesExactlyOneApertureWithItsPartAndSide()
        {
            Aperture aperture_Pane = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));
            aperture_Pane.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 6) }, out string _);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture_Pane });

            Assert.That(index.TryResolve(new ZoneSurfaceKey(ZoneA, 5), out System.Guid guid, out AperturePart part, out int side, out string refusal), Is.True);
            Assert.That(guid, Is.EqualTo(aperture_Pane.Guid));
            Assert.That(part, Is.EqualTo(AperturePart.Pane));
            Assert.That(side, Is.EqualTo(1));
            Assert.That(refusal, Is.Null);
        }

        [Test]
        public void Index_WrongZone_DoesNotMatch()
        {
            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { Stamped(AperturePart.Pane, Reference(ZoneA, 5)) });

            Assert.That(index.TryResolve(new ZoneSurfaceKey(ZoneB, 5), out System.Guid _, out AperturePart _, out int _, out string refusal), Is.False);
            Assert.That(refusal, Is.Null, "An unknown surface is not an error - a native TBD looks exactly like this.");
        }

        [Test]
        public void Index_WrongSurfaceNumber_DoesNotMatch()
        {
            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { Stamped(AperturePart.Pane, Reference(ZoneA, 5)) });

            Assert.That(index.TryResolve(new ZoneSurfaceKey(ZoneA, 6), out System.Guid _, out AperturePart _, out int _, out string _), Is.False);
        }

        [Test]
        public void Index_SideOneAndSideTwo_AreDistinguished()
        {
            //ZoneA sorts before ZoneB, so ZoneA is side 1 - by the zone, not by which was passed first.
            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { Stamped(AperturePart.Pane, Reference(ZoneB, 9), Reference(ZoneA, 5)) });

            index.TryResolve(new ZoneSurfaceKey(ZoneA, 5), out System.Guid _, out AperturePart _, out int side_A, out string _);
            index.TryResolve(new ZoneSurfaceKey(ZoneB, 9), out System.Guid _, out AperturePart _, out int side_B, out string _);

            Assert.That(side_A, Is.EqualTo(1));
            Assert.That(side_B, Is.EqualTo(2));
        }

        [Test]
        public void Index_PaneAndFrame_AreDistinguished()
        {
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5));
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 6) }, out string _);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });

            index.TryResolve(new ZoneSurfaceKey(ZoneA, 5), out System.Guid _, out AperturePart part_5, out int _, out string _);
            index.TryResolve(new ZoneSurfaceKey(ZoneA, 6), out System.Guid _, out AperturePart part_6, out int _, out string _);

            Assert.That(part_5, Is.EqualTo(AperturePart.Pane));
            Assert.That(part_6, Is.EqualTo(AperturePart.Frame));
        }

        [Test]
        public void Index_TwoAperturesClaimingOneSurface_RefusesRatherThanChoosing()
        {
            //THE central refusal. Picking the first claimant would update a window the user did not change,
            //and which one came first is an enumeration accident.
            Aperture aperture_1 = Stamped(AperturePart.Pane, Reference(ZoneA, 5));
            Aperture aperture_2 = Stamped(AperturePart.Pane, Reference(ZoneA, 5));

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture_1, aperture_2 });

            Assert.That(index.TryResolve(new ZoneSurfaceKey(ZoneA, 5), out System.Guid guid, out AperturePart _, out int _, out string refusal), Is.False);
            Assert.That(guid, Is.EqualTo(System.Guid.Empty));
            Assert.That(refusal, Is.Not.Null);
            Assert.That(index.Ambiguities().Count, Is.EqualTo(1));
            Assert.That(index.ResolvableCount, Is.EqualTo(0));
        }

        [Test]
        public void Index_ContestedSurface_RefusesRegardlessOfWhichApertureIsListedFirst()
        {
            Aperture aperture_1 = Stamped(AperturePart.Pane, Reference(ZoneA, 5));
            Aperture aperture_2 = Stamped(AperturePart.Pane, Reference(ZoneA, 5));

            foreach (Aperture[] order in new[] { new[] { aperture_1, aperture_2 }, new[] { aperture_2, aperture_1 } })
            {
                Assert.That(TasQuery.AperturePhysicalIndex(order).TryResolve(new ZoneSurfaceKey(ZoneA, 5), out System.Guid _, out AperturePart _, out int _, out string _), Is.False);
            }
        }

        [Test]
        public void Index_OneApertureClaimingOneSurfaceAsBothPaneAndFrame_Refuses()
        {
            //One physical surface cannot be both halves of its own opening. This is what a bad import used to
            //produce, stamping a lone pane as its own frame too.
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5));
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 5) }, out string _);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });

            Assert.That(index.TryResolve(new ZoneSurfaceKey(ZoneA, 5), out System.Guid _, out AperturePart _, out int _, out string refusal), Is.False);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void Index_AContestedSurfaceDoesNotPoisonTheRest()
        {
            Aperture aperture_1 = Stamped(AperturePart.Pane, Reference(ZoneA, 5));
            Aperture aperture_2 = Stamped(AperturePart.Pane, Reference(ZoneA, 5));
            Aperture aperture_3 = Stamped(AperturePart.Pane, Reference(ZoneA, 7));

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture_1, aperture_2, aperture_3 });

            Assert.That(index.TryResolve(new ZoneSurfaceKey(ZoneA, 7), out System.Guid guid, out AperturePart _, out int _, out string _), Is.True);
            Assert.That(guid, Is.EqualTo(aperture_3.Guid));
        }

        [Test]
        public void Index_HalfPopulatedStamp_ContributesNoResolvableKey()
        {
            Aperture aperture = Window();
            //Bypasses the mutator on purpose: this is what an older or hand-edited model can hold.
            aperture.SetValue(ApertureParameter.PaneZoneSurfaceReference_1, Reference(null, 5));

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });

            Assert.That(index.ResolvableCount, Is.EqualTo(0));
            Assert.That(index.Ambiguities(), Is.Empty);
        }

        // =================================================================================================
        // Many identical windows stay distinct - and the shared definition binding is NOT identity
        // =================================================================================================

        [Test]
        public void Index_TwoHundredIdenticalWindowsSharingOneBuildingElement_RemainTwoHundredPhysicalInstances()
        {
            //Every one of these agrees on geometry dimensions, construction and building element. Only the
            //physical surface tells them apart - and it must.
            const string buildingElementGuid = "SHARED-PANE-ELEMENT";

            List<Aperture> apertures = new List<Aperture>();
            for (int i = 0; i < 200; i++)
            {
                Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 100 + i));
                aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, buildingElementGuid);
                apertures.Add(aperture);
            }

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(apertures);

            Assert.That(index.ResolvableCount, Is.EqualTo(200));
            Assert.That(index.Ambiguities(), Is.Empty);

            //Each surface resolves to its OWN aperture, and no two resolve to the same one.
            List<System.Guid> resolved = new List<System.Guid>();
            for (int i = 0; i < 200; i++)
            {
                Assert.That(index.TryResolve(new ZoneSurfaceKey(ZoneA, 100 + i), out System.Guid guid, out AperturePart _, out int _, out string _), Is.True);
                Assert.That(guid, Is.EqualTo(apertures[i].Guid));
                resolved.Add(guid);
            }

            Assert.That(resolved.Distinct().Count(), Is.EqualTo(200));

            //And the shared binding is reported as shared - 200 members is the intended Stage 2 state, not a
            //fault, which is exactly why it cannot be identity.
            Assert.That(index.ApertureGuids(buildingElementGuid).Count, Is.EqualTo(200));
        }

        [Test]
        public void Index_BuildingElementGuid_ReportsEveryMemberRatherThanAWinner()
        {
            //Two windows sharing one pane element is the intended Stage 2 state. The lookup answers with BOTH,
            //because there is no correct single answer - which is exactly why a building-element GUID cannot
            //serve as physical identity.
            const string buildingElementGuid = "SHARED-PANE-ELEMENT";

            Aperture aperture_1 = Stamped(AperturePart.Pane, Reference(ZoneA, 5));
            Aperture aperture_2 = Stamped(AperturePart.Pane, Reference(ZoneA, 7));
            aperture_1.SetValue(ApertureParameter.PaneBuildingElementGuid, buildingElementGuid);
            aperture_2.SetValue(ApertureParameter.PaneBuildingElementGuid, buildingElementGuid);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture_1, aperture_2 });

            Assert.That(index.ApertureGuids(buildingElementGuid), Is.EquivalentTo(new[] { aperture_1.Guid, aperture_2.Guid }));
            Assert.That(index.ApertureGuids("no-such-element"), Is.Empty);
        }

        // =================================================================================================
        // The _1 / _2 slots: a slot is a SIDE and a side is a ZONE
        // =================================================================================================

        [Test]
        public void Sides_TwoZones_AreOrderedByZoneNotByArrival()
        {
            List<ZoneSurfaceKey> forwards = TasQuery.ApertureZoneSurfaceSides(new[] { new ZoneSurfaceKey(ZoneA, 5), new ZoneSurfaceKey(ZoneB, 9) }, out string refusal_1);
            List<ZoneSurfaceKey> backwards = TasQuery.ApertureZoneSurfaceSides(new[] { new ZoneSurfaceKey(ZoneB, 9), new ZoneSurfaceKey(ZoneA, 5) }, out string refusal_2);

            Assert.That(refusal_1, Is.Null);
            Assert.That(refusal_2, Is.Null);
            Assert.That(forwards, Is.EqualTo(backwards), "Slot order must be a property of the model, not of the list.");
            Assert.That(forwards[0], Is.EqualTo(new ZoneSurfaceKey(ZoneA, 5)));
            Assert.That(forwards[1], Is.EqualTo(new ZoneSurfaceKey(ZoneB, 9)));
        }

        [Test]
        public void Sides_SeveralSurfacesInOneZone_TakeOneSlotBetweenThem()
        {
            //An aperture whose pane is split into several faces contributes several surfaces to ONE side. The
            //previous fill-the-next-empty-slot behaviour put two same-side surfaces in the two slots and lost
            //the other side entirely.
            List<ZoneSurfaceKey> sides = TasQuery.ApertureZoneSurfaceSides(new[]
            {
                new ZoneSurfaceKey(ZoneA, 7),
                new ZoneSurfaceKey(ZoneA, 5),
                new ZoneSurfaceKey(ZoneB, 9)
            }, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(sides.Count, Is.EqualTo(2));
            Assert.That(sides[0], Is.EqualTo(new ZoneSurfaceKey(ZoneA, 5)), "Lowest surface number represents its zone.");
            Assert.That(sides[1], Is.EqualTo(new ZoneSurfaceKey(ZoneB, 9)));
        }

        [Test]
        public void Sides_ThreeZones_Refuses()
        {
            //An aperture separates at most two zones. A third means the caller's grouping is wrong, and
            //truncating would hide it.
            List<ZoneSurfaceKey> sides = TasQuery.ApertureZoneSurfaceSides(new[]
            {
                new ZoneSurfaceKey(ZoneA, 5),
                new ZoneSurfaceKey(ZoneB, 9),
                new ZoneSurfaceKey(ZoneC, 3)
            }, out string refusal);

            Assert.That(sides, Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void Sides_DuplicatesAndUnusableKeys_AreDropped()
        {
            List<ZoneSurfaceKey> sides = TasQuery.ApertureZoneSurfaceSides(new[]
            {
                new ZoneSurfaceKey(ZoneA, 5),
                new ZoneSurfaceKey("{" + ZoneA + "}", 5),
                null,
                new ZoneSurfaceKey(null, 5),
                new ZoneSurfaceKey(ZoneA, -1)
            }, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(sides.Count, Is.EqualTo(1));
        }

        [Test]
        public void Sides_NoSurfaces_IsAnEmptyAnswerNotARefusal()
        {
            List<ZoneSurfaceKey> sides = TasQuery.ApertureZoneSurfaceSides(new ZoneSurfaceKey[0], out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(sides, Is.Empty);
        }

        // =================================================================================================
        // The one mutator - clear then fill, on every write path
        // =================================================================================================

        [Test]
        public void Write_TwoSidedAperture_PutsTheLowerZoneInSlotOneWhicheverOrderItIsGiven()
        {
            Aperture forwards = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));
            Aperture backwards = Stamped(AperturePart.Pane, Reference(ZoneB, 9), Reference(ZoneA, 5));

            foreach (Aperture aperture in new[] { forwards, backwards })
            {
                Assert.That(Read(aperture, AperturePart.Pane, 1).ZoneGuid, Is.EqualTo(ZoneA));
                Assert.That(Read(aperture, AperturePart.Pane, 1).SurfaceNumber, Is.EqualTo(5));
                Assert.That(Read(aperture, AperturePart.Pane, 2).ZoneGuid, Is.EqualTo(ZoneB));
                Assert.That(Read(aperture, AperturePart.Pane, 2).SurfaceNumber, Is.EqualTo(9));
            }
        }

        [Test]
        public void Write_RepeatedWithTheSameSurfaces_ChangesNothing()
        {
            //"Repeated update does not swap references." The invariant a re-export or a second UpdateIds pass
            //depends on.
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));

            for (int pass = 0; pass < 3; pass++)
            {
                aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneB, 9), Reference(ZoneA, 5) }, out string refusal);

                Assert.That(refusal, Is.Null);
                Assert.That(Read(aperture, AperturePart.Pane, 1).SurfaceNumber, Is.EqualTo(5));
                Assert.That(Read(aperture, AperturePart.Pane, 2).SurfaceNumber, Is.EqualTo(9));
            }
        }

        [Test]
        public void Write_FewerSurfacesThanLastTime_ClearsTheStaleSlot()
        {
            //The defect this replaces: the write filled only an EMPTY slot, so a second pass over an
            //already-stamped model kept the previous run's _1 and overwrote _2. A stale stamp is not harmless -
            //TAS does not promise to reassign the same surface numbers, so it points at a real surface that
            //belongs to something else.
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));

            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 11) }, out string _);

            Assert.That(Read(aperture, AperturePart.Pane, 1).SurfaceNumber, Is.EqualTo(11));
            Assert.That(aperture.HasValue(ApertureParameter.PaneZoneSurfaceReference_2), Is.False);
        }

        [Test]
        public void Write_NoSurfaces_ClearsBothSlots()
        {
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));

            Assert.That(aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new ZoneSurfaceReference[0], out string refusal), Is.True);
            Assert.That(refusal, Is.Null);
            Assert.That(aperture.HasValue(ApertureParameter.PaneZoneSurfaceReference_1), Is.False);
            Assert.That(aperture.HasValue(ApertureParameter.PaneZoneSurfaceReference_2), Is.False);
        }

        [Test]
        public void Write_ARefusal_LeavesNoStampStanding()
        {
            //A refusal must not leave last run's stamps looking confirmed.
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5));

            Assert.That(aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 5), Reference(ZoneB, 9), Reference(ZoneC, 3) }, out string refusal), Is.False);
            Assert.That(refusal, Is.Not.Null);
            Assert.That(aperture.HasValue(ApertureParameter.PaneZoneSurfaceReference_1), Is.False);
            Assert.That(aperture.HasValue(ApertureParameter.PaneZoneSurfaceReference_2), Is.False);
        }

        [Test]
        public void Write_PaneAndFrame_DoNotDisturbEachOther()
        {
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 6) }, out string _);

            Assert.That(Read(aperture, AperturePart.Pane, 1).SurfaceNumber, Is.EqualTo(5));
            Assert.That(Read(aperture, AperturePart.Pane, 2).SurfaceNumber, Is.EqualTo(9));
            Assert.That(Read(aperture, AperturePart.Frame, 1).SurfaceNumber, Is.EqualTo(6));
            Assert.That(aperture.HasValue(ApertureParameter.FrameZoneSurfaceReference_2), Is.False);
        }

        [Test]
        public void Write_KeepsTheCallersSpellingOfTheZoneGuid()
        {
            //Ordering normalises; writing does not. A re-exported model must still diff clean against its
            //source rather than showing every stamp rewritten into canonical case.
            Aperture aperture = Stamped(AperturePart.Pane, Reference("{" + ZoneA + "}", 5));

            Assert.That(Read(aperture, AperturePart.Pane, 1).ZoneGuid, Is.EqualTo("{" + ZoneA + "}"));
        }

        [Test]
        public void Write_SeveralSurfacesOnOneSide_PreservesTheCompleteSetBehindRepresentativeSlots()
        {
            Aperture aperture = Stamped(
                AperturePart.Pane,
                Reference(ZoneA, 7),
                Reference(ZoneB, 9),
                Reference(ZoneA, 5));

            Assert.That(Read(aperture, AperturePart.Pane, 1).SurfaceNumber, Is.EqualTo(5));
            Assert.That(Read(aperture, AperturePart.Pane, 2).SurfaceNumber, Is.EqualTo(9));

            List<ZoneSurfaceReference> all = TasQuery.ApertureZoneSurfaceReferences(aperture, AperturePart.Pane);
            Assert.That(all.Select(x => new ZoneSurfaceKey(x.ZoneGuid, x.SurfaceNumber)), Is.EqualTo(new[]
            {
                new ZoneSurfaceKey(ZoneA, 5),
                new ZoneSurfaceKey(ZoneA, 7),
                new ZoneSurfaceKey(ZoneB, 9)
            }));

            Aperture roundTrip = new Aperture(aperture.ToJsonObject());
            Assert.That(TasQuery.ApertureZoneSurfaceReferences(roundTrip, AperturePart.Pane).Count, Is.EqualTo(3), "The complete set must survive the SAM file boundary.");
        }

        // =================================================================================================
        // The import's second side
        // =================================================================================================

        [Test]
        public void Add_SecondSide_JoinsTheFirstAndBothSidesSurvive()
        {
            //The import walks each zone in turn, so an internal aperture is met twice - created on the first
            //meeting, and this is the second. It used to be skipped, leaving the aperture stating one surface
            //where the TBD holds two.
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneB, 9));

            Assert.That(aperture.AddApertureZoneSurfaceReference(AperturePart.Pane, Reference(ZoneA, 5), out string refusal), Is.True);
            Assert.That(refusal, Is.Null);

            Assert.That(Read(aperture, AperturePart.Pane, 1).SurfaceNumber, Is.EqualTo(5));
            Assert.That(Read(aperture, AperturePart.Pane, 2).SurfaceNumber, Is.EqualTo(9));
        }

        [Test]
        public void Add_SecondSide_GivesTheSameResultWhicheverZoneTheImportWalkedFirst()
        {
            Aperture aperture_AFirst = Stamped(AperturePart.Pane, Reference(ZoneA, 5));
            aperture_AFirst.AddApertureZoneSurfaceReference(AperturePart.Pane, Reference(ZoneB, 9), out string _);

            Aperture aperture_BFirst = Stamped(AperturePart.Pane, Reference(ZoneB, 9));
            aperture_BFirst.AddApertureZoneSurfaceReference(AperturePart.Pane, Reference(ZoneA, 5), out string _);

            Assert.That(Read(aperture_AFirst, AperturePart.Pane, 1).SurfaceNumber, Is.EqualTo(Read(aperture_BFirst, AperturePart.Pane, 1).SurfaceNumber));
            Assert.That(Read(aperture_AFirst, AperturePart.Pane, 2).SurfaceNumber, Is.EqualTo(Read(aperture_BFirst, AperturePart.Pane, 2).SurfaceNumber));
        }

        [Test]
        public void Add_TheSameSurfaceTwice_StaysOneSide()
        {
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5));

            aperture.AddApertureZoneSurfaceReference(AperturePart.Pane, Reference(ZoneA, 5), out string _);

            Assert.That(Read(aperture, AperturePart.Pane, 1).SurfaceNumber, Is.EqualTo(5));
            Assert.That(aperture.HasValue(ApertureParameter.PaneZoneSurfaceReference_2), Is.False);
        }

        [Test]
        public void Add_AThirdZone_RefusesAndClearsRatherThanSilentlyDroppingIt()
        {
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));

            Assert.That(aperture.AddApertureZoneSurfaceReference(AperturePart.Pane, Reference(ZoneC, 3), out string refusal), Is.False);
            Assert.That(refusal, Is.Not.Null);
        }

        // =================================================================================================
        // A two-sided aperture round-trips through the index without inverting
        // =================================================================================================

        [Test]
        public void TwoSided_RepeatedWriteThenIndex_KeepsEachSideOnItsOwnSurface()
        {
            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 6), Reference(ZoneB, 10) }, out string _);

            for (int pass = 0; pass < 3; pass++)
            {
                //Re-written each pass from the surfaces the index reports, which is what an update does.
                aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, TasQuery.ApertureZoneSurfaceReferences(aperture, AperturePart.Pane), out string _);
                aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, TasQuery.ApertureZoneSurfaceReferences(aperture, AperturePart.Frame), out string _);

                AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });

                Assert.That(index.Ambiguities(), Is.Empty);
                Assert.That(index.ResolvableCount, Is.EqualTo(4));

                foreach (KeyValuePair<ZoneSurfaceKey, AperturePart> expected in new Dictionary<ZoneSurfaceKey, AperturePart>
                {
                    { new ZoneSurfaceKey(ZoneA, 5), AperturePart.Pane },
                    { new ZoneSurfaceKey(ZoneB, 9), AperturePart.Pane },
                    { new ZoneSurfaceKey(ZoneA, 6), AperturePart.Frame },
                    { new ZoneSurfaceKey(ZoneB, 10), AperturePart.Frame }
                })
                {
                    Assert.That(index.TryResolve(expected.Key, out System.Guid guid, out AperturePart part, out int _, out string _), Is.True);
                    Assert.That(guid, Is.EqualTo(aperture.Guid));
                    Assert.That(part, Is.EqualTo(expected.Value));
                }

                Assert.That(Read(aperture, AperturePart.Pane, 1).ZoneGuid, Is.EqualTo(ZoneA));
                Assert.That(Read(aperture, AperturePart.Frame, 1).ZoneGuid, Is.EqualTo(ZoneA));
            }
        }

        [Test]
        public void TwoSided_TwoInternalAperturesBetweenTheSameZonePair_DoNotCrossBind()
        {
            //Same zones, same construction, same dimensions - only the surface numbers differ. Nothing but
            //the physical pair could keep these apart.
            Aperture aperture_1 = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneB, 9));
            Aperture aperture_2 = Stamped(AperturePart.Pane, Reference(ZoneA, 6), Reference(ZoneB, 10));

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture_1, aperture_2 });

            Assert.That(index.Ambiguities(), Is.Empty);

            index.TryResolve(new ZoneSurfaceKey(ZoneA, 5), out System.Guid guid_A1, out AperturePart _, out int _, out string _);
            index.TryResolve(new ZoneSurfaceKey(ZoneB, 9), out System.Guid guid_B1, out AperturePart _, out int _, out string _);
            index.TryResolve(new ZoneSurfaceKey(ZoneA, 6), out System.Guid guid_A2, out AperturePart _, out int _, out string _);
            index.TryResolve(new ZoneSurfaceKey(ZoneB, 10), out System.Guid guid_B2, out AperturePart _, out int _, out string _);

            Assert.That(guid_A1, Is.EqualTo(aperture_1.Guid));
            Assert.That(guid_B1, Is.EqualTo(aperture_1.Guid), "Both sides of one opening must resolve to the same aperture.");
            Assert.That(guid_A2, Is.EqualTo(aperture_2.Guid));
            Assert.That(guid_B2, Is.EqualTo(aperture_2.Guid));
        }

        // =================================================================================================
        // Complete-set split/rebind planning - validation happens before replacement creation
        // =================================================================================================

        [Test]
        public void Rebind_MultiFacePane_SplitsEveryPhysicalSurfaceAndMergesBackTogether()
        {
            const string originalElement = "BE-ORIGINAL";
            const string splitElement = "BE-SPLIT";

            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 7), Reference(ZoneA, 5));
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, originalElement);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });
            Dictionary<ZoneSurfaceKey, string> bindings = new Dictionary<ZoneSurfaceKey, string>
            {
                [new ZoneSurfaceKey(ZoneA, 5)] = originalElement,
                [new ZoneSurfaceKey(ZoneA, 7)] = originalElement
            };

            List<ZoneSurfaceKey> splitPlan = TasQuery.ApertureRebindKeys(aperture.AperturePhysicalIdentity(), AperturePart.Pane, index, bindings, originalElement, out string splitRefusal);

            Assert.That(splitRefusal, Is.Null);
            Assert.That(splitPlan.Count, Is.EqualTo(2));
            foreach (ZoneSurfaceKey key in splitPlan)
            {
                bindings[key] = splitElement;
            }

            Assert.That(bindings.Values, Is.All.EqualTo(splitElement), "No physical face may remain on the shared definition after a split.");

            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, splitElement);
            List<ZoneSurfaceKey> mergePlan = TasQuery.ApertureRebindKeys(aperture.AperturePhysicalIdentity(), AperturePart.Pane, index, bindings, splitElement, out string mergeRefusal);

            Assert.That(mergeRefusal, Is.Null);
            Assert.That(mergePlan, Is.EqualTo(splitPlan));
            foreach (ZoneSurfaceKey key in mergePlan)
            {
                bindings[key] = originalElement;
            }

            Assert.That(bindings.Values, Is.All.EqualTo(originalElement), "Merge-back must reverse the complete split, not only its representative stamp.");
        }

        [Test]
        public void Rebind_ContestedSurface_RefusesBeforeAnyReplacementOrSurfaceMutation()
        {
            const string originalElement = "BE-ORIGINAL";

            Aperture aperture = Stamped(AperturePart.Pane, Reference(ZoneA, 5), Reference(ZoneA, 7));
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, originalElement);

            Aperture contestant = Stamped(AperturePart.Pane, Reference(ZoneA, 7));
            contestant.SetValue(ApertureParameter.PaneBuildingElementGuid, originalElement);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture, contestant });
            Dictionary<ZoneSurfaceKey, string> bindings = new Dictionary<ZoneSurfaceKey, string>
            {
                [new ZoneSurfaceKey(ZoneA, 5)] = originalElement,
                [new ZoneSurfaceKey(ZoneA, 7)] = originalElement
            };
            Dictionary<ZoneSurfaceKey, string> before = new Dictionary<ZoneSurfaceKey, string>(bindings);
            int buildingElementCount = 1;

            List<ZoneSurfaceKey> plan = TasQuery.ApertureRebindKeys(aperture.AperturePhysicalIdentity(), AperturePart.Pane, index, bindings, originalElement, out string refusal);
            if (plan != null)
            {
                buildingElementCount++;
                foreach (ZoneSurfaceKey key in plan)
                {
                    bindings[key] = "ORPHAN";
                }
            }

            Assert.That(plan, Is.Null);
            Assert.That(refusal, Does.Contain("claimed by more than one aperture"));
            Assert.That(bindings, Is.EqualTo(before), "A refused complete-set validation moves zero surfaces.");
            Assert.That(buildingElementCount, Is.EqualTo(1), "Replacement creation is downstream of validation, so refusal cannot leave an orphan definition.");
            Assert.That(aperture.TryGetValue(ApertureParameter.PaneBuildingElementGuid, out string bindingStamp), Is.True);
            Assert.That(bindingStamp, Is.EqualTo(originalElement));
        }

        [Test]
        public void Rebind_RepresentativeOnlyLegacyStamp_RefusesRatherThanRiskAPartialMove()
        {
            Aperture aperture = Window();
            aperture.SetValue(ApertureParameter.PaneZoneSurfaceReference_1, Reference(ZoneA, 5));

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });
            Dictionary<ZoneSurfaceKey, string> bindings = new Dictionary<ZoneSurfaceKey, string>
            {
                [new ZoneSurfaceKey(ZoneA, 5)] = "BE-ORIGINAL"
            };

            List<ZoneSurfaceKey> plan = TasQuery.ApertureRebindKeys(aperture.AperturePhysicalIdentity(), AperturePart.Pane, index, bindings, "BE-ORIGINAL", out string refusal);

            Assert.That(plan, Is.Null);
            Assert.That(refusal, Does.Contain("no preserved complete physical surface set"));
            Assert.That(bindings[new ZoneSurfaceKey(ZoneA, 5)], Is.EqualTo("BE-ORIGINAL"));
        }

        // =================================================================================================
        // ZoneSurfaceReferencesMatch - the same rule, on the legacy comparison
        // =================================================================================================

        [Test]
        public void ReferencesMatch_SameNumberDifferentStatedZone_DoesNotMatch()
        {
            Assert.That(TasQuery.ZoneSurfaceReferencesMatch(Reference(ZoneA, 5), Reference(ZoneB, 5)), Is.False);
        }

        [Test]
        public void ReferencesMatch_ZoneGuidSpelling_StillMatches()
        {
            //Kept in step with ZoneSurfaceKey: the two places a physical surface is compared must not
            //disagree about whether two spellings of one GUID are one zone.
            Assert.That(TasQuery.ZoneSurfaceReferencesMatch(Reference("{" + ZoneA + "}", 5), Reference(ZoneA.ToLower(), 5)), Is.True);
        }

        [Test]
        public void ReferencesMatch_OneSideStatesNoZone_FallsBackToTheNumber()
        {
            //A strict tightening, never a new refusal: an older stamp with no zone behaves as it always did.
            Assert.That(TasQuery.ZoneSurfaceReferencesMatch(Reference(null, 5), Reference(ZoneB, 5)), Is.True);
            Assert.That(TasQuery.ZoneSurfaceReferencesMatch(Reference(ZoneA, 5), Reference(null, 6)), Is.False);
        }
    }
}
