// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Convert
    {
        /// <summary>
        /// One space as a TM59 XML zone.
        /// <para>
        /// <b><paramref name="systemType"/> is the authoritative seam</b>, and it existed before the scenario
        /// work needed it. A caller that has an <c>OverheatingScenario</c> states the strategy here and the
        /// fallback below is never reached; <c>Convert.ToTM59(AnalyticalModel, TM59Manager,
        /// VentilationStrategyMap, out List&lt;string&gt;)</c> is that caller.
        /// </para>
        /// <para>
        /// <b>The fallback is derivation #1, and it is superseded.</b> Reading the space's
        /// <c>InternalCondition.VentilationSystemTypeName</c> and defaulting to natural ventilation is design
        /// data answering a question it was not asked - it disagreed with the mechanical-system derivation
        /// that used to override it and with the criterion the assessment applied. It stays reachable only
        /// for callers that state nothing, and it is not to be relied on for a Part O answer.
        /// </para>
        /// </summary>
        public static Zone ToTM59(this Space space, TM59Manager tM59Manager, SystemType systemType = SystemType.Undefined)
        {
            if (space == null || tM59Manager == null)
            {
                return null;
            }

            InternalCondition internalCondition = space.InternalCondition;
            if(internalCondition == null)
            {
                return null;
            }


            Guid guid = Guid.Empty;
            if(!space.TryGetValue(SpaceParameter.ZoneGuid, out guid) || guid == Guid.Empty)
            {
                guid = space.Guid;
            }

            if(systemType == SystemType.Undefined)
            {
                systemType = SystemType.NaturalVentilation;
                if (internalCondition.TryGetValue(InternalConditionParameter.VentilationSystemTypeName, out string ventilationSystemTypeName))
                {
                    if (Analytical.Query.IsMechanicalVentilation(ventilationSystemTypeName))
                    {
                        systemType = SystemType.MechanicalVentilation;
                    }
                }
            }

            return new Zone(guid, space.Name, 1, tM59Manager.RoomUse(space), systemType, true, 0.1);
        }
    }
}
