// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>First day of a full TAS simulation year, 1-based.</summary>
        public const int FullYear_FirstDay = 1;

        /// <summary>Last day of a full TAS simulation year, 1-based.</summary>
        public const int FullYear_LastDay = 365;

        /// <summary>
        /// Why a TSD is not a full-year simulation, or null when it is: <c>SimulationData.firstDay == 1</c> and
        /// <c>SimulationData.lastDay == 365</c>.
        /// <para>
        /// <b>For a full-year workflow boundary, not for a reader.</b> Part-year TSDs are legitimate (a TM52/TM59
        /// summer run, a design check); the generic readers - <see cref="ZoneResultSeries"/>, the annual getters, the
        /// TSD conversion - keep reading them. A caller whose assessment is DEFINED over a whole year asks this
        /// explicitly (<see cref="TSDConversionSettings.RequireFullYear"/>).
        /// </para>
        /// <para>
        /// <b>Why the day range and not the series length.</b> Measured on TSD 2.0.0.1: <c>GetAnnualZoneResult</c> and
        /// <c>GetAnnualBuildingResult</c> return 8760 values for ANY simulation, and every hour past
        /// <c>lastDay</c> is -1; daily reads outside the range return 24 x -1. Nothing throws, is null or short. So
        /// the 8760-length checks downstream (e.g. TM59's expected hour count) never detected a part-year file - they
        /// read the padding as -1 degC. <c>SimulationData</c> carries no status flag; its day range is the only
        /// signal.
        /// </para>
        /// </summary>
        public static string FullYearRefusal(this SimulationData simulationData)
        {
            if (simulationData == null)
            {
                return "The TSD has no simulation data, so it cannot be shown to hold a full year.";
            }

            return FullYearRefusal(simulationData.firstDay, simulationData.lastDay);
        }

        /// <summary>
        /// Why the day range <paramref name="firstDay"/>..<paramref name="lastDay"/> is not a full TAS simulation year,
        /// or null when it is. See <see cref="FullYearRefusal(SimulationData)"/>.
        /// </summary>
        public static string FullYearRefusal(int firstDay, int lastDay)
        {
            if (firstDay == FullYear_FirstDay && lastDay == FullYear_LastDay)
            {
                return null;
            }

            return string.Format(
                "The TSD holds simulation days {0}..{1}, not the full year {2}..{3}; its results are refused rather than read (TSD pads the days it did not simulate with -1).",
                firstDay,
                lastDay,
                FullYear_FirstDay,
                FullYear_LastDay);
        }
    }
}
