// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Guards the MAGNITUDE half of every profile-backed internal gain: what the TBD export writes as a
    /// profile's factor must be exactly what the TBD import reads back as the gain.
    /// <para>
    /// The defect these pin: the import read <c>profile.GetExtremeValue(true)</c> - which TAS defines as
    /// <c>factor * max(values)</c> - while the export writes the magnitude as <c>profile.factor</c> and
    /// the schedule as the profile's raw values. One generation of a round trip was therefore
    /// </para>
    /// <code>G(n+1) = G(n) * max(values)</code>
    /// <para>
    /// a fixed point only for a schedule normalised to a peak of 1.0. A licensed 3-generation chain on a
    /// TM59 residential model showed the kitchen occupancy gain, whose schedule peaks at 0.25, going
    /// 0.5 -> 0.125 -> 0.03125 W/m2 while lighting, equipment and infiltration - all peaking at 1.0 - held
    /// still. See <c>SAM.Analytical.Tas/INTERNAL_GAIN_MAGNITUDE_AUTHORITY.md</c>.
    /// </para>
    /// <para>
    /// These are COM-free but they are NOT a hand-written mirror of the production arithmetic: the real
    /// <c>Modify.UpdateInternalCondition</c>, <c>Convert.ToSAM</c> and <c>Convert.ToSAM_Profiles</c> run
    /// here, against the managed TBD stand-ins in <see cref="FakeProfile"/> and friends. That is what
    /// makes a "fixed point" claim about three generations mean the shipped code, not a model of it.
    /// </para>
    /// </summary>
    [TestFixture]
    public class InternalGainMagnitudeTests
    {
        private const double Area = 40.0;              // m2
        private const double Volume = 100.0;           // m3
        private const double AreaPerPerson = 10.0;     // m2/p  -> 4 people
        private const double SensiblePerPerson = 75.0; // W/p
        private const double LatentPerPerson = 55.0;   // W/p
        private const double Tolerance = 1E-5;

        // The authored per-area gains those imply, and so the ticOSG/ticOLG factors the export must write
        // in EVERY generation: 75 W/p * 4 p / 40 m2 and 55 W/p * 4 p / 40 m2.
        private const double SensiblePerArea = SensiblePerPerson / AreaPerPerson; // 7.5 W/m2
        private const double LatentPerArea = LatentPerPerson / AreaPerPerson;     // 5.5 W/m2

        // =================================================================================================
        // Fixture helpers
        // =================================================================================================

        /// <summary>
        /// A 24-hour occupancy shape whose maximum is exactly <paramref name="peak"/>. The shape itself is
        /// the same at every peak, so any difference a test sees is caused by the peak alone.
        /// </summary>
        private static List<double> Schedule(double peak)
        {
            double[] shape = { 0, 0, 0, 0, 0, 0, 0.2, 0.4, 1, 1, 0.8, 0.6, 0.6, 0.6, 0.8, 1, 1, 1, 0.8, 0.6, 0.4, 0.2, 0, 0 };
            return shape.Select(x => x * peak).ToList();
        }

        private static Space Space_WithCondition(InternalCondition internalCondition)
        {
            Space space = new Space("Cell 1");
            space.SetValue(SAM.Analytical.SpaceParameter.Area, Area);
            space.SetValue(SAM.Analytical.SpaceParameter.Volume, Volume);
            space.InternalCondition = internalCondition;
            return space;
        }

        /// <summary>The authored generation 0: occupancy stated per person, plus one occupancy schedule.</summary>
        private static InternalCondition Occupancy_InternalCondition(string profileName)
        {
            InternalCondition result = new InternalCondition("Cell 1");
            result.SetValue(InternalConditionParameter.AreaPerPerson, AreaPerPerson);
            result.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, SensiblePerPerson);
            result.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, LatentPerPerson);
            result.SetValue(InternalConditionParameter.OccupancyProfileName, profileName);
            return result;
        }

        /// <summary>
        /// One SAM -> TBD -> SAM generation, running the production code on both legs.
        /// <para>
        /// The TBD internal condition is fresh each time, exactly as the export builds one, and only the
        /// slots named in <paramref name="slots"/> exist on it.
        /// </para>
        /// </summary>
        private sealed class Generation
        {
            public FakeInternalCondition InternalCondition_TBD;
            public InternalCondition InternalCondition;   // what the import produced
            public ProfileLibrary ProfileLibrary;         // the library the NEXT export resolves against
            public double SensibleGain;                   // Query.OccupancySensibleGain of the exported space, W
            public double LatentGain;
            public double Occupancy;

            public FakeProfile Profile(TBD.Profiles slot)
            {
                return InternalCondition_TBD.InternalGain.Get(slot);
            }

            public double Factor(TBD.Profiles slot)
            {
                return Profile(slot).factor;
            }
        }

        private static Generation Roundtrip(InternalCondition internalCondition, ProfileLibrary profileLibrary, params TBD.Profiles[] slots)
        {
            Space space = Space_WithCondition(internalCondition);

            Generation result = new Generation
            {
                SensibleGain = SAM.Analytical.Query.OccupancySensibleGain(space),
                LatentGain = SAM.Analytical.Query.OccupancyLatentGain(space),
                Occupancy = SAM.Analytical.Query.CalculatedOccupancy(space),
                InternalCondition_TBD = new FakeInternalCondition(),
            };

            foreach (TBD.Profiles slot in slots)
            {
                result.InternalCondition_TBD.InternalGain.Enable(slot);
            }

            Assert.That(SAM.Analytical.Tas.Modify.UpdateInternalCondition(result.InternalCondition_TBD, space, profileLibrary, null), Is.True,
                "The export refused the condition - the fixture, not the code under test, is wrong.");

            // The import, exactly as Convert.ToSAM(TBD.Building, ...) drives it for one condition: collect
            // the profiles the TBD now holds into a library, then read the condition against that library.
            List<Profile> profiles = SAM.Analytical.Tas.Convert.ToSAM_Profiles(result.InternalCondition_TBD);
            result.ProfileLibrary = new ProfileLibrary("Imported", profiles);
            result.InternalCondition = SAM.Analytical.Tas.Convert.ToSAM(result.InternalCondition_TBD, Area);

            return result;
        }

        /// <summary>Runs <paramref name="count"/> generations, feeding each import into the next export.</summary>
        private static List<Generation> Chain(InternalCondition internalCondition, ProfileLibrary profileLibrary, int count, params TBD.Profiles[] slots)
        {
            List<Generation> result = new List<Generation>();
            for (int i = 0; i < count; i++)
            {
                Generation generation = Roundtrip(internalCondition, profileLibrary, slots);
                result.Add(generation);
                internalCondition = generation.InternalCondition;
                profileLibrary = generation.ProfileLibrary;
            }

            return result;
        }

        private static double Value(InternalCondition internalCondition, InternalConditionParameter internalConditionParameter)
        {
            double result;
            return internalCondition.TryGetValue(internalConditionParameter, out result) ? result : double.NaN;
        }

        // =================================================================================================
        // 1. The seam itself: what a profile's factor means, and what its extreme means
        // =================================================================================================

        [Test]
        public void GainMagnitude_IsTheFactor_NotTheExtreme()
        {
            FakeProfile profile = new FakeProfile { type = TBD.ProfileTypes.ticHourlyProfile, factor = 7.5f };
            List<double> schedule = Schedule(0.25);
            for (int i = 0; i < 24; i++)
            {
                profile.set_hourlyValues(i + 1, (float)schedule[i]);
            }

            Assert.That(SAM.Analytical.Tas.Query.GainMagnitude(profile), Is.EqualTo(7.5).Within(Tolerance),
                "The magnitude is the factor the export wrote.");
            Assert.That(profile.GetExtremeValue(true), Is.EqualTo(7.5 * 0.25).Within(Tolerance),
                "The extreme is factor * max(values) - the peak of the effective curve, not the magnitude.");
        }

        [Test]
        public void Export_WritesMagnitudeAsFactor_AndScheduleAsRawValues()
        {
            //The half of the contract that lives in Modify.Update, pinned directly: the magnitude and the
            //shape are written to two different places and neither is folded into the other.
            Profile profile = new Profile("Occupancy", ProfileType.Occupancy, Schedule(0.25));
            FakeProfile profile_TBD = new FakeProfile();

            Assert.That(SAM.Analytical.Tas.Modify.Update(profile_TBD, profile, 7.5), Is.True);

            Assert.That(profile_TBD.factor, Is.EqualTo(7.5f).Within(Tolerance));
            Assert.That(profile_TBD.Values(), Is.EqualTo(Schedule(0.25)).AsCollection.Within(Tolerance),
                "The schedule is copied across raw - the export does not normalise it and does not scale it.");
        }

        // =================================================================================================
        // 2. The defect, proved before the fix is asserted
        // =================================================================================================

        [TestCase(1.0, ExpectedResult = 1.0)]
        [TestCase(0.5, ExpectedResult = 0.5)]
        [TestCase(0.25, ExpectedResult = 0.25)]
        public double ExtremeBasedImport_DecaysTheGainByTheSchedulePeak_EveryGeneration(double peak)
        {
            //The recurrence the old import produced, derived from the production export plus the one line
            //that was wrong. G(n+1) = G(n) * peak, unbounded - which is what a licensed 3-generation chain
            //measured. This test exists so the fix below is a change of behaviour, not a restatement of it.
            double gainPerArea = SensiblePerArea + LatentPerArea;
            double personGain = SensiblePerPerson + LatentPerPerson;
            double occupancy = Area / AreaPerPerson;

            List<double> factors = new List<double>();
            for (int generation = 0; generation < 3; generation++)
            {
                // Export: the factor is the per-area gain the SAM side currently states.
                double sensible_Factor = SensiblePerPerson * occupancy / Area;
                double latent_Factor = LatentPerPerson * occupancy / Area;
                factors.Add(sensible_Factor);

                // Import, the OLD way: the extreme, not the factor.
                double sensiblePerArea = sensible_Factor * peak;
                double latentPerArea = latent_Factor * peak;
                gainPerArea = sensiblePerArea + latentPerArea;

                // Convert.ToSAM's occupancy derivation, verbatim.
                occupancy = gainPerArea * Area / personGain;
            }

            Assert.That(factors[1] / factors[0], Is.EqualTo(peak).Within(Tolerance));
            Assert.That(factors[2] / factors[1], Is.EqualTo(peak).Within(Tolerance));

            return factors[1] / factors[0];
        }

        // =================================================================================================
        // 3. The fix: occupancy is a fixed point at any schedule peak
        // =================================================================================================

        [TestCase(1.0)]
        [TestCase(0.5)]
        [TestCase(0.25)]
        public void Occupancy_ThreeGenerations_AreAFixedPoint(double peak)
        {
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Occupancy", ProfileType.Occupancy, Schedule(peak)) });
            List<Generation> generations = Chain(Occupancy_InternalCondition("Occupancy"), profileLibrary, 3, TBD.Profiles.ticOSG, TBD.Profiles.ticOLG);

            foreach (Generation generation in generations)
            {
                // The TBD factors - the magnitudes TAS simulates.
                Assert.That(generation.Factor(TBD.Profiles.ticOSG), Is.EqualTo(SensiblePerArea).Within(Tolerance),
                    "ticOSG factor drifted; the round trip is not an inverse.");
                Assert.That(generation.Factor(TBD.Profiles.ticOLG), Is.EqualTo(LatentPerArea).Within(Tolerance),
                    "ticOLG factor drifted; the round trip is not an inverse.");

                // The SAM side that produced them.
                Assert.That(generation.SensibleGain, Is.EqualTo(SensiblePerPerson * Area / AreaPerPerson).Within(Tolerance));
                Assert.That(generation.LatentGain, Is.EqualTo(LatentPerPerson * Area / AreaPerPerson).Within(Tolerance));
                Assert.That(generation.Occupancy, Is.EqualTo(Area / AreaPerPerson).Within(Tolerance),
                    "Occupancy is the quantity the old import actually decayed - the per-person gains never moved.");

                // ... and what the import wrote back for the next generation.
                Assert.That(Value(generation.InternalCondition, InternalConditionParameter.OccupancySensibleGainPerPerson), Is.EqualTo(SensiblePerPerson).Within(Tolerance));
                Assert.That(Value(generation.InternalCondition, InternalConditionParameter.OccupancyLatentGainPerPerson), Is.EqualTo(LatentPerPerson).Within(Tolerance));
                Assert.That(Value(generation.InternalCondition, InternalConditionParameter.AreaPerPerson), Is.EqualTo(AreaPerPerson).Within(Tolerance),
                    "AreaPerPerson is where the decay entered: personGain / gainPerArea inflates when gainPerArea is under-read.");
            }
        }

        [TestCase(1.0)]
        [TestCase(0.5)]
        [TestCase(0.25)]
        public void Occupancy_EffectiveHourlyCurve_IsUnchangedAcrossGenerations(double peak)
        {
            //The scalar factor being stable is necessary but not sufficient: what TAS simulates is
            //factor * schedule, hour by hour. Compare the whole curve, not the peak.
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Occupancy", ProfileType.Occupancy, Schedule(peak)) });
            List<Generation> generations = Chain(Occupancy_InternalCondition("Occupancy"), profileLibrary, 3, TBD.Profiles.ticOSG, TBD.Profiles.ticOLG);

            List<double> expected_Sensible = Schedule(peak).Select(x => SensiblePerArea * x).ToList();
            List<double> expected_Latent = Schedule(peak).Select(x => LatentPerArea * x).ToList();

            for (int i = 0; i < generations.Count; i++)
            {
                Assert.That(generations[i].Profile(TBD.Profiles.ticOSG).EffectiveValues(), Is.EqualTo(expected_Sensible).AsCollection.Within(1E-4),
                    "Generation " + (i + 1) + " sensible occupancy curve differs from the authored one.");
                Assert.That(generations[i].Profile(TBD.Profiles.ticOLG).EffectiveValues(), Is.EqualTo(expected_Latent).AsCollection.Within(1E-4),
                    "Generation " + (i + 1) + " latent occupancy curve differs from the authored one.");
            }
        }

        [Test]
        public void Occupancy_TenGenerations_StillTheSameGain()
        {
            //An unbounded decay does not need many generations to become obvious, but a fixed point should
            //survive an arbitrary number of them. At the old x0.25 per generation this ends at 1e-6 of the
            //authored gain.
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Occupancy", ProfileType.Occupancy, Schedule(0.25)) });
            List<Generation> generations = Chain(Occupancy_InternalCondition("Occupancy"), profileLibrary, 10, TBD.Profiles.ticOSG, TBD.Profiles.ticOLG);

            Assert.That(generations.Last().Factor(TBD.Profiles.ticOSG), Is.EqualTo(SensiblePerArea).Within(Tolerance));
            Assert.That(generations.Last().Factor(TBD.Profiles.ticOLG), Is.EqualTo(LatentPerArea).Within(Tolerance));
        }

        // =================================================================================================
        // 4. The other slots that share the helper
        // =================================================================================================

        private static InternalCondition PerArea_InternalCondition(InternalConditionParameter internalConditionParameter, double perArea, InternalConditionParameter profileNameParameter, string profileName)
        {
            InternalCondition result = new InternalCondition("Cell 1");
            result.SetValue(internalConditionParameter, perArea);
            result.SetValue(profileNameParameter, profileName);
            return result;
        }

        [TestCase(0.25)]
        [TestCase(0.5)]
        [TestCase(1.0)]
        public void LightingGainPerArea_ThreeGenerations_AreAFixedPoint(double peak)
        {
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Lighting", ProfileType.Lighting, Schedule(peak)) });
            InternalCondition internalCondition = PerArea_InternalCondition(InternalConditionParameter.LightingGainPerArea, 12.0, InternalConditionParameter.LightingProfileName, "Lighting");

            foreach (Generation generation in Chain(internalCondition, profileLibrary, 3, TBD.Profiles.ticLG))
            {
                Assert.That(generation.Factor(TBD.Profiles.ticLG), Is.EqualTo(12.0).Within(Tolerance));
                Assert.That(Value(generation.InternalCondition, InternalConditionParameter.LightingGainPerArea), Is.EqualTo(12.0).Within(Tolerance));
            }
        }

        [TestCase(0.25)]
        [TestCase(1.0)]
        public void EquipmentSensibleGainPerArea_ThreeGenerations_AreAFixedPoint(double peak)
        {
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Equipment", ProfileType.EquipmentSensible, Schedule(peak)) });
            InternalCondition internalCondition = PerArea_InternalCondition(InternalConditionParameter.EquipmentSensibleGainPerArea, 4.0, InternalConditionParameter.EquipmentSensibleProfileName, "Equipment");

            foreach (Generation generation in Chain(internalCondition, profileLibrary, 3, TBD.Profiles.ticESG))
            {
                Assert.That(generation.Factor(TBD.Profiles.ticESG), Is.EqualTo(4.0).Within(Tolerance));
                Assert.That(Value(generation.InternalCondition, InternalConditionParameter.EquipmentSensibleGainPerArea), Is.EqualTo(4.0).Within(Tolerance));
            }
        }

        [TestCase(0.25)]
        [TestCase(1.0)]
        public void EquipmentLatentGainPerArea_ThreeGenerations_AreAFixedPoint(double peak)
        {
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Equipment Latent", ProfileType.EquipmentLatent, Schedule(peak)) });
            InternalCondition internalCondition = PerArea_InternalCondition(InternalConditionParameter.EquipmentLatentGainPerArea, 1.5, InternalConditionParameter.EquipmentLatentProfileName, "Equipment Latent");

            foreach (Generation generation in Chain(internalCondition, profileLibrary, 3, TBD.Profiles.ticELG))
            {
                Assert.That(generation.Factor(TBD.Profiles.ticELG), Is.EqualTo(1.5).Within(Tolerance));
                Assert.That(Value(generation.InternalCondition, InternalConditionParameter.EquipmentLatentGainPerArea), Is.EqualTo(1.5).Within(Tolerance));
            }
        }

        [TestCase(0.25)]
        [TestCase(1.0)]
        public void PollutantGenerationPerArea_ThreeGenerations_AreAFixedPoint(double peak)
        {
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Pollutant", ProfileType.Pollutant, Schedule(peak)) });
            InternalCondition internalCondition = PerArea_InternalCondition(InternalConditionParameter.PollutantGenerationPerArea, 0.03, InternalConditionParameter.PollutantProfileName, "Pollutant");

            foreach (Generation generation in Chain(internalCondition, profileLibrary, 3, TBD.Profiles.ticCOG))
            {
                Assert.That(generation.Factor(TBD.Profiles.ticCOG), Is.EqualTo(0.03).Within(Tolerance));
                Assert.That(Value(generation.InternalCondition, InternalConditionParameter.PollutantGenerationPerArea), Is.EqualTo(0.03).Within(Tolerance));
            }
        }

        [TestCase(0.25)]
        [TestCase(1.0)]
        public void InfiltrationAirChangesPerHour_ThreeGenerations_AreAFixedPoint(double peak)
        {
            //Infiltration is not a gain, but it travels the same seam: the ACH is the factor and the
            //profile is when it applies. Its schedules are normally flat at 1.0, which is why it never
            //showed the defect in the field - it is covered here so it cannot start to.
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Infiltration", ProfileType.Infiltration, Schedule(peak)) });
            InternalCondition internalCondition = PerArea_InternalCondition(InternalConditionParameter.InfiltrationAirChangesPerHour, 0.15, InternalConditionParameter.InfiltrationProfileName, "Infiltration");

            foreach (Generation generation in Chain(internalCondition, profileLibrary, 3, TBD.Profiles.ticI))
            {
                Assert.That(generation.Factor(TBD.Profiles.ticI), Is.EqualTo(0.15).Within(Tolerance));
                Assert.That(Value(generation.InternalCondition, InternalConditionParameter.InfiltrationAirChangesPerHour), Is.EqualTo(0.15).Within(Tolerance));
            }
        }

        // =================================================================================================
        // 5. Mutation / reuse safety
        // =================================================================================================

        [Test]
        public void Roundtrip_DoesNotNormaliseOrMutateTheSharedProfileDefinition()
        {
            //The tempting "fix" is to divide the schedule by its peak and multiply the factor back. That
            //would rewrite a definition several internal conditions share (see Query.ProfileReuseIndex),
            //on behalf of one of them. The magnitude must be carried outside the shape instead.
            Profile profile = new Profile("Occupancy", ProfileType.Occupancy, Schedule(0.25));
            List<double> values_Before = new List<double>(profile.GetValues());
            System.Guid guid_Before = profile.Guid;

            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { profile });
            List<Generation> generations = Chain(Occupancy_InternalCondition("Occupancy"), profileLibrary, 3, TBD.Profiles.ticOSG, TBD.Profiles.ticOLG);

            Assert.That(profile.GetValues(), Is.EqualTo(values_Before).AsCollection.Within(Tolerance),
                "The authored profile definition was mutated by a round trip.");
            Assert.That(profile.Guid, Is.EqualTo(guid_Before), "The authored profile definition was replaced.");
            Assert.That(profile.GetValues().Max(), Is.EqualTo(0.25).Within(Tolerance),
                "The schedule peak must stay where the author put it.");

            foreach (Generation generation in generations)
            {
                Assert.That(generation.Profile(TBD.Profiles.ticOSG).Values(), Is.EqualTo(values_Before).AsCollection.Within(1E-6),
                    "The exported TBD schedule is not the authored shape.");
            }
        }

        [Test]
        public void Roundtrip_KeepsOneProfileDefinitionPerSlotPair_NoGuidProliferation()
        {
            //ticOSG and ticOLG share one occupancy definition. Each generation must keep collecting one
            //definition for the pair, not one per slot and not a new one per generation beyond the single
            //re-import the legacy (index-free) naming implies.
            ProfileLibrary profileLibrary = new ProfileLibrary("Authored", new List<Profile> { new Profile("Occupancy", ProfileType.Occupancy, Schedule(0.25)) });

            foreach (Generation generation in Chain(Occupancy_InternalCondition("Occupancy"), profileLibrary, 3, TBD.Profiles.ticOSG, TBD.Profiles.ticOLG))
            {
                List<Profile> profiles = generation.ProfileLibrary.GetProfiles();
                Assert.That(profiles.Count, Is.EqualTo(1),
                    "Expected exactly one occupancy definition collected per generation, got " + profiles.Count + ".");
                Assert.That(generation.Profile(TBD.Profiles.ticOSG).name, Is.EqualTo(generation.Profile(TBD.Profiles.ticOLG).name),
                    "The sensible and latent slots must keep pointing at the same shared shape.");
            }
        }
    }
}
