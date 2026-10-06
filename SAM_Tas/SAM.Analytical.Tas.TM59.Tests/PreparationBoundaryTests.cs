// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Step 9 - the TSD-simple vs TPD-full preparation boundary.</b>
    /// <para>
    /// The rule these tests exist to hold: <b>preparation differs; assessment does not.</b> Three routes reach
    /// TM59, and they prepare the required hourly <c>ResultantTemperature</c> series in three different ways:
    /// </para>
    /// <code>
    /// TSD-simple:  TSD ─────────────────────────────────────────────────► model ─┐
    /// TPD-full:    TPD ─► pass 1 ─► TBD COPY ─► pass 2 ─► TSD ─────────► model ─┼─► TM59AssessmentCalculator
    /// TPD-approx:  TPD + companion TSD ─► synthesised (MRT + ZT) / 2 ──► model ─┘
    /// </code>
    /// <para>
    /// <b>TPD-full is authoritative and its second simulation is deliberate.</b> A TPD run models the real system
    /// but produces no resultant temperature; only a TBD run does. TPD-approx is a <b>legacy approximation</b>
    /// kept for compatibility, and a failed TPD-full run must never quietly become one.
    /// </para>
    /// <para>
    /// <b>Why these tests need no installed TAS.</b> The two preparation types under test -
    /// <c>ResultantTemperaturePreparation</c> and <c>ApproximateResultantTemperatureMap</c> - are deliberately
    /// free of TAS COM types, which is what lets the decisions (which file, which quantity, refuse or proceed) be
    /// exercised directly. The COM work sits behind them.
    /// </para>
    /// </summary>
    [TestFixture]
    public class PreparationBoundaryTests
    {
        //Qualified throughout: inside this namespace SAM.Analytical.Tas.TM59 has its own Query, SAM.Analytical.Tas
        //its own SpaceParameter/SpaceDataType, and SAM.Analytical.Systems another SpaceDataType again.
        private const string key_Tas_OccupantSensibleGain = "Occupant Sensible Gain";

        private static readonly string[] flats = ["Flat 1", "Flat 2", "Flat 3"];

        // =============================================================================================
        // Mechanism A - the authoritative TPD-full two-pass route
        // =============================================================================================

        /// <summary>
        /// <b>The second pass targets a copy, and the copy is not the design model.</b> Path algebra only, but it
        /// is the invariant everything else on this route rests on.
        /// </summary>
        [Test]
        public void TheSecondPass_TargetsACopyDistinctFromTheDesignTBD()
        {
            ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(Path.Combine("C:", "models", "block.tpd"));

            Assert.That(resultantTemperaturePreparation.IsSupported, Is.True);

            Assert.That(resultantTemperaturePreparation.Path_TBD_Design, Is.EqualTo(Path.Combine("C:", "models", "block.tbd")));
            Assert.That(resultantTemperaturePreparation.Path_TBD_Simulation, Is.Not.EqualTo(resultantTemperaturePreparation.Path_TBD_Design), "The second pass would have written to the design model.");

            //The second pass owns both of these, and neither can collide with a design file.
            Assert.That(resultantTemperaturePreparation.Path_TBD_Simulation, Does.Contain(ResultantTemperaturePreparation.Suffix));
            Assert.That(resultantTemperaturePreparation.Path_TSD_Simulation, Does.Contain(ResultantTemperaturePreparation.Suffix));
        }

        /// <summary>
        /// <b>A refusal writes nothing.</b> The design TBD is byte-identical afterwards and no copy was created -
        /// proved on a real directory, because "we return false before the copy" is a claim about ordering that a
        /// future edit can silently break.
        /// </summary>
        [Test]
        public void ARefusedTransfer_CopiesNothingAndLeavesTheDesignTBDUntouched()
        {
            using (Fixture fixture = new Fixture())
            {
                //The transfer TAS cannot perform. Asking for it must not start the route.
                ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(fixture.Path_TPD, ResultantTemperatureTransfer.SupplyAirTemperatureAndAirflow);

                Dictionary<string, IndexedDoubles> transferred;
                List<string> refusals;

                Assert.That(resultantTemperaturePreparation.TryBeginSecondPass(Results(), out transferred, out refusals), Is.False);
                Assert.That(transferred, Is.Empty);

                //Refused for the RIGHT reason - the unsupported transfer - and not merely because the payload
                //came back empty, which would also have stopped it one guard later.
                Assert.That(refusals, Has.Count.EqualTo(1));
                Assert.That(refusals[0], Does.Contain("supply air temperature"));

                Assert.That(fixture.DesignTBDUnchanged, Is.True, "A refused route modified the design TBD.");
                Assert.That(File.Exists(resultantTemperaturePreparation.Path_TBD_Simulation), Is.False, "A refused route left a copy behind.");
            }
        }

        /// <summary>
        /// <b>Where the route does proceed, it makes the copy - at a path the design model does not own.</b>
        /// <para>
        /// Note what this does and does not say: <c>TryBeginSecondPass</c> never <i>modifies</i> a TBD, so
        /// "unchanged" here is about the copy existing and the original being left alone up to that point. The
        /// writing is the COM code beyond this seam and is not reachable without an installed TAS.
        /// </para>
        /// </summary>
        [Test]
        public void TheSupportedTransfer_CopiesTheDesignTBDToItsOwnPath()
        {
            using (Fixture fixture = new Fixture())
            {
                ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(fixture.Path_TPD);

                Dictionary<string, IndexedDoubles> transferred;
                List<string> refusals;

                Assert.That(resultantTemperaturePreparation.TryBeginSecondPass(Results(), out transferred, out refusals), Is.True);
                Assert.That(refusals, Is.Empty);

                Assert.That(File.Exists(resultantTemperaturePreparation.Path_TBD_Simulation), Is.True, "The second pass had no copy to work on.");
                Assert.That(fixture.DesignTBDUnchanged, Is.True, "The route modified the design TBD instead of the copy.");
            }
        }

        /// <summary>
        /// <b>A TPD with no first-pass results refuses, and still copies nothing.</b>
        /// <para>
        /// Without a payload the copy would be identical to the design model, so the second pass would return a
        /// plain TBD answer while the workflow reported it as a systems-aware one - the one failure on this route
        /// that produces a plausible number rather than an error.
        /// </para>
        /// </summary>
        [Test]
        public void AFirstPassWithNoResults_RefusesRatherThanSimulatingAnUnmodifiedCopy()
        {
            using (Fixture fixture = new Fixture())
            {
                ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(fixture.Path_TPD);

                Dictionary<string, IndexedDoubles> transferred;
                List<string> refusals;

                //A result that carries every series EXCEPT the one this transfer needs.
                SystemSpaceResult systemSpaceResult = Result("tas-zone-0", "Bedroom 2", new Dictionary<Analytical.Systems.SpaceDataType, IndexedDoubles>
                {
                    { Analytical.Systems.SpaceDataType.SupplyAirTemperature, Series(18.0) },
                });

                Assert.That(resultantTemperaturePreparation.TryBeginSecondPass([systemSpaceResult], out transferred, out refusals), Is.False);
                Assert.That(refusals, Is.Not.Empty);

                Assert.That(File.Exists(resultantTemperaturePreparation.Path_TBD_Simulation), Is.False);
                Assert.That(fixture.DesignTBDUnchanged, Is.True);
            }
        }

        /// <summary>
        /// <b>An empty zone-temperature series is not a payload.</b>
        /// <para>
        /// A <c>SystemSpaceResult</c> can carry a non-null but EMPTY series - a zone the first pass produced no
        /// hourly values for. Counting it towards the payload lets the route proceed and copy the TBD, and the
        /// COM write beyond this seam then reads <c>values.Count</c> off it: either an exception out of a route
        /// whose contract is refusal, or a default series simulated and reported as a systems-aware answer.
        /// Absent and empty mean the same thing here.
        /// </para>
        /// </summary>
        [Test]
        public void AnEmptyZoneTemperatureSeries_IsNotTakenAsAUsablePayload()
        {
            using (Fixture fixture = new Fixture())
            {
                ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(fixture.Path_TPD);

                Dictionary<string, IndexedDoubles> transferred;
                List<string> refusals;

                //The series this transfer needs is present, and holds nothing.
                SystemSpaceResult systemSpaceResult = Result("tas-zone-0", "Bedroom 2", new Dictionary<Analytical.Systems.SpaceDataType, IndexedDoubles>
                {
                    { Analytical.Systems.SpaceDataType.ZoneTemperature, new IndexedDoubles() },
                });

                Assert.That(resultantTemperaturePreparation.TryBeginSecondPass([systemSpaceResult], out transferred, out refusals), Is.False);
                Assert.That(refusals, Is.Not.Empty);
                Assert.That(transferred, Is.Empty);

                Assert.That(File.Exists(resultantTemperaturePreparation.Path_TBD_Simulation), Is.False, "An unusable payload still copied the design TBD.");
                Assert.That(fixture.DesignTBDUnchanged, Is.True);
            }
        }

        /// <summary>
        /// <b>A copy that cannot be made is a refusal, not an exception.</b>
        /// <para>
        /// The routine case is a previous run's <c>_TPDThermostat.tbd</c> still held open by TAS.
        /// <c>Modify.CalculateResultantTemperature</c> does not catch, so an escaping <c>IOException</c> would
        /// surface as a bare exception on a port that reports every other failure as a refusal - and the design
        /// TBD must still be untouched.
        /// </para>
        /// </summary>
        [Test]
        public void ACopyTargetThatCannotBeWritten_IsRefusedRatherThanThrown()
        {
            using (Fixture fixture = new Fixture())
            {
                ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(fixture.Path_TPD);

                //The target held open with no sharing - what a TAS session that still has the previous run's
                //copy loaded does to it.
                using (FileStream fileStream = new FileStream(resultantTemperaturePreparation.Path_TBD_Simulation, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Dictionary<string, IndexedDoubles> transferred;
                    List<string> refusals;

                    Assert.That(resultantTemperaturePreparation.TryBeginSecondPass(Results(), out transferred, out refusals), Is.False);
                    Assert.That(refusals, Is.Not.Empty);
                    Assert.That(transferred, Is.Empty);

                    Assert.That(fixture.DesignTBDUnchanged, Is.True);
                }
            }
        }

        /// <summary>
        /// <b>The route genuinely consumes the first simulation.</b> The payload is the systems model's own hourly
        /// output: change the first pass and the payload changes with it. A payload that were a setpoint, a
        /// schedule or anything restated from the design model would be identical across these two runs.
        /// </summary>
        [Test]
        public void TheTransfer_CarriesTheFirstPassOwnResultsNotARestatedSetpoint()
        {
            ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(Path.Combine("C:", "models", "block.tpd"));

            Dictionary<string, IndexedDoubles> transferred_Warm = resultantTemperaturePreparation.Transferred([Result("tas-zone-0", "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 26.5)]);
            Dictionary<string, IndexedDoubles> transferred_Cool = resultantTemperaturePreparation.Transferred([Result("tas-zone-0", "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 21.0)]);

            Assert.That(transferred_Warm["Bedroom 2"].GetValue(0), Is.EqualTo(26.5).Within(1e-9));
            Assert.That(transferred_Cool["Bedroom 2"].GetValue(0), Is.EqualTo(21.0).Within(1e-9));

            Assert.That(transferred_Warm["Bedroom 2"].GetValue(0), Is.Not.EqualTo(transferred_Cool["Bedroom 2"].GetValue(0)), "The payload did not follow the first pass's results.");
        }

        // =============================================================================================
        // The limitation: the intended supply-conditions transfer, and why it is refused
        // =============================================================================================

        /// <summary>
        /// <b>The intended transfer - supply air temperature and supply airflow into the TBD copy - is refused,
        /// not approximated.</b>
        /// <para>
        /// Established against the TAS interop rather than assumed: across the whole <c>Interop.TBD</c> object
        /// model the only temperature-valued member is <c>IWeatherYear.groundTemperature</c>. A TBD zone, its
        /// internal condition, its thermostat and its internal gain expose no per-zone supply air temperature, so
        /// the write half of that transfer has nowhere to go.
        /// </para>
        /// </summary>
        [Test]
        public void TheSupplyConditionsTransfer_IsRefusedWithTheReasonStated()
        {
            ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(Path.Combine("C:", "models", "block.tpd"), ResultantTemperatureTransfer.SupplyAirTemperatureAndAirflow);

            Assert.That(resultantTemperaturePreparation.IsSupported, Is.False);
            Assert.That(resultantTemperaturePreparation.Refusals, Has.Count.EqualTo(1));

            //Refused for the real reason, so a reader is not left to guess that it is merely unimplemented.
            Assert.That(resultantTemperaturePreparation.Refusals[0], Does.Contain("supply air temperature"));
        }

        /// <summary>
        /// <b>The limitation is on the write side, and this pins which side.</b>
        /// <para>
        /// The read half is available and already happening - a simulated TPD's <c>SystemZone</c> yields both
        /// <c>SupplyAirTemperature</c> and <c>FlowRate</c>, and <c>Convert.ToSAM_SpaceSystemResults</c> asks for
        /// every <c>SpaceDataType</c>. So the route is not blocked for want of data. It is blocked because TBD
        /// cannot receive a supply air temperature, which is why <b>neither half is transferred</b>: air injected
        /// without its temperature enters at outside conditions and states a system that does not exist.
        /// </para>
        /// </summary>
        [Test]
        public void TheFirstPassSupplyConditions_AreReadableButNeitherHalfIsTransferred()
        {
            SystemSpaceResult systemSpaceResult = Result("tas-zone-0", "Bedroom 2", new Dictionary<Analytical.Systems.SpaceDataType, IndexedDoubles>
            {
                { Analytical.Systems.SpaceDataType.SupplyAirTemperature, Series(18.0) },
                { Analytical.Systems.SpaceDataType.FlowRate, Series(42.0) },
                { Analytical.Systems.SpaceDataType.ZoneTemperature, Series(24.0) },
            });

            //Both halves of the intended transfer are in hand on the read side.
            Assert.That(systemSpaceResult[Analytical.Systems.SpaceDataType.SupplyAirTemperature.ToString()], Is.Not.Null);
            Assert.That(systemSpaceResult[Analytical.Systems.SpaceDataType.FlowRate.ToString()], Is.Not.Null);

            //And the route still carries neither of them: no partial transfer is attempted.
            ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(Path.Combine("C:", "models", "block.tpd"), ResultantTemperatureTransfer.SupplyAirTemperatureAndAirflow);

            Assert.That(resultantTemperaturePreparation.Transferred([systemSpaceResult]), Is.Empty, "A refused transfer produced a payload anyway.");
        }

        /// <summary>An unstated transfer is refused rather than defaulted to whichever one happens to work.</summary>
        [Test]
        public void AnUnstatedTransfer_IsRefused()
        {
            ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(Path.Combine("C:", "models", "block.tpd"), ResultantTemperatureTransfer.Undefined);

            Assert.That(resultantTemperaturePreparation.IsSupported, Is.False);
        }

        // =============================================================================================
        // Mechanism A does not fall back to Mechanism B
        // =============================================================================================

        /// <summary>
        /// <b>A failed TPD-full run yields nothing an assessment can run on - it does not become an
        /// approximation.</b>
        /// <para>
        /// Structural, and deliberately so: the two routes are separately callable and neither reaches the other.
        /// A refusal produces no TSD path and no payload, so there is nothing for the caller to convert and
        /// assess; obtaining the approximate route requires explicitly constructing it with a companion TSD model
        /// that the failed route never produced.
        /// </para>
        /// </summary>
        [Test]
        public void AFailedTPDFullRun_ProducesNothingAssessableAndNeverFallsBack()
        {
            using (Fixture fixture = new Fixture())
            {
                ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(fixture.Path_TPD, ResultantTemperatureTransfer.SupplyAirTemperatureAndAirflow);

                Dictionary<string, IndexedDoubles> transferred;
                List<string> refusals;

                Assert.That(resultantTemperaturePreparation.TryBeginSecondPass(Results(), out transferred, out refusals), Is.False);

                //No second TSD was produced, so the TPD-full route has handed the caller no series at all.
                Assert.That(File.Exists(resultantTemperaturePreparation.Path_TSD_Simulation), Is.False);
                Assert.That(transferred, Is.Empty);
                Assert.That(refusals, Is.Not.Empty);
            }

            //And "never falls back" is asserted STRUCTURALLY, because no amount of exercising a refusal can prove
            //the absence of a code path that would only appear if someone added one. The TPD-full route's own type
            //must not so much as mention the approximate route.
            //
            //A reflection check, and deliberately so: this is a statement about what the assembly does NOT
            //contain, which is not a behaviour any input can elicit.
            Assert.That(
                typeof(Modify).Assembly.GetType(typeof(Modify).FullName)
                    .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .Any(x => x.Name == "CalculateResultantTemperature"
                        && x.GetParameters().Any(y => y.ParameterType == typeof(ApproximateResultantTemperatureMap))),
                Is.False,
                "The two-pass route has acquired a dependency on the approximate route.");

            //Nor may the authoritative route's own preparation type know how to build the approximate one.
            Assert.That(
                typeof(ResultantTemperaturePreparation).GetMethods()
                    .Any(x => x.ReturnType == typeof(ApproximateResultantTemperatureMap)),
                Is.False,
                "The two-pass preparation can now hand back an approximation.");
        }

        // =============================================================================================
        // Mechanism B - the legacy approximate route, now prepared inside SAM_Tas
        // =============================================================================================

        /// <summary>
        /// <b>The approximation is the mean of the companion TSD's radiant temperature and the TPD's zone
        /// temperature</b>, synthesised in <c>SAM_Tas</c> and written under the key the assessment reads.
        /// </summary>
        [Test]
        public void TheApproximateRoute_SynthesisesTheMeanOfRadiantAndZoneTemperature()
        {
            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(Model_CompanionTSD(), Results());

            Assert.That(approximateResultantTemperatureMap.Refusals, Is.Empty);
            Assert.That(approximateResultantTemperatureMap.Prepared, Has.Count.EqualTo(4));

            //Radiant 20/24/28/30 against a flat zone temperature of 24 - so 22/24/26/27.
            Space space = Simulated(approximateResultantTemperatureMap.AnalyticalModel, "tas-zone-0");

            Assert.That(Series(space, Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.ResultantTemperature)), Is.EqualTo(new[] { 22.0, 24.0, 26.0, 27.0 }).Within(1e-9));
        }

        /// <summary>
        /// <b>The synthesis lives in SAM_Tas, not in the common calculator - and this proves it behaviourally.</b>
        /// <para>
        /// Handed a model carrying only a radiant temperature series, the engine-neutral
        /// <c>TM59AssessmentCalculator</c> produces no assessment at all. It cannot synthesise a resultant
        /// temperature, does not know how, and must not learn: the arithmetic is a TAS accommodation for a series
        /// TPD does not produce.
        /// </para>
        /// </summary>
        [Test]
        public void TheCommonCalculator_CannotSynthesiseAResultantTemperatureItself()
        {
            AnalyticalModel analyticalModel_CompanionTSD = Model_CompanionTSD();

            //Straight to the assessment, with no TAS preparation in between.
            Analytical.TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel_CompanionTSD.TM59AssessmentCalculator(Model_Design());
            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            TM59AssessmentResult tM59AssessmentResult = tM59AssessmentCalculator.Calculate(tM59AssessmentCalculator.Spaces(null, null));

            Assert.That(Count(tM59AssessmentResult), Is.Zero, "The common calculator produced an assessment from a radiant series alone.");

            //And the arithmetic that makes those spaces assessable is in the TAS TPD assembly, where the boundary
            //puts it - not in SAM.Analytical alongside the calculator.
            Assert.That(typeof(ApproximateResultantTemperatureMap).Assembly, Is.Not.EqualTo(typeof(Analytical.TM59AssessmentCalculator).Assembly));
        }

        /// <summary>
        /// <b>The approximate route associates TPD results by identity.</b> Three flats, one room name, and each
        /// room gets its own flat's system results.
        /// </summary>
        [Test]
        public void TheApproximateRoute_TiesEachBedroom2ToItsOwnTPDResult()
        {
            //Each flat's zone temperature is distinct, so a cross-wiring changes the synthesised series.
            List<SystemSpaceResult> systemSpaceResults =
            [
                Result("tas-zone-0", "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 20.0),
                Result("tas-zone-1", "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 24.0),
                Result("tas-zone-2", "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 28.0),
                Result("tas-zone-corridor", "Corridor", Analytical.Systems.SpaceDataType.ZoneTemperature, 22.0),
            ];

            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(Model_CompanionTSD(), systemSpaceResults);

            Assert.That(approximateResultantTemperatureMap.Refusals, Is.Empty);

            //Radiant is 20/24/28/30 for every space, so the first hour is (20 + own zone temperature) / 2.
            Assert.That(First(approximateResultantTemperatureMap, "tas-zone-0"), Is.EqualTo(20.0).Within(1e-9));
            Assert.That(First(approximateResultantTemperatureMap, "tas-zone-1"), Is.EqualTo(22.0).Within(1e-9));
            Assert.That(First(approximateResultantTemperatureMap, "tas-zone-2"), Is.EqualTo(24.0).Within(1e-9));
        }

        /// <summary>
        /// <b>Without an identity the approximate route refuses rather than crossing flats.</b>
        /// <para>
        /// This is the defect the moved preparation removes. The route it replaced took the first TPD result whose
        /// <i>name</i> matched, so all three "Bedroom 2" rooms silently received <b>Flat 1's</b> system results.
        /// </para>
        /// </summary>
        [Test]
        public void WithoutAnIdentity_TheApproximateRouteRefusesRatherThanCrossingFlats()
        {
            List<SystemSpaceResult> systemSpaceResults =
            [
                Result(null, "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 20.0),
                Result(null, "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 24.0),
                Result(null, "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 28.0),
                Result(null, "Corridor", Analytical.Systems.SpaceDataType.ZoneTemperature, 22.0),
            ];

            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(Model_CompanionTSD(), systemSpaceResults);

            //All three bedrooms refused, plus the one model-wide message saying attribution fell back to names -
            //which is the CAUSE, where the three per-room messages are the symptom.
            Assert.That(approximateResultantTemperatureMap.Refusals, Has.Count.EqualTo(4));
            Assert.That(approximateResultantTemperatureMap.Refusals.FindAll(x => x.Contains("NAME")), Has.Count.EqualTo(1));

            Assert.That(approximateResultantTemperatureMap.Prepared, Has.Count.EqualTo(1));
            Assert.That(approximateResultantTemperatureMap.Prepared[0].Name, Is.EqualTo("Corridor"));

            //And not one of them was given a neighbour's results anyway.
            foreach (string flat in flats)
            {
                Space space = Simulated(approximateResultantTemperatureMap.AnalyticalModel, StableKey(flat));

                Assert.That(space.GetParameterSets().Find(x => x.Contains(Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.ResultantTemperature))), Is.Null, "A room with no resolvable identity was given another room's system results.");
            }
        }

        /// <summary>
        /// <b>A non-numeric radiant value is refused for its own space, and does not abort the whole
        /// preparation.</b> The series comes from parameter-set data and is not strongly typed, so one
        /// hand-edited value could reach it. <c>GetValue&lt;double&gt;</c> threw on anything that was not a
        /// double - which on this route is an exception out of a Grasshopper component instead of a refusal it
        /// can report, and it took the other three spaces down with it rather than excluding the one affected.
        /// </summary>
        [TestCase("\"warm\"", TestName = "NonNumericRadiantValue_RefusesOnlyItsOwnSpace(string)")]
        [TestCase("true", TestName = "NonNumericRadiantValue_RefusesOnlyItsOwnSpace(bool)")]
        [TestCase("null", TestName = "NonNumericRadiantValue_RefusesOnlyItsOwnSpace(null)")]
        public void NonNumericRadiantValue_RefusesOnlyItsOwnSpace(string json_Value)
        {
            AnalyticalModel analyticalModel = Model_CompanionTSD();

            //Flat 2's third hour is replaced in place, so the other three spaces are untouched.
            Corrupt(analyticalModel, StableKey("Flat 2"), 2, json_Value);

            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(analyticalModel, Results());

            //Refused, with a reason, rather than thrown out of the component.
            Assert.That(approximateResultantTemperatureMap.Refusals, Has.Count.EqualTo(1));
            Assert.That(approximateResultantTemperatureMap.Refusals[0], Does.Contain("non-numeric"));

            //And ONLY that space - the other three still prepared, which is what "aborts the entire map" cost.
            Assert.That(approximateResultantTemperatureMap.Prepared, Has.Count.EqualTo(3));
            Assert.That(approximateResultantTemperatureMap.Prepared.Exists(x => Analytical.Tas.Query.SimulationSpaceKey(x) == StableKey("Flat 2")), Is.False);
        }

        /// <summary>
        /// The other half of the same guard: <b>a whole-number radiant value is data, not a fault.</b> A
        /// <c>JsonValue</c> built in-process from an <c>int</c> is <c>JsonValueKind.Number</c> and yet fails
        /// both <c>TryGetValue&lt;double&gt;</c> and <c>GetValue&lt;double&gt;</c> - the framework converts
        /// numeric types only for <c>JsonElement</c>-backed values. So refusing on
        /// <c>TryGetValue&lt;double&gt;</c> alone would have turned a legitimate series into a refusal, and
        /// the pre-existing cast would have thrown on it.
        /// </summary>
        [Test]
        public void AWholeNumberRadiantValue_IsRead()
        {
            AnalyticalModel analyticalModel = Model_CompanionTSD();

            //Written as an in-process int, which is the backing that used to throw. 30 rather than the fixture's
            //own 20, so the assertion below cannot pass if the replacement never landed.
            Corrupt(analyticalModel, StableKey("Flat 2"), 0, null, JsonValue.Create(30));

            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(analyticalModel, Results());

            Assert.That(approximateResultantTemperatureMap.Refusals, Is.Empty);
            Assert.That(approximateResultantTemperatureMap.Prepared, Has.Count.EqualTo(4));

            //Zone temperature is a flat 24 and the replaced radiant hour is 30, so the synthesised value is 27 -
            //and 22 would mean the int never reached the reader at all.
            Assert.That(First(approximateResultantTemperatureMap, StableKey("Flat 2")), Is.EqualTo(27.0).Within(1e-9));
        }

        /// <summary>A space with no TPD result is reported, not silently dropped.</summary>
        [Test]
        public void ASpaceWithNoTPDResult_IsRefusedAndReported()
        {
            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(Model_CompanionTSD(), [Result("tas-zone-0", "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 24.0)]);

            Assert.That(approximateResultantTemperatureMap.Prepared, Has.Count.EqualTo(1));
            Assert.That(approximateResultantTemperatureMap.Refusals, Has.Count.EqualTo(3));
        }

        /// <summary>
        /// <b>A zone series longer than the radiant series is refused, not truncated.</b>
        /// <para>
        /// The two series describe one period hour-for-hour. A longer zone series previously passed the
        /// one-sided count check and silently discarded its extra hours, producing a plausible-looking
        /// assessment from inputs that do not represent the same year.
        /// </para>
        /// </summary>
        [Test]
        public void AZoneSeriesLongerThanTheRadiantSeries_IsRefusedRatherThanTruncated()
        {
            //Five zone hours against the fixture's four radiant hours.
            IndexedDoubles series = new IndexedDoubles([24.0, 24.0, 24.0, 24.0, 24.0]);

            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(Model_CompanionTSD(), [Result("tas-zone-0", "Bedroom 2", new Dictionary<Analytical.Systems.SpaceDataType, IndexedDoubles> { { Analytical.Systems.SpaceDataType.ZoneTemperature, series } })]);

            Assert.That(approximateResultantTemperatureMap.Prepared, Is.Empty);
            Assert.That(approximateResultantTemperatureMap.Refusals.Find(x => x.Contains("cannot be combined")), Is.Not.Null);
        }

        /// <summary>
        /// <b>A gapped zone series is refused, not zero-filled.</b>
        /// <para>
        /// A series whose minimum is 0 and whose count matches the radiant length can still be missing keys -
        /// {0,2,3,4} has a hole at hour 1. The bounded read would fill the hole with 0 degrees and average it
        /// into a plausible-looking resultant temperature. The maximum-index check catches exactly that
        /// shape: with the count equal and the minimum fixed at 0, a maximum below Count - 1 is a gap.
        /// </para>
        /// </summary>
        [Test]
        public void AGappedZoneSeries_IsRefusedRatherThanZeroFilled()
        {
            IndexedDoubles series = new IndexedDoubles();
            series.Add(0, 24.0);
            series.Add(2, 24.0);
            series.Add(3, 24.0);
            series.Add(4, 24.0);

            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(Model_CompanionTSD(), [Result("tas-zone-0", "Bedroom 2", new Dictionary<Analytical.Systems.SpaceDataType, IndexedDoubles> { { Analytical.Systems.SpaceDataType.ZoneTemperature, series } })]);

            Assert.That(approximateResultantTemperatureMap.Prepared, Is.Empty);
            Assert.That(approximateResultantTemperatureMap.Refusals.Find(x => x.Contains("cannot be aligned")), Is.Not.Null);
        }

        // =============================================================================================
        // The boundary itself
        // =============================================================================================

        /// <summary>
        /// <b>TSD-simple never enters the two-pass route.</b>
        /// <para>
        /// Behavioural rather than a code scan: a TSD-simple model carries a real <c>ResultantTemperature</c> and
        /// nothing else - no TPD, no system results, no companion file - and it assesses. So it consumes nothing
        /// the TPD-full route produces, and cannot have run it. The expensive preparation is reached only by
        /// callers that have a TPD.
        /// </para>
        /// </summary>
        [Test]
        public void TheTSDSimpleRoute_AssessesWithoutTheTwoPassPreparation()
        {
            AnalyticalModel analyticalModel_TSD = Model_TSD();

            Analytical.TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel_TSD.TM59AssessmentCalculator(Model_Design());
            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            TM59AssessmentResult tM59AssessmentResult = tM59AssessmentCalculator.Calculate(tM59AssessmentCalculator.Spaces(null, null));

            Assert.That(Count(tM59AssessmentResult), Is.EqualTo(4));

            //The load-bearing part: this model carries a ResultantTemperature and NO system results at all, so it
            //consumed nothing the two-pass route produces. Feeding the same model to the approximate preparation
            //proves the point from the other side - with no TPD results there is nothing to synthesise from, and
            //it refuses every space, while the TSD-simple route above assessed all four.
            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(Model_TSD(), null);

            Assert.That(approximateResultantTemperatureMap.Prepared, Is.Empty);
            Assert.That(approximateResultantTemperatureMap.Refusals, Is.Not.Empty);
        }

        /// <summary>
        /// <b>The two identities are NOT assumed to be the same string.</b>
        /// <para>
        /// A space's identity is the TBD/TSD zone guid; a TPD result's <c>Reference</c> is the TPD component's own
        /// guid, and nothing establishes that they are equal - see the remarks on
        /// <c>ApproximateResultantTemperatureMap</c>. The other tests here give both sides the same string, which
        /// is convenient but encodes exactly the assumption under doubt. This one gives them <b>different</b>
        /// strings, which is the real-model shape if the guids differ, and pins what then happens: the whole model
        /// falls to name mode, and it says so ONCE rather than leaving the reader to infer it from four
        /// per-room messages.
        /// </para>
        /// </summary>
        [Test]
        public void WhereTheTwoIdentityNamespacesDisagree_ItSaysSoOnceAndFallsToNames()
        {
            //Uniquely named spaces, so name mode can still resolve them and the test is about the DIAGNOSIS.
            AnalyticalModel analyticalModel = Model(Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.MeanRadiantTemperature), [20.0, 24.0, 28.0, 30.0], unique: true);

            List<SystemSpaceResult> systemSpaceResults = [];
            foreach (string name in new[] { "Flat 1", "Flat 2", "Flat 3", "Corridor" })
            {
                //A TPD-component-shaped reference that matches no space identity.
                systemSpaceResults.Add(Result("tpd-component-" + name, name, Analytical.Systems.SpaceDataType.ZoneTemperature, 24.0));
            }

            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(analyticalModel, systemSpaceResults);

            //Resolved by name, so results were still produced...
            Assert.That(approximateResultantTemperatureMap.Prepared, Has.Count.EqualTo(4));

            //...but the fall back to names is reported exactly once, naming the cause.
            List<string> refusals = approximateResultantTemperatureMap.Refusals;

            Assert.That(refusals, Has.Count.EqualTo(1));
            Assert.That(refusals[0], Does.Contain("NAME"));
        }

        /// <summary>
        /// <b>The two routes prepare distinguishably, and the difference is visible in the series itself.</b>
        /// <para>
        /// TPD-full carries the first pass's zone temperature through <b>unchanged</b> and lets the second TBD run
        /// produce a real resultant temperature. TPD-approx never runs a second pass and hands over an
        /// <b>average</b>. Given the same inputs the two produce different numbers, which is why one may never
        /// silently stand in for the other.
        /// </para>
        /// </summary>
        [Test]
        public void TheTwoTPDRoutes_PrepareDistinguishably()
        {
            List<SystemSpaceResult> systemSpaceResults = [Result("tas-zone-0", "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 24.0)];

            //TPD-full: the payload is the first pass's series, untouched.
            ResultantTemperaturePreparation resultantTemperaturePreparation = new ResultantTemperaturePreparation(Path.Combine("C:", "models", "block.tpd"));

            Assert.That(resultantTemperaturePreparation.Transferred(systemSpaceResults)["Bedroom 2"].GetValue(0), Is.EqualTo(24.0).Within(1e-9));

            //TPD-approx: the radiant series is averaged in, so hour 0 is (20 + 24) / 2.
            ApproximateResultantTemperatureMap approximateResultantTemperatureMap = Approximate(Model_CompanionTSD(), systemSpaceResults);

            Assert.That(First(approximateResultantTemperatureMap, "tas-zone-0"), Is.EqualTo(22.0).Within(1e-9));

            //So the same first-pass input produces demonstrably different series on the two routes - which is why
            //one may never silently stand in for the other.
            Assert.That(First(approximateResultantTemperatureMap, "tas-zone-0"), Is.Not.EqualTo(resultantTemperaturePreparation.Transferred(systemSpaceResults)["Bedroom 2"].GetValue(0)));

            //And only the two-pass route owns simulation files: the approximate route names no second TBD or TSD
            //anywhere, because it never runs one.
            Assert.That(resultantTemperaturePreparation.Path_TSD_Simulation, Does.Contain(ResultantTemperaturePreparation.Suffix));
            Assert.That(typeof(ApproximateResultantTemperatureMap).GetProperties().Any(x => x.Name.Contains("Path")), Is.False, "The approximate route has acquired simulation file paths, so it is no longer distinguishable from the two-pass route.");
        }

        /// <summary>
        /// <b>Both routes converge on the identical engine-neutral assessment.</b>
        /// <para>
        /// Given the same effective resultant temperatures, a TSD-simple model and an approximate-route model
        /// produce the same TM59 answer - so the calculator is indifferent to which side prepared it, which is the
        /// whole of "preparation differs; assessment does not".
        /// </para>
        /// </summary>
        [Test]
        public void BothRoutes_ConvergeOnTheSameAssessment()
        {
            //Radiant 20/24/28/30 averaged against a flat 24 gives 22/24/26/27.
            AnalyticalModel analyticalModel_Approximate = Approximate(Model_CompanionTSD(), Results()).AnalyticalModel;

            //The TSD-simple model is handed those same values directly.
            AnalyticalModel analyticalModel_TSD = Model_TSD([22.0, 24.0, 26.0, 27.0]);

            TM59AssessmentResult tM59AssessmentResult_Approximate = Assess(analyticalModel_Approximate);
            TM59AssessmentResult tM59AssessmentResult_TSD = Assess(analyticalModel_TSD);

            Assert.That(Count(tM59AssessmentResult_Approximate), Is.EqualTo(4));
            Assert.That(Count(tM59AssessmentResult_Approximate), Is.EqualTo(Count(tM59AssessmentResult_TSD)));

            //Comparing the bucket COUNTS alone would prove nothing: which criterion a room is assessed against
            //comes from the shared design model, so the counts match however wrong the temperatures are. The
            //assessment's own verdict per room is what has to agree.
            Assert.That(Verdicts(tM59AssessmentResult_Approximate), Is.EqualTo(Verdicts(tM59AssessmentResult_TSD)));

            //And the comparison is only meaningful if the two models really did arrive by different routes: one
            //synthesised its series, the other was handed one.
            Assert.That(Series(Simulated(analyticalModel_Approximate, "tas-zone-0"), Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.ResultantTemperature)),
                Is.EqualTo(Series(Simulated(analyticalModel_TSD, "tas-zone-0"), Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.ResultantTemperature))).Within(1e-9));
        }

        /// <summary>
        /// <b>Step 7 survives the move: the scenario is still authoritative over ventilation strategy on the
        /// approximate route.</b> The design data says the opposite in every case, and the scenario wins.
        /// </summary>
        [Test]
        public void OnTheApproximateRoute_TheScenarioStillDecidesTheCriterion()
        {
            AnalyticalModel analyticalModel_Design = Model_Design();
            AnalyticalModel analyticalModel_Approximate = Approximate(Model_CompanionTSD(), Results()).AnalyticalModel;

            SimulationSpaceMap simulationSpaceMap = analyticalModel_Design.SimulationSpaceMap(analyticalModel_Approximate);

            OverheatingScenarioMap overheatingScenarioMap = new OverheatingScenarioMap(Scenarios(analyticalModel_Design), analyticalModel_Design, simulationSpaceMap);

            Assert.That(overheatingScenarioMap.IsComplete, Is.True);

            Analytical.TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel_Approximate.TM59AssessmentCalculator(analyticalModel_Design, simulationSpaceMap);
            tM59AssessmentCalculator.VentilationStrategyMap = overheatingScenarioMap.VentilationStrategyMap;
            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            TM59AssessmentResult tM59AssessmentResult = tM59AssessmentCalculator.Calculate(tM59AssessmentCalculator.Spaces(null, null));

            Assert.That(tM59AssessmentResult.VentilationStrategyRefusals, Is.Empty);

            Assert.That(tM59AssessmentResult.MechanicalVentilationResults.Count, Is.EqualTo(2), "Flat 1 and Flat 3 state MVRE.");
            Assert.That(tM59AssessmentResult.NaturalVentilationResults.Count, Is.EqualTo(1), "Flat 2 states NV.");
            Assert.That(tM59AssessmentResult.CorridorResults.Count, Is.EqualTo(1), "The corridor states UV.");
        }

        // =============================================================================================
        // Fixture
        // =============================================================================================

        /// <summary>
        /// A real directory holding a TPD and its companion TBD, so the "writes nothing on refusal" invariants can
        /// be asserted against the file system rather than inferred.
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            private const string content_TBD = "design-tbd-contents";

            private readonly string directory;

            public Fixture()
            {
                directory = Path.Combine(Path.GetTempPath(), "SAM-PartO-Step9-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);

                Path_TPD = Path.Combine(directory, "block.tpd");

                File.WriteAllText(Path_TPD, "simulated-tpd-contents");
                File.WriteAllText(Path.Combine(directory, "block.tbd"), content_TBD);
            }

            public string Path_TPD { get; }

            /// <summary>Whether the design TBD is byte-for-byte what it was written as.</summary>
            public bool DesignTBDUnchanged => File.ReadAllText(Path.Combine(directory, "block.tbd")) == content_TBD;

            public void Dispose()
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private static ApproximateResultantTemperatureMap Approximate(AnalyticalModel analyticalModel_Simulation, IEnumerable<SystemSpaceResult> systemSpaceResults)
        {
            //The two series keys and the identity are TAS's vocabulary, supplied by the caller exactly as
            //Create.TM59AssessmentCalculator supplies the calculator's - so SAM.Analytical.Tas.TPD stays free of
            //them and the preparation stays testable.
            return new ApproximateResultantTemperatureMap(
                analyticalModel_Simulation,
                systemSpaceResults,
                Analytical.Tas.Query.SimulationSpaceKey,
                Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.MeanRadiantTemperature),
                Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.ResultantTemperature));
        }

        private static TM59AssessmentResult Assess(AnalyticalModel analyticalModel)
        {
            Analytical.TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel.TM59AssessmentCalculator(Model_Design());
            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            return tM59AssessmentCalculator.Calculate(tM59AssessmentCalculator.Spaces(null, null));
        }

        private static int Count(TM59AssessmentResult tM59AssessmentResult)
        {
            return tM59AssessmentResult == null ? 0 : tM59AssessmentResult.MechanicalVentilationResults.Count + tM59AssessmentResult.NaturalVentilationResults.Count + tM59AssessmentResult.CorridorResults.Count;
        }

        /// <summary>
        /// The assessment's actual answer per room - criterion, pass/fail and the hours behind it - sorted so two
        /// runs can be compared without depending on the order results came back in.
        /// <para>
        /// Comparing bucket counts is not comparing assessments: the criterion each room is assessed against comes
        /// from the design model, so counts agree however wrong the temperatures were.
        /// </para>
        /// </summary>
        private static List<string> Verdicts(TM59AssessmentResult tM59AssessmentResult)
        {
            List<string> result = [];

            foreach (TMResult tMResult in Enumerable.Concat(
                Enumerable.Concat(tM59AssessmentResult.MechanicalVentilationResults, tM59AssessmentResult.NaturalVentilationResults),
                tM59AssessmentResult.CorridorResults))
            {
                result.Add(string.Format("{0}|{1}|{2}|{3}|{4}", tMResult.GetType().Name, tMResult.Name, tMResult.Pass, tMResult.OccupiedHours, tMResult.MaxExceedableHours));
            }

            result.Sort(System.StringComparer.Ordinal);

            return result;
        }

        private static double First(ApproximateResultantTemperatureMap approximateResultantTemperatureMap, string key)
        {
            return Series(Simulated(approximateResultantTemperatureMap.AnalyticalModel, key), Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.ResultantTemperature))[0];
        }

        private static Space Simulated(AnalyticalModel analyticalModel, string key)
        {
            return analyticalModel.GetSpaces().Find(x => Analytical.Tas.Query.SimulationSpaceKey(x) == key);
        }

        private static double[] Series(Space space, string name)
        {
            JsonArray jsonArray = space.GetParameterSets().Find(x => x.Contains(name)).ToObject(name) as JsonArray;

            double[] result = new double[jsonArray.Count];
            for (int i = 0; i < jsonArray.Count; i++)
            {
                result[i] = (double)jsonArray[i];
            }

            return result;
        }

        private static List<SystemSpaceResult> Results()
        {
            //One result per space, all at a flat 24 degrees, keyed by the identity the TPD carries.
            List<SystemSpaceResult> result = [];

            foreach (string name in new[] { "Flat 1", "Flat 2", "Flat 3", "Corridor" })
            {
                result.Add(Result(StableKey(name), name == "Corridor" ? "Corridor" : "Bedroom 2", Analytical.Systems.SpaceDataType.ZoneTemperature, 24.0));
            }

            return result;
        }

        private static SystemSpaceResult Result(string reference, string name, Analytical.Systems.SpaceDataType spaceDataType, double value)
        {
            return Result(reference, name, new Dictionary<Analytical.Systems.SpaceDataType, IndexedDoubles> { { spaceDataType, Series(value) } });
        }

        private static SystemSpaceResult Result(string reference, string name, Dictionary<Analytical.Systems.SpaceDataType, IndexedDoubles> dictionary)
        {
            return new SystemSpaceResult(reference, name, "SAM.Analytical.Tas.TPD.dll", 10, 25, dictionary);
        }

        /// <summary>A four-hour series, matching the four-hour radiant series the fixture models carry.</summary>
        private static IndexedDoubles Series(double value)
        {
            return new IndexedDoubles([value, value, value, value]);
        }

        private static string StableKey(string name)
        {
            int index = Array.IndexOf(flats, name);

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

        /// <summary>Three flats each with a room called exactly "Bedroom 2", plus a corridor.</summary>
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
                space.SetValue(Analytical.Tas.SpaceParameter.ZoneGuid, StableKey(name));

                Analytical.Zone zone = new Analytical.Zone(name);

                adjacencyCluster.AddObject(space);
                adjacencyCluster.AddObject(zone);
                adjacencyCluster.AddRelation(zone, space);
            }

            return new AnalyticalModel("Three Flats", null, null, null, adjacencyCluster);
        }

        /// <summary>
        /// The companion TSD beside a TPD, as the approximate route reads it: a <b>mean radiant temperature</b>
        /// series and no resultant temperature at all - which is the whole reason that route synthesises one.
        /// </summary>
        private static AnalyticalModel Model_CompanionTSD()
        {
            return Model(Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.MeanRadiantTemperature), [20.0, 24.0, 28.0, 30.0]);
        }

        /// <summary>The TSD-simple model: a real resultant temperature series, straight from one TAS run.</summary>
        private static AnalyticalModel Model_TSD(double[] values = null)
        {
            return Model(Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.ResultantTemperature), values ?? [21.0, 24.5, 27.5, 29.0]);
        }

        private static AnalyticalModel Model(string name_Series, double[] values, bool unique = false)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            foreach (string name in new[] { "Flat 1", "Flat 2", "Flat 3", "Corridor" })
            {
                //unique: the duplicate-name hazard is deliberately absent, so a test can be about something else.
                Space space = new Space(unique ? name : (name == "Corridor" ? "Corridor" : "Bedroom 2"));

                space.SetValue(Analytical.Tas.SpaceParameter.ZoneGuid, StableKey(name));

                ParameterSet parameterSet = new ParameterSet("SAM.Analytical.Tas.dll");

                parameterSet.Add(name_Series, Values(values));
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

        /// <summary>
        /// Replaces one hour of one space's mean radiant temperature series in place, so the other spaces keep
        /// the series the fixture gave them and a refusal can be attributed to this space alone.
        /// </summary>
        /// <param name="json_Value">
        /// The replacement as JSON text - <c>"warm"</c>, <c>true</c>, <c>null</c>. Parsed, so the node is
        /// <c>JsonElement</c>-backed exactly as a stored parameter set's would be.
        /// </param>
        /// <param name="jsonNode">
        /// An already-built node instead, for the in-process backings that parsing cannot produce.
        /// </param>
        private static void Corrupt(AnalyticalModel analyticalModel, string key, int index, string json_Value, JsonNode jsonNode = null)
        {
            string name_Series = Analytical.Tas.Query.Text(Analytical.Tas.SpaceDataType.MeanRadiantTemperature);

            Space space = Simulated(analyticalModel, key);

            ParameterSet parameterSet = space.GetParameterSets().Find(x => x.Contains(name_Series));
            JsonArray jsonArray = parameterSet.ToObject(name_Series) as JsonArray;

            //The premise: there really is an hour there to replace, so this cannot pass by corrupting nothing.
            Assert.That(jsonArray, Is.Not.Null);
            Assert.That(jsonArray.Count, Is.GreaterThan(index));

            jsonArray[index] = jsonNode ?? (json_Value == "null" ? null : JsonNode.Parse(json_Value));

            //Written back at every level, because each of these may hand out a clone rather than the live object -
            //GetParameterSets certainly does, which the preparation itself relies on.
            parameterSet.Add(name_Series, jsonArray);
            space.Add(parameterSet);
            analyticalModel.AdjacencyCluster.AddObject(space);
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
