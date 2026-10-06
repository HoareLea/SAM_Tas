// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        /// <summary>
        /// The identity schema marker for the objects PR2 materialises into its working copy of the PR1
        /// graph. <b>Bump it only for a deliberate, breaking change to what the key is made of.</b>
        /// <para>
        /// <c>static readonly</c> rather than <c>const</c>, which the compiler would inline into every
        /// consuming assembly, leaving an unrebuilt assembly deriving keys under the old marker while
        /// believing it was current.
        /// </para>
        /// </summary>
        public static readonly string SystemVentilationIdentitySchema = "SystemVentilationConversion:v1";

        /// <summary>
        /// Namespace for the derivation, so a key PR2 derives can only ever collide with another key PR2
        /// derives - never with a real model guid, and never with one of PR1's materialised identities,
        /// which use a namespace of their own.
        /// </summary>
        private static readonly Guid guid_Namespace_SystemVentilation = new Guid("b41c7d59-0e26-4a8f-9d13-5fa8c2071e64");

        /// <summary>
        /// A deterministic guid for one object PR2 materialises - a leg's duty-carrying damper, or one of
        /// the two connections that put it in the leg - derived from what the object <i>is</i> and never
        /// from a name, an enumeration index or a position in a file.
        /// <para>
        /// <b>The convention is PR1's, deliberately mirrored rather than shared:</b> namespace bytes,
        /// then the schema, then the domain, then every component UTF-8 and length-prefixed (null as
        /// length -1, so it is distinct from empty, normalised to NFC), SHA-256, first sixteen bytes
        /// stamped version 8 and the RFC 4122 variant. PR1's own helper is <c>internal</c> to
        /// <c>SAM.Analytical.Systems</c> and that repository is frozen for this change, so it cannot be
        /// called; mirroring the convention under a different namespace guid keeps the two key spaces
        /// provably disjoint, which sharing one would not.
        /// </para>
        /// <para>
        /// The hash is a spreading function, not a security primitive. It is unsalted on purpose: the
        /// whole value of it is that the same graph converted twice, in any enumeration order, produces
        /// the same identities.
        /// </para>
        /// </summary>
        /// <param name="domain">The kind of object being keyed - "DutyCarrier", "CarrierConnectionIn", …</param>
        /// <param name="components">The identity components, in a fixed order.</param>
        public static Guid SystemVentilationGuid(string domain, params string[] components)
        {
            List<byte> bytes = new List<byte>(guid_Namespace_SystemVentilation.ToByteArray());

            Append(bytes, SystemVentilationIdentitySchema);
            Append(bytes, domain);

            AppendLength(bytes, components == null ? 0 : components.Length);

            if (components != null)
            {
                foreach (string component in components)
                {
                    Append(bytes, component);
                }
            }

            byte[] hash;

            using (global::System.Security.Cryptography.SHA256 sHA256 = global::System.Security.Cryptography.SHA256.Create())
            {
                hash = sHA256.ComputeHash(bytes.ToArray());
            }

            byte[] result = new byte[16];
            Array.Copy(hash, result, 16);

            result[7] = (byte)((result[7] & 0x0F) | 0x80);
            result[8] = (byte)((result[8] & 0x3F) | 0x80);

            return new Guid(result);
        }

        /// <summary>A guid as a key component, formatted invariantly so it goes through one encoding rule.</summary>
        public static string SystemVentilationGuidComponent(Guid guid)
        {
            return guid.ToString("D", CultureInfo.InvariantCulture);
        }

        private static void Append(List<byte> bytes, string text)
        {
            if (text == null)
            {
                AppendLength(bytes, -1);
                return;
            }

            string text_Canonical;

            try
            {
                text_Canonical = text.Normalize(NormalizationForm.FormC);
            }
            catch (ArgumentException)
            {
                text_Canonical = text;
            }

            byte[] bytes_Text = Encoding.UTF8.GetBytes(text_Canonical);

            AppendLength(bytes, bytes_Text.Length);
            bytes.AddRange(bytes_Text);
        }

        /// <summary>
        /// Writes an int as four bytes, least significant first, explicitly rather than through
        /// <c>BitConverter</c> - which is endian-dependent, and a key must not depend on the architecture
        /// that derived it.
        /// </summary>
        private static void AppendLength(List<byte> bytes, int value)
        {
            unchecked
            {
                bytes.Add((byte)value);
                bytes.Add((byte)(value >> 8));
                bytes.Add((byte)(value >> 16));
                bytes.Add((byte)(value >> 24));
            }
        }
    }
}
