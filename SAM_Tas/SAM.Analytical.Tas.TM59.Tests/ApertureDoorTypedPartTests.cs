// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using ApertureBuildingElementUsage = SAM.Analytical.Tas.ApertureBuildingElementUsage;
using AperturePhysicalIndex = SAM.Analytical.Tas.AperturePhysicalIndex;
using TasQuery = SAM.Analytical.Tas.Query;
using ZoneSurfaceKey = SAM.Analytical.Tas.ZoneSurfaceKey;
using ZoneSurfaceReference = SAM.Core.Tas.ZoneSurfaceReference;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>An opening whose pane TAS typed DOOR rather than GLAZING.</b>
    /// <para>
    /// TAS does not always give an opening's glazed half <c>BEType</c> 12. On the gbXML route it picks
    /// <c>DOORELEMENT</c> (14) for a WHOLE MODEL at a time - a single pane-less <c>WindowType</c> anywhere in
    /// the exported gbXML is enough, and a round-tripped model acquires one as soon as
    /// <c>SAMAnalytical.FromTBD</c> runs with <c>_importUnused_</c> on and the previous TBD holds an unpaired
    /// aperture construction. So this is never one stray window: it is every window in the file.
    /// </para>
    /// <para>
    /// <b>Two readings of that element disagreed with the import's.</b> <c>Convert.ToSAM</c> has always used
    /// <c>Query.AperturePart_BuildingElementType</c>, which calls a door leaf a PANE.
    /// <c>Query.Match</c> - which is how <c>Modify.UpdateIds</c> decides which half of an aperture a physical
    /// surface is - used <c>Query.AperturePart(int)</c>, which calls it a FRAME; and the sweep in
    /// <c>Modify.UpdateApertureDefinitions</c> recognised only 12 and 15 as aperture elements at all.
    /// </para>
    /// <para>
    /// The consequences are pinned below: the aperture ends a refresh with BOTH its surfaces in the frame set
    /// and none in the pane set, its frame binding names whichever of the two elements was written last, the
    /// pane is skipped for want of a binding and the frame is refused because the set it claims is not all on
    /// one element - "40 aperture part(s) considered; 0 rebound" - and even once the rebind succeeds, the
    /// emptied per-aperture door elements cannot be swept and stay in TAS's Building Elements list.
    /// </para>
    /// <para>COM-free: every decision under test is a pure function over plain values.</para>
    /// </summary>
    [TestFixture]
    public class ApertureDoorTypedPartTests
    {
        private const int BEType_Glazing = 12;
        private const int BEType_Rooflight = 13;
        private const int BEType_Door = 14;
        private const int BEType_Frame = 15;

        private const string ZoneA = "11111111-1111-1111-1111-111111111111";

        private const string Element_Pane = "{AAAAAAAA-0000-0000-0000-000000000001}";
        private const string Element_Frame = "{AAAAAAAA-0000-0000-0000-000000000002}";

        // =================================================================================================
        // 1 - the reading itself
        // =================================================================================================

        [Test]
        public void AperturePart_BEType_ReadsADoorLeafAsThePane()
        {
            //THE DEFECT, at its source. A door leaf is the glazed half of its opening, so a surface on a
            //door-typed element is that opening's PANE. Reading it as the frame leaves the opening with no
            //pane at all, and the pane's opening controls and result mapping go with it.
            Assert.That(TasQuery.AperturePart_BEType(BEType_Door), Is.EqualTo(AperturePart.Pane));
        }

        [Test]
        public void AperturePart_BEType_ReadsEveryOpeningHalfTheWayTheImportDoes()
        {
            Assert.Multiple(() =>
            {
                Assert.That(TasQuery.AperturePart_BEType(BEType_Glazing), Is.EqualTo(AperturePart.Pane));
                Assert.That(TasQuery.AperturePart_BEType(BEType_Rooflight), Is.EqualTo(AperturePart.Pane));
                Assert.That(TasQuery.AperturePart_BEType(BEType_Door), Is.EqualTo(AperturePart.Pane));
                Assert.That(TasQuery.AperturePart_BEType(BEType_Frame), Is.EqualTo(AperturePart.Frame));
            });
        }

        [TestCase(0)]   //Null / Air
        [TestCase(1)]   //Internal Wall
        [TestCase(2)]   //External Wall
        [TestCase(3)]   //Roof
        [TestCase(4)]   //Internal Floor
        [TestCase(5)]   //Shade
        [TestCase(11)]  //Slab on Grade
        [TestCase(16)]  //Curtain Wall - an opening TAS models differently; a guess would be worse than a refusal
        [TestCase(20)]  //Vehicle Door - likewise
        public void AperturePart_BEType_RefusesAnythingThatIsNotHalfOfAnOpening(int bEType)
        {
            Assert.That(TasQuery.AperturePart_BEType(bEType), Is.EqualTo(AperturePart.Undefined));
        }

        [Test]
        public void AperturePart_Int_StaysTheWriteSideHelperAndIsNotUsedToReadAnElement()
        {
            //Deliberately UNCHANGED. Query.AperturePart(int) answers Frame for 14 because the aperture-type
            //WRITE wants "the half that is not glazing", and that is fine where it is used. It is pinned here
            //so that the difference between the two is a decision rather than an accident - and so that the
            //next reader of a TBD element reaches for AperturePart_BEType instead.
            Assert.That(TasQuery.AperturePart(BEType_Door), Is.EqualTo(AperturePart.Frame));
            Assert.That(TasQuery.AperturePart_BEType(BEType_Door), Is.Not.EqualTo(TasQuery.AperturePart(BEType_Door)));
        }

        // =================================================================================================
        // 2 - the reader and the sweep may never drift apart again
        // =================================================================================================

        [Test]
        public void TheSweepRecognisesExactlyWhatTheReaderRecognises()
        {
            //The sweep used to name 12 and 15 only, so a door- or rooflight-typed element it had just emptied
            //failed its own aperture test and could not be marked. One definition now answers both questions;
            //this is the guard that keeps it that way.
            for (int bEType = 0; bEType <= 20; bEType++)
            {
                bool isOpeningHalf = TasQuery.AperturePart_BEType(bEType) != AperturePart.Undefined;
                bool isAperture = new ApertureBuildingElementUsage("guid-" + bEType, "element-" + bEType, bEType, 0).IsAperture;

                Assert.That(isAperture, Is.EqualTo(isOpeningHalf), "BEType " + bEType + ": the sweep and the element reader disagree.");
            }
        }

        // =================================================================================================
        // 3 - the sweep, on a door-typed model
        // =================================================================================================

        [Test]
        public void Sweep_MarksThePerApertureDoorElementTheRebindEmptied()
        {
            //After the rebind the per-aperture door element holds no surface. It is dead weight carrying a
            //physical aperture GUID in its name, and it stayed in the TBD: twenty windows, twenty leftovers.
            List<ApertureBuildingElementUsage> usages =
            [
                new ApertureBuildingElementUsage("canonical-pane", "Windows: SIM_EXT_GLZ -pane", BEType_Glazing, 20),
                new ApertureBuildingElementUsage("orphan-door", "Windows: SIM_EXT_GLZ 0d5d346b -pane", BEType_Door, 0),
                new ApertureBuildingElementUsage("orphan-rooflight", "Windows: SIM_EXT_GLZ 61433574 -pane", BEType_Rooflight, 0),
                new ApertureBuildingElementUsage("orphan-frame", "Windows: SIM_EXT_GLZ 0d5d346b -frame", BEType_Frame, 0)
            ];

            List<string> guids = TasQuery.UnusedApertureBuildingElementGuids(usages, new string[] { "canonical-pane" });

            Assert.That(guids, Is.EquivalentTo(new[] { "orphan-door", "orphan-rooflight", "orphan-frame" }));
        }

        [Test]
        public void Sweep_LeavesADoorElementThatStillHoldsASurfaceAndOneItResolvedOnto()
        {
            //Neither of the other two gates is relaxed by widening the type test: an element standing for a
            //real opening is never a candidate, and a canonical element momentarily emptied by a refusal is
            //never deleted.
            List<ApertureBuildingElementUsage> usages =
            [
                new ApertureBuildingElementUsage("door-in-use", "Front door -pane", BEType_Door, 1),
                new ApertureBuildingElementUsage("door-canonical-empty", "Doors: OAK -pane", BEType_Door, 0),
                new ApertureBuildingElementUsage("panel", "External Wall", TasQuery.BEType("External Wall"), 0)
            ];

            List<string> guids = TasQuery.UnusedApertureBuildingElementGuids(usages, new string[] { "door-canonical-empty" });

            Assert.That(guids, Is.Empty);
        }

        // =================================================================================================
        // 4 - what the wrong reading did to the aperture, and what the right one does
        // =================================================================================================

        /// <summary>
        /// The state <c>Modify.UpdateIds</c> leaves when the two physical surfaces of one window are read
        /// CORRECTLY - the door-typed surface as the pane, the frame-typed one as the frame.
        /// </summary>
        private static Aperture StampedTheWayTheImportReadsIt()
        {
            Aperture aperture = Window();
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 4) }, out string _);
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 3) }, out string _);
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_Pane);
            aperture.SetValue(ApertureParameter.FrameBuildingElementGuid, Element_Frame);
            return aperture;
        }

        /// <summary>
        /// The state it left when the door-typed surface was read as a FRAME: both surfaces collected into the
        /// frame set, no pane stamp, and the frame binding naming whichever element the pass wrote last -
        /// which is the pane's, because the pane surface follows the frame surface within the zone.
        /// </summary>
        private static Aperture StampedTheWayTheOldReadingLeftIt()
        {
            Aperture aperture = Window();
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 3), Reference(ZoneA, 4) }, out string _);
            aperture.SetValue(ApertureParameter.FrameBuildingElementGuid, Element_Pane);
            return aperture;
        }

        [Test]
        public void OldReading_LeftTheApertureWithNoPaneAndBothSurfacesInTheFrameSet()
        {
            //The shape measured on the licensed run that reproduced the report: paneBE absent, paneKeys 0,
            //frameKeys 2.
            AperturePhysicalIdentity identity = StampedTheWayTheOldReadingLeftIt().AperturePhysicalIdentity();

            Assert.Multiple(() =>
            {
                Assert.That(identity.BuildingElementGuid(AperturePart.Pane), Is.Null, "The pane was never stamped, so UpdateApertureDefinitions skips it and its element is never collapsed.");
                Assert.That(identity.AllKeys(AperturePart.Pane), Is.Empty);
                Assert.That(identity.AllKeys(AperturePart.Frame), Has.Count.EqualTo(2), "The pane's own surface was collected as a frame surface.");
            });
        }

        [Test]
        public void OldReading_ThenRefusedTheFrameBecauseItsSurfacesSatOnTwoElements()
        {
            //The refusal was correct - the set really was not all on one element - but it was about state
            //that only existed because the pane surface had been mistaken for a frame surface.
            Aperture aperture = StampedTheWayTheOldReadingLeftIt();
            AperturePhysicalIdentity identity = aperture.AperturePhysicalIdentity();

            List<ZoneSurfaceKey> keys = TasQuery.ApertureRebindKeys(
                identity,
                AperturePart.Frame,
                TasQuery.AperturePhysicalIndex(new List<Aperture> { aperture }),
                Bindings(),
                Element_Pane,
                out string refusal);

            Assert.That(keys, Is.Null);
            Assert.That(refusal, Is.Not.Null.And.Contain("is not currently bound to the element the aperture stamp claims"));
        }

        [Test]
        public void RightReading_RebindsBothHalvesWithNoRefusal()
        {
            //With the door leaf read as the pane, each half owns exactly its own surface, each is complete,
            //and each rebinds - which is what turns "40 considered; 0 rebound" into "40 considered; 40
            //rebound" on the licensed chain.
            Aperture aperture = StampedTheWayTheImportReadsIt();
            AperturePhysicalIdentity identity = aperture.AperturePhysicalIdentity();
            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new List<Aperture> { aperture });
            IReadOnlyDictionary<ZoneSurfaceKey, string> bindings = Bindings();

            List<ZoneSurfaceKey> keys_Pane = TasQuery.ApertureRebindKeys(identity, AperturePart.Pane, index, bindings, Element_Pane, out string refusal_Pane);
            List<ZoneSurfaceKey> keys_Frame = TasQuery.ApertureRebindKeys(identity, AperturePart.Frame, index, bindings, Element_Frame, out string refusal_Frame);

            Assert.Multiple(() =>
            {
                Assert.That(refusal_Pane, Is.Null);
                Assert.That(refusal_Frame, Is.Null);
                Assert.That(keys_Pane, Is.EquivalentTo(new[] { new ZoneSurfaceKey(ZoneA, 4) }));
                Assert.That(keys_Frame, Is.EquivalentTo(new[] { new ZoneSurfaceKey(ZoneA, 3) }));
                Assert.That(identity.SurfaceSetComplete(AperturePart.Pane), Is.True);
                Assert.That(identity.SurfaceSetComplete(AperturePart.Frame), Is.True);
            });
        }

        // =================================================================================================
        // Builders
        // =================================================================================================

        /// <summary>
        /// The TBD as TAS's gbXML conversion leaves it: surface 3 is the opening's frame, surface 4 its pane,
        /// each on its own per-aperture element.
        /// </summary>
        private static IReadOnlyDictionary<ZoneSurfaceKey, string> Bindings()
        {
            return new Dictionary<ZoneSurfaceKey, string>
            {
                { new ZoneSurfaceKey(ZoneA, 3), Element_Frame },
                { new ZoneSurfaceKey(ZoneA, 4), Element_Pane }
            };
        }

        private static ApertureConstruction Glazing()
        {
            return new ApertureConstruction(
                System.Guid.NewGuid(),
                "SIM_EXT_GLZ",
                ApertureType.Window,
                new List<ConstructionLayer> { new ConstructionLayer("Glass 6mm", 0.006) },
                new List<ConstructionLayer> { new ConstructionLayer("Timber", 0.05) });
        }

        private static Aperture Window()
        {
            return new Aperture(Glazing(), new Polygon3D(new List<Point3D>
            {
                new Point3D(0, 0, 0),
                new Point3D(1, 0, 0),
                new Point3D(1, 0, 1),
                new Point3D(0, 0, 1)
            }));
        }

        private static ZoneSurfaceReference Reference(string zoneGuid, int surfaceNumber)
        {
            return new ZoneSurfaceReference(surfaceNumber, zoneGuid);
        }
    }
}
