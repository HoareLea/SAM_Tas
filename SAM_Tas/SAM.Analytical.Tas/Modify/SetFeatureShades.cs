// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Number of times <c>AssignFeatureShade</c> is attempted before the write is reported as failed.
        /// <para>
        /// <b>Licensed TAS does not attach a <c>FeatureShade</c> on the FIRST assignment</b> when the shade
        /// was just created on a building TAS itself has only moments ago written
        /// (<c>T3DDocument.ExportNew</c>). The call returns without error, and the element still reads
        /// <c>GetFeatureShade(1) == null</c>. Assigning the SAME object a second time lands it, and every
        /// subsequent shade on that building then lands first time. Measured on the licensed gbXML route:
        /// a probe writing the same shade to the same element three times in a row read back
        /// <c>0, 1, 1</c>, and a raw <c>AddFeatureShade</c>/<c>AssignFeatureShade</c> pair with no SAM code
        /// in between reproduced it exactly.
        /// </para>
        /// <para>
        /// Two is therefore what is actually needed; the third is slack. A cap rather than a loop, because
        /// a shade that will never attach must be REPORTED, not spun on.
        /// </para>
        /// </summary>
        private const int Attempts_AssignFeatureShade = 3;

        /// <summary>
        /// Replaces whatever feature shade <paramref name="buildingElement"/> carries with
        /// <paramref name="featureShade"/>.
        /// <para>
        /// <b>The assignment is established by RE-READING the element, not by trusting the COM call.</b> See
        /// <see cref="Attempts_AssignFeatureShade"/> for the licensed behaviour that makes this necessary.
        /// The retry re-assigns the SAME <c>TBD.FeatureShade</c> - it never creates a second one - so it can
        /// leave neither a duplicate shade on the element nor an orphan on the building.
        /// </para>
        /// </summary>
        /// <returns>
        /// The shades now ON the element: empty when the assignment never took (the element carries no
        /// shade), null when there was nothing to do.
        /// </returns>
        public static List<TBD.FeatureShade> SetFeatureShades(this Building building, buildingElement buildingElement, FeatureShade featureShade)
        {
            if (building == null || buildingElement == null || featureShade == null)
            {
                return null;
            }

            buildingElement.RemoveFeatureShades();

            List<TBD.FeatureShade> result = new List<TBD.FeatureShade>();

            TBD.FeatureShade featureShade_TBD = Convert.ToTBD(featureShade, building);
            if (featureShade_TBD == null)
            {
                return result;
            }

            for (int attempt = 0; attempt < Attempts_AssignFeatureShade && buildingElement.GetFeatureShade(1) == null; attempt++)
            {
                buildingElement.AssignFeatureShade(featureShade_TBD);
            }

            if (buildingElement.GetFeatureShade(1) == null)
            {
                //Never attached. The caller is told nothing is on the element rather than being handed the
                //object that was created for it.
                return result;
            }

            result.Add(featureShade_TBD);

            List<dayType> dayTypes = building.DayTypes();
            if (dayTypes != null)
            {
                dayTypes.RemoveAll(x => x.name.Equals("HDD") || x.name.Equals("CDD"));
                foreach (dayType dayType in dayTypes)
                {
                    featureShade_TBD.SetDayType(dayType, true);
                }
            }

            return result;
        }
    }
}
