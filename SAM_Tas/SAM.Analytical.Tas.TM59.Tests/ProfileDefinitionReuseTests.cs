// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProfileDefinition = SAM.Analytical.Tas.ProfileDefinition;
using ProfileReuseIndex = SAM.Analytical.Tas.ProfileReuseIndex;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The TBD -&gt; SAM profile seam: what makes two imported profiles the same reusable library definition,
    /// the deterministic naming that follows, and the guarantee that every rewritten reference still resolves.</b>
    /// <para>
    /// A SAM <c>Profile</c> is a library-level REUSABLE DEFINITION - a native SAM model already shares one across
    /// every <c>InternalCondition</c> that references it. The TBD import instead minted one per internal-condition
    /// slot and named it <c>"{internal condition} [{profile}]"</c>, so a two-zone building carrying one activity
    /// produced two copies of every schedule. The decision that governs whether an import creates forty-two
    /// profiles or twenty is therefore <b>definition equality</b>: category plus complete flattened values, and
    /// nothing about a zone.
    /// </para>
    /// <para>
    /// <b>Why these tests need no installed TAS.</b> Everything that decides is COM-free:
    /// <see cref="ProfileDefinition"/>, <see cref="ProfileReuseIndex"/>, <c>TasQuery.ProfileSignature</c>,
    /// <c>TasQuery.ProfileName</c> and <c>TasQuery.ProfileCategory</c> name no TAS COM type. What genuinely needs
    /// COM is reading the values out of a <c>TBD.profile</c>, and the registration API takes those values as
    /// plain doubles for exactly that reason - so the whole reuse and naming decision is exercised here, and the
    /// COM layer above it is a one-line read per slot.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ProfileDefinitionReuseTests
    {
        // TBD.Profiles slot numbers. Named here rather than referenced, so this project keeps needing no
        // Interop.TBD reference; the production slot tables built over them are pinned by
        // References_VentilationSlotIsCollected_SoItsReferenceResolves via ProductionSlotNames.
        private const int ticUL = 1;
        private const int ticLL = 2;
        private const int ticHUL = 5;
        private const int ticHLL = 6;
        private const int ticI = 7;
        private const int ticV = 8;
        private const int ticLG = 9;
        private const int ticOSG = 10;
        private const int ticOLG = 11;
        private const int ticESG = 12;
        private const int ticELG = 13;
        private const int ticCOG = 18;

        private const string Infiltration = "Infiltration";
        private const string Lighting = "Lighting";
        private const string Occupancy = "Occupancy";
        private const string EquipmentSensible = "Equipment Sensible";
        private const string EquipmentLatent = "Equipment Latent";
        private const string Pollutant = "Pollutant";
        private const string Cooling = "Cooling";
        private const string Heating = "Heating";
        private const string Humidification = "Humidification";
        private const string Dehumidification = "Dehumidification";
        private const string Ventilation = "Ventilation";

        // =================================================================================================
        // Builders
        // =================================================================================================

        /// <summary>Run-length expansion, so a 24-value profile reads as the shape it is.</summary>
        private static double[] Values(params double[] runs)
        {
            List<double> result = new List<double>();
            for (int i = 0; i < runs.Length; i += 2)
            {
                for (int j = 0; j < (int)runs[i + 1]; j++)
                {
                    result.Add(runs[i]);
                }
            }

            return result.ToArray();
        }

        private static double[] Flat(double value, int count)
        {
            return Enumerable.Repeat(value, count).ToArray();
        }

        private static ProfileDefinition Definition(string category, params double[] values)
        {
            return new ProfileDefinition(category, values);
        }

        /// <summary>The legacy name the import gives a profile it is NOT sharing.</summary>
        private static string Legacy(string internalConditionName, string profileName)
        {
            return string.Format("{0} [{1}]", internalConditionName, profileName);
        }

        private static bool Register(ProfileReuseIndex profileReuseIndex, string internalConditionName, int slot, string category, string sourceName, double[] values)
        {
            return profileReuseIndex.Register(internalConditionName, slot, category, values, sourceName, Legacy(internalConditionName, sourceName));
        }

        private static ProfileLibrary Library(ProfileReuseIndex profileReuseIndex)
        {
            ProfileLibrary result = new ProfileLibrary("Test");
            profileReuseIndex.Profiles.ForEach(x => result.Add(x));

            return result;
        }

        // =================================================================================================
        // The ModelA-Tas fixture, as data
        // =================================================================================================

        /// <summary>
        /// One TBD internal-condition profile slot, as the import reads it.
        /// </summary>
        private sealed class Slot
        {
            public Slot(string internalConditionName, int slot, string category, string sourceName, double[] values)
            {
                InternalConditionName = internalConditionName;
                Index = slot;
                Category = category;
                SourceName = sourceName;
                ProfileValues = values;
            }

            public string InternalConditionName { get; }
            public int Index { get; }
            public string Category { get; }
            public string SourceName { get; }
            public double[] ProfileValues { get; }
        }

        /// <summary>
        /// The eleven slots a non-HDD <c>ModelA-Tas</c> cell carries, with the values read out of the fixture.
        /// </summary>
        private static List<Slot> Slots_Cell(string name)
        {
            return new List<Slot>
            {
                new Slot(name, ticI,   Infiltration,      "Constant",            Flat(1.0, 24)),
                new Slot(name, ticLG,  Lighting,          "8to18",               Values(0, 7, 0.5, 1, 1, 10, 0.5, 1, 0, 5)),
                new Slot(name, ticOLG, Occupancy,         "8to19",               Values(0, 7, 0.5, 1, 1, 11, 0.5, 1, 0, 4)),
                new Slot(name, ticOSG, Occupancy,         "8to19",               Values(0, 7, 0.5, 1, 1, 11, 0.5, 1, 0, 4)),
                new Slot(name, ticESG, EquipmentSensible, "8to19",               Values(0, 7, 0.5, 1, 1, 11, 0.5, 1, 0, 4)),
                new Slot(name, ticELG, EquipmentLatent,   "OFF",                 Flat(0.0, 24)),
                new Slot(name, ticCOG, Pollutant,         "OFF",                 Flat(0.0, 24)),
                new Slot(name, ticUL,  Cooling,           "CLG_7to19_25",        Values(28, 7, 25, 12, 28, 5)),
                new Slot(name, ticLL,  Heating,           "HTG_7to19_21",        Values(16, 7, 21, 12, 16, 5)),
                new Slot(name, ticHLL, Humidification,    "No Humidification",   Flat(0.0, 24)),
                new Slot(name, ticHUL, Dehumidification,  "No Dehumidification", Flat(100.0, 24)),
            };
        }

        /// <summary>
        /// The eleven slots an HDD sizing variant carries - all single-value profiles, and the source of both
        /// name collisions in the fixture (<c>Infiltration::Constant</c> and <c>Heating::HTG_7to19_21</c>, each
        /// wanted by a one-value definition AND by a 24-value one).
        /// </summary>
        private static List<Slot> Slots_Cell_HDD(string name)
        {
            return new List<Slot>
            {
                new Slot(name, ticI,   Infiltration,      "Constant",                 new[] { 0.20000000298023224 }),
                new Slot(name, ticLG,  Lighting,          "Lighting Gain",            new[] { 0.0 }),
                new Slot(name, ticOLG, Occupancy,         "Occupancy Latent Gain",    new[] { 0.0 }),
                new Slot(name, ticOSG, Occupancy,         "Occupancy Sensible Gain",  new[] { 0.0 }),
                new Slot(name, ticESG, EquipmentSensible, "Equipment Sensible Gain",  new[] { 0.0 }),
                new Slot(name, ticELG, EquipmentLatent,   "Equipment Latent Gain",    new[] { 0.0 }),
                new Slot(name, ticCOG, Pollutant,         "Pollutant Generation",     new[] { 0.0 }),
                new Slot(name, ticUL,  Cooling,           "Upper Limit",              new[] { 150.0 }),
                new Slot(name, ticLL,  Heating,           "HTG_7to19_21",             new[] { 21.0 }),
                new Slot(name, ticHLL, Humidification,    "Humidity Lower Limit",     new[] { 0.0 }),
                new Slot(name, ticHUL, Dehumidification,  "Humidity Upper Limit",     new[] { 100.0 }),
            };
        }

        /// <summary>
        /// Every profile slot <c>ModelA-Tas.sam</c>'s four internal conditions carry: 44 slots, which the legacy
        /// import turned into 42 library entries (the two lost pairs are each cell's occupancy sensible and
        /// latent slots, which share a source name and so silently overwrote one another by library key).
        /// </summary>
        private static List<Slot> Slots_ModelA()
        {
            List<Slot> result = new List<Slot>();
            result.AddRange(Slots_Cell("Cell 1"));
            result.AddRange(Slots_Cell("Cell 2"));
            result.AddRange(Slots_Cell_HDD("Cell 1 - HDD"));
            result.AddRange(Slots_Cell_HDD("Cell 2 - HDD"));

            return result;
        }

        private static ProfileReuseIndex Index(IEnumerable<Slot> slots)
        {
            ProfileReuseIndex result = new ProfileReuseIndex();
            foreach (Slot slot in slots)
            {
                Register(result, slot.InternalConditionName, slot.Index, slot.Category, slot.SourceName, slot.ProfileValues);
            }

            result.Resolve();

            return result;
        }

        // =================================================================================================
        // 1-8. Equality
        // =================================================================================================

        [Test]
        public void Equality_SameCategorySameValues_IsOneDefinition()
        {
            ProfileDefinition a = Definition(Heating, Values(16, 7, 21, 12, 16, 5));
            ProfileDefinition b = Definition(Heating, Values(16, 7, 21, 12, 16, 5));

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.CompareTo(b), Is.Zero);

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticLL, Heating, "HTG", Values(16, 7, 21, 12, 16, 5));
            Register(profileReuseIndex, "IC B", ticLL, Heating, "HTG", Values(16, 7, 21, 12, 16, 5));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(1));
        }

        [Test]
        public void Equality_SameCategoryDifferentValues_AreTwoDefinitions()
        {
            ProfileDefinition a = Definition(Heating, Values(16, 7, 21, 12, 16, 5));
            ProfileDefinition b = Definition(Heating, Values(16, 7, 22, 12, 16, 5));

            Assert.That(a, Is.Not.EqualTo(b));

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticLL, Heating, "HTG", Values(16, 7, 21, 12, 16, 5));
            Register(profileReuseIndex, "IC B", ticLL, Heating, "HTG", Values(16, 7, 22, 12, 16, 5));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(2));
        }

        [Test]
        public void Equality_SameValuesDifferentCategory_AreTwoDefinitions()
        {
            ProfileDefinition a = Definition(Heating, Flat(1.0, 24));
            ProfileDefinition b = Definition(Cooling, Flat(1.0, 24));

            Assert.That(a, Is.Not.EqualTo(b));

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticLL, Heating, "Constant", Flat(1.0, 24));
            Register(profileReuseIndex, "IC A", ticUL, Cooling, "Constant", Flat(1.0, 24));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(2));

            //Category scopes the name claim set, exactly as the ProfileLibrary key "{Category}::{Name}" does,
            //so both keep the bare source name.
            Assert.That(profileReuseIndex.GetProfileName(Heating, Flat(1.0, 24)), Is.EqualTo("Constant"));
            Assert.That(profileReuseIndex.GetProfileName(Cooling, Flat(1.0, 24)), Is.EqualTo("Constant"));
        }

        [Test]
        public void Equality_DifferentSourceNamesSameCategoryAndValues_IsOneDefinition()
        {
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticLL, Heating, "Setpoint A", Flat(21.0, 24));
            Register(profileReuseIndex, "IC B", ticLL, Heating, "Setpoint B", Flat(21.0, 24));
            Register(profileReuseIndex, "IC C", ticLL, Heating, "Setpoint C", Flat(21.0, 24));
            profileReuseIndex.Resolve();

            //The name is metadata; it takes no part in identity.
            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(1));
            Assert.That(profileReuseIndex.Profiles.Count, Is.EqualTo(1));

            string name = profileReuseIndex.GetProfileName("IC A", ticLL);
            Assert.That(profileReuseIndex.GetProfileName("IC B", ticLL), Is.EqualTo(name));
            Assert.That(profileReuseIndex.GetProfileName("IC C", ticLL), Is.EqualTo(name));
        }

        [Test]
        public void Equality_OneBitApart_AreTwoDefinitions()
        {
            double value = 0.1;
            double neighbour = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value) + 1);

            Assert.That(neighbour, Is.Not.EqualTo(value), "The neighbouring bit pattern must be a different double for this test to mean anything.");

            ProfileDefinition a = Definition(Lighting, value);
            ProfileDefinition b = Definition(Lighting, neighbour);

            //No tolerance: both sides come from the same TAS read, so a tolerance could only merge two
            //profiles the model states as different.
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(TasQuery.ProfileSignature(a), Is.Not.EqualTo(TasQuery.ProfileSignature(b)));
        }

        [Test]
        public void Equality_NegativeZero_IsZero()
        {
            ProfileDefinition a = Definition(Lighting, 0.0, 1.0);
            ProfileDefinition b = Definition(Lighting, -0.0, 1.0);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()), "Equal definitions must hash alike or the dictionary contract is broken.");
            Assert.That(TasQuery.ProfileSignature(a), Is.EqualTo(TasQuery.ProfileSignature(b)));

            //And the stored value is the positive zero, not the negative one it came in as.
            Assert.That(BitConverter.DoubleToInt64Bits(b.Values[0]), Is.EqualTo(BitConverter.DoubleToInt64Bits(0.0)));

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticLG, Lighting, "OFF", new[] { 0.0, 1.0 });
            Register(profileReuseIndex, "IC B", ticLG, Lighting, "OFF", new[] { -0.0, 1.0 });
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(1));
        }

        [Test]
        public void Equality_NaN_IsDeterministic()
        {
            //Two different NaN payloads. Under raw IEEE-754 neither equals itself, let alone the other.
            double nan_A = double.NaN;
            double nan_B = BitConverter.Int64BitsToDouble(0x7FF8000000000001L);

            Assume.That(double.IsNaN(nan_B));
            Assume.That(BitConverter.DoubleToInt64Bits(nan_A), Is.Not.EqualTo(BitConverter.DoubleToInt64Bits(nan_B)));

            ProfileDefinition a = Definition(Lighting, nan_A, 1.0);
            ProfileDefinition b = Definition(Lighting, nan_B, 1.0);

            Assert.That(a, Is.EqualTo(a), "A definition carrying a NaN must at least equal itself.");
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(TasQuery.ProfileSignature(a), Is.EqualTo(TasQuery.ProfileSignature(b)));

            //Deterministic across repeated construction, which is what a persisted name discriminator needs.
            Assert.That(TasQuery.ProfileSignatureHash(Definition(Lighting, double.NaN)), Is.EqualTo(TasQuery.ProfileSignatureHash(Definition(Lighting, double.NaN))));
        }

        [Test]
        public void Equality_ValueCountParticipatesInIdentity()
        {
            //A one-value profile and a 24-value profile of the same number are different shapes, and TAS
            //writes them back as different profile types.
            ProfileDefinition one = Definition(Infiltration, 1.0);
            ProfileDefinition twentyFour = Definition(Infiltration, Flat(1.0, 24));

            Assert.That(one, Is.Not.EqualTo(twentyFour));
            Assert.That(one.Count, Is.EqualTo(1));
            Assert.That(twentyFour.Count, Is.EqualTo(24));

            //And a shorter prefix of the same values is a different definition too.
            Assert.That(Definition(Infiltration, Flat(1.0, 23)), Is.Not.EqualTo(twentyFour));

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticI, Infiltration, "Constant", new[] { 1.0 });
            Register(profileReuseIndex, "IC B", ticI, Infiltration, "Constant", Flat(1.0, 24));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(2));
        }

        [Test]
        public void Equality_ZeroLengthDefinition_IsNotReusable()
        {
            //A TAS function profile reads back with no values at all, so its flattened form is an incomplete
            //representation of it - merging by that would be unsafe. It keeps today's per-internal-condition
            //import instead.
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();

            Assert.That(Register(profileReuseIndex, "IC A", ticLG, Lighting, "Daylight", new double[0]), Is.False);
            Assert.That(Register(profileReuseIndex, "IC B", ticLG, Lighting, "Daylight", new double[0]), Is.False);
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.Zero, "A zero-length profile is never a shared definition.");

            //Both still reach the library, under exactly the names today's import gives them.
            Assert.That(profileReuseIndex.GetProfileName("IC A", ticLG), Is.EqualTo("IC A [Daylight]"));
            Assert.That(profileReuseIndex.GetProfileName("IC B", ticLG), Is.EqualTo("IC B [Daylight]"));

            ProfileLibrary profileLibrary = Library(profileReuseIndex);
            Assert.That(profileLibrary.GetProfiles().Count, Is.EqualTo(2));
            Assert.That(profileLibrary.GetProfile("IC A [Daylight]", ProfileType.Lighting), Is.Not.Null);
            Assert.That(profileLibrary.GetProfile("IC B [Daylight]", ProfileType.Lighting), Is.Not.Null);
        }

        // =================================================================================================
        // 9-16. Deterministic naming
        // =================================================================================================

        [Test]
        public void Naming_SameNameSameDefinition_KeepsTheSourceName()
        {
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "Cell 1", ticLL, Heating, "HTG_7to19_21", Flat(21.0, 24));
            Register(profileReuseIndex, "Cell 2", ticLL, Heating, "HTG_7to19_21", Flat(21.0, 24));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.Profiles.Count, Is.EqualTo(1));

            //The underscores in a real TAS setpoint name survive: the name base normalises whitespace and
            //control characters only.
            Assert.That(profileReuseIndex.Profiles[0].Name, Is.EqualTo("HTG_7to19_21"));
        }

        [Test]
        public void Naming_DifferentNamesSameDefinition_TakesTheOrdinalSmallest()
        {
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticOSG, Occupancy, "Occupancy Sensible Gain", new[] { 0.0 });
            Register(profileReuseIndex, "IC A", ticOLG, Occupancy, "Occupancy Latent Gain", new[] { 0.0 });
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.Profiles.Count, Is.EqualTo(1));
            Assert.That(profileReuseIndex.Profiles[0].Name, Is.EqualTo("Occupancy Latent Gain"));
        }

        [Test]
        public void Naming_SameNameDifferentDefinitions_DiscriminatesDeterministically()
        {
            ProfileDefinition one = Definition(Infiltration, 0.2);
            ProfileDefinition twentyFour = Definition(Infiltration, Flat(1.0, 24));

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "Cell 1", ticI, Infiltration, "Constant", Flat(1.0, 24));
            Register(profileReuseIndex, "Cell 1 - HDD", ticI, Infiltration, "Constant", new[] { 0.2 });
            profileReuseIndex.Resolve();

            List<Profile> profiles = profileReuseIndex.Profiles;
            Assert.That(profiles.Count, Is.EqualTo(2), "Two definitions wanting one name must stay two definitions.");

            List<string> names = profiles.ConvertAll(x => x.Name);
            Assert.That(names.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(2), "Neither definition may be dropped or overwritten.");

            //Claim order is ProfileDefinition.CompareTo - category, then value count, then value bits - so the
            //one-value definition claims the bare name and the 24-value one is discriminated. Never the other
            //way round, whatever order the building was walked in.
            Assert.That(profileReuseIndex.GetProfileName(Infiltration, new[] { 0.2 }), Is.EqualTo("Constant"));
            Assert.That(profileReuseIndex.GetProfileName(Infiltration, Flat(1.0, 24)), Is.EqualTo("Constant_" + TasQuery.ProfileSignatureHash(twentyFour)));

            Assert.That(TasQuery.ProfileSignatureHash(one), Is.Not.EqualTo(TasQuery.ProfileSignatureHash(twentyFour)));
        }

        [Test]
        public void Naming_HDDFlattenedProfilesWithTheirOwnNames_ReachAStableFixedPoint()
        {
            //The export flattens a space condition's infiltration and heating profiles onto the HDD sizing
            //condition as single-value ticValueProfiles, and names that flattened content after itself
            //("<name> - HDD"), never after the full schedule it was derived from. The next import therefore
            //sees two names for two definitions instead of one name for two - which is what used to accrete
            //one "_<hash>" discriminator per SAM -> TAS -> SAM generation.
            List<Slot> slots_Generation1 = new List<Slot>
            {
                new Slot("Cell 1", ticI, Infiltration, "Constant", Flat(1.0, 24)),
                new Slot("Cell 1", ticLL, Heating, "HTG_7to19_21", Values(16, 7, 21, 12, 16, 5)),
                new Slot("Cell 1 - HDD", ticI, Infiltration, TasQuery.ProfileName_HDD("Constant"), new[] { 0.20000000298023224 }),
                new Slot("Cell 1 - HDD", ticLL, Heating, TasQuery.ProfileName_HDD("HTG_7to19_21"), new[] { 21.0 }),
            };

            ProfileReuseIndex generation1 = Index(slots_Generation1);

            //Both definitions exist per category, and neither needed the signature discriminator.
            Assert.That(generation1.DefinitionCount, Is.EqualTo(4));
            Assert.That(generation1.GetProfileName("Cell 1", ticI), Is.EqualTo("Constant"));
            Assert.That(generation1.GetProfileName("Cell 1 - HDD", ticI), Is.EqualTo(TasQuery.ProfileName_HDD("Constant")));
            Assert.That(generation1.GetProfileName("Cell 1", ticLL), Is.EqualTo("HTG_7to19_21"));
            Assert.That(generation1.GetProfileName("Cell 1 - HDD", ticLL), Is.EqualTo(TasQuery.ProfileName_HDD("HTG_7to19_21")));

            //The next export writes each slot's resolved name - the HDD sibling is derived from the space's
            //own condition and applies the SAME production rule (Query.ProfileName_HDD, which is what
            //Modify.UpdateInternalCondition_HDD calls at both of its write sites) - so generation 2 reads
            //back exactly these names...
            List<Slot> slots_Generation2 = new List<Slot>
            {
                new Slot("Cell 1", ticI, Infiltration, generation1.GetProfileName("Cell 1", ticI), Flat(1.0, 24)),
                new Slot("Cell 1", ticLL, Heating, generation1.GetProfileName("Cell 1", ticLL), Values(16, 7, 21, 12, 16, 5)),
                new Slot("Cell 1 - HDD", ticI, Infiltration, TasQuery.ProfileName_HDD(generation1.GetProfileName("Cell 1", ticI)), new[] { 0.20000000298023224 }),
                new Slot("Cell 1 - HDD", ticLL, Heating, TasQuery.ProfileName_HDD(generation1.GetProfileName("Cell 1", ticLL)), new[] { 21.0 }),
            };

            ProfileReuseIndex generation2 = Index(slots_Generation2);

            //...and resolves them to the identical names: the fixed point. No discriminator ever appears.
            Assert.That(generation2.DefinitionCount, Is.EqualTo(generation1.DefinitionCount));
            Assert.That(generation2.GetProfileName("Cell 1", ticI), Is.EqualTo(generation1.GetProfileName("Cell 1", ticI)));
            Assert.That(generation2.GetProfileName("Cell 1 - HDD", ticI), Is.EqualTo(generation1.GetProfileName("Cell 1 - HDD", ticI)));
            Assert.That(generation2.GetProfileName("Cell 1", ticLL), Is.EqualTo(generation1.GetProfileName("Cell 1", ticLL)));
            Assert.That(generation2.GetProfileName("Cell 1 - HDD", ticLL), Is.EqualTo(generation1.GetProfileName("Cell 1 - HDD", ticLL)));
        }

        [Test]
        public void Naming_FirstDiscriminator_IsTheSignatureHash()
        {
            ProfileDefinition profileDefinition = Definition(Heating, Flat(21.0, 24));

            string hash = TasQuery.ProfileSignatureHash(profileDefinition);
            Assert.That(hash, Is.Not.Null.And.Length.EqualTo(8));
            Assert.That(hash, Is.EqualTo(TasQuery.ProfileSignatureHash(Definition(Heating, Flat(21.0, 24)))), "The discriminator must be stable across repeated construction.");

            string name = TasQuery.ProfileName(new HashSet<string>(StringComparer.Ordinal) { "Setpoint" }, profileDefinition, "Setpoint");
            Assert.That(name, Is.EqualTo("Setpoint_" + hash));
        }

        [Test]
        public void Naming_CollisionAfterTheFirstDiscriminator_ExtendsDeterministically()
        {
            ProfileDefinition profileDefinition = Definition(Heating, Flat(21.0, 24));
            string hash = TasQuery.ProfileSignatureHash(profileDefinition);

            //Both the base and its signature-qualified form are already taken - reachable when the bounded
            //fingerprint collides, or when another definition's own preferred name is that exact string.
            HashSet<string> claimed = new HashSet<string>(StringComparer.Ordinal) { "Setpoint", "Setpoint_" + hash };

            string name = TasQuery.ProfileName(claimed, profileDefinition, "Setpoint");
            Assert.That(name, Is.EqualTo("Setpoint_" + hash + "_2"), "The extension must be deterministic, not a refusal and not an overwrite.");

            claimed.Add(name);
            Assert.That(TasQuery.ProfileName(claimed, profileDefinition, "Setpoint"), Is.EqualTo("Setpoint_" + hash + "_3"));

            //Repeating the same question against the same claim set gives the same answer.
            Assert.That(TasQuery.ProfileName(claimed, profileDefinition, "Setpoint"), Is.EqualTo("Setpoint_" + hash + "_3"));
        }

        [Test]
        public void Naming_ReversedRegistrationOrder_GivesIdenticalNames()
        {
            List<Slot> slots = Slots_ModelA();

            Dictionary<string, string> forward = Names(Index(slots));

            List<Slot> reversed = new List<Slot>(slots);
            reversed.Reverse();
            Dictionary<string, string> backward = Names(Index(reversed));

            Assert.That(backward, Is.EqualTo(forward));

            //And an order that interleaves the internal conditions rather than merely reversing them.
            List<Slot> interleaved = slots.OrderBy(x => x.Index).ThenBy(x => x.InternalConditionName, StringComparer.Ordinal).ToList();
            Assert.That(Names(Index(interleaved)), Is.EqualTo(forward));
        }

        [Test]
        public void Naming_RepeatedBuild_GivesIdenticalNames()
        {
            Dictionary<string, string> first = Names(Index(Slots_ModelA()));
            Dictionary<string, string> second = Names(Index(Slots_ModelA()));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Naming_OrderingIsOrdinal_NotCultureAware()
        {
            //"Z" sorts BEFORE "a" ordinally (0x5A < 0x61) and AFTER it under every common culture. Pinning the
            //ordinal answer is what stops a name persisted into a model depending on the machine's locale.
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticLL, Heating, "a schedule", Flat(21.0, 24));
            Register(profileReuseIndex, "IC B", ticLL, Heating, "Z schedule", Flat(21.0, 24));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.Profiles.Count, Is.EqualTo(1));
            Assert.That(profileReuseIndex.Profiles[0].Name, Is.EqualTo("Z schedule"));

            //Case is significant too - two names differing only in case are two candidates, not one.
            ProfileReuseIndex profileReuseIndex_Case = new ProfileReuseIndex();
            Register(profileReuseIndex_Case, "IC A", ticLL, Heating, "setpoint", Flat(21.0, 24));
            Register(profileReuseIndex_Case, "IC B", ticLL, Heating, "SETPOINT", Flat(21.0, 24));
            profileReuseIndex_Case.Resolve();

            Assert.That(profileReuseIndex_Case.Profiles[0].Name, Is.EqualTo("SETPOINT"));
        }

        [Test]
        public void Naming_NameBase_NormalisesWhitespaceAndKeepsUnderscores()
        {
            Assert.That(TasQuery.ProfileNameBase("  HTG_7to19_21  "), Is.EqualTo("HTG_7to19_21"));
            Assert.That(TasQuery.ProfileNameBase("No   Humidification"), Is.EqualTo("No Humidification"));
            Assert.That(TasQuery.ProfileNameBase(null), Is.EqualTo(TasQuery.ProfileNameBase_Default));
            Assert.That(TasQuery.ProfileNameBase("   "), Is.EqualTo(TasQuery.ProfileNameBase_Default));
            Assert.That(TasQuery.ProfileNameBase("A\tB"), Is.EqualTo("A B"));
            Assert.That(TasQuery.ProfileNameBase(new string('x', TasQuery.ProfileNameBaseLimit + 40)).Length, Is.EqualTo(TasQuery.ProfileNameBaseLimit));

            //Two names that normalise alike are one candidate, so the definitions behind them stay separate by
            //discriminator rather than by merging.
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "IC A", ticLL, Heating, "Set  point", Flat(21.0, 24));
            Register(profileReuseIndex, "IC B", ticLL, Heating, " Set point ", Flat(22.0, 24));
            profileReuseIndex.Resolve();

            List<string> names = profileReuseIndex.Profiles.ConvertAll(x => x.Name);
            Assert.That(names.Count, Is.EqualTo(2));
            Assert.That(names.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(2));
            Assert.That(names, Has.Member("Set point"));
        }

        // =================================================================================================
        // 17-21. Reference integrity
        // =================================================================================================

        [Test]
        public void References_EverySlotResolvesToExactlyOneLibraryDefinition()
        {
            List<Slot> slots = Slots_ModelA();
            ProfileReuseIndex profileReuseIndex = Index(slots);
            ProfileLibrary profileLibrary = Library(profileReuseIndex);

            foreach (Slot slot in slots)
            {
                string name = profileReuseIndex.GetProfileName(slot.InternalConditionName, slot.Index);
                Assert.That(name, Is.Not.Null.And.Not.Empty, "Slot " + slot.InternalConditionName + "/" + slot.Index + " resolved to nothing.");

                List<Profile> matches = profileLibrary.GetProfiles().FindAll(x => string.Equals(x.Name, name, StringComparison.Ordinal) && string.Equals(x.Category, slot.Category, StringComparison.Ordinal));
                Assert.That(matches.Count, Is.EqualTo(1), "Slot " + slot.InternalConditionName + "/" + slot.Index + " must name exactly one library definition, found " + matches.Count + ".");

                //18. the resolved category is the slot's category...
                Assert.That(matches[0].Category, Is.EqualTo(slot.Category));

                //19. ...and the resolved values are the slot's complete values, not a truncation or a summary.
                Assert.That(matches[0].GetValues(), Is.EqualTo(slot.ProfileValues));
            }
        }

        [Test]
        public void References_ResolveThroughTheSameLookupSAMItselfUses()
        {
            //The export reads a reference back with InternalCondition.GetProfile(profileType, profileLibrary),
            //which filters by ProfileType rather than by the raw category string. A name that resolves by
            //category but not by profile type would still be dangling at export time.
            List<Slot> slots = Slots_ModelA();
            ProfileReuseIndex profileReuseIndex = Index(slots);
            ProfileLibrary profileLibrary = Library(profileReuseIndex);

            foreach (Slot slot in slots)
            {
                string name = profileReuseIndex.GetProfileName(slot.InternalConditionName, slot.Index);

                InternalCondition internalCondition = new InternalCondition(slot.InternalConditionName);
                ProfileType profileType = Core.Query.Enum<ProfileType>(slot.Category);
                Assume.That(profileType, Is.Not.EqualTo(ProfileType.Undefined));

                internalCondition.SetProfileName(profileType, name);

                Profile profile = internalCondition.GetProfile(profileType, profileLibrary, false);
                Assert.That(profile, Is.Not.Null, "Reference '" + name + "' did not resolve as a " + profileType + " profile.");
                Assert.That(profile.GetValues(), Is.EqualTo(slot.ProfileValues));
            }
        }

        [Test]
        public void References_TemplateInternalConditionResolvesThroughTheSameIndex()
        {
            //The uncovered path: an internal condition that no zone owns, imported by
            //Modify.AddUnusedInternalConditions. Its slots must resolve against the SAME index the library was
            //built from, or the templates keep legacy references the library no longer carries.
            List<Slot> slots = new List<Slot>();
            slots.AddRange(Slots_Cell("Cell 1"));

            const string template = "Unassigned Activity";
            slots.AddRange(Slots_Cell(template));

            ProfileReuseIndex profileReuseIndex = Index(slots);
            ProfileLibrary profileLibrary = Library(profileReuseIndex);

            //The template carries the same activity as the zone-owned condition, so it adds no definitions at all.
            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(Index(Slots_Cell("Cell 1")).DefinitionCount));

            foreach (Slot slot in slots.FindAll(x => x.InternalConditionName == template))
            {
                string name = profileReuseIndex.GetProfileName(template, slot.Index);
                Assert.That(name, Is.Not.Null);
                Assert.That(name, Does.Not.Contain(template), "A shared profile name may never carry an internal condition's identity.");
                Assert.That(profileLibrary.GetProfiles().FindAll(x => x.Name == name && x.Category == slot.Category).Count, Is.EqualTo(1));

                //And it is the very same entry the zone-owned condition references.
                Assert.That(profileReuseIndex.GetProfileName("Cell 1", slot.Index), Is.EqualTo(name));
            }
        }

        [Test]
        public void References_AmbiguousSlotKeyAnswersNothing_AndTheDefinitionalLookupCoversBothSides()
        {
            //A slot key is (internal condition name, slot), and a NAME is not an identity. Two TBD internal
            //conditions sharing a name and disagreeing on a slot must not make that key answer for one of them -
            //that would be a wrong reference on the other. It stops answering instead.
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "Duplicate", ticLL, Heating, "Setpoint A", Flat(21.0, 24));
            Register(profileReuseIndex, "Duplicate", ticLL, Heating, "Setpoint B", Flat(19.0, 24));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(2), "Both definitions must still exist.");
            Assert.That(profileReuseIndex.GetProfileName("Duplicate", ticLL), Is.Null, "An ambiguous slot key must answer nothing rather than answer wrongly.");

            //And the definitional lookup - which the conversion falls back to - answers correctly for each.
            Assert.That(profileReuseIndex.GetProfileName(Heating, Flat(21.0, 24)), Is.EqualTo("Setpoint A"));
            Assert.That(profileReuseIndex.GetProfileName(Heating, Flat(19.0, 24)), Is.EqualTo("Setpoint B"));
        }

        [Test]
        public void References_SlotThatIsSharedOnOneConditionAndZeroLengthOnAnother_AnswersNothing()
        {
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "Duplicate", ticLG, Lighting, "8to18", Flat(1.0, 24));
            Register(profileReuseIndex, "Duplicate", ticLG, Lighting, "Daylight", new double[0]);
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.GetProfileName("Duplicate", ticLG), Is.Null);

            //The shared definition resolves by definition; the zero-length one falls through to its legacy name,
            //and the library carries both entries so neither reference dangles.
            Assert.That(profileReuseIndex.GetProfileName(Lighting, Flat(1.0, 24)), Is.EqualTo("8to18"));
            Assert.That(profileReuseIndex.GetProfileName(Lighting, new double[0]), Is.Null);

            List<string> names = Library(profileReuseIndex).GetProfiles().ConvertAll(x => x.Name);
            Assert.That(names, Is.EquivalentTo(new[] { "8to18", "Duplicate [Daylight]" }));
        }

        [Test]
        public void References_SlotThatIsZeroLengthOnBothConditionsUnderDifferentNames_AnswersNothing()
        {
            //The reusable path's failure shape, on the exclusion path. One slot key, two TBD internal conditions
            //sharing a name, two DIFFERENT zero-length profiles. Answering the first legacy name for the second
            //condition would be a SILENT MISREFERENCE - it names a real library entry, just the wrong one - so
            //the key must stop answering and let each condition fall back to its own legacy name.
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();

            Assert.That(Register(profileReuseIndex, "Duplicate", ticLG, Lighting, "Daylight", new double[0]), Is.False);
            Assert.That(Register(profileReuseIndex, "Duplicate", ticLG, Lighting, "Dimmer", new double[0]), Is.False);
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.GetProfileName("Duplicate", ticLG), Is.Null, "An ambiguous slot key must answer nothing rather than answer the other condition's profile.");

            //And both legacy names the conversion falls back to still name a library entry, so neither dangles.
            List<string> names_Excluded = Library(profileReuseIndex).GetProfiles().ConvertAll(x => x.Name);
            Assert.That(names_Excluded, Is.EquivalentTo(new[] { "Duplicate [Daylight]", "Duplicate [Dimmer]" }));
        }

        [Test]
        public void References_SlotRegisteredTwiceWithTheSameZeroLengthName_KeepsAnswering()
        {
            //The other half of that rule: reaching one internal condition twice - the building walk visits a
            //template condition and the zone that owns it - is agreement, not a disagreement, so the fast path
            //must keep answering and the library must still carry exactly one entry.
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();

            Register(profileReuseIndex, "Duplicate", ticLG, Lighting, "Daylight", new double[0]);
            Register(profileReuseIndex, "Duplicate", ticLG, Lighting, "Daylight", new double[0]);
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.GetProfileName("Duplicate", ticLG), Is.EqualTo("Duplicate [Daylight]"));
            Assert.That(Library(profileReuseIndex).GetProfiles().Count, Is.EqualTo(1));
        }

        [Test]
        public void References_AllTBDInternalConditionConversionPathsAcceptTheIndex()
        {
            //Structural, and deliberately so: the whole scheme fails silently if ONE conversion path is left
            //without the index, and the failure surfaces as a dangling reference rather than an exception.
            AssertHasProfileReuseIndexParameter(typeof(Analytical.Tas.Convert), "ToSAM", "TBD.InternalCondition");
            AssertHasProfileReuseIndexParameter(typeof(Analytical.Tas.Convert), "ToSAM", "TBD.zone");
            AssertHasProfileReuseIndexParameter(typeof(Analytical.Tas.Convert), "ToSAM", "TBD.Building");
            AssertHasProfileReuseIndexParameter(typeof(Analytical.Tas.Convert), "ToSAM_ProfileLibrary", "TBD.Building");
            AssertHasProfileReuseIndexParameter(typeof(Analytical.Tas.Modify), "AddUnusedInternalConditions", "SAM.Analytical.AdjacencyCluster");
        }

        [Test]
        public void References_VentilationSlotIsCollected_SoItsReferenceResolves()
        {
            //The import has always written InternalConditionParameter.VentilationProfileName but never emitted
            //the ticV profile behind it, so the reference dangled. ticV is now collected like every other
            //internal-gain slot, and the reference resolves.
            //
            //These tables now drive BOTH collectors - Query.ProfileReuseIndex's registration walk and the
            //legacy Convert.ToSAM_Profiles mirror both foreach over them, gated by the same
            //Query.IsCollectableSlot - so this assertion pins the emitted set on both sides at once. That
            //closes the gap noted in review: while the mirror repeated the twelve slots by hand, a slot could
            //be emitted by one collector and not the other, which is a reference naming a definition the
            //library does not carry.
            IEnumerable<string> slots = ProductionSlotNames("ProfileSlots_InternalGain").Concat(ProductionSlotNames("ProfileSlots_Thermostat")).ToList();

            Assert.That(slots, Has.Member("ticV"));
            Assert.That(slots, Is.EquivalentTo(new[] { "ticI", "ticV", "ticLG", "ticOLG", "ticOSG", "ticESG", "ticELG", "ticCOG", "ticUL", "ticLL", "ticHLL", "ticHUL" }));

            //Two internal conditions carrying the same ventilation schedule share ONE definition, exactly as
            //every other slot does. (Representative shape - not read from a real model.)
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "Cell 1", ticV, Ventilation, "Min Fresh Air", Flat(1.0, 24));
            Register(profileReuseIndex, "Cell 2", ticV, Ventilation, "Min Fresh Air", Flat(1.0, 24));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(1));

            string name = profileReuseIndex.GetProfileName("Cell 1", ticV);
            Assert.That(name, Is.EqualTo("Min Fresh Air"));
            Assert.That(profileReuseIndex.GetProfileName("Cell 2", ticV), Is.EqualTo(name));

            //And the name resolves through the very lookup the export uses
            //(InternalCondition.GetProfile(profileType, profileLibrary)).
            ProfileLibrary profileLibrary = Library(profileReuseIndex);
            InternalCondition internalCondition = new InternalCondition("Cell 1");
            internalCondition.SetProfileName(ProfileType.Ventilation, name);

            Profile profile = internalCondition.GetProfile(ProfileType.Ventilation, profileLibrary, false);
            Assert.That(profile, Is.Not.Null, "The ventilation reference must resolve as a Ventilation profile.");
            Assert.That(profile.GetValues(), Is.EqualTo(Flat(1.0, 24)));

            //And the production guard agrees this shape is collectable at all - the boundary the
            //zero-length tests below hold the other side of.
            Assert.That(ProductionIsCollectableSlot(ticV, Flat(1.0, 24)), Is.True);
        }

        // =================================================================================================
        // Zero-length (TAS function) ventilation must NOT ride the new collected path
        // =================================================================================================

        [Test]
        public void ZeroLength_Ventilation_IsNotCollectable_SoItCannotBecomeAResolvableValueProfile()
        {
            //Core.Tas.Query.Values has no case for ticFunctionProfile, so a TAS function
            //profile flattens to ZERO values. Collecting ticV would have given that zero-length profile a
            //ProfileLibrary entry under its legacy name - the very name the import writes as
            //VentilationProfileName - so the reference would resolve, the export would call the ordinary
            //value writer, and Modify.Update would replace the TAS function profile with a 24-value hourly
            //one (see ZeroLength_ProfileCountIsZero_... below for why Count == 0 lands there).
            //ticV is therefore collected only when its values are a COMPLETE representation.
            Assert.That(ProductionIsCollectableSlot(ticV, Flat(1.0, 24)), Is.True, "An ordinary hourly ticV is collected.");
            Assert.That(ProductionIsCollectableSlot(ticV, new[] { 0.5 }), Is.True, "An ordinary single-value ticV is collected.");
            Assert.That(ProductionIsCollectableSlot(ticV, new double[0]), Is.False, "A zero-length (function) ticV must NOT be collected.");
            Assert.That(ProductionIsCollectableSlot(ticV, null), Is.False, "An unreadable ticV must NOT be collected.");
        }

        [Test]
        public void ZeroLength_Ventilation_NotCollected_LeavesTheReferenceDanglingExactlyAsBefore()
        {
            //What the guard buys: the slot is never registered, so the index has no name for it and no
            //library entry under one. The import then falls back to the legacy name (Convert.ToSAM's
            //ProfileName helper returns name_Legacy when the index answers nothing), that name resolves to
            //nothing, and the export's GetProfile returns null - so Modify.Update is never reached and the
            //TAS function profile survives untouched. That is precisely the pre-PR-#38 behaviour.
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Register(profileReuseIndex, "Cell 1", ticI, Infiltration, "Constant", Flat(1.0, 24));
            //...and NO ticV registration, because the production guard refused it.
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.GetProfileName("Cell 1", ticV), Is.Null,
                "An uncollected slot must not answer a name.");
            Assert.That(profileReuseIndex.GetProfileName(Ventilation, new double[0]), Is.Null,
                "Nor may the value lookup answer for an empty definition.");

            ProfileLibrary profileLibrary = Library(profileReuseIndex);
            Assert.That(profileLibrary.GetProfiles().FindAll(x => x.ProfileType == ProfileType.Ventilation), Is.Empty,
                "No ventilation definition may exist for a zero-length ticV.");

            //The reference the import writes in that case, and the export's own lookup for it.
            InternalCondition internalCondition = new InternalCondition("Cell 1");
            internalCondition.SetProfileName(ProfileType.Ventilation, Legacy("Cell 1", "Fresh Air Function"));

            Assert.That(internalCondition.GetProfile(ProfileType.Ventilation, profileLibrary, false), Is.Null,
                "The export must not resolve a zero-length ventilation profile - resolving it is what let the "
                + "ordinary value writer overwrite a TAS function profile.");
        }

        [Test]
        public void ZeroLength_ProfileCountIsZero_WhichIsWhyResolvingItWouldCorruptTheFunctionProfile()
        {
            //Modify.Update guards on Count == -1, then branches Count == 1 -> ticValueProfile and
            //Count <= 24 -> ticHourlyProfile. A zero-length profile reports Count == 0, so the guard misses
            //it and it lands in the 24-hour branch. Pinned so the hazard the guard avoids stays visible
            //without needing TAS - and so this starts failing if Update ever learns to handle Count 0.
            Profile profile = new Profile("Fresh Air Function", ProfileType.Ventilation, new List<double>());

            Assert.That(profile.Count, Is.EqualTo(0), "A zero-length profile reports Count 0, not -1.");
            Assert.That(profile.Count, Is.Not.EqualTo(-1), "So Modify.Update's Count == -1 guard does not catch it.");
            Assert.That(profile.Count, Is.LessThanOrEqualTo(24), "And it falls into the ticHourlyProfile branch.");
        }

        [Test]
        public void ZeroLength_Ventilation_ReservesItsLegacyName_SoNoCanonicalDefinitionCanClaimIt()
        {
            //Skipping the slot is not enough on its own: the dangling reference still HAS a name,
            //and if a canonical name were later assigned that same string in the same category the reference
            //would stop dangling and start resolving to an unrelated value profile - which the export would
            //then write over the function profile. Not theoretical: a round-tripped model's TAS profile names
            //ARE "{condition} [{profile}]" strings, because that is exactly what Modify.Update writes back.
            string name_Skipped = Legacy("Cell 1", "Fresh Air Function");

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();

            //The production skip path reserves the name it leaves dangling...
            profileReuseIndex.Reserve(Ventilation, name_Skipped);

            //...and here is the adversary: an ordinary ticV definition whose own TAS source name is that very
            //string, which without the reservation would be handed it as its canonical name.
            Register(profileReuseIndex, "Cell 2", ticV, Ventilation, name_Skipped, Flat(1.0, 24));
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(1));

            string name_Resolved = profileReuseIndex.GetProfileName("Cell 2", ticV);
            Assert.That(name_Resolved, Is.Not.Null);
            Assert.That(name_Resolved, Is.Not.EqualTo(name_Skipped),
                "A canonical name must never be the reserved name of a skipped function profile.");

            //So the skipped condition's reference still resolves to nothing.
            ProfileLibrary profileLibrary = Library(profileReuseIndex);
            InternalCondition internalCondition = new InternalCondition("Cell 1");
            internalCondition.SetProfileName(ProfileType.Ventilation, name_Skipped);

            Assert.That(internalCondition.GetProfile(ProfileType.Ventilation, profileLibrary, false), Is.Null,
                "The skipped function profile's reference must stay dangling.");

            //While the ordinary definition is unaffected and still resolves for its own condition.
            InternalCondition internalCondition_Ordinary = new InternalCondition("Cell 2");
            internalCondition_Ordinary.SetProfileName(ProfileType.Ventilation, name_Resolved);
            Assert.That(internalCondition_Ordinary.GetProfile(ProfileType.Ventilation, profileLibrary, false), Is.Not.Null);
        }

        [Test]
        public void ZeroLength_Ventilation_SlotKeyCollision_WithOrdinaryTicVOnSameNamedCondition_StaysDangling()
        {
            //Follow-up on this same guard. Two TBD internal conditions CAN share a name - a
            //duplicate space name, a generic template - and still differ on ticV: one a genuine schedule, the
            //other a zero-length TAS function profile. Both then compete for the SAME slot key
            //(internalConditionName, ticV). Reserve() alone (the test above) protects only a coincidental
            //STRING collision between UNRELATED conditions; it does nothing here, because Reserve never
            //touches definitionsBySlot/excludedNamesBySlot. The production skip path therefore still calls
            //Register (not just Reserve), with suppressLibraryEntry - so the shared key's ambiguity tracking,
            //the SAME mechanism that already protects every other excluded slot, runs regardless of order.
            const string name = "Duplicate";

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();

            //First registration on "Duplicate": a zero-length (function) ticV, exactly as the production
            //skip path registers it - library entry suppressed, name reserved.
            string name_Skipped = Legacy(name, "Fresh Air Function");
            profileReuseIndex.Register(name, ticV, Ventilation, new double[0], "Fresh Air Function", name_Skipped, suppressLibraryEntry: true);
            profileReuseIndex.Reserve(Ventilation, name_Skipped);

            //Second registration on the SAME name, SAME slot: an ordinary ticV schedule.
            Register(profileReuseIndex, name, ticV, Ventilation, "Min Fresh Air", Flat(1.0, 24));

            profileReuseIndex.Resolve();

            //The shared slot key must answer NOTHING - not the function profile's legacy name, and not the
            //ordinary profile's canonical name either. Both conditions fall back to their own lookups instead.
            Assert.That(profileReuseIndex.GetProfileName(name, ticV), Is.Null,
                "A slot key shared by a genuine definition and a skipped function profile must answer nothing.");

            //The function condition's own reference: the slot-key lookup answers null, so it falls back to
            //its legacy name, which the library does not carry - the safe dangling behaviour.
            ProfileLibrary profileLibrary = Library(profileReuseIndex);
            InternalCondition internalCondition_Function = new InternalCondition(name);
            internalCondition_Function.SetProfileName(ProfileType.Ventilation, name_Skipped);

            Assert.That(internalCondition_Function.GetProfile(ProfileType.Ventilation, profileLibrary, false), Is.Null,
                "The function condition's reference must stay dangling, NOT resolve to the ordinary condition's profile.");

            //The ordinary condition is unaffected: its slot-key lookup also answers null (ambiguous), but the
            //value-based fallback GetProfileName(category, values) - keyed on the definition itself, not the
            //shared slot - still finds its own registered definition.
            string name_Ordinary = profileReuseIndex.GetProfileName(Ventilation, Flat(1.0, 24));
            Assert.That(name_Ordinary, Is.Not.Null);
            Assert.That(name_Ordinary, Is.Not.EqualTo(name_Skipped));

            InternalCondition internalCondition_Ordinary = new InternalCondition(name);
            internalCondition_Ordinary.SetProfileName(ProfileType.Ventilation, name_Ordinary);

            Profile profile_Ordinary = internalCondition_Ordinary.GetProfile(ProfileType.Ventilation, profileLibrary, false);
            Assert.That(profile_Ordinary, Is.Not.Null);
            Assert.That(profile_Ordinary.GetValues(), Is.EqualTo(Flat(1.0, 24)));
        }

        [Test]
        public void Reserve_AfterResolve_IsRefused()
        {
            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            profileReuseIndex.Resolve();

            Assert.Throws<InvalidOperationException>(() => profileReuseIndex.Reserve(Ventilation, "Cell 1 [X]"));
        }

        [Test]
        public void ZeroLength_NonVentilationSlots_KeepTheirPR37ExclusionBehaviour()
        {
            //The guard is deliberately ticV-only. Every other slot keeps exactly the PR #37 treatment: a
            //zero-length profile is excluded from dedup but still gets its own legacy-named library entry.
            //Changing that is function-profile work, not this fix.
            foreach (int slot in new[] { ticI, ticLG, ticOLG, ticOSG, ticESG, ticELG, ticCOG, ticUL, ticLL, ticHLL, ticHUL })
            {
                Assert.That(ProductionIsCollectableSlot(slot, new double[0]), Is.True,
                    "Slot " + slot + " must keep its PR #37 zero-length behaviour.");
            }

            ProfileReuseIndex profileReuseIndex = new ProfileReuseIndex();
            Assert.That(Register(profileReuseIndex, "Cell 1", ticI, Infiltration, "Function", new double[0]), Is.False,
                "A zero-length profile is never a reusable definition.");
            profileReuseIndex.Resolve();

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(0));
            Assert.That(profileReuseIndex.GetProfileName("Cell 1", ticI), Is.EqualTo(Legacy("Cell 1", "Function")),
                "The excluded slot still answers its legacy name, as PR #37 established.");
            Assert.That(Library(profileReuseIndex).GetProfiles().FindAll(x => x.ProfileType == ProfileType.Infiltration), Has.Count.EqualTo(1),
                "And still carries its own library entry.");
        }

        // =================================================================================================
        // The ModelA-Tas regression
        // =================================================================================================

        [Test]
        public void ModelA_FortyTwoProfilesCollapseToTwenty()
        {
            List<Slot> slots = Slots_ModelA();
            Assert.That(slots.Count, Is.EqualTo(44), "Four internal conditions x eleven collected slots.");

            //What the legacy import produced: one profile per slot, named "{internal condition} [{profile}]",
            //silently collapsed by the library key "{Category}::{Name}" - which is how 44 slots became the 42
            //entries ModelA-Tas.sam holds.
            HashSet<string> legacyKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Slot slot in slots)
            {
                legacyKeys.Add(slot.Category + "::" + Legacy(slot.InternalConditionName, slot.SourceName));
            }

            Assert.That(legacyKeys.Count, Is.EqualTo(42), "ModelA-Tas.sam holds 42 profiles.");

            ProfileReuseIndex profileReuseIndex = Index(slots);
            ProfileLibrary profileLibrary = Library(profileReuseIndex);

            Assert.That(profileReuseIndex.DefinitionCount, Is.EqualTo(20), "42 profiles are 20 distinct (Category, flattened Values) definitions.");
            Assert.That(profileLibrary.GetProfiles().Count, Is.EqualTo(20));

            //Behavioural, not a hard-coded list: every library entry is a distinct definition, and every
            //definition has exactly one entry.
            List<Profile> profiles = profileLibrary.GetProfiles();
            Assert.That(profiles.Select(x => new ProfileDefinition(x.Category, x.GetValues())).Distinct().Count(), Is.EqualTo(profiles.Count));
            Assert.That(profiles.Select(x => x.Category + "::" + x.Name).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(profiles.Count));

            //And no shared name carries a zone or internal-condition identity any more.
            foreach (Profile profile in profiles)
            {
                Assert.That(profile.Name, Does.Not.Contain("Cell 1"));
                Assert.That(profile.Name, Does.Not.Contain("Cell 2"));
                Assert.That(profile.Name, Does.Not.Contain("["));
            }
        }

        [Test]
        public void ModelA_TheTwoNameCollisionsAreDiscriminated()
        {
            ProfileReuseIndex profileReuseIndex = Index(Slots_ModelA());

            //Both collisions come from a one-value HDD sizing profile and a 24-value design profile sharing a
            //source name. Neither definition is dropped and neither name is reused.
            AssertCollision(profileReuseIndex, Infiltration, "Constant", new[] { 0.20000000298023224 }, Flat(1.0, 24));
            AssertCollision(profileReuseIndex, Heating, "HTG_7to19_21", new[] { 21.0 }, Values(16, 7, 21, 12, 16, 5));

            //Every other group keeps its bare source name - the discriminator is the exception, not the rule.
            Assert.That(profileReuseIndex.GetProfileName(Lighting, Values(0, 7, 0.5, 1, 1, 10, 0.5, 1, 0, 5)), Is.EqualTo("8to18"));
            Assert.That(profileReuseIndex.GetProfileName(Cooling, Values(28, 7, 25, 12, 28, 5)), Is.EqualTo("CLG_7to19_25"));
            Assert.That(profileReuseIndex.GetProfileName(Occupancy, new[] { 0.0 }), Is.EqualTo("Occupancy Latent Gain"));
            Assert.That(profileReuseIndex.GetProfileName(Dehumidification, Flat(100.0, 24)), Is.EqualTo("No Dehumidification"));
        }

        // =================================================================================================
        // Helpers
        // =================================================================================================

        private static void AssertCollision(ProfileReuseIndex profileReuseIndex, string category, string sourceName, double[] values_One, double[] values_Many)
        {
            string name_One = profileReuseIndex.GetProfileName(category, values_One);
            string name_Many = profileReuseIndex.GetProfileName(category, values_Many);

            Assert.That(name_One, Is.Not.Null);
            Assert.That(name_Many, Is.Not.Null);
            Assert.That(name_One, Is.Not.EqualTo(name_Many), category + "::" + sourceName + " is two definitions and must be two names.");

            Assert.That(name_One, Is.EqualTo(sourceName), "Claim order is by value count, so the one-value definition keeps the bare name.");
            Assert.That(name_Many, Is.EqualTo(sourceName + "_" + TasQuery.ProfileSignatureHash(new ProfileDefinition(category, values_Many))));
        }

        /// <summary>Every slot's resolved name, keyed by internal condition and slot - the whole naming outcome.</summary>
        private static Dictionary<string, string> Names(ProfileReuseIndex profileReuseIndex)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Slot slot in Slots_ModelA())
            {
                result[slot.InternalConditionName + "/" + slot.Index] = profileReuseIndex.GetProfileName(slot.InternalConditionName, slot.Index);
            }

            return result;
        }

        /// <summary>
        /// The <c>TBD.Profiles</c> members of one of <c>Query</c>'s production slot tables, by name. Read
        /// reflectively so this project still needs no Interop.TBD reference of its own.
        /// </summary>
        /// <summary>
        /// The production collectability guard (<c>Query.IsCollectableSlot</c>), by reflection - the test
        /// project deliberately carries no Interop.TBD reference, and internals are not visible to it.
        /// </summary>
        private static bool ProductionIsCollectableSlot(int slot, double[] values)
        {
            MethodInfo methodInfo = typeof(Analytical.Tas.Query).GetMethod("IsCollectableSlot", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(methodInfo, Is.Not.Null, "Query.IsCollectableSlot no longer exists - the guard this test pins has moved or been renamed.");

            return (bool)methodInfo.Invoke(null, new object[] { slot, values });
        }

        private static List<string> ProductionSlotNames(string fieldName)
        {
            FieldInfo fieldInfo = typeof(Analytical.Tas.Query).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(fieldInfo, Is.Not.Null, "Query." + fieldName + " no longer exists - the slot table this test pins has moved or been renamed.");

            List<string> result = new List<string>();
            foreach (object entry in (IEnumerable)fieldInfo.GetValue(null))
            {
                result.Add(entry.GetType().GetProperty("Key").GetValue(entry).ToString());
            }

            return result;
        }

        private static void AssertHasProfileReuseIndexParameter(Type type, string methodName, string firstParameterTypeName)
        {
            List<MethodInfo> methodInfos = type
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.Name == methodName)
                .Where(x => x.GetParameters().Length != 0 && x.GetParameters()[0].ParameterType.FullName == firstParameterTypeName)
                .ToList();

            Assert.That(methodInfos, Is.Not.Empty, type.Name + "." + methodName + "(" + firstParameterTypeName + ", ...) no longer exists.");
            Assert.That(
                methodInfos.Any(x => x.GetParameters().Any(y => y.ParameterType == typeof(ProfileReuseIndex))),
                Is.True,
                type.Name + "." + methodName + "(" + firstParameterTypeName + ", ...) takes no ProfileReuseIndex, so that conversion path cannot share the model's profile definitions.");
        }
    }
}
