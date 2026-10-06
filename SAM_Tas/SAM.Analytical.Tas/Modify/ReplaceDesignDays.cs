// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Makes the cluster's design-day record the design days a run has just written into its TBD - and
        /// only those.
        /// <para>
        /// <see cref="AddDesignDays(TBD.Building, IEnumerable{DesignDay}, IEnumerable{DesignDay}, int)"/>
        /// CLEARS the TBD's design days before writing the run's, so after it the TBD holds exactly these.
        /// The cluster used to be appended to instead: a model re-run in an optimisation carried one more
        /// pair per run, and a model first simulated on another weather kept that weather's design days
        /// beside the current ones (the Part O 2B live run of 26 Sep 2026 carried London design days next to
        /// its CIBSE Z1 ones) - a record that no longer described the TBD it came back from.
        /// </para>
        /// <para>
        /// Nothing sizes or simulates from this record - the TBD is written from the run's settings or its
        /// weather (see <see cref="Query.DesignDays_Authoritative"/>) - so replacing it changes no input. It
        /// is what the saved model says it was sized on.
        /// </para>
        /// </summary>
        /// <returns>The number of design days the cluster held before, all of which were removed.</returns>
        public static int ReplaceDesignDays(this AdjacencyCluster adjacencyCluster, IEnumerable<DesignDay> coolingDesignDays, IEnumerable<DesignDay> heatingDesignDays)
        {
            if (adjacencyCluster == null)
            {
                return 0;
            }

            int result = 0;

            List<DesignDay> designDays_Existing = adjacencyCluster.GetObjects<DesignDay>();
            if (designDays_Existing != null)
            {
                foreach (DesignDay designDay_Existing in designDays_Existing)
                {
                    if (adjacencyCluster.RemoveObject(designDay_Existing))
                    {
                        result++;
                    }
                }
            }

            if (coolingDesignDays != null)
            {
                foreach (DesignDay coolingDesignDay in coolingDesignDays)
                {
                    if (coolingDesignDay != null)
                    {
                        adjacencyCluster.AddObject(new DesignDay(coolingDesignDay, LoadType.Cooling));
                    }
                }
            }

            if (heatingDesignDays != null)
            {
                foreach (DesignDay heatingDesignDay in heatingDesignDays)
                {
                    if (heatingDesignDay != null)
                    {
                        adjacencyCluster.AddObject(new DesignDay(heatingDesignDay, LoadType.Heating));
                    }
                }
            }

            return result;
        }
    }
}
