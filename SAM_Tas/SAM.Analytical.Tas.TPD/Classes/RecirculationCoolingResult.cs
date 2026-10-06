// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// What one air system's recirculation cooling branch did, hour by hour, as TAS reported it - PR5B
    /// (SAM#111) - and whether that is the behaviour the branch was built to have.
    /// <para>
    /// <b>OperatingAirFlow, kept apart.</b> <see cref="OperatingAirFlow_Lps"/> is the recirculation airflow
    /// the cooling loop actually carried each hour. It is never a ventilation design airflow, never the
    /// selected equipment capacity and never outdoor air:
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c>.
    /// </para>
    /// <para>
    /// <b>What is and is not claimed.</b> The coil is an aggregate thermal surrogate: its outlet
    /// temperature is the published table's, and <see cref="Cooling_kWh"/> is the air-side sensible duty
    /// that implies (<c>Q x rho.cp x (inlet - outlet)</c>). No electrical consumption, efficiency ratio,
    /// latent split or refrigerant behaviour is stated anywhere here.
    /// </para>
    /// <para>Free of TAS COM types.</para>
    /// </summary>
    public class RecirculationCoolingResult
    {
        private readonly double[] operatingAirFlow_Lps;
        private readonly double[] mixedReturnTemperature_C;
        private readonly double[] supplyTemperature_C;
        private readonly double[] outdoorTemperature_C;
        private readonly List<string> refusals;

        internal RecirculationCoolingResult(
            Guid guid_AirHandlingUnit,
            Guid guid_AirSystem,
            int startHour,
            double[] operatingAirFlow_Lps,
            double[] mixedReturnTemperature_C,
            double[] supplyTemperature_C,
            double[] outdoorTemperature_C,
            double maximumOperatingAirFlow_Lps,
            double minimumOperatingAirFlow_Lps,
            double coolingEnableTemperature_C,
            int count_Cooling,
            int count_Heating,
            int count_BelowGate,
            int count_GateViolation,
            int count_OutOfRange,
            int count_Clamped,
            double maximumClampedExcursion_Lps,
            int count_OffLaw,
            int count_InPublishedDomain,
            double maximumTableError_K,
            double maximumCanonicalDeviation_Lps,
            double cooling_kWh,
            IEnumerable<string> refusals)
        {
            Guid_AirHandlingUnit = guid_AirHandlingUnit;
            Guid_AirSystem = guid_AirSystem;
            StartHour = startHour;
            this.operatingAirFlow_Lps = operatingAirFlow_Lps ?? new double[0];
            this.mixedReturnTemperature_C = mixedReturnTemperature_C ?? new double[0];
            this.supplyTemperature_C = supplyTemperature_C ?? new double[0];
            this.outdoorTemperature_C = outdoorTemperature_C ?? new double[0];
            MaximumOperatingAirFlow_Lps = maximumOperatingAirFlow_Lps;
            MinimumOperatingAirFlow_Lps = minimumOperatingAirFlow_Lps;
            CoolingEnableTemperature_C = coolingEnableTemperature_C;
            Count_Cooling = count_Cooling;
            Count_Heating = count_Heating;
            Count_BelowGate = count_BelowGate;
            Count_GateViolation = count_GateViolation;
            Count_OutOfRange = count_OutOfRange;
            Count_Clamped = count_Clamped;
            MaximumClampedExcursion_Lps = maximumClampedExcursion_Lps;
            Count_OffLaw = count_OffLaw;
            Count_InPublishedDomain = count_InPublishedDomain;
            MaximumTableError_K = maximumTableError_K;
            MaximumCanonicalDeviation_Lps = maximumCanonicalDeviation_Lps;
            Cooling_kWh = cooling_kWh;
            this.refusals = refusals == null ? new List<string>() : new List<string>(refusals);
        }

        public Guid Guid_AirHandlingUnit { get; }

        public Guid Guid_AirSystem { get; }

        /// <summary>The 0-based hour of year the first value belongs to.</summary>
        public int StartHour { get; }

        public int Count
        {
            get { return operatingAirFlow_Lps.Length; }
        }

        /// <summary>The recirculation airflow [l/s] entering the coil each hour - the branch's OperatingAirFlow.</summary>
        public double[] OperatingAirFlow_Lps
        {
            get { return (double[])operatingAirFlow_Lps.Clone(); }
        }

        /// <summary>The mixed-return temperature [degC] entering the coil each hour - what the gate and the law act on.</summary>
        public double[] MixedReturnTemperature_C
        {
            get { return (double[])mixedReturnTemperature_C.Clone(); }
        }

        /// <summary>The coil outlet temperature [degC] each hour.</summary>
        public double[] SupplyTemperature_C
        {
            get { return (double[])supplyTemperature_C.Clone(); }
        }

        /// <summary>The outdoor dry bulb [degC] each hour, from the thermal source's weather.</summary>
        public double[] OutdoorTemperature_C
        {
            get { return (double[])outdoorTemperature_C.Clone(); }
        }

        /// <summary>The validated ceiling [l/s] - the table's airflow axis, not a capacity.</summary>
        public double MaximumOperatingAirFlow_Lps { get; }

        /// <summary>The lowest recirculation the law allows [l/s].</summary>
        public double MinimumOperatingAirFlow_Lps { get; }

        /// <summary>The declared cooling-enable temperature [degC].</summary>
        public double CoolingEnableTemperature_C { get; }

        /// <summary>Hours in which the coil cooled (outlet below inlet).</summary>
        public int Count_Cooling { get; }

        /// <summary>Hours in which the coil heated (outlet above inlet). Anything but zero refuses.</summary>
        public int Count_Heating { get; }

        /// <summary>Hours with the mixed return below the cooling-enable temperature.</summary>
        public int Count_BelowGate { get; }

        /// <summary>Of <see cref="Count_BelowGate"/>, the hours the coil cooled anyway. Anything but zero refuses.</summary>
        public int Count_GateViolation { get; }

        /// <summary>
        /// Hours the recirculation airflow left the law's range by more than it can be clamped back into
        /// it - see <see cref="Count_Clamped"/>. Anything but zero refuses.
        /// </summary>
        public int Count_OutOfRange { get; }

        /// <summary>
        /// Hours whose airflow stood just outside the law's range and was reported AT the range instead -
        /// see <c>Create.RecirculationCoolingClamp_Lps</c> for what "just" means and why. Reported, not
        /// refused: the declared control cannot command a flow outside its own range, so an excursion this
        /// small is the native solver's within-hour ramp rather than the design's behaviour.
        /// <para>
        /// <b>Watch it.</b> This is the number that says how much of the clamp's margin the model is using.
        /// It is recorded rather than swallowed precisely because the excursion grows with the size of the
        /// controller's approach jump, so a steeper model can use more of it than the one it was
        /// calibrated on.
        /// </para>
        /// </summary>
        public int Count_Clamped { get; }

        /// <summary>
        /// The largest amount [l/s] by which any clamped hour stood outside the law's range, or 0 where no
        /// hour was clamped. Compare against <c>Create.RecirculationCoolingClamp_Lps</c>.
        /// </summary>
        public double MaximumClampedExcursion_Lps { get; }

        /// <summary>
        /// Hours the airflow was more than half a litre per second off the ideal linear law at that hour's
        /// mixed-return temperature - a native within-hour convergence observation, reported, not refused.
        /// </summary>
        public int Count_OffLaw { get; }

        /// <summary>Hours whose outdoor, entering and airflow coordinates all lie inside the published table.</summary>
        public int Count_InPublishedDomain { get; }

        /// <summary>The largest difference [K] between the coil outlet and the published table held at its edges.</summary>
        public double MaximumTableError_K { get; }

        /// <summary>The largest departure [l/s] of the ventilation (canonical) flows from their design, any hour.</summary>
        public double MaximumCanonicalDeviation_Lps { get; }

        /// <summary>Air-side sensible cooling [kWh] implied by the coil's inlet/outlet temperatures and airflow.</summary>
        public double Cooling_kWh { get; }

        public double OperatingAirFlowMinimum_Lps
        {
            get { return Statistic(operatingAirFlow_Lps, System.Math.Min, double.PositiveInfinity); }
        }

        public double OperatingAirFlowMaximum_Lps
        {
            get { return Statistic(operatingAirFlow_Lps, System.Math.Max, double.NegativeInfinity); }
        }

        public double OperatingAirFlowMean_Lps
        {
            get
            {
                if (operatingAirFlow_Lps.Length == 0)
                {
                    return double.NaN;
                }

                double sum = 0;
                foreach (double value in operatingAirFlow_Lps)
                {
                    sum += value;
                }

                return sum / operatingAirFlow_Lps.Length;
            }
        }

        public List<string> Refusals
        {
            get { return new List<string>(refusals); }
        }

        public bool IsValid
        {
            get { return refusals.Count == 0; }
        }

        private static double Statistic(double[] values, Func<double, double, double> func, double seed)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            double result = seed;
            foreach (double value in values)
            {
                result = func(result, value);
            }

            return result;
        }
    }
}
