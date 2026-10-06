// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using System;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Guards the MAGNITUDE half of the ventilation round trip - the unit contract between what the TBD
    /// import stores for a collected <c>ticV</c> profile and what the export multiplies back out.
    /// <para>
    /// These tests exist because a licensed round trip turned a 2.0 ACH source profile into 40.8 ACH. The
    /// shape and type came back correctly; the rate did not. The import was storing a ticV extreme - an
    /// AIR CHANGE RATE - into <see cref="InternalConditionParameter.SupplyAirFlow"/>, which
    /// <c>Query.CalculatedSupplyAirFlow</c> reads as m3/s, and the export's own "/ volume * 3600" then
    /// inflated it by 3600/volume. The defect was dormant for as long as the imported ventilation
    /// reference dangled, and went live the moment ticV became a collected, resolvable profile.
    /// </para>
    /// <para>
    /// Everything here is COM-free: a Space plus an InternalCondition is enough to exercise the whole
    /// SAM-side airflow calculation, and the export's conversion is one documented line of arithmetic
    /// (<c>Modify.UpdateInternalCondition</c>, ticV slot) mirrored in <see cref="TicVFactor"/>. That is
    /// what makes this reproducible without a licence - and what would have caught the failure before
    /// TAS was ever run.
    /// </para>
    /// </summary>
    [TestFixture]
    public class VentilationAirflowMagnitudeTests
    {
        private const double Volume = 200.0;   // m3
        private const double Area = 50.0;      // m2
        private const double SourceRate = 2.0; // ACH, the peak of the source ticV profile

        // The export's ticV conversion, verbatim from Modify.UpdateInternalCondition: the SAM design
        // airflow [m3/s] becomes the TBD ticV factor [ACH] over the space volume.
        private static double TicVFactor(Space space)
        {
            double airFlow = SAM.Analytical.Query.CalculatedSupplyAirFlow(space);
            if (double.IsNaN(airFlow))
            {
                return 1.0; // the export's own fallback when nothing is specified
            }

            space.TryGetValue(SAM.Analytical.SpaceParameter.Volume, out double volume);
            return airFlow / volume * 3600.0;
        }

        // The unit lives in the ParameterProperties attribute's description (SAM.Core.Query.Description
        // reads DescriptionAttribute, which these parameters do not carry), so read it directly.
        private static string DeclaredUnit(InternalConditionParameter internalConditionParameter)
        {
            System.Reflection.FieldInfo fieldInfo = typeof(InternalConditionParameter).GetField(internalConditionParameter.ToString());
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(SAM.Core.Attributes.ParameterProperties), false);

            Assert.That(attributes, Is.Not.Empty, "Expected a ParameterProperties attribute on " + internalConditionParameter);

            return ((SAM.Core.Attributes.ParameterProperties)attributes[0]).Description;
        }

        // The template path's parameter preference, verbatim from Modify.UpdateInternalConditionTemplate.
        private static InternalConditionParameter TemplateTicVParameter(InternalCondition internalCondition)
        {
            if (internalCondition.TryGetValue(InternalConditionParameter.SupplyAirChangesPerHour, out double airChangesPerHour) && !double.IsNaN(airChangesPerHour))
            {
                return InternalConditionParameter.SupplyAirChangesPerHour;
            }

            return InternalConditionParameter.SupplyAirFlow;
        }

        private static Space Space_WithCondition(Action<InternalCondition> configure)
        {
            Space space = new Space("Cell 1");
            space.SetValue(SAM.Analytical.SpaceParameter.Volume, Volume);
            space.SetValue(SAM.Analytical.SpaceParameter.Area, Area);

            InternalCondition internalCondition = new InternalCondition("Cell 1");
            configure(internalCondition);
            space.InternalCondition = internalCondition;

            return space;
        }

        // =================================================================================================
        // The unit contract the fix rests on
        // =================================================================================================

        [Test]
        public void Units_SupplyAirFlowIsVolumeFlow_AndSupplyAirChangesPerHourIsARate()
        {
            //The whole defect is a unit confusion between these two parameters, so pin their declared
            //units. If either description ever changes, the reasoning in these tests stops holding.
            Assert.That(DeclaredUnit(InternalConditionParameter.SupplyAirFlow), Does.Contain("m3/s"),
                "SupplyAirFlow is a volume flow; a ticV extreme is not one.");
            Assert.That(DeclaredUnit(InternalConditionParameter.SupplyAirChangesPerHour), Does.Contain("ACH"),
                "SupplyAirChangesPerHour is the air-change-rate basis a ticV extreme belongs in.");
        }

        // =================================================================================================
        // The round trip
        // =================================================================================================

        [Test]
        public void ImportedTicVPeak_StoredAsAirChangesPerHour_ExportsBackAsTheSameRate()
        {
            //What the corrected import writes for a source ticV whose peak factor*value is 2.0 ACH.
            Space space = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, SourceRate));

            //The ACH basis converts to m3/s as rate * volume / 3600 ...
            Assert.That(SAM.Analytical.Query.CalculatedSupplyAirFlow(space), Is.EqualTo(SourceRate * Volume / 3600.0).Within(1e-12));

            //... and the export's "/ volume * 3600" is its exact inverse, so the rate returns unchanged.
            Assert.That(TicVFactor(space), Is.EqualTo(SourceRate).Within(1e-9),
                "A ticV rate carried on the ACH basis must round-trip unchanged, whatever the volume.");
        }

        [TestCase(50.0)]
        [TestCase(200.0)]
        [TestCase(1234.5)]
        public void ImportedTicVPeak_OnTheAirChangesBasis_IsVolumeIndependent(double volume)
        {
            //The inverse-pair property must not depend on the zone: this is what makes the ACH basis the
            //right home for a rate, and it is exactly what the m3/s basis fails below.
            Space space = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, SourceRate));
            space.SetValue(SAM.Analytical.SpaceParameter.Volume, volume);

            Assert.That(TicVFactor(space), Is.EqualTo(SourceRate).Within(1e-9));
        }

        // =================================================================================================
        // The regression itself
        // =================================================================================================

        [Test]
        public void ImportedTicVPeak_StoredAsSupplyAirFlow_IsInflatedByTheVolumeRatio()
        {
            //The pre-fix import state: the same 2.0 ACH stored as though it were 2.0 m3/s.
            Space space = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirFlow, SourceRate));

            //Read as a volume flow it passes straight through, and the export then scales it by 3600/volume.
            Assert.That(SAM.Analytical.Query.CalculatedSupplyAirFlow(space), Is.EqualTo(SourceRate).Within(1e-12));

            double factor = TicVFactor(space);
            Assert.That(factor, Is.EqualTo(36.0).Within(1e-9), "2.0 'm3/s' over 200 m3 is 36 ACH.");
            Assert.That(factor, Is.EqualTo(SourceRate * 3600.0 / Volume).Within(1e-9),
                "The error is exactly the 3600/volume factor - the signature of the unit mismatch.");

            //And the headline assertion: the two storage choices are NOT interchangeable.
            Assert.That(factor, Is.Not.EqualTo(SourceRate).Within(1e-6),
                "SupplyAirFlow = 2.0 must never be treated as equivalent to SupplyAirChangesPerHour = 2.0.");
        }

        [Test]
        public void TheTwoStorageChoices_DisagreeByTheVolumeRatio_NotByRounding()
        {
            Space space_ACH = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, SourceRate));
            Space space_Flow = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirFlow, SourceRate));

            Assert.That(TicVFactor(space_Flow) / TicVFactor(space_ACH), Is.EqualTo(3600.0 / Volume).Within(1e-9),
                "18x on a 200 m3 zone - the licensed run measured this as 2.0 ACH becoming 40.8 with the "
                + "per-person basis included.");
        }

        // =================================================================================================
        // The stored basis is the source FACTOR, not the schedule peak
        // =================================================================================================

        // What Modify.Update writes back out of a space: factor = the ACH basis, values = the imported
        // profile's own values, verbatim. So the ventilation the simulation sees is basis * max(values).
        private static double ExportedPeak(Space space, double[] profileValues)
        {
            double max = profileValues[0];
            foreach (double value in profileValues)
            {
                if (value > max)
                {
                    max = value;
                }
            }

            return TicVFactor(space) * max;
        }

        [TestCase(2.0, 1.0)]     // the usual TAS convention: values normalised to a peak of 1
        [TestCase(2.0, 0.5)]     // NOT normalised - this is the case that exposed the double scaling
        [TestCase(0.35, 0.8)]
        [TestCase(1.0, 0.25)]
        public void ImportedFactor_RoundTripsThePeak_ForAnyProfileNormalisation(double factor_Source, double peak_Source)
        {
            //GetExtremeValue(true) is factor * max(values), and Modify.Update re-applies the same
            //values on top of whatever basis it is given - so storing the PEAK scales the schedule twice
            //(factor * max^2) and only looks right when max == 1. Storing the FACTOR is exact for any shape.
            double[] values = new double[24];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = i < 8 ? peak_Source * 0.25 : peak_Source;
            }

            double peak_Effective = factor_Source * peak_Source;

            //What the corrected import stores: the source factor.
            Space space = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, factor_Source));
            Assert.That(ExportedPeak(space, values), Is.EqualTo(peak_Effective).Within(1e-9),
                "The exported ventilation peak must equal the source's factor * max(values).");

            //What storing the extreme would have done instead.
            Space space_Peak = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, peak_Effective));
            Assert.That(ExportedPeak(space_Peak, values), Is.EqualTo(peak_Effective * peak_Source).Within(1e-9),
                "Storing the extreme re-applies max(values) a second time.");
        }

        [Test]
        public void StoringTheExtreme_IsIndistinguishableFromTheFactor_OnlyWhenTheProfileIsNormalised()
        {
            //Why the first authored licensed oracle passed while this defect was live: its 0.25/1.0/0.5 shape
            //peaks at exactly 1.0, where factor and extreme coincide. Pinned so the blind spot is on record.
            double[] normalised = new double[] { 0.25, 1.0, 0.5 };
            double[] notNormalised = new double[] { 0.125, 0.5, 0.25 };

            Space space = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, 2.0));

            Assert.That(ExportedPeak(space, normalised), Is.EqualTo(2.0).Within(1e-9));
            Assert.That(ExportedPeak(space, notNormalised), Is.EqualTo(1.0).Within(1e-9),
                "A profile peaking at 0.5 carries half the factor as its ventilation rate - which is exactly "
                + "the source's own factor * max, so the round trip is still faithful.");
        }

        // =================================================================================================
        // Precedence: how the bases combine
        // =================================================================================================

        [Test]
        public void CalculatedSupplyAirFlow_SumsEveryBasis_ItDoesNotSelectOne()
        {
            //Pinned because the licensed 40.8 ACH was the ACH-misread-as-m3/s term PLUS a per-person term,
            //and reading the combination rule wrongly would misattribute the error. There is no precedence
            //and no max(): every specified basis is added.
            Space space = Space_WithCondition(x =>
            {
                x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, SourceRate);   // 0.111111 m3/s
                x.SetValue(InternalConditionParameter.SupplyAirFlowPerPerson, 0.008);         // per person
                x.SetValue(InternalConditionParameter.AreaPerPerson, 7.5);                   // -> 6.667 people
                x.SetValue(InternalConditionParameter.SupplyAirFlowPerArea, 0.0004);          // per m2
                x.SetValue(InternalConditionParameter.SupplyAirFlow, 0.01);                  // absolute
            });

            double expected = SourceRate * Volume / 3600.0     // ACH basis
                + 0.008 * (Area / 7.5)                          // per-person basis
                + 0.0004 * Area                                 // per-area basis
                + 0.01;                                         // absolute basis

            Assert.That(SAM.Analytical.Query.CalculatedSupplyAirFlow(space), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void CalculatedSupplyAirFlow_WithNoBasisSpecified_IsNaN_SoTheExportFallsBackToFactorOne()
        {
            //The untouched-ticV case: nothing specified, so the export writes its factor-1 fallback rather
            //than a computed rate. This is why a model with no ventilation intent is left alone.
            Space space = Space_WithCondition(x => { });

            Assert.That(SAM.Analytical.Query.CalculatedSupplyAirFlow(space), Is.NaN);
            Assert.That(TicVFactor(space), Is.EqualTo(1.0));
        }

        // =================================================================================================
        // Template / unused internal conditions
        // =================================================================================================

        [Test]
        public void TemplateCondition_HasNoVolume_SoItsTicVFactorIsTheAirChangesValueItself()
        {
            //Modify.UpdateInternalConditionTemplate has no space to convert through, so whichever parameter
            //it reads becomes the ticV factor verbatim. An imported condition now carries only the ACH
            //basis, so a template path still reading SupplyAirFlow would find nothing and silently write a
            //zero ventilation factor - which is why that path prefers SupplyAirChangesPerHour.
            InternalCondition internalCondition = new InternalCondition("Template");
            internalCondition.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, SourceRate);

            Assert.That(TemplateTicVParameter(internalCondition), Is.EqualTo(InternalConditionParameter.SupplyAirChangesPerHour));
            Assert.That(internalCondition.TryGetValue(TemplateTicVParameter(internalCondition), out double value), Is.True);
            Assert.That(value, Is.EqualTo(SourceRate).Within(1e-12),
                "The template factor is this value verbatim, in the same ACH unit the space path yields.");

            Assert.That(internalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlow, out double _), Is.False,
                "The corrected import writes only the ACH basis.");
        }

        [Test]
        public void TemplateCondition_CarryingOnlyTheLegacyFlowParameter_StillUsesIt()
        {
            //A SAM-authored template that predates the ACH basis must keep the factor it has always been
            //given, so the preference falls back rather than writing a zero.
            InternalCondition internalCondition = new InternalCondition("Legacy template");
            internalCondition.SetValue(InternalConditionParameter.SupplyAirFlow, 0.05);

            Assert.That(TemplateTicVParameter(internalCondition), Is.EqualTo(InternalConditionParameter.SupplyAirFlow),
                "Native SAM-authored template ventilation must not be silently dropped by the correction.");
        }

        // =================================================================================================
        // Every basis reaches the ticV factor - including per person
        // =================================================================================================

        //Occupant fresh air on the fixture: 8 l/s/p over 5 people is 0.04 m3/s, which on 200 m3 is exactly
        //0.72 ACH. Exact binary values, so the assertions are equalities.
        private const double AirFlowPerPerson = 0.008; // m3/s/p, i.e. TBD freshAirRate 8 l/s/p
        private const double AreaPerPerson = 10.0;     // m2/p -> occupancy 5.0 on the 50 m2 fixture
        private const double PerPersonAirChangesPerHour = 0.72;

        private static Space Space_WithAirChangesAndOccupants()
        {
            return Space_WithCondition(x =>
            {
                x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, SourceRate);
                x.SetValue(InternalConditionParameter.SupplyAirFlowPerPerson, AirFlowPerPerson);
                x.SetValue(InternalConditionParameter.AreaPerPerson, AreaPerPerson);
            });
        }

        [Test]
        public void TicVFactor_IsTheWholeRequirement_PerPersonIncluded()
        {
            //A ticV factor is only ever written where a SAM Ventilation profile has CHOSEN Building Simulator
            //mechanical ventilation as the realisation (Modify.UpdateInternalCondition gates on it). Having
            //chosen it, that profile has to deliver the whole requirement: leaving a basis out would
            //under-ventilate the zone in the simulation by exactly that term.
            Space space = Space_WithAirChangesAndOccupants();

            double occupancy = Area / AreaPerPerson;
            double perPerson_m3s = AirFlowPerPerson * occupancy;

            Assert.Multiple(() =>
            {
                //Sanity on the fixture arithmetic the rest of the file leans on.
                Assert.That(perPerson_m3s / Volume * 3600.0, Is.EqualTo(PerPersonAirChangesPerHour).Within(1e-12));

                Assert.That(SAM.Analytical.Query.CalculatedSupplyAirFlow(space),
                    Is.EqualTo(SourceRate * Volume / 3600.0 + perPerson_m3s).Within(1e-12),
                    "CalculatedSupplyAirFlow sums all four bases; that contract is untouched.");

                Assert.That(SAM.Analytical.Tas.Query.VentilationAirChangesPerHour(space),
                    Is.EqualTo(SourceRate + PerPersonAirChangesPerHour).Within(1e-9),
                    "The factor states the full calculated requirement, not a part of it.");

                //It IS the plain conversion of the summed total - nothing is subtracted on the way.
                Assert.That(SAM.Analytical.Tas.Query.VentilationAirChangesPerHour(space),
                    Is.EqualTo(TicVFactor(space)).Within(1e-12));
            });
        }

        [Test]
        public void TicVFactor_AndFreshAirRate_HoldTheSameRateWithoutDoubleCounting()
        {
            //internalGain.freshAirRate carries the per-person rate too - but as TAS's OUTSIDE AIR field, which
            //feeds Part L / EPC and Tas Systems and does not itself supply the zone in the TSD simulation.
            //Both holding it is therefore not a Building Simulator double count, and the ticV factor must not
            //be reduced on the assumption that it is. Pinned because an earlier revision of this fix did
            //exactly that, and changed the physical total to manufacture a fixed point.
            Space space = Space_WithAirChangesAndOccupants();

            space.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlowPerPerson, out double airFlowPerPerson);

            Assert.Multiple(() =>
            {
                //What Modify.UpdateInternalCondition writes to freshAirRate, verbatim: m3/s/p -> l/s/p.
                Assert.That(airFlowPerPerson * 1000, Is.EqualTo(8.0).Within(1e-9));

                Assert.That(SAM.Analytical.Tas.Query.VentilationAirChangesPerHour(space) - SourceRate,
                    Is.EqualTo(PerPersonAirChangesPerHour).Within(1e-9),
                    "The per-person term is present in the factor, in full.");
            });
        }

        [Test]
        public void TicVFactor_WithNoOccupants_IsJustTheOtherBases()
        {
            //Nothing is added where there is no per-person rate to add. On the licensed fixture these are the
            //corridor, bathroom and ensuites, which state no AreaPerPerson and held a stable 1.00 ACH in every
            //generation both before and after this work.
            Space space = Space_WithCondition(x => x.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, SourceRate));

            Assert.That(SAM.Analytical.Tas.Query.VentilationAirChangesPerHour(space), Is.EqualTo(TicVFactor(space)).Within(1e-12));
            Assert.That(SAM.Analytical.Tas.Query.VentilationAirChangesPerHour(space), Is.EqualTo(SourceRate).Within(1e-9));
        }

        [Test]
        public void TicVFactor_KeepsTheAreaAndFlatBases_WhichHaveNoOtherHomeInTBD()
        {
            //The bases with no native TAS field at all. Dropping either would lose that ventilation outright.
            Space space = Space_WithCondition(x =>
            {
                x.SetValue(InternalConditionParameter.SupplyAirFlow, 0.01);
                x.SetValue(InternalConditionParameter.SupplyAirFlowPerArea, 0.0004);
            });

            double expected_m3s = 0.01 + 0.0004 * Area;

            Assert.That(SAM.Analytical.Tas.Query.VentilationAirChangesPerHour(space),
                Is.EqualTo(expected_m3s / Volume * 3600.0).Within(1e-9));
        }

        [Test]
        public void TicVFactor_StatingNoSupplyAirAtAll_IsRefusedRatherThanZero()
        {
            //NaN, so Modify.UpdateInternalCondition's own "then use 1" fallback still fires - the correction
            //must not turn "nothing stated" into "no ventilation".
            Space space = Space_WithCondition(x => x.SetValue(InternalConditionParameter.AreaPerPerson, AreaPerPerson));

            Assert.That(SAM.Analytical.Tas.Query.VentilationAirChangesPerHour(space), Is.NaN);
        }
    }
}
