// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// Where the Part O Iteration 3 route gets each room's hourly <c>ResultantTemperature</c> from, once the
    /// TAS Systems simulation has run.
    /// <para>
    /// <b>Why this seam exists.</b> TAS Systems does not currently answer the resultant temperature the Part O
    /// / TM59 assessment needs, so SAM_Tas obtains it through a temporary thermostat bridge
    /// (<see cref="ThermostatBridgeResultantTemperatureProvider"/>): the achieved Systems
    /// <c>ZoneTemperature</c> is imposed on a copy of the same no-IZAM building, which is simulated a second
    /// time. When TAS exposes a native Systems resultant temperature, a provider reading it replaces the bridge
    /// here and nothing upstream (SAM, SAM_Systems) or downstream (TM59) changes.
    /// </para>
    /// <para>
    /// Deliberately narrow: one Systems route in, one complete-or-refused set of series out, keyed by
    /// analytical room guid.
    /// </para>
    /// </summary>
    public interface IResultantTemperatureProvider
    {
        /// <summary>
        /// Each room's hourly resultant temperature for the route's rooms and period. Never null: a provider
        /// that cannot answer returns a refused, empty <see cref="TPD.ResultantTemperatureResults"/> saying why.
        /// </summary>
        ResultantTemperatureResults ResultantTemperatureResults(SystemVentilationRoute systemVentilationRoute);
    }
}
