// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// The current <see cref="IResultantTemperatureProvider"/>: the temporary thermostat bridge.
    /// <para>
    /// <b>The one class to delete</b> when TAS Systems exposes a native resultant temperature - together with
    /// <see cref="Create.ThermostatBridge(SystemVentilationRoute, string, double)"/> and the types only it uses.
    /// A caller holds an <see cref="IResultantTemperatureProvider"/> and never learns that a second TAS
    /// simulation was run to answer it.
    /// </para>
    /// </summary>
    public class ThermostatBridgeResultantTemperatureProvider : IResultantTemperatureProvider
    {
        /// <param name="path_TBD">
        /// Where the bridge writes its copy of the no-IZAM building. Its TSD goes beside it. Must not be the
        /// source's own TBD or TSD, or the route's TPD - the bridge refuses rather than overwrite them.
        /// </param>
        /// <param name="achievedAirTemperatureTolerance">See <see cref="ThermostatBridge.DefaultAchievedAirTemperatureTolerance"/>.</param>
        public ThermostatBridgeResultantTemperatureProvider(string path_TBD, double achievedAirTemperatureTolerance = ThermostatBridge.DefaultAchievedAirTemperatureTolerance)
        {
            Path_TBD = path_TBD;
            AchievedAirTemperatureTolerance = achievedAirTemperatureTolerance;
        }

        /// <summary>Where the bridge writes its copy.</summary>
        public string Path_TBD { get; }

        /// <summary>The largest achieved-air deviation accepted.</summary>
        public double AchievedAirTemperatureTolerance { get; }

        public ResultantTemperatureResults ResultantTemperatureResults(SystemVentilationRoute systemVentilationRoute)
        {
            return Create.ThermostatBridge(systemVentilationRoute, Path_TBD, AchievedAirTemperatureTolerance).ResultantTemperatureResults;
        }
    }
}
