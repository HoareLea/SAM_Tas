// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>Which of these names carry a PHYSICAL aperture GUID</b> - an exact test against the model's own
        /// aperture GUIDs, not a pattern guess.
        /// <para>
        /// This is the one thing that separates a reusable definition from an instance-named object, and the
        /// gbXML route needs it twice. TAS's own gbXML conversion names the constructions and building
        /// elements it creates after the T3D window, which carries the aperture GUID
        /// (<c>SAM.Analytical.gbXML.Convert.TogbXML(Aperture, …)</c> writes it into the opening name, and it has
        /// to - <c>Query.UpdateT3D</c> decodes it back to find the SAM aperture). Such an object:
        /// </para>
        /// <list type="bullet">
        /// <item><b>must never be ADOPTED as a reusable definition.</b> Its content may well match what a
        /// window asks for, and the reuse cache would hand it over - leaving twenty windows sharing one
        /// element named after whichever one of them happened to be first. Sharing and instance-naming are
        /// mutually exclusive: an instance-named definition can never be found again by anything but itself;</item>
        /// <item><b>is the thing to sweep up</b> once every surface has been rebound onto the shared
        /// definitions.</item>
        /// </list>
        /// <para>
        /// Both spellings a GUID realistically reaches a generated name in are tested - hyphenated
        /// (<c>D</c>) and bare (<c>N</c>) - case-insensitively, since neither TAS nor the gbXML writer
        /// guarantees a case.
        /// </para>
        /// </summary>
        /// <param name="names">The names to test.</param>
        /// <param name="apertureGuids">The model's physical aperture GUIDs.</param>
        /// <returns>Those names that carry one, in input order, without duplicates. Never null.</returns>
        public static List<string> NamesContainingApertureGuid(IEnumerable<string> names, IEnumerable<Guid> apertureGuids)
        {
            List<string> result = new List<string>();

            if (names == null || apertureGuids == null)
            {
                return result;
            }

            HashSet<string> guidTexts = new HashSet<string>();
            foreach (Guid apertureGuid in apertureGuids)
            {
                guidTexts.Add(apertureGuid.ToString("D").ToUpperInvariant());
                guidTexts.Add(apertureGuid.ToString("N").ToUpperInvariant());
            }

            if (guidTexts.Count == 0)
            {
                return result;
            }

            HashSet<string> seen = new HashSet<string>();
            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                {
                    continue;
                }

                string name_Upper = name.ToUpperInvariant();
                if (guidTexts.Any(x => name_Upper.Contains(x)))
                {
                    result.Add(name);
                }
            }

            return result;
        }
    }
}
