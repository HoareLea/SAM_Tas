// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Which of a zone's imported internal conditions is the zone's ACTUAL one - the index of the first
        /// that is not a design-day companion, or 0 when they all are.
        /// <para>
        /// A TBD zone carries its normal condition alongside <c>" - HDD"</c> / <c>" - CDD"</c> siblings that
        /// exist only to size on a design day. <c>Convert.ToSAM(TBD.Building, …)</c> has always assigned the
        /// first non-companion to the space and kept the rest as cluster objects; this states that rule ONCE so
        /// the zone-description metadata is applied to the same condition the space ends up holding. Two
        /// copies of the rule that drifted apart would restore the authored airflow onto a companion condition
        /// nothing simulates, silently.
        /// </para>
        /// </summary>
        /// <returns>The index, or -1 when there is nothing to choose from.</returns>
        public static int PrimaryInternalConditionIndex(IList<InternalCondition> internalConditions)
        {
            if (internalConditions == null || internalConditions.Count == 0)
            {
                return -1;
            }

            for (int i = 0; i < internalConditions.Count; i++)
            {
                string name = internalConditions[i]?.Name;
                if (name == null)
                {
                    continue;
                }

                if (!name.EndsWith("HDD") && !name.EndsWith("CDD"))
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
