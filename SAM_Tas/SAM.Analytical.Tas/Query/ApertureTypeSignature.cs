// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The numeric format every generated aperture-type NAME renders a discharge coefficient and an
        /// opening factor in - human-readable display text, e.g. <c>Opening Cd0.62 F1</c>. Fixed here so
        /// the name a definition resolves to is the same string on every machine and every export -
        /// <see cref="CultureInfo.InvariantCulture"/> is used with it for the same reason.
        /// <para>
        /// This is DISPLAY rounding only and plays no part in identity. The signature below carries the
        /// exact IEEE-754 bit pattern of each float instead, so two TAS float definitions that merely
        /// round to the same display text - 0.6201 and 0.6202 - never share a collision identity.
        /// </para>
        /// </summary>
        public const string ApertureTypeNumberFormat = "0.###";

        /// <summary>
        /// A deterministic, build-stable fingerprint of an <see cref="ApertureTypeDefinition"/>, used to
        /// generate and to collision-resolve TBD aperture type NAMES - never to decide reuse. Reuse is
        /// decided by full field equality, exactly as schedule reuse is decided by the 24 values.
        /// <para>
        /// Shape: <c>Cd&lt;cd bits&gt; F&lt;factor bits&gt;[ S&lt;schedule signature&gt;][ Fn&lt;hash&gt;] D&lt;hash&gt; DT&lt;hash&gt;</c>,
        /// e.g. <c>Cd3F1EB852 F3F800000 S00FFFE D811C9DC5 DT…</c> - the discharge coefficient and factor
        /// are their exact <see cref="float"/> bit patterns (see <see cref="SingleBitsHex(float)"/>), not
        /// the rounded <see cref="ApertureTypeNumberFormat"/> display text, so two floats equality keeps
        /// apart can never collide here. The schedule component reuses
        /// <see cref="ScheduleSignature(IEnumerable{int})"/> so one definition of that 24-bit mask serves
        /// both names.
        /// </para>
        /// <para>
        /// The description and day-type components are always present, even when empty, so that the
        /// signature of a definition without them cannot be confused with a prefix of one that has them.
        /// Day types are part of the signature because the S1-C0 probe found membership readable, which
        /// makes it part of simulation identity and therefore something a collision suffix has to be able
        /// to tell apart.
        /// </para>
        /// <para>
        /// Hashes are FNV-1a computed arithmetically, never <c>GetHashCode</c>, which is not stable across
        /// runtimes or builds and must never decide a name persisted into a TBD.
        /// </para>
        /// </summary>
        public static string ApertureTypeSignature(this ApertureTypeDefinition apertureTypeDefinition)
        {
            if (apertureTypeDefinition == null)
            {
                return null;
            }

            List<string> parts = new List<string>
            {
                string.Format("Cd{0}", SingleBitsHex(apertureTypeDefinition.DischargeCoefficient)),
                string.Format("F{0}", SingleBitsHex(apertureTypeDefinition.Factor))
            };

            if (apertureTypeDefinition.HasSchedule)
            {
                parts.Add(string.Format("S{0}", ScheduleSignature(apertureTypeDefinition.ScheduleValues) ?? "??????"));
            }

            if (apertureTypeDefinition.Mode == ApertureTypeProfileMode.Function)
            {
                parts.Add(string.Format("Fn{0}", Fnv1aHex(apertureTypeDefinition.Function ?? string.Empty)));
            }

            parts.Add(string.Format("D{0}", Fnv1aHex(apertureTypeDefinition.Description ?? string.Empty)));
            parts.Add(string.Format("DT{0}", Fnv1aHex(string.Join("", apertureTypeDefinition.DayTypeNames))));

            return string.Join(" ", parts);
        }

        /// <summary>
        /// The eight-hex-digit discriminator appended to a preferred aperture type name when that name is
        /// already taken by a DIFFERENT definition. Derived from the full signature - which carries the
        /// exact float bit patterns - so the same definition resolves to the same discriminator on every
        /// repeated export, and two definitions that round to the same display text never share one.
        /// </summary>
        public static string ApertureTypeSignatureHash(this ApertureTypeDefinition apertureTypeDefinition)
        {
            string signature = ApertureTypeSignature(apertureTypeDefinition);

            return signature == null ? null : Fnv1aHex(signature);
        }

        /// <summary>
        /// The exact IEEE-754 bit pattern of a <see cref="float"/>, as eight uppercase hexadecimal digits.
        /// <para>
        /// Identity for a TAS-stored float is the stored bit pattern, not any rounded rendering of it:
        /// two discharge coefficients such as 0.6201 and 0.6202 display identically at
        /// <see cref="ApertureTypeNumberFormat"/> (<c>Cd0.62</c>) yet are different controls, so the
        /// signature feeding the deterministic collision hash is built from the bits. Assembled
        /// explicitly little-endian so the same value renders identically on every runtime and build -
        /// the same property the arithmetic <see cref="Fnv1a(string)"/> has and <c>GetHashCode</c> does
        /// not.
        /// </para>
        /// </summary>
        private static string SingleBitsHex(float value)
        {
            byte[] bytes = global::System.BitConverter.GetBytes(value);
            uint bits = global::System.BitConverter.IsLittleEndian
                ? (uint)bytes[0] | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16) | ((uint)bytes[3] << 24)
                : (uint)bytes[3] | ((uint)bytes[2] << 8) | ((uint)bytes[1] << 16) | ((uint)bytes[0] << 24);

            return bits.ToString("X8", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// FNV-1a over the UTF-16 code units of <paramref name="text"/>, low byte first. Arithmetic, so it
        /// is identical on every runtime and build - the property <c>GetHashCode</c> does not have and the
        /// reason it is never used for a name that ends up persisted in a TBD.
        /// </summary>
        public static uint Fnv1a(string text)
        {
            uint hash = 2166136261;

            if (text == null)
            {
                return hash;
            }

            foreach (char character in text)
            {
                hash = unchecked((hash ^ (uint)(character & 0xFF)) * 16777619);
                hash = unchecked((hash ^ (uint)((character >> 8) & 0xFF)) * 16777619);
            }

            return hash;
        }

        /// <summary><see cref="Fnv1a(string)"/> rendered as eight uppercase hexadecimal digits.</summary>
        public static string Fnv1aHex(string text)
        {
            return Fnv1a(text).ToString("X8");
        }
    }
}
