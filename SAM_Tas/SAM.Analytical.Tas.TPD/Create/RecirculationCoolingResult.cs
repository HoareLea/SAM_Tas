// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        /// <summary>
        /// How far a ventilation flow may depart from its design and still be the same flow [l/s]. Licensed
        /// evidence (SAM#111 PR5B, canonical Leeds TRY acceptance): the native solver holds it to within
        /// 0.0263 l/s over a full year, and the real-project acceptance measured 0.0023 / 0.0261 / 0.0299.
        /// <para>
        /// <b>This no longer sets the recirculation range boundary.</b> It once served that second purpose
        /// too, on the strength of the same canonical run - which held the range excursion to 0.013 l/s. The
        /// two quantities turned out not to move together: on a harsher weather file the ventilation
        /// deviation was unchanged while the range excursion grew about fourfold and exhausted this margin
        /// (SAM#111 real-project acceptance, 2026-09-15). The range boundary is now
        /// <see cref="RecirculationCoolingClamp_Lps"/>, which addresses that excursion's actual mechanism;
        /// this constant keeps only the ventilation-deviation duty it was measured for.
        /// </para>
        /// </summary>
        public const double RecirculationCoolingTolerance_Flow_Lps = 0.05;

        /// <summary>
        /// How far the coil outlet may stand from the published table and still be the table [K] - half the
        /// table's own 0.1 K resolution. Licensed evidence: 0 K.
        /// </summary>
        public const double RecirculationCoolingTolerance_Table_K = 0.05;

        /// <summary>The temperature difference [K] below which a coil is judged to have done nothing.</summary>
        public const double RecirculationCoolingTolerance_Idle_K = 1e-6;

        /// <summary>The departure [l/s] from the ideal law that is counted - and only counted - as off-law.</summary>
        public const double RecirculationCoolingOffLaw_Lps = 0.5;

        /// <summary>
        /// How far outside the law's range a flow may stand and still be reported AT the range rather than
        /// refused [l/s]. <b>The declared control cannot command a flow outside its own range</b> - the flow
        /// fraction is at most 1 - so a small excursion is the native solver's, not the design's, and
        /// reporting it as a commanded flow is the error.
        /// <para>
        /// <b>Where 0.1 comes from, and what would change it.</b> Licensed evidence, SAM#111 real-project
        /// acceptance 2026-09-15 (3 units x 8760 h): only 5 hours of 26 280 stood above the 120 l/s ceiling,
        /// every one of them the FIRST hour the control law saturates, and the excursion rose monotonically
        /// with the size of the approach jump - 1.3 / 13.6 / 20.5 / 27.2 / 43.9 l/s of approach gave
        /// 0.00014 / 0.00037 / 0.00059 / 0.0267 / 0.0503 l/s of overshoot. 0.1 is about twice the largest
        /// measured. It is a <b>measured and reviewable</b> bound, not a derived one: nothing structural
        /// bounds a controller overshoot, so a steeper model can use more of it. That is why every clamped
        /// hour is recorded on the result (<c>Count_Clamped</c>, <c>MaximumClampedExcursion_Lps</c>) instead
        /// of being swallowed - if a model ever approaches 0.1, this constant is the thing to revisit, with
        /// a fresh measurement, rather than to raise.
        /// </para>
        /// <para>
        /// Beyond this, the flow is left as TAS answered it and still refuses: a solver genuinely running
        /// the branch outside its envelope is never clamped into silence.
        /// </para>
        /// </summary>
        public const double RecirculationCoolingClamp_Lps = 0.1;

        /// <summary>Air rho.cp [J/(m3.K)] as TAS Systems uses it (measured, SAM#111 PR5A Phase 0).</summary>
        public const double RecirculationCoolingRhoCp = 1214.4;

        /// <summary>
        /// PR5B (SAM#111): reduces one branch's hourly native results to the checked evidence. COM-free - the
        /// native reading is <c>Modify.RecirculationCoolingResults</c>'s; this only judges the numbers.
        /// <para>
        /// <b>What is refused.</b> Any heating hour; any hour the coil cooled with its mixed return below the
        /// cooling-enable temperature; any recirculation airflow outside the law's range by more than
        /// <see cref="RecirculationCoolingClamp_Lps"/> (just outside is reported AT the range, and counted);
        /// a coil outlet off
        /// the published table held at its edges; any ventilation flow off its design; any missing or
        /// non-finite value. <b>What is only counted</b> is the native within-hour departure from the ideal
        /// law - measured, and not a property of the declared control.
        /// </para>
        /// </summary>
        /// <param name="canonicalDeviation_Lps">Each hour's largest departure of any ventilation flow of the same air system from its design, or null when not read.</param>
        public static RecirculationCoolingResult RecirculationCoolingResult(
            MechanicalVentilationRecirculationCooling mechanicalVentilationRecirculationCooling,
            int startHour,
            IList<double> outdoorTemperature_C,
            IList<double> mixedReturnTemperature_C,
            IList<double> operatingAirFlow_Lps,
            IList<double> supplyTemperature_C,
            IList<double> canonicalDeviation_Lps)
        {
            if (mechanicalVentilationRecirculationCooling == null)
            {
                return null;
            }

            MechanicalVentilationCoolingSettings settings = mechanicalVentilationRecirculationCooling.Settings;
            List<string> refusals = new List<string>();

            string label = string.Format("Recirculation cooling of air system {0}", mechanicalVentilationRecirculationCooling.Guid_AirSystem);

            int count = mixedReturnTemperature_C?.Count ?? 0;

            if (settings == null || settings.Refusal() != null)
            {
                refusals.Add(string.Format("{0} carries no valid cooling settings to judge it against.", label));
                count = 0;
            }
            else if (count == 0
                || outdoorTemperature_C == null || outdoorTemperature_C.Count != count
                || operatingAirFlow_Lps == null || operatingAirFlow_Lps.Count != count
                || supplyTemperature_C == null || supplyTemperature_C.Count != count
                || (canonicalDeviation_Lps != null && canonicalDeviation_Lps.Count != count))
            {
                refusals.Add(string.Format("{0}: TAS did not answer a complete hourly series for the coil inlet, outlet, airflow and the outdoor air.", label));
                count = 0;
            }

            double ceiling = settings?.MaximumOperatingAirFlow_Lps ?? double.NaN;
            double minimum = settings?.MinimumOperatingAirFlow_Lps ?? double.NaN;
            double gate = settings?.CoolingEnableTemperature_C ?? double.NaN;

            double[] q = new double[count];
            double[] tMix = new double[count];
            double[] tOut = new double[count];
            double[] odb = new double[count];

            int count_Cooling = 0, count_Heating = 0, count_BelowGate = 0, count_GateViolation = 0, count_OutOfRange = 0, count_Clamped = 0, count_OffLaw = 0, count_InDomain = 0, count_NonFinite = 0;
            double maximumClampedExcursion = 0;
            double maximumTableError = 0, maximumCanonicalDeviation = 0, cooling_Wh = 0;

            VentilationUnitPerformanceTable table = settings?.SupplyAirTemperatureTable;
            int[] axisIndexes = new int[MechanicalVentilationCoolingSettings.AxisNames.Count];
            for (int i = 0; count != 0 && i < axisIndexes.Length; i++)
            {
                axisIndexes[i] = table.AxisIndex(MechanicalVentilationCoolingSettings.AxisNames[i]);
            }

            for (int h = 0; h < count; h++)
            {
                q[h] = operatingAirFlow_Lps[h];
                tMix[h] = mixedReturnTemperature_C[h];
                tOut[h] = supplyTemperature_C[h];
                odb[h] = outdoorTemperature_C[h];

                if (!Finite(q[h]) || !Finite(tMix[h]) || !Finite(tOut[h]) || !Finite(odb[h]))
                {
                    count_NonFinite++;
                    continue;
                }

                //Just outside the range is reported AT the range - see RecirculationCoolingClamp_Lps.
                //
                //Two flows from here on, and the split matters. q[h] is the flow the declared control could
                //have commanded: it is what this result REPORTS and what the hour's duty and range are
                //judged on. qMeasured is what TAS actually answered, and it is what the checks of TAS's own
                //fidelity use - the published-table comparison and the domain and off-law observations -
                //because those ask "did TAS follow its table at the flow it used?", which is a question
                //about TAS, not about the design. Judging the table at a flow TAS did not use could refuse
                //a coil that followed the table exactly, wherever the ceiling sits below the table's
                //airflow axis. Production always derives the ceiling FROM that axis
                //(SAM_UI Query.PartOIteration3CoolingResolution: ceiling_Lps = axis_AirFlow.Maximum), so
                //the two agree there today; this keeps them right if that ever stops being true.
                double qMeasured = q[h];
                double excursion = q[h] > ceiling ? q[h] - ceiling : (q[h] < minimum ? minimum - q[h] : 0);
                if (excursion > 0 && excursion <= RecirculationCoolingClamp_Lps)
                {
                    q[h] = q[h] > ceiling ? ceiling : minimum;
                    count_Clamped++;
                    maximumClampedExcursion = System.Math.Max(maximumClampedExcursion, excursion);
                }

                bool cooling = tOut[h] < tMix[h] - RecirculationCoolingTolerance_Idle_K;
                bool heating = tOut[h] > tMix[h] + RecirculationCoolingTolerance_Idle_K;

                if (cooling)
                {
                    count_Cooling++;
                    cooling_Wh += q[h] / 1000.0 * RecirculationCoolingRhoCp * (tMix[h] - tOut[h]);
                }

                if (heating)
                {
                    count_Heating++;
                }

                if (tMix[h] < gate)
                {
                    count_BelowGate++;
                    if (cooling)
                    {
                        count_GateViolation++;
                    }
                }

                if (q[h] < minimum - RecirculationCoolingTolerance_Flow_Lps || q[h] > ceiling + RecirculationCoolingTolerance_Flow_Lps)
                {
                    count_OutOfRange++;
                }

                double law = ceiling * settings.FlowFractionByControlTemperature.FlowFraction(tMix[h]);
                if (System.Math.Abs(qMeasured - law) > RecirculationCoolingOffLaw_Lps)
                {
                    count_OffLaw++;
                }

                double[] coordinates = new double[table.AxisCount];
                coordinates[axisIndexes[0]] = odb[h];
                coordinates[axisIndexes[1]] = tMix[h];
                coordinates[axisIndexes[2]] = qMeasured;

                if (table.InDomain(coordinates))
                {
                    count_InDomain++;
                }

                //The coil: idle below the gate; above it, never warmer than its inlet and otherwise the
                //published value held at the table's edges.
                double lookup = table.Value(VentilationUnitPerformanceOutput.Name_SupplyAirTemperature, coordinates, Analytical.Enums.PerformanceDomainPolicy.ClampToDomain);
                double expected = tMix[h] < gate ? tMix[h] : System.Math.Min(tMix[h], lookup);
                maximumTableError = System.Math.Max(maximumTableError, System.Math.Abs(tOut[h] - expected));

                if (canonicalDeviation_Lps != null)
                {
                    maximumCanonicalDeviation = System.Math.Max(maximumCanonicalDeviation, Finite(canonicalDeviation_Lps[h]) ? canonicalDeviation_Lps[h] : double.PositiveInfinity);
                }
            }

            if (count_NonFinite != 0)
            {
                refusals.Add(string.Format("{0}: {1} hour(s) answered a value that is not finite.", label, count_NonFinite));
            }

            if (count_Heating != 0)
            {
                refusals.Add(string.Format("{0} heated the air in {1} hour(s); the module never heats.", label, count_Heating));
            }

            if (count_GateViolation != 0)
            {
                refusals.Add(string.Format("{0} cooled in {1} hour(s) with its mixed return below the {2} C cooling-enable temperature.", label, count_GateViolation, gate));
            }

            if (count_OutOfRange != 0)
            {
                refusals.Add(string.Format("{0} carried a recirculation airflow outside {1:0.###}..{2:0.###} l/s in {3} hour(s).", label, minimum, ceiling, count_OutOfRange));
            }

            if (count != 0 && maximumTableError > RecirculationCoolingTolerance_Table_K)
            {
                refusals.Add(string.Format("{0}: the coil outlet departs from the published table by up to {1:0.####} K.", label, maximumTableError));
            }

            if (maximumCanonicalDeviation > RecirculationCoolingTolerance_Flow_Lps)
            {
                refusals.Add(string.Format("{0}: a ventilation flow of the same air system departed from its design by up to {1:0.####} l/s - the cooling loop disturbed the ventilation.", label, maximumCanonicalDeviation));
            }

            return new RecirculationCoolingResult(
                mechanicalVentilationRecirculationCooling.Guid_AirHandlingUnit,
                mechanicalVentilationRecirculationCooling.Guid_AirSystem,
                startHour,
                q,
                tMix,
                tOut,
                odb,
                ceiling,
                minimum,
                gate,
                count_Cooling,
                count_Heating,
                count_BelowGate,
                count_GateViolation,
                count_OutOfRange,
                count_Clamped,
                maximumClampedExcursion,
                count_OffLaw,
                count_InDomain,
                maximumTableError,
                canonicalDeviation_Lps == null ? double.NaN : maximumCanonicalDeviation,
                cooling_Wh / 1000.0,
                refusals);
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
