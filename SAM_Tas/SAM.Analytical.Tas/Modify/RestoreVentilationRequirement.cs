// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// <b>Restores the SAM airflow REQUIREMENT an export recorded, in place of what the native TAS import
        /// inferred</b> - the import half of <see cref="SAMZoneMetadata"/>.
        /// <para>
        /// The native import can only state what TAS states: <c>freshAirRate</c> as
        /// <c>SupplyAirFlowPerPerson</c>, and <c>ticV.factor</c> as <c>SupplyAirChangesPerHour</c>. Where a
        /// Ventilation profile realised the requirement, that factor is the CALCULATED TOTAL of all four SAM
        /// bases - so storing it back on the ACH basis makes the next export sum the other bases on top of a
        /// figure that already contains them, and the rate grows once per generation, without bound. This
        /// replaces that inference with the decomposition SAM actually authored, which is a fixed point.
        /// </para>
        /// <para>
        /// <b>Requirement and realisation stay separate.</b> Where the metadata records that NO Ventilation
        /// profile was assigned, the ventilation profile reference the native import wrote from the mere
        /// presence of a <c>ticV</c> slot is removed again. Otherwise the next export would find a profile,
        /// write a factor, and TBD Building Simulator mechanical ventilation would have switched itself on
        /// during a round trip that was only ever asked to carry engineering data.
        /// </para>
        /// <para>
        /// <b>Conservative about staleness.</b> The metadata is transport data; the TAS file may have been
        /// edited since. The native values SAM left behind are compared with what TAS states now, and any
        /// disagreement refuses the whole section rather than half-applying it - the caller keeps the native
        /// import and <paramref name="note"/> says what disagreed. This is a mismatch check, not a
        /// conflict-resolution engine: it decides only whether the recorded decomposition is still known to
        /// describe this zone.
        /// </para>
        /// </summary>
        /// <param name="internalCondition">The condition the native import just produced, modified in place.</param>
        /// <param name="metadata">The parsed SAM section, or null - null is simply "nothing to restore".</param>
        /// <param name="freshAirRate">What TAS states now for <c>InternalGain.freshAirRate</c> [l/s/p].</param>
        /// <param name="ventilationFactor">What TAS states now for <c>ticV.factor</c> [ACH].</param>
        /// <param name="note">Null when nothing needed saying; otherwise why the section was refused.</param>
        /// <returns>True when the authored bases were restored; false when the native import stands.</returns>
        public static bool RestoreVentilationRequirement(this InternalCondition internalCondition, SAMZoneMetadata metadata, double freshAirRate, double ventilationFactor, out string note)
        {
            note = null;

            if (internalCondition == null || metadata == null)
            {
                return false;
            }

            if (!VentilationValueMatches(metadata.FreshAirRate, freshAirRate))
            {
                note = VentilationStaleNote("freshAirRate", metadata.FreshAirRate, freshAirRate);
                return false;
            }

            //Only fingerprinted where SAM authored the factor. Where it did not, a value there is TAS's own
            //and says nothing about whether the requirement data went stale.
            if (metadata.VentilationProfileApplied && !VentilationValueMatches(metadata.VentilationFactor, ventilationFactor))
            {
                note = VentilationStaleNote("ticV.factor", metadata.VentilationFactor, ventilationFactor);
                return false;
            }

            RestoreVentilationValue(internalCondition, InternalConditionParameter.SupplyAirFlow, metadata.SupplyAirFlow);
            RestoreVentilationValue(internalCondition, InternalConditionParameter.SupplyAirFlowPerArea, metadata.SupplyAirFlowPerArea);
            RestoreVentilationValue(internalCondition, InternalConditionParameter.SupplyAirFlowPerPerson, metadata.SupplyAirFlowPerPerson);
            RestoreVentilationValue(internalCondition, InternalConditionParameter.SupplyAirChangesPerHour, metadata.SupplyAirChangesPerHour);

            if (!metadata.VentilationProfileApplied)
            {
                internalCondition.RemoveValue(InternalConditionParameter.VentilationProfileName);
            }

            return true;
        }

        //A basis the export did not record was not authored, so it must be REMOVED and not left holding what
        //the native import inferred - a restore that only ever writes would leave the inferred ACH basis in
        //place and reinstate the feedback it exists to break.
        private static void RestoreVentilationValue(InternalCondition internalCondition, InternalConditionParameter internalConditionParameter, double value)
        {
            if (double.IsNaN(value))
            {
                internalCondition.RemoveValue(internalConditionParameter);
                return;
            }

            internalCondition.SetValue(internalConditionParameter, value);
        }

        //Both TAS fields are singles, and the value makes a round trip through the file, so an exact
        //comparison would refuse valid metadata over the last bit. Relative, with an absolute floor so a rate
        //of zero compares sensibly.
        private static bool VentilationValueMatches(double value_Recorded, double value)
        {
            if (double.IsNaN(value_Recorded) && double.IsNaN(value))
            {
                return true;
            }

            if (double.IsNaN(value_Recorded) || double.IsNaN(value))
            {
                return false;
            }

            double difference = System.Math.Abs(value_Recorded - value);

            return difference <= 1e-6 || difference <= 1e-5 * System.Math.Abs(value_Recorded);
        }

        private static string VentilationStaleNote(string name, double value_Recorded, double value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "SAM zone metadata ignored: TAS states {0} = {1}, the export recorded {2}. The file was edited after SAM wrote it, so the recorded SAM airflow bases are no longer known to describe this zone - imported what TAS states instead.",
                name,
                VentilationValueText(value),
                VentilationValueText(value_Recorded));
        }

        private static string VentilationValueText(double value)
        {
            return double.IsNaN(value) ? "nothing" : value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
