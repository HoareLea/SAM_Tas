// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// Which leg of a mechanical ventilation network a connection is.
    /// <para>
    /// The three are kept distinct because the Part O invariant depends on it:
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c>, and
    /// a supply duty is never an extract duty. Measured on licensed TAS, they are carried differently
    /// too: a room's supply sits on its <c>SystemZone</c>, while extract and transfer sit on an in-line
    /// <c>Damper</c> in the leg itself.
    /// </para>
    /// </summary>
    public enum SystemVentilationConnectionType
    {
        /// <summary>Not stated. Always a refusal.</summary>
        Undefined,

        /// <summary>Air delivered to a room from the air handling unit.</summary>
        Supply,

        /// <summary>Air removed from a room to the air handling unit.</summary>
        Extract,

        /// <summary>Air moving from one room to another, originating in a <c>SpaceAirMovement</c>.</summary>
        Transfer,
    }
}
