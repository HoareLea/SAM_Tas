// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using System;
using System.Collections.Generic;
using TasCreate = SAM.Analytical.Tas.Create;
using TasQuery = SAM.Analytical.Tas.Query;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>U-value calculator PR1a - the default layer the thickness calculation adjusts.</b>
    /// <para>
    /// The picker used to choose the lowest-conductivity layer of 10 mm or more. A model's gas materials have
    /// real, positive conductivities (0.024 and 0.01622 W/mK), below mineral wool's 0.025, so the air cavity
    /// won and the target U-value was not reachable ("Could not calculate construction for given criteria").
    /// The rule is now by material TYPE: gas and transparent layers are never adjustable. All of this is
    /// COM-free: <see cref="TasQuery.AdjustableLayerIndex(Construction, MaterialLibrary)"/> and
    /// <see cref="TasCreate.LayerThicknessCalculationData(Construction, MaterialLibrary)"/> read SAM objects
    /// only, and the TCD-side fallback shares <see cref="TasQuery.IsAdjustableLayer"/>.
    /// </para>
    /// </summary>
    [TestFixture]
    public class AdjustableLayerPickerTests
    {
        // =================================================================================================
        // Builders - the conductivities are the ones in the user's model (SIM_EXT_SLD)
        // =================================================================================================

        private static GasMaterial Gas(string name, double conductivity)
        {
            return Analytical.Create.GasMaterial(name, string.Empty, name, string.Empty, conductivity, 1000, 1.2, 1.8E-5, 0.05, 1, 5);
        }

        private static OpaqueMaterial Opaque(string name, double conductivity)
        {
            return Analytical.Create.OpaqueMaterial(name, string.Empty, name, string.Empty, conductivity, 1000, 20, 0.1, 1, 0.5, 0.5, 0.5, 0.5, 0.9, 0.9, false);
        }

        private static TransparentMaterial Glass(string name, double conductivity)
        {
            return Analytical.Create.TransparentMaterial(name, string.Empty, name, string.Empty, conductivity, 0.006, 1, 0.8, 0.8, 0.1, 0.1, 0.1, 0.1, 0.9, 0.9, false);
        }

        private static MaterialLibrary Library(params IMaterial[] materials)
        {
            MaterialLibrary materialLibrary = new MaterialLibrary("Test");
            foreach (IMaterial material in materials)
            {
                Assert.That(materialLibrary.Add(material), Is.True, material.Name);
            }

            return materialLibrary;
        }

        private static Construction Wall(params (string Name, double Thickness)[] layers)
        {
            List<ConstructionLayer> constructionLayers = new List<ConstructionLayer>();
            foreach ((string name, double thickness) in layers)
            {
                constructionLayers.Add(new ConstructionLayer(name, thickness));
            }

            Construction construction = new Construction("SIM_EXT_SLD", constructionLayers);
            construction.SetValue(Analytical.ConstructionParameter.DefaultPanelType, PanelType.WallExternal.ToString());
            return construction;
        }

        private const string Air50 = "Ar90Up_Air__50mm_1.25W/m2K";
        private const string Cement = "CementParticleboard";
        private const string MineralWool = "I01_Mineral Wool_20kg/m3_0.025W/mK";
        private const string Air50Other = "Ar90Down_Air__50mm";
        private const string Rainscreen = "Rainscreen";

        private static MaterialLibrary UserModelLibrary()
        {
            return Library(
                Gas(Air50, 0.024),
                Opaque(Cement, 0.23),
                Opaque(MineralWool, 0.025),
                Gas(Air50Other, 0.01622),
                Opaque(Rainscreen, 0.2));
        }

        private static Construction UserModelWall()
        {
            return Wall((Air50, 0.05), (Cement, 0.012), (MineralWool, 0.08), (Air50Other, 0.05), (Rainscreen, 0.003));
        }

        // =================================================================================================
        // The user's wall
        // =================================================================================================

        [Test]
        public void UserModelWall_PicksMineralWool_NotTheAirCavity()
        {
            Assert.That(TasQuery.AdjustableLayerIndex(UserModelWall(), UserModelLibrary()), Is.EqualTo(2));
        }

        [Test]
        public void UserModelWall_CalculationData_CarriesTheMineralWoolLayerAndTheWallHeatFlow()
        {
            LayerThicknessCalculationData data = TasCreate.LayerThicknessCalculationData(UserModelWall(), UserModelLibrary());

            Assert.That(data, Is.Not.Null);
            Assert.That(data.LayerIndex, Is.EqualTo(2));
            Assert.That(data.ConstructionName, Is.EqualTo("SIM_EXT_SLD"));
            Assert.That(data.HeatFlowDirection, Is.Not.EqualTo(HeatFlowDirection.Undefined));
        }

        // =================================================================================================
        // Gas and glass are excluded by type, never by conductivity
        // =================================================================================================

        [Test]
        public void GasLayer_IsNeverChosen_EvenWithTheLowestConductivity()
        {
            MaterialLibrary materialLibrary = Library(Gas("Cavity", 0.0001), Opaque("Brick", 0.8));
            Construction construction = Wall(("Cavity", 0.05), ("Brick", 0.1));

            Assert.That(TasQuery.AdjustableLayerIndex(construction, materialLibrary), Is.EqualTo(1));
        }

        [Test]
        public void TransparentLayer_IsNeverChosen_EvenWithTheLowestConductivity()
        {
            MaterialLibrary materialLibrary = Library(Glass("Pane", 0.0001), Opaque("Brick", 0.8));
            Construction construction = Wall(("Pane", 0.02), ("Brick", 0.1));

            Assert.That(TasQuery.AdjustableLayerIndex(construction, materialLibrary), Is.EqualTo(1));
        }

        [Test]
        public void OnlyGasAndGlass_GivesNoAdjustableLayer()
        {
            MaterialLibrary materialLibrary = Library(Glass("Pane", 1.0), Gas("Cavity", 0.024), Glass("Pane2", 1.0));
            Construction construction = Wall(("Pane", 0.02), ("Cavity", 0.05), ("Pane2", 0.02));

            Assert.That(TasQuery.AdjustableLayerIndex(construction, materialLibrary), Is.EqualTo(-1));
            Assert.That(TasCreate.LayerThicknessCalculationData(construction, materialLibrary).LayerIndex, Is.EqualTo(-1));
        }

        // =================================================================================================
        // The 10 mm rule and the secondary guards
        // =================================================================================================

        [Test]
        public void LayersUnderTenMillimetres_AreSkipped()
        {
            MaterialLibrary materialLibrary = Library(Opaque("Foil", 0.001), Opaque("Thin", 0.002), Opaque("Brick", 0.8));
            Construction construction = Wall(("Foil", 0.003), ("Thin", 0.009), ("Brick", 0.1));

            Assert.That(TasQuery.AdjustableLayerIndex(construction, materialLibrary), Is.EqualTo(2));
        }

        [Test]
        public void ExactlyTenMillimetres_IsAdjustable()
        {
            MaterialLibrary materialLibrary = Library(Opaque("Board", 0.05), Opaque("Brick", 0.8));
            Construction construction = Wall(("Board", 0.01), ("Brick", 0.1));

            Assert.That(TasQuery.AdjustableLayerIndex(construction, materialLibrary), Is.EqualTo(0));
        }

        [Test]
        public void TenMillimetresStoredAsFloat_IsAdjustable()
        {
            // TCD keeps widths as float: 0.01f reads back as 0.00999999977.
            Assert.That(TasQuery.IsAdjustableLayer(MaterialType.Opaque, 0.05, (double)0.01f), Is.True);
        }

        [Test]
        public void OnlyThinLayers_GivesNoAdjustableLayer()
        {
            MaterialLibrary materialLibrary = Library(Opaque("Foil", 0.001), Opaque("Thin", 0.002));
            Construction construction = Wall(("Foil", 0.003), ("Thin", 0.009));

            Assert.That(TasQuery.AdjustableLayerIndex(construction, materialLibrary), Is.EqualTo(-1));
        }

        [Test]
        public void NaNOrNonPositiveConductivity_IsNeverChosen()
        {
            Assert.That(TasQuery.IsAdjustableLayer(MaterialType.Opaque, double.NaN, 0.1), Is.False);
            Assert.That(TasQuery.IsAdjustableLayer(MaterialType.Opaque, 0, 0.1), Is.False);
            Assert.That(TasQuery.IsAdjustableLayer(MaterialType.Opaque, -0.5, 0.1), Is.False);
            Assert.That(TasQuery.IsAdjustableLayer(MaterialType.Opaque, 0.5, double.NaN), Is.False);
            Assert.That(TasQuery.IsAdjustableLayer(MaterialType.Opaque, 0.5, 0.1), Is.True);
        }

        [Test]
        public void UnknownMaterial_IsSkipped_AndTheIndexStaysAlignedWithTheConstructionLayers()
        {
            MaterialLibrary materialLibrary = Library(Opaque("Brick", 0.8));
            Construction construction = Wall(("NotInLibrary", 0.1), ("Brick", 0.1));

            Assert.That(TasQuery.AdjustableLayerIndex(construction, materialLibrary), Is.EqualTo(1));
        }

        [Test]
        public void LowestConductivityWins_AndTiesKeepTheFirstLayer()
        {
            MaterialLibrary materialLibrary = Library(Opaque("A", 0.5), Opaque("B", 0.04), Opaque("C", 0.04));
            Construction construction = Wall(("A", 0.1), ("B", 0.1), ("C", 0.1));

            Assert.That(TasQuery.AdjustableLayerIndex(construction, materialLibrary), Is.EqualTo(1));
        }

        [Test]
        public void NoLayersOrNoConstruction_GivesNoAdjustableLayer()
        {
            Assert.That(TasQuery.AdjustableLayerIndex((Construction)null, new MaterialLibrary("Test")), Is.EqualTo(-1));
            Assert.That(TasQuery.AdjustableLayerIndex(new Construction("Empty"), new MaterialLibrary("Test")), Is.EqualTo(-1));
            Assert.That(TasQuery.AdjustableLayerIndex(UserModelWall(), null), Is.EqualTo(-1));
            Assert.That(TasQuery.AdjustableLayerIndex((IEnumerable<Tuple<MaterialType, double, double>>)null), Is.EqualTo(-1));
        }

        // =================================================================================================
        // The TCD-side fallback shares the rule (it works on layer values, so no TCD COM object is needed)
        // =================================================================================================

        [Test]
        public void LayerListRule_UserModelWall_PicksMineralWool()
        {
            // Same wall as read back from TCD: type, conductivity, width per layer.
            List<Tuple<MaterialType, double, double>> layers = new List<Tuple<MaterialType, double, double>>
            {
                Tuple.Create(MaterialType.Gas, 0.024, (double)0.05f),
                Tuple.Create(MaterialType.Opaque, 0.23, (double)0.012f),
                Tuple.Create(MaterialType.Opaque, 0.025, (double)0.08f),
                Tuple.Create(MaterialType.Gas, 0.01622, (double)0.05f),
                Tuple.Create(MaterialType.Opaque, 0.2, (double)0.003f),
            };

            Assert.That(TasQuery.AdjustableLayerIndex(layers), Is.EqualTo(2));
        }

        [Test]
        public void LayerListRule_OnlyGasAndGlass_GivesNoAdjustableLayer()
        {
            List<Tuple<MaterialType, double, double>> layers = new List<Tuple<MaterialType, double, double>>
            {
                Tuple.Create(MaterialType.Transparent, 1.0, 0.006),
                Tuple.Create(MaterialType.Gas, 0.024, 0.016),
                Tuple.Create(MaterialType.Transparent, 1.0, 0.006),
            };

            Assert.That(TasQuery.AdjustableLayerIndex(layers), Is.EqualTo(-1));
        }
    }
}
