// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// What the TPD-full route carries out of the first (systems) simulation and into the TBD copy that the
    /// second (building) simulation reads.
    /// <para>
    /// <b>Why this is an enum rather than an implementation detail.</b> The two-pass route exists because a TPD
    /// simulation does not produce the <c>ResultantTemperature</c> series TM59 requires, so a second TAS run is
    /// deliberately paid for. Which quantity crosses between the passes is the whole engineering content of that
    /// route, and it was previously implicit in one method body - which is how a future refactor comes to read
    /// the route as duplicated work and delete it. Naming the transfer makes the choice reviewable, and makes
    /// the transfer that TAS <b>cannot</b> perform refusable by name rather than silently absent.
    /// </para>
    /// </summary>
    public enum ResultantTemperatureTransfer
    {
        /// <summary>No transfer stated. Refused; the route never guesses which quantity to carry.</summary>
        [Description("Undefined")] Undefined = 0,

        /// <summary>
        /// The first pass's per-zone air temperature, written into the TBD copy's thermostat upper- and
        /// lower-limit profiles so the second pass is held at the temperature the real system actually achieved.
        /// <para>
        /// <b>This is the only transfer TAS can currently perform, and it is an approximation.</b> It carries the
        /// systems simulation's <i>outcome</i> rather than its supply conditions, and it is what
        /// <see cref="Modify.CalculateResultantTemperature(string, out string, out string)"/> has always done.
        /// It closes the loop because <c>ResultantTemperature</c> is a function of air temperature and mean
        /// radiant temperature: pinning the air temperature to the first pass's answer leaves TBD to compute
        /// only the radiant half, which is the half TPD cannot give.
        /// </para>
        /// </summary>
        [Description("Zone Temperature To Thermostat Limits")] ZoneTemperatureToThermostatLimits = 1,

        /// <summary>
        /// The first pass's per-zone supply air temperature and supply airflow, injected into the TBD copy so the
        /// second pass derives the zone temperature itself.
        /// <para>
        /// <b>The intended transfer, and TAS cannot perform it. Requesting it is refused, never approximated.</b>
        /// The read half exists - a simulated TPD's <c>SystemZone</c> already yields both
        /// <c>SpaceDataType.SupplyAirTemperature</c> and <c>SpaceDataType.FlowRate</c>. The write half does not:
        /// the TBD object model has nowhere to put a per-zone supply air temperature. See
        /// <see cref="ResultantTemperaturePreparation"/> for the evidence and the exact limitation.
        /// </para>
        /// </summary>
        [Description("Supply Air Temperature And Airflow")] SupplyAirTemperatureAndAirflow = 2,
    }
}
