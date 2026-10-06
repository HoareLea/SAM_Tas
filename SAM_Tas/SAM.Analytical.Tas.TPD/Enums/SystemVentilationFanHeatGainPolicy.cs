// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// What the explicit ventilation route does with a fan's native <c>HeatGainFactor</c> - Part O
    /// Iteration 3 PR5A (SAM#111 plan §D/§K.3).
    /// <para>
    /// <b><c>ClearToZero</c> is the B0 control and stays the default.</b> Every fan's heat gain factor is
    /// forced to 0 and read back, exactly as <see cref="Modify.GroundVentilationFans"/> always has -
    /// measured on the acceptance fixture to move <c>ZoneTemperature</c> by up to 2.80 K if left at the
    /// shipped template's 1.0, so it is stated deliberately rather than inherited.
    /// </para>
    /// <para>
    /// <b><c>FromSystemsGraph</c> is the manufacturer-aware route.</b> SAM_Systems (PR5A) may already have
    /// written a resolved <c>SupplyFanHeatGainFactor</c>/<c>ExtractFanHeatGainFactor</c> onto the
    /// materialised <c>SystemFan</c> before conversion - forcing it back to 0 here would silently discard
    /// that resolved figure. This policy leaves the native value exactly as
    /// <c>Convert.ToTPD(SystemFan, …)</c> already wrote it, and still reads it back: a write TAS declined
    /// to keep is refused here exactly as <c>ClearToZero</c> refuses one.
    /// </para>
    /// </summary>
    public enum SystemVentilationFanHeatGainPolicy
    {
        /// <summary>Force every fan's <c>HeatGainFactor</c> to 0 and read it back. The B0 control; the default.</summary>
        ClearToZero,

        /// <summary>Leave every fan's <c>HeatGainFactor</c> exactly as the Systems graph wrote it, and read it back unchanged.</summary>
        FromSystemsGraph,
    }
}
