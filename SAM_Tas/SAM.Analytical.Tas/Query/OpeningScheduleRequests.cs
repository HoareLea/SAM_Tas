// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The child opening properties a schedule request is resolved against: the opening itself when it
        /// is single, or each <see cref="MultipleOpeningProperties.SingleOpeningProperties"/> entry in
        /// order. This is the expansion the aperture-type write iterates, so child index i here is the
        /// child whose write the write reports as opening i + 1.
        /// </summary>
        private static List<ISingleOpeningProperties> OpeningScheduleChildren(IOpeningProperties openingProperties)
        {
            List<ISingleOpeningProperties> result = new List<ISingleOpeningProperties>();

            if (openingProperties is ISingleOpeningProperties single)
            {
                result.Add(single);
            }
            else if (openingProperties is MultipleOpeningProperties multiple && multiple.SingleOpeningProperties != null)
            {
                result.AddRange(multiple.SingleOpeningProperties);
            }

            return result;
        }

        /// <summary>
        /// Per child opening property, whether that child STATES an availability schedule the TBD aperture
        /// control must carry - one entry per child, in child order. The same resolution the write itself
        /// uses (<see cref="TryGetOpeningScheduleSource(ISingleOpeningProperties, out string, out int[], out string)"/>)
        /// decides each entry, so a diagnostic counting requests cannot drift from what was actually
        /// requested. A child whose stated source is unusable still counts as requesting: its write is
        /// refused, and a refused request is exactly what the schedule-delivery diagnostic must catch.
        /// </summary>
        public static List<bool> OpeningScheduleRequests(this IOpeningProperties openingProperties)
        {
            List<bool> result = new List<bool>();

            foreach (ISingleOpeningProperties child in OpeningScheduleChildren(openingProperties))
            {
                bool requested = child.TryGetOpeningScheduleSource(out string _, out int[] _, out string refusal);
                result.Add(requested || refusal != null);
            }

            return result;
        }

        /// <summary>
        /// The indices of the child opening properties that stated an availability schedule their TBD write
        /// did NOT deliver.
        /// <para>
        /// <paramref name="scheduleDelivered"/> is per child, in the same order as
        /// <see cref="OpeningScheduleRequests(IOpeningProperties)"/>: whether the aperture type THAT child's
        /// write returned carries a schedule read back off the TBD. Every requesting child is checked
        /// against its own write - never against the first child's aperture type - so a multiple-opening
        /// aperture mixing unrestricted and schedule-carrying children is judged per child: an unrestricted
        /// child 0 does not mask or impersonate a schedule requested by child 1, and a schedule on child 0
        /// does not hide one child 1 failed to get.
        /// </para>
        /// </summary>
        public static List<int> UndeliveredOpeningScheduleRequests(this IOpeningProperties openingProperties, IReadOnlyList<bool> scheduleDelivered)
        {
            List<int> result = new List<int>();

            List<bool> requests = OpeningScheduleRequests(openingProperties);
            for (int i = 0; i < requests.Count; i++)
            {
                if (!requests[i])
                {
                    continue;
                }

                bool delivered = scheduleDelivered != null && i < scheduleDelivered.Count && scheduleDelivered[i];
                if (!delivered)
                {
                    result.Add(i);
                }
            }

            return result;
        }
    }
}
