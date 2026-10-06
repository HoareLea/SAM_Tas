// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// A deterministic, build-stable fingerprint of a <see cref="ProfileDefinition"/>, used to generate and
        /// to collision-resolve imported SAM profile NAMES - never to decide reuse. Reuse is decided by full
        /// definitional equality, exactly as aperture-type reuse is
        /// (<see cref="ApertureTypeSignature(ApertureTypeDefinition)"/>).
        /// <para>
        /// Shape: <c>C&lt;category hash&gt; N&lt;value count&gt; V&lt;value hash&gt;</c>, e.g.
        /// <c>C1A2B3C4D N24 V0F1E2D3C</c>. The value component is a hash rather than the values themselves
        /// because a yearly profile carries 8760 of them and a name discriminator has to stay short; the count
        /// is carried in full so two definitions of different length can never share a fingerprint.
        /// </para>
        /// <para>
        /// <b>This fingerprint is deliberately not injective.</b> Two distinct definitions may in principle hash
        /// alike, and then they both prefer the same discriminated name - which
        /// <see cref="ProfileName(ICollection{string}, ProfileDefinition, string)"/> resolves by extending
        /// deterministically rather than by overwriting or refusing. Nothing about reuse rests on it.
        /// </para>
        /// <para>
        /// Hashes are FNV-1a computed arithmetically, never <c>GetHashCode</c>, which is not stable across
        /// runtimes or builds and must never decide a name persisted into a model.
        /// </para>
        /// </summary>
        public static string ProfileSignature(this ProfileDefinition profileDefinition)
        {
            return profileDefinition == null ? null : ProfileSignature(profileDefinition.Category, profileDefinition.Values);
        }

        /// <summary>
        /// The primitive behind <see cref="ProfileSignature(ProfileDefinition)"/>, over the two parts directly.
        /// Used by the <see cref="ProfileDefinition"/> constructor itself, which cannot hand out a definition
        /// it has not finished building.
        /// </summary>
        /// <param name="category">The definition's normalised category.</param>
        /// <param name="values">
        /// The values as the definition stores them, i.e. already normalised (<c>-0.0</c> to <c>0.0</c>, NaN
        /// canonicalised). Passing un-normalised values would produce a signature that two EQUAL definitions
        /// disagree on.
        /// </param>
        public static string ProfileSignature(string category, IEnumerable<double> values)
        {
            uint hash = 2166136261;
            int count = 0;

            if (values != null)
            {
                foreach (double value in values)
                {
                    hash = Fnv1a(hash, BitConverter.DoubleToInt64Bits(value));
                    count++;
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "C{0} N{1} V{2}",
                Fnv1aHex(category ?? string.Empty),
                count.ToString(CultureInfo.InvariantCulture),
                hash.ToString("X8", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// The eight-hex-digit discriminator appended to a preferred imported profile name when that name is
        /// already claimed by a DIFFERENT definition. Derived from the full signature - which carries the exact
        /// value bit patterns - so the same definition resolves to the same discriminator on every repeated
        /// import, and encounter order plays no part.
        /// </summary>
        public static string ProfileSignatureHash(this ProfileDefinition profileDefinition)
        {
            string signature = ProfileSignature(profileDefinition);

            return signature == null ? null : Fnv1aHex(signature);
        }

        /// <summary>
        /// FNV-1a folded over the eight bytes of an IEEE-754 bit pattern, low byte first. Arithmetic, so it is
        /// identical on every runtime and build - the same property <see cref="Fnv1a(string)"/> has and
        /// <c>GetHashCode</c> does not.
        /// </summary>
        private static uint Fnv1a(uint hash, long bits)
        {
            ulong unsigned = unchecked((ulong)bits);

            for (int i = 0; i < 8; i++)
            {
                hash = unchecked((hash ^ (uint)((unsigned >> (i * 8)) & 0xFF)) * 16777619);
            }

            return hash;
        }
    }
}
