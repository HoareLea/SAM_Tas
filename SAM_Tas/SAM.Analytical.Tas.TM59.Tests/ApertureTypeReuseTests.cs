// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using ApertureTypeDefinition = SAM.Analytical.Tas.ApertureTypeDefinition;
using ApertureTypeProfileMode = SAM.Analytical.Tas.ApertureTypeProfileMode;
using ApertureTypeReconciliation = SAM.Analytical.Tas.ApertureTypeReconciliation;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The SAM -&gt; TBD aperture-control seam: definition equality, deterministic naming, ordinal
    /// multiplicity, and the guarantee that reusing a shared type writes nothing.</b>
    /// <para>
    /// A <c>TBD.ApertureType</c> is a building-level REUSABLE DEFINITION, assignable to any number of
    /// building elements. The decision that governs whether an export creates two hundred of them or one is
    /// therefore <b>definition equality</b>, never the name - the same discipline schedule reuse already
    /// applies to 24 values, one level up. These tests exercise that decision, the collision-safe naming
    /// that follows it, the ordinal key that keeps an element's two identical openings two openings, and
    /// the mutation-safety rule: <b>a shared definition is immutable, so reuse writes nothing at all.</b>
    /// </para>
    /// <para>
    /// <b>Why these tests need no installed TAS.</b> Everything that decides - <c>ApertureTypeDefinition</c>,
    /// <c>TasQuery.ApertureTypeSignature</c>, <c>ApertureTypeIndex</c>, <c>ApertureTypeName</c>,
    /// <c>ApertureTypeOrdinals</c>, <c>ApertureTypeReconciliation</c> and the COM-free definition factory -
    /// names no TAS COM type. What genuinely needs COM is the write itself, and that is modelled here by
    /// hand-written fakes that RECORD EVERY PROPERTY SET, in the same style as <c>FakeTBDSchedule</c> in
    /// <see cref="OpeningScheduleResolutionTests"/>. The write log is what makes "reuse touches nothing" a
    /// test rather than a claim.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ApertureTypeReuseTests
    {
        /// <summary>
        /// The day types every control this export writes is assigned to - the building's day types bar
        /// HDD and CDD, which is what the write has always excluded.
        /// </summary>
        private static readonly string[] DayTypes = { "Weekday", "Saturday", "Sunday" };

        // -------------------------------------------------------------------------------------------------
        // Builders
        // -------------------------------------------------------------------------------------------------

        private static bool[] BoolWindow(int from, int to)
        {
            bool[] values = new bool[24];
            for (int hour = 0; hour < 24; hour++)
            {
                values[hour] = from <= to ? (hour >= from && hour < to) : (hour >= from || hour < to);
            }

            return values;
        }

        private static DailyAvailabilitySchedule Schedule(string name, int from, int to)
        {
            return new DailyAvailabilitySchedule(name, BoolWindow(from, to));
        }

        private static ApertureTypeDefinition Definition(
            float dischargeCoefficient = 0.62f,
            float factor = 1f,
            ApertureTypeProfileMode mode = ApertureTypeProfileMode.Plain,
            string function = null,
            IEnumerable<int> scheduleValues = null,
            string description = null,
            IEnumerable<string> dayTypeNames = null)
        {
            return new ApertureTypeDefinition(dischargeCoefficient, factor, mode, function, scheduleValues, description, dayTypeNames ?? DayTypes);
        }

        private static int[] Values(int from, int to)
        {
            return BoolWindow(from, to).Select(x => x ? 1 : 0).ToArray();
        }

        // -------------------------------------------------------------------------------------------------
        // Definition equality - each field flips it independently, the name never does
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void Equality_TwoIdenticalDefinitions_AreEqual()
        {
            Assert.That(Definition(), Is.EqualTo(Definition()));
            Assert.That(Definition().GetHashCode(), Is.EqualTo(Definition().GetHashCode()));
        }

        [Test]
        public void Equality_DifferentDischargeCoefficient_AreNotEqual()
        {
            Assert.That(Definition(dischargeCoefficient: 0.62f), Is.Not.EqualTo(Definition(dischargeCoefficient: 0.55f)),
                "Cd changes the flow through the opening, so it is simulation identity");
        }

        [Test]
        public void Equality_DifferentFactor_AreNotEqual()
        {
            Assert.That(Definition(factor: 1f), Is.Not.EqualTo(Definition(factor: 0.5f)));
        }

        [Test]
        public void Equality_DifferentFunction_AreNotEqual()
        {
            ApertureTypeDefinition definition_1 = Definition(mode: ApertureTypeProfileMode.Function, function: "TA>24");
            ApertureTypeDefinition definition_2 = Definition(mode: ApertureTypeProfileMode.Function, function: "TA>26");

            Assert.That(definition_1, Is.Not.EqualTo(definition_2));
        }

        [Test]
        public void Equality_DifferentScheduleValues_AreNotEqual()
        {
            ApertureTypeDefinition definition_1 = Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Values(8, 23));
            ApertureTypeDefinition definition_2 = Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Values(7, 22));

            Assert.That(definition_1, Is.Not.EqualTo(definition_2));
        }

        [Test]
        public void Equality_ScheduleVersusNoSchedule_AreNotEqual()
        {
            ApertureTypeDefinition definition_Scheduled = Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Values(8, 23));

            Assert.That(definition_Scheduled, Is.Not.EqualTo(Definition()));
        }

        [Test]
        public void Equality_SameScheduleValuesDifferentScheduleName_AreEqual()
        {
            //Names take no part in identity, at the schedule level or at this one.
            ApertureTypeDefinition definition_1 = Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Schedule("PartO_DayOpen_08_23", 8, 23).ScheduleValues());
            ApertureTypeDefinition definition_2 = Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Schedule("Somebody elses name", 8, 23).ScheduleValues());

            Assert.That(definition_1, Is.EqualTo(definition_2));
        }

        [Test]
        public void Equality_DifferentMode_AreNotEqual()
        {
            Assert.That(Definition(mode: ApertureTypeProfileMode.Plain), Is.Not.EqualTo(Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Values(8, 23))));
        }

        [Test]
        public void Equality_DifferentDescription_AreNotEqual()
        {
            Assert.That(Definition(description: "Bedroom openable"), Is.Not.EqualTo(Definition(description: "Kitchen openable")),
                "the description round-trips back into OpeningPropertiesParameter.Description, so merging two would lose one");
        }

        /// <summary>
        /// A TBD aperture type with no description reads back as an empty string, not as null. That is the
        /// same control as one that never carried a description, and treating it otherwise would refuse to
        /// reuse a type this very export created.
        /// </summary>
        [Test]
        public void Equality_EmptyDescriptionAndNoDescription_AreEqual()
        {
            Assert.That(Definition(description: ""), Is.EqualTo(Definition(description: null)));
            Assert.That(Definition(description: "   "), Is.EqualTo(Definition(description: null)));
        }

        /// <summary>
        /// The function text is only part of the control in Function mode. A stale text TBD never reads must
        /// not split one definition into two.
        /// </summary>
        [Test]
        public void Equality_FunctionTextOutsideFunctionMode_IsIgnored()
        {
            Assert.That(Definition(mode: ApertureTypeProfileMode.Plain, function: "TA>24"), Is.EqualTo(Definition(mode: ApertureTypeProfileMode.Plain)));
        }

        [Test]
        public void Equality_AgainstNull_IsFalse()
        {
            Assert.That(Definition().Equals(null), Is.False);
            Assert.That(Definition().Equals((object)"not a definition"), Is.False);
        }

        // -------------------------------------------------------------------------------------------------
        // Day-type membership - the S1-C0 probe branch
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>S1-C0 outcome A: day-type membership is READABLE, so it is an equality field.</b>
        /// <c>TBD.IApertureType.GetDayType(int)</c> exists in the Interop.TBD metadata, and licensed TAS
        /// confirmed it reads back exactly what <c>SetDayType</c> wrote, survives save/reopen, reports an
        /// unassigned type as empty and does not duplicate a repeated write. Two controls that apply on
        /// different days are therefore different controls.
        /// </summary>
        [Test]
        public void Equality_DifferentDayTypeMembership_AreNotEqual()
        {
            Assert.That(Definition(dayTypeNames: new[] { "Weekday", "Saturday", "Sunday" }),
                Is.Not.EqualTo(Definition(dayTypeNames: new[] { "Weekday" })));
        }

        /// <summary>
        /// The probe also established that TAS reports membership in the order <c>SetDayType</c> was called
        /// in, NOT in calendar order - two aperture types with the same membership written in opposite
        /// orders read back in opposite orders. Membership is therefore compared as a SET; comparing
        /// sequences would split one definition into two for no reason at all.
        /// </summary>
        [Test]
        public void Equality_DayTypeMembershipWrittenInDifferentOrders_AreEqual()
        {
            Assert.That(Definition(dayTypeNames: new[] { "Weekday", "Sunday" }),
                Is.EqualTo(Definition(dayTypeNames: new[] { "Sunday", "Weekday" })));
        }

        [Test]
        public void Equality_DuplicatedDayTypeName_IsIgnored()
        {
            //TAS does not duplicate an entry when SetDayType is called twice for the same day type.
            Assert.That(Definition(dayTypeNames: new[] { "Weekday", "Weekday" }), Is.EqualTo(Definition(dayTypeNames: new[] { "Weekday" })));
        }

        [Test]
        public void Equality_NoDayTypesAtAll_IsItsOwnDefinition()
        {
            Assert.That(Definition(dayTypeNames: new string[0]), Is.Not.EqualTo(Definition()));
        }

        // -------------------------------------------------------------------------------------------------
        // Signature - deterministic, and distinct where the definition is
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void Signature_IsDeterministic()
        {
            Assert.That(TasQuery.ApertureTypeSignature(Definition()), Is.EqualTo(TasQuery.ApertureTypeSignature(Definition())));
        }

        [Test]
        public void Signature_CarriesThePartODefaultScheduleMask()
        {
            string signature = TasQuery.ApertureTypeSignature(Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Schedule("PartO_DayOpen_08_23", 8, 23).ScheduleValues()));

            Assert.That(signature, Does.Contain("S00FFFE"), "the default Part O 08:00-23:00 window is the 24-bit mask 00FFFE");
        }

        [Test]
        public void Signature_DiffersForEveryFieldTheDefinitionDiffersOn()
        {
            List<ApertureTypeDefinition> definitions = new List<ApertureTypeDefinition>
            {
                Definition(),
                Definition(dischargeCoefficient: 0.55f),
                Definition(factor: 0.5f),
                Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Values(8, 23)),
                Definition(mode: ApertureTypeProfileMode.Function, function: "TA>24"),
                Definition(description: "Bedroom openable"),
                Definition(dayTypeNames: new[] { "Weekday" })
            };

            List<string> signatures = definitions.Select(x => TasQuery.ApertureTypeSignature(x)).ToList();

            Assert.That(signatures.Distinct().Count(), Is.EqualTo(signatures.Count), string.Join(" | ", signatures));
        }

        [Test]
        public void Signature_OfNull_IsNull()
        {
            Assert.That(TasQuery.ApertureTypeSignature(null), Is.Null);
            Assert.That(TasQuery.ApertureTypeSignatureHash(null), Is.Null);
        }

        [Test]
        public void Fnv1aHex_IsArithmeticAndStable()
        {
            //Pinned, so a change of runtime or of the hash itself is a test failure rather than a silent
            //renaming of every aperture type in every existing TBD.
            Assert.That(TasQuery.Fnv1aHex(""), Is.EqualTo("811C9DC5"));
            Assert.That(TasQuery.Fnv1aHex("a"), Is.EqualTo(TasQuery.Fnv1aHex("a")));
            Assert.That(TasQuery.Fnv1aHex("a"), Is.Not.EqualTo(TasQuery.Fnv1aHex("b")));
            Assert.That(TasQuery.Fnv1aHex("a").Length, Is.EqualTo(8));
        }

        // -------------------------------------------------------------------------------------------------
        // Exact float identity - the display name rounds, the collision identity never does
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// 0.6201 and 0.6202 round to the same display text (<c>Cd0.62</c>) but are different TAS float
        /// definitions: equality stays exact float equality.
        /// </summary>
        [Test]
        public void Equality_CloseFloatsThatShareADisplayText_AreNotEqual()
        {
            Assert.That(Definition(dischargeCoefficient: 0.6201f), Is.Not.EqualTo(Definition(dischargeCoefficient: 0.6202f)));
            Assert.That(Definition(factor: 1.0001f), Is.Not.EqualTo(Definition(factor: 1.0002f)));
        }

        /// <summary>
        /// -0f and +0f are EQUAL under float comparison, so they must hash alike too. The signature hashes
        /// raw IEEE-754 bit patterns, which differ for signed zero, so the constructor normalises it to
        /// positive zero - equality and GetHashCode then agree, and the name signature carries one stable
        /// bit pattern for zero.
        /// </summary>
        [Test]
        public void Equality_SignedZero_NormalisesBeforeEqualityAndHashing()
        {
            Assert.That(Definition(dischargeCoefficient: -0f), Is.EqualTo(Definition(dischargeCoefficient: 0f)));
            Assert.That(Definition(dischargeCoefficient: -0f).GetHashCode(), Is.EqualTo(Definition(dischargeCoefficient: 0f).GetHashCode()));

            Assert.That(Definition(factor: -0f), Is.EqualTo(Definition(factor: 0f)));
            Assert.That(Definition(factor: -0f).GetHashCode(), Is.EqualTo(Definition(factor: 0f).GetHashCode()));

            Assert.That(TasQuery.ApertureTypeSignature(Definition(factor: -0f)), Is.EqualTo(TasQuery.ApertureTypeSignature(Definition(factor: 0f))));
        }

        /// <summary>The human-readable name keeps its rounded text for the close values.</summary>
        [Test]
        public void Name_CloseFloatsThatShareADisplayText_KeepTheReadableName()
        {
            Assert.That(TasQuery.ApertureTypeName(new string[0], Definition(dischargeCoefficient: 0.6201f), 1, out string _), Is.EqualTo("Opening Cd0.62 F1"));
            Assert.That(TasQuery.ApertureTypeName(new string[0], Definition(dischargeCoefficient: 0.6202f), 1, out string _), Is.EqualTo("Opening Cd0.62 F1"));
            Assert.That(TasQuery.ApertureTypeName(new string[0], Definition(factor: 1.0001f), 1, out string _), Is.EqualTo("Opening Cd0.62 F1"));
        }

        /// <summary>
        /// ...but the deterministic collision identity is derived from the exact TAS-stored float, so two
        /// definitions that merely round alike never resolve to the same one.
        /// </summary>
        [Test]
        public void Signature_CloseFloatsThatShareADisplayText_Differ()
        {
            ApertureTypeDefinition definition_1 = Definition(dischargeCoefficient: 0.6201f);
            ApertureTypeDefinition definition_2 = Definition(dischargeCoefficient: 0.6202f);

            Assert.That(TasQuery.ApertureTypeSignature(definition_1), Is.Not.EqualTo(TasQuery.ApertureTypeSignature(definition_2)));
            Assert.That(TasQuery.ApertureTypeSignatureHash(definition_1), Is.Not.EqualTo(TasQuery.ApertureTypeSignatureHash(definition_2)),
                "the collision discriminator is derived from the exact float, never from the rounded display text");

            Assert.That(TasQuery.ApertureTypeSignatureHash(Definition(factor: 1.0001f)),
                Is.Not.EqualTo(TasQuery.ApertureTypeSignatureHash(Definition(factor: 1.0002f))));
        }

        /// <summary>The signature carries the raw IEEE-754 bit pattern, pinned on well-known values.</summary>
        [Test]
        public void Signature_CarriesTheExactSingleBitPattern()
        {
            string signature = TasQuery.ApertureTypeSignature(Definition(dischargeCoefficient: 0.5f, factor: 1f));

            Assert.That(signature, Does.Contain("Cd3F000000"), "0.5f is 0x3F000000");
            Assert.That(signature, Does.Contain("F3F800000"), "1.0f is 0x3F800000");
            Assert.That(signature, Does.Not.Contain("0.5"), "no rounded display text leaks into the identity");
        }

        /// <summary>
        /// The collision case the exact identity exists for: the preferred (rounded) name is taken, and
        /// two distinct close-float definitions must still land on two distinct deterministic names.
        /// </summary>
        [Test]
        public void Name_CloseFloatCollision_QualifiesWithDistinctDeterministicHashes()
        {
            ApertureTypeDefinition definition_1 = Definition(dischargeCoefficient: 0.6201f);
            ApertureTypeDefinition definition_2 = Definition(dischargeCoefficient: 0.6202f);

            string qualified_1 = TasQuery.ApertureTypeName(new[] { "Opening Cd0.62 F1" }, definition_1, 1, out string refusal_1);
            string qualified_2 = TasQuery.ApertureTypeName(new[] { "Opening Cd0.62 F1", qualified_1 }, definition_2, 1, out string refusal_2);

            Assert.That(refusal_1, Is.Null);
            Assert.That(refusal_2, Is.Null);
            Assert.That(qualified_1, Does.StartWith("Opening Cd0.62 F1_"));
            Assert.That(qualified_2, Does.StartWith("Opening Cd0.62 F1_"));
            Assert.That(qualified_1, Is.Not.EqualTo(qualified_2), "two distinct TAS float definitions never resolve to the same collision identity");
            Assert.That(TasQuery.ApertureTypeName(new[] { "Opening Cd0.62 F1" }, definition_1, 1, out string _), Is.EqualTo(qualified_1), "and each is deterministic");
        }

        // -------------------------------------------------------------------------------------------------
        // Index - first equal definition wins, unreadable entries never match
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void Index_FindsTheFirstEqualDefinition()
        {
            List<ApertureTypeDefinition> existing = new List<ApertureTypeDefinition>
            {
                Definition(dischargeCoefficient: 0.55f),
                Definition(),
                Definition()
            };

            Assert.That(TasQuery.ApertureTypeIndex(existing, Definition()), Is.EqualTo(1));
        }

        [Test]
        public void Index_NoMatch_IsMinusOne()
        {
            Assert.That(TasQuery.ApertureTypeIndex(new[] { Definition(dischargeCoefficient: 0.55f) }, Definition()), Is.EqualTo(-1));
            Assert.That(TasQuery.ApertureTypeIndex(null, Definition()), Is.EqualTo(-1));
            Assert.That(TasQuery.ApertureTypeIndex(new[] { Definition() }, null), Is.EqualTo(-1));
        }

        /// <summary>
        /// A null entry is a seeded aperture type this export may not reuse - unreadable, sheltered, a
        /// profile shape it does not write. It must never match: its name occupies the namespace and it
        /// takes no other part.
        /// </summary>
        [Test]
        public void Index_NonReusableEntry_NeverMatches()
        {
            Assert.That(TasQuery.ApertureTypeIndex(new ApertureTypeDefinition[] { null, Definition() }, Definition()), Is.EqualTo(1));
        }

        // -------------------------------------------------------------------------------------------------
        // Naming - deterministic, description-based, collision-safe, and free of aperture identity
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void Name_UsesTheDescriptionAsItsBase()
        {
            string name = TasQuery.ApertureTypeName(new string[0], Definition(description: "Bedroom openable"), 1, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(name, Does.StartWith("Bedroom openable Cd0.62 F1"));
        }

        [Test]
        public void Name_WithNoDescription_FallsBackToOpening()
        {
            string name = TasQuery.ApertureTypeName(new string[0], Definition(), 1, out string _);

            Assert.That(name, Is.EqualTo("Opening Cd0.62 F1"));
        }

        [Test]
        public void Name_WithSchedule_CarriesTheScheduleSignature()
        {
            string name = TasQuery.ApertureTypeName(new string[0], Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Values(8, 23)), 1, out string _);

            Assert.That(name, Is.EqualTo("Opening Cd0.62 F1 S00FFFE"));
        }

        [Test]
        public void Name_IsDeterministic()
        {
            Assert.That(TasQuery.ApertureTypeName(new string[0], Definition(description: "Bedroom openable"), 1, out string _),
                Is.EqualTo(TasQuery.ApertureTypeName(new string[0], Definition(description: "Bedroom openable"), 1, out string _)));
        }

        [Test]
        public void Name_SecondOccurrence_CarriesTheOrdinal()
        {
            Assert.That(TasQuery.ApertureTypeName(new string[0], Definition(), 1, out string _), Is.EqualTo("Opening Cd0.62 F1"));
            Assert.That(TasQuery.ApertureTypeName(new string[0], Definition(), 2, out string _), Is.EqualTo("Opening Cd0.62 F1 2"));
            Assert.That(TasQuery.ApertureTypeName(new string[0], Definition(), 3, out string _), Is.EqualTo("Opening Cd0.62 F1 3"));
        }

        /// <summary>
        /// A name is only ever derived after a DEFINITION search has failed, so an existing name collision
        /// is necessarily with a different control. The suffix is the definition's own signature hash -
        /// deterministic, so a repeated export resolves to the very same name rather than accumulating
        /// <c>(1)</c>, <c>(2)</c>, ...
        /// </summary>
        [Test]
        public void Name_CollidingWithADifferentDefinition_IsSignatureQualified()
        {
            ApertureTypeDefinition definition = Definition(description: "Bedroom openable");
            string preferred = TasQuery.ApertureTypeName(new string[0], definition, 1, out string _);

            string qualified = TasQuery.ApertureTypeName(new[] { preferred }, definition, 1, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(qualified, Is.EqualTo(string.Format("{0}_{1}", preferred, TasQuery.ApertureTypeSignatureHash(definition))));
            Assert.That(qualified, Is.EqualTo(TasQuery.ApertureTypeName(new[] { preferred }, definition, 1, out string _)), "and it is stable");
        }

        [Test]
        public void Name_BothPreferredAndQualifiedTaken_Refuses()
        {
            ApertureTypeDefinition definition = Definition();
            string preferred = TasQuery.ApertureTypeName(new string[0], definition, 1, out string _);
            string qualified = string.Format("{0}_{1}", preferred, TasQuery.ApertureTypeSignatureHash(definition));

            string name = TasQuery.ApertureTypeName(new[] { preferred, qualified }, definition, 1, out string refusal);

            Assert.That(name, Is.Null);
            Assert.That(refusal, Does.Contain(preferred).And.Contain(qualified));
        }

        [Test]
        public void Name_OfNullDefinition_Refuses()
        {
            Assert.That(TasQuery.ApertureTypeName(new string[0], null, 1, out string refusal), Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        /// <summary>
        /// <b>The whole point of the rename.</b> The previous convention named an aperture type after its
        /// building element, and a building element is named <c>"Windows: &lt;name&gt; &lt;guid&gt; -pane"</c>.
        /// A name carrying an aperture GUID can never be found again by the next identical window, so
        /// sharing was impossible while it stood. No generated name may contain any part of it.
        /// </summary>
        [Test]
        public void Name_NeverContainsPhysicalApertureIdentity()
        {
            System.Guid guid = System.Guid.NewGuid();
            string elementName = string.Format("Windows: W01 {0} -pane", guid);

            foreach (ApertureTypeDefinition definition in new[]
            {
                Definition(),
                Definition(description: "Bedroom openable"),
                Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Values(8, 23), description: "Bedroom openable")
            })
            {
                foreach (int ordinal in new[] { 1, 2 })
                {
                    string name = TasQuery.ApertureTypeName(new string[0], definition, ordinal, out string _);

                    Assert.That(name, Does.Not.Contain(guid.ToString()));
                    Assert.That(name, Does.Not.Contain(elementName));
                    Assert.That(name, Does.Not.Contain("Windows:"));
                }
            }
        }

        [Test]
        public void NameBase_SanitisesAndBounds()
        {
            Assert.That(TasQuery.ApertureTypeNameBase("  Bedroom   openable  "), Is.EqualTo("Bedroom openable"));
            Assert.That(TasQuery.ApertureTypeNameBase("with_underscore"), Is.EqualTo("withunderscore"), "the underscore is the collision suffix's own separator");
            Assert.That(TasQuery.ApertureTypeNameBase(null), Is.EqualTo(TasQuery.ApertureTypeNameBase_Default));
            Assert.That(TasQuery.ApertureTypeNameBase("   "), Is.EqualTo(TasQuery.ApertureTypeNameBase_Default));
            Assert.That(TasQuery.ApertureTypeNameBase(new string('x', 200)).Length, Is.EqualTo(TasQuery.ApertureTypeNameBaseLimit));
        }

        // -------------------------------------------------------------------------------------------------
        // Name decomposition - who is likely to have written a name, and at which ordinal
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void Decomposition_RecognisesEveryNameThisExportGenerates()
        {
            foreach (ApertureTypeDefinition definition in new[]
            {
                Definition(),
                Definition(description: "Bedroom openable"),
                Definition(mode: ApertureTypeProfileMode.ScheduleOnly, scheduleValues: Values(8, 23)),
                Definition(factor: 0f)
            })
            {
                foreach (int ordinal in new[] { 1, 2, 3 })
                {
                    string name = TasQuery.ApertureTypeName(new string[0], definition, ordinal, out string _);

                    Assert.That(TasQuery.TryDecomposeApertureTypeName(name, out string _, out int ordinal_Read), Is.True, name);
                    Assert.That(ordinal_Read, Is.EqualTo(ordinal), name);
                }
            }
        }

        [Test]
        public void Decomposition_RecognisesACollisionQualifiedName()
        {
            ApertureTypeDefinition definition = Definition();
            string preferred = TasQuery.ApertureTypeName(new string[0], definition, 2, out string _);
            string qualified = TasQuery.ApertureTypeName(new[] { preferred }, definition, 2, out string _);

            Assert.That(TasQuery.TryDecomposeApertureTypeName(qualified, out string _, out int ordinal), Is.True, qualified);
            Assert.That(ordinal, Is.EqualTo(2));
        }

        [Test]
        public void Decomposition_RejectsAForeignName()
        {
            Assert.That(TasQuery.TryDecomposeApertureTypeName("My openable window", out string _, out int _), Is.False);
            Assert.That(TasQuery.TryDecomposeApertureTypeName("Windows: W01 -pane", out string _, out int _), Is.False);
            Assert.That(TasQuery.TryDecomposeApertureTypeName(null, out string _, out int _), Is.False);
        }

        [Test]
        public void LegacyName_IsTheElementNameOrTheElementNamePlusAChildIndex()
        {
            const string elementName = "Windows: W01 5f2c -pane";

            Assert.That(TasQuery.IsLegacyApertureTypeName(elementName, elementName), Is.True);
            Assert.That(TasQuery.IsLegacyApertureTypeName(elementName + " 1", elementName), Is.True);
            Assert.That(TasQuery.IsLegacyApertureTypeName(elementName + " 2", elementName), Is.True);

            Assert.That(TasQuery.IsLegacyApertureTypeName(elementName + " x", elementName), Is.False);
            Assert.That(TasQuery.IsLegacyApertureTypeName("Opening Cd0.62 F1", elementName), Is.False);
            Assert.That(TasQuery.IsLegacyApertureTypeName(null, elementName), Is.False);
        }

        // -------------------------------------------------------------------------------------------------
        // COM-free definition factory - the resolution the write performs before touching COM
        // -------------------------------------------------------------------------------------------------

        private static PartOOpeningProperties PartO(OpeningRestriction openingRestriction = OpeningRestriction.Unrestricted)
        {
            return new PartOOpeningProperties(1.2, 1.0, 30.0, openingRestriction);
        }

        [Test]
        public void Factory_UnrestrictedPartO_IsPlainWithNoSchedule()
        {
            ApertureTypeDefinition definition = PartO().ApertureTypeDefinition(DayTypes, out string name_Schedule, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Mode, Is.EqualTo(ApertureTypeProfileMode.Plain));
            Assert.That(definition.HasSchedule, Is.False);
            Assert.That(name_Schedule, Is.Null);
            Assert.That(definition.DayTypeNames, Is.EqualTo(new[] { "Saturday", "Sunday", "Weekday" }));
        }

        [Test]
        public void Factory_NightClosedPartO_IsScheduleOnlyCarryingThePartOMask()
        {
            ApertureTypeDefinition definition = PartO(OpeningRestriction.NightClosed).ApertureTypeDefinition(DayTypes, out string name_Schedule, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(definition.Mode, Is.EqualTo(ApertureTypeProfileMode.ScheduleOnly));
            Assert.That(definition.HasSchedule, Is.True);
            Assert.That(definition.ScheduleValues, Is.EqualTo(Values(8, 23)));
            Assert.That(name_Schedule, Is.EqualTo("PartO_DayOpen_08_23"));
        }

        /// <summary>
        /// Identity is what TBD carries, not why it carries it. An AlwaysClosed opening has its factor
        /// zeroed by the write, so it is the same control as one the model gives factor 0 outright - and
        /// must share its aperture type, not get one of its own.
        /// </summary>
        [Test]
        public void Factory_AlwaysClosed_IsTheSameDefinitionAsAnExplicitZeroFactor()
        {
            ApertureTypeDefinition definition_AlwaysClosed = PartO(OpeningRestriction.AlwaysClosed).ApertureTypeDefinition(DayTypes, out string _);

            PartOOpeningProperties explicitZero = PartO();
            explicitZero.Factor = 0;
            ApertureTypeDefinition definition_Explicit = explicitZero.ApertureTypeDefinition(DayTypes, out string _);

            Assert.That(definition_AlwaysClosed.Factor, Is.EqualTo(0f));
            Assert.That(definition_AlwaysClosed, Is.EqualTo(definition_Explicit));
        }

        [Test]
        public void Factory_FunctionAndSchedule_IsFunctionModeWithTheScheduleStillKeyed()
        {
            ProfileOpeningProperties openingProperties = new ProfileOpeningProperties(0.62, Schedule("PartO_DayOpen_08_23", 8, 23));
            openingProperties.SetValue(OpeningPropertiesParameter.Function, "TA>24");

            ApertureTypeDefinition definition = openingProperties.ApertureTypeDefinition(DayTypes, out string _);

            Assert.That(definition.Mode, Is.EqualTo(ApertureTypeProfileMode.Function));
            Assert.That(definition.Function, Is.EqualTo("TA>24"));
            Assert.That(definition.HasSchedule, Is.True, "a function and a schedule are not mutually exclusive - the schedule stays on as an availability multiplier");
        }

        [Test]
        public void Factory_Description_IsCarriedIntoTheDefinition()
        {
            ProfileOpeningProperties openingProperties = new ProfileOpeningProperties(0.62);
            openingProperties.SetValue(OpeningPropertiesParameter.Description, "Bedroom openable");

            ApertureTypeDefinition definition = openingProperties.ApertureTypeDefinition(DayTypes, out string _);

            Assert.That(definition.Description, Is.EqualTo("Bedroom openable"));
        }

        /// <summary>
        /// An opening that STATES a schedule it cannot supply is refused here, before any COM object has
        /// been read or created - the same guarantee the write has always made.
        /// </summary>
        [Test]
        public void Factory_UnusableScheduleSource_Refuses()
        {
            ProfileOpeningProperties openingProperties = new ProfileOpeningProperties(0.62, new Profile("Empty", ProfileGroup.Ventilation.Text()));

            ApertureTypeDefinition definition = openingProperties.ApertureTypeDefinition(DayTypes, out string name_Schedule, out string refusal);

            Assert.That(definition, Is.Null);
            Assert.That(refusal, Is.Not.Null);
            Assert.That(name_Schedule, Is.Null);
        }

        [Test]
        public void Factory_NullOpeningProperties_Refuses()
        {
            Assert.That(((ISingleOpeningProperties)null).ApertureTypeDefinition(DayTypes, out string refusal), Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        // -------------------------------------------------------------------------------------------------
        // Ordinals - an element's identical children stay separate openings
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void Ordinals_IdenticalChildren_AreSuccessiveOccurrences()
        {
            List<int> ordinals = TasQuery.ApertureTypeOrdinals(new[] { Definition(), Definition(), Definition() });

            Assert.That(ordinals, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Ordinals_ChildrenAAB_AreOneTwoOne()
        {
            ApertureTypeDefinition a = Definition();
            ApertureTypeDefinition b = Definition(dischargeCoefficient: 0.55f);

            Assert.That(TasQuery.ApertureTypeOrdinals(new[] { a, a, b }), Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.That(TasQuery.ApertureTypeOrdinals(new[] { a, b, a }), Is.EqualTo(new[] { 1, 1, 2 }));
        }

        [Test]
        public void Ordinals_UnresolvedChild_TakesNoOccurrence()
        {
            ApertureTypeDefinition a = Definition();

            Assert.That(TasQuery.ApertureTypeOrdinals(new[] { a, null, a }), Is.EqualTo(new[] { 1, -1, 2 }));
        }

        [Test]
        public void Ordinals_OfNull_IsEmpty()
        {
            Assert.That(TasQuery.ApertureTypeOrdinals(null), Is.Empty);
        }

        /// <summary>
        /// The compatibility overload derives its reuse ordinal from the legacy 1-based child index: any
        /// position 1 or later is an occurrence, anything else means the first.
        /// </summary>
        [Test]
        public void Ordinal_IndexDerived_IsThePosition()
        {
            Assert.That(TasQuery.ApertureTypeOrdinal(-1), Is.EqualTo(-1));
            Assert.That(TasQuery.ApertureTypeOrdinal(0), Is.EqualTo(-1));
            Assert.That(TasQuery.ApertureTypeOrdinal(1), Is.EqualTo(1));
            Assert.That(TasQuery.ApertureTypeOrdinal(2), Is.EqualTo(2));
        }

        // -------------------------------------------------------------------------------------------------
        // Reconciliation - what may happen to the aperture types an element already carried
        // -------------------------------------------------------------------------------------------------

        private const string ElementName = "Windows: W01 5f2c9d1e-0000-0000-0000-000000000001 -pane";

        private static List<KeyValuePair<string, ApertureTypeDefinition>> Assigned(params KeyValuePair<string, ApertureTypeDefinition>[] entries)
        {
            return new List<KeyValuePair<string, ApertureTypeDefinition>>(entries);
        }

        private static KeyValuePair<string, ApertureTypeDefinition> Entry(string name, ApertureTypeDefinition definition)
        {
            return new KeyValuePair<string, ApertureTypeDefinition>(name, definition);
        }

        [Test]
        public void Reconciliation_NothingAssigned_IsCreate()
        {
            ApertureTypeReconciliation reconciliation = TasQuery.ApertureTypeReconciliation(ElementName, Assigned(), Definition(), 1, out int index, out string refusal);

            Assert.That(reconciliation, Is.EqualTo(ApertureTypeReconciliation.Create));
            Assert.That(index, Is.EqualTo(-1));
            Assert.That(refusal, Is.Null);
        }

        [Test]
        public void Reconciliation_AllTypesNamedAfterTheElement_IsLegacy()
        {
            ApertureTypeReconciliation reconciliation = TasQuery.ApertureTypeReconciliation(
                ElementName,
                Assigned(Entry(ElementName + " 1", Definition(dischargeCoefficient: 0.5f)), Entry(ElementName + " 2", null)),
                Definition(),
                1,
                out int _,
                out string refusal);

            Assert.That(reconciliation, Is.EqualTo(ApertureTypeReconciliation.Legacy), "a per-element name carries the aperture GUID, so those types are exclusive to this element");
            Assert.That(refusal, Is.Null);
        }

        [Test]
        public void Reconciliation_AnAssignedTypeIsAlreadyThisControl_IsReuseWithNoWrites()
        {
            ApertureTypeReconciliation reconciliation = TasQuery.ApertureTypeReconciliation(
                ElementName,
                Assigned(Entry("Opening Cd0.62 F1", Definition())),
                Definition(),
                1,
                out int index,
                out string refusal);

            Assert.That(reconciliation, Is.EqualTo(ApertureTypeReconciliation.Reuse));
            Assert.That(index, Is.EqualTo(0));
            Assert.That(refusal, Is.Null);
        }

        /// <summary>
        /// An element carrying two copies of one control hands its first child the first and its second
        /// child the second. Matching on the definition alone would give both children the same type, and
        /// TAS keeps one entry per type - so the element would silently lose an opening.
        /// </summary>
        [Test]
        public void Reconciliation_TwoAssignedCopiesOfOneControl_AreClaimedByOrdinal()
        {
            List<KeyValuePair<string, ApertureTypeDefinition>> assigned = Assigned(
                Entry("Opening Cd0.62 F1", Definition()),
                Entry("Opening Cd0.62 F1 2", Definition()));

            Assert.That(TasQuery.ApertureTypeReconciliation(ElementName, assigned, Definition(), 1, out int index_1, out string _), Is.EqualTo(ApertureTypeReconciliation.Reuse));
            Assert.That(index_1, Is.EqualTo(0));

            Assert.That(TasQuery.ApertureTypeReconciliation(ElementName, assigned, Definition(), 2, out int index_2, out string _), Is.EqualTo(ApertureTypeReconciliation.Reuse));
            Assert.That(index_2, Is.EqualTo(1));
        }

        [Test]
        public void Reconciliation_OnlyOneAssignedCopyButTwoNeeded_RefusesTheSecond()
        {
            ApertureTypeReconciliation reconciliation = TasQuery.ApertureTypeReconciliation(
                ElementName,
                Assigned(Entry("Opening Cd0.62 F1", Definition())),
                Definition(),
                2,
                out int _,
                out string refusal);

            Assert.That(reconciliation, Is.EqualTo(ApertureTypeReconciliation.Refuse));
            Assert.That(refusal, Does.Contain("Opening Cd0.62 F1"));
        }

        /// <summary>
        /// A shared type that does not say what this opening says is the one genuinely unsafe case: adding
        /// a second would double the ventilation, and rewriting the first would change the control of every
        /// other element referencing it. Both are refused, and the stale type is named so it can be dealt
        /// with in TAS.
        /// </summary>
        [Test]
        public void Reconciliation_StaleSharedType_Refuses()
        {
            ApertureTypeReconciliation reconciliation = TasQuery.ApertureTypeReconciliation(
                ElementName,
                Assigned(Entry("Opening Cd0.55 F1", Definition(dischargeCoefficient: 0.55f))),
                Definition(),
                1,
                out int _,
                out string refusal);

            Assert.That(reconciliation, Is.EqualTo(ApertureTypeReconciliation.Refuse));
            Assert.That(refusal, Does.Contain("Opening Cd0.55 F1"));
            Assert.That(refusal, Does.Contain(ElementName));
        }

        /// <summary>
        /// An unrecognised name is somebody else's work. It is left exactly as it is, and the requested
        /// control is added alongside it - the coexistence the previous write already produced.
        /// </summary>
        [Test]
        public void Reconciliation_ForeignUserAuthoredType_IsCreateAlongside()
        {
            ApertureTypeReconciliation reconciliation = TasQuery.ApertureTypeReconciliation(
                ElementName,
                Assigned(Entry("My hand-made trickle vent", Definition(dischargeCoefficient: 0.3f))),
                Definition(),
                1,
                out int _,
                out string refusal);

            Assert.That(reconciliation, Is.EqualTo(ApertureTypeReconciliation.Create));
            Assert.That(refusal, Is.Null);
        }

        [Test]
        public void Reconciliation_MixedLegacyAndShared_IsNotLegacy()
        {
            ApertureTypeReconciliation reconciliation = TasQuery.ApertureTypeReconciliation(
                ElementName,
                Assigned(Entry(ElementName, Definition(dischargeCoefficient: 0.5f)), Entry("Opening Cd0.55 F1", Definition(dischargeCoefficient: 0.55f))),
                Definition(),
                1,
                out int _,
                out string _);

            Assert.That(reconciliation, Is.EqualTo(ApertureTypeReconciliation.Refuse), "the legacy fence needs every assigned type to be element-exclusive");
        }

        [Test]
        public void Reconciliation_NoDefinition_Refuses()
        {
            Assert.That(TasQuery.ApertureTypeReconciliation(ElementName, Assigned(), null, 1, out int _, out string refusal), Is.EqualTo(ApertureTypeReconciliation.Refuse));
            Assert.That(refusal, Is.Not.Null);
        }

        // =================================================================================================
        // Fake-COM: the write itself, with every property set recorded
        // =================================================================================================

        /// <summary>
        /// A stand-in for a <c>TBD.profile</c> that RECORDS every property set. The recording is the point:
        /// a shared definition is immutable, so the test for reuse is that this log does not grow - not that
        /// it grows with the same values.
        /// </summary>
        private sealed class FakeTBDProfile
        {
            private float value;
            private float factor = 1;
            private float setbackValue;
            private int type = 1; //ticValueProfile, the TBD default
            private string function = string.Empty;
            private int[] schedule;

            public List<string> WriteLog { get; } = new List<string>();

            /// <summary>Failure injection: the schedule assignment is recorded but not retained.</summary>
            public bool DropsSchedule { get; set; }

            public float Value
            {
                get { return value; }
                set { WriteLog.Add("profile.value"); this.value = value; }
            }

            public float Factor
            {
                get { return factor; }
                set { WriteLog.Add("profile.factor"); factor = value; }
            }

            public float SetbackValue
            {
                get { return setbackValue; }
                set { WriteLog.Add("profile.setbackValue"); setbackValue = value; }
            }

            public int Type
            {
                get { return type; }
                set { WriteLog.Add("profile.type"); type = value; }
            }

            public string Function
            {
                get { return function; }
                set { WriteLog.Add("profile.function"); function = value; }
            }

            public int[] Schedule
            {
                get { return schedule; }
                set { WriteLog.Add("profile.schedule"); schedule = DropsSchedule ? null : value; }
            }
        }

        /// <summary>A stand-in for a <c>TBD.ApertureType</c>, recording every property set.</summary>
        private sealed class FakeTBDApertureType
        {
            private string name;
            private string description = string.Empty;
            private float dischargeCoefficient;

            public FakeTBDProfile Profile { get; } = new FakeTBDProfile();

            public List<string> DayTypeNames { get; } = new List<string>();

            private readonly List<string> writeLog = new List<string>();

            /// <summary>Every property set on this aperture type OR on its profile.</summary>
            public List<string> WriteLog
            {
                get { return writeLog.Concat(Profile.WriteLog).ToList(); }
            }

            public string Name
            {
                get { return name; }
                set { writeLog.Add("apertureType.name"); name = value; }
            }

            public string Description
            {
                get { return description; }
                set { writeLog.Add("apertureType.description"); description = value; }
            }

            public float DischargeCoefficient
            {
                get { return dischargeCoefficient; }
                set { writeLog.Add("apertureType.dischargeCoefficient"); dischargeCoefficient = RoundsDischargeCoefficient ? (float)System.Math.Round(value, 2) : value; }
            }

            /// <summary>Failure injection: the store rounds the discharge coefficient to two decimals.</summary>
            public bool RoundsDischargeCoefficient { get; set; }

            /// <summary>What TAS does: idempotent, and the read-back order is the write order.</summary>
            public void SetDayType(string dayTypeName, bool add)
            {
                writeLog.Add("apertureType.SetDayType");

                if (add)
                {
                    if (!DayTypeNames.Contains(dayTypeName))
                    {
                        DayTypeNames.Add(dayTypeName);
                    }
                }
                else
                {
                    DayTypeNames.Remove(dayTypeName);
                }
            }

            /// <summary>The definition this type currently represents, as the seed reader would read it.</summary>
            public ApertureTypeDefinition Definition()
            {
                ApertureTypeProfileMode mode = Profile.Type == 4
                    ? ApertureTypeProfileMode.Function
                    : (Profile.Schedule != null ? ApertureTypeProfileMode.ScheduleOnly : ApertureTypeProfileMode.Plain);

                return new ApertureTypeDefinition(DischargeCoefficient, Profile.Factor, mode, Profile.Function, Profile.Schedule, Description, DayTypeNames);
            }
        }

        /// <summary>A stand-in for a <c>TBD.buildingElement</c>, recording every assignment.</summary>
        private sealed class FakeTBDBuildingElement
        {
            public FakeTBDBuildingElement(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public List<FakeTBDApertureType> ApertureTypes { get; } = new List<FakeTBDApertureType>();

            private List<FakeTBDApertureType> apertureTypes_Existing;

            /// <summary>
            /// What the element carried BEFORE this export touched it - captured on first touch, exactly as
            /// <c>BuildingReuseCache.ExistingAssignments</c> captures it. Reconciling against the live list
            /// instead would make an element's own first child look like a stale shared type to its second.
            /// </summary>
            public List<FakeTBDApertureType> ExistingApertureTypes
            {
                get { return apertureTypes_Existing ?? (apertureTypes_Existing = ApertureTypes.ToList()); }
            }

            /// <summary>Models re-opening the document: the next export's "already carried" is what is there now.</summary>
            public void Reopen()
            {
                apertureTypes_Existing = null;
            }

            public List<string> WriteLog { get; } = new List<string>();

            /// <summary>
            /// What TAS does, verified on licensed TAS: assigning a type an element already carries adds a
            /// SECOND entry. The production guard is what stops that.
            /// </summary>
            public void AssignApertureType(FakeTBDApertureType apertureType)
            {
                WriteLog.Add("buildingElement.AssignApertureType");
                ApertureTypes.Add(apertureType);
            }
        }

        /// <summary>
        /// A stand-in for a <c>TBD.schedule</c>: a name plus the 24 values. <see cref="Reusable"/> is the
        /// reservation half of the model: a schedule whose value write failed after it was created and
        /// named stays in the TBD (there is no <c>RemoveSchedule</c>) and can never be reused - but its
        /// name still occupies the namespace.
        /// </summary>
        private sealed class FakeTBDSchedule
        {
            public string Name { get; set; }

            public int[] Values { get; set; }

            public bool Reusable { get; set; } = true;
        }

        /// <summary>A stand-in for a <c>TBD.Building</c>'s reusable definitions.</summary>
        private sealed class FakeTBDBuilding
        {
            public List<FakeTBDApertureType> ApertureTypes { get; } = new List<FakeTBDApertureType>();

            public List<FakeTBDSchedule> Schedules { get; } = new List<FakeTBDSchedule>();

            public string[] DayTypeNames { get; } = DayTypes;

            /// <summary>Failure injection: the next created type's profile does not retain its schedule.</summary>
            public bool DropsNextSchedule { get; set; }

            /// <summary>Failure injection: the next created type stores a rounded discharge coefficient.</summary>
            public bool RoundsNextDischargeCoefficient { get; set; }

            /// <summary>Failure injection: the next created schedule does not persist its values.</summary>
            public bool FailsNextScheduleValues { get; set; }

            /// <summary>
            /// Types created this session whose write did not complete and verify - the mirror of a
            /// <c>BuildingReuseCache</c> reservation: never reusable, but the name stays in the namespace.
            /// </summary>
            public HashSet<FakeTBDApertureType> NonReusable { get; } = new HashSet<FakeTBDApertureType>();

            public FakeTBDApertureType AddApertureType()
            {
                FakeTBDApertureType apertureType = new FakeTBDApertureType();
                apertureType.Profile.DropsSchedule = DropsNextSchedule;
                apertureType.RoundsDischargeCoefficient = RoundsNextDischargeCoefficient;
                DropsNextSchedule = false;
                RoundsNextDischargeCoefficient = false;
                ApertureTypes.Add(apertureType);

                return apertureType;
            }
        }

        /// <summary>
        /// The reuse algorithm <c>Modify.SetApertureType</c> runs, with the COM objects replaced by the
        /// fakes above and every decision delegated to the very production helpers the export uses.
        /// </summary>
        private static FakeTBDApertureType Write(FakeTBDBuilding building, FakeTBDBuildingElement buildingElement, ISingleOpeningProperties singleOpeningProperties, int ordinal, out string refusal)
        {
            refusal = null;

            //1. Resolve the control COM-free. An unusable source is refused before anything is touched.
            ApertureTypeDefinition apertureTypeDefinition = singleOpeningProperties.ApertureTypeDefinition(building.DayTypeNames, out string name_Schedule, out string refusal_Definition);
            if (apertureTypeDefinition == null)
            {
                refusal = refusal_Definition;
                return null;
            }

            //2. Reconcile against what the element already carried. The snapshot is taken before this
            //   export assigns anything, which is what the cache does in production.
            List<FakeTBDApertureType> assigned = buildingElement.ExistingApertureTypes;
            List<KeyValuePair<string, ApertureTypeDefinition>> assignments = assigned
                .Select(x => new KeyValuePair<string, ApertureTypeDefinition>(x.Name, x.Definition()))
                .ToList();

            ApertureTypeReconciliation reconciliation = TasQuery.ApertureTypeReconciliation(buildingElement.Name, assignments, apertureTypeDefinition, ordinal, out int index, out string refusal_Reconciliation);
            if (reconciliation == ApertureTypeReconciliation.Refuse)
            {
                refusal = refusal_Reconciliation;
                return null;
            }

            if (reconciliation == ApertureTypeReconciliation.Reuse)
            {
                //Already correct. Nothing is written and nothing is assigned.
                return assigned[index];
            }

            if (reconciliation == ApertureTypeReconciliation.Legacy)
            {
                string name_Legacy = ordinal >= 2 ? string.Format("{0} {1}", buildingElement.Name, ordinal) : buildingElement.Name;
                FakeTBDApertureType apertureType_Legacy = building.ApertureTypes.Find(x => x.Name == name_Legacy);
                if (apertureType_Legacy == null)
                {
                    apertureType_Legacy = building.AddApertureType();
                    apertureType_Legacy.Name = name_Legacy;
                }

                if (!Apply(building, apertureType_Legacy, apertureTypeDefinition, name_Schedule, out string refusal_Legacy))
                {
                    refusal = refusal_Legacy;
                    return null;
                }

                Assign(buildingElement, apertureType_Legacy);

                return apertureType_Legacy;
            }

            //3. Building-level reuse. NOTHING on a hit is written. A type whose own write failed earlier
            //   this session is reserved by name only: it can never be a candidate, however it would read
            //   back - the mirror of the cache's definitionless reservation entry.
            List<ApertureTypeDefinition> candidates = building.ApertureTypes
                .Select(x => !building.NonReusable.Contains(x) && TasQuery.TryDecomposeApertureTypeName(x.Name, out string _, out int ordinal_Existing) && ordinal_Existing == (ordinal < 1 ? 1 : ordinal) ? x.Definition() : null)
                .ToList();

            int index_Reuse = TasQuery.ApertureTypeIndex(candidates, apertureTypeDefinition);
            if (index_Reuse != -1)
            {
                FakeTBDApertureType apertureType_Reused = building.ApertureTypes[index_Reuse];
                Assign(buildingElement, apertureType_Reused);

                return apertureType_Reused;
            }

            //4. A new definition is genuinely needed.
            string name = TasQuery.ApertureTypeName(building.ApertureTypes.Select(x => x.Name), apertureTypeDefinition, ordinal, out string refusal_Name);
            if (name == null)
            {
                refusal = refusal_Name;
                return null;
            }

            FakeTBDApertureType result = building.AddApertureType();
            result.Name = name;

            if (!Apply(building, result, apertureTypeDefinition, name_Schedule, out string refusal_Apply))
            {
                //A late failure - the schedule write above - leaves the created type behind: never
                //reusable, its name reserved.
                building.NonReusable.Add(result);
                refusal = refusal_Apply;
                return null;
            }

            //5. Full read-back verification before registration or assignment, as the production write now
            //   does: only a persisted definition EQUAL to the requested one makes the type reusable. A
            //   mismatch refuses; the type stays behind, named and never reusable.
            if (!result.Definition().Equals(apertureTypeDefinition))
            {
                building.NonReusable.Add(result);
                refusal = string.Format("Aperture type '{0}' was created but did not read back as the requested opening control, so it stays in the TBD as a named, non-reusable type and was not assigned.", name);
                return null;
            }

            Assign(buildingElement, result);

            return result;
        }

        /// <summary>
        /// The write sequence itself, in production's order: the schedule is resolved first (reused by
        /// value, or created under a reserved name and verified), then mode, then the schedule last.
        /// </summary>
        private static bool Apply(FakeTBDBuilding building, FakeTBDApertureType apertureType, ApertureTypeDefinition apertureTypeDefinition, string name_Schedule, out string refusal)
        {
            refusal = null;

            if (apertureTypeDefinition.Description != null)
            {
                apertureType.Description = apertureTypeDefinition.Description;
            }

            int[] schedule = null;
            if (apertureTypeDefinition.HasSchedule)
            {
                //Schedules are reused by their 24 VALUES, whatever they are called - but only a schedule
                //whose own write verified. One whose write failed stays in the TBD (there is no
                //RemoveSchedule) and takes no part here.
                List<FakeTBDSchedule> reusable = building.Schedules.Where(x => x.Reusable).ToList();
                int index = TasQuery.ScheduleIndex(reusable.Select(x => x.Values), apertureTypeDefinition.ScheduleValues);
                if (index != -1)
                {
                    schedule = reusable[index].Values;
                }
                else
                {
                    //A new schedule is genuinely needed. Its name is chosen against every occupied name -
                    //reusable and reserved alike - and the schedule exists, named and reserved, before
                    //any value is written.
                    string name = TasQuery.ScheduleName(building.Schedules.Select(x => x.Name), name_Schedule, apertureTypeDefinition.ScheduleValues, out string refusal_Name);
                    if (name == null)
                    {
                        refusal = refusal_Name;
                        return false;
                    }

                    FakeTBDSchedule schedule_New = new FakeTBDSchedule { Name = name, Values = apertureTypeDefinition.ScheduleValues };
                    building.Schedules.Add(schedule_New);

                    if (building.FailsNextScheduleValues)
                    {
                        //The write fails AFTER creation and naming: the schedule stays behind, never
                        //reusable, its name reserved - and the aperture type write is refused with it.
                        building.FailsNextScheduleValues = false;
                        schedule_New.Reusable = false;
                        schedule_New.Values = new int[24];
                        refusal = string.Format("TBD schedule '{0}' did not persist its 24 hourly values.", name);
                        return false;
                    }

                    schedule = schedule_New.Values;
                }
            }

            apertureType.DischargeCoefficient = apertureTypeDefinition.DischargeCoefficient;
            apertureType.Profile.Value = 1;
            apertureType.Profile.Factor = apertureTypeDefinition.Factor;

            if (apertureTypeDefinition.HasSchedule)
            {
                apertureType.Profile.SetbackValue = 0;
            }

            if (apertureTypeDefinition.Mode == ApertureTypeProfileMode.Function)
            {
                apertureType.Profile.Type = 4;
                apertureType.Profile.Function = apertureTypeDefinition.Function;
            }
            else if (apertureTypeDefinition.Mode == ApertureTypeProfileMode.ScheduleOnly)
            {
                apertureType.Profile.Type = 1;
            }

            if (apertureTypeDefinition.HasSchedule)
            {
                apertureType.Profile.Schedule = schedule;
            }

            foreach (string dayTypeName in building.DayTypeNames)
            {
                apertureType.SetDayType(dayTypeName, true);
            }

            return true;
        }

        /// <summary>The assigned-guard: TAS adds a second entry when handed a type the element has.</summary>
        private static void Assign(FakeTBDBuildingElement buildingElement, FakeTBDApertureType apertureType)
        {
            if (buildingElement.ApertureTypes.Exists(x => x.Name == apertureType.Name))
            {
                return;
            }

            buildingElement.AssignApertureType(apertureType);
        }

        private static List<FakeTBDBuildingElement> Export(FakeTBDBuilding building, int count, System.Func<int, IOpeningProperties> openingProperties, string namePrefix = "Windows: W")
        {
            List<FakeTBDBuildingElement> result = new List<FakeTBDBuildingElement>();

            for (int i = 0; i < count; i++)
            {
                //A per-window element name, exactly as the direct export produces today: the aperture's
                //name and GUID. Stage 1 shares aperture types across these, not the elements themselves.
                FakeTBDBuildingElement buildingElement = new FakeTBDBuildingElement(string.Format("{0}{1:000} {2} -pane", namePrefix, i, System.Guid.NewGuid()));
                result.Add(buildingElement);

                IOpeningProperties properties = openingProperties(i);
                List<ISingleOpeningProperties> children = properties is MultipleOpeningProperties multiple
                    ? multiple.SingleOpeningProperties
                    : new List<ISingleOpeningProperties> { (ISingleOpeningProperties)properties };

                List<ApertureTypeDefinition> definitions = children.Select(x => x.ApertureTypeDefinition(building.DayTypeNames, out string _)).ToList();
                List<int> ordinals = TasQuery.ApertureTypeOrdinals(definitions);

                for (int child = 0; child < children.Count; child++)
                {
                    Write(building, buildingElement, children[child], ordinals[child], out string _);
                }
            }

            return result;
        }

        // -------------------------------------------------------------------------------------------------
        // Same definition -> one shared type; different definition -> its own type
        // -------------------------------------------------------------------------------------------------

        [Test]
        public void Export_TwoHundredIdenticalWindows_ProduceOneApertureTypeAndOneSchedule()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();

            List<FakeTBDBuildingElement> buildingElements = Export(building, 200, x => PartO(OpeningRestriction.NightClosed));

            Assert.That(building.ApertureTypes.Count, Is.EqualTo(1), "one control, one definition");
            Assert.That(building.Schedules.Count, Is.EqualTo(1), "and one schedule, reused by value");
            Assert.That(buildingElements.Count(x => x.ApertureTypes.Count == 1), Is.EqualTo(200), "with 200 assignments of it");
            Assert.That(buildingElements.All(x => x.ApertureTypes[0] == building.ApertureTypes[0]), Is.True);
        }

        [Test]
        public void Export_FiveControlVariants_ProduceFiveApertureTypes()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();

            List<ISingleOpeningProperties> variants = new List<ISingleOpeningProperties>
            {
                PartO(),
                PartO(OpeningRestriction.NightClosed),
                PartO(OpeningRestriction.AlwaysClosed),
                new PartOOpeningProperties(1.2, 1.0, 45.0),
                new PartOOpeningProperties(1.2, 1.0, 30.0) { Factor = 0.5 }
            };

            Export(building, 200, x => variants[x % variants.Count]);

            Assert.That(building.ApertureTypes.Count, Is.EqualTo(5));
            Assert.That(building.ApertureTypes.Select(x => x.Name).Distinct().Count(), Is.EqualTo(5), "and every name is distinct");
        }

        /// <summary>
        /// <b>The mutation-safety rule, as a test.</b> A shared definition is referenced by every element
        /// that uses it, so reusing one must write NOTHING - not even rewrite a property to the value it
        /// already holds. The write log is empty from window two onward.
        /// </summary>
        [Test]
        public void Export_ReusingAnExistingSharedType_WritesNothingToIt()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();

            Export(building, 1, x => PartO(OpeningRestriction.NightClosed));

            FakeTBDApertureType apertureType = building.ApertureTypes.Single();
            int writes_AfterCreation = apertureType.WriteLog.Count;
            Assert.That(writes_AfterCreation, Is.GreaterThan(0), "creating one does write");

            Export(building, 199, x => PartO(OpeningRestriction.NightClosed));

            Assert.That(building.ApertureTypes.Count, Is.EqualTo(1));
            Assert.That(apertureType.WriteLog.Count, Is.EqualTo(writes_AfterCreation),
                "not one property was written to the shared type by the 199 windows that reused it");
        }

        /// <summary>
        /// Running the same export into the TBD it already produced creates nothing and writes nothing.
        /// This is the idempotence that value-based schedule reuse established, one level up.
        /// </summary>
        [Test]
        public void Export_RepeatedIntoItsOwnOutput_CreatesNothingAndWritesNothing()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();

            List<FakeTBDBuildingElement> buildingElements = Export(building, 20, x => PartO(OpeningRestriction.NightClosed));

            int count_ApertureTypes = building.ApertureTypes.Count;
            int count_Schedules = building.Schedules.Count;
            List<int> writes = building.ApertureTypes.Select(x => x.WriteLog.Count).ToList();

            //The same elements, exported again - which is what re-running against an existing TBD does, so
            //this time each element's aperture types ARE what it already carried.
            foreach (FakeTBDBuildingElement buildingElement in buildingElements)
            {
                buildingElement.Reopen();
                Write(building, buildingElement, PartO(OpeningRestriction.NightClosed), 1, out string refusal);
                Assert.That(refusal, Is.Null);
            }

            Assert.That(building.ApertureTypes.Count, Is.EqualTo(count_ApertureTypes), "no new aperture types");
            Assert.That(building.Schedules.Count, Is.EqualTo(count_Schedules), "no new schedules");
            Assert.That(building.ApertureTypes.Select(x => x.WriteLog.Count).ToList(), Is.EqualTo(writes), "and no writes into the existing ones");
            Assert.That(buildingElements.All(x => x.ApertureTypes.Count == 1), Is.True, "and no element gained a second opening");
        }

        /// <summary>
        /// The element already carries this very control, so the second call neither writes nor assigns -
        /// assigning again is what would give the element two openings where the model states one.
        /// </summary>
        [Test]
        public void Export_SecondWriteToTheSameElement_DoesNotAssignTwice()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();
            FakeTBDBuildingElement buildingElement = new FakeTBDBuildingElement("Windows: W01 " + System.Guid.NewGuid() + " -pane");

            Write(building, buildingElement, PartO(), 1, out string _);
            int writes_Element = buildingElement.WriteLog.Count;

            Write(building, buildingElement, PartO(), 1, out string _);

            Assert.That(buildingElement.ApertureTypes.Count, Is.EqualTo(1));
            Assert.That(buildingElement.WriteLog.Count, Is.EqualTo(writes_Element));
        }

        /// <summary>
        /// The compatibility overload is called once per indexed child, with the legacy 1-based index
        /// doubling as the reuse ordinal. Two calls for two identical indexed openings must produce two
        /// distinct types and two assignments - a fixed ordinal would make the second call reuse the
        /// first's type and the assignment guard would silently collapse two openings into one.
        /// </summary>
        [Test]
        public void Export_TwoIdenticalIndexedWrites_ProduceTwoTypesAndTwoAssignments()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();
            FakeTBDBuildingElement buildingElement = new FakeTBDBuildingElement("Windows: W01 " + System.Guid.NewGuid() + " -pane");

            Write(building, buildingElement, PartO(), TasQuery.ApertureTypeOrdinal(1), out string _);
            Write(building, buildingElement, PartO(), TasQuery.ApertureTypeOrdinal(2), out string _);

            Assert.That(building.ApertureTypes.Count, Is.EqualTo(2), "occurrence 1 and occurrence 2 of one control");
            Assert.That(buildingElement.ApertureTypes.Count, Is.EqualTo(2), "both openings stay assigned");
            Assert.That(buildingElement.ApertureTypes[0], Is.Not.SameAs(buildingElement.ApertureTypes[1]), "never the same type twice on one element");
        }

        // -------------------------------------------------------------------------------------------------
        // Ordinal multiplicity through the write
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Two identical children on one element are two openings, so they get two distinct types - and
        /// those two types are then shared with every other element that has the same two children.
        /// </summary>
        [Test]
        public void Export_TwoIdenticalChildrenOnManyElements_ProduceExactlyTwoApertureTypes()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();

            List<FakeTBDBuildingElement> buildingElements = Export(building, 50, x => new MultipleOpeningProperties(new List<ISingleOpeningProperties> { PartO(), PartO() }));

            Assert.That(building.ApertureTypes.Count, Is.EqualTo(2), "occurrence 1 and occurrence 2 of one control");
            string name_1 = TasQuery.ApertureTypeName(new string[0], PartO().ApertureTypeDefinition(building.DayTypeNames, out string _), 1, out string _);
            string name_2 = TasQuery.ApertureTypeName(new string[0], PartO().ApertureTypeDefinition(building.DayTypeNames, out string _), 2, out string _);
            Assert.That(building.ApertureTypes.Select(x => x.Name), Is.EqualTo(new[] { name_1, name_2 }));
            Assert.That(name_2, Is.EqualTo(name_1 + " 2"));
            Assert.That(buildingElements.All(x => x.ApertureTypes.Count == 2), Is.True, "and every element keeps both of its openings");
            Assert.That(buildingElements.All(x => x.ApertureTypes[0] != x.ApertureTypes[1]), Is.True, "never the same type twice on one element");
        }

        [Test]
        public void Export_TwoDifferentChildren_AreBothOccurrenceOne()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();

            Export(building, 10, x => new MultipleOpeningProperties(new List<ISingleOpeningProperties> { PartO(), PartO(OpeningRestriction.NightClosed) }));

            Assert.That(building.ApertureTypes.Count, Is.EqualTo(2));
            Assert.That(building.ApertureTypes.All(x => !x.Name.EndsWith(" 2")), Is.True, "neither is a second occurrence of the other");
        }

        // -------------------------------------------------------------------------------------------------
        // The legacy fence
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// A TBD whose aperture types are named after their elements keeps behaving exactly as it did: the
        /// per-element type is updated in place, and no shared type is created. That is safe only because
        /// the element name carries the aperture GUID, which makes the type exclusive to it.
        /// </summary>
        [Test]
        public void Export_ElementCarryingItsOwnLegacyType_UpdatesItInPlaceAndCreatesNoSharedType()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();
            FakeTBDBuildingElement buildingElement = new FakeTBDBuildingElement("Windows: W01 " + System.Guid.NewGuid() + " -pane");

            //A legacy TBD: the type is named after the element and already assigned to it.
            FakeTBDApertureType apertureType_Legacy = building.AddApertureType();
            apertureType_Legacy.Name = buildingElement.Name;
            apertureType_Legacy.DischargeCoefficient = 0.3f;
            buildingElement.AssignApertureType(apertureType_Legacy);

            int count_Before = building.ApertureTypes.Count;

            FakeTBDApertureType written = Write(building, buildingElement, PartO(), 1, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(written, Is.SameAs(apertureType_Legacy), "the legacy type is the one written");
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(count_Before), "and no shared type was created alongside it");
            Assert.That(apertureType_Legacy.WriteLog, Does.Contain("apertureType.dischargeCoefficient"), "it IS written in place - the one place that still happens");
            Assert.That(buildingElement.ApertureTypes.Count, Is.EqualTo(1));
        }

        [Test]
        public void Export_ElementCarryingAStaleSharedType_IsRefusedAndNothingIsWritten()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();
            FakeTBDBuildingElement buildingElement = new FakeTBDBuildingElement("Windows: W01 " + System.Guid.NewGuid() + " -pane");

            //A shared type from an earlier export, stating a different control.
            FakeTBDApertureType apertureType_Stale = building.AddApertureType();
            apertureType_Stale.Name = "Opening Cd0.3 F1";
            apertureType_Stale.DischargeCoefficient = 0.3f;
            apertureType_Stale.Profile.Value = 1;
            foreach (string dayTypeName in building.DayTypeNames)
            {
                apertureType_Stale.SetDayType(dayTypeName, true);
            }

            buildingElement.AssignApertureType(apertureType_Stale);
            int writes_Stale = apertureType_Stale.WriteLog.Count;

            FakeTBDApertureType written = Write(building, buildingElement, PartO(), 1, out string refusal);

            Assert.That(written, Is.Null);
            Assert.That(refusal, Does.Contain("Opening Cd0.3 F1"));
            Assert.That(apertureType_Stale.WriteLog.Count, Is.EqualTo(writes_Stale), "the stale shared type is not rewritten - other elements reference it");
            Assert.That(buildingElement.ApertureTypes.Count, Is.EqualTo(1), "and no second opening is added");
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(1), "and no replacement type is created");
        }

        /// <summary>
        /// Two elements whose controls differ in one field only: the first keeps its type untouched and the
        /// second gets its own. This is the type-level half of the change/split case.
        /// </summary>
        [Test]
        public void Export_OneWindowOutOfAHundredChangesItsCd_LeavesTheOtherNinetyNineUntouched()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();

            Export(building, 99, x => PartO());
            FakeTBDApertureType apertureType_Shared = building.ApertureTypes.Single();
            int writes = apertureType_Shared.WriteLog.Count;

            Export(building, 1, x => new PartOOpeningProperties(1.2, 1.0, 45.0));

            Assert.That(building.ApertureTypes.Count, Is.EqualTo(2));
            Assert.That(apertureType_Shared.WriteLog.Count, Is.EqualTo(writes), "the 99 windows' shared type was not touched");
        }

        // -------------------------------------------------------------------------------------------------
        // Late failure - a created type stays behind, never reusable, its name reserved
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// COM creation succeeding and a LATER write failing leaves the object in the TBD. It must never
        /// become reusable - but its name stays reserved, so the next write cannot accidentally choose
        /// the same name. Here the failure is the profile not retaining the assigned schedule.
        /// </summary>
        [Test]
        public void Export_ScheduleWriteFailingLate_LeavesTheNameReservedAndTheTypeNonReusable()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();
            building.DropsNextSchedule = true;

            FakeTBDBuildingElement element_1 = new FakeTBDBuildingElement("Windows: W01 " + System.Guid.NewGuid() + " -pane");
            FakeTBDApertureType failed = Write(building, element_1, PartO(OpeningRestriction.NightClosed), 1, out string refusal_1);

            Assert.That(failed, Is.Null);
            Assert.That(refusal_1, Is.Not.Null);
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(1), "the half-written type remains in the TBD");
            Assert.That(element_1.ApertureTypes, Is.Empty, "and it was never assigned");

            string name_Failed = building.ApertureTypes[0].Name;
            Assert.That(name_Failed, Does.Contain("S00FFFE"), "the NightClosed control carries the Part O mask in its name");

            //The next opening stating the same control: the stale type is not adopted, and its name is
            //not taken - a deterministic collision suffix is chosen instead.
            FakeTBDBuildingElement element_2 = new FakeTBDBuildingElement("Windows: W02 " + System.Guid.NewGuid() + " -pane");
            FakeTBDApertureType written = Write(building, element_2, PartO(OpeningRestriction.NightClosed), 1, out string refusal_2);

            Assert.That(refusal_2, Is.Null);
            Assert.That(written, Is.Not.Null);
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(2));
            Assert.That(written.Name, Does.StartWith(name_Failed + "_"), "a deterministic collision suffix - never the failed type's name again");
            Assert.That(building.ApertureTypes.Select(x => x.Name), Does.Contain(name_Failed), "the failed name stays reserved in the namespace");
            Assert.That(element_2.ApertureTypes, Does.Contain(written));
            Assert.That(element_2.ApertureTypes, Does.Not.Contain(building.ApertureTypes[0]), "the stale type is never handed out");
        }

        /// <summary>
        /// The schedule half of the late-failure rule: the value write fails AFTER the schedule was
        /// created and named. The schedule stays in the TBD and can never be reused - but its name stays
        /// reserved, so the next opening with the same values gets the deterministic qualified name. The
        /// aperture type whose write was abandoned with it is likewise left named, reserved and
        /// non-reusable.
        /// </summary>
        [Test]
        public void Export_ScheduleValuesFailingLate_ReservesBothNamesAndReusesNeitherObject()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();
            building.FailsNextScheduleValues = true;

            FakeTBDBuildingElement element_1 = new FakeTBDBuildingElement("Windows: W01 " + System.Guid.NewGuid() + " -pane");
            FakeTBDApertureType failed = Write(building, element_1, PartO(OpeningRestriction.NightClosed), 1, out string refusal_1);

            Assert.That(failed, Is.Null);
            Assert.That(refusal_1, Does.Contain("PartO_DayOpen_08_23"), "the failed schedule is named in the refusal");
            Assert.That(building.Schedules.Count, Is.EqualTo(1), "the failed schedule stays in the TBD");
            Assert.That(building.Schedules[0].Reusable, Is.False, "and is never reusable");
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(1), "the abandoned aperture type stays too");
            Assert.That(element_1.ApertureTypes, Is.Empty);

            //The next opening stating the same control: neither stale object is adopted, and neither name
            //is taken - the schedule lands on its deterministic qualified name, the aperture type on its
            //own collision suffix.
            FakeTBDBuildingElement element_2 = new FakeTBDBuildingElement("Windows: W02 " + System.Guid.NewGuid() + " -pane");
            FakeTBDApertureType written = Write(building, element_2, PartO(OpeningRestriction.NightClosed), 1, out string refusal_2);

            Assert.That(refusal_2, Is.Null);
            Assert.That(building.Schedules.Count, Is.EqualTo(2));
            Assert.That(building.Schedules[1].Name, Is.EqualTo(building.Schedules[0].Name + "_00FFFE"), "the reserved name is occupied, so the deterministic signature suffix is chosen");
            Assert.That(building.Schedules[1].Reusable, Is.True);
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(2));
            Assert.That(written.Name, Does.StartWith(building.ApertureTypes[0].Name + "_"));
            Assert.That(element_2.ApertureTypes, Does.Contain(written));
        }

        /// <summary>
        /// The strict half of the rule: even when the stale type would read back as EXACTLY the requested
        /// control, a failed creation is never adopted - the session reserved it as non-reusable. Here the
        /// dropped schedule leaves the stale type reading back as the PLAIN control an unrestricted
        /// opening then asks for.
        /// </summary>
        [Test]
        public void Export_FailedTypeReadingBackAsTheRequestedControl_IsStillNeverReused()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();
            building.DropsNextSchedule = true;

            Write(building, new FakeTBDBuildingElement("Windows: W01 " + System.Guid.NewGuid() + " -pane"), PartO(OpeningRestriction.NightClosed), 1, out string _);
            FakeTBDApertureType failed = building.ApertureTypes.Single();
            Assert.That(failed.Definition().HasSchedule, Is.False, "the dropped schedule makes the stale type read back as the plain control");

            FakeTBDBuildingElement element = new FakeTBDBuildingElement("Windows: W02 " + System.Guid.NewGuid() + " -pane");
            FakeTBDApertureType written = Write(building, element, PartO(), 1, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(written, Is.Not.Null);
            Assert.That(written, Is.Not.SameAs(failed), "the stale type is never reusable, however it reads back");
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(2), "a new, verified type was created for the plain control");
            Assert.That(element.ApertureTypes, Does.Contain(written));
        }

        /// <summary>
        /// A newly created shared type is registered as reusable only after the WHOLE type reads back as
        /// the requested definition. Here the store rounds the discharge coefficient, so what persisted
        /// is NOT what was requested: the write refuses, the name stays reserved, the type is never
        /// reusable - and the collision suffix the next attempt lands on is derived from the EXACT float
        /// bit pattern, so close values never share it.
        /// </summary>
        [Test]
        public void Export_CreatedTypeReadingBackDifferently_IsRefusedReservedAndNeverReused()
        {
            FakeTBDBuilding building = new FakeTBDBuilding();
            building.RoundsNextDischargeCoefficient = true;

            FakeTBDBuildingElement element_1 = new FakeTBDBuildingElement("Windows: W01 " + System.Guid.NewGuid() + " -pane");
            FakeTBDApertureType failed_1 = Write(building, element_1, new ProfileOpeningProperties(0.6201), 1, out string refusal_1);

            Assert.That(failed_1, Is.Null);
            Assert.That(refusal_1, Does.Contain("Opening Cd0.62 F1"), "the refused type is named, with the rounded text kept for display");
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(1));
            Assert.That(element_1.ApertureTypes, Is.Empty);

            //A second opening stating the same 0.6201 control: the failed name is reserved, so it lands on
            //the collision suffix derived from the exact 0.6201 bit pattern - and fails the same way.
            building.RoundsNextDischargeCoefficient = true;
            FakeTBDBuildingElement element_2 = new FakeTBDBuildingElement("Windows: W02 " + System.Guid.NewGuid() + " -pane");
            FakeTBDApertureType failed_2 = Write(building, element_2, new ProfileOpeningProperties(0.6201), 1, out string refusal_2);

            Assert.That(failed_2, Is.Null);
            Assert.That(refusal_2, Does.Contain("Opening Cd0.62 F1_"), "the second 0.6201 control takes the exact-hash collision name, not the reserved one");
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(2));
            string name_Failed_2 = building.ApertureTypes[1].Name;
            Assert.That(name_Failed_2, Does.StartWith("Opening Cd0.62 F1_"));

            //An opening stating exactly what the failed types persisted - Cd 0.62 - still may not adopt
            //either of them: a failed creation is never reusable. It gets its own verified type, on the
            //collision suffix of its own exact hash (the 0.6201 suffix is reserved too).
            FakeTBDBuildingElement element_3 = new FakeTBDBuildingElement("Windows: W03 " + System.Guid.NewGuid() + " -pane");
            FakeTBDApertureType written = Write(building, element_3, new ProfileOpeningProperties(0.62), 1, out string refusal_3);

            Assert.That(refusal_3, Is.Null);
            Assert.That(building.ApertureTypes.Count, Is.EqualTo(3), "the 0.62 control gets its own verified type");
            Assert.That(written.Name, Does.StartWith("Opening Cd0.62 F1_"));
            Assert.That(written.Name, Is.Not.EqualTo(name_Failed_2), "the two bit patterns hash to two different suffixes");
            Assert.That(element_3.ApertureTypes, Does.Contain(written));
        }
    }
}
