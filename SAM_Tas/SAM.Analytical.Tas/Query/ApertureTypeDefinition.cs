// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The reusable TBD aperture control one SAM opening asks for, resolved WITHOUT touching COM.
        /// <para>
        /// This is the same resolution <c>Modify.SetApertureType</c> has always performed - the schedule
        /// source, the discharge coefficient, the factor and the Part O <c>AlwaysClosed -&gt; 0</c>
        /// override, the function and the description - lifted out so that an opening's control can be
        /// identified, compared and named before a single TBD object is read or created. An opening whose
        /// stated schedule is unusable is refused here, ahead of any COM, exactly as it was before.
        /// </para>
        /// </summary>
        /// <param name="dayTypeNames">
        /// The day types the control will apply on - for a control this export writes, every day type in the
        /// building bar HDD and CDD, which is what <c>Modify.SetApertureType</c> assigns. Membership is part
        /// of the definition because the S1-C0 probe established TAS reads it back faithfully.
        /// </param>
        /// <param name="name_Schedule">The requested schedule name, or null when the opening states none. Naming only; it takes no part in identity.</param>
        /// <param name="refusal">Why no definition could be resolved, or null on success.</param>
        public static ApertureTypeDefinition ApertureTypeDefinition(this ISingleOpeningProperties singleOpeningProperties, IEnumerable<string> dayTypeNames, out string name_Schedule, out string refusal)
        {
            name_Schedule = null;
            refusal = null;

            if (singleOpeningProperties == null)
            {
                refusal = "No opening properties to resolve an aperture control from.";
                return null;
            }

            //Refused before anything else, exactly as the write refuses it: an opening that states an
            //unusable schedule must not reach a TBD at all.
            bool scheduleRequested = singleOpeningProperties.TryGetOpeningScheduleSource(out name_Schedule, out int[] values_Schedule, out refusal);
            if (refusal != null)
            {
                name_Schedule = null;
                return null;
            }

            float factor = global::System.Convert.ToSingle(singleOpeningProperties.GetFactor());
            if (singleOpeningProperties is PartOOpeningProperties partOOpeningProperties && partOOpeningProperties.OpeningRestriction == OpeningRestriction.AlwaysClosed)
            {
                //Identity is what TBD carries, not why. An opening zeroed because it takes no part in the
                //overheating strategy is the same control as one explicitly given factor 0.
                factor = 0;
            }

            singleOpeningProperties.TryGetValue(OpeningPropertiesParameter.Function, out string function);
            singleOpeningProperties.TryGetValue(OpeningPropertiesParameter.Description, out string description);

            ApertureTypeProfileMode mode;
            if (!string.IsNullOrWhiteSpace(function))
            {
                //A function and a schedule are not mutually exclusive: the function is the base curve and
                //the schedule stays on as an availability multiplier, so the schedule is still keyed.
                mode = ApertureTypeProfileMode.Function;
            }
            else
            {
                mode = scheduleRequested ? ApertureTypeProfileMode.ScheduleOnly : ApertureTypeProfileMode.Plain;
            }

            return new ApertureTypeDefinition(
                global::System.Convert.ToSingle(singleOpeningProperties.GetDischargeCoefficient()),
                factor,
                mode,
                function,
                scheduleRequested ? values_Schedule : null,
                description,
                dayTypeNames);
        }

        /// <summary>
        /// The same resolution, for callers that do not need the requested schedule name.
        /// </summary>
        public static ApertureTypeDefinition ApertureTypeDefinition(this ISingleOpeningProperties singleOpeningProperties, IEnumerable<string> dayTypeNames, out string refusal)
        {
            return ApertureTypeDefinition(singleOpeningProperties, dayTypeNames, out string _, out refusal);
        }

        /// <summary>
        /// Per child opening property, the 1-based occurrence of that child's definition among the children
        /// preceding it - the ordinal half of the <c>(definition, ordinal)</c> reuse key.
        /// <para>
        /// <b>Why an element's identical children still get distinct types.</b> TAS keeps ONE entry per
        /// aperture type on a building element, so assigning one type twice would silently collapse a
        /// two-opening window into a one-opening window. Two identical children are therefore two
        /// occurrences - <c>"… "</c> and <c>"… 2"</c> - and cross-element sharing is untouched: every other
        /// window with two identical children uses those same two types.
        /// </para>
        /// <para>
        /// A child whose definition could not be resolved contributes null and takes no ordinal; its write
        /// is refused, so it can neither claim nor displace an occurrence.
        /// </para>
        /// </summary>
        public static List<int> ApertureTypeOrdinals(IEnumerable<ApertureTypeDefinition> apertureTypeDefinitions)
        {
            List<int> result = new List<int>();

            if (apertureTypeDefinitions == null)
            {
                return result;
            }

            List<ApertureTypeDefinition> seen = new List<ApertureTypeDefinition>();
            foreach (ApertureTypeDefinition apertureTypeDefinition in apertureTypeDefinitions)
            {
                if (apertureTypeDefinition == null)
                {
                    result.Add(-1);
                    continue;
                }

                int ordinal = 1;
                foreach (ApertureTypeDefinition previous in seen)
                {
                    if (previous != null && previous.Equals(apertureTypeDefinition))
                    {
                        ordinal++;
                    }
                }

                result.Add(ordinal);
                seen.Add(apertureTypeDefinition);
            }

            return result;
        }

        /// <summary>
        /// The reuse ordinal a legacy 1-based child <paramref name="index"/> implies for a single-child
        /// write, or -1 when it implies none.
        /// <para>
        /// <b>Why the compatibility overload needs this.</b> A caller that writes an element's children one
        /// by one through <c>SetApertureType</c>'s legacy <c>index</c> parameter cannot supply the sibling
        /// set <see cref="ApertureTypeOrdinals(IEnumerable{ApertureTypeDefinition})"/> computes the true
        /// per-definition occurrence from. Position is therefore used AS the occurrence: position 1 is
        /// occurrence 1, position 2 is occurrence 2. That is exact for identical children - which is the
        /// case where the ordinal matters, because two identical children must never silently collapse
        /// into one assignment - and conservative for different children: the later one gets an occurrence
        /// higher than 1 and shares no type with occurrence 1, which over-splits by at most the child
        /// count rather than collapsing anything.
        /// </para>
        /// </summary>
        public static int ApertureTypeOrdinal(int index)
        {
            return index >= 1 ? index : -1;
        }
    }
}
