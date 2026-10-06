// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        /// <summary>
        /// A <see cref="Analytical.TM59AssessmentCalculator"/> set up to read a model that TAS wrote.
        /// <para>
        /// <b>Three values, and all three are TAS's.</b> The assessment recipe itself is engine-free and lives
        /// in <c>SAM.Analytical</c>; what is not engine-free is the vocabulary the TSD conversion writes -
        /// notably "Occupant Sensible Gain", where the analytical vocabulary says "Occupancy Sensible Gain" -
        /// and the provenance a result carries when the model has no name. Reading the wrong series key is
        /// silent: the space simply produces no assessment.
        /// </para>
        /// <para>
        /// <b>Why a factory rather than three assignments at the call site.</b> These are exactly the values
        /// <see cref="OverheatingCalculator"/> already supplies, and a caller that restated them would be a
        /// second place to keep them right - which is the drift the recipe was extracted to stop. A caller
        /// asks for a calculator; TAS's vocabulary stays in TAS's assembly.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">
        /// The model read back from a TAS simulation - fresh spaces carrying hourly series, not the design
        /// model.
        /// </param>
        /// <param name="analyticalModel_Design">
        /// The design model, which holds the internal conditions and the zone-to-space relations.
        /// </param>
        /// <param name="simulationSpaceMap">
        /// How a simulated space is known to be a given design space. Null builds one with
        /// <see cref="SimulationSpaceMap(AnalyticalModel, AnalyticalModel)"/>, which matches on the zone guid TAS
        /// stamps across the round trip - the right default for a TAS caller, and the reason this factory exists.
        /// </param>
        public static Analytical.TM59AssessmentCalculator TM59AssessmentCalculator(this AnalyticalModel analyticalModel, AnalyticalModel analyticalModel_Design, Analytical.SimulationSpaceMap simulationSpaceMap = null)
        {
            return new Analytical.TM59AssessmentCalculator(analyticalModel, analyticalModel_Design, simulationSpaceMap ?? SimulationSpaceMap(analyticalModel_Design, analyticalModel))
            {
                //The keys TAS wrote, which are not what the analytical vocabulary would have called them.
                ResultantTemperatureSeriesKey = SpaceDataType.ResultantTemperature.Text(),
                OccupancySensibleGainSeriesKey = SpaceDataType.OccupantSensibleGain.Text(),

                //Provenance only, and the same fallback OverheatingCalculator has always stamped.
                SourceFallback = Query.Source(),
            };
        }
    }
}
