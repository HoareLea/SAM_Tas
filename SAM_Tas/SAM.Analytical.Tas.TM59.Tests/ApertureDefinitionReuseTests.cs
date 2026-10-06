// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using ApertureTypeAssignment = SAM.Analytical.Tas.ApertureTypeAssignment;
using ApertureTypeDefinition = SAM.Analytical.Tas.ApertureTypeDefinition;
using ApertureTypeProfileMode = SAM.Analytical.Tas.ApertureTypeProfileMode;
using BuildingElementDefinition = SAM.Analytical.Tas.BuildingElementDefinition;
using BuildingElementSeed = SAM.Analytical.Tas.BuildingElementSeed;
using ConstructionDefinition = SAM.Analytical.Tas.ConstructionDefinition;
using ConstructionLayerDefinition = SAM.Analytical.Tas.ConstructionLayerDefinition;
using ConstructionMaterialDefinition = SAM.Analytical.Tas.ConstructionMaterialDefinition;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>The SAM -&gt; TBD aperture-definition seam: what makes two aperture constructions and two aperture
    /// building elements the same thing, the deterministic naming that follows, and the guarantee that
    /// sharing one writes nothing.</b>
    /// <para>
    /// A <c>TBD.Construction</c> and an aperture's <c>TBD.buildingElement</c> are both building-level REUSABLE
    /// DEFINITIONS - the same relationship a <c>TBD.ApertureType</c> has, one level up, and the same
    /// discipline: identity is the DEFINITION and never the name. Two hundred identical windows keep two
    /// hundred pane surfaces and two hundred frame surfaces, because those are the physical windows; what
    /// they point at is two constructions and two elements.
    /// </para>
    /// <para>
    /// <b>The unsafe behaviour these replace</b> was a by-NAME lookup of both objects. That was harmless only
    /// because the name carried the aperture's own GUID and so matched nothing but itself; with names derived
    /// from the reusable SAM <c>ApertureConstruction</c>, a by-name lookup would hand one window another
    /// window's glazing. <see cref="Construction_SameNameDifferentContent_IsADifferentDefinition"/> and
    /// <see cref="Shared_MismatchedExistingConstruction_IsNeverWrittenTo"/> pin that.
    /// </para>
    /// <para>
    /// <b>Why these tests need no installed TAS.</b> Everything that DECIDES is COM-free:
    /// <c>ConstructionDefinition</c>, <c>BuildingElementDefinition</c>, their signatures, the two naming
    /// functions, the COM-free factories over a SAM <c>ApertureConstruction</c> and <c>Aperture</c>, and the
    /// two seed gates, which take a value read out of COM rather than a COM object. What genuinely needs COM
    /// is the write itself, and that is modelled by hand-written fakes that RECORD EVERY PROPERTY SET, in the
    /// same style as <see cref="ApertureTypeReuseTests"/>. The write log is what makes "sharing touches
    /// nothing" a test rather than a claim.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ApertureDefinitionReuseTests
    {
        private static readonly string[] DayTypes = { "Weekday", "Saturday", "Sunday" };

        private const string GlazingName = "SIM_EXT_GLZ";

        // =================================================================================================
        // Builders - SAM-side inputs
        // =================================================================================================

        private static MaterialLibrary Library()
        {
            MaterialLibrary materialLibrary = new MaterialLibrary("Test");

            materialLibrary.Add(new TransparentMaterial("Glass 6mm", "Glass", "Glass 6mm", "Clear float", 1.0, 750, 2500));
            materialLibrary.Add(new TransparentMaterial("Glass 4mm", "Glass", "Glass 4mm", "Clear float", 1.0, 750, 2500));
            materialLibrary.Add(new GasMaterial("Air 16mm", "Gas", "Air 16mm", "Cavity", 0.025, 1000, 1.2, 0.0000181));
            materialLibrary.Add(new OpaqueMaterial("Timber", "Frame", "Timber", "Softwood", 0.13, 1600, 500));
            materialLibrary.Add(new OpaqueMaterial("Aluminium", "Frame", "Aluminium", "Metal", 160, 880, 2700));

            return materialLibrary;
        }

        private static ApertureConstruction Glazing(
            string name = GlazingName,
            ApertureType apertureType = ApertureType.Window,
            IEnumerable<ConstructionLayer> paneConstructionLayers = null,
            IEnumerable<ConstructionLayer> frameConstructionLayers = null)
        {
            return new ApertureConstruction(
                System.Guid.NewGuid(),
                name,
                apertureType,
                paneConstructionLayers ?? new List<ConstructionLayer>
                {
                    new ConstructionLayer("Glass 6mm", 0.006),
                    new ConstructionLayer("Air 16mm", 0.016),
                    new ConstructionLayer("Glass 6mm", 0.006)
                },
                frameConstructionLayers ?? new List<ConstructionLayer> { new ConstructionLayer("Timber", 0.05) });
        }

        private static Aperture Window(ApertureConstruction apertureConstruction = null, IOpeningProperties openingProperties = null, System.Drawing.Color? color = null, double offset = 0)
        {
            //A distinct, real polygon per aperture - the point of the exercise is that 200 DIFFERENT physical
            //apertures resolve to ONE definition, so they must not be the same object.
            Polygon3D polygon3D = new Polygon3D(new List<Point3D>
            {
                new Point3D(offset, 0, 0),
                new Point3D(offset + 1, 0, 0),
                new Point3D(offset + 1, 0, 1),
                new Point3D(offset, 0, 1)
            });

            Aperture aperture = new Aperture(apertureConstruction ?? Glazing(), polygon3D);

            if (openingProperties != null)
            {
                aperture.SetValue(SAM.Analytical.ApertureParameter.OpeningProperties, openingProperties);
            }

            if (color != null && color.HasValue)
            {
                aperture.SetValue(SAM.Analytical.ApertureParameter.Color, color.Value);
            }

            return aperture;
        }

        private static PartOOpeningProperties PartO(OpeningRestriction openingRestriction = OpeningRestriction.Unrestricted, double dischargeCoefficient = 1.2)
        {
            return new PartOOpeningProperties(dischargeCoefficient, 1.0, 30.0, openingRestriction);
        }

        // =================================================================================================
        // Builders - definitions
        // =================================================================================================

        private static ConstructionMaterialDefinition MaterialDefinition(
            string name = "Glass 6mm",
            int type = 3,
            string description = null,
            float conductivity = 1f,
            float specificHeat = 0f,
            float density = 0f,
            float vapourDiffusionFactor = 0f,
            float externalSolarReflectance = 0f,
            float internalSolarReflectance = 0f,
            float externalLightReflectance = 0f,
            float internalLightReflectance = 0f,
            float externalEmissivity = 0f,
            float internalEmissivity = 0f,
            float solarTransmittance = 0f,
            float lightTransmittance = 0f,
            float dynamicViscosity = 0f,
            float convectionCoefficient = 0f,
            int isBlind = 0)
        {
            return new ConstructionMaterialDefinition(
                name, type, description, conductivity, specificHeat, density, vapourDiffusionFactor,
                externalSolarReflectance, internalSolarReflectance, externalLightReflectance, internalLightReflectance,
                externalEmissivity, internalEmissivity, solarTransmittance, lightTransmittance,
                dynamicViscosity, convectionCoefficient, isBlind);
        }

        private static ConstructionLayerDefinition Layer(ConstructionMaterialDefinition material = null, float width = 0.006f)
        {
            return new ConstructionLayerDefinition(material ?? MaterialDefinition(), width, width);
        }

        private static ConstructionDefinition Construction(
            AperturePart aperturePart = AperturePart.Pane,
            int type = 2,
            float additionalHeatTransfer = 0f,
            string description = null,
            IEnumerable<ConstructionLayerDefinition> layers = null)
        {
            return new ConstructionDefinition(aperturePart, type, additionalHeatTransfer, description, layers ?? new List<ConstructionLayerDefinition> { Layer() });
        }

        private static ApertureTypeDefinition ApertureControl(float dischargeCoefficient = 1.2f, float factor = 1f, string description = null)
        {
            return new ApertureTypeDefinition(dischargeCoefficient, factor, ApertureTypeProfileMode.Plain, null, null, description, DayTypes);
        }

        private static BuildingElementDefinition Element(
            ApertureType apertureType = ApertureType.Window,
            AperturePart aperturePart = AperturePart.Pane,
            int bEType = 12,
            uint colour = 0x0088CCu,
            ConstructionDefinition constructionDefinition = null,
            IEnumerable<ApertureTypeAssignment> apertureTypes = null)
        {
            return new BuildingElementDefinition(apertureType, aperturePart, bEType, colour, constructionDefinition ?? Construction(), apertureTypes);
        }

        private static BuildingElementSeed Seed(
            string name = "Windows: " + GlazingName + " -pane",
            int ghost = 0,
            string description = null,
            bool featureShade = false,
            bool substituteElement = false,
            int ground = 0,
            int markDelete = 0,
            float width = 0f,
            int bEType = 12,
            uint colour = 0x0088CCu,
            ConstructionDefinition constructionDefinition = null,
            IEnumerable<KeyValuePair<string, ApertureTypeDefinition>> apertureTypeAssignments = null)
        {
            return new BuildingElementSeed(
                name, ghost, description, featureShade, substituteElement, ground, markDelete, width, bEType, colour,
                constructionDefinition ?? Construction(), apertureTypeAssignments);
        }

        // =================================================================================================
        // Construction identity - each field flips it independently, the name never does
        // =================================================================================================

        [Test]
        public void Construction_TwoIdenticalDefinitions_AreEqual()
        {
            Assert.That(Construction(), Is.EqualTo(Construction()));
            Assert.That(Construction().GetHashCode(), Is.EqualTo(Construction().GetHashCode()));
        }

        [Test]
        public void Construction_PaneAndFrame_AreNeverEqualEvenWithIdenticalLayers()
        {
            ConstructionDefinition pane = Construction(AperturePart.Pane);
            ConstructionDefinition frame = Construction(AperturePart.Frame);

            Assert.That(pane, Is.Not.EqualTo(frame), "the aperture import reads a window's two halves back from the -pane/-frame pair, so merging them would lose one side");
        }

        [Test]
        public void Construction_DifferentType_IsADifferentDefinition()
        {
            Assert.That(Construction(type: 2), Is.Not.EqualTo(Construction(type: 1)));
        }

        [Test]
        public void Construction_DifferentAdditionalHeatTransfer_IsADifferentDefinition()
        {
            Assert.That(Construction(additionalHeatTransfer: 0f), Is.Not.EqualTo(Construction(additionalHeatTransfer: 0.5f)));
        }

        [Test]
        public void Construction_DifferentDescription_IsADifferentDefinition()
        {
            Assert.That(Construction(description: null), Is.Not.EqualTo(Construction(description: "Seeded by another tool")));
            Assert.That(Construction(description: "A"), Is.Not.EqualTo(Construction(description: "B")));
        }

        [Test]
        public void Construction_EmptyAndAbsentDescription_AreTheSameDefinition()
        {
            Assert.That(Construction(description: null), Is.EqualTo(Construction(description: "   ")), "a construction with no description reads back as an empty string");
        }

        [Test]
        public void Construction_DifferentLayerCount_IsADifferentDefinition()
        {
            ConstructionDefinition one = Construction(layers: new List<ConstructionLayerDefinition> { Layer() });
            ConstructionDefinition two = Construction(layers: new List<ConstructionLayerDefinition> { Layer(), Layer() });

            Assert.That(one, Is.Not.EqualTo(two));
        }

        [Test]
        public void Construction_LayerOrder_IsSignificant()
        {
            ConstructionLayerDefinition glass = Layer(MaterialDefinition(name: "Glass 6mm"), 0.006f);
            ConstructionLayerDefinition air = Layer(MaterialDefinition(name: "Air 16mm", type: 4), 0.016f);

            ConstructionDefinition glassFirst = Construction(layers: new List<ConstructionLayerDefinition> { glass, air });
            ConstructionDefinition airFirst = Construction(layers: new List<ConstructionLayerDefinition> { air, glass });

            Assert.That(glassFirst, Is.Not.EqualTo(airFirst), "a construction IS its layer sequence");
        }

        [Test]
        public void Construction_DifferentLayerWidth_IsADifferentDefinition()
        {
            ConstructionDefinition six = Construction(layers: new List<ConstructionLayerDefinition> { Layer(width: 0.006f) });
            ConstructionDefinition four = Construction(layers: new List<ConstructionLayerDefinition> { Layer(width: 0.004f) });

            Assert.That(six, Is.Not.EqualTo(four));
        }

        [Test]
        public void Construction_MaterialWidthDisagreeingWithLayerWidth_IsADifferentDefinition()
        {
            ConstructionDefinition agreeing = Construction(layers: new List<ConstructionLayerDefinition> { new ConstructionLayerDefinition(MaterialDefinition(), 0.006f, 0.006f) });
            ConstructionDefinition disagreeing = Construction(layers: new List<ConstructionLayerDefinition> { new ConstructionLayerDefinition(MaterialDefinition(), 0.006f, 0.004f) });

            Assert.That(agreeing, Is.Not.EqualTo(disagreeing), "TBD keeps a layer thickness in two places and the simulation reads both");
        }

        [Test]
        public void Construction_DifferentMaterialName_IsADifferentDefinition()
        {
            ConstructionDefinition a = Construction(layers: new List<ConstructionLayerDefinition> { Layer(MaterialDefinition(name: "Glass 6mm")) });
            ConstructionDefinition b = Construction(layers: new List<ConstructionLayerDefinition> { Layer(MaterialDefinition(name: "Glass 4mm")) });

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [TestCase("type")]
        [TestCase("description")]
        [TestCase("conductivity")]
        [TestCase("specificHeat")]
        [TestCase("density")]
        [TestCase("vapourDiffusionFactor")]
        [TestCase("externalSolarReflectance")]
        [TestCase("internalSolarReflectance")]
        [TestCase("externalLightReflectance")]
        [TestCase("internalLightReflectance")]
        [TestCase("externalEmissivity")]
        [TestCase("internalEmissivity")]
        [TestCase("solarTransmittance")]
        [TestCase("lightTransmittance")]
        [TestCase("dynamicViscosity")]
        [TestCase("convectionCoefficient")]
        [TestCase("isBlind")]
        public void Construction_AnyMaterialContentField_FlipsTheDefinition(string field)
        {
            ConstructionMaterialDefinition baseline = MaterialDefinition();
            ConstructionMaterialDefinition changed = MaterialDefinition(
                type: field == "type" ? 4 : 3,
                description: field == "description" ? "Changed" : null,
                conductivity: field == "conductivity" ? 2f : 1f,
                specificHeat: field == "specificHeat" ? 1f : 0f,
                density: field == "density" ? 1f : 0f,
                vapourDiffusionFactor: field == "vapourDiffusionFactor" ? 1f : 0f,
                externalSolarReflectance: field == "externalSolarReflectance" ? 1f : 0f,
                internalSolarReflectance: field == "internalSolarReflectance" ? 1f : 0f,
                externalLightReflectance: field == "externalLightReflectance" ? 1f : 0f,
                internalLightReflectance: field == "internalLightReflectance" ? 1f : 0f,
                externalEmissivity: field == "externalEmissivity" ? 1f : 0f,
                internalEmissivity: field == "internalEmissivity" ? 1f : 0f,
                solarTransmittance: field == "solarTransmittance" ? 1f : 0f,
                lightTransmittance: field == "lightTransmittance" ? 1f : 0f,
                dynamicViscosity: field == "dynamicViscosity" ? 1f : 0f,
                convectionCoefficient: field == "convectionCoefficient" ? 1f : 0f,
                isBlind: field == "isBlind" ? 1 : 0);

            Assert.That(changed, Is.Not.EqualTo(baseline), field + " is simulation content, so it must split the definition");

            ConstructionDefinition constructionDefinition_Baseline = Construction(layers: new List<ConstructionLayerDefinition> { Layer(baseline) });
            ConstructionDefinition constructionDefinition_Changed = Construction(layers: new List<ConstructionLayerDefinition> { Layer(changed) });

            Assert.That(constructionDefinition_Changed, Is.Not.EqualTo(constructionDefinition_Baseline));
        }

        [Test]
        public void Construction_MaterialContentNaN_StillComparesEqualToItselfAndHashesAlike()
        {
            //A material that states no conductivity stores NaN. Under `==` that layer would never equal
            //itself, so every window carrying it would get its own construction.
            ConstructionMaterialDefinition a = MaterialDefinition(conductivity: float.NaN);
            ConstructionMaterialDefinition b = MaterialDefinition(conductivity: float.NaN);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(Construction(layers: new List<ConstructionLayerDefinition> { Layer(a) }), Is.EqualTo(Construction(layers: new List<ConstructionLayerDefinition> { Layer(b) })));
        }

        [Test]
        public void Construction_SignedZero_IsNormalisedSoEqualsAndGetHashCodeAgree()
        {
            ConstructionMaterialDefinition positive = MaterialDefinition(conductivity: 0f);
            ConstructionMaterialDefinition negative = MaterialDefinition(conductivity: -0f);

            Assert.That(positive, Is.EqualTo(negative));
            Assert.That(positive.GetHashCode(), Is.EqualTo(negative.GetHashCode()), "equal definitions must hash alike, and the signature hashes bit patterns");
        }

        [Test]
        public void Construction_AnUnresolvedLayer_IsNeverEqualToAnything()
        {
            ConstructionDefinition unproven = Construction(layers: new List<ConstructionLayerDefinition> { new ConstructionLayerDefinition(null, 0.006f, 0.006f) });

            Assert.That(unproven.Proven, Is.False);
            Assert.That(unproven, Is.Not.EqualTo(Construction()));
            Assert.That(unproven, Is.Not.EqualTo(Construction(layers: new List<ConstructionLayerDefinition> { new ConstructionLayerDefinition(null, 0.006f, 0.006f) })), "unknown content is never proven equal, not even to itself by value");
        }

        [Test]
        public void Construction_AnUndefinedPart_IsNeverEqualToAnything()
        {
            ConstructionDefinition undefined = Construction(AperturePart.Undefined);

            Assert.That(undefined.Proven, Is.False);
            Assert.That(undefined, Is.Not.EqualTo(Construction(AperturePart.Undefined)));
        }

        [Test]
        public void Construction_Signature_IsStableAndDiscriminating()
        {
            Assert.That(TasQuery.ConstructionSignature(Construction()), Is.EqualTo(TasQuery.ConstructionSignature(Construction())));
            Assert.That(TasQuery.ConstructionSignature(Construction(AperturePart.Pane)), Is.Not.EqualTo(TasQuery.ConstructionSignature(Construction(AperturePart.Frame))));

            //Two widths that round to the same display text must not share a collision identity.
            ConstructionDefinition a = Construction(layers: new List<ConstructionLayerDefinition> { Layer(width: 0.0060001f) });
            ConstructionDefinition b = Construction(layers: new List<ConstructionLayerDefinition> { Layer(width: 0.0060002f) });

            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(TasQuery.ConstructionSignatureHash(a), Is.Not.EqualTo(TasQuery.ConstructionSignatureHash(b)));
        }

        // =================================================================================================
        // The COM-free factory over a SAM ApertureConstruction - what the write will really produce
        // =================================================================================================

        [Test]
        public void Factory_TwoApertureConstructionsWithTheSameContent_ResolveToOneDefinition()
        {
            MaterialLibrary materialLibrary = Library();

            ConstructionDefinition a = TasQuery.ConstructionDefinition(Glazing(), AperturePart.Pane, materialLibrary, out string refusal_1);
            ConstructionDefinition b = TasQuery.ConstructionDefinition(Glazing(), AperturePart.Pane, materialLibrary, out string refusal_2);

            Assert.That(refusal_1, Is.Null);
            Assert.That(refusal_2, Is.Null);
            Assert.That(a.Proven, Is.True);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void Factory_PaneAndFrameOfOneApertureConstruction_ResolveToDifferentDefinitions()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();

            ConstructionDefinition pane = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);
            ConstructionDefinition frame = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Frame, materialLibrary, out string _);

            Assert.That(pane, Is.Not.EqualTo(frame));
            Assert.That(pane.LayerCount, Is.EqualTo(3));
            Assert.That(frame.LayerCount, Is.EqualTo(1));
        }

        [Test]
        public void Factory_APaneAndAFrameWithIdenticalLayers_StillResolveToDifferentDefinitions()
        {
            MaterialLibrary materialLibrary = Library();

            List<ConstructionLayer> constructionLayers = new List<ConstructionLayer> { new ConstructionLayer("Timber", 0.05) };
            ApertureConstruction apertureConstruction = Glazing(paneConstructionLayers: constructionLayers, frameConstructionLayers: constructionLayers);

            ConstructionDefinition pane = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);
            ConstructionDefinition frame = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Frame, materialLibrary, out string _);

            Assert.That(pane, Is.Not.EqualTo(frame), "the part is identity in its own right, not something inferred from content");
        }

        [Test]
        public void Factory_ADifferentGlazingBuildUp_ResolvesToADifferentDefinition()
        {
            MaterialLibrary materialLibrary = Library();

            ConstructionDefinition six = TasQuery.ConstructionDefinition(Glazing(), AperturePart.Pane, materialLibrary, out string _);
            ConstructionDefinition four = TasQuery.ConstructionDefinition(
                Glazing(paneConstructionLayers: new List<ConstructionLayer>
                {
                    new ConstructionLayer("Glass 4mm", 0.004),
                    new ConstructionLayer("Air 16mm", 0.016),
                    new ConstructionLayer("Glass 4mm", 0.004)
                }),
                AperturePart.Pane,
                materialLibrary,
                out string _);

            Assert.That(six, Is.Not.EqualTo(four));
        }

        [Test]
        public void Factory_ALayerTheLibraryDoesNotHold_IsSkippedExactlyAsTheWriteSkipsIt()
        {
            MaterialLibrary materialLibrary = Library();

            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(
                Glazing(paneConstructionLayers: new List<ConstructionLayer>
                {
                    new ConstructionLayer("Glass 6mm", 0.006),
                    new ConstructionLayer("Unobtainium", 0.010),
                    new ConstructionLayer("Glass 6mm", 0.006)
                }),
                AperturePart.Pane,
                materialLibrary,
                out string _);

            Assert.That(constructionDefinition.LayerCount, Is.EqualTo(2), "the write never reaches TBD with a layer the library cannot resolve, so the definition must not claim it does");
            Assert.That(constructionDefinition.Proven, Is.True);
        }

        [Test]
        public void Factory_TheMaterialMirror_MatchesWhatTheTBDWriteStores()
        {
            MaterialLibrary materialLibrary = Library();

            ConstructionMaterialDefinition glass = TasQuery.ConstructionMaterialDefinition(materialLibrary.GetMaterial("Glass 6mm"));
            Assert.That(glass.Type, Is.EqualTo(3), "tcdTransparentLayer");
            Assert.That(glass.Conductivity, Is.EqualTo(1f));
            Assert.That(glass.SpecificHeat, Is.EqualTo(0f), "the TBD transparent write does not touch specificHeat, unlike its TCD sibling");
            Assert.That(glass.Density, Is.EqualTo(0f), "nor density");
            Assert.That(glass.Name, Is.EqualTo("Glass 6mm"));

            ConstructionMaterialDefinition timber = TasQuery.ConstructionMaterialDefinition(materialLibrary.GetMaterial("Timber"));
            Assert.That(timber.Type, Is.EqualTo(1), "tcdOpaqueMaterial - the TBD opaque write, not the TCD tcdOpaqueLayer");
            Assert.That(timber.Conductivity, Is.EqualTo(0.13f));
            Assert.That(timber.SpecificHeat, Is.EqualTo(1600f));
            Assert.That(timber.Density, Is.EqualTo(500f));
            Assert.That(timber.SolarTransmittance, Is.EqualTo(0f), "not written by the opaque path");

            ConstructionMaterialDefinition air = TasQuery.ConstructionMaterialDefinition(materialLibrary.GetMaterial("Air 16mm"));
            Assert.That(air.Type, Is.EqualTo(4), "tcdGasLayer");
            Assert.That(air.DynamicViscosity, Is.EqualTo(0.0000181f).Within(1e-12f));
            Assert.That(air.IsBlind, Is.EqualTo(0));
        }

        [Test]
        public void Factory_TheGasMirror_ClampsExactlyAsTheWriteClamps()
        {
            GasMaterial gasMaterial = new GasMaterial("Odd gas", "Gas", "Odd gas", "Negative conductivity", -1, -1, 1.2, 0.000018);

            ConstructionMaterialDefinition constructionMaterialDefinition = TasQuery.ConstructionMaterialDefinition(gasMaterial);

            Assert.That(constructionMaterialDefinition.Conductivity, Is.EqualTo(0f), "the gas write stores a negative conductivity as zero");
            Assert.That(constructionMaterialDefinition.SpecificHeat, Is.EqualTo(0f));
        }

        [Test]
        public void Factory_NoApertureConstruction_RefusesRatherThanGuessing()
        {
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(null, AperturePart.Pane, Library(), out string refusal);

            Assert.That(constructionDefinition, Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        // =================================================================================================
        // Construction naming - definition-derived, deterministic, and never physical identity
        // =================================================================================================

        [Test]
        public void ConstructionName_IsTheApertureConstructionNamePlusThePartSuffix()
        {
            Assert.That(TasQuery.ConstructionName(new string[0], Construction(AperturePart.Pane), GlazingName, out string refusal_Pane), Is.EqualTo("SIM_EXT_GLZ -pane"));
            Assert.That(refusal_Pane, Is.Null);

            Assert.That(TasQuery.ConstructionName(new string[0], Construction(AperturePart.Frame), GlazingName, out string refusal_Frame), Is.EqualTo("SIM_EXT_GLZ -frame"));
            Assert.That(refusal_Frame, Is.Null);
        }

        [Test]
        public void ConstructionName_NeverContainsPhysicalApertureIdentity()
        {
            //The previous naming was Query.Name(aperture.UniqueName(), …) - the aperture's GUID. A
            //GUID-named construction can never be found again by the next identical window.
            System.Guid guid = System.Guid.NewGuid();
            string name_Physical = string.Format("W01 {0}", guid);

            foreach (AperturePart aperturePart in new AperturePart[] { AperturePart.Pane, AperturePart.Frame })
            {
                string name = TasQuery.ConstructionName(new string[0], Construction(aperturePart), GlazingName, out string _);

                Assert.That(name, Does.Not.Contain(guid.ToString()));
                Assert.That(name, Does.Not.Contain(name_Physical));
                Assert.That(name, Does.Contain(GlazingName));
            }
        }

        [Test]
        public void ConstructionName_SameDefinitionOnEveryCall_ResolvesToTheSameName()
        {
            string first = TasQuery.ConstructionName(new string[0], Construction(), GlazingName, out string _);
            string second = TasQuery.ConstructionName(new string[0], Construction(), GlazingName, out string _);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ConstructionName_APreferredNameTakenByDifferentContent_GetsADeterministicSuffixAndKeepsThePartSuffixTerminal()
        {
            ConstructionDefinition constructionDefinition = Construction();

            string name = TasQuery.ConstructionName(new string[] { "SIM_EXT_GLZ -pane" }, constructionDefinition, GlazingName, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(name, Is.EqualTo(string.Format("SIM_EXT_GLZ_{0} -pane", TasQuery.ConstructionSignatureHash(constructionDefinition))));
            Assert.That(name, Does.EndWith("-pane"), "the aperture import classifies a construction by the END of its name, so the discriminator goes on the base");

            //And it is stable: the same definition resolves to the same qualified name next export.
            Assert.That(TasQuery.ConstructionName(new string[] { "SIM_EXT_GLZ -pane" }, Construction(), GlazingName, out string _), Is.EqualTo(name));
        }

        [Test]
        public void ConstructionName_BothCandidatesTaken_RefusesRatherThanGuessingAThird()
        {
            ConstructionDefinition constructionDefinition = Construction();

            string preferred = "SIM_EXT_GLZ -pane";
            string qualified = string.Format("SIM_EXT_GLZ_{0} -pane", TasQuery.ConstructionSignatureHash(constructionDefinition));

            string name = TasQuery.ConstructionName(new string[] { preferred, qualified }, constructionDefinition, GlazingName, out string refusal);

            Assert.That(name, Is.Null);
            Assert.That(refusal, Is.Not.Null);
            Assert.That(refusal, Does.Contain(preferred));
        }

        [Test]
        public void ConstructionName_AnUnusableApertureConstructionName_FallsBackWithoutBreakingTheGrammar()
        {
            Assert.That(TasQuery.ConstructionName(new string[0], Construction(), null, out string _), Is.EqualTo(TasQuery.ConstructionNameBase_Default + " -pane"));
            Assert.That(TasQuery.ConstructionName(new string[0], Construction(), "   ", out string _), Is.EqualTo(TasQuery.ConstructionNameBase_Default + " -pane"));
        }

        [Test]
        public void ConstructionNameBase_SanitisesWhatWouldBreakTheNameGrammar()
        {
            Assert.That(TasQuery.ConstructionNameBase("  Double   Glazing  "), Is.EqualTo("Double Glazing"));
            Assert.That(TasQuery.ConstructionNameBase("SIM_EXT_GLZ"), Is.EqualTo("SIM_EXT_GLZ"), "underscores are kept - this base is the round-trip identity of the SAM ApertureConstruction, and real construction names are full of them");
            Assert.That(TasQuery.ConstructionNameBase("Glazing -pane"), Is.EqualTo("Glazing"), "a base already ending in a part suffix would decompose ambiguously");
            Assert.That(TasQuery.ConstructionNameBase(new string('x', 200)).Length, Is.EqualTo(TasQuery.ConstructionNameBaseLimit));
        }

        [Test]
        public void ConstructionName_Decomposition_RecognisesOnlyThisExportsConvention()
        {
            Assert.That(TasQuery.TryDecomposeConstructionName("SIM_EXT_GLZ -pane", out string @base, out AperturePart aperturePart), Is.True);
            Assert.That(@base, Is.EqualTo("SIM_EXT_GLZ"));
            Assert.That(aperturePart, Is.EqualTo(AperturePart.Pane));

            Assert.That(TasQuery.TryDecomposeConstructionName("SIM_EXT_GLZ -frame", out string _, out AperturePart frame), Is.True);
            Assert.That(frame, Is.EqualTo(AperturePart.Frame));

            Assert.That(TasQuery.TryDecomposeConstructionName("Brick Cavity Wall", out string _, out AperturePart _), Is.False);
            Assert.That(TasQuery.TryDecomposeConstructionName("SIM_EXT_GLZ -pane_1F3A0C21", out string _, out AperturePart _), Is.False, "a name whose part suffix is not terminal is not one of ours - the import would not read it as a pane either");
            Assert.That(TasQuery.TryDecomposeConstructionName(null, out string _, out AperturePart _), Is.False);
        }

        // =================================================================================================
        // The construction seed gate - a construction already in the TBD
        // =================================================================================================

        [Test]
        public void Seed_AConstructionOutsideTheNamingConvention_IsRefused()
        {
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition("Brick Cavity Wall", 1, 0, null, new List<ConstructionLayerDefinition> { Layer() }, out string refusal);

            Assert.That(constructionDefinition, Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void Seed_AConstructionWithAnUnreadableLayer_IsRefused()
        {
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(
                "SIM_EXT_GLZ -pane", 2, 0, null,
                new List<ConstructionLayerDefinition> { Layer(), new ConstructionLayerDefinition(null, 0.016f, 0.016f) },
                out string refusal);

            Assert.That(constructionDefinition, Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void Seed_AConstructionWithNoLayersReported_IsRefused()
        {
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition("SIM_EXT_GLZ -pane", 2, 0, null, null, out string refusal);

            Assert.That(constructionDefinition, Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void Construction_SameNameDifferentContent_IsADifferentDefinition()
        {
            //The heart of the fix. Both are named "SIM_EXT_GLZ -pane"; one is 6mm glazing, the other 4mm.
            ConstructionDefinition seeded = TasQuery.ConstructionDefinition(
                "SIM_EXT_GLZ -pane", 2, 0, null,
                new List<ConstructionLayerDefinition> { Layer(width: 0.004f) },
                out string refusal);

            ConstructionDefinition wanted = Construction(layers: new List<ConstructionLayerDefinition> { Layer(width: 0.006f) });

            Assert.That(refusal, Is.Null);
            Assert.That(seeded, Is.Not.EqualTo(wanted), "a matching name proves nothing about content - which is exactly what the previous by-name lookup assumed");
        }

        [Test]
        public void Seed_AConstructionCarryingAnAdditionalHeatTransfer_IsNotAdopted()
        {
            ConstructionDefinition seeded = TasQuery.ConstructionDefinition("SIM_EXT_GLZ -pane", 2, 0.4f, null, new List<ConstructionLayerDefinition> { Layer() }, out string _);

            Assert.That(seeded, Is.Not.EqualTo(Construction()), "the direct export writes no additional heat transfer, so one that has it states something the model does not");
        }

        [Test]
        public void Seed_AnEquivalentSeededConstruction_IsAdopted()
        {
            //The whole point of reading the physics: a construction this export wrote on a previous run must
            //be recognised again, so a repeated export creates nothing new.
            MaterialLibrary materialLibrary = Library();
            ConstructionDefinition wanted = TasQuery.ConstructionDefinition(Glazing(), AperturePart.Frame, materialLibrary, out string _);

            ConstructionDefinition seeded = TasQuery.ConstructionDefinition(
                "SIM_EXT_GLZ -frame",
                wanted.Type,
                wanted.AdditionalHeatTransfer,
                wanted.Description,
                wanted.Layers,
                out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(seeded, Is.EqualTo(wanted));
        }

        // =================================================================================================
        // BuildingElement identity
        // =================================================================================================

        [Test]
        public void Element_TwoIdenticalDefinitions_AreEqual()
        {
            Assert.That(Element(), Is.EqualTo(Element()));
            Assert.That(Element().GetHashCode(), Is.EqualTo(Element().GetHashCode()));
        }

        [Test]
        public void Element_WindowsAndDoors_NeverMerge()
        {
            //BEType is written from the PART, so a door's pane and a window's pane share it - the aperture
            //type has to be a field of its own.
            BuildingElementDefinition window = Element(ApertureType.Window);
            BuildingElementDefinition door = Element(ApertureType.Door);

            Assert.That(window, Is.Not.EqualTo(door));
            Assert.That(window.BEType, Is.EqualTo(door.BEType), "the two are indistinguishable by BEType, which is why ApertureType is kept explicitly");
        }

        [Test]
        public void Element_PaneAndFrame_NeverMerge()
        {
            Assert.That(Element(aperturePart: AperturePart.Pane), Is.Not.EqualTo(Element(aperturePart: AperturePart.Frame)));
        }

        [Test]
        public void Element_DifferentConstruction_IsADifferentDefinition()
        {
            BuildingElementDefinition six = Element(constructionDefinition: Construction(layers: new List<ConstructionLayerDefinition> { Layer(width: 0.006f) }));
            BuildingElementDefinition four = Element(constructionDefinition: Construction(layers: new List<ConstructionLayerDefinition> { Layer(width: 0.004f) }));

            Assert.That(six, Is.Not.EqualTo(four));
        }

        [Test]
        public void Element_DifferentColour_IsADifferentDefinition()
        {
            Assert.That(Element(colour: 0x0088CCu), Is.Not.EqualTo(Element(colour: 0xCC8800u)));
        }

        [Test]
        public void Element_DifferentBEType_IsADifferentDefinition()
        {
            Assert.That(Element(bEType: 12), Is.Not.EqualTo(Element(bEType: 15)));
        }

        [Test]
        public void Element_DifferentOpeningControl_IsADifferentDefinition()
        {
            BuildingElementDefinition a = Element(apertureTypes: new List<ApertureTypeAssignment> { new ApertureTypeAssignment(ApertureControl(dischargeCoefficient: 1.2f), 1) });
            BuildingElementDefinition b = Element(apertureTypes: new List<ApertureTypeAssignment> { new ApertureTypeAssignment(ApertureControl(dischargeCoefficient: 0.62f), 1) });

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void Element_OpeningMultiplicity_ParticipatesInEquality()
        {
            ApertureTypeDefinition apertureTypeDefinition = ApertureControl();

            BuildingElementDefinition one = Element(apertureTypes: new List<ApertureTypeAssignment>
            {
                new ApertureTypeAssignment(apertureTypeDefinition, 1)
            });

            BuildingElementDefinition two = Element(apertureTypes: new List<ApertureTypeAssignment>
            {
                new ApertureTypeAssignment(apertureTypeDefinition, 1),
                new ApertureTypeAssignment(apertureTypeDefinition, 2)
            });

            Assert.That(one, Is.Not.EqualTo(two), "TAS keeps one entry per aperture type, so a two-opening window needs two types - sharing an element between it and a one-opening window would change how much either ventilates");

            //And the ordinal itself matters: one opening at occurrence 2 is not one opening at occurrence 1.
            BuildingElementDefinition secondOccurrenceAlone = Element(apertureTypes: new List<ApertureTypeAssignment>
            {
                new ApertureTypeAssignment(apertureTypeDefinition, 2)
            });

            Assert.That(secondOccurrenceAlone, Is.Not.EqualTo(one));
        }

        [Test]
        public void Element_NoOpeningProperties_IsAValidSharedDefinition()
        {
            BuildingElementDefinition bare_1 = Element(apertureTypes: null);
            BuildingElementDefinition bare_2 = Element(apertureTypes: new List<ApertureTypeAssignment>());

            Assert.That(bare_1.Proven, Is.True);
            Assert.That(bare_1.ApertureTypeCount, Is.EqualTo(0));
            Assert.That(bare_1, Is.EqualTo(bare_2), "every sealed window in a model resolves to the one bare element");

            Assert.That(bare_1, Is.Not.EqualTo(Element(apertureTypes: new List<ApertureTypeAssignment> { new ApertureTypeAssignment(ApertureControl(), 1) })), "an empty list and a one-entry list are different lists");
        }

        [Test]
        public void Element_AnUnprovenConstruction_MakesTheElementUnshareable()
        {
            BuildingElementDefinition unproven = Element(constructionDefinition: Construction(layers: new List<ConstructionLayerDefinition> { new ConstructionLayerDefinition(null, 0.006f, 0.006f) }));

            Assert.That(unproven.Proven, Is.False);
            Assert.That(unproven, Is.Not.EqualTo(Element()));
        }

        [Test]
        public void Element_ANullConstruction_MakesTheElementUnshareable()
        {
            BuildingElementDefinition unproven = new BuildingElementDefinition(ApertureType.Window, AperturePart.Pane, 12, 0x0088CCu, null, null);

            Assert.That(unproven.Proven, Is.False);
            Assert.That(unproven, Is.Not.EqualTo(Element()));
        }

        [Test]
        public void Element_Signature_IsStableAndDiscriminating()
        {
            Assert.That(TasQuery.BuildingElementSignature(Element()), Is.EqualTo(TasQuery.BuildingElementSignature(Element())));
            Assert.That(TasQuery.BuildingElementSignature(Element(ApertureType.Window)), Is.Not.EqualTo(TasQuery.BuildingElementSignature(Element(ApertureType.Door))));
            Assert.That(TasQuery.BuildingElementSignatureHash(Element(colour: 1u)), Is.Not.EqualTo(TasQuery.BuildingElementSignatureHash(Element(colour: 2u))));
        }

        // =================================================================================================
        // The COM-free factory over a SAM Aperture
        // =================================================================================================

        [Test]
        public void ElementFactory_TwoEquivalentApertures_ResolveToOneDefinition()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);

            BuildingElementDefinition a = TasQuery.BuildingElementDefinition(Window(apertureConstruction, PartO(), offset: 0), AperturePart.Pane, constructionDefinition, DayTypes, out string refusal_1);
            BuildingElementDefinition b = TasQuery.BuildingElementDefinition(Window(apertureConstruction, PartO(), offset: 5), AperturePart.Pane, constructionDefinition, DayTypes, out string refusal_2);

            Assert.That(refusal_1, Is.Null);
            Assert.That(refusal_2, Is.Null);
            Assert.That(a.Proven, Is.True);
            Assert.That(a, Is.EqualTo(b), "two DIFFERENT physical windows stating the same thing are one definition");
        }

        [Test]
        public void ElementFactory_AFrame_CarriesNoOpeningsEvenWhenTheApertureStatesThem()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Frame, materialLibrary, out string _);

            BuildingElementDefinition frame = TasQuery.BuildingElementDefinition(Window(apertureConstruction, PartO()), AperturePart.Frame, constructionDefinition, DayTypes, out string _);

            Assert.That(frame.ApertureTypeCount, Is.EqualTo(0), "only the pane write reaches SetApertureTypes");
        }

        [Test]
        public void ElementFactory_TwoIdenticalOpeningChildren_AreTwoOccurrences()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);

            MultipleOpeningProperties multipleOpeningProperties = new MultipleOpeningProperties(new List<ISingleOpeningProperties> { PartO(), PartO() });

            BuildingElementDefinition buildingElementDefinition = TasQuery.BuildingElementDefinition(Window(apertureConstruction, multipleOpeningProperties), AperturePart.Pane, constructionDefinition, DayTypes, out string _);

            Assert.That(buildingElementDefinition.ApertureTypeCount, Is.EqualTo(2));
            Assert.That(buildingElementDefinition.ApertureTypes[0].Ordinal, Is.EqualTo(1));
            Assert.That(buildingElementDefinition.ApertureTypes[1].Ordinal, Is.EqualTo(2));
            Assert.That(buildingElementDefinition.ApertureTypes[0].ApertureTypeDefinition, Is.EqualTo(buildingElementDefinition.ApertureTypes[1].ApertureTypeDefinition));
        }

        [Test]
        public void ElementFactory_AnApertureStatingNoOpeningProperties_ResolvesToTheBareDefinition()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);

            BuildingElementDefinition a = TasQuery.BuildingElementDefinition(Window(apertureConstruction), AperturePart.Pane, constructionDefinition, DayTypes, out string _);
            BuildingElementDefinition b = TasQuery.BuildingElementDefinition(Window(apertureConstruction, offset: 9), AperturePart.Pane, constructionDefinition, DayTypes, out string _);

            Assert.That(a.ApertureTypeCount, Is.EqualTo(0));
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void ElementFactory_AnExplicitApertureColour_SplitsTheDefinition()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);

            BuildingElementDefinition plain = TasQuery.BuildingElementDefinition(Window(apertureConstruction, PartO()), AperturePart.Pane, constructionDefinition, DayTypes, out string _);
            BuildingElementDefinition coloured = TasQuery.BuildingElementDefinition(Window(apertureConstruction, PartO(), color: System.Drawing.Color.Magenta), AperturePart.Pane, constructionDefinition, DayTypes, out string _);

            Assert.That(coloured.Colour, Is.Not.EqualTo(plain.Colour));
            Assert.That(coloured, Is.Not.EqualTo(plain));
        }

        [Test]
        public void ElementFactory_AWindowAndADoorOfTheSameBuildUp_NeverMerge()
        {
            MaterialLibrary materialLibrary = Library();

            ApertureConstruction window = Glazing(apertureType: ApertureType.Window);
            ApertureConstruction door = Glazing(apertureType: ApertureType.Door);

            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(window, AperturePart.Pane, materialLibrary, out string _);

            BuildingElementDefinition a = TasQuery.BuildingElementDefinition(Window(window, PartO()), AperturePart.Pane, constructionDefinition, DayTypes, out string _);
            BuildingElementDefinition b = TasQuery.BuildingElementDefinition(Window(door, PartO()), AperturePart.Pane, constructionDefinition, DayTypes, out string _);

            Assert.That(a.ApertureType, Is.EqualTo(ApertureType.Window));
            Assert.That(b.ApertureType, Is.EqualTo(ApertureType.Door));
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void ElementFactory_AChildWhoseControlCannotBeResolved_IsOmittedRatherThanCarriedAsAGap()
        {
            //A profile-driven opening with an empty profile is refused by the Stage 1 resolution, so its
            //write puts nothing on the element - and the element is therefore identical to one whose model
            //never stated that child.
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);

            ProfileOpeningProperties unusable = new ProfileOpeningProperties(0.62, new Profile("Empty", ProfileGroup.Ventilation.Text()));

            BuildingElementDefinition buildingElementDefinition = TasQuery.BuildingElementDefinition(
                Window(apertureConstruction, new MultipleOpeningProperties(new List<ISingleOpeningProperties> { PartO(), unusable })),
                AperturePart.Pane,
                constructionDefinition,
                DayTypes,
                out string _);

            Assert.That(buildingElementDefinition.ApertureTypeCount, Is.EqualTo(1));
            Assert.That(buildingElementDefinition.Proven, Is.True);
            Assert.That(buildingElementDefinition.ApertureTypes[0].Ordinal, Is.EqualTo(1));
        }

        [Test]
        public void ElementFactory_AnApertureStatingNeitherWindowNorDoor_StillGetsAShareableDefinition()
        {
            //The write has always handled this: the "Windows: " prefix covers everything that is not a door,
            //and the BEType comes from the part. Refusing here would take a building element away from an
            //aperture that used to get one - and with a deterministic name there would be nowhere for the
            //second such aperture to go.
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing(apertureType: ApertureType.Undefined);
            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);

            BuildingElementDefinition a = TasQuery.BuildingElementDefinition(Window(apertureConstruction, PartO()), AperturePart.Pane, constructionDefinition, DayTypes, out string refusal);
            BuildingElementDefinition b = TasQuery.BuildingElementDefinition(Window(apertureConstruction, PartO(), offset: 7), AperturePart.Pane, constructionDefinition, DayTypes, out string _);

            Assert.That(refusal, Is.Null);
            Assert.That(a.Proven, Is.True, "an undefined aperture type is a distinct value, not a missing one");
            Assert.That(a, Is.EqualTo(b), "so two of them still share one element");
            Assert.That(a, Is.Not.EqualTo(Element(ApertureType.Window, constructionDefinition: constructionDefinition, apertureTypes: a.ApertureTypes)), "and never merge with a real window");

            Assert.That(TasQuery.BuildingElementName(new string[0], a, GlazingName, out string _), Is.EqualTo("Windows: SIM_EXT_GLZ -pane"), "named exactly as the previous write named it - anything that is not a door takes the Windows: prefix");
        }

        [Test]
        public void ElementFactory_AnUndefinedPart_IsRefused()
        {
            //Without a part there is no -pane/-frame suffix, so no name and nothing the import could read.
            BuildingElementDefinition buildingElementDefinition = TasQuery.BuildingElementDefinition(Window(), AperturePart.Undefined, Construction(), DayTypes, out string refusal);

            Assert.That(buildingElementDefinition, Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        // =================================================================================================
        // BuildingElement naming
        // =================================================================================================

        [Test]
        public void ElementName_IsThePrefixTheApertureConstructionNameAndThePartSuffix()
        {
            Assert.That(TasQuery.BuildingElementName(new string[0], Element(ApertureType.Window, AperturePart.Pane), GlazingName, out string _), Is.EqualTo("Windows: SIM_EXT_GLZ -pane"));
            Assert.That(TasQuery.BuildingElementName(new string[0], Element(ApertureType.Window, AperturePart.Frame), GlazingName, out string _), Is.EqualTo("Windows: SIM_EXT_GLZ -frame"));
            Assert.That(TasQuery.BuildingElementName(new string[0], Element(ApertureType.Door, AperturePart.Pane), GlazingName, out string _), Is.EqualTo("Doors: SIM_EXT_GLZ -pane"));
        }

        [Test]
        public void ElementName_NeverContainsPhysicalApertureIdentity()
        {
            System.Guid guid = System.Guid.NewGuid();

            string name = TasQuery.BuildingElementName(new string[0], Element(), GlazingName, out string _);

            Assert.That(name, Does.Not.Contain(guid.ToString()));
            Assert.That(name, Does.Not.Match("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"));
            Assert.That(name, Does.Contain(GlazingName));
        }

        [Test]
        public void ElementName_APreferredNameTakenByADifferentDefinition_GetsADeterministicSuffixAndKeepsThePartSuffixTerminal()
        {
            BuildingElementDefinition buildingElementDefinition = Element();

            string name = TasQuery.BuildingElementName(new string[] { "Windows: SIM_EXT_GLZ -pane" }, buildingElementDefinition, GlazingName, out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(name, Is.EqualTo(string.Format("Windows: SIM_EXT_GLZ_{0} -pane", TasQuery.BuildingElementSignatureHash(buildingElementDefinition))));
            Assert.That(name, Does.EndWith("-pane"));
            Assert.That(name, Does.StartWith("Windows: "));
        }

        [Test]
        public void ElementName_BothCandidatesTaken_RefusesRatherThanGuessingAThird()
        {
            BuildingElementDefinition buildingElementDefinition = Element();

            string preferred = "Windows: SIM_EXT_GLZ -pane";
            string qualified = string.Format("Windows: SIM_EXT_GLZ_{0} -pane", TasQuery.BuildingElementSignatureHash(buildingElementDefinition));

            string name = TasQuery.BuildingElementName(new string[] { preferred, qualified }, buildingElementDefinition, GlazingName, out string refusal);

            Assert.That(name, Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void ElementName_Decomposition_RecognisesOnlyThisExportsConvention()
        {
            Assert.That(TasQuery.TryDecomposeBuildingElementName("Windows: SIM_EXT_GLZ -pane", out string @base, out ApertureType apertureType, out AperturePart aperturePart), Is.True);
            Assert.That(@base, Is.EqualTo("SIM_EXT_GLZ"));
            Assert.That(apertureType, Is.EqualTo(ApertureType.Window));
            Assert.That(aperturePart, Is.EqualTo(AperturePart.Pane));

            Assert.That(TasQuery.TryDecomposeBuildingElementName("Doors: D01 -frame", out string _, out ApertureType door, out AperturePart _), Is.True);
            Assert.That(door, Is.EqualTo(ApertureType.Door));

            Assert.That(TasQuery.TryDecomposeBuildingElementName("External Wall", out string _, out ApertureType _, out AperturePart _), Is.False, "a panel element is never an aperture reuse candidate");
            Assert.That(TasQuery.TryDecomposeBuildingElementName("Glazing -pane", out string _, out ApertureType _, out AperturePart _), Is.False, "no Windows:/Doors: prefix");
        }

        [Test]
        public void ElementName_ALegacyGuidNamedElement_StillDecomposesButItsBaseIsNotAName()
        {
            //A TBD written before this change carries "Windows: W01 <guid> -pane". It decomposes, so it is a
            //CANDIDATE; whether it is shared is decided by its definition, and its base will not match any
            //ApertureConstruction name, so a fresh export will not collide with it.
            string name = string.Format("Windows: W01 {0} -pane", System.Guid.NewGuid());

            Assert.That(TasQuery.TryDecomposeBuildingElementName(name, out string @base, out ApertureType _, out AperturePart _), Is.True);
            Assert.That(@base, Is.Not.EqualTo(GlazingName));
        }

        // =================================================================================================
        // The BuildingElement seed gate - an element already in the TBD
        // =================================================================================================

        [Test]
        public void SeedGate_AnEquivalentElementThisExportAuthored_IsAdopted()
        {
            BuildingElementDefinition buildingElementDefinition = TasQuery.BuildingElementDefinition(Seed(), out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(buildingElementDefinition, Is.EqualTo(Element()));
        }

        [Test]
        public void SeedGate_AnElementOutsideTheNamingConvention_IsRefused()
        {
            Assert.That(TasQuery.BuildingElementDefinition(Seed(name: "External Wall"), out string refusal), Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void SeedGate_ANonDefaultGhost_IsRefused()
        {
            Assert.That(TasQuery.BuildingElementDefinition(Seed(ghost: 1), out string refusal), Is.Null);
            Assert.That(refusal, Does.Contain("ghost"));
        }

        [Test]
        public void SeedGate_AForeignDescription_IsRefused()
        {
            Assert.That(TasQuery.BuildingElementDefinition(Seed(description: "Authored by another tool"), out string refusal), Is.Null);
            Assert.That(refusal, Does.Contain("description"));
        }

        [Test]
        public void SeedGate_AnAssignedFeatureShade_IsRefused()
        {
            Assert.That(TasQuery.BuildingElementDefinition(Seed(featureShade: true), out string refusal), Is.Null);
            Assert.That(refusal, Does.Contain("feature shade"));
        }

        [Test]
        public void SeedGate_AnAssignedSubstituteElement_IsRefused()
        {
            Assert.That(TasQuery.BuildingElementDefinition(Seed(substituteElement: true), out string refusal), Is.Null);
            Assert.That(refusal, Does.Contain("substitute element"));
        }

        [TestCase(1, 0, 0f)]
        [TestCase(0, 1, 0f)]
        [TestCase(0, 0, 0.1f)]
        public void SeedGate_AnUnclassifiedFieldSetBySomethingElse_IsRefused(int ground, int markDelete, float width)
        {
            Assert.That(TasQuery.BuildingElementDefinition(Seed(ground: ground, markDelete: markDelete, width: width), out string refusal), Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void SeedGate_AnUnreadableConstruction_IsRefused()
        {
            //Built inline rather than through Seed(): a null there means "use the default", and the point
            //here is an element whose construction genuinely could not be read or classified.
            BuildingElementSeed buildingElementSeed = new BuildingElementSeed("Windows: " + GlazingName + " -pane", 0, null, false, false, 0, 0, 0f, 12, 0x0088CCu, null, null);

            Assert.That(TasQuery.BuildingElementDefinition(buildingElementSeed, out string refusal), Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void SeedGate_AnOpeningControlThatMayNotBeReused_IsRefused()
        {
            BuildingElementSeed buildingElementSeed = Seed(apertureTypeAssignments: new List<KeyValuePair<string, ApertureTypeDefinition>>
            {
                new KeyValuePair<string, ApertureTypeDefinition>("Opening Cd1.2 F1", null)
            });

            Assert.That(TasQuery.BuildingElementDefinition(buildingElementSeed, out string refusal), Is.Null);
            Assert.That(refusal, Is.Not.Null);
        }

        [Test]
        public void SeedGate_AnOpeningNamedOutsideTheStage1Convention_IsRefused()
        {
            BuildingElementSeed buildingElementSeed = Seed(apertureTypeAssignments: new List<KeyValuePair<string, ApertureTypeDefinition>>
            {
                new KeyValuePair<string, ApertureTypeDefinition>("Somebody else's opening", ApertureControl())
            });

            Assert.That(TasQuery.BuildingElementDefinition(buildingElementSeed, out string refusal), Is.Null);
            Assert.That(refusal, Does.Contain("occurrence"));
        }

        [Test]
        public void SeedGate_AFrameCarryingAnOpening_IsRefused()
        {
            BuildingElementSeed buildingElementSeed = Seed(
                name: "Windows: " + GlazingName + " -frame",
                constructionDefinition: Construction(AperturePart.Frame),
                apertureTypeAssignments: new List<KeyValuePair<string, ApertureTypeDefinition>>
                {
                    new KeyValuePair<string, ApertureTypeDefinition>("Opening Cd1.2 F1", ApertureControl())
                });

            Assert.That(TasQuery.BuildingElementDefinition(buildingElementSeed, out string refusal), Is.Null);
            Assert.That(refusal, Does.Contain("frame"));
        }

        [Test]
        public void SeedGate_TheOrdinalComesFromTheOpeningName()
        {
            ApertureTypeDefinition apertureTypeDefinition = ApertureControl();

            BuildingElementDefinition buildingElementDefinition = TasQuery.BuildingElementDefinition(
                Seed(apertureTypeAssignments: new List<KeyValuePair<string, ApertureTypeDefinition>>
                {
                    new KeyValuePair<string, ApertureTypeDefinition>("Opening Cd1.2 F1", apertureTypeDefinition),
                    new KeyValuePair<string, ApertureTypeDefinition>("Opening Cd1.2 F1 2", apertureTypeDefinition)
                }),
                out string refusal);

            Assert.That(refusal, Is.Null);
            Assert.That(buildingElementDefinition.ApertureTypes.Select(x => x.Ordinal), Is.EqualTo(new[] { 1, 2 }));
        }

        // =================================================================================================
        // Fake-COM: the write itself, with every property set recorded
        // =================================================================================================

        /// <summary>
        /// A stand-in for a <c>TBD.Construction</c> that RECORDS every property set. The recording is the
        /// point: a shared definition is immutable, so the test for reuse is that this log does not grow -
        /// not that it grows with the same values.
        /// </summary>
        private sealed class FakeTBDConstruction
        {
            private string name;
            private int type = 1;

            public List<string> WriteLog { get; } = new List<string>();

            public List<KeyValuePair<string, float>> Layers { get; } = new List<KeyValuePair<string, float>>();

            public string Name
            {
                get { return name; }
                set { WriteLog.Add("construction.name"); name = value; }
            }

            public int Type
            {
                get { return type; }
                set { WriteLog.Add("construction.type"); type = value; }
            }

            public void AddMaterial(string materialName, float width)
            {
                WriteLog.Add("construction.AddMaterial");
                Layers.Add(new KeyValuePair<string, float>(materialName, width));
            }
        }

        /// <summary>A stand-in for an aperture's <c>TBD.buildingElement</c>, recording every property set.</summary>
        private sealed class FakeTBDApertureBuildingElement
        {
            private string name;
            private uint colour;
            private int bEType;

            public List<string> WriteLog { get; } = new List<string>();

            public List<zoneSurfaceStub> ZoneSurfaces { get; } = new List<zoneSurfaceStub>();

            public List<string> ApertureTypes { get; } = new List<string>();

            public FakeTBDConstruction Construction { get; private set; }

            public string Name
            {
                get { return name; }
                set { WriteLog.Add("buildingElement.name"); name = value; }
            }

            public uint Colour
            {
                get { return colour; }
                set { WriteLog.Add("buildingElement.colour"); colour = value; }
            }

            public int BEType
            {
                get { return bEType; }
                set { WriteLog.Add("buildingElement.BEType"); bEType = value; }
            }

            public void AssignConstruction(FakeTBDConstruction construction)
            {
                WriteLog.Add("buildingElement.AssignConstruction");
                Construction = construction;
            }

            public void AssignApertureType(string apertureTypeName)
            {
                WriteLog.Add("buildingElement.AssignApertureType");
                ApertureTypes.Add(apertureTypeName);
            }
        }

        /// <summary>A physical aperture surface. One per window half, always - that is the invariant.</summary>
        private sealed class zoneSurfaceStub
        {
            public zoneSurfaceStub(System.Guid apertureGuid, AperturePart aperturePart)
            {
                ApertureGuid = apertureGuid;
                AperturePart = aperturePart;
            }

            public System.Guid ApertureGuid { get; }

            public AperturePart AperturePart { get; }

            public FakeTBDApertureBuildingElement BuildingElement { get; set; }
        }

        /// <summary>
        /// The building's reusable definitions, as <c>BuildingReuseCache</c> holds them: a definition-keyed
        /// equality scan, plus a name namespace. Every DECISION is delegated to the production helpers - what
        /// a definition is, whether two are equal, and what name a new object gets - so this harness models
        /// the COM traffic and nothing else.
        /// </summary>
        private sealed class FakeReuseStore
        {
            private readonly List<KeyValuePair<ConstructionDefinition, FakeTBDConstruction>> constructions = new List<KeyValuePair<ConstructionDefinition, FakeTBDConstruction>>();
            private readonly List<KeyValuePair<BuildingElementDefinition, FakeTBDApertureBuildingElement>> buildingElements = new List<KeyValuePair<BuildingElementDefinition, FakeTBDApertureBuildingElement>>();

            public List<string> ConstructionNames { get; } = new List<string>();

            public List<string> BuildingElementNames { get; } = new List<string>();

            public List<FakeTBDConstruction> Constructions
            {
                get { return constructions.Select(x => x.Value).ToList(); }
            }

            public List<FakeTBDApertureBuildingElement> BuildingElements
            {
                get { return buildingElements.Select(x => x.Value).ToList(); }
            }

            public FakeTBDConstruction FindConstruction(ConstructionDefinition constructionDefinition)
            {
                if (constructionDefinition == null || !constructionDefinition.Proven)
                {
                    return null;
                }

                foreach (KeyValuePair<ConstructionDefinition, FakeTBDConstruction> keyValuePair in constructions)
                {
                    if (keyValuePair.Key != null && keyValuePair.Key.Equals(constructionDefinition))
                    {
                        return keyValuePair.Value;
                    }
                }

                return null;
            }

            public void RegisterConstruction(FakeTBDConstruction construction, ConstructionDefinition constructionDefinition)
            {
                constructions.Add(new KeyValuePair<ConstructionDefinition, FakeTBDConstruction>(constructionDefinition, construction));
            }

            public FakeTBDApertureBuildingElement FindApertureBuildingElement(BuildingElementDefinition buildingElementDefinition)
            {
                if (buildingElementDefinition == null || !buildingElementDefinition.Proven)
                {
                    return null;
                }

                foreach (KeyValuePair<BuildingElementDefinition, FakeTBDApertureBuildingElement> keyValuePair in buildingElements)
                {
                    if (keyValuePair.Key != null && keyValuePair.Key.Equals(buildingElementDefinition))
                    {
                        return keyValuePair.Value;
                    }
                }

                return null;
            }

            public void RegisterApertureBuildingElement(FakeTBDApertureBuildingElement buildingElement, BuildingElementDefinition buildingElementDefinition)
            {
                buildingElements.Add(new KeyValuePair<BuildingElementDefinition, FakeTBDApertureBuildingElement>(buildingElementDefinition, buildingElement));
            }
        }

        /// <summary>
        /// The direct export's aperture pass, in the same order <c>Modify.Update</c> runs it: resolve the
        /// construction definition, reuse or create, resolve the element definition, reuse or create, write
        /// the openings ONCE on creation, then point this aperture's physical surfaces at whatever came back.
        /// Every decision is the production helper's.
        /// </summary>
        private static List<zoneSurfaceStub> Export(FakeReuseStore store, MaterialLibrary materialLibrary, IEnumerable<Aperture> apertures)
        {
            List<zoneSurfaceStub> zoneSurfaces = new List<zoneSurfaceStub>();

            foreach (Aperture aperture in apertures)
            {
                foreach (AperturePart aperturePart in new AperturePart[] { AperturePart.Frame, AperturePart.Pane })
                {
                    //The physical surface exists whatever the definitions resolve to.
                    zoneSurfaceStub zoneSurface = new zoneSurfaceStub(aperture.Guid, aperturePart);
                    zoneSurfaces.Add(zoneSurface);

                    ApertureConstruction apertureConstruction = aperture.ApertureConstruction;

                    ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, aperturePart, materialLibrary, out string _);

                    FakeTBDConstruction construction = store.FindConstruction(constructionDefinition);
                    if (construction == null)
                    {
                        string constructionName = TasQuery.ConstructionName(store.ConstructionNames, constructionDefinition, apertureConstruction.Name, out string _);
                        if (constructionName != null)
                        {
                            construction = new FakeTBDConstruction { Name = constructionName };
                            store.ConstructionNames.Add(constructionName);

                            if (apertureConstruction.Transparent(materialLibrary, aperturePart))
                            {
                                construction.Type = 2;
                            }

                            foreach (ConstructionLayerDefinition constructionLayerDefinition in constructionDefinition.Layers)
                            {
                                construction.AddMaterial(constructionLayerDefinition.Material?.Name, constructionLayerDefinition.Width);
                            }

                            if (constructionDefinition.Proven)
                            {
                                store.RegisterConstruction(construction, constructionDefinition);
                            }
                        }
                    }

                    if (construction == null)
                    {
                        continue;
                    }

                    BuildingElementDefinition buildingElementDefinition = TasQuery.BuildingElementDefinition(aperture, aperturePart, constructionDefinition, DayTypes, out string _);

                    FakeTBDApertureBuildingElement buildingElement = store.FindApertureBuildingElement(buildingElementDefinition);
                    if (buildingElement == null)
                    {
                        string buildingElementName = TasQuery.BuildingElementName(store.BuildingElementNames, buildingElementDefinition, apertureConstruction.Name, out string _);
                        if (buildingElementName != null)
                        {
                            buildingElement = new FakeTBDApertureBuildingElement { Name = buildingElementName };
                            store.BuildingElementNames.Add(buildingElementName);

                            buildingElement.Colour = buildingElementDefinition.Colour;
                            buildingElement.BEType = buildingElementDefinition.BEType;
                            buildingElement.AssignConstruction(construction);

                            //SetApertureTypes, once, for the pane - one assignment per opening, named by the
                            //Stage 1 definition-derived naming.
                            List<string> names_ApertureType = new List<string>();
                            foreach (ApertureTypeAssignment apertureTypeAssignment in buildingElementDefinition.ApertureTypes)
                            {
                                string name_ApertureType = TasQuery.ApertureTypeName(names_ApertureType, apertureTypeAssignment.ApertureTypeDefinition, apertureTypeAssignment.Ordinal, out string _);
                                names_ApertureType.Add(name_ApertureType);
                                buildingElement.AssignApertureType(name_ApertureType);
                            }

                            if (buildingElementDefinition.Proven && buildingElement.ApertureTypes.Count == buildingElementDefinition.ApertureTypeCount)
                            {
                                store.RegisterApertureBuildingElement(buildingElement, buildingElementDefinition);
                            }
                        }
                    }

                    zoneSurface.BuildingElement = buildingElement;
                }
            }

            return zoneSurfaces;
        }

        [Test]
        public void Export_TwoHundredIdenticalWindows_ProduceTwoConstructionsTwoElementsAndFourHundredSurfaces()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            FakeReuseStore store = new FakeReuseStore();

            List<Aperture> apertures = Enumerable.Range(0, 200).Select(i => Window(apertureConstruction, PartO(), offset: i * 2)).ToList();

            List<zoneSurfaceStub> zoneSurfaces = Export(store, materialLibrary, apertures);

            Assert.Multiple(() =>
            {
                Assert.That(zoneSurfaces.Count, Is.EqualTo(400), "every physical window keeps its own pane and frame surface");
                Assert.That(zoneSurfaces.Select(x => x.ApertureGuid).Distinct().Count(), Is.EqualTo(200));
                Assert.That(zoneSurfaces.All(x => x.BuildingElement != null), Is.True, "no surface is left without a building element");

                Assert.That(store.Constructions.Count, Is.EqualTo(2), "one pane construction and one frame construction");
                Assert.That(store.BuildingElements.Count, Is.EqualTo(2), "one pane element and one frame element");

                Assert.That(store.ConstructionNames, Is.EquivalentTo(new[] { "SIM_EXT_GLZ -pane", "SIM_EXT_GLZ -frame" }));
                Assert.That(store.BuildingElementNames, Is.EquivalentTo(new[] { "Windows: SIM_EXT_GLZ -pane", "Windows: SIM_EXT_GLZ -frame" }));

                Assert.That(zoneSurfaces.Where(x => x.AperturePart == AperturePart.Pane).Select(x => x.BuildingElement).Distinct().Count(), Is.EqualTo(1));
                Assert.That(zoneSurfaces.Where(x => x.AperturePart == AperturePart.Frame).Select(x => x.BuildingElement).Distinct().Count(), Is.EqualTo(1));
            });
        }

        [Test]
        public void Export_ASharedHit_PerformsNoWritesOnTheSharedDefinitions()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            FakeReuseStore store = new FakeReuseStore();

            //First window: creates both definitions, so writes are expected.
            Export(store, materialLibrary, new List<Aperture> { Window(apertureConstruction, PartO(), offset: 0) });

            List<FakeTBDConstruction> constructions = store.Constructions;
            List<FakeTBDApertureBuildingElement> buildingElements = store.BuildingElements;

            List<string> log_Constructions = constructions.SelectMany(x => x.WriteLog).ToList();
            List<string> log_Elements = buildingElements.SelectMany(x => x.WriteLog).ToList();
            List<int> counts_ApertureTypes = buildingElements.Select(x => x.ApertureTypes.Count).ToList();

            Assert.That(log_Constructions, Is.Not.Empty, "the first window really does create them");

            //Ninety-nine more identical windows: every one of them must be a pure lookup plus a surface
            //assignment. Not one property may be set on the shared objects - every other window sees it.
            Export(store, materialLibrary, Enumerable.Range(1, 99).Select(i => Window(apertureConstruction, PartO(), offset: i * 2)).ToList());

            Assert.Multiple(() =>
            {
                Assert.That(store.Constructions.Count, Is.EqualTo(2));
                Assert.That(store.BuildingElements.Count, Is.EqualTo(2));
                Assert.That(constructions.SelectMany(x => x.WriteLog), Is.EqualTo(log_Constructions), "a shared construction is immutable");
                Assert.That(buildingElements.SelectMany(x => x.WriteLog), Is.EqualTo(log_Elements), "a shared building element is immutable");
                Assert.That(buildingElements.Select(x => x.ApertureTypes.Count), Is.EqualTo(counts_ApertureTypes), "no element gained a second opening");
            });
        }

        [Test]
        public void Export_RepeatingTheExportOverTheSameStore_CreatesNothingFurther()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            FakeReuseStore store = new FakeReuseStore();

            List<Aperture> apertures = Enumerable.Range(0, 20).Select(i => Window(apertureConstruction, PartO(), offset: i * 2)).ToList();

            Export(store, materialLibrary, apertures);

            int count_Constructions = store.Constructions.Count;
            int count_Elements = store.BuildingElements.Count;

            Export(store, materialLibrary, apertures);

            Assert.That(store.Constructions.Count, Is.EqualTo(count_Constructions));
            Assert.That(store.BuildingElements.Count, Is.EqualTo(count_Elements));
        }

        [Test]
        public void Export_SeveralConstructionsAndControls_ProduceOneDefinitionEach()
        {
            MaterialLibrary materialLibrary = Library();
            FakeReuseStore store = new FakeReuseStore();

            ApertureConstruction glazing_6mm = Glazing("GLZ_6", ApertureType.Window);
            ApertureConstruction glazing_4mm = Glazing(
                "GLZ_4",
                ApertureType.Window,
                new List<ConstructionLayer> { new ConstructionLayer("Glass 4mm", 0.004), new ConstructionLayer("Air 16mm", 0.016), new ConstructionLayer("Glass 4mm", 0.004) },
                new List<ConstructionLayer> { new ConstructionLayer("Aluminium", 0.05) });
            ApertureConstruction door = Glazing("DR_01", ApertureType.Door);

            List<Aperture> apertures = new List<Aperture>();
            for (int i = 0; i < 10; i++)
            {
                apertures.Add(Window(glazing_6mm, PartO(), offset: i * 10 + 0));
                apertures.Add(Window(glazing_6mm, PartO(OpeningRestriction.NightClosed), offset: i * 10 + 1));
                apertures.Add(Window(glazing_6mm, null, offset: i * 10 + 2));
                apertures.Add(Window(glazing_4mm, PartO(), offset: i * 10 + 3));
                apertures.Add(Window(door, PartO(), offset: i * 10 + 4));
            }

            List<zoneSurfaceStub> zoneSurfaces = Export(store, materialLibrary, apertures);

            Assert.Multiple(() =>
            {
                Assert.That(zoneSurfaces.Count, Is.EqualTo(apertures.Count * 2), "every physical aperture surface survives");
                Assert.That(zoneSurfaces.All(x => x.BuildingElement != null), Is.True);

                //Three aperture constructions x pane + frame. GLZ_6 and DR_01 share their build-up, so their
                //CONSTRUCTIONS are shared even though their ELEMENTS are not - content is content.
                Assert.That(store.Constructions.Count, Is.EqualTo(4), "GLZ_6 and DR_01 hold identical layers, so they share a pane and a frame construction");

                //Elements, exhaustively: GLZ_6 panes for unrestricted / night-closed / sealed, plus one
                //GLZ_6 frame; a GLZ_4 pane and a GLZ_4 frame; a DR_01 pane and a DR_01 frame. The sealed
                //window differs from the openable one in colour as well as in openings, and a frame colour
                //does not depend on whether the window opens - so the three GLZ_6 windows share one frame.
                Assert.That(store.BuildingElements.Count, Is.EqualTo(8));

                //The three GLZ_6 panes all prefer one name, so two of them take the deterministic
                //collision-suffixed form - and every one of them still ends in the part suffix, which is what
                //the aperture import classifies on.
                List<string> names_Pane = store.BuildingElementNames.Where(x => x.EndsWith("-pane")).ToList();
                Assert.That(names_Pane.Count, Is.EqualTo(5));
                Assert.That(names_Pane.Distinct().Count(), Is.EqualTo(names_Pane.Count), "no two definitions may share a name");
                Assert.That(names_Pane.Count(x => x.StartsWith("Windows: GLZ_6")), Is.EqualTo(3));

                Assert.That(store.BuildingElementNames.Count(x => x.StartsWith("Doors: ")), Is.EqualTo(2), "a door never merges into a window");
                Assert.That(store.BuildingElementNames.All(x => !System.Text.RegularExpressions.Regex.IsMatch(x, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-")), Is.True, "no generated name carries a physical aperture GUID");
                Assert.That(store.ConstructionNames.All(x => !System.Text.RegularExpressions.Regex.IsMatch(x, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-")), Is.True);
            });
        }

        [Test]
        public void Export_WindowsWithTwoIdenticalOpenings_KeepBothOpeningsOnOneSharedElement()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            FakeReuseStore store = new FakeReuseStore();

            List<Aperture> apertures = Enumerable.Range(0, 50)
                .Select(i => Window(apertureConstruction, new MultipleOpeningProperties(new List<ISingleOpeningProperties> { PartO(), PartO() }), offset: i * 2))
                .ToList();

            Export(store, materialLibrary, apertures);

            FakeTBDApertureBuildingElement pane = store.BuildingElements.Find(x => x.Name.EndsWith("-pane"));

            Assert.That(store.BuildingElements.Count, Is.EqualTo(2));
            Assert.That(pane.ApertureTypes.Count, Is.EqualTo(2), "two identical openings are two distinct aperture types - one per occurrence");
            Assert.That(pane.ApertureTypes.Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void Shared_MismatchedExistingConstruction_IsNeverWrittenTo()
        {
            //A construction already occupying the name this export wants, holding different content. The old
            //by-name lookup adopted it; the definition lookup must not, and must not touch it either.
            MaterialLibrary materialLibrary = Library();
            FakeReuseStore store = new FakeReuseStore();

            FakeTBDConstruction seeded = new FakeTBDConstruction { Name = "SIM_EXT_GLZ -pane" };
            seeded.AddMaterial("Glass 4mm", 0.004f);
            store.ConstructionNames.Add(seeded.Name);

            ConstructionDefinition seededDefinition = TasQuery.ConstructionDefinition(
                seeded.Name, 2, 0, null,
                new List<ConstructionLayerDefinition> { Layer(MaterialDefinition(name: "Glass 4mm"), 0.004f) },
                out string _);
            store.RegisterConstruction(seeded, seededDefinition);

            List<string> log_Seeded = seeded.WriteLog.ToList();
            List<KeyValuePair<string, float>> layers_Seeded = seeded.Layers.ToList();

            Export(store, materialLibrary, new List<Aperture> { Window(Glazing(), PartO()) });

            Assert.Multiple(() =>
            {
                Assert.That(seeded.WriteLog, Is.EqualTo(log_Seeded), "a construction whose content does not match is never mutated to make it fit");
                Assert.That(seeded.Layers, Is.EqualTo(layers_Seeded));

                FakeTBDConstruction created = store.Constructions.Find(x => x != seeded && x.Name.EndsWith("-pane"));
                Assert.That(created, Is.Not.Null, "a distinct construction is created instead");
                Assert.That(created.Name, Does.StartWith("SIM_EXT_GLZ_"), "under a deterministic collision-suffixed name");
                Assert.That(created.Name, Does.EndWith("-pane"));
            });
        }

        [Test]
        public void Shared_MismatchedExistingBuildingElement_IsNeverWrittenTo()
        {
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            FakeReuseStore store = new FakeReuseStore();

            //An element of the wanted name whose definition differs only in colour.
            FakeTBDApertureBuildingElement seeded = new FakeTBDApertureBuildingElement { Name = "Windows: SIM_EXT_GLZ -pane" };
            store.BuildingElementNames.Add(seeded.Name);

            ConstructionDefinition constructionDefinition = TasQuery.ConstructionDefinition(apertureConstruction, AperturePart.Pane, materialLibrary, out string _);
            BuildingElementDefinition seededDefinition = TasQuery.BuildingElementDefinition(
                Seed(constructionDefinition: constructionDefinition, colour: 0x123456u, apertureTypeAssignments: null),
                out string _);
            store.RegisterApertureBuildingElement(seeded, seededDefinition);

            List<string> log_Seeded = seeded.WriteLog.ToList();

            Export(store, materialLibrary, new List<Aperture> { Window(apertureConstruction, PartO()) });

            Assert.That(seeded.WriteLog, Is.EqualTo(log_Seeded), "a building element whose definition does not match is never rewritten to make it fit");
            Assert.That(seeded.ApertureTypes, Is.Empty);

            FakeTBDApertureBuildingElement created = store.BuildingElements.Find(x => x != seeded && x.Name.EndsWith("-pane"));
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Name, Does.StartWith("Windows: SIM_EXT_GLZ_"));
            Assert.That(created.Name, Does.EndWith("-pane"));
        }

        [Test]
        public void Shared_AnEquivalentSeededElement_IsAdoptedWithoutAnyWrite()
        {
            //The repeated-export case: the element this export authored last time is recognised and adopted,
            //and nothing at all is written to it.
            MaterialLibrary materialLibrary = Library();
            ApertureConstruction apertureConstruction = Glazing();
            FakeReuseStore store = new FakeReuseStore();

            Export(store, materialLibrary, new List<Aperture> { Window(apertureConstruction, PartO(), offset: 0) });

            List<FakeTBDApertureBuildingElement> buildingElements = store.BuildingElements;
            List<string> log = buildingElements.SelectMany(x => x.WriteLog).ToList();

            List<zoneSurfaceStub> zoneSurfaces = Export(store, materialLibrary, new List<Aperture> { Window(apertureConstruction, PartO(), offset: 100) });

            Assert.That(store.BuildingElements.Count, Is.EqualTo(2));
            Assert.That(buildingElements.SelectMany(x => x.WriteLog), Is.EqualTo(log));
            Assert.That(zoneSurfaces.All(x => buildingElements.Contains(x.BuildingElement)), Is.True, "the second window's surfaces point at the first window's elements");
        }
    }
}
