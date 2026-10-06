// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The index of the first definition in <paramref name="existingDefinitions"/> equal to
        /// <paramref name="apertureTypeDefinition"/>, or -1 if none is.
        /// <para>
        /// <b>Identity is the definition, never the name.</b> The mirror of
        /// <see cref="ScheduleIndex(IEnumerable{int[]}, IEnumerable{int})"/>, and for the same reason: a
        /// <c>TBD.ApertureType</c> is building-level and shared across every element that needs that
        /// control, so two hundred identical windows must find the one type the first of them created
        /// rather than each create their own.
        /// </para>
        /// <para>
        /// A null entry is a seeded aperture type this export could not read or must not reuse. It never
        /// matches - its name still occupies the namespace for collision purposes, and nothing else.
        /// </para>
        /// <para>
        /// First match wins, so the result is stable for a given building ordering and the operation is
        /// idempotent: a second export finds the type the first one created.
        /// </para>
        /// </summary>
        public static int ApertureTypeIndex(IEnumerable<ApertureTypeDefinition> existingDefinitions, ApertureTypeDefinition apertureTypeDefinition)
        {
            if (existingDefinitions == null || apertureTypeDefinition == null)
            {
                return -1;
            }

            int index = 0;
            foreach (ApertureTypeDefinition existing in existingDefinitions)
            {
                if (existing != null && existing.Equals(apertureTypeDefinition))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}
