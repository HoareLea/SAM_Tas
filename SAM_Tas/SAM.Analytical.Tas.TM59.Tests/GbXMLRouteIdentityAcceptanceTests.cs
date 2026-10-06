// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Acceptance regression for the normal Grasshopper results workflow</b>, which is:
    /// <code>
    /// To gbXML -> SAMAnalytical.WorkflowgbXML (Simulation = true) -> TAS writes the TSD
    ///          -> Tas.TSDQueryTM59Results -> TM59 results inspected in Grasshopper
    /// </code>
    /// <para>
    /// <b>What it proves: duplicate room names cannot cross flats on that route.</b> The fixture is the real
    /// validation shape - three flats whose room is called <i>exactly</i> <c>"Bedroom 2"</c>, plus a communal
    /// corridor - and it exercises the sequence <c>Tas.TSDQueryTM59Results</c> actually runs, through
    /// <c>Create.TM59AssessmentCalculator</c>, so the TAS-side default identity is under test rather than a
    /// hand-built one.
    /// </para>
    /// <para>
    /// <b>The identity is the zone guid TAS preserves</b>, stamped as <c>SpaceParameter.ZoneGuid</c> - written onto
    /// the design spaces by the workflow and written again by <c>Convert.ToSAM</c> onto the fresh spaces it builds
    /// from a TSD. The fixture stamps it exactly as those two do; nothing here converts a real TSD, so no TAS COM
    /// is needed.
    /// </para>
    /// <para>
    /// <b>Why it is worth having as well as the SAM-side tests.</b> Those prove the mechanism. This proves the
    /// wiring: that this route's default key is the right one, and that a caller who supplies nothing still gets
    /// identity rather than name matching.
    /// </para>
    /// </summary>
    [TestFixture]
    public class GbXMLRouteIdentityAcceptanceTests
    {
        //Types are qualified throughout: inside this namespace SAM.Analytical.Tas.TM59 has its own Zone and
        //Query, and SAM.Analytical.Tas its own SpaceParameter and AnalyticalModelParameter, each shadowing the
        //SAM.Analytical one. The same nested-namespace trap that broke seven call sites during the Part F work.
        private const string key_Tas_OccupantSensibleGain = "Occupant Sensible Gain";

        private static readonly string[] flats = ["Flat 1", "Flat 2", "Flat 3"];

        /// <summary>
        /// The route's own default identity resolves all three same-named rooms, each to its own flat - and the
        /// internal condition that drives each assessment came from the right flat.
        /// </summary>
        [Test]
        public void TheGbXMLRoute_TiesEachBedroom2ToItsOwnFlat()
        {
            AnalyticalModel analyticalModel_Design = Model_Design();
            AnalyticalModel analyticalModel_TSD = Model_TSD();

            //Exactly what the component does: no map supplied, so the factory builds the TAS one.
            Analytical.TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel_TSD.TM59AssessmentCalculator(analyticalModel_Design);

            Assert.That(tM59AssessmentCalculator.RestoreDesignInternalConditions(), Is.True);
            Assert.That(tM59AssessmentCalculator.AssociationRefusals, Is.Empty);

            //Three rooms, one name, three different internal conditions - each from its own flat.
            foreach (string flat in flats)
            {
                Space space = Simulated(tM59AssessmentCalculator, analyticalModel_TSD, flat);

                Assert.That(space.Name, Is.EqualTo("Bedroom 2"));
                Assert.That(space.InternalCondition.Name, Is.EqualTo(flat + " IC"), "A flat's room was given another flat's design intent.");
            }

            //And the map really is complete in both directions on this route.
            Assert.That(tM59AssessmentCalculator.SimulationSpaceMap.IsComplete, Is.True);
        }

        /// <summary>
        /// <b>The characterisation that matters: without the stable identity, the route REFUSES rather than
        /// crossing flats.</b>
        /// <para>
        /// The stamp is dropped from the whole simulated model, which is the shape of an older model or a
        /// conversion that did not write it. Three rooms then share one name and nothing can tell them apart - so
        /// all three are refused, and not one of them is given a neighbour's internal condition. Before step 8
        /// this same input silently gave every "Bedroom 2" the FIRST flat's design intent.
        /// </para>
        /// </summary>
        [Test]
        public void WithoutTheStableIdentity_TheRouteRefusesRatherThanCrossingFlats()
        {
            AnalyticalModel analyticalModel_Design = Model_Design();
            AnalyticalModel analyticalModel_TSD = Model_TSD(stamp: false);

            Analytical.TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel_TSD.TM59AssessmentCalculator(analyticalModel_Design);

            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            //The corridor's name is unique, so it still resolves. The three "Bedroom 2" rooms do not.
            Assert.That(tM59AssessmentCalculator.AssociationRefusals.Count, Is.EqualTo(3));

            List<Space> spaces = tM59AssessmentCalculator.AnalyticalModel.GetSpaces();

            //Asserted over ALL THREE at once, because without the stamp the test itself cannot say which room is
            //Flat 2's - which is exactly the position the production code is in, and exactly why it must refuse.
            List<Space> spaces_Bedroom = spaces.FindAll(x => x.Name == "Bedroom 2");

            Assert.That(spaces_Bedroom.Count, Is.EqualTo(3));

            foreach (Space space in spaces_Bedroom)
            {
                Assert.That(space.InternalCondition, Is.Null, "A room with no resolvable identity was given design intent anyway.");
            }

            //And the production component's whole-model call cannot put the refused bedrooms back into the
            //assessment after RestoreDesignInternalConditions left them alone.
            List<Space> spaces_Assessment = tM59AssessmentCalculator.Spaces(null, null);
            Assert.That(spaces_Assessment.Count, Is.EqualTo(1));
            Assert.That(spaces_Assessment[0].Name, Is.EqualTo("Corridor"));
            Assert.That(tM59AssessmentCalculator.AssociationRefusals.Count, Is.EqualTo(3));

            //And the uniquely named corridor is unaffected by its neighbours' ambiguity.
            Assert.That(spaces.Find(x => x.Name == "Corridor").InternalCondition, Is.Not.Null);
        }

        /// <summary>
        /// End to end on this route: with scenarios stated, each flat is assessed against <i>its own</i>
        /// scenario's criterion and its result associates back to <i>its own</i> scenario - and the corridor to
        /// neither of the flats.
        /// </summary>
        [Test]
        public void TheGbXMLRoute_AssociatesEachResultToItsOwnScenario()
        {
            AnalyticalModel analyticalModel_Design = Model_Design();
            AnalyticalModel analyticalModel_TSD = Model_TSD();

            SimulationSpaceMap simulationSpaceMap = analyticalModel_Design.SimulationSpaceMap(analyticalModel_TSD);

            //Flat 1 MVRE, Flat 2 NV, Flat 3 MVRE, corridor UV - the design data says the opposite in each case.
            List<OverheatingScenario> overheatingScenarios = Scenarios(analyticalModel_Design);

            OverheatingScenarioMap overheatingScenarioMap = new OverheatingScenarioMap(overheatingScenarios, analyticalModel_Design, simulationSpaceMap);

            Assert.That(overheatingScenarioMap.IsComplete, Is.True);
            Assert.That(overheatingScenarioMap.Refusals, Is.Empty);

            Analytical.TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel_TSD.TM59AssessmentCalculator(analyticalModel_Design, simulationSpaceMap);
            tM59AssessmentCalculator.VentilationStrategyMap = overheatingScenarioMap.VentilationStrategyMap;

            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            TM59AssessmentResult tM59AssessmentResult = tM59AssessmentCalculator.Calculate(tM59AssessmentCalculator.Spaces(null, null));

            Assert.That(tM59AssessmentResult.VentilationStrategyRefusals, Is.Empty);

            Dictionary<OverheatingScenario, List<TMResult>> dictionary = overheatingScenarioMap.Associate(tM59AssessmentResult, out List<TMResult> tMResults_Unassociated);

            Assert.That(tMResults_Unassociated, Is.Empty);
            Assert.That(dictionary.Count, Is.EqualTo(4));

            //Each flat's single result is its own, and the criterion is the scenario's.
            foreach (OverheatingScenario overheatingScenario in overheatingScenarios)
            {
                Assert.That(dictionary[overheatingScenario].Count, Is.EqualTo(1));
            }

            Assert.That(tM59AssessmentResult.MechanicalVentilationResults.Count, Is.EqualTo(2), "Flat 1 and Flat 3 state MVRE.");
            Assert.That(tM59AssessmentResult.NaturalVentilationResults.Count, Is.EqualTo(1), "Flat 2 states NV.");
            Assert.That(tM59AssessmentResult.CorridorResults.Count, Is.EqualTo(1), "The corridor states UV.");
        }

        /// <summary>
        /// <b>Acceptance regression for the official TAS print workflow</b>:
        /// <code>
        /// SAMAnalytical.CreateTBDByTM59 -> TM59 XML/TBD/TSD -> TAS TM59 tabs -> print
        /// </code>
        /// The XML is prepared from the design model, so its SimulationSpaceMap is identity. All three flats may
        /// call their room "Bedroom 2" without one flat's scenario deciding another flat's Nat/Mech Vent value.
        /// </summary>
        [Test]
        public void TheOfficialTasPrintRoute_WritesEachBedroom2WithItsOwnScenario()
        {
            AnalyticalModel analyticalModel_Design = Model_Design();
            List<OverheatingScenario> overheatingScenarios = Scenarios(analyticalModel_Design);

            SimulationSpaceMap simulationSpaceMap = SimulationSpaceMap.Identity(analyticalModel_Design.GetSpaces());
            OverheatingScenarioMap overheatingScenarioMap = new(overheatingScenarios, analyticalModel_Design, simulationSpaceMap);

            Assert.That(overheatingScenarioMap.IsComplete, Is.True);

            string path = Path.Combine(Path.GetTempPath(), "SAM-PartO-Step8-" + Guid.NewGuid().ToString("N") + ".xml");

            try
            {
                bool converted = Analytical.Tas.TM59.Convert.ToXml(
                    analyticalModel_Design,
                    path,
                    new TM59Manager(),
                    overheatingScenarioMap.VentilationStrategyMap,
                    out List<string> refusals);

                Assert.That(converted, Is.True);
                Assert.That(refusals, Is.Empty);

                XDocument document = XDocument.Load(path);
                List<XElement> zoneElements = [.. document.Descendants("DomOverheatZoneItem")];

                Assert.That(zoneElements.Count, Is.EqualTo(4));

                foreach (OverheatingScenario overheatingScenario in overheatingScenarios)
                {
                    Analytical.Zone zone_Design = analyticalModel_Design.GetZones().Find(x => x.Guid == overheatingScenario.ZoneGuid);
                    Space space_Design = analyticalModel_Design.AdjacencyCluster.GetRelatedObjects<Space>(zone_Design)[0];
                    XElement zoneElement = zoneElements.Find(x => Guid.Parse(x.Element("GUID").Value.Trim('{', '}')) == space_Design.Guid);

                    Assert.That(zoneElement, Is.Not.Null);

                    string expected = overheatingScenario.VentilationStrategy == "MVRE" ? "Mech Vent" : "Nat Vent";
                    Assert.That(zoneElement.Element("SystemType").Value, Is.EqualTo(expected), zone_Design.Name + " received another zone's scenario.");
                }
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        // ---------------------------------------------------------------------------------------------
        // Fixture
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// The simulated space for a flat, read back off the calculator's rebuilt model.
        /// <para>
        /// <b>Found by the stable key, not the name</b> - the test could not do otherwise, because all three
        /// rooms are called "Bedroom 2". Which is the same reason the production code could not.
        /// </para>
        /// </summary>
        private static Space Simulated(Analytical.TM59AssessmentCalculator tM59AssessmentCalculator, AnalyticalModel analyticalModel_TSD, string name)
        {
            Guid guid = analyticalModel_TSD.GetSpaces().Find(x => Tas.Query.SimulationSpaceKey(x) == StableKey(name)).Guid;

            return tM59AssessmentCalculator.AnalyticalModel.GetSpaces().Find(x => x.Guid == guid);
        }

        private static string StableKey(string name)
        {
            int index = System.Array.IndexOf(flats, name);

            return index == -1 ? (name == "Corridor" ? "tas-zone-corridor" : null) : "tas-zone-" + index;
        }

        private static List<OverheatingScenario> Scenarios(AnalyticalModel analyticalModel_Design)
        {
            List<OverheatingScenario> result = [];

            foreach (KeyValuePair<string, string> keyValuePair in new Dictionary<string, string> { { "Flat 1", "MVRE" }, { "Flat 2", "NV" }, { "Flat 3", "MVRE" }, { "Corridor", "UV" } })
            {
                Analytical.Zone zone = analyticalModel_Design.GetZones().Find(x => x.Name == keyValuePair.Key);

                result.Add(new OverheatingScenario(
                    keyValuePair.Key == "Corridor" ? PartOAssessmentScope.CommonSpace : PartOAssessmentScope.Dwelling,
                    zone.Guid,
                    PartOIteration.Undefined,
                    new SystemTemplate(keyValuePair.Value, null, null, null, null, null)));
            }

            return result;
        }

        /// <summary>
        /// The design model: three flats each with a room called exactly "Bedroom 2", plus a corridor. Each room
        /// carries the zone guid the workflow stamps, and an internal condition named after its FLAT so a
        /// cross-wiring is visible.
        /// </summary>
        private static AnalyticalModel Model_Design()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            foreach (string name in new[] { "Flat 1", "Flat 2", "Flat 3", "Corridor" })
            {
                Space space = new Space(name == "Corridor" ? "Corridor" : "Bedroom 2");

                InternalCondition internalCondition = new InternalCondition(name + " IC");

                //Deliberately contradicting the scenarios, so the criterion cannot be tracking this.
                internalCondition.SetValue(InternalConditionParameter.VentilationSystemTypeName, name == "Flat 2" ? "MVRE" : "NV");

                space.InternalCondition = internalCondition;
                space.SetValue(SpaceParameter.ZoneGuid, StableKey(name));

                Analytical.Zone zone = new Analytical.Zone(name);

                adjacencyCluster.AddObject(space);
                adjacencyCluster.AddObject(zone);
                adjacencyCluster.AddRelation(zone, space);
            }

            return new AnalyticalModel("Three Flats", null, null, null, adjacencyCluster);
        }

        /// <summary>
        /// The model as <c>Convert.ToSAM</c> rebuilds it from a TSD: fresh spaces with fresh guids, hourly series
        /// under the TAS spellings, no internal conditions, and the zone guid stamped as the converter stamps it.
        /// </summary>
        private static AnalyticalModel Model_TSD(bool stamp = true)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            foreach (string name in new[] { "Flat 1", "Flat 2", "Flat 3", "Corridor" })
            {
                Space space = new Space(name == "Corridor" ? "Corridor" : "Bedroom 2");

                if (stamp)
                {
                    space.SetValue(SpaceParameter.ZoneGuid, StableKey(name));
                }

                ParameterSet parameterSet = new ParameterSet("SAM.Analytical.Tas.dll");

                parameterSet.Add(Tas.Query.Text(SpaceDataType.ResultantTemperature), Values([21.0, 24.5, 27.5, 29.0]));
                parameterSet.Add(key_Tas_OccupantSensibleGain, Values([0, 80.0, 80.0, 0]));

                space.Add(parameterSet);

                adjacencyCluster.AddObject(space);
            }

            AnalyticalModel result = new AnalyticalModel("Three Flats", null, null, null, adjacencyCluster);

            result.SetValue(Analytical.AnalyticalModelParameter.WeatherData, new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear()));

            return result;
        }

        private static WeatherYear WeatherYear()
        {
            WeatherYear result = new WeatherYear(2018);

            for (int day = 0; day < 365; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    result.Add(day, hour, new Dictionary<string, double> { { WeatherDataType.DryBulbTemperature.ToString(), 20.0 } });
                }
            }

            return result;
        }

        private static JsonArray Values(IEnumerable<double> values)
        {
            JsonArray result = new JsonArray();

            foreach (double value in values)
            {
                result.Add(value);
            }

            return result;
        }
    }
}
