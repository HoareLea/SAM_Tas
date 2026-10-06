// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Tas;
using SAM.Core;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <c>Create.TM59AssessmentCalculator</c> - the three values that are TAS's and not the assessment's.
    /// <para>
    /// <b>Why this is worth a test.</b> The <c>Tas.TSDQueryTM59Results</c> Grasshopper component used to
    /// state the whole TM59 recipe inline and got these three values from
    /// <c>OverheatingCalculator</c>'s constructor. Repointing it at the extracted
    /// <c>SAM.Analytical.TM59AssessmentCalculator</c> - which knows nothing about TAS and defaults to the
    /// analytical vocabulary - would silently have changed all three: the assessment would have read a series
    /// key TAS never wrote (producing <i>no assessment at all</i>, silently) and stamped a different
    /// assembly's name as provenance. The factory is where TAS's vocabulary stays, and this asserts it is
    /// still there.
    /// </para>
    /// <para>
    /// No TAS COM: the factory touches no interop type, and nothing here converts a TSD.
    /// </para>
    /// </summary>
    [TestFixture]
    public class TM59AssessmentCalculatorFactoryTests
    {
        /// <summary>
        /// <b>The series key that matters.</b> The TSD conversion writes "Occupant Sensible Gain"; the
        /// analytical vocabulary says "Occupancy Sensible Gain". Reading the wrong one is not an error - the
        /// space simply produces no assessment - so the wrong default is invisible until a real assessment
        /// comes back empty.
        /// </summary>
        [Test]
        public void TheFactory_SuppliesTheSeriesKeysTasActuallyWrites()
        {
            TM59AssessmentCalculator tM59AssessmentCalculator = Model().TM59AssessmentCalculator(Model());

            Assert.That(tM59AssessmentCalculator.OccupancySensibleGainSeriesKey, Is.EqualTo(SpaceDataType.OccupantSensibleGain.Text()));
            Assert.That(tM59AssessmentCalculator.ResultantTemperatureSeriesKey, Is.EqualTo(SpaceDataType.ResultantTemperature.Text()));

            //Not vacuous: the analytical default really is a different string for the gain series, which is
            //the whole reason the factory exists. Both names are qualified because SAM.Analytical.Tas has its
            //own SpaceSimulationResultParameter that shadows SAM.Analytical's from inside this namespace.
            Assert.That(tM59AssessmentCalculator.OccupancySensibleGainSeriesKey, Is.Not.EqualTo(Core.Query.Name(Analytical.SpaceSimulationResultParameter.OccupancySensibleGain)));
        }

        /// <summary>
        /// Provenance: a result off an unnamed model reports this assembly, exactly as
        /// <c>OverheatingCalculator</c> has always made it report. Provenance only - it names no object and
        /// takes no part in any scenario, criterion or result identity.
        /// </summary>
        [Test]
        public void TheFactory_KeepsTheProvenanceTheTasWrapperAlwaysStamped()
        {
            //Tas.Query, qualified: SAM.Analytical.Tas.TM59 has its own Query that shadows it here.
            Assert.That(Model().TM59AssessmentCalculator(Model()).SourceFallback, Is.EqualTo(Tas.Query.Source()));

            //And it is the wrapper's, not the analytical assembly's - the value that would have been lost.
            Assert.That(Tas.Query.Source(), Is.EqualTo("SAM.Analytical.Tas"));
        }

        /// <summary>
        /// The factory configures and nothing else: the model it was asked about is the model it assesses,
        /// and the TM52 category is left at the analytical default for the caller to state.
        /// </summary>
        [Test]
        public void TheFactory_ConfiguresAndDecidesNothing()
        {
            AnalyticalModel analyticalModel = Model();

            TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel.TM59AssessmentCalculator(analyticalModel);

            Assert.That(tM59AssessmentCalculator.AnalyticalModel, Is.SameAs(analyticalModel));
            Assert.That(tM59AssessmentCalculator.TM52BuildingCategory, Is.EqualTo(TM52BuildingCategory.CategoryII));

            //No VentilationStrategyMap: the factory does not invent a scenario, so a caller that states none
            //keeps the old criterion derivation rather than having every space refused.
            Assert.That(tM59AssessmentCalculator.VentilationStrategyMap, Is.Null);

            //But it DOES build a SimulationSpaceMap, because identity is not optional - step 8. It reads the zone
            //guid TAS preserves across the round trip, so this route never matches a space by name.
            Assert.That(tM59AssessmentCalculator.SimulationSpaceMap, Is.Not.Null);
            Assert.That(tM59AssessmentCalculator.AnalyticalModel_Design, Is.SameAs(analyticalModel));

            //A null model is configured just the same rather than throwing - the component reaches here
            //before it can know whether the TSD read produced anything.
            Assert.That(((AnalyticalModel)null).TM59AssessmentCalculator(null), Is.Not.Null);
        }

        private static AnalyticalModel Model()
        {
            return new AnalyticalModel("Three Flats", null, null, null, new AdjacencyCluster());
        }
    }
}
