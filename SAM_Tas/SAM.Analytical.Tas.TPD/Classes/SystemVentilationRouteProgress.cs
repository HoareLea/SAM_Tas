// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// One coarse progress report from <c>Create.SystemVentilationRoute</c>: which operation is starting, and,
    /// where the route counts real items, which one it is on. Independent of any UI.
    /// </summary>
    public sealed class SystemVentilationRouteProgress
    {
        public SystemVentilationRouteProgress(SystemVentilationRouteStage stage, int current, int total)
        {
            Stage = stage;
            Current = current;
            Total = total;
        }

        public SystemVentilationRouteStage Stage { get; }

        /// <summary>The 1-based item now starting, or 0 where the stage is not counted.</summary>
        public int Current { get; }

        /// <summary>How many items the stage has, or 0 where the stage is not counted.</summary>
        public int Total { get; }

        /// <summary>True where <see cref="Current"/> of <see cref="Total"/> is a real count.</summary>
        public bool HasCount => Total > 0 && Current > 0 && Current <= Total;
    }
}
