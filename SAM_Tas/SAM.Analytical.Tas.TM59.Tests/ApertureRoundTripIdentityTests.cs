// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using AperturePhysicalIndex = SAM.Analytical.Tas.AperturePhysicalIndex;
using TasQuery = SAM.Analytical.Tas.Query;
using ZoneSurfaceKey = SAM.Analytical.Tas.ZoneSurfaceKey;
using ZoneSurfaceReference = SAM.Core.Tas.ZoneSurfaceReference;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The FULL round trip: TBD -> FromTBD -> SAM -> gbXML -> a NEW TBD.</b>
    /// <para>
    /// Earlier acceptance pinned <c>SAM -> WorkflowgbXML -> TBD</c> and repeated operation against a KEPT
    /// TBD. Both leave one seam unexercised: a model reconstructed from one TBD and then exported to make a
    /// DIFFERENT one. Across that seam a <c>Pane</c>/<c>FrameBuildingElementGuid</c> changes meaning. It is
    /// only ever the statement "this part was bound to definition X <b>in the TBD it was last stamped
    /// against</b>", and TAS mints its own aperture elements on every gbXML/T3D conversion - so carried into
    /// the new file it names an element that is not there, while the surface it claims really sits on a new
    /// one.
    /// </para>
    /// <para>
    /// <b>The rule these tests fix.</b> <c>Modify.UpdateIds</c> clears every aperture's physical stamps
    /// unconditionally and refills only what it re-matches. The binding must be cleared in the same breath and
    /// on the same terms: a part the refresh cannot resolve has to read as UNSTAMPED, not as bound to the
    /// previous file. It used to survive, so <c>Modify.UpdateApertureDefinitions</c> counted the part as bound
    /// and <c>Query.ApertureRebindKeys</c> then refused it - correctly, but against a binding that was never
    /// current, which is how a whole model could report "40 aperture part(s) considered; 0 rebound".
    /// </para>
    /// <para>
    /// <b>The refusals stay strict.</b> Nothing here loosens a gate: the point is that a failed refresh must
    /// present honest state to gates that are already right, so the tests below re-pin every refusal
    /// alongside the clearing.
    /// </para>
    /// <para>COM-free: every decision under test is a pure function over plain SAM value objects.</para>
    /// </summary>
    [TestFixture]
    public class ApertureRoundTripIdentityTests
    {
        private const string ZoneA = "11111111-1111-1111-1111-111111111111";
        private const string ZoneB = "22222222-2222-2222-2222-222222222222";

        //Two TBD generations. The same physical window is bound to the first in the file it was imported from
        //and to the second in the file that was just written; nothing may confuse the two.
        private const string Element_TBD1_Pane = "{AAAAAAAA-0000-0000-0000-000000000001}";
        private const string Element_TBD1_Frame = "{AAAAAAAA-0000-0000-0000-000000000002}";
        private const string Element_TBD2_Pane = "{BBBBBBBB-0000-0000-0000-000000000001}";
        private const string Element_TBD2_Frame = "{BBBBBBBB-0000-0000-0000-000000000002}";

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

        /// <summary>A wall large enough to hold <see cref="Window"/>'s aperture, for building a Panel.</summary>
        private static Face3D Wall()
        {
            return new Face3D(new Polygon3D(new List<Point3D>
            {
                new Point3D(-5, 0, -5),
                new Point3D(5, 0, -5),
                new Point3D(5, 0, 5),
                new Point3D(-5, 0, 5)
            }));
        }

        private static ZoneSurfaceReference Reference(string zoneGuid, int surfaceNumber)
        {
            return new ZoneSurfaceReference(surfaceNumber, zoneGuid);
        }

        /// <summary>
        /// An aperture as the TBD import leaves it: a COMPLETE physical set for each part, and each part bound
        /// to the element of the file it was read from.
        /// </summary>
        private static Aperture Imported()
        {
            Aperture aperture = Window();
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 5) }, out string _);
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 4) }, out string _);
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_TBD1_Pane);
            aperture.SetValue(ApertureParameter.FrameBuildingElementGuid, Element_TBD1_Frame);
            return aperture;
        }

        /// <summary>
        /// The clearing half of a <c>Modify.UpdateIds</c> pass, which is unconditional. Deliberately the very
        /// mutator <c>UpdateIds</c> calls rather than a re-spelling of it, so the pairing these tests fix is
        /// the pairing the workflow actually gets.
        /// </summary>
        private static void ClearForRefresh(Aperture aperture)
        {
            aperture.RemoveApertureTasIdentity();
        }

        private static string Binding(Aperture aperture, AperturePart aperturePart)
        {
            aperture.TryGetValue(TasQuery.ApertureBuildingElementGuidParameter(aperturePart), out string result);
            return result;
        }

        // =================================================================================================
        // 1 - the import preserves the complete physical set
        // =================================================================================================

        [Test]
        public void Import_StampsCompletePhysicalSetForBothParts()
        {
            AperturePhysicalIdentity identity = Imported().AperturePhysicalIdentity();

            Assert.That(identity.SurfaceSetComplete(AperturePart.Pane), Is.True);
            Assert.That(identity.SurfaceSetComplete(AperturePart.Frame), Is.True);
            Assert.That(identity.AllKeys(AperturePart.Pane), Has.Count.EqualTo(1));
            Assert.That(identity.AllKeys(AperturePart.Frame), Has.Count.EqualTo(1));
        }

        [Test]
        public void Import_MultipleFacesOnOneSide_StayOnePhysicalApertureAndOneSide()
        {
            //TAS may split one pane side into several faces. They are one aperture's one side, so they must
            //travel together in the complete set and still occupy a single slot.
            Aperture aperture = Window();
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 5), Reference(ZoneA, 7) }, out string _);

            AperturePhysicalIdentity identity = aperture.AperturePhysicalIdentity();

            Assert.That(identity.AllKeys(AperturePart.Pane), Has.Count.EqualTo(2));
            Assert.That(identity.Keys(AperturePart.Pane), Has.Count.EqualTo(1), "One zone is one side, however many faces TAS split it into.");
            Assert.That(identity.Key(AperturePart.Pane, 2), Is.Null);
        }

        // =================================================================================================
        // 2 - the complete set survives the SAM copy/serialization seam
        // =================================================================================================

        [Test]
        public void CompleteSet_SurvivesCopyConstructorAndJsonRoundTrip()
        {
            //The model crosses a component boundary between FromTBD and the next export. If the complete set
            //were lost there, every part would refuse for want of one while its representative slots stood.
            Aperture aperture = Imported();

            Aperture copied = new Aperture(aperture);
            Aperture serialized = Core.Query.IJSAMObject<Aperture>(aperture.ToJsonObject());

            foreach (Aperture round in new[] { copied, serialized })
            {
                AperturePhysicalIdentity identity = round.AperturePhysicalIdentity();
                Assert.That(identity.SurfaceSetComplete(AperturePart.Pane), Is.True);
                Assert.That(identity.SurfaceSetComplete(AperturePart.Frame), Is.True);
                Assert.That(identity.AllKeys(AperturePart.Pane)[0].Value, Is.EqualTo(new ZoneSurfaceKey(ZoneA, 5)));
                Assert.That(identity.AllKeys(AperturePart.Frame)[0].Value, Is.EqualTo(new ZoneSurfaceKey(ZoneA, 4)));
            }
        }

        // =================================================================================================
        // 3 - a failed refresh leaves no stale binding behind
        // =================================================================================================

        [Test]
        public void FailedRefresh_ClearsTheImportedBindingRatherThanLeavingItCurrent()
        {
            //THE DEFECT. The clearing pass is unconditional and the refill is not, so this is the state of any
            //part the new TBD could not be matched to. The imported binding must not survive it.
            Aperture aperture = Imported();

            ClearForRefresh(aperture);

            Assert.That(Binding(aperture, AperturePart.Pane), Is.Null);
            Assert.That(Binding(aperture, AperturePart.Frame), Is.Null);
            Assert.That(aperture.AperturePhysicalIdentity().HasStamps, Is.False);
        }

        [Test]
        public void FailedRefresh_LeavesNoPartClaimingAPreviousGenerationElement()
        {
            //Read the way Modify.UpdateApertureDefinitions reads it: a part with no binding is not "considered"
            //at all, which is the honest record of a refresh that could not resolve it. With the stale binding
            //standing, the part was counted as bound and then refused - a refusal about state that was never
            //current.
            Aperture aperture = Imported();

            ClearForRefresh(aperture);

            AperturePhysicalIdentity identity = aperture.AperturePhysicalIdentity();
            Assert.That(identity.BuildingElementGuid(AperturePart.Pane), Is.Null);
            Assert.That(identity.BuildingElementGuid(AperturePart.Frame), Is.Null);
        }

        [Test]
        public void ClearingOnePart_LeavesTheOtherPartUntouched()
        {
            //Pane and frame remain distinct: clearing is per part, so a pass that resolves one and not the
            //other does not discard the one it resolved.
            Aperture aperture = Imported();

            aperture.RemoveApertureBuildingElementGuid(AperturePart.Pane);

            Assert.That(Binding(aperture, AperturePart.Pane), Is.Null);
            Assert.That(Binding(aperture, AperturePart.Frame), Is.EqualTo(Element_TBD1_Frame));
        }

        [Test]
        public void PhysicalStampMutator_DoesNotClearTheBinding()
        {
            //The two are cleared together only by the pass that re-resolves both. The physical mutator owns the
            //stamps alone - the direct export and the import write a binding through it and must keep it.
            Aperture aperture = Imported();

            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 9) }, out string _);

            Assert.That(Binding(aperture, AperturePart.Pane), Is.EqualTo(Element_TBD1_Pane));
        }

        // =================================================================================================
        // 4 - a successful refresh replaces the binding with the current one
        // =================================================================================================

        [Test]
        public void SuccessfulRefresh_ReplacesBothStampAndBindingWithTheNewGeneration()
        {
            Aperture aperture = Imported();

            ClearForRefresh(aperture);

            //The refill half of the pass, for a part the new TBD did match - on ITS surface numbers, which TAS
            //need not have carried over.
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 11) }, out string _);
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_TBD2_Pane);

            AperturePhysicalIdentity identity = aperture.AperturePhysicalIdentity();
            Assert.That(identity.BuildingElementGuid(AperturePart.Pane), Is.EqualTo(Element_TBD2_Pane));
            Assert.That(identity.SurfaceSetComplete(AperturePart.Pane), Is.True);
            Assert.That(identity.AllKeys(AperturePart.Pane)[0].Value, Is.EqualTo(new ZoneSurfaceKey(ZoneA, 11)));
            Assert.That(identity.AllKeys(AperturePart.Pane), Has.Count.EqualTo(1), "The previous generation's surface must not linger beside the new one.");
        }

        [Test]
        public void FullyRefreshedAperture_PassesTheRebindGate()
        {
            //The target of the whole exercise: fresh re-stamping, not a suppressed refusal.
            Aperture aperture = Window();
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 11) }, out string _);
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_TBD2_Pane);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });
            Dictionary<ZoneSurfaceKey, string> bindings = new Dictionary<ZoneSurfaceKey, string>
            {
                [new ZoneSurfaceKey(ZoneA, 11)] = Element_TBD2_Pane
            };

            List<ZoneSurfaceKey> plan = TasQuery.ApertureRebindKeys(
                aperture.AperturePhysicalIdentity(), AperturePart.Pane, index, bindings, Element_TBD2_Pane, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(plan, Has.Count.EqualTo(1));
        }

        [Test]
        public void RefreshedPaneAndFrame_RemainTwoDistinctBindings()
        {
            Aperture aperture = Window();
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 11) }, out string _);
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Frame, new[] { Reference(ZoneA, 10) }, out string _);
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_TBD2_Pane);
            aperture.SetValue(ApertureParameter.FrameBuildingElementGuid, Element_TBD2_Frame);

            AperturePhysicalIdentity identity = aperture.AperturePhysicalIdentity();

            Assert.That(identity.BuildingElementGuid(AperturePart.Pane), Is.EqualTo(Element_TBD2_Pane));
            Assert.That(identity.BuildingElementGuid(AperturePart.Frame), Is.EqualTo(Element_TBD2_Frame));
            Assert.That(identity.AllKeys(AperturePart.Pane)[0].Value, Is.Not.EqualTo(identity.AllKeys(AperturePart.Frame)[0].Value));
        }

        // =================================================================================================
        // 5 - the refusals stay exactly as strict as they were
        // =================================================================================================

        [Test]
        public void Rebind_StaleBindingFromThePreviousGeneration_IsRefused()
        {
            //What the stale binding produced when it was allowed to stand: the surface is bound to the element
            //the NEW file created, so the claim is false and the gate refuses. Left in place, this is the whole
            //model's outcome; the fix is upstream, and the gate itself must not move.
            Aperture aperture = Imported();

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });
            Dictionary<ZoneSurfaceKey, string> bindings = new Dictionary<ZoneSurfaceKey, string>
            {
                [new ZoneSurfaceKey(ZoneA, 5)] = Element_TBD2_Pane
            };

            List<ZoneSurfaceKey> plan = TasQuery.ApertureRebindKeys(
                aperture.AperturePhysicalIdentity(), AperturePart.Pane, index, bindings, Element_TBD1_Pane, out string refusal);

            Assert.That(plan, Is.Null);
            Assert.That(refusal, Does.Contain("is not currently bound to the element the aperture stamp claims"));
        }

        [Test]
        public void Rebind_RepresentativeStampsWithoutACompleteSet_IsRefused()
        {
            //A legacy aperture carrying only the _1/_2 slots cannot prove another same-side face was not lost,
            //so it must be restamped before it may be rebound.
            Aperture aperture = Window();
            aperture.SetValue(ApertureParameter.PaneZoneSurfaceReference_1, Reference(ZoneA, 5));
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_TBD2_Pane);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });
            Dictionary<ZoneSurfaceKey, string> bindings = new Dictionary<ZoneSurfaceKey, string>
            {
                [new ZoneSurfaceKey(ZoneA, 5)] = Element_TBD2_Pane
            };

            List<ZoneSurfaceKey> plan = TasQuery.ApertureRebindKeys(
                aperture.AperturePhysicalIdentity(), AperturePart.Pane, index, bindings, Element_TBD2_Pane, out string refusal);

            Assert.That(plan, Is.Null);
            Assert.That(refusal, Does.Contain("no preserved complete physical surface set"));
        }

        [Test]
        public void Rebind_ContestedPhysicalOwnership_IsRefused()
        {
            //Two apertures claiming one surface is an ambiguity, and an ambiguity refuses rather than guessing
            //which of them owns it.
            Aperture aperture = Window();
            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 5) }, out string _);
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_TBD2_Pane);

            Aperture contestant = Window(10);
            contestant.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneA, 5) }, out string _);
            contestant.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_TBD2_Pane);

            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture, contestant });
            Dictionary<ZoneSurfaceKey, string> bindings = new Dictionary<ZoneSurfaceKey, string>
            {
                [new ZoneSurfaceKey(ZoneA, 5)] = Element_TBD2_Pane
            };

            List<ZoneSurfaceKey> plan = TasQuery.ApertureRebindKeys(
                aperture.AperturePhysicalIdentity(), AperturePart.Pane, index, bindings, Element_TBD2_Pane, out string refusal);

            Assert.That(plan, Is.Null);
            Assert.That(refusal, Does.Contain("does not resolve uniquely"));
        }

        [Test]
        public void Rebind_ClearedPart_StatesNoBindingToMoveAtAll()
        {
            //After the fix this is what an unresolvable part looks like to the definition pass: no binding, so
            //nothing to move and nothing to create - not a refusal about a foreign file's element.
            Aperture aperture = Imported();
            ClearForRefresh(aperture);

            AperturePhysicalIdentity identity = aperture.AperturePhysicalIdentity();
            AperturePhysicalIndex index = TasQuery.AperturePhysicalIndex(new[] { aperture });

            List<ZoneSurfaceKey> plan = TasQuery.ApertureRebindKeys(
                identity, AperturePart.Pane, index, new Dictionary<ZoneSurfaceKey, string>(), identity.BuildingElementGuid(AperturePart.Pane), out string refusal);

            Assert.That(plan, Is.Null);
            Assert.That(refusal, Is.EqualTo("The physical rebind plan is incomplete."));
        }

        // =================================================================================================
        // 6 - two-zone apertures keep their side identity across the refresh
        // =================================================================================================

        [Test]
        public void TwoZoneAperture_KeepsBothSidesAfterARefresh()
        {
            //An internal aperture is met once per zone. Both its surfaces belong to one physical instance, and
            //a refresh must restate both rather than collapsing it to whichever zone it reached first.
            Aperture aperture = Imported();
            ClearForRefresh(aperture);

            aperture.SetApertureZoneSurfaceReferences(AperturePart.Pane, new[] { Reference(ZoneB, 3), Reference(ZoneA, 11) }, out string _);
            aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, Element_TBD2_Pane);

            AperturePhysicalIdentity identity = aperture.AperturePhysicalIdentity();

            Assert.That(identity.Keys(AperturePart.Pane), Has.Count.EqualTo(2));
            Assert.That(identity.Key(AperturePart.Pane, 1), Is.EqualTo(new ZoneSurfaceKey(ZoneA, 11)), "Side order is by zone GUID, not arrival order.");
            Assert.That(identity.Key(AperturePart.Pane, 2), Is.EqualTo(new ZoneSurfaceKey(ZoneB, 3)));
            Assert.That(identity.SurfaceSetComplete(AperturePart.Pane), Is.True);
        }

        // =================================================================================================
        // 7 - the clearing pass reaches BOTH shapes an aperture can be held in
        // =================================================================================================

        /// <summary>
        /// <b>Caught in review, before merge.</b> An <see cref="AdjacencyCluster"/> can hold one aperture on
        /// its panel AND, independently, as a cluster object in its own right - real models carry both (every
        /// aperture on the gbXML route with <c>_importSurfaceShades_</c> on).
        /// <see cref="AdjacencyCluster.GetAperture(System.Guid)"/> and
        /// <see cref="AdjacencyCluster.GetObject{T}(System.Guid)"/> answer with the standalone copy, not the
        /// panel-held one that a refresh restamps (<see cref="AperturePanelIndex"/>).
        /// <see cref="Modify.UpdateApertureDefinitions(TBD.Building, AdjacencyCluster, Core.MaterialLibrary, out List{string})"/>'s
        /// own successful restamp already writes both shapes for exactly this reason - its private
        /// <c>RestampApertureBinding</c> updates the panel copy via <c>AperturePanelIndex</c> and then the
        /// standalone one via <c>GetObject&lt;Aperture&gt;</c>. <c>Modify.UpdateIds</c>'s clearing loop is
        /// COM-driven and cannot run here, but the shape of the fix - clear both, the same way the restamp
        /// writes both - is COM-free, and is pinned directly below.
        /// </summary>
        [Test]
        public void ClearingBothShapes_LeavesNoStaleBindingReachableThroughGetObject()
        {
            //Fixture: one aperture held BOTH ways, exactly as UpdateIds meets it on the gbXML route with
            //_importSurfaceShades_ on. The standalone copy is a snapshot of the imported (bound) state.
            Aperture aperture_Panel = Imported();
            Panel panel = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, Wall());
            Assume.That(panel.AddAperture(aperture_Panel), Is.True, "fixture: the window must sit in the wall");

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddObject(new Aperture(aperture_Panel));

            //THE FIX, spelt out the way UpdateIds now applies it: clear the panel-held copy, then look up and
            //clear the standalone copy by the SAME GUID, and write both back.
            Aperture aperture_PanelHeld = panel.GetAperture(aperture_Panel.Guid);
            aperture_PanelHeld.RemoveApertureTasIdentity();
            panel.RemoveAperture(aperture_PanelHeld.Guid);
            panel.AddAperture(aperture_PanelHeld);
            adjacencyCluster.AddObject(panel);

            Aperture aperture_Standalone = adjacencyCluster.GetObject<Aperture>(aperture_Panel.Guid);
            Assert.That(aperture_Standalone, Is.Not.Null, "fixture: the standalone copy must exist for this test to mean anything");
            aperture_Standalone.RemoveApertureTasIdentity();
            adjacencyCluster.AddObject(aperture_Standalone);

            //Read the way anything downstream that calls GetAperture/GetObject<Aperture> would - and the way
            //the panel walk (AperturePanelIndex, AperturePhysicalIndex) would.
            Aperture read_ViaGetAperture = adjacencyCluster.GetAperture(aperture_Panel.Guid);
            Aperture read_ViaPanel = adjacencyCluster.AperturePanelIndex().GetAperture(aperture_Panel.Guid);

            Assert.Multiple(() =>
            {
                Assert.That(read_ViaGetAperture.AperturePhysicalIdentity().HasStamps, Is.False,
                    "GetAperture answers with the standalone copy - it must read unstamped too, not just the panel copy.");
                Assert.That(read_ViaPanel.AperturePhysicalIdentity().HasStamps, Is.False);
            });
        }

        /// <summary>
        /// The bug this guards against, demonstrated: clearing ONLY the panel-held copy (the gap before the
        /// review fix) leaves <c>GetAperture</c>/<c>GetObject&lt;Aperture&gt;</c> still answering with the
        /// previous TBD's binding, even though the panel copy - the one a refresh actually restamps - is
        /// correctly unstamped.
        /// </summary>
        [Test]
        public void ClearingOnlyThePanelCopy_LeavesGetApertureAnsweringWithTheStaleBinding()
        {
            Aperture aperture_Panel = Imported();
            Panel panel = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, Wall());
            Assume.That(panel.AddAperture(aperture_Panel), Is.True);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddObject(new Aperture(aperture_Panel));

            //Only the panel-held copy cleared - the pre-review-fix behaviour.
            Aperture aperture_PanelHeld = panel.GetAperture(aperture_Panel.Guid);
            aperture_PanelHeld.RemoveApertureTasIdentity();
            panel.RemoveAperture(aperture_PanelHeld.Guid);
            panel.AddAperture(aperture_PanelHeld);
            adjacencyCluster.AddObject(panel);

            Aperture read_ViaGetAperture = adjacencyCluster.GetAperture(aperture_Panel.Guid);
            Aperture read_ViaPanel = adjacencyCluster.AperturePanelIndex().GetAperture(aperture_Panel.Guid);

            Assert.Multiple(() =>
            {
                Assert.That(read_ViaPanel.AperturePhysicalIdentity().HasStamps, Is.False, "the panel copy is correctly unstamped");
                Assert.That(read_ViaGetAperture.AperturePhysicalIdentity().HasStamps, Is.True,
                    "the uncleared standalone copy is what GetAperture hands back, and it is stale - this is the defect the review fix closes");
            });
        }
    }
}
