// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// Which of the three shapes <c>Modify.SetApertureType</c> writes a TBD aperture control in. The mode
    /// is part of an <see cref="ApertureTypeDefinition"/>'s simulation identity: two openings that produce
    /// different modes are different aperture controls whatever else they share.
    /// </summary>
    public enum ApertureTypeProfileMode
    {
        [Description("Undefined")] Undefined = 0,

        /// <summary>
        /// <c>value = 1</c>, a factor, and no schedule and no function - the profile's type is left
        /// untouched, exactly as the write leaves it.
        /// </summary>
        [Description("Plain")] Plain = 1,

        /// <summary>
        /// <c>ticValueProfile</c> with an availability schedule: the schedule's own values are the whole
        /// opening curve.
        /// </summary>
        [Description("Schedule Only")] ScheduleOnly = 2,

        /// <summary>
        /// <c>ticFunctionProfile</c> with a function text. A schedule may ALSO be present, in which case it
        /// stays assigned as an availability multiplier on top of the function - the two are not mutually
        /// exclusive.
        /// </summary>
        [Description("Function")] Function = 3
    }
}
