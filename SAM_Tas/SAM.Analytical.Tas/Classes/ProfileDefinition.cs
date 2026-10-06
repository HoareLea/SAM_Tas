// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>What makes two imported SAM <see cref="Profile"/>s the same reusable definition.</b> An immutable,
    /// COM-free value object over exactly the two things a SAM <c>ProfileLibrary</c> entry is: its
    /// <see cref="Category"/> and its complete flattened values.
    /// <para>
    /// A SAM <c>Profile</c> is a library-level REUSABLE DEFINITION, not a per-zone object - a native SAM model
    /// already shares one profile across every <c>InternalCondition</c> that references it. The TBD import,
    /// however, minted one profile per TBD internal-condition slot and named it
    /// <c>"{internal condition} [{TAS profile}]"</c>, so a two-zone building carrying one activity produced two
    /// copies of every schedule. This class is the equality that collapses them, and it is deliberately the
    /// whole of it.
    /// </para>
    /// <para>
    /// <b>No physical or zone identity takes part.</b> Not the TAS internal-condition name, not the space name,
    /// not the SAM <c>Profile</c> Guid, not the order the building happened to be walked in. Those are
    /// properties of a PLACE; a reusable definition is a property of a SHAPE. Mixing the two is exactly what
    /// made one profile per zone unavoidable.
    /// </para>
    /// <para>
    /// <b>Values are compared by exact IEEE-754 bit pattern</b>, with two normalisations applied on the way in:
    /// <c>-0.0</c> becomes <c>0.0</c> (the simulation cannot tell them apart, and leaving the sign in would let
    /// two equal profiles hash differently), and every NaN becomes the one canonical <see cref="double.NaN"/>
    /// pattern (so a definition carrying a NaN still equals itself and still signs deterministically, which raw
    /// IEEE NaN semantics would not give). No tolerance is applied: both sides come from the same TAS read, so a
    /// tolerance could only ever merge two profiles the model states as different.
    /// </para>
    /// <para>
    /// <b>The value COUNT is part of identity.</b> A one-value profile and a 24-value profile of all the same
    /// number are different shapes, and TAS writes them back as different profile types
    /// (<c>ticValueProfile</c> vs <c>ticHourlyProfile</c>).
    /// </para>
    /// <para>
    /// <b>A zero-length definition is not reusable.</b> TAS function profiles read back with no values at all
    /// (see <c>SAM.Core.Tas.Query.Values</c>), so their flattened form is an incomplete representation of the
    /// profile and merging by it would be unsafe. <see cref="ProfileReuseIndex"/> excludes them from dedup and
    /// keeps their existing per-internal-condition import untouched.
    /// </para>
    /// <para><b>Instances are immutable.</b> A shared definition is never rewritten.</para>
    /// </summary>
    public sealed class ProfileDefinition : IEquatable<ProfileDefinition>, IComparable<ProfileDefinition>
    {
        private readonly double[] values;
        private readonly int hashCode;

        /// <param name="category">
        /// The SAM <c>Profile.Category</c> string, normalised exactly as the <see cref="Profile"/> constructor
        /// normalises it (see <see cref="Query.ProfileCategory(string)"/>) so that this category and the category
        /// of the <see cref="Profile"/> built from this definition are always the same string - which is what
        /// makes the <c>ProfileLibrary</c> key <c>"{Category}::{Name}"</c> predictable from here.
        /// </param>
        /// <param name="values">The complete flattened values. Null and empty are the same thing: a definition that is not reusable.</param>
        public ProfileDefinition(string category, IEnumerable<double> values)
        {
            Category = Query.ProfileCategory(category);

            double[] array = values == null ? new double[0] : values.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = NormalizeValue(array[i]);
            }

            this.values = array;

            //Derived from the same deterministic signature the naming uses rather than from string/array
            //GetHashCode, so a definition used as a dictionary key behaves the same on every runtime and build.
            //Reuse itself never depends on this: the lookup is a full equality comparison.
            hashCode = unchecked((int)Query.Fnv1a(Query.ProfileSignature(Category, this.values)));
        }

        /// <summary>The SAM profile category, normalised. Compared ordinally, never by culture.</summary>
        public string Category { get; }

        /// <summary>The number of flattened values. Zero means the definition is not reusable - see the class remarks.</summary>
        public int Count
        {
            get { return values.Length; }
        }

        /// <summary>Whether this definition may be shared at all. False for the zero-length (TAS function profile) case.</summary>
        public bool IsReusable
        {
            get { return values.Length != 0; }
        }

        /// <summary>The complete flattened values, normalised. A copy - the stored array is never handed out.</summary>
        public double[] Values
        {
            get { return (double[])values.Clone(); }
        }

        public bool Equals(ProfileDefinition other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (!string.Equals(Category, other.Category, StringComparison.Ordinal))
            {
                return false;
            }

            if (values.Length != other.values.Length)
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                //Bit comparison, not ==: after the constructor's normalisation this is exact IEEE-754 equality
                //with signed zero unified and NaN self-equal, which is precisely the rule the class states.
                if (BitConverter.DoubleToInt64Bits(values[i]) != BitConverter.DoubleToInt64Bits(other.values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ProfileDefinition);
        }

        /// <summary>
        /// Consistent with <see cref="Equals(ProfileDefinition)"/> and computed once in the constructor. Derived
        /// from <see cref="Query.ProfileSignature(string, IEnumerable{double})"/>, so it is identical on every
        /// runtime and build.
        /// </summary>
        public override int GetHashCode()
        {
            return hashCode;
        }

        /// <summary>
        /// A deterministic TOTAL order over definitions - category ordinally, then value count, then the exact
        /// value bit patterns element by element.
        /// <para>
        /// This exists so that the naming pass can claim names in an order that depends only on the definitions
        /// themselves and never on the order the building was walked in. Unlike
        /// <see cref="Query.ProfileSignature(string, IEnumerable{double})"/>, which is a bounded fingerprint and
        /// so not injective, this is a genuine total order: two definitions compare equal here only when they are
        /// equal under <see cref="Equals(ProfileDefinition)"/>.
        /// </para>
        /// </summary>
        public int CompareTo(ProfileDefinition other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            if (ReferenceEquals(other, this))
            {
                return 0;
            }

            int result = string.CompareOrdinal(Category, other.Category);
            if (result != 0)
            {
                return result;
            }

            result = values.Length.CompareTo(other.values.Length);
            if (result != 0)
            {
                return result;
            }

            for (int i = 0; i < values.Length; i++)
            {
                result = BitConverter.DoubleToInt64Bits(values[i]).CompareTo(BitConverter.DoubleToInt64Bits(other.values[i]));
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        public override string ToString()
        {
            return Query.ProfileSignature(Category, values);
        }

        /// <summary>
        /// <c>-0.0</c> to <c>0.0</c>, and every NaN payload to the one canonical <see cref="double.NaN"/> bit
        /// pattern. See the class remarks for why both are required.
        /// </summary>
        private static double NormalizeValue(double value)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            return value == 0 ? 0d : value;
        }
    }
}
