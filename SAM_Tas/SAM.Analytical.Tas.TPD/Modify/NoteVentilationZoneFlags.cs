// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// Declares, in the workflow output, what the explicit route left on every native zone's
        /// <c>Flags</c> - so the deviation is stated rather than left for a reader to infer from a
        /// number.
        /// <para>
        /// <b>The route treats the three flags asymmetrically, deliberately.</b>
        /// <c>tpdSystemZoneFlagDisplacementVent</c> (1) is <b>inherited from the template prototype</b>
        /// on both the explicit and the replicated route - it states how air is delivered within a room,
        /// not a second ventilation model, and nothing here decides it.
        /// <c>tpdSystemZoneFlagModelVentFlow</c> (4) and
        /// <c>tpdSystemZoneFlagModelInterzoneFlow</c> (2) are <b>suppressed on the explicit route</b>,
        /// because the graph already carries every supply, extract and room-to-room transfer and the
        /// building model stating its own would be a second statement of the same air.
        /// </para>
        /// <para>
        /// <b>What the suppression is worth was measured, and the honest answer is: on this route,
        /// nothing.</b> Re-enabling both bits on the licensed acceptance document and re-simulating
        /// moved <c>ZoneTemperature</c> by at most <b>4.0e-05 K</b> across 9 rooms x 8760 hours -
        /// exactly the bound two runs of the <i>same</i> document differ by, so it is the solver's noise
        /// floor and not a signal. The reason is that this route's thermal source is the no-IZAM TBD:
        /// <c>ticV</c> is zeroed on every internal condition and no interzone air movement is authored,
        /// so there is nothing for those bits to re-apply. Clearing them is therefore <b>structural
        /// correctness</b> - it makes double counting unreachable if a future source did carry those
        /// terms - and not a demonstrated numerical correction. For contrast, the fan heat gain factor
        /// on the same fixture is worth up to 2.80 K; see <see cref="GroundVentilationFans"/>.
        /// </para>
        /// <para>
        /// One note per air system rather than per room, and read off the native zones rather than off
        /// the SAM values that were written, so it reports what TAS holds.
        /// </para>
        /// </summary>
        public static bool NoteVentilationZoneFlags(
            SystemVentilationConversionContext systemVentilationConversionContext,
            global::TPD.System system)
        {
            if (systemVentilationConversionContext == null || system == null)
            {
                return false;
            }

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(system);
            if (systemComponents == null)
            {
                return true;
            }

            int count_Zones = 0;
            int count_DisplacementVent = 0;
            int count_ModelVentFlow = 0;
            int count_ModelInterzoneFlow = 0;

            foreach (global::TPD.SystemComponent systemComponent in systemComponents)
            {
                if (!(systemComponent is global::TPD.SystemZone systemZone))
                {
                    continue;
                }

                count_Zones++;

                int flags = systemZone.Flags;

                if ((flags & (int)tpdSystemZoneFlags.tpdSystemZoneFlagDisplacementVent) != 0)
                {
                    count_DisplacementVent++;
                }

                if ((flags & (int)tpdSystemZoneFlags.tpdSystemZoneFlagModelVentFlow) != 0)
                {
                    count_ModelVentFlow++;
                }

                if ((flags & (int)tpdSystemZoneFlags.tpdSystemZoneFlagModelInterzoneFlow) != 0)
                {
                    count_ModelInterzoneFlow++;
                }
            }

            if (count_Zones == 0)
            {
                return true;
            }

            systemVentilationConversionContext.Note(string.Format(
                "Air system '{0}': {1} native zone(s). Displacement ventilation on {2} of them - "
                + "inherited from the template prototype, not decided by this route. The building "
                + "model's own ventilation flow is off on {3} of {1} and its interzone flow off on "
                + "{4} of {1}, because the graph states every supply, extract and transfer leg "
                + "explicitly.",
                ((dynamic)system).Name ?? "<unnamed>",
                count_Zones,
                count_DisplacementVent,
                count_Zones - count_ModelVentFlow,
                count_Zones - count_ModelInterzoneFlow));

            if (count_ModelVentFlow != 0 || count_ModelInterzoneFlow != 0)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Air system '{0}': {1} native zone(s) still model the building's own ventilation flow "
                    + "and {2} its interzone flow, so that air would be applied twice - once by the "
                    + "explicit graph and once by the building model.",
                    ((dynamic)system).Name ?? "<unnamed>",
                    count_ModelVentFlow,
                    count_ModelInterzoneFlow));

                return false;
            }

            return true;
        }
    }
}
