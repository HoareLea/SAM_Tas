// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The SAM airflow REQUIREMENT crossing the <c>SAM -> TBD -> SAM</c> seam, and staying separate from
    /// the choice of how TAS REALISES it.</b>
    /// <para>
    /// SAM lets an engineer state a supply-air requirement on four simultaneous bases, which
    /// <c>Query.CalculatedSupplyAirFlow</c> sums into one required total. TAS cannot hold that decomposition:
    /// it has <c>InternalGain.freshAirRate</c> (per person, Outside Air) and - only where a Ventilation profile
    /// deliberately selects Building Simulator mechanical ventilation - one air-change rate in
    /// <c>ticV.factor</c>. The export used to write the summed TOTAL into that single slot and the import read
    /// it back as the ACH BASIS, so the next export summed the other bases on top of a figure that already
    /// contained them: a licensed bedroom went 1.72 -> 2.44 -> 3.16 ACH and kept climbing.
    /// </para>
    /// <para>
    /// <see cref="SAMZoneMetadata"/> carries the authored decomposition in the TBD zone description, and
    /// <c>Modify.RestoreVentilationRequirement</c> puts it back on import in place of what was inferred from
    /// the total. This file covers both, plus the requirement/realisation boundary they have to respect.
    /// </para>
    /// <para>
    /// No TAS COM. The metadata is a string and the restore takes plain doubles, so the whole mechanism is
    /// exercisable without a licence - which is the point: this is the class of defect a licensed run found
    /// three generations late.
    /// </para>
    /// </summary>
    [TestFixture]
    public class VentilationRequirementMetadataTests
    {
        private const double Volume = 420.0;          // m3, the licensed Bedroom 2_3
        private const double Area = 105.0;            // m2
        private const double AreaPerPerson = 10.0;    // -> occupancy 10.5
        private const double AirFlowPerPerson = 0.008; // m3/s/p == freshAirRate 8 l/s/p
        private const double AirChangesPerHour = 1.72; // the ACH basis A0 stated

        //0.084 m3/s over 420 m3 == 0.72 ACH: the term that used to be added once per generation.
        private const double PerPersonAirChangesPerHour = 0.72;
        private const double RequiredAirChangesPerHour = AirChangesPerHour + PerPersonAirChangesPerHour; // 2.44

        // =================================================================================================
        // Helpers - the SAM side of a round trip, without COM
        // =================================================================================================

        private static Space Space_Licensed(bool ventilationProfile, bool perPerson = true, bool airChanges = true)
        {
            Space space = new Space("Bedroom 2_3");
            space.SetValue(SAM.Analytical.SpaceParameter.Volume, Volume);
            space.SetValue(SAM.Analytical.SpaceParameter.Area, Area);

            InternalCondition internalCondition = new InternalCondition("Bedroom 2_3");
            internalCondition.SetValue(InternalConditionParameter.AreaPerPerson, AreaPerPerson);

            if (airChanges)
            {
                internalCondition.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, AirChangesPerHour);
            }

            if (perPerson)
            {
                internalCondition.SetValue(InternalConditionParameter.SupplyAirFlowPerPerson, AirFlowPerPerson);
            }

            if (ventilationProfile)
            {
                internalCondition.SetValue(InternalConditionParameter.VentilationProfileName, "Bedroom 2_3 [VENT]");
            }

            space.InternalCondition = internalCondition;

            return space;
        }

        /// <summary>
        /// What <c>Create.ZoneMetadata</c> records for a space, mirrored here because building the real one
        /// needs a live TBD internal condition. The two native fields are what
        /// <c>Modify.UpdateInternalCondition</c> writes: <c>freshAirRate</c> always, the <c>ticV</c> factor
        /// ONLY when a Ventilation profile is assigned.
        /// </summary>
        private static SAMZoneMetadata Export(Space space, out double freshAirRate, out double ventilationFactor)
        {
            InternalCondition internalCondition = space.InternalCondition;
            bool ventilationProfile = internalCondition.TryGetValue(InternalConditionParameter.VentilationProfileName, out string name) && !string.IsNullOrWhiteSpace(name);

            freshAirRate = internalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlowPerPerson, out double airFlowPerPerson) && !double.IsNaN(airFlowPerPerson)
                ? (float)((float)airFlowPerPerson * 1000)
                : 0.0f;   // the TBD template default a SAM export leaves alone

            //The gate. Without a profile the factor is never written, so it keeps whatever the TBD template
            //carried - a value SAM did not author and must not later read back as a requirement.
            ventilationFactor = ventilationProfile ? (float)SAM.Analytical.Tas.Query.VentilationAirChangesPerHour(space) : 1.0;

            return new SAMZoneMetadata
            {
                SupplyAirFlow = Value(internalCondition, InternalConditionParameter.SupplyAirFlow),
                SupplyAirFlowPerArea = Value(internalCondition, InternalConditionParameter.SupplyAirFlowPerArea),
                SupplyAirFlowPerPerson = Value(internalCondition, InternalConditionParameter.SupplyAirFlowPerPerson),
                SupplyAirChangesPerHour = Value(internalCondition, InternalConditionParameter.SupplyAirChangesPerHour),
                VentilationProfileApplied = ventilationProfile,
                FreshAirRate = freshAirRate,
                VentilationFactor = ventilationProfile ? ventilationFactor : double.NaN,
            };
        }

        /// <summary>
        /// The NATIVE import, verbatim from <c>Convert.ToSAM(TBD.InternalCondition, …)</c>: the per-person rate
        /// from <c>freshAirRate</c>, the ACH basis from the <c>ticV</c> factor, and a ventilation profile
        /// reference written from the mere presence of a <c>ticV</c> slot - which every TBD internal condition
        /// has. That last one is why the restore has to be able to take it away again.
        /// </summary>
        private static Space Import_Native(double freshAirRate, double ventilationFactor)
        {
            Space space = new Space("Bedroom 2_3");
            space.SetValue(SAM.Analytical.SpaceParameter.Volume, Volume);
            space.SetValue(SAM.Analytical.SpaceParameter.Area, Area);

            InternalCondition internalCondition = new InternalCondition("Bedroom 2_3");
            //Recovered from personGain and the per-area occupancy gains by the real import; constant here.
            internalCondition.SetValue(InternalConditionParameter.AreaPerPerson, AreaPerPerson);
            internalCondition.SetValue(InternalConditionParameter.SupplyAirFlowPerPerson, freshAirRate / 1000.0);
            internalCondition.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, ventilationFactor);
            internalCondition.SetValue(InternalConditionParameter.VentilationProfileName, "Bedroom 2_3 [VENT]");

            space.InternalCondition = internalCondition;

            return space;
        }

        /// <summary>One whole generation: export the space, then import what the TBD now holds.</summary>
        private static Space Generation(Space space, out double ticVFactor, out string note)
        {
            SAMZoneMetadata metadata = Export(space, out double freshAirRate, out ticVFactor);

            string description = SAMZoneMetadata.Compose(null, null, null, metadata);

            Space space_Result = Import_Native(freshAirRate, ticVFactor);
            Restore(space_Result, SAMZoneMetadata.Parse(description), freshAirRate, ticVFactor, out note);

            return space_Result;
        }

        /// <summary>
        /// The restore, applied to a space. <c>Space.InternalCondition</c> hands out a COPY on every read and
        /// stores a copy on every write, so the condition has to be held and put back - the real import
        /// restores onto the list element BEFORE <c>Convert.ToSAM(TBD.Building, …)</c> assigns it to the space,
        /// which is the same thing by a different route.
        /// </summary>
        private static bool Restore(Space space, SAMZoneMetadata metadata, double freshAirRate, double ventilationFactor, out string note)
        {
            InternalCondition internalCondition = space.InternalCondition;

            bool result = internalCondition.RestoreVentilationRequirement(metadata, freshAirRate, ventilationFactor, out note);

            space.InternalCondition = internalCondition;

            return result;
        }

        private static double Value(InternalCondition internalCondition, InternalConditionParameter internalConditionParameter)
        {
            return internalCondition.TryGetValue(internalConditionParameter, out double result) ? result : double.NaN;
        }

        private static double Value(Space space, InternalConditionParameter internalConditionParameter)
        {
            return Value(space.InternalCondition, internalConditionParameter);
        }

        // =================================================================================================
        // 1. Requirement only - no Ventilation profile
        // =================================================================================================

        [Test]
        public void NoVentilationProfile_TheFourBasesSurvive_AndNoMechanicalVentilationIsActivated()
        {
            //The engineer has stated a requirement and has NOT chosen Building Simulator ventilation to deliver
            //it - the realisation may be an IZAM, Tas Systems, a TBD profile assigned later, or nothing yet.
            //The data must cross the seam; the choice must not be made on the engineer's behalf.
            Space space = Space_Licensed(ventilationProfile: false);

            //Held and put back: Space.InternalCondition hands out a copy.
            InternalCondition internalCondition = space.InternalCondition;
            internalCondition.SetValue(InternalConditionParameter.SupplyAirFlow, 0.01);
            internalCondition.SetValue(InternalConditionParameter.SupplyAirFlowPerArea, 0.0004);
            space.InternalCondition = internalCondition;

            Space space_Imported = Generation(space, out double ticVFactor, out string note);

            Assert.Multiple(() =>
            {
                Assert.That(note, Is.Null);

                Assert.That(Value(space_Imported, InternalConditionParameter.SupplyAirFlow), Is.EqualTo(0.01).Within(1e-12));
                Assert.That(Value(space_Imported, InternalConditionParameter.SupplyAirFlowPerArea), Is.EqualTo(0.0004).Within(1e-12));
                Assert.That(Value(space_Imported, InternalConditionParameter.SupplyAirFlowPerPerson), Is.EqualTo(AirFlowPerPerson).Within(1e-12));
                Assert.That(Value(space_Imported, InternalConditionParameter.SupplyAirChangesPerHour), Is.EqualTo(AirChangesPerHour).Within(1e-9),
                    "The authored ACH basis returns, NOT the ticV factor the TBD template happened to hold.");

                Assert.That(space_Imported.InternalCondition.TryGetValue(InternalConditionParameter.VentilationProfileName, out string _), Is.False,
                    "A ticV slot exists in every TBD internal condition. Letting the reference the native import writes from it stand would activate Building Simulator ventilation on the next export, from requirement data alone.");
            });
        }

        // =================================================================================================
        // 2. An explicit Ventilation profile - the realisation is chosen, and it delivers everything
        // =================================================================================================

        [Test]
        public void WithAVentilationProfile_TheTicVFactorIsTheWholeRequirement_AndThreeGenerationsAgree()
        {
            //THE DEFECT, and its absence. The licensed Bedroom 2_3: 1.72 ACH stated plus 8 l/s/p over 10.5
            //people (0.72 ACH), so the profile that has been chosen to realise the requirement must deliver
            //2.44 ACH - and must deliver 2.44 again next generation, and the one after.
            Space space_0 = Space_Licensed(ventilationProfile: true);

            Space space_1 = Generation(space_0, out double ticV_1, out string note_1);
            Space space_2 = Generation(space_1, out double ticV_2, out string note_2);
            Space space_3 = Generation(space_2, out double ticV_3, out string note_3);

            Assert.Multiple(() =>
            {
                Assert.That(ticV_1, Is.EqualTo(RequiredAirChangesPerHour).Within(1e-5),
                    "The realisation delivers the full requirement: the stated ACH plus the per-person term.");
                Assert.That(ticV_2, Is.EqualTo(ticV_1).Within(1e-5), "1.72 -> 2.44 -> 3.16 was the growth this fixes.");
                Assert.That(ticV_3, Is.EqualTo(ticV_1).Within(1e-5));

                Assert.That(note_1, Is.Null);
                Assert.That(note_2, Is.Null);
                Assert.That(note_3, Is.Null);

                //And the reason it holds: the AUTHORED basis comes back, not the total.
                foreach (Space space in new Space[] { space_1, space_2, space_3 })
                {
                    Assert.That(Value(space, InternalConditionParameter.SupplyAirChangesPerHour), Is.EqualTo(AirChangesPerHour).Within(1e-5));
                    Assert.That(Value(space, InternalConditionParameter.SupplyAirFlowPerPerson), Is.EqualTo(AirFlowPerPerson).Within(1e-9));
                }

                //freshAirRate keeps carrying the per-person rate for Part L / Tas Systems throughout.
                Export(space_3, out double freshAirRate, out double _);
                Assert.That(freshAirRate, Is.EqualTo(8.0).Within(1e-6));
            });
        }

        [Test]
        public void WithoutTheMetadata_TheSameChainCompounds()
        {
            //The control, and the reason the metadata has to exist. Same export, but the import is left with
            //only what TAS states - the total, on the single ACH basis - and the per-person term is added again
            //every generation.
            Space space = Space_Licensed(ventilationProfile: true);

            List<double> factors = new List<double>();
            for (int i = 0; i < 3; i++)
            {
                Export(space, out double freshAirRate, out double ticVFactor);
                factors.Add(ticVFactor);
                space = Import_Native(freshAirRate, ticVFactor);
            }

            Assert.That(factors[0], Is.EqualTo(2.44).Within(1e-5));
            Assert.That(factors[1], Is.EqualTo(3.16).Within(1e-5));
            Assert.That(factors[2], Is.EqualTo(3.88).Within(1e-5));
        }

        // =================================================================================================
        // 3. Per person only
        // =================================================================================================

        [Test]
        public void PerPersonOnly_WithAProfile_ReachesTheFactorAndStaysThere()
        {
            Space space_0 = Space_Licensed(ventilationProfile: true, airChanges: false);

            Space space_1 = Generation(space_0, out double ticV_1, out string _);
            Generation(space_1, out double ticV_2, out string _);

            Assert.Multiple(() =>
            {
                Assert.That(ticV_1, Is.EqualTo(PerPersonAirChangesPerHour).Within(1e-5),
                    "Per-person air is the whole requirement here, so it is the whole factor.");
                Assert.That(ticV_2, Is.EqualTo(ticV_1).Within(1e-5));

                Assert.That(space_1.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirChangesPerHour, out double _), Is.False,
                    "No ACH basis was authored, so none is restored - the factor must not become one.");
            });
        }

        [Test]
        public void PerPersonOnly_WithNoProfile_IsKeptAsRequirementAndActivatesNothing()
        {
            Space space_Imported = Generation(Space_Licensed(ventilationProfile: false, airChanges: false), out double _, out string _);

            Assert.Multiple(() =>
            {
                Assert.That(Value(space_Imported, InternalConditionParameter.SupplyAirFlowPerPerson), Is.EqualTo(AirFlowPerPerson).Within(1e-12));
                Assert.That(space_Imported.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirChangesPerHour, out double _), Is.False);
                Assert.That(space_Imported.InternalCondition.TryGetValue(InternalConditionParameter.VentilationProfileName, out string _), Is.False);
            });
        }

        // =================================================================================================
        // 4. A TAS-authored model, with no metadata at all
        // =================================================================================================

        [Test]
        public void NoMetadata_TheNativeImportStandsUntouched()
        {
            //Nothing about this mechanism may be required for a TBD that SAM never wrote. Without a section
            //there is nothing to restore and nothing to refuse - and no decomposition is invented from a total
            //that TAS states as a single rate.
            Space space = Import_Native(8.0, 1.72);

            bool restored = Restore(space, SAMZoneMetadata.Parse("Any TAS note at all"), 8.0, 1.72, out string note);

            Assert.Multiple(() =>
            {
                Assert.That(SAMZoneMetadata.Parse("Any TAS note at all"), Is.Null);
                Assert.That(SAMZoneMetadata.Parse(null), Is.Null);
                Assert.That(SAMZoneMetadata.Parse("[Id]=1234; [LevelName]=Level 01"), Is.Null);

                Assert.That(restored, Is.False);
                Assert.That(note, Is.Null, "Absent is not stale: a TAS-authored file must produce no diagnostic.");

                Assert.That(Value(space, InternalConditionParameter.SupplyAirChangesPerHour), Is.EqualTo(1.72).Within(1e-12),
                    "A genuine TAS ticV factor still becomes the SAM ACH basis.");
                Assert.That(Value(space, InternalConditionParameter.SupplyAirFlowPerPerson), Is.EqualTo(0.008).Within(1e-12),
                    "freshAirRate still becomes SupplyAirFlowPerPerson.");
            });
        }

        [Test]
        public void AMalformedOrFutureSection_FallsBackRatherThanGuessing()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SAMZoneMetadata.Parse("[SAM_META_V1]={not json"), Is.Null);
                Assert.That(SAMZoneMetadata.Parse("[SAM_META_V1]={\"native\":{}}"), Is.Null, "No ventilation section is not an empty requirement.");
                Assert.That(SAMZoneMetadata.Parse("[SAM_META_V2]={\"ventilation\":{}}"), Is.Null, "A version this build does not know must be left to the native import.");
            });
        }

        // =================================================================================================
        // 5. The zone description is shared - everything else in it survives
        // =================================================================================================

        [Test]
        public void ComposePreservesIdLevelNameAndAnythingElseInTheDescription()
        {
            SAMZoneMetadata metadata = Export(Space_Licensed(ventilationProfile: true), out double _, out double _);

            //A description as a previous export left it, plus a note a TAS user typed in themselves.
            string description_Existing = "[Id]=OLD; [LevelName]=OLD; Checked by DM 2026-08-20; [SAM_META_V1]={\"ventilation\":{},\"native\":{}}";

            string description = SAMZoneMetadata.Compose(description_Existing, "1234", "Level 01", metadata);

            Assert.Multiple(() =>
            {
                Assert.That(description, Does.StartWith("[Id]=1234; [LevelName]=Level 01; "));
                Assert.That(description, Does.Contain("Checked by DM 2026-08-20"),
                    "Content this class does not own is preserved verbatim - the previous unconditional overwrite discarded it.");

                //The managed segments are REPLACED, not appended to.
                Assert.That(description, Does.Not.Contain("=OLD"));
                Assert.That(CountOf(description, SAMZoneMetadata.Marker), Is.EqualTo(1));

                //And the section still parses out of the composed string.
                SAMZoneMetadata metadata_Parsed = SAMZoneMetadata.Parse(description);
                Assert.That(metadata_Parsed, Is.Not.Null);
                Assert.That(metadata_Parsed.SupplyAirChangesPerHour, Is.EqualTo(AirChangesPerHour).Within(1e-12));
            });
        }

        [Test]
        public void ComposeKeepsTheExistingIdAndLevelName_WhereTheSpaceNoLongerStatesThem()
        {
            //The import reads neither back onto the space, so a model that has been through FromTBD carries
            //neither - and the second generation would silently drop a level name the first one wrote. They
            //used to survive only by accident, because a space stating neither left the description untouched.
            SAMZoneMetadata metadata = Export(Space_Licensed(ventilationProfile: true), out double _, out double _);

            string description = SAMZoneMetadata.Compose("[Id]=1234; [LevelName]=Level 01", null, null, metadata);

            Assert.Multiple(() =>
            {
                Assert.That(description, Does.StartWith("[Id]=1234; [LevelName]=Level 01; "));
                Assert.That(CountOf(description, "[Id]="), Is.EqualTo(1));
                Assert.That(CountOf(description, "[LevelName]="), Is.EqualTo(1));

                //A space that DOES state one still wins over what is there.
                Assert.That(SAMZoneMetadata.Compose(description, "9999", null, metadata), Does.StartWith("[Id]=9999; [LevelName]=Level 01; "));
            });
        }

        [Test]
        public void TheSectionRoundTripsDeterministically_AndInInvariantCulture()
        {
            SAMZoneMetadata metadata = Export(Space_Licensed(ventilationProfile: true), out double _, out double _);
            metadata.SupplyAirFlowPerArea = 0.00035;

            string json = metadata.ToJson();

            Assert.Multiple(() =>
            {
                //Byte-identical on repeat, so a re-export of an unchanged model produces an unchanged file.
                Assert.That(metadata.ToJson(), Is.EqualTo(json));
                Assert.That(SAMZoneMetadata.Parse(SAMZoneMetadata.Marker + json).ToJson(), Is.EqualTo(json));

                //Invariant-culture numbers: a decimal point, never a comma, whatever the machine's locale.
                Assert.That(json, Does.Contain("0.00035"));
                Assert.That(json, Does.Not.Contain("0,00035"));

                //Absent bases are omitted, not written as a zero that would later read as an authored value.
                Assert.That(json, Does.Not.Contain("\"flow\""));

                //Volume and area are the TBD zone's own; a second copy here could only go stale.
                Assert.That(json, Does.Not.Contain("volume").IgnoreCase);
                Assert.That(json, Does.Not.Contain("\"area\""));
            });
        }

        [Test]
        public void ComposeUnderAFrenchCulture_StillWritesADecimalPoint()
        {
            System.Globalization.CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");

                SAMZoneMetadata metadata = Export(Space_Licensed(ventilationProfile: true), out double _, out double _);

                Assert.That(metadata.ToJson(), Does.Contain("1.72"));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = cultureInfo;
            }
        }

        // =================================================================================================
        // 6. The file was edited in TAS after SAM wrote it
        // =================================================================================================

        [Test]
        public void AChangedFreshAirRate_RefusesTheSection_AndSaysSo()
        {
            SAMZoneMetadata metadata = Export(Space_Licensed(ventilationProfile: true), out double freshAirRate, out double ticVFactor);

            //Someone opened the TBD and changed the outside air to 12 l/s/p. The recorded decomposition is no
            //longer known to describe this zone, so it must not be presented as if it were current.
            Space space = Import_Native(12.0, ticVFactor);

            bool restored = Restore(space, metadata, 12.0, ticVFactor, out string note);

            Assert.Multiple(() =>
            {
                Assert.That(restored, Is.False);
                Assert.That(note, Does.Contain("freshAirRate").And.Contains("12").And.Contains("8"));

                Assert.That(Value(space, InternalConditionParameter.SupplyAirFlowPerPerson), Is.EqualTo(0.012).Within(1e-12),
                    "Fall back to what TAS states, in full - not a mixture of the two.");
                Assert.That(Value(space, InternalConditionParameter.SupplyAirChangesPerHour), Is.EqualTo(ticVFactor).Within(1e-5));

                //Sanity: the same section applied to the file SAM actually wrote is accepted.
                Assert.That(Restore(Import_Native(freshAirRate, ticVFactor), metadata, freshAirRate, ticVFactor, out string _), Is.True);
            });
        }

        [Test]
        public void AChangedTicVFactor_RefusesTheSection_OnlyWhereSAMAuthoredThatFactor()
        {
            SAMZoneMetadata metadata_Applied = Export(Space_Licensed(ventilationProfile: true), out double freshAirRate, out double ticVFactor);

            bool restored_Applied = Restore(Import_Native(freshAirRate, 9.9), metadata_Applied, freshAirRate, 9.9, out string note_Applied);

            //No profile: SAM never wrote that factor, so a value there is TAS's own and is not evidence of
            //anything having gone stale. Refusing on it would throw away good requirement data.
            SAMZoneMetadata metadata_NotApplied = Export(Space_Licensed(ventilationProfile: false), out double freshAirRate_NotApplied, out double _);

            Space space_NotApplied = Import_Native(freshAirRate_NotApplied, 9.9);
            bool restored_NotApplied = Restore(space_NotApplied, metadata_NotApplied, freshAirRate_NotApplied, 9.9, out string note_NotApplied);

            Assert.Multiple(() =>
            {
                Assert.That(restored_Applied, Is.False);
                Assert.That(note_Applied, Does.Contain("ticV.factor"));

                Assert.That(restored_NotApplied, Is.True);
                Assert.That(note_NotApplied, Is.Null);
                Assert.That(Value(space_NotApplied, InternalConditionParameter.SupplyAirChangesPerHour), Is.EqualTo(AirChangesPerHour).Within(1e-9));
            });
        }

        // =================================================================================================
        // The restore removes as well as writes
        // =================================================================================================

        [Test]
        public void ABasisTheExportDidNotRecord_IsRemovedAndNotLeftAsInferred()
        {
            //The single most important line of the restore. The native import has just written an ACH basis
            //from the ticV factor; if a section that records no ACH basis only ever WROTE, that inferred value
            //would survive and the feedback loop would be back.
            Space space = Import_Native(8.0, 2.44);

            SAMZoneMetadata metadata = new SAMZoneMetadata
            {
                SupplyAirFlowPerPerson = AirFlowPerPerson,
                VentilationProfileApplied = true,
                FreshAirRate = 8.0,
                VentilationFactor = 2.44,
            };

            Assert.That(Restore(space, metadata, 8.0, 2.44, out string _), Is.True);

            Assert.That(space.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirChangesPerHour, out double _), Is.False,
                "No ACH basis was authored, so the one inferred from the total must go.");
        }

        private static int CountOf(string text, string value)
        {
            int result = 0;
            for (int index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + 1, StringComparison.Ordinal))
            {
                result++;
            }

            return result;
        }
    }
}
