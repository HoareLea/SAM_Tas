// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// The coarse operations <c>Create.SystemVentilationRoute</c> reports to an optional progress callback.
    /// Each is a real boundary in the route, entered once (or once per air system) - never inside an hourly
    /// or per-cell loop - so a caller can say which major operation is running.
    /// </summary>
    public enum SystemVentilationRouteStage
    {
        /// <summary>Building the TAS Systems document, one air system at a time. Current/Total count air systems.</summary>
        ConvertingAirSystems,

        /// <summary>Checking the finished document against the source ventilation graph. No count.</summary>
        ReconcilingConversion,

        /// <summary><c>ISystem.Simulate</c>, one air system at a time. Current/Total count air systems.</summary>
        SimulatingAirSystems,

        /// <summary>Reading the recirculation-cooling branches' behaviour back from one plant-room pass. No count.</summary>
        ReadingRecirculationCooling,

        /// <summary>The manufacturer-guidance evidence pass (<c>SimulateEx</c>). One opaque TAS call; no count.</summary>
        ReadingManufacturerGuidance,
    }
}
