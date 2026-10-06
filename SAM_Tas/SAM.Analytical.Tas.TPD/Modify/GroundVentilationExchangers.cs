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
        /// Reads back the one native property PR5A adds to the air-side exchanger conversion -
        /// <c>ExchCalcType</c> (SAM#111 plan §D) - and refuses if TAS did not keep what
        /// <c>Convert.ToTPD(DisplaySystemExchanger, …)</c> wrote.
        /// <para>
        /// <b>Why only this property.</b> Every other exchanger property this route writes
        /// (<c>SensibleEfficiency</c>, <c>LatentEfficiency</c>, <c>ExchLatType</c>, <c>ExchangerType</c>,
        /// <c>Flags</c>) predates PR5A and is unchanged by it. <c>ExchCalcType</c> is the one write this
        /// slice adds, and it matters: Phase 0 (X1-D) measured that <c>Simple</c> only ever applied because
        /// it happens to be TAS's own default for a new exchanger, that <c>NTU</c> refuses outright with
        /// zero heat transfer area, and that <c>Duty</c> ignores the stated efficiency entirely. A write
        /// TAS silently declined here would leave the exchanger's calculation method wrong with nothing
        /// announcing it - exactly the failure mode <c>Modify.GroundVentilationFans</c> already guards for
        /// its own properties.
        /// </para>
        /// <para>
        /// <b>Expects <c>Simple</c>, not whatever the graph asked for.</b> The frozen PR5A mapping
        /// (SAM#111 plan §C) is the only source that ever states <c>ExchangerCalculationMethod</c>, and it
        /// only ever states <c>Simple</c> - so this checks the one calculation method PR5A actually
        /// produces, the same way <c>ClearToZero</c> checks for a literal 0 rather than re-deriving an
        /// expectation from the graph. A future slice that legitimately writes <c>NTU</c>/<c>Duty</c> would
        /// need this revisited, not silently satisfied.
        /// </para>
        /// <para>
        /// <b>Late-bound, following the established pattern.</b> <c>Convert.ToSAM</c>'s own read of this
        /// same native property, on the sibling liquid-exchanger/chiller conversions, reads it as
        /// <c>@dynamic.ExchCalcType</c> rather than a typed accessor - the same split this repository
        /// already documents for <c>ISystemComponent.GUID</c>/<c>.Name</c> and a fan's schedule
        /// (<c>Modify.GroundVentilationFans</c>). This follows that precedent rather than assuming a typed
        /// read is reliable here too.
        /// </para>
        /// <para>
        /// <b>A no-op wherever no exchanger exists.</b> B0 has none - measured, in the licensed B0/A parity
        /// decomposition, on all three of the frozen fixture's air systems - so this call costs nothing and
        /// changes nothing for the control.
        /// </para>
        /// </summary>
        public static bool GroundVentilationExchangers(
            SystemVentilationConversionContext systemVentilationConversionContext,
            global::TPD.System system)
        {
            if (systemVentilationConversionContext == null)
            {
                return false;
            }

            if (system == null)
            {
                systemVentilationConversionContext.Refuse(
                    "An air system produced no native TAS system, so its exchangers could not be grounded.");

                return false;
            }

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(system);
            if (systemComponents == null)
            {
                return true;
            }

            List<string> notes = new List<string>();

            foreach (global::TPD.SystemComponent systemComponent in systemComponents)
            {
                if (!(systemComponent is global::TPD.Exchanger exchanger))
                {
                    continue;
                }

                string reference_Exchanger = Query.NativeReference(exchanger) ?? "<no identifier>";

                tpdExchangerCalcMethod exchCalcType;

                try
                {
                    exchCalcType = (tpdExchangerCalcMethod)((dynamic)exchanger).ExchCalcType;
                }
                catch (Exception exception)
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Exchanger {0}: reading its calculation method back threw {1}: {2}.",
                        reference_Exchanger,
                        exception.GetType().Name,
                        exception.Message));

                    continue;
                }

                if (exchCalcType != tpdExchangerCalcMethod.tpdExchangerCalcSimple)
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Exchanger {0}: TAS reports its calculation method as {1}, not the Simple method the "
                        + "route stated - NTU refuses with zero heat transfer area and Duty ignores the "
                        + "stated efficiency, so this is not a substitutable configuration.",
                        reference_Exchanger,
                        exchCalcType));

                    continue;
                }

                notes.Add(string.Format(
                    "Exchanger {0} states calculation method {1}.",
                    reference_Exchanger,
                    exchCalcType));
            }

            notes.Sort(StringComparer.Ordinal);

            foreach (string note in notes)
            {
                systemVentilationConversionContext.Note(note);
            }

            return systemVentilationConversionContext.Refusals.Count == 0;
        }
    }
}
