// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Iteration 0 step 7, export side: the <c>OverheatingScenario</c> is authoritative over the ventilation
    /// system type written into the TM59 XML.</b>
    /// <para>
    /// The two derivations this replaces both live here. <c>Space.ToTM59</c> read the space's
    /// <c>InternalCondition.VentilationSystemTypeName</c> and defaulted to natural ventilation (#1);
    /// <c>AnalyticalModel.ToTM59</c> then let any related <c>VentilationSystem</c> that
    /// <c>IsMechanicalVentilation()</c> override it (#2). They disagreed with each other and with the criterion
    /// the assessment applied, which is how one real run exported "Nat Vent" and "Mech Vent" mixed across three
    /// identical flats.
    /// </para>
    /// <para>
    /// No TAS COM: <c>Building</c> and <c>Zone</c> are XML writers over analytical objects, and nothing here
    /// converts a TBD.
    /// </para>
    /// </summary>
    [TestFixture]
    public class VentilationStrategyExportTests
    {
        private static TM59Manager tM59Manager;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "SAM_InternalConditionTextMap_TM59.JSON");

            tM59Manager = new TM59Manager(Core.Create.IJSAMObject<TextMap>(File.ReadAllText(path)));
        }

        /// <summary>
        /// <b>An MVRE scenario beats an internal condition saying NV</b> - derivation #1. The control shows the
        /// same model exporting "Nat Vent" through the model-derived overload, so this is a change of answer
        /// and not an agreement.
        /// </summary>
        [Test]
        public void AnMVREScenario_OverridesAnInternalConditionSayingNV()
        {
            AnalyticalModel analyticalModel = Model("NV");

            //Control: the internal condition decides it today.
            Assert.That(SystemTypes(analyticalModel.ToTM59(tM59Manager)), Is.EqualTo(new[] { SystemType.NaturalVentilation }));

            Building building = analyticalModel.ToTM59(tM59Manager, Map(analyticalModel, "MVRE"), out List<string> refusals);

            Assert.That(refusals, Is.Empty);
            Assert.That(SystemTypes(building), Is.EqualTo(new[] { SystemType.MechanicalVentilation }));
        }

        /// <summary>
        /// <b>An NV scenario beats a mechanical <c>VentilationSystem</c> on the model</b> - derivation #2, the
        /// one that used to override everything else.
        /// </summary>
        [Test]
        public void AnNVScenario_OverridesAMechanicalVentilationSystemOnTheModel()
        {
            AnalyticalModel analyticalModel = Model("NV", ventilationSystem: true);

            //Control: the mechanical system overrides the internal condition's "NV" today.
            Assert.That(SystemTypes(analyticalModel.ToTM59(tM59Manager)), Is.EqualTo(new[] { SystemType.MechanicalVentilation }));

            Building building = analyticalModel.ToTM59(tM59Manager, Map(analyticalModel, "NV"), out List<string> refusals);

            Assert.That(refusals, Is.Empty);
            Assert.That(SystemTypes(building), Is.EqualTo(new[] { SystemType.NaturalVentilation }));
        }

        /// <summary>
        /// <b>A refusal loses the whole document, not one room.</b> This XML is configuration for the external
        /// TAS TM59 tool, which cannot be told that a room is missing - it would assess what it was given and
        /// produce a complete-looking answer for an incomplete building. Every space is still visited, so one
        /// unstated dwelling does not hide the others' reasons behind it.
        /// </summary>
        [Test]
        public void AnUnsettledStrategy_RefusesTheWholeExport()
        {
            AnalyticalModel analyticalModel = Model_ThreeFlats();
            List<Space> spaces = analyticalModel.GetSpaces();

            VentilationStrategyMap ventilationStrategyMap = new VentilationStrategyMap();

            //Flat 1 states MVRE, Flat 2's scenario states nothing, Flat 3 is not covered at all.
            ventilationStrategyMap.Add(Scenario("MVRE"), new List<Space> { Find(spaces, "Flat 1 Bedroom 2") });
            ventilationStrategyMap.Add(Scenario(null), new List<Space> { Find(spaces, "Flat 2 Bedroom 2") });

            Building building = analyticalModel.ToTM59(tM59Manager, ventilationStrategyMap, out List<string> refusals);

            Assert.That(building, Is.Null);
            Assert.That(refusals.Count, Is.EqualTo(2));

            //Both are named, and each says the right thing about itself.
            Assert.That(refusals.Exists(x => x.Contains("Flat 2 Bedroom 2") && x.Contains("states no ventilation strategy")), Is.True);
            Assert.That(refusals.Exists(x => x.Contains("Flat 3 Bedroom 2") && x.Contains("No overheating scenario covers")), Is.True);
        }

        /// <summary>
        /// <b>A space that cannot be exported is a refusal, not a silently missing room.</b>
        /// <para>
        /// <c>Space.ToTM59</c> returns null for a space with no <c>InternalCondition</c>. Dropping it would pass
        /// the completeness gate and ship a two-zone document for a three-space building as a success - the
        /// exact outcome refusing the whole document exists to prevent, and the one thing the external TAS TM59
        /// tool could never notice.
        /// </para>
        /// </summary>
        [Test]
        public void ASpaceThatCannotBeExported_RefusesRatherThanVanishing()
        {
            AnalyticalModel analyticalModel = Model_ThreeFlats(name_WithoutInternalCondition: "Flat 2 Bedroom 2");

            Assert.That(analyticalModel.GetSpaces().Count, Is.EqualTo(3));

            //Every space has a stated strategy, so nothing is refused for being unsettled.
            Building building = analyticalModel.ToTM59(tM59Manager, Map(analyticalModel, "MVRE"), out List<string> refusals);

            Assert.That(building, Is.Null);
            Assert.That(refusals.Count, Is.EqualTo(1));
            Assert.That(refusals[0], Does.Contain("Flat 2 Bedroom 2"));
            Assert.That(refusals[0], Does.Contain("cannot be exported as a TM59 zone"));
        }

        /// <summary>
        /// The degenerate form of the same hole: a null <c>TM59Manager</c> makes every zone unexportable, which
        /// used to return a zero-zone <c>Building</c> and report success.
        /// </summary>
        [Test]
        public void ANullManager_RefusesRatherThanExportingNothing()
        {
            AnalyticalModel analyticalModel = Model("NV");

            Building building = analyticalModel.ToTM59(null, Map(analyticalModel, "MVRE"), out List<string> refusals);

            Assert.That(building, Is.Null);
            Assert.That(refusals, Is.Not.Empty);
        }

        /// <summary>
        /// <b>A null return always carries a reason.</b> Where there is nothing to export at all, a caller
        /// reading the out parameter to find out why must get an answer rather than an empty list.
        /// </summary>
        [Test]
        public void ANullReturn_AlwaysCarriesAReason()
        {
            Assert.That(((AnalyticalModel)null).ToTM59(tM59Manager, new VentilationStrategyMap(), out List<string> refusals), Is.Null);
            Assert.That(refusals, Is.Not.Empty);
            Assert.That(refusals[0], Does.Contain("nothing to export"));
        }

        /// <summary>
        /// <b>A model whose space list is EMPTY refuses too, not just one whose list is null.</b> The condition
        /// said "there is no model, or it holds no spaces" and only tested the first half.
        /// <para>
        /// <c>GetObjects&lt;T&gt;</c> returns an empty list for a cluster that holds objects but none of that
        /// type - a model with plant and no spaces yet - so this is the ordinary shape of "no spaces". Falling
        /// through gave the loop nothing to visit, so no refusal was recorded, the completeness gate saw a clean
        /// run, and a zero-zone document was written and reported as a success. That is the most complete form
        /// of the very thing refusing the document exists to prevent, and the external TAS TM59 tool would
        /// assess it without a word.
        /// </para>
        /// </summary>
        [Test]
        public void AModelWithNoSpaces_RefusesRatherThanExportingAnEmptyDocument()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            //**A space is added and then REMOVED, and that is the only way to reach the empty case.** An
            //untouched cluster has no Space type registered at all, so GetSpaces() returns null - the half that
            //already refused. RemoveObject deletes the guid but leaves the type's bucket behind, so the lookup
            //now finds the type and returns an EMPTY list. A model whose spaces were all deleted is exactly
            //the shape a user can produce, and it is the one that used to export a zero-zone document.
            Space space = Space("Flat 1 Bedroom 2", "NV");

            adjacencyCluster.AddObject(space);

            Assert.That(adjacencyCluster.RemoveObject(space), Is.True);

            AnalyticalModel analyticalModel = new AnalyticalModel("No Spaces", null, null, null, adjacencyCluster);

            //The premise, pinned: empty and NOT null, or this test proves nothing about the new half.
            Assert.That(analyticalModel.AdjacencyCluster.GetSpaces(), Is.Not.Null);
            Assert.That(analyticalModel.AdjacencyCluster.GetSpaces(), Is.Empty);

            Building building = analyticalModel.ToTM59(tM59Manager, new VentilationStrategyMap(), out List<string> refusals);

            Assert.That(building, Is.Null);
            Assert.That(refusals, Is.Not.Empty);
            Assert.That(refusals[0], Does.Contain("nothing to export"));

            //And the end the finding was actually about: no file is written, and success is not reported.
            string path = Path.Combine(Path.GetTempPath(), "SAM_TM59_NoSpaces_" + Guid.NewGuid().ToString("N") + ".xml");

            try
            {
                Assert.That(analyticalModel.ToXml(path, tM59Manager, new VentilationStrategyMap(), out List<string> refusals_Xml), Is.False);
                Assert.That(refusals_Xml, Is.Not.Empty);
                Assert.That(File.Exists(path), Is.False);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        /// <summary>
        /// <b>An unrecognised strategy refuses the export instead of being written as "Mech Vent".</b> The
        /// mapping treats anything that is not <c>NV</c> or <c>UV</c> as mechanical, so without the map's closed
        /// vocabulary a scenario stating "Natural" would have been exported as mechanically ventilated.
        /// </summary>
        [Test]
        public void AnUnrecognisedStrategy_RefusesTheExport()
        {
            AnalyticalModel analyticalModel = Model("NV");

            Building building = analyticalModel.ToTM59(tM59Manager, Map(analyticalModel, "Natural"), out List<string> refusals);

            Assert.That(building, Is.Null);
            Assert.That(refusals.Count, Is.EqualTo(1));
            Assert.That(refusals[0], Does.Contain("not a ventilation identity"));
        }

        /// <summary>
        /// Two scenarios disagreeing over one space refuse the export as well, so ambiguity does not reach the
        /// XML through this path either.
        /// </summary>
        [Test]
        public void ConflictingScenarios_RefuseTheExport()
        {
            AnalyticalModel analyticalModel = Model("NV");
            List<Space> spaces = analyticalModel.GetSpaces();

            VentilationStrategyMap ventilationStrategyMap = new VentilationStrategyMap();
            ventilationStrategyMap.Add(Scenario("MVRE"), spaces);
            ventilationStrategyMap.Add(Scenario("NV"), spaces);

            Building building = analyticalModel.ToTM59(tM59Manager, ventilationStrategyMap, out List<string> refusals);

            Assert.That(building, Is.Null);
            Assert.That(refusals.Count, Is.EqualTo(1));
            Assert.That(refusals[0], Does.Contain("different ventilation strategies"));
        }

        /// <summary>
        /// <b>No map means no change.</b> A caller with no scenario gets the old model-derived export, not an
        /// empty one - and no refusals, because nothing was asked of a map that was not supplied.
        /// </summary>
        [Test]
        public void WithoutAMap_TheModelDerivedExportIsUnchanged()
        {
            AnalyticalModel analyticalModel = Model("MVRE");

            Building building = analyticalModel.ToTM59(tM59Manager, null, out List<string> refusals);

            Assert.That(refusals, Is.Empty);
            Assert.That(SystemTypes(building), Is.EqualTo(SystemTypes(analyticalModel.ToTM59(tM59Manager))));
            Assert.That(SystemTypes(building), Is.EqualTo(new[] { SystemType.MechanicalVentilation }));

            //And a null model still refuses rather than throwing, either way in.
            Assert.That(((AnalyticalModel)null).ToTM59(tM59Manager, new VentilationStrategyMap(), out _), Is.Null);
            Assert.That(((AnalyticalModel)null).ToTM59(tM59Manager), Is.Null);
        }

        /// <summary>
        /// <b>What a strategy means is unchanged.</b> The XML vocabulary has only two values, so the corridor
        /// criterion the assessment distinguishes has no representation here and <c>UV</c> exports as naturally
        /// ventilated - the same <c>Query.IsMechanicalVentilation</c> mapping the old derivations used. Step 7
        /// changed which strategy applies, not what one means.
        /// </summary>
        [TestCase("NV", SystemType.NaturalVentilation)]
        [TestCase("UV", SystemType.NaturalVentilation)]
        [TestCase("MV", SystemType.MechanicalVentilation)]
        [TestCase("MVRE", SystemType.MechanicalVentilation)]
        public void TheStrategyToSystemTypeMapping_IsTheExistingOne(string ventilationStrategy, SystemType systemType_Expected)
        {
            AnalyticalModel analyticalModel = Model("NV");

            Building building = analyticalModel.ToTM59(tM59Manager, Map(analyticalModel, ventilationStrategy), out List<string> refusals);

            Assert.That(refusals, Is.Empty);
            Assert.That(SystemTypes(building), Is.EqualTo(new[] { systemType_Expected }));
        }

        /// <summary>
        /// The exported zone still carries everything it did - the room use, the stamped zone guid, the export
        /// flag - so making the strategy authoritative changed one field of the export and nothing else.
        /// </summary>
        [Test]
        public void OnlyTheSystemType_Changes()
        {
            AnalyticalModel analyticalModel = Model("NV");

            Zone zone_Derived = analyticalModel.ToTM59(tM59Manager).Zones[0];
            Zone zone_Scenario = analyticalModel.ToTM59(tM59Manager, Map(analyticalModel, "MVRE"), out _).Zones[0];

            Assert.That(zone_Scenario.Name, Is.EqualTo(zone_Derived.Name));
            Assert.That(zone_Scenario.Guid, Is.EqualTo(zone_Derived.Guid));
            Assert.That(zone_Scenario.RoomUse, Is.EqualTo(zone_Derived.RoomUse));
            Assert.That(zone_Scenario.Factor, Is.EqualTo(zone_Derived.Factor));
            Assert.That(zone_Scenario.Export, Is.EqualTo(zone_Derived.Export));
            Assert.That(zone_Scenario.WindSpeed, Is.EqualTo(zone_Derived.WindSpeed));

            //Not vacuous - the room really did resolve to a bedroom rather than Undefined.
            Assert.That(zone_Derived.RoomUse, Is.EqualTo(RoomUse.Bedroom));
            Assert.That(zone_Scenario.SystemType, Is.Not.EqualTo(zone_Derived.SystemType));
        }

        // ---------------------------------------------------------------------------------------------
        // Fixture
        // ---------------------------------------------------------------------------------------------

        private static SystemType[] SystemTypes(Building building)
        {
            return building?.Zones?.ConvertAll(x => x.SystemType)?.ToArray();
        }

        private static OverheatingScenario Scenario(string ventilationStrategy)
        {
            SystemTemplate systemTemplate = ventilationStrategy == null ? null : new SystemTemplate(ventilationStrategy, null, null, null, null, null);

            return new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.Undefined, systemTemplate);
        }

        private static VentilationStrategyMap Map(AnalyticalModel analyticalModel, string ventilationStrategy)
        {
            VentilationStrategyMap result = new VentilationStrategyMap();

            result.Add(Scenario(ventilationStrategy), analyticalModel.GetSpaces());

            return result;
        }

        private static Space Find(List<Space> spaces, string name)
        {
            return spaces.Find(x => x.Name == name);
        }

        private static AnalyticalModel Model(string ventilationSystemTypeName, bool ventilationSystem = false)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            Space space = Space("Flat 1 Bedroom 2", ventilationSystemTypeName);

            adjacencyCluster.AddObject(space);

            if (ventilationSystem)
            {
                VentilationSystem ventilationSystem_Temp = new VentilationSystem("1", new VentilationSystemType("MVRE", "Mechanical Ventilation with Recirculation"));

                adjacencyCluster.AddObject(ventilationSystem_Temp);
                adjacencyCluster.AddRelation(ventilationSystem_Temp, space);
            }

            return new AnalyticalModel("Three Flats", null, null, null, adjacencyCluster);
        }

        /// <summary>
        /// Three flats each with a "Bedroom 2", all stating NV in their design data.
        /// </summary>
        /// <param name="name_WithoutInternalCondition">
        /// A space to leave with no <c>InternalCondition</c>, which is what makes <c>Space.ToTM59</c> return null
        /// for it. Null leaves all three complete.
        /// </param>
        private static AnalyticalModel Model_ThreeFlats(string name_WithoutInternalCondition = null)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            foreach (string name in new[] { "Flat 1", "Flat 2", "Flat 3" })
            {
                string name_Space = name + " Bedroom 2";

                adjacencyCluster.AddObject(Space(name_Space, "NV", name_Space != name_WithoutInternalCondition));
            }

            return new AnalyticalModel("Three Flats", null, null, null, adjacencyCluster);
        }

        private static Space Space(string name, string ventilationSystemTypeName, bool internalCondition = true)
        {
            Space result = new Space(name);

            if (!internalCondition)
            {
                return result;
            }

            InternalCondition internalCondition_Temp = new InternalCondition(name);

            if (!string.IsNullOrEmpty(ventilationSystemTypeName))
            {
                internalCondition_Temp.SetValue(InternalConditionParameter.VentilationSystemTypeName, ventilationSystemTypeName);
            }

            result.InternalCondition = internalCondition_Temp;

            return result;
        }
    }
}
