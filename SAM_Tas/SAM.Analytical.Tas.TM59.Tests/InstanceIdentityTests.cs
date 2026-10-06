// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using ApertureTypeDefinition = SAM.Analytical.Tas.ApertureTypeDefinition;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>Stage 3 - the two COM-free decisions that let a follow-up update resolve physical identity
    /// without decoding a GUID out of a shared, definition-derived element name.</b>
    /// <para>
    /// <see cref="TasQuery.Match(Core.Tas.ZoneSurfaceReference, IEnumerable{Aperture}, out AperturePart)"/> -
    /// the ZoneGuid fix: a bare surface NUMBER is not unique across a building (TAS numbers per zone), so
    /// two zones' same-numbered surfaces must not be confused once a reference states which zone it means.
    /// </para>
    /// <para>
    /// <see cref="TasQuery.ApertureMatchesExistingAssignment(Aperture, AperturePart, uint, IEnumerable{ApertureTypeDefinition}, IEnumerable{string}, FeatureShade)"/> -
    /// the split/rebind decision: whether a member aperture's own required colour, opening control and
    /// feature shade are still exactly what a shared building element already carries, or whether it has
    /// drifted and must be split onto its own element instead. Both are pure functions of SAM-side,
    /// COM-free state - the <c>Aperture</c>/<c>Core.Tas.ZoneSurfaceReference</c> value objects and the
    /// plain values a caller would have already read off the real TBD element - so the decision is
    /// testable with no TAS install.
    /// </para>
    /// </summary>
    [TestFixture]
    public class InstanceIdentityTests
    {
        private static readonly string[] DayTypes = { "Weekday", "Saturday", "Sunday" };

        private const string GlazingName = "SIM_EXT_GLZ";

        // =================================================================================================
        // Builders
        // =================================================================================================

        private static ApertureConstruction Glazing()
        {
            return new ApertureConstruction(
                System.Guid.NewGuid(),
                GlazingName,
                ApertureType.Window,
                new List<ConstructionLayer> { new ConstructionLayer("Glass 6mm", 0.006) },
                new List<ConstructionLayer> { new ConstructionLayer("Timber", 0.05) });
        }

        private static Aperture Window(IOpeningProperties openingProperties = null, System.Drawing.Color? color = null, double offset = 0, FeatureShade featureShade = null)
        {
            Polygon3D polygon3D = new Polygon3D(new List<Point3D>
            {
                new Point3D(offset, 0, 0),
                new Point3D(offset + 1, 0, 0),
                new Point3D(offset + 1, 0, 1),
                new Point3D(offset, 0, 1)
            });

            Aperture aperture = new Aperture(Glazing(), polygon3D);

            if (openingProperties != null)
            {
                aperture.SetValue(SAM.Analytical.ApertureParameter.OpeningProperties, openingProperties);
            }

            if (color.HasValue)
            {
                aperture.SetValue(SAM.Analytical.ApertureParameter.Color, color.Value);
            }

            if (featureShade != null)
            {
                aperture.SetValue(SAM.Analytical.ApertureParameter.FeatureShade, featureShade);
            }

            return aperture;
        }

        private static FeatureShade Shade(double overhangDepth = 0.5)
        {
            return new FeatureShade("shade", null, 1.0, 2.0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, overhangDepth, 0.0, 0.5);
        }

        private static BuildingElementDefinition PaneDefinition()
        {
            // Naming needs no resolved construction (its signature renders "unresolved") - the tests below
            // are about NAME allocation, not reuse, so a bare definition is enough.
            return new BuildingElementDefinition(ApertureType.Window, AperturePart.Pane, 12, 0x0088CCu, null, null);
        }

        private static PartOOpeningProperties PartO(double dischargeCoefficient = 1.2)
        {
            return new PartOOpeningProperties(dischargeCoefficient, 1.0, 30.0, OpeningRestriction.Unrestricted);
        }

        private static List<ApertureTypeDefinition> Assignments(Aperture aperture, AperturePart aperturePart)
        {
            List<ApertureTypeAssignment> assignments = TasQuery.ApertureTypeAssignments(aperture, aperturePart, DayTypes);
            List<ApertureTypeDefinition> result = new List<ApertureTypeDefinition>();
            foreach (ApertureTypeAssignment assignment in assignments)
            {
                result.Add(assignment.ApertureTypeDefinition);
            }
            return result;
        }

        private static uint Colour(Aperture aperture, AperturePart aperturePart)
        {
            return Core.Convert.ToUint(TasQuery.Color(aperture, aperturePart).Value);
        }

        // =================================================================================================
        // ApertureMatchesExistingAssignment - the split/rebind decision
        // =================================================================================================

        [Test]
        public void MatchesExisting_IdenticalColourAndOpening_ReturnsTrue()
        {
            Aperture aperture = Window(PartO(1.2), System.Drawing.Color.Red);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), Assignments(aperture, AperturePart.Pane), DayTypes);

            Assert.That(result, Is.True, "an aperture stating exactly what the element already carries must match - this is the zero-writes case");
        }

        [Test]
        public void MatchesExisting_DifferentColour_ReturnsFalse()
        {
            Aperture aperture = Window(PartO(1.2), System.Drawing.Color.Red);
            uint colour_Existing = Colour(Window(PartO(1.2), System.Drawing.Color.Blue), AperturePart.Pane);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, colour_Existing, Assignments(aperture, AperturePart.Pane), DayTypes);

            Assert.That(result, Is.False, "the aperture's own colour changed since the element was bound - it must split, not stay silently mismatched");
        }

        [Test]
        public void MatchesExisting_DifferentDischargeCoefficient_ReturnsFalse()
        {
            Aperture aperture = Window(PartO(1.2), System.Drawing.Color.Red);
            List<ApertureTypeDefinition> assignments_Existing = Assignments(Window(PartO(0.6), System.Drawing.Color.Red), AperturePart.Pane);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), assignments_Existing, DayTypes);

            Assert.That(result, Is.False, "a different opening control (Cd changed) is a different definition - the aperture no longer matches what the element carries");
        }

        [Test]
        public void MatchesExisting_ExistingCarriesNoOpening_RequiredCarriesOne_ReturnsFalse()
        {
            Aperture aperture = Window(PartO(1.2), System.Drawing.Color.Red);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), new List<ApertureTypeDefinition>(), DayTypes);

            Assert.That(result, Is.False, "an aperture that now asks for an opening control the element does not carry must split - count alone already disqualifies it");
        }

        [Test]
        public void MatchesExisting_NeitherCarriesAnOpening_ReturnsTrue()
        {
            Aperture aperture = Window(null, System.Drawing.Color.Red);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), new List<ApertureTypeDefinition>(), DayTypes);

            Assert.That(result, Is.True, "a sealed window matches a sealed (opening-free) element - an empty list is a definition in its own right, not a gap");
        }

        [Test]
        public void MatchesExisting_Frame_IgnoresOpeningAndMatchesOnColourAlone()
        {
            // A frame never carries opening assignments - only a pane's write reaches SetApertureTypes -
            // so an opening-stating aperture's FRAME side must still match a colour-only element.
            Aperture aperture = Window(PartO(1.2), System.Drawing.Color.Red);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Frame, Colour(aperture, AperturePart.Frame), new List<ApertureTypeDefinition>(), DayTypes);

            Assert.That(result, Is.True, "a frame is judged on colour alone; its aperture's own OpeningProperties never enters the comparison");
        }

        [Test]
        public void MatchesExisting_Frame_ExistingCarriesAssignments_ReturnsFalse()
        {
            // Defensive: a FRAMEELEMENT should never carry assignments in practice, but if one somehow does,
            // the comparison must not silently ignore that inconsistency.
            Aperture aperture = Window(null, System.Drawing.Color.Red);
            List<ApertureTypeDefinition> assignments_Existing = Assignments(Window(PartO(1.2), System.Drawing.Color.Red), AperturePart.Pane);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Frame, Colour(aperture, AperturePart.Frame), assignments_Existing, DayTypes);

            Assert.That(result, Is.False, "a frame's required assignment list is always empty - it cannot match an element that carries any");
        }

        // =================================================================================================
        // ApertureMatchesExistingAssignment - the feature-shade half of the split decision
        //
        // A stamped pane that adds, removes or changes ONLY its FeatureShade still matches on colour and
        // openings, so without a shade comparison it would stay bound and never reach SetFeatureShades.
        // =================================================================================================

        [Test]
        public void MatchesExisting_SameFeatureShade_ReturnsTrue()
        {
            Aperture aperture = Window(null, System.Drawing.Color.Red, featureShade: Shade(0.5));

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), new List<ApertureTypeDefinition>(), DayTypes, Shade(0.5));

            Assert.That(result, Is.True, "an aperture whose stated shade is what the element already carries must match - the zero-writes case");
        }

        [Test]
        public void MatchesExisting_ShadeAdded_ReturnsFalse()
        {
            Aperture aperture = Window(null, System.Drawing.Color.Red, featureShade: Shade(0.5));

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), new List<ApertureTypeDefinition>(), DayTypes, null);

            Assert.That(result, Is.False, "the aperture gained a shade the element does not carry - it must split, not keep the shade-less element");
        }

        [Test]
        public void MatchesExisting_ShadeRemoved_ReturnsFalse()
        {
            Aperture aperture = Window(null, System.Drawing.Color.Red);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), new List<ApertureTypeDefinition>(), DayTypes, Shade(0.5));

            Assert.That(result, Is.False, "the aperture dropped its shade but the element still carries one - it must split, not keep the stale shade");
        }

        [Test]
        public void MatchesExisting_ShadeChanged_ReturnsFalse()
        {
            Aperture aperture = Window(null, System.Drawing.Color.Red, featureShade: Shade(0.5));

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), new List<ApertureTypeDefinition>(), DayTypes, Shade(0.9));

            Assert.That(result, Is.False, "a different overhang depth is a different shade - the TBD must not retain the previous one");
        }

        [Test]
        public void MatchesExisting_ShadeRoundTrippedThroughFloat_ReturnsTrue()
        {
            // TBD stores the shade fields as Single, so a shade read back off the element comes back at
            // float precision. That round trip is NOT a divergence, or every update would re-split every
            // shaded pane forever.
            Aperture aperture = Window(null, System.Drawing.Color.Red, featureShade: Shade(0.1));
            FeatureShade featureShade_Existing = Shade((double)(float)0.1);

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Pane, Colour(aperture, AperturePart.Pane), new List<ApertureTypeDefinition>(), DayTypes, featureShade_Existing);

            Assert.That(result, Is.True, "float read-back precision is what a TBD round trip produces - it must not be treated as a change");
        }

        [Test]
        public void MatchesExisting_Frame_IgnoresFeatureShade()
        {
            // A frame's write never reaches SetFeatureShades, so its split decision is colour alone even
            // when the aperture states a shade.
            Aperture aperture = Window(null, System.Drawing.Color.Red, featureShade: Shade(0.5));

            bool result = TasQuery.ApertureMatchesExistingAssignment(aperture, AperturePart.Frame, Colour(aperture, AperturePart.Frame), new List<ApertureTypeDefinition>(), DayTypes, null);

            Assert.That(result, Is.True, "a frame is judged on colour alone; the aperture's shade never enters its comparison");
        }

        // =================================================================================================
        // FeatureShadesMatch - the content comparison itself
        // =================================================================================================

        [Test]
        public void FeatureShadesMatch_NullCases()
        {
            Assert.That(TasQuery.FeatureShadesMatch(null, null), Is.True, "neither side stating a shade is not a divergence");
            Assert.That(TasQuery.FeatureShadesMatch(Shade(), null), Is.False);
            Assert.That(TasQuery.FeatureShadesMatch(null, Shade()), Is.False);
        }

        [Test]
        public void FeatureShadesMatch_NameAndDescriptionAreNotCompared()
        {
            FeatureShade featureShade_1 = Shade();
            FeatureShade featureShade_2 = new FeatureShade("a TAS-assigned name", "some description", 1.0, 2.0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, 0.5, 0.0, 0.5);

            Assert.That(TasQuery.FeatureShadesMatch(featureShade_1, featureShade_2), Is.True, "the export creates the TBD shade with a null name, so TAS assigns one - text must not decide equality");
        }

        [Test]
        public void FeatureShadesMatch_NaNFieldHandling()
        {
            FeatureShade featureShade_NaN = new FeatureShade("a", null, double.NaN, 2.0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, 0.5, 0.0, 0.5);

            Assert.That(TasQuery.FeatureShadesMatch(Shade(), featureShade_NaN), Is.False, "a field written on one side and absent on the other is a real difference");
            Assert.That(TasQuery.FeatureShadesMatch(featureShade_NaN, featureShade_NaN), Is.True, "NaN on both sides is a field neither side states, not a difference");
        }

        // =================================================================================================
        // ShadedBuildingElementName - collision-safe naming for shade splits
        //
        // The plain two-name budget (preferred + signature-qualified) excludes the shade from the
        // signature, so every shade split of one definition would derive the same two names. A null name
        // leaves the pane bound to the element whose shade no longer matches - these elements are never
        // reused, so the shade-aware naming must always find one.
        // =================================================================================================

        [Test]
        public void ShadedName_FirstSplit_KeepsTheConventionName()
        {
            string name = TasQuery.ShadedBuildingElementName(new List<string>(), PaneDefinition(), GlazingName, Shade());

            Assert.That(name, Is.EqualTo("Windows: " + GlazingName + " -pane"), "one shade variant in the building needs no discriminator");
        }

        [Test]
        public void ShadedName_TwoShadeSplitsOfOneDefinition_GetDistinctNames()
        {
            BuildingElementDefinition definition = PaneDefinition();
            string preferred = "Windows: " + GlazingName + " -pane";

            string name_1 = TasQuery.ShadedBuildingElementName(new List<string> { preferred }, definition, GlazingName, Shade(0.5));
            string name_2 = TasQuery.ShadedBuildingElementName(new List<string> { preferred, name_1 }, definition, GlazingName, Shade(0.9));

            Assert.That(name_1, Is.Not.Null);
            Assert.That(name_2, Is.Not.Null, "the second shade split must not come back nameless - that path leaves the pane bound to the wrong-shaded element");
            Assert.That(name_2, Is.Not.EqualTo(name_1), "the shade differs, so the shade-content discriminator differs");
            Assert.That(TasQuery.TryDecomposeBuildingElementName(name_2, out _, out _, out AperturePart aperturePart), Is.True, "the convention shape must survive the discriminator");
            Assert.That(aperturePart, Is.EqualTo(AperturePart.Pane));
        }

        [Test]
        public void ShadedName_ReSplitAfterAnotherShadeChange_DoesNotCollideWithItsOwnPreviousElement()
        {
            // The pane was split once (shade 0.5, element created under the signature-qualified name), then
            // the shade changed to 0.9: the re-split must find a fresh name even though its own previous
            // element still occupies the qualified one.
            BuildingElementDefinition definition = PaneDefinition();
            string preferred = "Windows: " + GlazingName + " -pane";
            string qualified = string.Format("Windows: {0}_{1} -pane", GlazingName, TasQuery.BuildingElementSignatureHash(definition));

            string name = TasQuery.ShadedBuildingElementName(new List<string> { preferred, qualified }, definition, GlazingName, Shade(0.9));

            Assert.That(name, Is.Not.Null, "without the shade-aware fallback this exact sequence returned null and stranded the pane on the stale shade");
            Assert.That(name, Is.Not.EqualTo(qualified));
        }

        [Test]
        public void ShadedName_IdenticalShadeSplitTwice_CounterKeepsNamesUnique()
        {
            // Two identical apertures stating the identical shade (or a shade changed back to a value a
            // now-unused element still carries): the shade-content discriminator is the SAME, so the
            // counter is what keeps the second element's name unique. The elements never share.
            BuildingElementDefinition definition = PaneDefinition();
            string preferred = "Windows: " + GlazingName + " -pane";
            string qualified = string.Format("Windows: {0}_{1} -pane", GlazingName, TasQuery.BuildingElementSignatureHash(definition));

            string name_1 = TasQuery.ShadedBuildingElementName(new List<string> { preferred, qualified }, definition, GlazingName, Shade(0.5));
            string name_2 = TasQuery.ShadedBuildingElementName(new List<string> { preferred, qualified, name_1 }, definition, GlazingName, Shade(0.5));

            Assert.That(name_1, Is.Not.Null);
            Assert.That(name_2, Is.Not.Null);
            Assert.That(name_2, Is.Not.EqualTo(name_1));
            Assert.That(TasQuery.TryDecomposeBuildingElementName(name_2, out _, out _, out AperturePart aperturePart), Is.True);
            Assert.That(aperturePart, Is.EqualTo(AperturePart.Pane));
        }

        // =================================================================================================
        // ZoneSurfaceReferencesMatch - the ZoneGuid fix
        //
        // Tests the pure comparison directly rather than through the full, heavily-overloaded Match(...)
        // family, which is also overloaded with TBD/TAS3D COM parameter types this COM-free test project
        // deliberately never references (see the reference block at the top of this project's .csproj).
        // =================================================================================================

        [Test]
        public void ZoneSurfaceReferencesMatch_SameNumberSameZoneGuid_ReturnsTrue()
        {
            bool result = TasQuery.ZoneSurfaceReferencesMatch(new Core.Tas.ZoneSurfaceReference(5, "ZONE-A"), new Core.Tas.ZoneSurfaceReference(5, "ZONE-A"));

            Assert.That(result, Is.True);
        }

        [Test]
        public void ZoneSurfaceReferencesMatch_SameNumberDifferentZoneGuid_ReturnsFalse()
        {
            // The bug this fixes: TAS numbers surfaces PER ZONE, so zone A's surface 5 and zone B's
            // surface 5 are different physical surfaces that merely share a number.
            bool result = TasQuery.ZoneSurfaceReferencesMatch(new Core.Tas.ZoneSurfaceReference(5, "ZONE-A"), new Core.Tas.ZoneSurfaceReference(5, "ZONE-B"));

            Assert.That(result, Is.False, "same surface number in two different, GUID-stated zones must not be treated as the same surface");
        }

        [Test]
        public void ZoneSurfaceReferencesMatch_DifferentNumber_ReturnsFalseRegardlessOfZoneGuid()
        {
            bool result = TasQuery.ZoneSurfaceReferencesMatch(new Core.Tas.ZoneSurfaceReference(5, "ZONE-A"), new Core.Tas.ZoneSurfaceReference(6, "ZONE-A"));

            Assert.That(result, Is.False);
        }

        [Test]
        public void ZoneSurfaceReferencesMatch_TargetHasNoZoneGuid_FallsBackToSurfaceNumberOnly()
        {
            // A reference that never states a ZoneGuid (an older stamp, or a caller that never set one)
            // must still match exactly as it did before this fix - a strict tightening, not a new refusal.
            bool result = TasQuery.ZoneSurfaceReferencesMatch(new Core.Tas.ZoneSurfaceReference(5, "ZONE-A"), new Core.Tas.ZoneSurfaceReference(5, null));

            Assert.That(result, Is.True, "a side with no ZoneGuid falls back to SurfaceNumber alone, so pre-fix behaviour is preserved for stamps that never carried one");
        }

        [Test]
        public void ZoneSurfaceReferencesMatch_BothHaveNoZoneGuid_FallsBackToSurfaceNumberOnly()
        {
            bool result = TasQuery.ZoneSurfaceReferencesMatch(new Core.Tas.ZoneSurfaceReference(5, null), new Core.Tas.ZoneSurfaceReference(5, null));

            Assert.That(result, Is.True);
        }

        [Test]
        public void ZoneSurfaceReferencesMatch_NullReference_ReturnsFalse()
        {
            Assert.That(TasQuery.ZoneSurfaceReferencesMatch(null, new Core.Tas.ZoneSurfaceReference(5, "ZONE-A")), Is.False);
            Assert.That(TasQuery.ZoneSurfaceReferencesMatch(new Core.Tas.ZoneSurfaceReference(5, "ZONE-A"), null), Is.False);
        }
    }
}
