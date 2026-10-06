// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The prefix of a generated TBD schedule name, used when the requesting SAM schedule has no
        /// meaningful name of its own. Followed by the value signature, e.g.
        /// <c>SAM_DailyAvailability_00FFFE</c>.
        /// </summary>
        public const string ScheduleNamePrefix = "SAM_DailyAvailability_";

        /// <summary>
        /// The name to create a NEW TBD schedule under, given the names already in the building.
        /// <para>
        /// <b>This is only ever reached after a value search has failed</b> - see
        /// <see cref="ScheduleIndex(IEnumerable{int[]}, IEnumerable{int})"/>. Values establish reuse; a name
        /// is metadata. So any existing schedule sharing the requested name necessarily has DIFFERENT
        /// values, and must not be overwritten.
        /// </para>
        /// <para>The rule, in order:</para>
        /// <list type="number">
        /// <item>the requested name if it is free (e.g. <c>PartO_DayOpen_08_23</c>);</item>
        /// <item><c>SAM_DailyAvailability_&lt;signature&gt;</c> if there is no requested name;</item>
        /// <item>
        /// <c>&lt;name&gt;_&lt;signature&gt;</c> when the preferred name is taken by different values - a
        /// deterministic collision suffix, never a TAS/UI-style <c>(1)</c>/<c>(2)</c> counter, so the same
        /// requested values resolve to the same name on every repeated export;
        /// </item>
        /// <item>otherwise a refusal, rather than a third guess.</item>
        /// </list>
        /// </summary>
        /// <param name="refusal">Why no name could be chosen, or null on success.</param>
        /// <returns>The name to create the schedule under, or null when <paramref name="refusal"/> is set.</returns>
        public static string ScheduleName(IEnumerable<string> existingNames, string requestedName, IEnumerable<int> values, out string refusal)
        {
            refusal = null;

            string signature = ScheduleSignature(values);
            if (signature == null)
            {
                refusal = string.Format("A TBD schedule needs exactly {0} hourly values; the requested schedule did not supply them, so no schedule name was derived and nothing was created.", ScheduleHourCount);
                return null;
            }

            HashSet<string> names = new HashSet<string>(existingNames == null ? Enumerable.Empty<string>() : existingNames.Where(x => x != null));

            string preferred = string.IsNullOrWhiteSpace(requestedName) ? string.Format("{0}{1}", ScheduleNamePrefix, signature) : requestedName;
            if (!names.Contains(preferred))
            {
                return preferred;
            }

            string qualified = string.Format("{0}_{1}", preferred, signature);
            if (!names.Contains(qualified))
            {
                return qualified;
            }

            refusal = string.Format("TBD schedule '{0}' already exists with different values, and so does the value-qualified alternative '{1}'. Rather than guess a third name or overwrite a schedule this export did not author, nothing was written.", preferred, qualified);
            return null;
        }
    }
}
