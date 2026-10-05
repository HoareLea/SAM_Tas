// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>What makes two TBD aperture controls the same control.</b> An immutable, COM-free value object
    /// over exactly the fields that decide whether one <c>TBD.ApertureType</c> may stand for two openings.
    /// <para>
    /// A <c>TBD.ApertureType</c> is a building-level REUSABLE DEFINITION, not a per-window object: the same
    /// type may be assigned to any number of building elements. Two hundred identical windows therefore
    /// need one aperture type and two hundred assignments, not two hundred types. This class is the
    /// equality that decides that, and it is deliberately the whole of it - a name takes no part, exactly
    /// as a schedule's name takes no part in <see cref="Query.ScheduleValuesEqual(IEnumerable{int}, IEnumerable{int})"/>.
    /// </para>
    /// <para>
    /// <b>Every field here is simulation identity.</b> Discharge coefficient, opening factor, profile mode,
    /// function text, the 24 schedule values and the day types the control applies on all change what TAS
    /// simulates. <see cref="Description"/> is round-trip identity: it is read back into
    /// <c>OpeningPropertiesParameter.Description</c> on import, so two otherwise-identical controls that
    /// describe themselves differently must stay two types or the round trip loses one of the descriptions.
    /// </para>
    /// <para>
    /// <b>Day-type membership is a first-class field</b> because the S1-C0 probe established it is readable:
    /// <c>TBD.IApertureType.GetDayType(int)</c> exists in the Interop.TBD metadata and licensed TAS confirms
    /// it reads back faithfully, in insertion order, and survives save/reopen. Membership is therefore
    /// compared as a SET - insertion order is an artefact of the order <c>SetDayType</c> happened to be
    /// called in, not a property of the control.
    /// </para>
    /// <para>
    /// <b>The factor stored here is the factor TBD carries</b>, i.e. after the Part O
    /// <c>AlwaysClosed -&gt; 0</c> override. Identity is what the simulation sees, not the reason it was
    /// written: an opening zeroed because it is always closed and an opening explicitly given factor 0 are
    /// the same control.
    /// </para>
    /// <para><b>Instances are immutable.</b> A shared definition is never rewritten - see the mutation-safety
    /// rule in the aperture-reuse design notes.</para>
    /// </summary>
    public sealed class ApertureTypeDefinition : IEquatable<ApertureTypeDefinition>
    {
        private readonly int[] scheduleValues;
        private readonly string[] dayTypeNames;

        /// <summary>
        /// The raw-values constructor. Used by the seed reader (which builds a definition out of an existing
        /// TBD aperture type) and by tests.
        /// </summary>
        /// <param name="dischargeCoefficient">As written to <c>ApertureType.dischargeCoefficient</c>, i.e. after <c>Convert.ToSingle</c>.</param>
        /// <param name="factor">As written to <c>profile.factor</c>, i.e. AFTER any AlwaysClosed override.</param>
        /// <param name="mode">Which of the three write shapes this control is.</param>
        /// <param name="function">The function text, or null. Kept only when <paramref name="mode"/> is <see cref="ApertureTypeProfileMode.Function"/>.</param>
        /// <param name="scheduleValues">Exactly <see cref="Query.ScheduleHourCount"/> values, or null when the control carries no schedule.</param>
        /// <param name="description">The description, or null. Empty and whitespace normalise to null - a TBD aperture type with no description reads back as an empty string, and that is the same control as one that never had one.</param>
        /// <param name="dayTypeNames">The names of the day types the control applies on. Order and duplicates are not significant.</param>
        public ApertureTypeDefinition(float dischargeCoefficient, float factor, ApertureTypeProfileMode mode, string function, IEnumerable<int> scheduleValues, string description, IEnumerable<string> dayTypeNames)
        {
            //Signed zero is normalised to positive zero. Under float equality -0f and +0f are EQUAL, while
            //the signature hashes their distinct IEEE-754 bit patterns, so without this an equal pair could
            //produce different hash codes and break the dictionary contract the GetHashCode remarks promise.
            //Zero is zero to the simulation either way; the name signature then also carries one stable bit
            //pattern for it. NaN is left untouched: NaN never equals NaN, so its bits may differ freely.
            DischargeCoefficient = NormalizeZero(dischargeCoefficient);
            Factor = NormalizeZero(factor);
            Mode = mode;

            //A function text is only part of the control in Function mode. Storing it in the other modes
            //would let a stale text that TBD never reads split one definition into two.
            Function = mode == ApertureTypeProfileMode.Function ? Normalize(function) : null;

            int[] values = scheduleValues?.ToArray();
            this.scheduleValues = values != null && values.Length == Query.ScheduleHourCount ? values : null;

            Description = Normalize(description);

            //Sorted and de-duplicated on the way in, so equality and the signature are both order-blind
            //without either having to sort at comparison time.
            this.dayTypeNames = dayTypeNames == null
                ? new string[0]
                : dayTypeNames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }

        /// <summary>The discharge coefficient, as the <c>float</c> TBD stores. Compared exactly; signed zero is normalised to positive zero.</summary>
        public float DischargeCoefficient { get; }

        /// <summary>The opening factor TBD carries, after any AlwaysClosed override. Compared exactly; signed zero is normalised to positive zero.</summary>
        public float Factor { get; }

        /// <summary>Which of the three write shapes this control is.</summary>
        public ApertureTypeProfileMode Mode { get; }

        /// <summary>The function text in <see cref="ApertureTypeProfileMode.Function"/> mode, otherwise null.</summary>
        public string Function { get; }

        /// <summary>The description, normalised so empty and absent are the same thing.</summary>
        public string Description { get; }

        /// <summary>Whether this control carries an availability schedule.</summary>
        public bool HasSchedule
        {
            get { return scheduleValues != null; }
        }

        /// <summary>
        /// The control's 24 hourly schedule values, or null when it carries no schedule. A copy - the stored
        /// array is never handed out.
        /// </summary>
        public int[] ScheduleValues
        {
            get { return scheduleValues == null ? null : (int[])scheduleValues.Clone(); }
        }

        /// <summary>
        /// The names of the day types this control applies on, sorted and de-duplicated. Never null; empty
        /// means the control applies on no day type at all.
        /// </summary>
        public string[] DayTypeNames
        {
            get { return (string[])dayTypeNames.Clone(); }
        }

        public bool Equals(ApertureTypeDefinition other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            //Exact float comparison, deliberately: both sides have been through the same Convert.ToSingle,
            //so a tolerance would only ever merge two controls the model states as different.
            if (DischargeCoefficient != other.DischargeCoefficient || Factor != other.Factor)
            {
                return false;
            }

            if (Mode != other.Mode || !string.Equals(Function, other.Function, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(Description, other.Description, StringComparison.Ordinal))
            {
                return false;
            }

            if (HasSchedule != other.HasSchedule)
            {
                return false;
            }

            if (HasSchedule && !Query.ScheduleValuesEqual(scheduleValues, other.scheduleValues))
            {
                return false;
            }

            //Both arrays are already sorted and de-duplicated, so a sequence comparison IS the set
            //comparison the probe showed is needed (TAS reports membership in insertion order).
            return dayTypeNames.Length == other.dayTypeNames.Length && dayTypeNames.SequenceEqual(other.dayTypeNames, StringComparer.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ApertureTypeDefinition);
        }

        /// <summary>
        /// Consistent with <see cref="Equals(ApertureTypeDefinition)"/>, and derived from the same
        /// deterministic signature the naming uses rather than from <c>string.GetHashCode</c> - so a
        /// definition used as a dictionary key behaves the same on every runtime and build. Reuse itself
        /// never depends on this: the lookup is a full equality scan, exactly as the schedule lookup is.
        /// </summary>
        public override int GetHashCode()
        {
            return unchecked((int)Query.Fnv1a(Query.ApertureTypeSignature(this) ?? string.Empty));
        }

        public override string ToString()
        {
            return Query.ApertureTypeSignature(this) ?? "ApertureTypeDefinition";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static float NormalizeZero(float value)
        {
            return value == 0 ? 0f : value;
        }
    }
}
