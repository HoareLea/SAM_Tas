// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using ApertureBuildingElementUsage = SAM.Analytical.Tas.ApertureBuildingElementUsage;
using ApertureDefinitionBinding = SAM.Analytical.Tas.ApertureDefinitionBinding;
using BuildingElementDefinition = SAM.Analytical.Tas.BuildingElementDefinition;
using ConstructionDefinition = SAM.Analytical.Tas.ConstructionDefinition;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The standard Grasshopper/SAM_UI gbXML workflow gets the same reusable aperture definitions the
    /// direct <c>SAMAnalytical.TBD</c> export already has.</b>
    /// <para>
    /// On the gbXML route SAM_Tas does not write the TBD: TAS's own <c>T3DDocument.ExportNew</c> does, from a
    /// T3D in which every aperture is its own <c>window</c> - it has to be, because the gbXML opening name
    /// carries the aperture GUID and <c>Query.UpdateT3D</c> decodes it back to find the SAM aperture. TAS
    /// therefore creates one aperture building element and one construction PER APERTURE PER PART, named
    /// after that aperture, and nothing afterwards collapsed them.
    /// <c>Modify.UpdateApertureDefinitions</c> closes that gap.
    /// </para>
    /// <para>
    /// <b>Why these tests need no installed TAS.</b> Everything that DECIDES is COM-free:
    /// <c>Query.ApertureDefinitionBindings</c> asks, per aperture per part, which reusable definition it wants
    /// - through the very <c>ConstructionDefinition</c>/<c>BuildingElementDefinition</c> factories
    /// <c>Modify.ResolveApertureDefinition</c> resolves against, so there is no second set of equality rules
    /// to drift. <c>Query.UnusedApertureBuildingElementGuids</c> and
    /// <c>Query.OrphanApertureConstructionNames</c> decide the sweep from values read out of COM. What
    /// genuinely needs COM - assigning an element to a surface, and
    /// <c>Building.DeleteMarkedBuildingElements</c> - is the licensed acceptance's job, not this fixture's.
    /// </para>
    /// <para>
    /// <b>The identity rule these must never break.</b> Many physical <c>zoneSurface</c>s point at few shared
    /// definitions; the apertures themselves are never merged. So every test that asserts a definition COUNT
    /// also asserts the physical binding count is untouched.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ApertureDefinitionReuseGbXMLTests
    {
        private static readonly string[] DayTypes = { "Weekday", "Saturday", "Sunday" };

        private const string GlazingName = "SIM_EXT_GLZ";

        private const int Count_IdenticalWindows = 20;

        // =================================================================================================
        // Builders - the same SAM-side shapes ApertureDefinitionReuseTests uses
        // =================================================================================================

        private static MaterialLibrary Library()
        {
            MaterialLibrary materialLibrary = new MaterialLibrary("Test");

            materialLibrary.Add(new TransparentMaterial("Glass 6mm", "Glass", "Glass 6mm", "Clear float", 1.0, 750, 2500));
            materialLibrary.Add(new GasMaterial("Air 16mm", "Gas", "Air 16mm", "Cavity", 0.025, 1000, 1.2, 0.0000181));
            materialLibrary.Add(new OpaqueMaterial("Timber", "Frame", "Timber", "Softwood", 0.13, 1600, 500));

            return materialLibrary;
        }

        private static ApertureConstruction Glazing(string name = GlazingName, IEnumerable<ConstructionLayer> frameConstructionLayers = null)
        {
            return new ApertureConstruction(
                Guid.NewGuid(),
                name,
                ApertureType.Window,
                new List<ConstructionLayer>
                {
                    new ConstructionLayer("Glass 6mm", 0.006),
                    new ConstructionLayer("Air 16mm", 0.016),
                    new ConstructionLayer("Glass 6mm", 0.006)
                },
                frameConstructionLayers ?? new List<ConstructionLayer> { new ConstructionLayer("Timber", 0.05) });
        }

        private static Aperture Window(ApertureConstruction apertureConstruction, IOpeningProperties openingProperties, double offset)
        {
            //A distinct, real polygon per aperture: the point is that N DIFFERENT physical apertures resolve
            //to FEW definitions, so they must not be the same object.
            Polygon3D polygon3D = new Polygon3D(new List<Point3D>
            {
                new Point3D(offset, 0, 0),
                new Point3D(offset + 1, 0, 0),
                new Point3D(offset + 1, 0, 1),
                new Point3D(offset, 0, 1)
            });

            Aperture aperture = new Aperture(apertureConstruction, polygon3D);

            if (openingProperties != null)
            {
                aperture.SetValue(SAM.Analytical.ApertureParameter.OpeningProperties, openingProperties);
            }

            return aperture;
        }

        private static PartOOpeningProperties PartO(double dischargeCoefficient = 1.2)
        {
            return new PartOOpeningProperties(dischargeCoefficient, 1.0, 30.0, OpeningRestriction.Unrestricted);
        }

        /// <summary>
        /// The fixture the whole gap is about: N identical windows off ONE reusable
        /// <c>ApertureConstruction</c>, stating one opening control - which is what the gbXML route turns into
        /// N GUID-named element pairs.
        /// </summary>
        private static List<Aperture> IdenticalWindows(int count = Count_IdenticalWindows, ApertureConstruction apertureConstruction = null)
        {
            ApertureConstruction apertureConstruction_Temp = apertureConstruction ?? Glazing();

            List<Aperture> result = new List<Aperture>(count);
            for (int index = 0; index < count; index++)
            {
                result.Add(Window(apertureConstruction_Temp, PartO(), index * 2.0));
            }

            return result;
        }

        private static List<ApertureDefinitionBinding> Bindings(IEnumerable<Aperture> apertures, MaterialLibrary materialLibrary = null)
        {
            return TasQuery.ApertureDefinitionBindings(apertures, materialLibrary ?? Library(), DayTypes);
        }

        private static List<ApertureDefinitionBinding> Panes(IEnumerable<ApertureDefinitionBinding> bindings)
        {
            return bindings.Where(x => x.AperturePart == AperturePart.Pane).ToList();
        }

        private static List<ApertureDefinitionBinding> Frames(IEnumerable<ApertureDefinitionBinding> bindings)
        {
            return bindings.Where(x => x.AperturePart == AperturePart.Frame).ToList();
        }

        /// <summary>
        /// The names the pass would create for these definitions - the thing that must never carry a physical
        /// aperture GUID.
        /// </summary>
        private static List<string> GeneratedNames(IEnumerable<BuildingElementDefinition> buildingElementDefinitions, IEnumerable<ConstructionDefinition> constructionDefinitions)
        {
            List<string> result = new List<string>();

            foreach (ConstructionDefinition constructionDefinition in constructionDefinitions)
            {
                string name = TasQuery.ConstructionName(result, constructionDefinition, GlazingName, out string _);
                if (name != null)
                {
                    result.Add(name);
                }
            }

            foreach (BuildingElementDefinition buildingElementDefinition in buildingElementDefinitions)
            {
                string name = TasQuery.BuildingElementName(result, buildingElementDefinition, GlazingName, out string _);
                if (name != null)
                {
                    result.Add(name);
                }
            }

            return result;
        }

        // =================================================================================================
        // Test A - N identical windows through the gbXML route
        // =================================================================================================

        /// <summary>
        /// Twenty identical windows ask for exactly TWO reusable building elements and TWO reusable
        /// constructions - the same answer the direct route gives - while all forty physical aperture parts
        /// remain forty.
        /// </summary>
        [Test]
        public void GbXMLRoute_IdenticalWindows_AskForOnePaneAndOneFrameDefinition()
        {
            List<Aperture> apertures = IdenticalWindows();

            List<ApertureDefinitionBinding> bindings = Bindings(apertures);

            //The PHYSICAL count is untouched: one binding per window per part. This is the invariant that
            //separates "few shared definitions" from "merged apertures".
            Assert.That(bindings, Has.Count.EqualTo(Count_IdenticalWindows * 2), "The physical aperture parts were not preserved one per window per part.");
            Assert.That(Panes(bindings), Has.Count.EqualTo(Count_IdenticalWindows));
            Assert.That(Frames(bindings), Has.Count.EqualTo(Count_IdenticalWindows));
            Assert.That(bindings.Select(x => x.ApertureGuid).Distinct().Count(), Is.EqualTo(Count_IdenticalWindows), "Two bindings claim the same physical aperture.");

            //The DEFINITION count is two: one pane, one frame.
            List<BuildingElementDefinition> buildingElementDefinitions = bindings.DistinctBuildingElementDefinitions();
            List<ConstructionDefinition> constructionDefinitions = bindings.DistinctConstructionDefinitions();

            Assert.That(buildingElementDefinitions, Has.Count.EqualTo(2), "Twenty identical windows did not collapse to one pane and one frame building element definition.");
            Assert.That(constructionDefinitions, Has.Count.EqualTo(2), "Twenty identical windows did not collapse to one pane and one frame construction.");

            Assert.That(buildingElementDefinitions.ConvertAll(x => x.AperturePart), Is.EquivalentTo(new[] { AperturePart.Pane, AperturePart.Frame }));
            Assert.That(constructionDefinitions.ConvertAll(x => x.AperturePart), Is.EquivalentTo(new[] { AperturePart.Pane, AperturePart.Frame }));
        }

        /// <summary>
        /// No name the pass would generate carries a physical aperture GUID. That is the whole difference
        /// between a reusable definition and the instance-named object TAS leaves behind - an instance-named
        /// definition can never be found again by anything but itself.
        /// </summary>
        [Test]
        public void GbXMLRoute_GeneratedDefinitionNames_CarryNoPhysicalApertureGuid()
        {
            List<Aperture> apertures = IdenticalWindows();
            List<ApertureDefinitionBinding> bindings = Bindings(apertures);

            List<string> names = GeneratedNames(bindings.DistinctBuildingElementDefinitions(), bindings.DistinctConstructionDefinitions());

            Assert.That(names, Has.Count.EqualTo(4), "Two constructions and two building elements should have been named.");
            Assert.That(TasQuery.NamesContainingApertureGuid(names, apertures.ConvertAll(x => x.Guid)), Is.Empty, "A generated reusable definition name carries a physical aperture GUID.");

            //And they are the shapes the import reads back, exactly as the direct route writes them.
            Assert.That(names, Contains.Item(GlazingName + " -pane"));
            Assert.That(names, Contains.Item(GlazingName + " -frame"));
            Assert.That(names, Contains.Item("Windows: " + GlazingName + " -pane"));
            Assert.That(names, Contains.Item("Windows: " + GlazingName + " -frame"));
        }

        // =================================================================================================
        // Test B - one diverging aperture
        // =================================================================================================

        /// <summary>
        /// One window given a different opening control needs ONE extra pane definition, bound to it alone.
        /// The other nineteen stay on the shared original, and the frame definition is untouched - a frame
        /// carries no opening, so nothing about a control divergence reaches it.
        /// </summary>
        [Test]
        public void GbXMLRoute_OneDivergingAperture_AddsExactlyOneDefinitionBoundToItAlone()
        {
            ApertureConstruction apertureConstruction = Glazing();

            List<Aperture> apertures = IdenticalWindows(Count_IdenticalWindows, apertureConstruction);

            //The divergence: this one window's opening states a different discharge coefficient, so its
            //effective aperture control - and therefore its pane element - is a different definition.
            Aperture aperture_Diverged = Window(apertureConstruction, PartO(0.6), 1000.0);
            apertures.Add(aperture_Diverged);

            List<ApertureDefinitionBinding> bindings = Bindings(apertures);

            Assert.That(bindings, Has.Count.EqualTo((Count_IdenticalWindows + 1) * 2), "The physical aperture parts were not preserved.");

            List<BuildingElementDefinition> buildingElementDefinitions = bindings.DistinctBuildingElementDefinitions();
            Assert.That(buildingElementDefinitions, Has.Count.EqualTo(3), "A diverging aperture did not produce exactly one additional building element definition.");

            //Constructions are unaffected: the divergence is a control, not a layer.
            Assert.That(bindings.DistinctConstructionDefinitions(), Has.Count.EqualTo(2), "A control divergence should not add a construction.");

            //Only the diverged aperture binds the new definition; every other pane stays on the shared one.
            BuildingElementDefinition definition_Diverged = Panes(bindings).First(x => x.ApertureGuid == aperture_Diverged.Guid).BuildingElementDefinition;
            List<Guid> onDivergedDefinition = Panes(bindings).Where(x => x.BuildingElementDefinition.Equals(definition_Diverged)).Select(x => x.ApertureGuid).ToList();

            Assert.That(onDivergedDefinition, Is.EqualTo(new List<Guid> { aperture_Diverged.Guid }), "The diverging definition is bound by more than the diverging aperture.");

            List<BuildingElementDefinition> paneDefinitions = Panes(bindings).DistinctBuildingElementDefinitions();
            Assert.That(paneDefinitions, Has.Count.EqualTo(2));
            Assert.That(Frames(bindings).DistinctBuildingElementDefinitions(), Has.Count.EqualTo(1), "The frame definition changed when only a pane control diverged.");

            //The nineteen unchanged windows are all still on one definition between them.
            BuildingElementDefinition definition_Shared = paneDefinitions.First(x => !x.Equals(definition_Diverged));
            Assert.That(Panes(bindings).Count(x => x.BuildingElementDefinition.Equals(definition_Shared)), Is.EqualTo(Count_IdenticalWindows), "The unchanged windows did not all stay on the original shared definition.");
        }

        // =================================================================================================
        // Test C - merge-back
        // =================================================================================================

        /// <summary>
        /// Restoring the diverged window's control puts it back on the ORIGINAL shared definition - it does
        /// not need, and must not get, a second equivalent definition of its own. This is what makes a
        /// divergence reversible rather than a one-way ratchet.
        /// </summary>
        [Test]
        public void GbXMLRoute_ADivergenceRestored_ReusesTheOriginalSharedDefinition()
        {
            ApertureConstruction apertureConstruction = Glazing();
            MaterialLibrary materialLibrary = Library();

            List<Aperture> apertures = IdenticalWindows(Count_IdenticalWindows, apertureConstruction);

            //Before: everything shared.
            BuildingElementDefinition definition_Original = Panes(Bindings(apertures, materialLibrary)).DistinctBuildingElementDefinitions().Single();

            //Diverged.
            Aperture aperture_Diverged = Window(apertureConstruction, PartO(0.6), 1000.0);
            apertures.Add(aperture_Diverged);
            Assert.That(Panes(Bindings(apertures, materialLibrary)).DistinctBuildingElementDefinitions(), Has.Count.EqualTo(2), "The divergence did not take effect, so the merge-back would prove nothing.");

            //Restored to the original effective values.
            apertures.Remove(aperture_Diverged);
            Aperture aperture_Restored = Window(apertureConstruction, PartO(), 1000.0);
            apertures.Add(aperture_Restored);

            List<ApertureDefinitionBinding> bindings = Bindings(apertures, materialLibrary);

            List<BuildingElementDefinition> paneDefinitions = Panes(bindings).DistinctBuildingElementDefinitions();
            Assert.That(paneDefinitions, Has.Count.EqualTo(1), "Restoring the diverged aperture left a duplicate equivalent pane definition.");
            Assert.That(paneDefinitions.Single(), Is.EqualTo(definition_Original), "The restored aperture did not resolve back to the ORIGINAL shared definition.");

            Assert.That(bindings.DistinctBuildingElementDefinitions(), Has.Count.EqualTo(2));
            Assert.That(bindings, Has.Count.EqualTo((Count_IdenticalWindows + 1) * 2), "The physical aperture parts changed during the merge-back.");
        }

        // =================================================================================================
        // Test D - pane and frame never collapse
        // =================================================================================================

        /// <summary>
        /// A window whose frame layers are IDENTICAL to its pane layers still needs two definitions. The
        /// import pairs a window's two halves by the <c>-pane</c>/<c>-frame</c> suffix, so a collapse here
        /// would lose half the window on the round trip - and the two elements carry different
        /// <c>BEType</c>s (<c>GLAZING</c> and <c>FRAMEELEMENT</c>), which is what TAS simulates from.
        /// </summary>
        [Test]
        public void GbXMLRoute_IdenticalPaneAndFrameContent_StillNeedsTwoDefinitions()
        {
            //The frame given the pane's own layers, exactly - the case where nothing but the part tells them
            //apart.
            ApertureConstruction apertureConstruction = Glazing(frameConstructionLayers: new List<ConstructionLayer>
            {
                new ConstructionLayer("Glass 6mm", 0.006),
                new ConstructionLayer("Air 16mm", 0.016),
                new ConstructionLayer("Glass 6mm", 0.006)
            });

            List<ApertureDefinitionBinding> bindings = Bindings(IdenticalWindows(Count_IdenticalWindows, apertureConstruction));

            List<ConstructionDefinition> constructionDefinitions = bindings.DistinctConstructionDefinitions();
            Assert.That(constructionDefinitions, Has.Count.EqualTo(2), "A pane and a frame with identical layers collapsed into one construction.");
            Assert.That(constructionDefinitions.ConvertAll(x => x.AperturePart), Is.EquivalentTo(new[] { AperturePart.Pane, AperturePart.Frame }));

            List<BuildingElementDefinition> buildingElementDefinitions = bindings.DistinctBuildingElementDefinitions();
            Assert.That(buildingElementDefinitions, Has.Count.EqualTo(2), "A pane and a frame with identical layers collapsed into one building element.");

            //GLAZING and FRAMEELEMENT, not one type twice.
            Assert.That(buildingElementDefinitions.ConvertAll(x => x.BEType), Is.EquivalentTo(new[] { TasQuery.BEType(AperturePart.Pane), TasQuery.BEType(AperturePart.Frame) }));
            Assert.That(TasQuery.BEType(AperturePart.Pane), Is.Not.EqualTo(TasQuery.BEType(AperturePart.Frame)));

            //And their generated names still differ, so neither can adopt the other.
            List<string> names = GeneratedNames(buildingElementDefinitions, constructionDefinitions);
            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Count), "A pane and a frame were given the same name.");
        }

        // =================================================================================================
        // Test E - repeated workflow
        // =================================================================================================

        /// <summary>
        /// Running the resolution twice over the same model asks for the SAME definitions - so the second run
        /// finds every one of them already in the TBD and creates nothing. This is what makes a repeated
        /// gbXML export stable instead of growing a definition per run.
        /// </summary>
        [Test]
        public void GbXMLRoute_RepeatedRun_AsksForTheSameDefinitionsAndSoAddsNone()
        {
            MaterialLibrary materialLibrary = Library();
            List<Aperture> apertures = IdenticalWindows();

            List<BuildingElementDefinition> run_1 = Bindings(apertures, materialLibrary).DistinctBuildingElementDefinitions();
            List<BuildingElementDefinition> run_2 = Bindings(apertures, materialLibrary).DistinctBuildingElementDefinitions();

            Assert.That(run_2, Has.Count.EqualTo(run_1.Count), "A repeated run asked for a different number of definitions.");

            //Every definition the second run asks for is one the first run already produced, by VALUE - which
            //is what the reuse cache matches on, so the second run creates nothing.
            foreach (BuildingElementDefinition buildingElementDefinition in run_2)
            {
                Assert.That(run_1.Exists(x => x.Equals(buildingElementDefinition)), Is.True, "A repeated run asked for a definition the first run did not produce, so the second run would create one.");
            }

            //Names are stable too: derived from the ApertureConstruction and the signature, never from a
            //counter, so the same definition resolves to the same name on every run.
            List<string> names_1 = GeneratedNames(run_1, Bindings(apertures, materialLibrary).DistinctConstructionDefinitions());
            List<string> names_2 = GeneratedNames(run_2, Bindings(apertures, materialLibrary).DistinctConstructionDefinitions());
            Assert.That(names_2, Is.EqualTo(names_1), "The generated definition names are not stable across runs.");
        }

        // =================================================================================================
        // The instance-named guard - what must never be adopted as a shared definition
        // =================================================================================================

        /// <summary>
        /// The names TAS's gbXML conversion produces are recognised as instance-named, in both GUID
        /// spellings and either case, while the definition-named ones are not. Without this the reuse cache
        /// would hand a per-aperture element over as reusable and twenty windows would share a definition
        /// named after whichever one of them came first.
        /// </summary>
        [Test]
        public void InstanceNamedDefinitions_AreRecognisedInEitherGuidSpellingAndCase()
        {
            Guid apertureGuid = Guid.NewGuid();
            Guid apertureGuid_Other = Guid.NewGuid();

            List<string> names =
            [
                "Windows: " + GlazingName + " " + apertureGuid.ToString("D") + " -pane",
                "Windows: " + GlazingName + " " + apertureGuid.ToString("D").ToUpperInvariant() + " -frame",
                GlazingName + " " + apertureGuid.ToString("N") + " -pane",
                "Windows: " + GlazingName + " -pane",
                GlazingName + " -frame",
                "External Wall"
            ];

            List<string> instanceNamed = TasQuery.NamesContainingApertureGuid(names, new List<Guid> { apertureGuid });

            Assert.That(instanceNamed, Has.Count.EqualTo(3), "An instance-named definition was not recognised, or a definition-named one was.");
            Assert.That(instanceNamed, Has.None.EqualTo("Windows: " + GlazingName + " -pane"));
            Assert.That(instanceNamed, Has.None.EqualTo(GlazingName + " -frame"));
            Assert.That(instanceNamed, Has.None.EqualTo("External Wall"));

            //Another aperture's GUID does not make a name instance-named for THIS aperture set.
            Assert.That(TasQuery.NamesContainingApertureGuid(names, new List<Guid> { apertureGuid_Other }), Is.Empty);

            //No aperture GUIDs at all: nothing is instance-named.
            Assert.That(TasQuery.NamesContainingApertureGuid(names, new List<Guid>()), Is.Empty);
        }

        // =================================================================================================
        // The sweep - which of TAS's leftovers are removed, and which are never touched
        // =================================================================================================

        private static ApertureBuildingElementUsage Usage(string guid, AperturePart aperturePart, int zoneSurfaceCount, string name = null)
        {
            return new ApertureBuildingElementUsage(guid, name ?? ("Windows: " + GlazingName + " " + guid + " -pane"), TasQuery.BEType(aperturePart), zoneSurfaceCount);
        }

        /// <summary>
        /// Only a surface-less, non-canonical APERTURE element is swept. A panel element, an aperture element
        /// still standing for a real window, and a canonical element the pass resolved onto are all left
        /// exactly where they are.
        /// </summary>
        [Test]
        public void Sweep_RemovesOnlySurfacelessNonCanonicalApertureElements()
        {
            ApertureBuildingElementUsage usage_Canonical = Usage("canonical-pane", AperturePart.Pane, Count_IdenticalWindows, "Windows: " + GlazingName + " -pane");
            ApertureBuildingElementUsage usage_Canonical_Empty = Usage("canonical-frame", AperturePart.Frame, 0, "Windows: " + GlazingName + " -frame");
            ApertureBuildingElementUsage usage_Orphan_1 = Usage("orphan-1", AperturePart.Pane, 0);
            ApertureBuildingElementUsage usage_Orphan_2 = Usage("orphan-2", AperturePart.Frame, 0);
            ApertureBuildingElementUsage usage_StillUsed = Usage("still-used", AperturePart.Pane, 1);
            ApertureBuildingElementUsage usage_Panel = new ApertureBuildingElementUsage("panel", "External Wall", TasQuery.BEType("External Wall"), 0);

            List<ApertureBuildingElementUsage> usages = [usage_Canonical, usage_Canonical_Empty, usage_Orphan_1, usage_StillUsed, usage_Panel, usage_Orphan_2];

            List<string> guids = TasQuery.UnusedApertureBuildingElementGuids(usages, new List<string> { "canonical-pane", "canonical-frame" });

            Assert.That(guids, Is.EqualTo(new List<string> { "orphan-1", "orphan-2" }), "The sweep removed the wrong set of elements.");
        }

        /// <summary>
        /// A canonical element left surface-less by a REFUSAL is never swept. A contested physical surface
        /// refuses rather than guessing, and deleting the definition that refusal was conservative about
        /// would turn a safe refusal into a loss.
        /// </summary>
        [Test]
        public void Sweep_ACanonicalElementLeftEmptyByARefusal_IsKept()
        {
            List<ApertureBuildingElementUsage> usages = [Usage("canonical", AperturePart.Pane, 0, "Windows: " + GlazingName + " -pane")];

            Assert.That(TasQuery.UnusedApertureBuildingElementGuids(usages, new List<string> { "canonical" }), Is.Empty);

            //Without the canonical guard the very same element WOULD be swept - which is the point.
            Assert.That(TasQuery.UnusedApertureBuildingElementGuids(usages, new List<string>()), Is.EqualTo(new List<string> { "canonical" }));
        }

        /// <summary>
        /// <b>The sweep is deliberately blind to a foreign element's provenance</b> - it does not distinguish
        /// "TAS created this for the current aperture GUID" from "something else authored this" purely by its
        /// three gates (aperture type, zero surfaces, not canonical). That is only safe because
        /// <c>T3DDocument.ExportNew</c> - measured, not assumed, see
        /// <c>APERTURE_DEFINITION_REUSE_GBXML.md</c>, "Licensed verification: ExportNew always replaces
        /// building elements" - never leaves ANY pre-existing building element in the file for this pass to
        /// find, under <c>RemoveExistingTBD</c> true OR its default false: a sentinel aperture element
        /// injected into an existing <c>.tbd</c> did not survive a second gbXML/T3D conversion under either
        /// setting. So a hypothetically "foreign" surface-less aperture element - same shape as
        /// <see cref="Sweep_RemovesOnlySurfacelessNonCanonicalApertureElements"/>'s orphans - is swept exactly
        /// like a TAS-created one, and this pins that as the deliberate, licensed-verified contract rather
        /// than an accidental one: no name or origin field distinguishes them, because in production nothing
        /// but a TAS-created orphan is ever there to distinguish.
        /// </summary>
        [Test]
        public void Sweep_HasNoNameOrProvenanceGuard_BecauseExportNewLeavesNothingForeignToProtect()
        {
            ApertureBuildingElementUsage usage_TasOrphan = Usage("tas-orphan", AperturePart.Pane, 0);
            ApertureBuildingElementUsage usage_HypotheticalForeign = new ApertureBuildingElementUsage("foreign-guid", "SOME_OTHER_TOOL_NAMED_THIS", TasQuery.BEType(AperturePart.Frame), 0);

            List<string> guids = TasQuery.UnusedApertureBuildingElementGuids([usage_TasOrphan, usage_HypotheticalForeign], new List<string>());

            Assert.That(guids, Is.EquivalentTo(new List<string> { "tas-orphan", "foreign-guid" }),
                "The sweep gained a name/provenance guard - if that is intentional, the licensed verification " +
                "in APERTURE_DEFINITION_REUSE_GBXML.md needs revisiting, since it justified the ABSENCE of one.");
        }

        /// <summary>
        /// A panel element is never swept, whatever its surface count - the sweep is about apertures only.
        /// </summary>
        [Test]
        public void Sweep_APanelElement_IsNeverAnApertureCandidate()
        {
            foreach (string panelType in new string[] { "External Wall", "Internal Wall", "Roof", "Ground Floor", "Shade" })
            {
                ApertureBuildingElementUsage usage = new ApertureBuildingElementUsage("panel-" + panelType, panelType, TasQuery.BEType(panelType), 0);

                Assert.That(usage.IsAperture, Is.False, panelType + " was treated as an aperture element.");
                Assert.That(TasQuery.UnusedApertureBuildingElementGuids([usage], new List<string>()), Is.Empty, panelType + " was swept.");
            }

            Assert.That(new ApertureBuildingElementUsage("g", "g", TasQuery.BEType(AperturePart.Pane), 0).IsAperture, Is.True);
            Assert.That(new ApertureBuildingElementUsage("f", "f", TasQuery.BEType(AperturePart.Frame), 0).IsAperture, Is.True);
        }

        // =================================================================================================
        // The construction sweep - only what names a physical aperture goes
        // =================================================================================================

        /// <summary>
        /// An unreferenced aperture construction is removed only when it NAMES a physical aperture. An
        /// unreferenced one that does not is a library definition with no window using it right now, and the
        /// export has always kept those - removing one would be a behaviour change nothing asks for.
        /// </summary>
        [Test]
        public void ConstructionSweep_RemovesTheInstanceNamedOnesAndKeepsTheLibraryOnes()
        {
            Guid apertureGuid = Guid.NewGuid();

            string name_Instance_Pane = GlazingName + " " + apertureGuid + " -pane";
            string name_Instance_Frame = GlazingName + " " + apertureGuid + " -frame";
            string name_Shared_Pane = GlazingName + " -pane";
            string name_Shared_Frame = GlazingName + " -frame";
            string name_Library = "SIM_INT_GLZ -pane";
            string name_Panel = "External Wall";

            List<string> names_Construction = [name_Instance_Pane, name_Instance_Frame, name_Shared_Pane, name_Shared_Frame, name_Library, name_Panel];

            //After the sweep the surviving elements carry only the shared pair.
            List<string> names_Referenced = [name_Shared_Pane, name_Shared_Frame];

            List<string> names_Orphan = TasQuery.OrphanApertureConstructionNames(names_Construction, names_Referenced, new List<Guid> { apertureGuid }, null, out List<string> names_Kept);

            Assert.That(names_Orphan, Is.EqualTo(new List<string> { name_Instance_Pane, name_Instance_Frame }), "The construction sweep removed the wrong set.");
            Assert.That(names_Kept, Is.EqualTo(new List<string> { name_Library }), "An unreferenced library aperture construction was not kept and reported.");

            //A panel construction is outside the aperture convention and so takes no part at all, referenced
            //or not.
            Assert.That(names_Orphan, Has.None.EqualTo(name_Panel));
            Assert.That(names_Kept, Has.None.EqualTo(name_Panel));
        }

        /// <summary>
        /// A referenced instance-named construction is never removed. The sweep runs after the element sweep
        /// precisely so that "referenced" means what it says; if a refusal left an element behind, the
        /// construction it carries survives with it.
        /// </summary>
        [Test]
        public void ConstructionSweep_AReferencedInstanceNamedConstruction_IsKept()
        {
            Guid apertureGuid = Guid.NewGuid();
            string name_Instance = GlazingName + " " + apertureGuid + " -pane";

            List<string> names_Orphan = TasQuery.OrphanApertureConstructionNames([name_Instance], [name_Instance], new List<Guid> { apertureGuid }, null, out List<string> names_Kept);

            Assert.That(names_Orphan, Is.Empty, "A construction a surviving element still carries was removed.");
            Assert.That(names_Kept, Is.Empty);
        }

        /// <summary>
        /// <b>A SUPERSEDED plain name is removed once nothing references it.</b> On the real gbXML route
        /// <c>Modify.UpdateConstructions</c> writes <c>SIM_EXT_GLZ -frame</c> before this pass runs, and its
        /// content differs from the Stage 2 definition in one field - <c>Modify.UpdateConstruction</c> sets
        /// <c>material.width</c> only for a TRANSPARENT material, so an opaque frame layer keeps the library
        /// default there while <c>construction.materialWidth</c> carries the real thickness, and a
        /// <c>ConstructionLayerDefinition</c> compares BOTH. The resolver therefore cannot adopt it and takes
        /// the signature-qualified name instead; without this gate the squatter would linger, referenced by
        /// nothing, and the gbXML route would report one more aperture construction than the direct route for
        /// the same model.
        /// </summary>
        [Test]
        public void ConstructionSweep_ASupersededPreferredName_IsRemovedOnceUnreferenced()
        {
            string name_Preferred = GlazingName + " -frame";
            string name_Qualified = GlazingName + "_CEAB27C2 -frame";
            string name_Library = "SIM_INT_GLZ -frame";

            List<string> names_Construction = [name_Preferred, name_Qualified, name_Library];

            //The pass bound every frame surface to the qualified construction, so only that one is referenced.
            List<string> names_Orphan = TasQuery.OrphanApertureConstructionNames(names_Construction, [name_Qualified], new List<Guid>(), [name_Preferred], out List<string> names_Kept);

            Assert.That(names_Orphan, Is.EqualTo(new List<string> { name_Preferred }), "The superseded preferred name was not removed.");
            Assert.That(names_Kept, Is.EqualTo(new List<string> { name_Library }), "An unrelated library construction was swept with it.");
        }

        /// <summary>
        /// A superseded name that something still REFERENCES is never removed - the sweep runs after the
        /// element sweep so that "referenced" means what it says. Two aperture constructions can sanitise to
        /// the same base, and if the plain name is genuinely in use it stays.
        /// </summary>
        [Test]
        public void ConstructionSweep_ASupersededNameStillReferenced_IsKept()
        {
            string name_Preferred = GlazingName + " -frame";
            string name_Qualified = GlazingName + "_CEAB27C2 -frame";

            List<string> names_Orphan = TasQuery.OrphanApertureConstructionNames(
                [name_Preferred, name_Qualified],
                [name_Preferred, name_Qualified],
                new List<Guid>(),
                [name_Preferred],
                out List<string> names_Kept);

            Assert.That(names_Orphan, Is.Empty, "A superseded name another definition still uses was removed.");
            Assert.That(names_Kept, Is.Empty);
        }

        // =================================================================================================
        // Reclaiming a freed plain name - what keeps the import pairing a window's two halves
        // =================================================================================================

        /// <summary>
        /// <b>A qualified construction reclaims the plain name once the sweep frees it.</b> This is not
        /// cosmetic: the aperture import pairs a window's two halves by the base name left after stripping
        /// <c>-pane</c>/<c>-frame</c>, so <c>SIM_EXT_GLZ -pane</c> beside <c>SIM_EXT_GLZ_CEAB27C2 -frame</c>
        /// stops being one two-sided window and comes back as one aperture per SURFACE. Licensed TAS showed
        /// exactly that - the import reported 28 apertures for 14 windows - which is what this reclaim fixes.
        /// </summary>
        [Test]
        public void Reclaim_AFreedPlainName_IsTakenByTheQualifiedConstruction()
        {
            string name_Preferred = GlazingName + " -frame";
            string name_Qualified = GlazingName + "_CEAB27C2 -frame";

            List<KeyValuePair<string, string>> renames = TasQuery.SupersededConstructionRenames(
                [new KeyValuePair<string, string>(name_Preferred, name_Qualified)],
                [name_Preferred]);

            Assert.That(renames, Has.Count.EqualTo(1));
            Assert.That(renames[0].Key, Is.EqualTo(name_Qualified), "The rename should be FROM the qualified name.");
            Assert.That(renames[0].Value, Is.EqualTo(name_Preferred), "The rename should be TO the plain name.");

            //And after it, both halves share a base again - which is what the import pairs on.
            Assert.That(TasQuery.TryDecomposeConstructionName(GlazingName + " -pane", out string base_Pane, out AperturePart part_Pane), Is.True);
            Assert.That(TasQuery.TryDecomposeConstructionName(renames[0].Value, out string base_Frame, out AperturePart part_Frame), Is.True);
            Assert.That(base_Frame, Is.EqualTo(base_Pane), "Pane and frame no longer share a base name, so the import would not pair them.");
            Assert.That(part_Pane, Is.EqualTo(AperturePart.Pane));
            Assert.That(part_Frame, Is.EqualTo(AperturePart.Frame));
        }

        /// <summary>
        /// A plain name the sweep did NOT remove is never renamed onto - that would put two constructions
        /// under one name, or steal a name something still uses.
        /// </summary>
        [Test]
        public void Reclaim_APlainNameStillInUse_IsNeverTaken()
        {
            string name_Preferred = GlazingName + " -frame";
            string name_Qualified = GlazingName + "_CEAB27C2 -frame";

            //Nothing removed.
            Assert.That(TasQuery.SupersededConstructionRenames([new KeyValuePair<string, string>(name_Preferred, name_Qualified)], new List<string>()), Is.Empty);

            //Something else removed.
            Assert.That(TasQuery.SupersededConstructionRenames([new KeyValuePair<string, string>(name_Preferred, name_Qualified)], ["SIM_INT_GLZ -frame"]), Is.Empty);
        }

        /// <summary>
        /// A freed name TWO definitions both wanted goes to neither. Handing it to whichever was enumerated
        /// first would be arbitrary, and would leave the other still qualified anyway.
        /// </summary>
        [Test]
        public void Reclaim_AContestedFreedName_IsGivenToNeither()
        {
            string name_Preferred = GlazingName + " -frame";

            List<KeyValuePair<string, string>> renames = TasQuery.SupersededConstructionRenames(
                [
                    new KeyValuePair<string, string>(name_Preferred, GlazingName + "_AAAAAAAA -frame"),
                    new KeyValuePair<string, string>(name_Preferred, GlazingName + "_BBBBBBBB -frame")
                ],
                [name_Preferred]);

            Assert.That(renames, Is.Empty, "A contested freed name was handed to one of the claimants.");
        }

        // =================================================================================================
        // Part selection - the two routes must agree on which parts exist
        // =================================================================================================

        /// <summary>
        /// A part exists when it has thickness, which is the direct export's own test. A window with no frame
        /// layers asks for a pane definition and nothing else - so the two routes cannot disagree about how
        /// many definitions one model needs.
        /// </summary>
        [Test]
        public void PartSelection_AWindowWithNoFrame_AsksForAPaneDefinitionOnly()
        {
            ApertureConstruction apertureConstruction = Glazing(frameConstructionLayers: new List<ConstructionLayer>());

            List<Aperture> apertures = IdenticalWindows(3, apertureConstruction);

            Assert.That(apertures[0].ApertureHasPart(AperturePart.Pane), Is.True);
            Assert.That(apertures[0].ApertureHasPart(AperturePart.Frame), Is.False);
            Assert.That(apertures[0].ApertureHasPart(AperturePart.Undefined), Is.False);

            List<ApertureDefinitionBinding> bindings = Bindings(apertures);

            Assert.That(bindings, Has.Count.EqualTo(3), "A frameless window produced a frame binding.");
            Assert.That(Frames(bindings), Is.Empty);
            Assert.That(bindings.DistinctBuildingElementDefinitions(), Has.Count.EqualTo(1));
            Assert.That(bindings.DistinctConstructionDefinitions(), Has.Count.EqualTo(1));
        }

        /// <summary>
        /// <b>Content decides reuse, never the <c>ApertureConstruction</c> the window came from.</b> Two
        /// differently named constructions whose PANES hold identical layers share one pane definition
        /// between all twenty windows, while their frames - which differ by layer thickness - stay apart.
        /// Three definitions, not four: reuse that keyed off the construction's name instead of its content
        /// would give four, and reuse that ignored the layers would give two.
        /// </summary>
        [Test]
        public void GbXMLRoute_TwoApertureConstructions_ShareByContentAndNotByName()
        {
            MaterialLibrary materialLibrary = Library();

            ApertureConstruction apertureConstruction_1 = Glazing("SIM_EXT_GLZ");
            ApertureConstruction apertureConstruction_2 = Glazing("SIM_EXT_GLZ_2", new List<ConstructionLayer> { new ConstructionLayer("Timber", 0.10) });

            List<Aperture> apertures = IdenticalWindows(10, apertureConstruction_1);
            apertures.AddRange(IdenticalWindows(10, apertureConstruction_2));

            List<ApertureDefinitionBinding> bindings = Bindings(apertures, materialLibrary);

            Assert.That(bindings, Has.Count.EqualTo(40));
            Assert.That(bindings.DistinctBuildingElementDefinitions(), Has.Count.EqualTo(3), "Two constructions with identical panes and differing frames did not resolve to one shared pane and two frames.");

            //The frames differ by layer thickness; the panes are identical content, so they SHARE - reuse is
            //by content, never by the ApertureConstruction's name.
            Assert.That(Frames(bindings).DistinctConstructionDefinitions(), Has.Count.EqualTo(2));
            Assert.That(Panes(bindings).DistinctConstructionDefinitions(), Has.Count.EqualTo(1), "Two identical panes were split by their ApertureConstruction name rather than shared by content.");

            //All twenty panes, from BOTH constructions, on the one shared pane element.
            Assert.That(Panes(bindings).DistinctBuildingElementDefinitions(), Has.Count.EqualTo(1));
            Assert.That(Frames(bindings).DistinctBuildingElementDefinitions(), Has.Count.EqualTo(2));
        }
    }
}
