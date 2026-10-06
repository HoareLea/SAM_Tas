// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The two aperture defects hardened after the reusable-definition programme, in their COM-free
    /// parts.</b>
    /// <para>
    /// <b>1. Aperture identity for a WRITE.</b> An <see cref="AdjacencyCluster"/> can hold one aperture in two
    /// shapes - on its panel, and as a cluster object in its own right - and real models carry both.
    /// <see cref="AdjacencyCluster.GetAperture(System.Guid)"/> answers from the cluster OBJECT first, so an
    /// export reading aperture state through it reads a copy that an ordinary edit
    /// (<c>panel.RemoveAperture</c>/<c>panel.AddAperture</c>) never reached. That is why a SAM aperture
    /// stating <see cref="ApertureParameter.FeatureShade"/> produced a TBD pane with no shade: the write
    /// resolved the wrong copy. <see cref="AperturePanelIndex"/> answers from the panel walk only.
    /// </para>
    /// <para>
    /// <b>2. The importer's pane/frame relationship rule.</b> Stage 2 shares a definition BY VALUE, so two
    /// aperture-construction families with identical pane layers and different frame layers export as one
    /// shared pane construction plus two frame constructions. Pairing a window's halves by their construction
    /// base NAME then puts the second family's pane in the first family's group and its frame in a group of
    /// its own. The relationship key is now the PAIR of construction identities
    /// (<see cref="TasQuery.ApertureConstructionPairKey(string, string)"/>); the name is decided afterwards
    /// and only labels the result.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ApertureHardeningTests
    {
        // =================================================================================================
        // Builders
        // =================================================================================================

        private static ApertureConstruction Glazing(string name = "Glazing", double frameThickness = 0.05)
        {
            return new ApertureConstruction(
                System.Guid.NewGuid(),
                name,
                ApertureType.Window,
                new List<ConstructionLayer> { new ConstructionLayer("Glass 6mm", 0.006) },
                new List<ConstructionLayer> { new ConstructionLayer("Timber", frameThickness) });
        }

        private static FeatureShade Shade()
        {
            return new FeatureShade("shade", null, 1.0, 2.0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, 0.5, 0.0, 0.5);
        }

        /// <summary>A wall with one window in it, held in an <see cref="AdjacencyCluster"/>.</summary>
        private static AdjacencyCluster Cluster(out Panel panel, out Aperture aperture)
        {
            Polygon3D polygon3D_Panel = new Polygon3D(new List<Point3D>
            {
                new Point3D(0, 0, 0),
                new Point3D(10, 0, 0),
                new Point3D(10, 0, 10),
                new Point3D(0, 0, 10)
            });

            Polygon3D polygon3D_Aperture = new Polygon3D(new List<Point3D>
            {
                new Point3D(2, 0, 2),
                new Point3D(4, 0, 2),
                new Point3D(4, 0, 4),
                new Point3D(2, 0, 4)
            });

            //Fully qualified: SAM.Analytical.Tas declares its own Create/Query, which shadow these.
            panel = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, new Face3D(polygon3D_Panel));
            aperture = new Aperture(Glazing(), polygon3D_Aperture);

            Assert.That(panel.AddAperture(aperture), Is.True, "fixture: the window must sit in the wall");

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(panel);

            //Read back: the panel now holds its OWN copy, which is the one every edit lands on.
            aperture = panel.GetAperture(aperture.Guid);

            return adjacencyCluster;
        }

        // =================================================================================================
        // 1. AperturePanelIndex - resolving an aperture for a WRITE
        // =================================================================================================

        // The defect, exactly: the cluster holds a STALE standalone copy of the aperture as well as the
        // panel-held one, the user's FeatureShade is set on the panel-held one (the only shape an ordinary
        // edit reaches), and AdjacencyCluster.GetAperture hands back the stale one.
        [Test]
        public void PanelIndex_StandaloneClusterObject_DoesNotShadowThePanelHeldAperture()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out Panel panel, out Aperture aperture);

            //A stale duplicate, as a cluster object in its own right - the shape every aperture in the
            //licensed fixture is ALSO held in.
            adjacencyCluster.AddObject(new Aperture(aperture));

            //The edit, made the ordinary way: it reaches the PANEL's copy and nothing else.
            Aperture aperture_Shaded = new Aperture(aperture);
            aperture_Shaded.SetValue(SAM.Analytical.ApertureParameter.FeatureShade, Shade());
            panel.RemoveAperture(aperture.Guid);
            panel.AddAperture(aperture_Shaded);
            adjacencyCluster.AddObject(panel);

            Assert.That(adjacencyCluster.GetAperture(aperture.Guid).HasValue(SAM.Analytical.ApertureParameter.FeatureShade), Is.False,
                "the standalone cluster object is what GetAperture answers with, and it is stale");

            Aperture aperture_Resolved = adjacencyCluster.AperturePanelIndex().GetAperture(aperture.Guid);
            Assert.That(aperture_Resolved, Is.Not.Null);
            Assert.That(aperture_Resolved.HasValue(SAM.Analytical.ApertureParameter.FeatureShade), Is.True,
                "the index answers from the panel walk, so it returns the copy the edit landed on");
        }

        // The split/rebind path needs the OWNING PANEL, and GetAperture(guid, out panel) leaves it null on
        // exactly the models above - so every re-stamp refused.
        [Test]
        public void PanelIndex_YieldsTheOwningPanel_WhereGetApertureLeavesItNull()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out Panel panel, out Aperture aperture);
            adjacencyCluster.AddObject(new Aperture(aperture));

            adjacencyCluster.GetAperture(aperture.Guid, out Panel panel_ViaCluster);
            Assert.That(panel_ViaCluster, Is.Null, "the overload returns as soon as the cluster object hits");

            Assert.That(adjacencyCluster.AperturePanelIndex().TryGetValue(aperture.Guid, out Aperture aperture_Indexed, out Panel panel_Indexed), Is.True);
            Assert.That(panel_Indexed.Guid, Is.EqualTo(panel.Guid));
            Assert.That(aperture_Indexed.Guid, Is.EqualTo(aperture.Guid));
        }

        [Test]
        public void PanelIndex_WithoutAStandaloneCopy_StillResolves()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out Panel panel, out Aperture aperture);

            AperturePanelIndex aperturePanelIndex = adjacencyCluster.AperturePanelIndex();
            Assert.That(aperturePanelIndex.Count, Is.EqualTo(1));
            Assert.That(aperturePanelIndex.GetAperture(aperture.Guid), Is.Not.Null);
            Assert.That(aperturePanelIndex.GetPanel(aperture.Guid).Guid, Is.EqualTo(panel.Guid));
        }

        // An aperture no panel holds is not part of the model's physical fabric. The index says so rather
        // than falling back to the cluster object, which is the copy that started the whole problem.
        [Test]
        public void PanelIndex_ApertureNoPanelHolds_IsRefused()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out Panel _, out Aperture _);

            Aperture aperture_Orphan = new Aperture(Glazing(), new Polygon3D(new List<Point3D>
            {
                new Point3D(50, 0, 0),
                new Point3D(51, 0, 0),
                new Point3D(51, 0, 1),
                new Point3D(50, 0, 1)
            }));

            adjacencyCluster.AddObject(aperture_Orphan);

            AperturePanelIndex aperturePanelIndex = adjacencyCluster.AperturePanelIndex();
            Assert.That(aperturePanelIndex.GetAperture(aperture_Orphan.Guid), Is.Null);
            Assert.That(aperturePanelIndex.GetPanel(aperture_Orphan.Guid), Is.Null);
            Assert.That(aperturePanelIndex.TryGetValue(aperture_Orphan.Guid, out Aperture _, out Panel _), Is.False);
        }

        [Test]
        public void PanelIndex_NullCluster_IsEmptyRatherThanThrowing()
        {
            AdjacencyCluster adjacencyCluster = null;
            AperturePanelIndex aperturePanelIndex = adjacencyCluster.AperturePanelIndex();
            Assert.That(aperturePanelIndex.Count, Is.EqualTo(0));
            Assert.That(aperturePanelIndex.GetAperture(System.Guid.NewGuid()), Is.Null);
        }

        // =================================================================================================
        // 2. The importer's relationship key - the PAIR of construction identities
        // =================================================================================================

        // Family A = shared pane P + frame F1, Family B = shared pane P + frame F2. The two families are
        // distinguishable only by the pair; every name-based rule collapses them.
        [Test]
        public void PairKey_SharedPane_DifferentFrames_AreDifferentFamilies()
        {
            string key_A = TasQuery.ApertureConstructionPairKey("P", "F1");
            string key_B = TasQuery.ApertureConstructionPairKey("P", "F2");

            Assert.That(key_A, Is.Not.EqualTo(key_B));
        }

        [Test]
        public void PairKey_SharedFrame_DifferentPanes_AreDifferentFamilies()
        {
            Assert.That(TasQuery.ApertureConstructionPairKey("P1", "F"), Is.Not.EqualTo(TasQuery.ApertureConstructionPairKey("P2", "F")));
        }

        [Test]
        public void PairKey_TheSamePair_IsTheSameFamilyInEveryZone()
        {
            Assert.That(TasQuery.ApertureConstructionPairKey("P", "F1"), Is.EqualTo(TasQuery.ApertureConstructionPairKey(" P ", "F1 ")));
        }

        // A frameless opening is its own family: "pane P, no frame" must never merge into "pane P, frame F",
        // or the import would hand a frameless window a frame it does not have.
        [Test]
        public void PairKey_AnAbsentHalf_IsItsOwnFamily()
        {
            string key_Frameless = TasQuery.ApertureConstructionPairKey("P", null);

            Assert.That(key_Frameless, Is.Not.EqualTo(TasQuery.ApertureConstructionPairKey("P", "F")));
            Assert.That(key_Frameless, Is.EqualTo(TasQuery.ApertureConstructionPairKey("P", "   ")));
            Assert.That(TasQuery.ApertureConstructionPairKey(null, "F"), Is.Not.EqualTo(TasQuery.ApertureConstructionPairKey("F", null)),
                "which half a construction is on is part of the identity");
        }

        // =================================================================================================
        // 3. The family NAME - a label chosen after the identity, never the identity itself
        // =================================================================================================

        [Test]
        public void Name_HalvesAgreeing_IsTheNameTheImportHasAlwaysProduced()
        {
            Assert.That(TasQuery.ApertureConstructionName("Glazing", "Glazing", new List<string>()), Is.EqualTo("Glazing"));
        }

        // The shared-pane case: the second family's pane carries the FIRST family's name, so the free name -
        // its own frame's - is the one that identifies it.
        [Test]
        public void Name_SharedPane_FallsThroughToTheFramesOwnBase()
        {
            Assert.That(TasQuery.ApertureConstructionName("A", "B", new List<string> { "A" }), Is.EqualTo("B"));
        }

        // The inverse: a shared FRAME leaves the pane's base free, and it is taken straight away.
        [Test]
        public void Name_SharedFrame_TakesThePanesOwnBase()
        {
            Assert.That(TasQuery.ApertureConstructionName("D", "C", new List<string> { "C" }), Is.EqualTo("D"));
        }

        [Test]
        public void Name_BothCandidatesTaken_IsDiscriminatedRatherThanDuplicated()
        {
            string name = TasQuery.ApertureConstructionName("A", "B", new List<string> { "A", "B" });

            Assert.That(name, Is.EqualTo("A~2"));
            Assert.That(TasQuery.ApertureConstructionName("A", "B", new List<string> { "A", "B", "A~2" }), Is.EqualTo("A~3"));
        }

        [Test]
        public void Name_OnlyOneHalfStatesAName_UsesIt()
        {
            Assert.That(TasQuery.ApertureConstructionName(null, "Frame Only", null), Is.EqualTo("Frame Only"));
            Assert.That(TasQuery.ApertureConstructionName("Pane Only", "   ", null), Is.EqualTo("Pane Only"));
        }

        [Test]
        public void Name_NeitherHalfStatesAName_IsNull()
        {
            Assert.That(TasQuery.ApertureConstructionName(null, "  ", new List<string> { "A" }), Is.Null);
        }

        // =================================================================================================
        // 4. Stripping the part suffix - the only place a NAME is still read
        // =================================================================================================

        [Test]
        public void NameBase_StripsThePartSuffixAndItsSeparator()
        {
            Assert.That(TasQuery.ApertureConstructionNameBase("Windows: Glazing -pane"), Is.EqualTo("Windows: Glazing"));
            Assert.That(TasQuery.ApertureConstructionNameBase("Windows: Glazing -frame"), Is.EqualTo("Windows: Glazing"));
        }

        [Test]
        public void NameBase_LeavesAnUnsuffixedNameAlone()
        {
            Assert.That(TasQuery.ApertureConstructionNameBase("Curtain Wall"), Is.EqualTo("Curtain Wall"));
            Assert.That(TasQuery.ApertureConstructionNameBase("  Curtain Wall  "), Is.EqualTo("Curtain Wall"));
            Assert.That(TasQuery.ApertureConstructionNameBase(null), Is.Null);
            Assert.That(TasQuery.ApertureConstructionNameBase("   "), Is.Null);
        }

        // The discriminated construction name Stage 2 writes on a collision keeps the suffix TERMINAL, so
        // the base strip still works on it - which is what lets a split definition pair with its own frame.
        [Test]
        public void NameBase_HandlesAStageTwoDiscriminatedName()
        {
            Assert.That(TasQuery.ApertureConstructionNameBase("Glazing_1F3A0C21 -pane"), Is.EqualTo("Glazing_1F3A0C21"));
        }
    }
}
