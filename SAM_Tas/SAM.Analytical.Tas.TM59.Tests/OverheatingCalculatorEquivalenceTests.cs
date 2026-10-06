// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Weather;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Proof that extracting the TM52/TM59 calculation into <c>SAM.Analytical.TMOverheatingCalculator</c>
    /// changed no engineering behaviour.
    /// <para>
    /// Narrow on purpose: given identical model data, the compatibility wrapper
    /// <c>SAM.Analytical.Tas.OverheatingCalculator</c> must produce the same results as the pure calculator
    /// configured with the TAS legacy series keys. <b>Nothing here tests TSD conversion</b> - the converter
    /// is a separate concern and is not exercised, which is also why these tests need no TAS COM.
    /// </para>
    /// <para>
    /// Spaces are built the way the real converter builds them: <c>ParameterSet.Add(key, JsonArray)</c>
    /// followed by <c>Space.Add(parameterSet)</c>, storing the TAS legacy keys.
    /// </para>
    /// </summary>
    [TestFixture]
    public class OverheatingCalculatorEquivalenceTests
    {
        private const string key_ResultantTemperature = "Resultant Temperature";

        //TAS's spelling: "Occupant", where the analytical vocabulary says "Occupancy".
        private const string key_OccupantSensibleGain = "Occupant Sensible Gain";

        /// <summary>
        /// The three TM59 criteria the assessment can select, produced by one model, compared result by
        /// result. Type, reference, occupied hours, comfort thresholds and operative temperatures must all
        /// agree; provenance is checked separately below.
        /// </summary>
        [Test]
        public void Wrapper_AndPureCalculator_AgreeOnMechanicalNaturalAndCorridor()
        {
            AnalyticalModel analyticalModel = Model();

            List<TM59ExtendedResult> results_Wrapper = new OverheatingCalculator(analyticalModel) { TextMap = TextMap() }
                .Calculate_TM59(analyticalModel.GetSpaces());

            List<TM59ExtendedResult> results_Pure = PureCalculator(analyticalModel).Calculate_TM59(analyticalModel.GetSpaces());

            Assert.That(results_Wrapper, Is.Not.Null);
            Assert.That(results_Pure, Is.Not.Null);
            Assert.That(results_Wrapper.Count, Is.EqualTo(results_Pure.Count), "Different number of results.");

            //All three criteria really were exercised - otherwise this would pass on an empty set.
            Assert.That(results_Wrapper.Exists(x => x is TM59MechanicalVentilationExtendedResult), Is.True, "No mechanical result.");
            Assert.That(results_Wrapper.Exists(x => x is TM59NaturalVentilationExtendedResult), Is.True, "No natural result.");
            Assert.That(results_Wrapper.Exists(x => x is TM59CorridorExtendedResult), Is.True, "No corridor result.");

            for (int i = 0; i < results_Wrapper.Count; i++)
            {
                TM59ExtendedResult result_Wrapper = results_Wrapper[i];
                TM59ExtendedResult result_Pure = results_Pure[i];

                Assert.That(result_Pure.GetType(), Is.EqualTo(result_Wrapper.GetType()), "Criterion differs.");
                Assert.That(result_Pure.Name, Is.EqualTo(result_Wrapper.Name));
                Assert.That(result_Pure.Reference, Is.EqualTo(result_Wrapper.Reference), "Reference differs.");

                Assert.That(Values(result_Pure.OperativeTemperatures), Is.EqualTo(Values(result_Wrapper.OperativeTemperatures)), "Operative temperatures differ.");
                Assert.That(Values(result_Pure.MaxAcceptableTemperatures), Is.EqualTo(Values(result_Wrapper.MaxAcceptableTemperatures)), "Upper comfort thresholds differ.");
                Assert.That(Values(result_Pure.MinAcceptableTemperatures), Is.EqualTo(Values(result_Wrapper.MinAcceptableTemperatures)), "Lower comfort thresholds differ.");

                Assert.That(result_Pure.OccupiedHourIndices, Is.EquivalentTo(result_Wrapper.OccupiedHourIndices), "Occupied hours differ.");
                Assert.That(result_Pure.TM52BuildingCategory, Is.EqualTo(result_Wrapper.TM52BuildingCategory));
            }
        }

        /// <summary>
        /// Provenance is compared on its own, because it is not engineering content: it names no object and
        /// takes no part in identity or ownership. Both sides report the model name, which is what the
        /// wrapper reported before the extraction.
        /// </summary>
        [Test]
        public void Provenance_IsPreservedAndIsNotEngineeringContent()
        {
            AnalyticalModel analyticalModel = Model();

            OverheatingCalculator overheatingCalculator = new(analyticalModel) { TextMap = TextMap() };

            Assert.That(overheatingCalculator.Source, Is.EqualTo(analyticalModel.Name));

            foreach (TM59ExtendedResult tM59ExtendedResult in overheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()))
            {
                Assert.That(tM59ExtendedResult.Source, Is.EqualTo(analyticalModel.Name));
            }
        }

        /// <summary>
        /// The wrapper reads TAS's series keys and the analytical default does not - the difference the
        /// wrapper exists to absorb. Without this, equivalence above could pass with both sides reading
        /// nothing.
        /// </summary>
        [Test]
        public void Wrapper_ReadsTasSeriesKeysThatTheAnalyticalDefaultCannot()
        {
            AnalyticalModel analyticalModel = Model();

            Assert.That(new OverheatingCalculator(analyticalModel) { TextMap = TextMap() }.Calculate_TM59(analyticalModel.GetSpaces()), Is.Not.Empty);

            //Same model, analytical default series keys: TAS's "Occupant Sensible Gain" is invisible.
            Assert.That(new TMOverheatingCalculator(analyticalModel) { TextMap = TextMap() }.Calculate_TM59(analyticalModel.GetSpaces()), Is.Empty);
        }

        // ------------------------------------------------------------------
        // Fixture
        // ------------------------------------------------------------------

        private static TMOverheatingCalculator PureCalculator(AnalyticalModel analyticalModel)
        {
            return new TMOverheatingCalculator(analyticalModel)
            {
                TextMap = TextMap(),
                ResultantTemperatureSeriesKey = key_ResultantTemperature,
                OccupancySensibleGainSeriesKey = key_OccupantSensibleGain,
            };
        }

        /// <summary>
        /// The smallest TextMap that lets TM59 space applications resolve: a bedroom sleeps, a living
        /// kitchen lives. A corridor appears in neither, which is what makes it a corridor result.
        /// </summary>
        private static TextMap TextMap()
        {
            TextMap result = Core.Create.TextMap("TM59");

            result.Add("Sleeping", "Bedroom");
            result.Add("Living", "Living Kitchen");

            return result;
        }

        /// <summary>
        /// Three spaces, one per criterion: a mechanically ventilated bedroom, a naturally ventilated
        /// living kitchen, and a corridor with no TM59 application at all.
        /// </summary>
        private static AnalyticalModel Model()
        {
            AdjacencyCluster adjacencyCluster = new();

            adjacencyCluster.AddObject(Space("Bedroom 2_3", "Bedroom", "MVRE"));
            adjacencyCluster.AddObject(Space("Kitchen_4", "Living Kitchen", "NV"));
            adjacencyCluster.AddObject(Space("Corridor_1", "Corridor", "NV"));

            AnalyticalModel result = new("Three Flats", null, null, null, adjacencyCluster);

            //Qualified: unqualified AnalyticalModelParameter binds to SAM.Analytical.Tas's own enum here.
            result.SetValue(Analytical.AnalyticalModelParameter.WeatherData, new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear()));

            return result;
        }

        private static Space Space(string name, string name_InternalCondition, string name_VentilationSystemType)
        {
            InternalCondition internalCondition = new(name_InternalCondition);
            internalCondition.SetValue(InternalConditionParameter.VentilationSystemTypeName, name_VentilationSystemType);

            Space result = new(name) { InternalCondition = internalCondition };

            //Exactly how Analytical.Tas.Convert.ToSAM(TSD.ZoneData, ...) stores an hourly series.
            ParameterSet parameterSet = new("SAM.Analytical.Tas.dll");
            parameterSet.Add(key_ResultantTemperature, Series([21.0, 24.5, 27.5, 29.0]));
            parameterSet.Add(key_OccupantSensibleGain, Series([0, 80.0, 80.0, 0]));

            result.Add(parameterSet);

            return result;
        }

        /// <summary>A deterministic constant year - these tests are about the extraction, not the weather.</summary>
        private static WeatherYear WeatherYear()
        {
            WeatherYear result = new(2018);

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
            JsonArray result = [];

            foreach (double value in values)
            {
                result.Add(value);
            }

            return result;
        }

        private static List<double> Values(IndexedDoubles indexedDoubles)
        {
            return indexedDoubles?.Values == null ? [] : [.. indexedDoubles.Values];
        }
    }
}
