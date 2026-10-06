// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Analytical <c>Space.Guid</c> to the TAS zone guid the workflow stamped onto it.
        /// <para>
        /// <b>Why this is the key the Systems route binds by.</b> <c>Modify.UpdateIds</c> writes
        /// <c>SpaceParameter.ZoneGuid</c> onto each space during the TBD workflow, taking it straight
        /// from <c>zone.GUID</c>. Measured on licensed TAS, the TSD written from that TBD answers the
        /// <b>same</b> guid from its zone loads:
        /// </para>
        /// <code>
        /// TBD  zone[0] name="Cell 1" GUID={37FA3D5C-27E0-41D5-8825-366F8DBD66AD}
        /// TSD  load[1] name="Cell 1" GUID={37FA3D5C-27E0-41D5-8825-366F8DBD66AD}
        /// VERDICT: 2 of 2 ZoneLoad GUIDs are also TBD zone GUIDs
        /// </code>
        /// <para>
        /// So this map is what lets a TAS Systems zone be bound to the right zone load with no display
        /// name involved at any step.
        /// </para>
        /// <para>
        /// <b>A space with no stamp is left out rather than guessed at.</b> The caller refuses the room,
        /// naming it, which is a better answer than a plausible match on a name that two rooms share.
        /// </para>
        /// </summary>
        public static Dictionary<Guid, string> ZoneReferences(this AnalyticalModel analyticalModel)
        {
            return ZoneReferences(analyticalModel?.GetSpaces());
        }

        /// <summary>The same map over a space list a caller already holds.</summary>
        public static Dictionary<Guid, string> ZoneReferences(IEnumerable<Space> spaces)
        {
            if (spaces == null)
            {
                return null;
            }

            Dictionary<Guid, string> result = new Dictionary<Guid, string>();

            foreach (Space space in spaces)
            {
                if (space == null)
                {
                    continue;
                }

                if (!space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) || string.IsNullOrWhiteSpace(zoneGuid))
                {
                    continue;
                }

                result[space.Guid] = zoneGuid;
            }

            return result;
        }

        /// <summary>
        /// The TSD path a TBD's simulation writes to: the same directory, the same basename, the
        /// <c>.tsd</c> extension.
        /// <para>
        /// Stated here once because <c>WorkflowCalculator</c> derives it privately, and a consumer that
        /// re-derived it would be guessing at a convention it does not own. Anything that needs the path
        /// should take it from the thermal source that states it, not recompute it.
        /// </para>
        /// </summary>
        public static string Path_TSD(string path_TBD)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
            {
                return null;
            }

            string directory = global::System.IO.Path.GetDirectoryName(path_TBD);
            string fileName = global::System.IO.Path.GetFileNameWithoutExtension(path_TBD);

            return global::System.IO.Path.Combine(directory ?? string.Empty, string.Format("{0}.{1}", fileName, "tsd"));
        }
    }
}
