// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Core;
using SAM.Geometry.Spatial;
using SAM.Weather;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using AnalyticalCreate = SAM.Analytical.Create;
using AnalyticalModify = SAM.Analytical.Modify;
using AnalyticalQuery = SAM.Analytical.Query;
using AnalyticalZone = SAM.Analytical.Zone;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>One Base MVHR dwelling, carried COM-free from the Part O preparation to the places the TAS side
    /// has to agree with it: the directional air movements the inter-zone air movement export reads, the
    /// exported ventilation type, and the TM59 criterion the assessment applies.</b>
    /// <para>
    /// The mirror of <see cref="PartONaturalVentilationWorkflowTests"/>, and deliberately the smallest case
    /// that can tell the two directions apart: <b>one habitable room supplied and not extracted, one wet
    /// room extracted and not supplied</b>. That shape is what a balanced heat recovery dwelling actually
    /// is - the system balances, the rooms do not, and the air moves between them as transfer air - and it
    /// is the shape the previous implementation could not express, because it derived both directions from
    /// the space's supply airflow and so extracted from every bedroom and supplied every bathroom.
    /// </para>
    /// <para>
    /// <b>The dwelling states <c>NV</c> on its internal conditions on purpose.</b> Every control below shows
    /// the pre-scenario derivation answering "natural" for this model, so each assertion is a change of
    /// answer rather than an agreement - the explicit route is authoritative, or these tests would pass for
    /// the wrong reason. It is the same control the natural ventilation file uses, pointing the other way.
    /// </para>
    /// <para>
    /// <b>No TAS COM.</b> The air movements, their endpoints and their airflows are analytical objects;
    /// <c>Building</c> and <c>Zone</c> are XML writers over analytical objects;
    /// <c>TMOverheatingCalculator</c> reads hourly series off a <c>Space</c>. Nothing here instantiates a
    /// coclass or opens a document. What a licensed run adds is the file itself - see
    /// <c>SAM/documentation/PartO-TAS-VALIDATION.md</c>.
    /// </para>
    /// <para>
    /// Types are fully qualified throughout: this namespace nests under <c>SAM.Analytical.Tas</c> and
    /// <c>SAM.Analytical.Tas.TM59</c>, both of which declare their own <c>Query</c>, <c>Zone</c>,
    /// <c>Modify</c> and <c>Convert</c>, and an unqualified name binds silently to the wrong one.
    /// </para>
    /// </summary>
    [TestFixture]
    public class PartOBaseMVHRWorkflowTests
    {
        private const string SpaceName_Habitable = "Flat 1 Bedroom 2";

        private const string SpaceName_WetRoom = "Flat 1 Bathroom";

        private const string ZoneName = "Flat 1";

        /// <summary>What the model's own data says, and what the MVHR scenario has to beat.</summary>
        private const string VentilationSystemTypeName_Model = "NV";

        private const double Supply_Lps = 10.0;

        private const double Extract_Lps = 10.0;

        private const string key_ResultantTemperature = "Resultant Temperature";

        private const string key_OccupantSensibleGain = "Occupant Sensible Gain";

        private static TM59Manager tM59Manager;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "SAM_InternalConditionTextMap_TM59.JSON");

            tM59Manager = new TM59Manager(Core.Create.IJSAMObject<TextMap>(File.ReadAllText(path)));
        }

        // =================================================================================================
        // 1. The preparation this workflow starts from
        // =================================================================================================

        /// <summary>
        /// The model handed to the export is a dwelling with the Approved Document F continuous rate on it -
        /// the exact inverse of the natural ventilation case, where the assertion is that no such rate is
        /// there. Both directions are written, including the explicit zero a room with no terminal of that
        /// direction gets.
        /// </summary>
        [Test]
        public void ThePreparedMVHRDwelling_CarriesTheDesignAirflowInBothDirections()
        {
            AnalyticalModel analyticalModel = Prepared();

            Space space_Habitable = Space(analyticalModel, SpaceName_Habitable);
            Space space_WetRoom = Space(analyticalModel, SpaceName_WetRoom);

            Assert.That(space_Habitable.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlow, out double supply_Habitable), Is.True);
            Assert.That(space_Habitable.InternalCondition.TryGetValue(InternalConditionParameter.ExhaustAirFlow, out double extract_Habitable), Is.True);

            Assert.That(supply_Habitable, Is.EqualTo(Supply_Lps / 1000.0).Within(1e-9));
            Assert.That(extract_Habitable, Is.EqualTo(0).Within(1e-9));

            Assert.That(space_WetRoom.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlow, out double supply_WetRoom), Is.True);
            Assert.That(space_WetRoom.InternalCondition.TryGetValue(InternalConditionParameter.ExhaustAirFlow, out double extract_WetRoom), Is.True);

            Assert.That(supply_WetRoom, Is.EqualTo(0).Within(1e-9));
            Assert.That(extract_WetRoom, Is.EqualTo(Extract_Lps / 1000.0).Within(1e-9));
        }

        /// <summary>
        /// <b>The directional realization, room by room - the assertion the previous implementation fails.</b>
        /// <para>
        /// Air movements used to be derived from <c>CalculatedSupplyAirFlow</c> in BOTH directions, so the
        /// habitable room below would carry an extract movement of 10 l/s it has no extract terminal for, and
        /// the wet room a supply movement of 0. The dwelling moved roughly the right total amount of air
        /// through the wrong rooms, and the exported inter-zone air movements said so.
        /// </para>
        /// <para>
        /// Each direction is now the sum of that room's own design terminals of that direction, and a
        /// direction with no terminal produces <b>no movement at all</b> rather than one of zero: a movement
        /// that moves nothing is indistinguishable in the exported file from one that was meant to move air
        /// and failed to.
        /// </para>
        /// </summary>
        [Test]
        public void TheDirectionalAirMovements_MatchTheDesignTerminalDutiesRoomByRoom()
        {
            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            AirHandlingUnit airHandlingUnit = preparation.AirHandlingUnit;

            Assert.That(airHandlingUnit, Is.Not.Null);

            //The habitable room: supplied from the unit, and not extracted.
            Movements(adjacencyCluster, SpaceName_Habitable, airHandlingUnit, out List<SpaceAirMovement> supply_Habitable, out List<SpaceAirMovement> extract_Habitable);

            Assert.That(supply_Habitable.Count, Is.EqualTo(1), "The supplied room has no supply air movement.");
            Assert.That(extract_Habitable, Is.Empty, "The supplied room was given an extract air movement it has no extract terminal for.");

            Assert.That(supply_Habitable[0].AirFlow, Is.EqualTo(Supply_Lps / 1000.0).Within(1e-9));

            //The wet room: extracted back to the unit, and not supplied.
            Movements(adjacencyCluster, SpaceName_WetRoom, airHandlingUnit, out List<SpaceAirMovement> supply_WetRoom, out List<SpaceAirMovement> extract_WetRoom);

            Assert.That(supply_WetRoom, Is.Empty, "The extracted room was given a supply air movement it has no supply terminal for.");
            Assert.That(extract_WetRoom.Count, Is.EqualTo(1), "The extracted room has no extract air movement.");

            Assert.That(extract_WetRoom[0].AirFlow, Is.EqualTo(Extract_Lps / 1000.0).Within(1e-9));
        }

        /// <summary>
        /// The extract movement's destination is the unit, which is the only form the export can carry: a
        /// TBD inter-zone air movement moves air INTO the zones it is assigned to, from a source zone or from
        /// outside, and has no outward direction at all. An extract is therefore a movement on the UNIT's
        /// zone sourced from the room, and a destination of null - which is what every movement carried
        /// before this - is an inter-zone air movement with neither a source nor outside air behind it.
        /// </summary>
        [Test]
        public void TheExtractMovement_NamesTheUnitAsItsDestination_SoTheExportCanCarryIt()
        {
            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            Movements(adjacencyCluster, SpaceName_WetRoom, preparation.AirHandlingUnit, out List<SpaceAirMovement> _, out List<SpaceAirMovement> extract);

            SpaceAirMovement spaceAirMovement = extract[0];

            Assert.That(spaceAirMovement.From, Is.EqualTo(new ObjectReference(Space(analyticalModel, SpaceName_WetRoom)).ToString()));
            Assert.That(spaceAirMovement.To, Is.EqualTo(new ObjectReference(preparation.AirHandlingUnit).ToString()));
        }

        // =================================================================================================
        // 1b. Where the export stops agreeing with the model, and why
        // =================================================================================================

        /// <summary>
        /// <b>The export takes the room's extract straight outside, and gives the unit no exhaust.</b>
        /// <para>
        /// The SAM model above is right and stays as it is: the extract air physically passes through the
        /// unit. TAS cannot hold that, because <c>Modify.UpdateIZAMs</c> makes the unit a <b>thermal
        /// zone</b> and a TAS thermal zone is one well-mixed air node - a supply airstream and an extract
        /// airstream cannot pass through it without meeting. A licensed A/B measured what the meeting
        /// costs: the unit's zone ran 3.79 K above outside in the annual mean and its Air Movement Gain
        /// averaged +755 W, an unstated ~50%-effective heat exchanger in an iteration that specifies none.
        /// </para>
        /// <para>
        /// So <c>room -&gt; unit</c> plus <c>unit -&gt; Outside</c> is written as <c>room -&gt; Outside</c>.
        /// Both halves are asserted here, because either alone would be wrong: the flattening without the
        /// exhaust dropped would take the same air out of the building twice.
        /// </para>
        /// </summary>
        [Test]
        public void TheExport_TakesTheRoomExtractStraightOutside_AndLeavesTheUnitNoExhaust()
        {
            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            Movements(adjacencyCluster, SpaceName_WetRoom, preparation.AirHandlingUnit, out List<SpaceAirMovement> _, out List<SpaceAirMovement> extract);

            HashSet<System.Guid> guids_Flattened = TasQuery.DesignTerminalExtractFlattening(adjacencyCluster, out HashSet<System.Guid> guids_AirHandlingUnit);

            //The wet room's extract, and only it.
            Assert.That(guids_Flattened, Is.EquivalentTo(new[] { extract[0].Guid }));

            //And the unit it used to arrive at loses its exhaust, so that duty leaves the building once.
            Assert.That(guids_AirHandlingUnit, Is.EquivalentTo(new[] { preparation.AirHandlingUnit.Guid }));
        }

        /// <summary>
        /// <b>Only the extract is touched.</b> The supply movement from the unit into the habitable room and
        /// the transfer air between the rooms are the two shapes that must survive the correction unchanged
        /// - the first because <c>Outside -&gt; unit -&gt; room</c> is the airflow route being modelled, the
        /// second because the Approved Document F network is what balances a room that is extracted and not
        /// supplied. Flattening either would be a different building.
        /// </summary>
        [Test]
        public void TheExport_LeavesTheSupplyAndTheTransferAirAlone()
        {
            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            HashSet<System.Guid> guids_Flattened = TasQuery.DesignTerminalExtractFlattening(adjacencyCluster, out HashSet<System.Guid> _);

            string reference_AirHandlingUnit = new ObjectReference(preparation.AirHandlingUnit).ToString();

            int count_Supply = 0;
            int count_Transfer = 0;

            foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetObjects<SpaceAirMovement>() ?? new List<SpaceAirMovement>())
            {
                //Out of the unit into a room - the supply.
                if (spaceAirMovement.From == reference_AirHandlingUnit)
                {
                    Assert.That(guids_Flattened, Does.Not.Contain(spaceAirMovement.Guid), "A supply movement was flattened to outside.");

                    count_Supply++;

                    continue;
                }

                //Room to room - the Part F transfer network.
                if (adjacencyCluster.AirMovementEndpoint(spaceAirMovement.From) is Space && adjacencyCluster.AirMovementEndpoint(spaceAirMovement.To) is Space)
                {
                    Assert.That(guids_Flattened, Does.Not.Contain(spaceAirMovement.Guid), "A transfer air movement was flattened to outside.");

                    count_Transfer++;
                }
            }

            Assert.That(count_Supply, Is.GreaterThan(0), "The fixture carries no supply movement, so nothing was actually checked.");
            Assert.That(count_Transfer, Is.GreaterThan(0), "The fixture carries no transfer air, so nothing was actually checked.");
        }

        /// <summary>
        /// <b>The scope is the design terminal, not the shape and not the name.</b>
        /// <para>
        /// This is the same prepared dwelling - the same <c>MVHR-01</c>-style unit, the same
        /// <c>room -&gt; unit</c> extract movement, the same everything - with the design
        /// <c>VentilationTerminal</c>s taken off it. Nothing flattens. The gate is the authority
        /// <c>Modify.AddAirMovementObjects</c> itself uses to choose its design-terminal branch, which is
        /// the only branch that ever routes a room's air INTO a unit; the generic branch every MEP model
        /// without design terminals reaches already writes each space's outward movement straight to
        /// outside and gives its unit nothing to receive.
        /// </para>
        /// <para>
        /// So a generic MEP export cannot reach this correction even when it carries an air handling unit,
        /// and no existing workflow changes. Take this test away and the scope is a guess.
        /// </para>
        /// </summary>
        [Test]
        public void TheExport_FlattensNothingWithoutDesignTerminals_SoGenericMEPIsUntouched()
        {
            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            //Control: with the terminals on, this model does flatten - so the assertion below is a change of
            //answer rather than an agreement.
            Assert.That(TasQuery.DesignTerminalExtractFlattening(adjacencyCluster, out HashSet<System.Guid> _), Is.Not.Empty);

            List<VentilationTerminal> ventilationTerminals = adjacencyCluster.GetObjects<VentilationTerminal>();

            Assert.That(ventilationTerminals, Is.Not.Empty);

            foreach (VentilationTerminal ventilationTerminal in ventilationTerminals)
            {
                Assert.That(adjacencyCluster.RemoveObject<VentilationTerminal>(ventilationTerminal.Guid), Is.True);
            }

            //The unit and the room -> unit extract movement are both still there. Only the authority is gone.
            Assert.That(adjacencyCluster.GetObjects<AirHandlingUnit>(), Is.Not.Empty);

            Movements(adjacencyCluster, SpaceName_WetRoom, preparation.AirHandlingUnit, out List<SpaceAirMovement> _, out List<SpaceAirMovement> extract);

            Assert.That(extract.Count, Is.EqualTo(1));

            HashSet<System.Guid> guids_Flattened = TasQuery.DesignTerminalExtractFlattening(adjacencyCluster, out HashSet<System.Guid> guids_AirHandlingUnit);

            Assert.That(guids_Flattened, Is.Empty, "A model with no design terminals was flattened, so generic MEP exports are affected.");
            Assert.That(guids_AirHandlingUnit, Is.Empty, "A generic air handling unit lost its exhaust.");
        }

        /// <summary>
        /// The unit is given a name no space uses. <c>Modify.UpdateIZAMs</c> gives the unit a TAS zone of its
        /// own and names that zone after the unit, then resolves every movement's endpoints by zone name - so
        /// a unit sharing a name with a room would put that room's air movements on the unit's zone.
        /// </summary>
        [Test]
        public void TheAirHandlingUnit_IsNamedDistinctlyFromEverySpace()
        {
            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation);

            foreach (Space space in analyticalModel.GetSpaces())
            {
                Assert.That(space.Name.Trim().ToUpperInvariant(), Is.Not.EqualTo(preparation.AirHandlingUnit.Name.Trim().ToUpperInvariant()));
            }
        }

        /// <summary>
        /// <b>Airflow data by itself still does not switch TBD Building Simulator ventilation on.</b>
        /// <para>
        /// The preparation assigns no SAM Ventilation profile, so <c>Modify.UpdateInternalCondition</c>'s
        /// <c>ticV</c> write - which is gated on one being assigned, deliberately - stays shut. That gate is
        /// what stops the presence of an airflow parameter meaning "ventilate this zone", and Iteration 1a
        /// does not weaken it: the air arrives as inter-zone air movements from the unit instead, which is
        /// both truthful about the heat recovery and the only route with an extract direction.
        /// </para>
        /// </summary>
        [Test]
        public void ThePreparation_AssignsNoVentilationProfile()
        {
            AnalyticalModel analyticalModel = Prepared();

            foreach (Space space in analyticalModel.GetSpaces())
            {
                Assert.That(space.InternalCondition.GetProfile(ProfileType.Ventilation, analyticalModel.ProfileLibrary), Is.Null);
                Assert.That(space.InternalCondition.TryGetValue(InternalConditionParameter.VentilationProfileName, out string _), Is.False);
            }
        }

        // =================================================================================================
        // 2. The exported ventilation type
        // =================================================================================================

        /// <summary>
        /// <b>The MVHR route exports as Mechanical Ventilation, over a model that says NV.</b>
        /// <para>
        /// The control is the same building <i>before</i> preparation, where the model-derived overload reads
        /// the internal condition and answers Natural Ventilation. It has to be the pre-preparation model:
        /// after preparation this dwelling genuinely carries an MVHR system, so the derivation agrees with
        /// the scenario - and an agreement proves nothing about which of them was believed.
        /// </para>
        /// </summary>
        [Test]
        public void TheMVHRScenario_ExportsAsMechanicalVentilation_OverAModelStatingNV()
        {
            //Control: the dwelling as authored, stating NV on every internal condition and carrying no
            //ventilation system at all, exports as natural.
            Assert.That(SystemTypes(Model().ToTM59(tM59Manager)), Is.EqualTo(new[] { SystemType.NaturalVentilation }));

            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation);

            OverheatingScenario overheatingScenario = preparation.OverheatingScenarios[0];

            Assert.That(overheatingScenario.VentilationStrategy, Is.EqualTo("MVRE"));

            Building building = analyticalModel.ToTM59(tM59Manager, Map(analyticalModel, overheatingScenario), out List<string> refusals);

            Assert.That(refusals, Is.Empty);
            Assert.That(building, Is.Not.Null);
            Assert.That(SystemTypes(building), Is.EqualTo(new[] { SystemType.MechanicalVentilation }));
        }

        // =================================================================================================
        // 3. The TM59 criterion
        // =================================================================================================

        /// <summary>
        /// <b>The mechanical route, selected by the scenario.</b>
        /// <para>
        /// The control is the same building before preparation, where the model's own <c>NV</c> picks the
        /// natural bedroom criterion - so the assertion below is a change of answer.
        /// </para>
        /// <para>
        /// Only the habitable room is assessed against a dwelling criterion. TM59 states no space
        /// application for a bathroom, and a space with no application falls to the corridor criterion in
        /// <i>both</i> cases - that is existing behaviour and it is asserted here rather than worked around,
        /// so a future change to it shows up as a failure in a file that says why it matters.
        /// </para>
        /// </summary>
        [Test]
        public void TheMVHRScenario_SelectsTheMechanicalTM59Route()
        {
            //Control: the dwelling as authored, and its own NV picks the natural bedroom criterion.
            AnalyticalModel analyticalModel_Authored = Model();

            TM59ExtendedResult result_Control = Result(Calculator(analyticalModel_Authored, null).Calculate_TM59(analyticalModel_Authored.GetSpaces()), SpaceName_Habitable);

            Assert.That(result_Control, Is.InstanceOf<TM59NaturalVentilationBedroomExtendedResult>());

            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation);

            List<TM59ExtendedResult> results = Calculator(analyticalModel, Map(analyticalModel, preparation.OverheatingScenarios[0])).Calculate_TM59(analyticalModel.GetSpaces());

            TM59ExtendedResult result = Result(results, SpaceName_Habitable);

            Assert.That(result, Is.InstanceOf<TM59MechanicalVentilationExtendedResult>());

            //And it is the mechanical branch, not merely a type that inherits from something shared: the
            //extended results are a separate inheritance branch from their plain siblings, so an
            //"is TM59MechanicalVentilationResult" check would be false for every one of these.
            Assert.That(result, Is.Not.InstanceOf<TM59NaturalVentilationExtendedResult>());
            Assert.That(result, Is.Not.InstanceOf<TM59NaturalVentilationBedroomExtendedResult>());
            Assert.That(result, Is.Not.InstanceOf<TM59CorridorExtendedResult>());

            //The wet room, in both cases: no TM59 space application, so the corridor criterion.
            Assert.That(Result(results, SpaceName_WetRoom), Is.InstanceOf<TM59CorridorExtendedResult>());
        }

        /// <summary>
        /// <b>The word the Approved Document itself uses reaches the assessment.</b>
        /// <para>
        /// <c>Query.PartOVentilationMode</c> takes <c>MVHR</c> and <c>MVRE</c> as two spellings of the one
        /// route, but <c>VentilationStrategyMap</c>'s recognised vocabulary did not contain <c>MVHR</c> - so
        /// an assessment stating it prepared successfully, simulated, and then produced <b>no results at
        /// all</b>, every space refused. The scenario keeps saying <c>MVHR</c> rather than being rewritten
        /// into <c>MVRE</c>: a scenario has to keep saying what the assessment said.
        /// </para>
        /// </summary>
        [Test]
        public void TheWordMVHR_SelectsTheMechanicalTM59Route_WithNoRefusals()
        {
            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation, "MVHR");

            OverheatingScenario overheatingScenario = preparation.OverheatingScenarios[0];

            Assert.That(overheatingScenario.VentilationStrategy, Is.EqualTo("MVHR"));

            List<Space> spaces = analyticalModel.GetSpaces();

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel, Map(analyticalModel, overheatingScenario));

            List<TM59ExtendedResult> results = tMOverheatingCalculator.Calculate_TM59(spaces);

            //The point of the fix: no space was refused for stating a word the route resolution had already
            //accepted.
            Assert.That(tMOverheatingCalculator.VentilationStrategyRefusals, Is.Empty);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(Result(results, SpaceName_Habitable), Is.InstanceOf<TM59MechanicalVentilationExtendedResult>());
        }

        /// <summary>And it exports as mechanical too, so the criterion and the exported type agree.</summary>
        [Test]
        public void TheWordMVHR_ExportsAsMechanicalVentilation()
        {
            AnalyticalModel analyticalModel = Prepared(out PartOIterationPreparation preparation, "MVHR");

            Building building = analyticalModel.ToTM59(tM59Manager, Map(analyticalModel, preparation.OverheatingScenarios[0]), out List<string> refusals);

            Assert.That(refusals, Is.Empty);
            Assert.That(SystemTypes(building), Is.EqualTo(new[] { SystemType.MechanicalVentilation }));
        }

        // =================================================================================================
        // Fixture
        // =================================================================================================

        private static AnalyticalModel Prepared()
        {
            return Prepared(out PartOIterationPreparation _);
        }

        private static AnalyticalModel Prepared(out PartOIterationPreparation preparation, string ventilationStrategy = "MVRE")
        {
            AnalyticalModel analyticalModel = Model();

            List<AnalyticalZone> zones = analyticalModel.GetZones();

            Assert.That(zones.Count, Is.EqualTo(1));

            //The EXPLICIT Part O route. Not read off the model - which says NV - and not defaulted.
            Dictionary<System.Guid, string> dictionary_VentilationStrategy = new Dictionary<System.Guid, string> { { zones[0].Guid, ventilationStrategy } };

            //BasePassive, not BaseNaturalVentilation: Iteration 1a is the base configuration defined over the
            //MVHR route, and BaseNaturalVentilation would refuse here because its own operating assumptions
            //assert that the dwelling has no continuous mechanical supply or extract.
            preparation = AnalyticalModify.PreparePartOIteration(analyticalModel, PartOIteration.BasePassive, null, dictionary_VentilationStrategy);

            Assert.That(preparation.Refusal, Is.Null);
            Assert.That(preparation.VentilationMode, Is.EqualTo(PartOVentilationMode.MVHR));
            Assert.That(preparation.AirflowApplication, Is.EqualTo(PartOPartFAirflowApplication.Apply));
            Assert.That(preparation.Successful, Is.True);
            Assert.That(preparation.OverheatingScenarios.Count, Is.EqualTo(1));

            Assert.That(preparation.DesignSupplyDuty_Lps, Is.EqualTo(Supply_Lps).Within(1e-9));
            Assert.That(preparation.DesignExtractDuty_Lps, Is.EqualTo(Extract_Lps).Within(1e-9));

            return preparation.AnalyticalModel;
        }

        /// <summary>
        /// One flat with the two shapes that matter, and the hourly series a TSD conversion leaves behind.
        /// <para>
        /// The Approved Document F data is authored directly rather than calculated, so the fixture states
        /// exactly the asymmetry under test - a supplied room and an extracted room - without depending on
        /// an installed rule set. What runs for real is everything downstream of it: the production
        /// preparation, the design realization, the system, and the air movements.
        /// </para>
        /// </summary>
        private static AnalyticalModel Model()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            //Named "Bedroom" and "Bathroom" so the shipped TM59 TextMap resolves their applications.
            adjacencyCluster.AddObject(Space(SpaceName_Habitable, "Bedroom", 16.0, PartFVentilationType.supply, PartFTerminalRole.Supply, Supply_Lps));
            adjacencyCluster.AddObject(Space(SpaceName_WetRoom, "Bathroom", 6.0, PartFVentilationType.extract, PartFTerminalRole.GeneralExtract, Extract_Lps));

            //The partition between them. It is what makes this a dwelling rather than two loose rooms: the
            //supplied bedroom's air reaches the extracted bathroom across it, which is the transfer air an
            //MVHR design depends on. Without it neither room can balance, and TAS will not simulate a zone
            //that gains air it never loses - so the preparation refuses instead of producing one.
            AddPartition(adjacencyCluster, SpaceName_Habitable, SpaceName_WetRoom);

            AnalyticalZone zone = new AnalyticalZone(ZoneName);
            adjacencyCluster.AddObject(zone);

            //Related to every space this fixture builds, matching how a real model's own zoning relates a
            //dwelling zone to its rooms - Modify.PrepareBaseMVHR (SAM.Analytical) partitions the assessed
            //scope into one Base MVHR system per dwelling zone using exactly this relation
            //(Query.PartFDwellingZones plus the zone -> space relation).
            foreach (Space space_Existing in adjacencyCluster.GetSpaces())
            {
                adjacencyCluster.AddRelation(zone, space_Existing);
            }

            AnalyticalModel result = new AnalyticalModel("Part O Base MVHR Dwelling", null, null, null, adjacencyCluster);

            //Qualified: unqualified AnalyticalModelParameter binds to SAM.Analytical.Tas's own enum here.
            result.SetValue(Analytical.AnalyticalModelParameter.WeatherData, new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear()));

            return result;
        }

        /// <summary>
        /// Puts an internal separating element between two of the model's spaces, which is what makes them
        /// adjacent and so what puts an edge in the dwelling's transfer air network.
        /// </summary>
        private static void AddPartition(AdjacencyCluster adjacencyCluster, string name_1, string name_2)
        {
            List<Space> spaces = adjacencyCluster.GetSpaces();

            Panel panel = AnalyticalCreate.Panel(
                new Construction(System.Guid.NewGuid(), "Internal Partition"),
                PanelType.WallInternal,
                new Face3D(new Polygon3D(
                [
                    new Point3D(0, 0, 0),
                    new Point3D(4, 0, 0),
                    new Point3D(4, 0, 3),
                    new Point3D(0, 0, 3),
                ])));

            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(spaces.Find(x => x.Name == name_1), panel);
            adjacencyCluster.AddRelation(spaces.Find(x => x.Name == name_2), panel);
        }

        private static Space Space(string name, string name_InternalCondition, double area, PartFVentilationType partFVentilationType, PartFTerminalRole partFTerminalRole, double continuous_Lps)
        {
            InternalCondition internalCondition = new InternalCondition(name_InternalCondition);

            //What the model's own data says, and what the explicit MVHR route has to beat.
            internalCondition.SetValue(InternalConditionParameter.VentilationSystemTypeName, VentilationSystemTypeName_Model);

            Space result = new Space(name) { InternalCondition = internalCondition };

            result.SetValue(Analytical.SpaceParameter.Area, area);
            result.SetValue(Analytical.SpaceParameter.Volume, area * 2.5);

            PartFSpaceData partFSpaceData = new PartFSpaceData(
                name_InternalCondition,
                partFVentilationType == PartFVentilationType.supply ? PartFType.Habitable : PartFType.WetRoom,
                partFVentilationType,
                partFTerminalRole == PartFTerminalRole.Supply,
                null,
                true,
                true,
                partFVentilationType == PartFVentilationType.supply,
                false,
                "Volume",
                continuous_Lps);

            partFSpaceData.Terminals.Add(new PartFVentilationTerminalRequirement(string.Format("{0} - {1}", name, partFTerminalRole), result.Guid, partFTerminalRole)
            {
                SpaceName = name,
                OperatingMode = PartFOperatingMode.ContinuousDesign,
                ContinuousDesignFlowRate_Lps = continuous_Lps,
                IsInBalancedFlow = true,
                IsRequired = true,
                SourceReference = partFTerminalRole == PartFTerminalRole.Supply
                    ? "Approved Document F, Volume 1: Dwellings (2021 edition), paragraph 1.67 (page 16)"
                    : "Approved Document F, Volume 1: Dwellings (2021 edition), paragraph 1.17 (page 8), Table 1.2 (page 10) and paragraph 1.70 (page 17)",
            });

            result.SetValue(Analytical.SpaceParameter.PartFSpaceData, partFSpaceData);

            //Exactly how Analytical.Tas.Convert.ToSAM(TSD.ZoneData, ...) stores an hourly series.
            ParameterSet parameterSet = new ParameterSet("SAM.Analytical.Tas.dll");
            parameterSet.Add(key_ResultantTemperature, Series(new double[] { 21.0, 24.5, 27.5, 29.0 }));
            parameterSet.Add(key_OccupantSensibleGain, Series(new double[] { 0, 80.0, 80.0, 0 }));

            result.Add(parameterSet);

            return result;
        }

        /// <summary>The air movements of one space, split by direction relative to the unit.</summary>
        private static void Movements(AdjacencyCluster adjacencyCluster, string name_Space, AirHandlingUnit airHandlingUnit, out List<SpaceAirMovement> supply, out List<SpaceAirMovement> extract)
        {
            Space space = adjacencyCluster.GetSpaces().Find(x => x.Name == name_Space);

            Assert.That(space, Is.Not.Null);

            string reference_AirHandlingUnit = new ObjectReference(airHandlingUnit).ToString();

            supply = new List<SpaceAirMovement>();
            extract = new List<SpaceAirMovement>();

            foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(space) ?? new List<SpaceAirMovement>())
            {
                if (spaceAirMovement.From == reference_AirHandlingUnit)
                {
                    supply.Add(spaceAirMovement);
                }
                else if (spaceAirMovement.To == reference_AirHandlingUnit)
                {
                    extract.Add(spaceAirMovement);
                }
            }
        }

        private static Space Space(AnalyticalModel analyticalModel, string name)
        {
            Space result = analyticalModel.GetSpaces().Find(x => x.Name == name);

            Assert.That(result, Is.Not.Null);

            return result;
        }

        /// <summary>One space's result, by name - the results come back in whatever order the spaces did.</summary>
        private static TM59ExtendedResult Result(List<TM59ExtendedResult> results, string name_Space)
        {
            TM59ExtendedResult result = results?.Find(x => x?.Name == name_Space);

            Assert.That(result, Is.Not.Null, string.Format("No TM59 result was produced for '{0}'.", name_Space));

            return result;
        }

        /// <summary>The TAS-side calculator, reading the TAS legacy series keys the fixture stores.</summary>
        private static TMOverheatingCalculator Calculator(AnalyticalModel analyticalModel, VentilationStrategyMap ventilationStrategyMap)
        {
            return new TMOverheatingCalculator(analyticalModel)
            {
                TextMap = TM59TextMap(),
                ResultantTemperatureSeriesKey = key_ResultantTemperature,
                OccupancySensibleGainSeriesKey = key_OccupantSensibleGain,
                VentilationStrategyMap = ventilationStrategyMap,
            };
        }

        private static TextMap TM59TextMap()
        {
            TextMap result = Core.Create.TextMap("TM59");

            result.Add("Sleeping", "Bedroom");

            return result;
        }

        /// <summary>The scenario, mapped onto the spaces it governs - the whole dwelling.</summary>
        private static VentilationStrategyMap Map(AnalyticalModel analyticalModel, OverheatingScenario overheatingScenario)
        {
            VentilationStrategyMap result = new VentilationStrategyMap();

            result.Add(overheatingScenario, analyticalModel.GetSpaces());

            return result;
        }

        private static SystemType[] SystemTypes(Building building)
        {
            List<SystemType> result = new List<SystemType>();

            foreach (TM59.Zone zone in building?.Zones ?? new List<TM59.Zone>())
            {
                if (!result.Contains(zone.SystemType))
                {
                    result.Add(zone.SystemType);
                }
            }

            return result.ToArray();
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

        private static JsonArray Series(IEnumerable<double> values)
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
