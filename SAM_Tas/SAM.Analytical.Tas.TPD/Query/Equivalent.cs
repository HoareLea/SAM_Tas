// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        /// <summary>
        /// Whether two flow rates are the same number as far as TAS is concerned.
        /// <para>
        /// A <c>SizedFlowVariable.Value</c> is stored single precision: an authored <c>128.0</c> reads
        /// back as <c>128.00001525878906</c>, and <c>149.33333333</c> as <c>149.33334350585938</c>.
        /// Comparing those with <c>==</c> would refuse every correct conversion, so the comparison is
        /// relative, with an absolute floor so that a zero can still be recognised exactly.
        /// </para>
        /// <para>
        /// The tolerance is <see cref="SystemVentilationConversionContext.FlowRateTolerance_Relative"/>,
        /// stated once there so the writer and the reconciliation cannot drift apart.
        /// </para>
        /// </summary>
        public static bool Equivalent(double value_1, double value_2)
        {
            if (double.IsNaN(value_1) || double.IsNaN(value_2) || double.IsInfinity(value_1) || double.IsInfinity(value_2))
            {
                return false;
            }

            double tolerance = global::System.Math.Max(
                SystemVentilationConversionContext.FlowRateTolerance_Absolute,
                global::System.Math.Max(global::System.Math.Abs(value_1), global::System.Math.Abs(value_2)) * SystemVentilationConversionContext.FlowRateTolerance_Relative);

            return global::System.Math.Abs(value_1 - value_2) <= tolerance;
        }
    }
}
