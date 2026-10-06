// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>The importer's relationship key: which SAM <see cref="ApertureConstruction"/> family a physical
        /// aperture belongs to.</b> It is the PAIR of TBD construction identities its two halves carry - the
        /// pane's and the frame's - and nothing else.
        /// <para>
        /// <b>Why a pair and not a name.</b> Stage 2 shares a definition BY VALUE, so two SAM
        /// <c>ApertureConstruction</c>s stating identical pane layers but different frame layers export as
        /// ONE shared pane construction (under whichever family created it) plus two frame constructions. The
        /// two families' panes are then indistinguishable by name, and the importer's old rule - pair a
        /// window's halves by the base name left after stripping <c>-pane</c>/<c>-frame</c> - put the second
        /// family's pane in the FIRST family's group and its frame in a group of its own. One window came
        /// back as two apertures, one frameless and one paneless. Keying on the pair makes that impossible:
        /// <c>(P, F1)</c> and <c>(P, F2)</c> are different families however they are named, and
        /// <c>(P, F1)</c> is the same family in every zone it appears in.
        /// </para>
        /// <para>
        /// <b>GUID first, name second.</b> A TBD construction's GUID is its identity; the name is a label
        /// that Stage 2 may legitimately have reclaimed or discriminated. A construction with no GUID (which
        /// some hand-authored TBDs carry) falls back to its name so it still keys as itself rather than
        /// colliding with every other GUID-less construction.
        /// </para>
        /// <para>
        /// A half that is absent - a frameless opening's frame - keys as empty, so "pane P, no frame" is its
        /// own family and never merges with "pane P, frame F". COM-free: the caller reads the two identities
        /// off the TBD and passes strings.
        /// </para>
        /// </summary>
        /// <param name="key_Pane">The pane half's construction GUID, or its name, or null/blank when absent.</param>
        /// <param name="key_Frame">The frame half's construction GUID, or its name, or null/blank when absent.</param>
        /// <returns>A stable key; never null.</returns>
        public static string ApertureConstructionPairKey(string key_Pane, string key_Frame)
        {
            return string.Format("{0}|{1}", Normalise(key_Pane), Normalise(key_Frame));

            string Normalise(string value)
            {
                return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
            }
        }

        /// <summary>
        /// The NAME to give a family the importer has just identified by
        /// <see cref="ApertureConstructionPairKey(string, string)"/>. Purely cosmetic - the family's identity
        /// is the pair, and this only decides what to call it.
        /// <para>
        /// <b>The pane's base name is preferred, then the frame's.</b> For an ordinary window whose two halves
        /// agree, both answers are the same one and this is exactly the name the importer has always
        /// produced. Where they DISAGREE, one of the two halves is a definition another family created and
        /// the other is this family's own, and the one already taken is the borrowed one - so falling through
        /// to the free name recovers the original family name in both directions:
        /// <c>(shared pane P named "A", frame F2 named "B")</c> takes "B" because "A" is already the
        /// <c>(P, F1)</c> family, and <c>(pane P2 named "D", shared frame F named "C")</c> takes "D" straight
        /// away.
        /// </para>
        /// <para>
        /// <b>A name is never shared between two families.</b> If both candidates are taken - two families
        /// that genuinely collide, which a merged or hand-edited TBD can produce - the preferred one is
        /// discriminated with the lowest free <c>~n</c>. The export's own collision handling would recover
        /// from a duplicate anyway; refusing to create one here means the model never carries two different
        /// aperture constructions under one name in the first place.
        /// </para>
        /// </summary>
        /// <param name="name_PaneBase">The pane construction's name with its <c>-pane</c> suffix stripped.</param>
        /// <param name="name_FrameBase">The frame construction's name with its <c>-frame</c> suffix stripped.</param>
        /// <param name="names_Taken">Every name already given to a DIFFERENT family. Null is none.</param>
        /// <returns>The name, or null when neither half states one.</returns>
        public static string ApertureConstructionName(string name_PaneBase, string name_FrameBase, IEnumerable<string> names_Taken)
        {
            HashSet<string> taken = new HashSet<string>(StringComparer.Ordinal);
            if (names_Taken != null)
            {
                foreach (string name in names_Taken)
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        taken.Add(name.Trim());
                    }
                }
            }

            List<string> candidates = new List<string>();
            Add(name_PaneBase);
            Add(name_FrameBase);

            if (candidates.Count == 0)
            {
                return null;
            }

            foreach (string candidate in candidates)
            {
                if (!taken.Contains(candidate))
                {
                    return candidate;
                }
            }

            string preferred = candidates[0];
            for (int index = 2; ; index++)
            {
                string discriminated = string.Format("{0}~{1}", preferred, index);
                if (!taken.Contains(discriminated))
                {
                    return discriminated;
                }
            }

            void Add(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                string name_Trimmed = name.Trim();
                if (!candidates.Contains(name_Trimmed))
                {
                    candidates.Add(name_Trimmed);
                }
            }
        }

        /// <summary>
        /// A TBD aperture construction's name with its <c>-pane</c>/<c>-frame</c> suffix stripped - the base
        /// the two halves of one window have always shared where nothing forced them apart.
        /// </summary>
        public static string ApertureConstructionNameBase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string result = name.Trim();

            //Fully qualified: inside Query, the bare name binds to the Query.AperturePart(int) METHOD.
            foreach (Analytical.AperturePart aperturePart in new Analytical.AperturePart[] { Analytical.AperturePart.Frame, Analytical.AperturePart.Pane })
            {
                string sufix = aperturePart.Sufix();
                if (string.IsNullOrEmpty(sufix) || !result.EndsWith(sufix))
                {
                    continue;
                }

                //The suffix is written " -pane", so the separator ahead of it goes too - exactly as
                //Convert.ToSAM_ApertureConstruction has always trimmed it.
                return result.Substring(0, result.Length - sufix.Length - 1).Trim();
            }

            return result;
        }
    }
}
