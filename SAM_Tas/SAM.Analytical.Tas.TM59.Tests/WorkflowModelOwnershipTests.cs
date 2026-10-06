// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using AnalyticalCreate = SAM.Analytical.Create;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The TAS workflow owns its working model - the real TAS identity parameters, on the real types.</b>
    ///
    /// <para><b>The defect</b></para>
    /// <para>
    /// <c>WorkflowCalculator.Calculate</c> has always worked on a copy and handed the result back, so that a
    /// run which failed or was cancelled left the caller's model as it was. The copy was
    /// <c>new AnalyticalModel(analyticalModel)</c>, which rebuilds the cluster's dictionaries but stores the
    /// <b>same</b> <c>Space</c>, <c>Panel</c> and <c>Aperture</c> instances. That is safe for an operation
    /// writing by same-guid replacement - <c>SAM.Analytical.Modify.EvaluateTargetedDesignAirFlows</c> states
    /// exactly that rule at its own boundary - and it is not safe for this one:
    /// <see cref="Modify.UpdateIds"/> reads the live objects out of the cluster and stamps
    /// <c>SpaceParameter.ZoneGuid</c>, <c>PanelParameter.ZoneSurfaceReference_1</c>/<c>_2</c>,
    /// <c>PanelParameter.BuildingElementGuid</c> and the aperture identity parameters straight onto their
    /// parameter sets <b>in place</b>.
    /// </para>
    /// <para>
    /// So every TAS identity a run stamped was visible through the model it was given. On the Iteration 2B
    /// optimisation path that model is the <b>retained last-valid design</b> of the previous round, and a
    /// round that then failed or was cancelled handed that design back as the answer with a later run's TAS
    /// identities on it - disagreeing with its own persisted
    /// <c>SimulationResultProvenance.Fingerprint_Model</c>, which is a false "the model has changed since
    /// the simulation results were produced from it" on reopening and a re-simulation nobody needed.
    /// </para>
    ///
    /// <para><b>What is tested here, and why it does not need a TAS licence</b></para>
    /// <para>
    /// <see cref="Modify.UpdateIds"/> itself needs a <c>TBD.Building</c>, so the stamping is reproduced here
    /// by writing the same parameters, on the same object types, through the same
    /// read-mutate-<c>AddObject</c> sequence <c>UpdateIds</c> uses - including the
    /// <c>RemoveAperture</c>/<c>AddAperture</c> pairing it writes an aperture back into its panel with. What
    /// is being pinned is the OWNERSHIP of the objects those writes land on, which is decided entirely by
    /// the copy the workflow takes and not at all by TAS. The parameters are the production enums rather
    /// than stand-ins, so a rename cannot leave this test passing against something the conversion no
    /// longer writes.
    /// </para>
    /// <para>
    /// The corresponding assertion on the copy itself - that the shallow constructor shares and the deep one
    /// does not - is in <c>SAM.Tests.AnalyticalModelWorkingCopyTests</c>, beside the constructor.
    /// </para>
    /// </summary>
    [TestFixture]
    public class WorkflowModelOwnershipTests
    {
        private const string zoneGuid = "{6F1B0F2E-0000-4000-8000-0000000000AA}";

        private const string buildingElementGuid = "{6F1B0F2E-0000-4000-8000-0000000000BB}";

        private static readonly Construction construction_Wall = new(Guid.NewGuid(), "Wall");

        private static readonly ApertureConstruction apertureConstruction_Window = new(Guid.NewGuid(), "Window", ApertureType.Window);

        private static Point3D P(double x, double y, double z) => new(x, y, z);

        /// <summary>
        /// One space, one wall panel carrying one window, and the space-to-panel relation - every object
        /// shape <see cref="Modify.UpdateIds"/> stamps identity onto, including an aperture held inside a
        /// panel rather than standing alone in the cluster.
        /// </summary>
        private static AnalyticalModel Model()
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space = new("Bedroom 1", P(5, 5, 1.5));

            Face3D face3D_Panel = new(new Polygon3D(new List<Point3D> { P(0, 0, 0), P(10, 0, 0), P(10, 0, 3), P(0, 0, 3) }));
            Panel panel = AnalyticalCreate.Panel(construction_Wall, PanelType.Wall, face3D_Panel);

            Face3D face3D_Aperture = new(new Polygon3D(new List<Point3D> { P(2, 0, 1), P(4, 0, 1), P(4, 0, 2), P(2, 0, 2) }));
            panel.AddAperture(AnalyticalCreate.Aperture(apertureConstruction_Window, face3D_Aperture));

            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(space, panel);

            return new AnalyticalModel("Flat1", null, null, null, adjacencyCluster);
        }

        /// <summary>
        /// The identity stamping <see cref="Modify.UpdateIds"/> performs, by the same means: the live
        /// objects, mutated in place, put back with <c>AddObject</c>. Nothing here is a replacement.
        /// </summary>
        private static void StampTasIdentity(AnalyticalModel analyticalModel)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            foreach (Space space in adjacencyCluster.GetSpaces() ?? new List<Space>())
            {
                space.SetValue(SpaceParameter.ZoneGuid, zoneGuid);
                adjacencyCluster.AddObject(space);
            }

            foreach (Panel panel in adjacencyCluster.GetPanels() ?? new List<Panel>())
            {
                panel.SetValue(PanelParameter.ZoneSurfaceReference_1, new Core.Tas.ZoneSurfaceReference(1, zoneGuid));
                panel.SetValue(PanelParameter.ZoneSurfaceReference_2, new Core.Tas.ZoneSurfaceReference(2, zoneGuid));
                panel.SetValue(PanelParameter.BuildingElementGuid, buildingElementGuid);

                foreach (Aperture aperture in panel.Apertures ?? new List<Aperture>())
                {
                    aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, buildingElementGuid);
                    aperture.SetValue(ApertureParameter.PaneZoneSurfaceReference_1, new Core.Tas.ZoneSurfaceReference(1, zoneGuid));

                    panel.RemoveAperture(aperture.Guid);
                    panel.AddAperture(aperture);
                }

                adjacencyCluster.AddObject(panel);
            }
        }

        /// <summary>Whether any object in the model carries any of the TAS identity stamps.</summary>
        private static bool HasTasIdentity(AnalyticalModel analyticalModel)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            foreach (Space space in adjacencyCluster.GetSpaces() ?? new List<Space>())
            {
                if (space.HasValue(SpaceParameter.ZoneGuid))
                {
                    return true;
                }
            }

            foreach (Panel panel in adjacencyCluster.GetPanels() ?? new List<Panel>())
            {
                if (panel.HasValue(PanelParameter.ZoneSurfaceReference_1)
                    || panel.HasValue(PanelParameter.ZoneSurfaceReference_2)
                    || panel.HasValue(PanelParameter.BuildingElementGuid))
                {
                    return true;
                }

                foreach (Aperture aperture in panel.Apertures ?? new List<Aperture>())
                {
                    if (aperture.HasValue(ApertureParameter.PaneBuildingElementGuid)
                        || aperture.HasValue(ApertureParameter.PaneZoneSurfaceReference_1))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // =================================================================================================
        // The defect, reproduced
        // =================================================================================================

        /// <summary>
        /// The copy the workflow used to take shares its objects, so the stamping reaches the caller. Stated
        /// rather than merely fixed, so the reason the deep copy is load-bearing stays legible - and so a
        /// change that makes the ordinary copy constructor deep is noticed rather than silently paid for on
        /// every read path.
        /// </summary>
        [Test]
        public void TheShallowWorkflowCopy_LetsTasIdentityReachTheCallersModel()
        {
            AnalyticalModel analyticalModel_Caller = Model();

            AnalyticalModel analyticalModel_Working = new(analyticalModel_Caller);

            StampTasIdentity(analyticalModel_Working);

            Assert.That(HasTasIdentity(analyticalModel_Caller), Is.True);
        }

        // =================================================================================================
        // The rule
        // =================================================================================================

        /// <summary>
        /// <b>Test 1.</b> The working copy the workflow now takes receives every TAS identity stamp and the
        /// caller's model receives none of them.
        /// </summary>
        [Test]
        public void TheDeepWorkflowCopy_KeepsTasIdentityOffTheCallersModel()
        {
            AnalyticalModel analyticalModel_Caller = Model();

            AnalyticalModel analyticalModel_Working = new(analyticalModel_Caller, true);

            StampTasIdentity(analyticalModel_Working);

            Assert.That(HasTasIdentity(analyticalModel_Caller), Is.False, "The caller's model was stamped by the workflow's working copy.");

            //And the working model really was stamped, so the isolation above is not the stamp having
            //silently failed.
            Assert.That(HasTasIdentity(analyticalModel_Working), Is.True);
        }

        /// <summary>
        /// <b>Test 2.</b> A failed or cancelled round leaves the retained last-valid design's persisted
        /// provenance still valid for it - the fingerprint an engineer's reopened session is paired with its
        /// results by.
        /// <para>
        /// This is the whole consequence of the defect in one assertion. The stamping is the round; the
        /// round then produces nothing, so the last-valid model is handed back unchanged - and
        /// <c>IsCurrent</c> still agrees with it.
        /// </para>
        /// </summary>
        [Test]
        public void AFailedOrCancelledRound_LeavesTheLastValidModelsProvenanceValid()
        {
            AnalyticalModel analyticalModel_LastValid = Model();

            string path_TSD = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".tsd");
            System.IO.File.WriteAllText(path_TSD, "results");

            try
            {
                SimulationResultProvenance simulationResultProvenance = new(analyticalModel_LastValid, path_TSD);

                Assert.That(simulationResultProvenance.IsCurrent(analyticalModel_LastValid), Is.True, "The fixture's own provenance must be current before the round runs.");

                //The round: a working copy, converted, and then abandoned - nothing adopted.
                AnalyticalModel analyticalModel_Round = new(analyticalModel_LastValid, true);
                StampTasIdentity(analyticalModel_Round);

                Assert.That(simulationResultProvenance.IsCurrent(analyticalModel_LastValid), Is.True,
                    "A round that produced nothing moved the retained last-valid design's fingerprint, so its own saved results would be refused on reopening.");
            }
            finally
            {
                System.IO.File.Delete(path_TSD);
            }
        }

        /// <summary>
        /// <b>Test 4.</b> A successful run still adopts the stamped model. The isolation must not have been
        /// bought by losing the identities the run exists to produce - without them the DomOv export names
        /// SAM space guids the TBD beside it cannot resolve, and <c>SimulationSpaceMap</c> pairs nothing.
        /// </summary>
        [Test]
        public void ASuccessfulRun_StillAdoptsTheStampedModel()
        {
            AnalyticalModel analyticalModel_Caller = Model();

            AnalyticalModel analyticalModel_Working = new(analyticalModel_Caller, true);
            StampTasIdentity(analyticalModel_Working);

            //Adoption is the caller taking what came back, which is what every Part O run does on success.
            AnalyticalModel analyticalModel_Adopted = analyticalModel_Working;

            AdjacencyCluster adjacencyCluster = analyticalModel_Adopted.AdjacencyCluster;

            Space space = adjacencyCluster.GetSpaces()[0];
            Assert.That(space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid_Adopted), Is.True);
            Assert.That(zoneGuid_Adopted, Is.EqualTo(zoneGuid));

            Panel panel = adjacencyCluster.GetPanels()[0];
            Assert.That(panel.TryGetValue(PanelParameter.BuildingElementGuid, out string buildingElementGuid_Adopted), Is.True);
            Assert.That(buildingElementGuid_Adopted, Is.EqualTo(buildingElementGuid));

            //The aperture held inside the panel kept its own stamp through the Remove/Add pairing.
            Aperture aperture = panel.Apertures[0];
            Assert.That(aperture.TryGetValue(ApertureParameter.PaneBuildingElementGuid, out string _), Is.True);
        }

        /// <summary>
        /// <b>Test 5.</b> The warm-start path does not regress because identities happen to be equal.
        /// <para>
        /// A round started from a canonical TBD re-stamps the identities the baseline already wrote, so the
        /// working copy's stamps are <b>the same values</b> the caller's model carries. The isolation must
        /// therefore not be inferred from the values differing - and the caller must not be mutated even
        /// where the mutation would be invisible in the parameters. Equal values on distinct instances is
        /// exactly the state a value-comparing test would pass on while the objects were still shared.
        /// </para>
        /// </summary>
        [Test]
        public void TheWarmStartPath_IsIsolatedEvenWhenTheIdentitiesAreEqual()
        {
            AnalyticalModel analyticalModel_Caller = Model();

            //The baseline's own stamps, already on the caller's model.
            StampTasIdentity(analyticalModel_Caller);

            string fingerprint_Before = SimulationResultProvenance.Fingerprint(analyticalModel_Caller);

            AnalyticalModel analyticalModel_Working = new(analyticalModel_Caller, true);

            //The warm-started round re-stamps the same identities, and the fingerprint is unmoved because
            //the values are unchanged - which is the point: this test's evidence is the INSTANCES.
            StampTasIdentity(analyticalModel_Working);

            Assert.That(SimulationResultProvenance.Fingerprint(analyticalModel_Caller), Is.EqualTo(fingerprint_Before));

            AdjacencyCluster adjacencyCluster_Caller = analyticalModel_Caller.AdjacencyCluster;
            AdjacencyCluster adjacencyCluster_Working = analyticalModel_Working.AdjacencyCluster;

            Space space_Caller = adjacencyCluster_Caller.GetSpaces()[0];
            Space space_Working = adjacencyCluster_Working.GetSpaces()[0];

            Assert.That(space_Working.Guid, Is.EqualTo(space_Caller.Guid), "Identity must be preserved - the relations and the simulation space map depend on it.");
            Assert.That(ReferenceEquals(space_Working, space_Caller), Is.False, "The working model must own its own space even where the stamped values are identical.");

            //And the warm-started round's own stamp is really there, so a later step reading it resolves.
            Assert.That(space_Working.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid_Working), Is.True);
            Assert.That(zoneGuid_Working, Is.EqualTo(zoneGuid));
        }

        /// <summary>
        /// The deep copy keeps the relations and the aperture ownership <see cref="Modify.UpdateIds"/> reads
        /// through - <c>GetPanels(space)</c> for the zone's surfaces, and <c>panel.Apertures</c> for the
        /// aperture parts. A clone that stepped beside the original rather than into its place would leave
        /// the conversion matching nothing.
        /// </summary>
        [Test]
        public void TheDeepWorkflowCopy_KeepsTheRelationsTheConversionReadsThrough()
        {
            AnalyticalModel analyticalModel_Working = new(Model(), true);

            AdjacencyCluster adjacencyCluster = analyticalModel_Working.AdjacencyCluster;

            List<Space> spaces = adjacencyCluster.GetSpaces();
            Assert.That(spaces, Has.Count.EqualTo(1));

            List<Panel> panels = adjacencyCluster.GetPanels(spaces[0]);
            Assert.That(panels, Is.Not.Null);
            Assert.That(panels, Has.Count.EqualTo(1));

            Assert.That(panels[0].Apertures, Has.Count.EqualTo(1));

            //The standalone cluster copy of a panel and the related one are the same object, so a stamp
            //written through either is read back through the other - the equality UpdateIds' own
            //aperture-clearing pass depends on.
            Assert.That(ReferenceEquals(adjacencyCluster.GetObject<Panel>(panels[0].Guid), panels[0]), Is.True);
        }
    }
}
