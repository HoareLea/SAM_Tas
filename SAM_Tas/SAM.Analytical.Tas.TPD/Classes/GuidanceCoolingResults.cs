// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// SAM#123: what each manufacturer-guidance cooling unit did, hour by hour, read back from TAS - and a
    /// summary judged against what was grounded. <b>Manufacturer guidance, not certified performance.</b>
    /// Free of TAS COM types.
    /// </summary>
    public class GuidanceCoolingResults
    {
        private readonly List<GuidanceCoolingResult> results = new List<GuidanceCoolingResult>();
        private readonly List<string> refusals = new List<string>();
        private readonly List<string> notes = new List<string>();

        public GuidanceCoolingResults(int startHour, int endHour, string method)
        {
            StartHour = startHour;
            EndHour = endHour;
            Method = method;
        }

        public int StartHour { get; }

        public int EndHour { get; }

        public string Method { get; }

        public List<GuidanceCoolingResult> Results => new List<GuidanceCoolingResult>(results);

        public List<string> Refusals => new List<string>(refusals);

        public List<string> Notes => new List<string>(notes);

        public bool IsComplete => refusals.Count == 0 && results.Count != 0;

        public void Add(GuidanceCoolingResult guidanceCoolingResult)
        {
            if (guidanceCoolingResult != null)
            {
                results.Add(guidanceCoolingResult);
            }
        }

        public void Refuse(string refusal)
        {
            if (!string.IsNullOrWhiteSpace(refusal))
            {
                refusals.Add(refusal);
            }
        }

        public void Note(string note)
        {
            if (!string.IsNullOrWhiteSpace(note))
            {
                notes.Add(note);
            }
        }

        /// <summary>Every unit's hourly read-back, one row per unit and hour (hour is 0-based).</summary>
        public string ToCsv()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("air_system,hour,intake_C,stat_room_C,extract_C,exchanger_leaving_C,supply_C,supply_Lps,extract_Lps,cooling_signal,dx_sensible_W,dx_latent_W,supply_target_C,operating_state,exchanger_state");

            foreach (GuidanceCoolingResult result in results)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    stringBuilder.AppendLine(string.Join(",",
                        result.Name,
                        (StartHour + i).ToString(CultureInfo.InvariantCulture),
                        F(result.Intake_C[i]),
                        F(result.StatRoom_C[i]),
                        F(result.Extract_C[i]),
                        F(result.ExchangerLeaving_C[i]),
                        F(result.Supply_C[i]),
                        F(result.Supply_Lps[i]),
                        F(result.Extract_Lps[i]),
                        F(result.CoolingSignal(i)),
                        F(result.DXSensible_W[i]),
                        F(result.DXLatent_W[i]),
                        F(result.SupplyTarget_C(i)),
                        result.OperatingState(i),
                        result.ExchangerState(i)));
                }
            }

            return stringBuilder.ToString();
        }

        private static string F(double value)
        {
            return double.IsNaN(value) ? string.Empty : value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>One manufacturer-guidance unit's hourly read-back and its summary.</summary>
    public class GuidanceCoolingResult
    {
        public GuidanceCoolingResult(
            Guid guid_AirSystem,
            string name,
            double designSupply_Lps,
            double designExtract_Lps,
            double elevated_Lps,
            double coolingExtractFraction,
            double coilNetDrop_K,
            double minimumSupply_C,
            double bypassMinimumIntake_C,
            double bypassMinimumExtract_C,
            double coolingDuty_W,
            double activationTemperature_C,
            List<double> intake_C,
            List<double> statRoom_C,
            List<double> extract_C,
            List<double> exchangerLeaving_C,
            List<double> supply_C,
            List<double> supply_Lps,
            List<double> extract_Lps,
            List<double> dXSensible_W,
            List<double> dXLatent_W)
        {
            Guid_AirSystem = guid_AirSystem;
            Name = name;
            DesignSupply_Lps = designSupply_Lps;
            DesignExtract_Lps = designExtract_Lps;
            Elevated_Lps = elevated_Lps;
            CoolingExtractFraction = coolingExtractFraction;
            CoilNetDrop_K = coilNetDrop_K;
            MinimumSupply_C = minimumSupply_C;
            BypassMinimumIntake_C = bypassMinimumIntake_C;
            BypassMinimumExtract_C = bypassMinimumExtract_C;
            CoolingDuty_W = coolingDuty_W;
            ActivationTemperature_C = activationTemperature_C;
            Intake_C = intake_C;
            StatRoom_C = statRoom_C;
            Extract_C = extract_C;
            ExchangerLeaving_C = exchangerLeaving_C;
            Supply_C = supply_C;
            Supply_Lps = supply_Lps;
            Extract_Lps = extract_Lps;
            DXSensible_W = dXSensible_W;
            DXLatent_W = dXLatent_W;
        }

        public Guid Guid_AirSystem { get; }

        public int StartHour { get; set; }

        public string Name { get; }

        public double DesignSupply_Lps { get; }

        public double DesignExtract_Lps { get; }

        public double Elevated_Lps { get; }

        /// <summary>The exchanger's recovery fraction at the elevated airflow.</summary>
        public double CoolingExtractFraction { get; }

        /// <summary>What the coil takes off the air at the elevated airflow [K].</summary>
        public double CoilNetDrop_K { get; }

        /// <summary>The lowest temperature the coil delivers [&#176;C], or NaN where none is stated.</summary>
        public double MinimumSupply_C { get; }

        public double BypassMinimumIntake_C { get; }

        public double BypassMinimumExtract_C { get; }

        public double CoolingDuty_W { get; }

        public double ActivationTemperature_C { get; }

        public List<double> Intake_C { get; }

        public List<double> StatRoom_C { get; }

        public List<double> Extract_C { get; }

        public List<double> ExchangerLeaving_C { get; }

        public List<double> Supply_C { get; }

        public List<double> Supply_Lps { get; }

        public List<double> Extract_Lps { get; }

        public List<double> DXSensible_W { get; }

        public List<double> DXLatent_W { get; }

        public int Count => Intake_C?.Count ?? 0;

        /// <summary>The cooling-stat signal an hour's supply airflow carries: 0 at design, 1 at the elevated rate.</summary>
        public double CoolingSignal(int index)
        {
            return System.Math.Max(0.0, System.Math.Min(1.0, (Supply_Lps[index] - DesignSupply_Lps) / (Elevated_Lps - DesignSupply_Lps)));
        }

        /// <summary>
        /// The supply law's target [&#176;C] for an hour the coil is cooling - the coil entering temperature less the
        /// net drop, not below the minimum - else NaN.
        /// </summary>
        public double SupplyTarget_C(int index)
        {
            if (!IsCooling(index))
            {
                return double.NaN;
            }

            double result = ExchangerLeaving_C[index] - CoilNetDrop_K;
            return double.IsNaN(MinimumSupply_C) ? result : System.Math.Max(result, MinimumSupply_C);
        }

        /// <summary>
        /// What the exchanger should deliver at the elevated airflow [&#176;C]: intake air where the unit's bypass
        /// conditions hold, otherwise recovery at the elevated-airflow fraction.
        /// </summary>
        public double ExchangerTarget_C(int index)
        {
            bool bypass = Intake_C[index] >= BypassMinimumIntake_C && Extract_C[index] > Intake_C[index] && Extract_C[index] >= BypassMinimumExtract_C;
            return bypass ? Intake_C[index] : (CoolingExtractFraction * Extract_C[index]) + ((1 - CoolingExtractFraction) * Intake_C[index]);
        }

        public bool IsCooling(int index)
        {
            return ExchangerLeaving_C[index] - Supply_C[index] > 0.05;
        }

        /// <summary>Observed state only where both airflow and coil read-back are finite and agree.</summary>
        public string OperatingState(int index)
        {
            if (!Finite(Supply_Lps, index) || !Finite(ExchangerLeaving_C, index) || !Finite(Supply_C, index)
                || !IsFinite(DesignSupply_Lps) || !IsFinite(Elevated_Lps) || Elevated_Lps <= DesignSupply_Lps)
            {
                return "UNAVAILABLE";
            }

            double signal = CoolingSignal(index);
            bool coilCooling = IsCooling(index);
            if (signal <= 0.01 && !coilCooling) return "NORMAL";
            if (signal > 0.01 && coilCooling) return "COOLING";
            return "UNAVAILABLE";
        }

        /// <summary>Classifies exchanger read-back from observed temperatures, without assuming a recovery fraction.</summary>
        public string ExchangerState(int index)
        {
            if (!Finite(Intake_C, index) || !Finite(Extract_C, index) || !Finite(ExchangerLeaving_C, index)) return "UNAVAILABLE";

            double bypass = Intake_C[index];
            double extract = Extract_C[index];
            double leaving = ExchangerLeaving_C[index];
            if (System.Math.Abs(bypass - extract) <= 0.1) return "UNAVAILABLE";
            if (System.Math.Abs(leaving - bypass) <= 0.05) return "BYPASS";
            double fraction = (leaving - bypass) / (extract - bypass);
            return fraction >= 0.05 && fraction <= 1.05 ? "RECOVERY" : "UNAVAILABLE";
        }

        private static bool Finite(List<double> values, int index)
        {
            return values != null && index >= 0 && index < values.Count && IsFinite(values[index]);
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public bool IsFullyElevated(int index)
        {
            //TAS lands a fan-controlled flow within a few hundredths of a litre per second of its target.
            return System.Math.Abs(Supply_Lps[index] - Elevated_Lps) <= 0.005 * Elevated_Lps;
        }

        public bool IsCapacityLimited(int index)
        {
            return DXSensible_W[index] + DXLatent_W[index] >= 0.99 * CoolingDuty_W;
        }

        /// <summary>A one-paragraph summary of what the unit did, for the record and the report.</summary>
        public string Summary()
        {
            int hours_Elevated = 0, hours_Modulating = 0, hours_Cooling = 0, hours_CoolingWithoutSignal = 0, hours_SignalWithoutCooling = 0;
            int hours_Full = 0, hours_FullExact = 0, hours_CapacityLimited = 0, hours_RoomAboveBand = 0;
            int hours_ElevatedExchangerExact = 0, hours_AtMinimum = 0, hours_BelowMinimumCoilCooling = 0, hours_BelowMinimumEnteringCold = 0;
            int hours_Part = 0, hours_PartExact = 0;
            double maximumError_K = 0, minimumSupply_C = double.PositiveInfinity, maximumRoom_C = double.NegativeInfinity;

            for (int i = 0; i < Count; i++)
            {
                double signal = CoolingSignal(i);
                bool cooling = IsCooling(i);

                if (IsFullyElevated(i))
                {
                    hours_Elevated++;
                    if (System.Math.Abs(ExchangerLeaving_C[i] - ExchangerTarget_C(i)) <= 0.05)
                    {
                        hours_ElevatedExchangerExact++;
                    }
                }
                else if (signal > 0.01)
                {
                    hours_Modulating++;
                    if (cooling)
                    {
                        hours_Part++;
                        if (System.Math.Abs(Supply_C[i] - SupplyTarget_C(i)) <= 0.05)
                        {
                            hours_PartExact++;
                        }
                    }
                }

                //Below the minimum: either the coil cooled it there (a defect) or the air reached the coil already
                //below it and passed through (a coil does not heat).
                if (!double.IsNaN(MinimumSupply_C) && signal > 0.01)
                {
                    if (Supply_C[i] < MinimumSupply_C - 0.05)
                    {
                        if (ExchangerLeaving_C[i] - Supply_C[i] > 0.05)
                        {
                            hours_BelowMinimumCoilCooling++;
                        }
                        else
                        {
                            hours_BelowMinimumEnteringCold++;
                        }
                    }
                    else if (cooling && System.Math.Abs(Supply_C[i] - MinimumSupply_C) <= 0.05)
                    {
                        hours_AtMinimum++;
                    }
                }

                if (cooling)
                {
                    hours_Cooling++;
                    if (signal <= 0.01)
                    {
                        hours_CoolingWithoutSignal++;
                    }
                }
                else if (signal > 0.01)
                {
                    hours_SignalWithoutCooling++;
                }

                if (cooling && IsFullyElevated(i))
                {
                    if (IsCapacityLimited(i))
                    {
                        hours_CapacityLimited++;
                    }
                    else
                    {
                        hours_Full++;
                        double error_K = System.Math.Abs(Supply_C[i] - SupplyTarget_C(i));
                        maximumError_K = System.Math.Max(maximumError_K, error_K);
                        if (error_K <= 0.05)
                        {
                            hours_FullExact++;
                        }
                    }
                }

                minimumSupply_C = System.Math.Min(minimumSupply_C, Supply_C[i]);
                maximumRoom_C = System.Math.Max(maximumRoom_C, StatRoom_C[i]);

                if (StatRoom_C[i] > ActivationTemperature_C + 0.1)
                {
                    hours_RoomAboveBand++;
                }
            }

            //A rule that states no minimum has no floor to report against.
            string law = double.IsNaN(MinimumSupply_C)
                ? string.Format(CultureInfo.InvariantCulture, "coil entering - {0:0.###} K (no minimum stated)", CoilNetDrop_K)
                : string.Format(CultureInfo.InvariantCulture, "max({0:0.###}, coil entering - {1:0.###} K)", MinimumSupply_C, CoilNetDrop_K);
            string floor = double.IsNaN(MinimumSupply_C)
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, "{0} cooling hour(s) at the limit; below the limit while the stat calls: {1} h cooled there by the coil, {2} h with the coil entering already below it; ", hours_AtMinimum, hours_BelowMinimumCoilCooling, hours_BelowMinimumEnteringCold);

            string summary = string.Format(
                CultureInfo.InvariantCulture,
                "{0}: design {1:0.###}/{2:0.###} l/s supply/extract, elevated {3:0.###} l/s; {4} h fully elevated, {5} h modulating; exchanger state (bypass / recovery {19:0.####}) within 0.05 K in {20} of {4} fully elevated hours; DX cooling {6} h ({7} h without a stat signal, {8} h signal without cooling); supply law {27} met within 0.05 K in {10} of {11} fully elevated cooling hours not capacity-limited (max error {12:0.###} K) and in {25} of {26} part-flow cooling hours; {28}{13} fully elevated hour(s) at the {14:0} W total duty bound; minimum supply {15:0.##} C; stat room max {16:0.##} C, above {17:0.##} C in {18} h.",
                Name,
                DesignSupply_Lps,
                DesignExtract_Lps,
                Elevated_Lps,
                hours_Elevated,
                hours_Modulating,
                hours_Cooling,
                hours_CoolingWithoutSignal,
                hours_SignalWithoutCooling,
                CoilNetDrop_K,
                hours_FullExact,
                hours_Full,
                maximumError_K,
                hours_CapacityLimited,
                CoolingDuty_W,
                minimumSupply_C,
                maximumRoom_C,
                ActivationTemperature_C + 0.1,
                hours_RoomAboveBand,
                CoolingExtractFraction,
                hours_ElevatedExchangerExact,
                MinimumSupply_C,
                hours_AtMinimum,
                hours_BelowMinimumCoilCooling,
                hours_BelowMinimumEnteringCold,
                hours_PartExact,
                hours_Part,
                law,
                floor);

            int first = -1;
            for (int i = 0; i < Count; i++)
            {
                if (OperatingState(i) == "COOLING")
                {
                    first = i;
                    break;
                }
            }
            return summary + string.Format(CultureInfo.InvariantCulture,
                " First observed cooling activation: {0}; operating and exchanger states are in the hourly read-back (UNAVAILABLE where evidence cannot distinguish them).",
                first < 0 ? "UNAVAILABLE" : (StartHour + first).ToString(CultureInfo.InvariantCulture));
        }
    }
}
