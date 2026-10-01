// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// SAM_Tas PR3-0: the .tcd importer (<c>Convert.ToSAM(TCD.Document)</c>) was made linear. Its contract did not
    /// change, so these tests hold the production importer to the pre-PR importer - kept verbatim in
    /// <see cref="LegacyTcdDatabase"/> - over libraries built from managed TCD stand-ins: the converted
    /// ConstructionManager must serialise identically (Guids, which are random per object, removed), and the explicit
    /// expectations below pin the names, layer order, thicknesses and material properties directly so the parity check
    /// cannot pass by both sides being wrong in the same way.
    /// <para>
    /// The same comparison was run against the real databases (ASHRAE Constructions.tcd, Constructions.tcd, a slice of
    /// the International Glazing Database) through a licensed TCD.exe; the timings are recorded in the PR, not asserted.
    /// </para>
    /// </summary>
    [TestFixture]
    public class TcdImportParityTests
    {
        private const int Opaque = (int)TBD.MaterialTypes.tcdOpaqueMaterial;
        private const int OpaqueLayer = (int)TBD.MaterialTypes.tcdOpaqueLayer;
        private const int Transparent = (int)TBD.MaterialTypes.tcdTransparentLayer;
        private const int Gas = (int)TBD.MaterialTypes.tcdGasLayer;

        // ---- builders -------------------------------------------------------------------------------------------

        private static FakeTcdMaterial Material(string name, int type, float conductivity, float width = 0.1f, string description = "desc", float density = 1800.5f, float specificHeat = 840.25f, bool isBlind = false)
        {
            return new FakeTcdMaterial
            {
                name = name,
                description = description,
                width = width,
                externalSolarReflectance = 0.31f,
                internalSolarReflectance = 0.32f,
                solarTransmittance = type == Transparent ? 0.78f : 0f,
                externalEmissivity = 0.9f,
                internalEmissivity = 0.85f,
                lightTransmittance = type == Transparent ? 0.88f : 0f,
                externalLightReflectance = 0.11f,
                internalLightReflectance = 0.12f,
                isBlind = isBlind,
                conductivity = conductivity,
                density = density,
                specificHeat = specificHeat,
                vapourDiffusionFactor = 15f,
                convectionCoefficient = type == Gas ? 1.7f : 0f,
                type = type,
                dynamicViscosity = type == Gas ? 2.1e-5f : 0f,
            };
        }

        // TCD answers a new COM wrapper each time a material is asked for; the same material reached from another
        // construction therefore is a different object with equal values.
        private static FakeTcdMaterial Same(FakeTcdMaterial material)
        {
            return new FakeTcdMaterial
            {
                name = material.name,
                description = material.description,
                width = material.width,
                externalSolarReflectance = material.externalSolarReflectance,
                internalSolarReflectance = material.internalSolarReflectance,
                solarTransmittance = material.solarTransmittance,
                externalEmissivity = material.externalEmissivity,
                internalEmissivity = material.internalEmissivity,
                lightTransmittance = material.lightTransmittance,
                externalLightReflectance = material.externalLightReflectance,
                internalLightReflectance = material.internalLightReflectance,
                isBlind = material.isBlind,
                conductivity = material.conductivity,
                density = material.density,
                specificHeat = material.specificHeat,
                vapourDiffusionFactor = material.vapourDiffusionFactor,
                convectionCoefficient = material.convectionCoefficient,
                type = material.type,
                dynamicViscosity = material.dynamicViscosity,
            };
        }

        private static FakeTcdConstruction Construction(string name, string guid, string description = null, float additionalHeatTransfer = 0f, params (FakeTcdMaterial material, float width)[] layers)
        {
            FakeTcdConstruction construction = new FakeTcdConstruction { name_ = name, guid_ = guid, description_ = description, additionalHeatTransfer_ = additionalHeatTransfer };
            foreach ((FakeTcdMaterial material, float width) layer in layers)
            {
                construction.AddLayer(Same(layer.material), layer.width);
            }

            return construction;
        }

        private static string Guid(int index)
        {
            return string.Format("{{00000000-0000-4000-8000-{0:D12}}}", index);
        }

        /// <summary>
        /// A small library that exercises every naming / duplicate / conversion rule of the importer.
        /// </summary>
        private static FakeTcdDocument Representative()
        {
            FakeTcdMaterial brick = Material("Brick", Opaque, 0.77f, 0.215f);
            FakeTcdMaterial brickDense = Material("Brick", Opaque, 0.96f, 0.215f);                 // same name, other properties -> "Brick 2"
            FakeTcdMaterial insulation = Material("Insulation", OpaqueLayer, 0.035f, 0.1f, isBlind: true);
            FakeTcdMaterial plaster = Material("Plaster", Opaque, 0.4f, 0.013f);
            FakeTcdMaterial clear = Material("Clear 6mm", Transparent, 1f, 0.006f);
            FakeTcdMaterial argon = Material("Argon 12mm", Gas, 0.0162f, 0.012f);
            FakeTcdMaterial foo = Material("Foo", Opaque, 1.1f);
            FakeTcdMaterial foo2 = Material("Foo", Opaque, 1.2f);                                    // becomes "Foo 2" ...
            FakeTcdMaterial fooLiteral2 = Material("Foo 2", Opaque, 1.3f);                           // ... which this one is also called: the library keeps the first
            FakeTcdMaterial fooLower = Material("foo", Opaque, 1.4f);                                // names are case sensitive
            FakeTcdMaterial blank = Material(string.Empty, Opaque, 1.5f);
            FakeTcdMaterial unnamed = Material(null, Opaque, 1.6f);
            FakeTcdMaterial gypsum = Material("Gypsum", Opaque, 0.25f, 0.0125f);                    // only ever a construction layer

            FakeTcdMaterialFolder materialRoot = new FakeTcdMaterialFolder("Materials");
            FakeTcdMaterialFolder masonry = materialRoot.Add(new FakeTcdMaterialFolder("Masonry"));
            masonry.Add(brick);
            FakeTcdMaterialFolder dense = masonry.Add(new FakeTcdMaterialFolder("Dense"));
            dense.Add(brickDense);
            masonry.Add(Same(brick));                                                                // identical in every property: one material
            FakeTcdMaterialFolder glazing = materialRoot.Add(new FakeTcdMaterialFolder("Glazing"));
            glazing.Add(clear);
            glazing.Add(argon);
            materialRoot.Add(insulation);
            materialRoot.Add(foo);
            materialRoot.Add(foo2);
            materialRoot.Add(fooLiteral2);
            materialRoot.Add(fooLower);
            materialRoot.Add(blank);
            materialRoot.Add(unnamed);

            FakeTcdConstructionFolder constructionRoot = new FakeTcdConstructionFolder("Constructions");
            FakeTcdConstructionFolder external = constructionRoot.Add(new FakeTcdConstructionFolder("External"));
            external.Add(Construction("Wall A", Guid(1), "Cavity wall", 0.37f, (brick, 0.215f), (insulation, 0f), (plaster, 0.013f), (gypsum, 0f)));
            external.Add(Construction("Wall A", Guid(2), null, 0f, (brickDense, 0.1f), (plaster, 0.02f)));                 // "Wall A 2"
            FakeTcdConstructionFolder glazingSystems = external.Add(new FakeTcdConstructionFolder("Glazing"));
            glazingSystems.Add(Construction("Double", Guid(3), "Low-e", float.NaN, (clear, 0.006f), (argon, 0.012f), (clear, 0.006f)));
            glazingSystems.Add(Construction("Wall A", Guid(4), string.Empty, 0f, (brick, 0f), (plaster, 0f)));                // "Wall A 3"; both widths fall back to the material's
            constructionRoot.Add(Construction("Empty", Guid(5)));                                                           // no layers
            constructionRoot.Add(Construction("No guid", null, "skipped", 0f, (brick, 0.1f)));                              // TCD gave no GUID: not imported
            constructionRoot.Add(Construction("Names", Guid(6), "x", 0f, (foo, 0.1f), (foo2, 0.1f), (fooLiteral2, 0.1f), (fooLower, 0.1f), (blank, 0.1f), (unnamed, 0.1f)));
            return new FakeTcdDocument { materialRoot = materialRoot, constructionRoot = constructionRoot };
        }

        /// <summary>
        /// A larger pseudo-random library: names drawn from a pool smaller than the material count (so many names
        /// repeat with different properties), materials shared between constructions, layer-only materials, zero widths.
        /// </summary>
        private static FakeTcdDocument Generated(int materialCount, int constructionCount, int seed)
        {
            Random random = new Random(seed);
            int[] types = new[] { Opaque, OpaqueLayer, Transparent, Gas };

            List<FakeTcdMaterial> templates = new List<FakeTcdMaterial>();
            for (int i = 0; i < materialCount; i++)
            {
                string name = "Material " + random.Next(Math.Max(1, materialCount / 3));
                templates.Add(Material(name, types[random.Next(types.Length)], (float)Math.Round(random.NextDouble() * 2 + 0.01, 3), (float)Math.Round(random.NextDouble() * 0.3, 3), "d" + i % 7, 500 + random.Next(2000), 700 + random.Next(500), random.Next(5) == 0));
            }

            FakeTcdMaterialFolder materialRoot = new FakeTcdMaterialFolder("Materials");
            FakeTcdMaterialFolder current = materialRoot;
            for (int i = 0; i < templates.Count; i++)
            {
                if (i % 40 == 0)
                {
                    current = (i % 80 == 0 ? materialRoot : current).Add(new FakeTcdMaterialFolder("Folder " + i / 40));
                }

                if (random.Next(4) != 0)                                                                                    // a quarter are layer-only
                {
                    current.Add(Same(templates[i]));
                }
            }

            FakeTcdConstructionFolder constructionRoot = new FakeTcdConstructionFolder("Constructions");
            FakeTcdConstructionFolder folder = constructionRoot;
            for (int i = 0; i < constructionCount; i++)
            {
                if (i % 25 == 0)
                {
                    folder = constructionRoot.Add(new FakeTcdConstructionFolder("Group " + i / 25));
                }

                int layerCount = random.Next(0, 6);
                (FakeTcdMaterial, float)[] layers = new (FakeTcdMaterial, float)[layerCount];
                for (int j = 0; j < layerCount; j++)
                {
                    layers[j] = (templates[random.Next(templates.Count)], random.Next(3) == 0 ? 0f : (float)Math.Round(random.NextDouble() * 0.2, 3));
                }

                folder.Add(Construction("Construction " + random.Next(Math.Max(1, constructionCount / 2)), Guid(1000 + i), random.Next(3) == 0 ? "d" + i : null, random.Next(4) == 0 ? (float)random.NextDouble() : 0f, layers));
            }

            return new FakeTcdDocument { materialRoot = materialRoot, constructionRoot = constructionRoot };
        }

        // ---- the two importers ----------------------------------------------------------------------------------

        private static ConstructionManager ImportNew(FakeTcdDocument document)
        {
            return SAM.Analytical.Tas.Convert.ToSAM((TCD.Document)document);
        }

        private static ConstructionManager ImportLegacy(FakeTcdDocument document)
        {
            ConstructionManager result = new ConstructionManager();
            new LegacyTcdDatabase(document).Update(result);
            return result;
        }

        // The converted result as JSON with every Guid removed: Guids are random per object, everything else is
        // deterministic, so equal text means an identical conversion (names, order, thicknesses, properties, categories).
        private static string Signature(ConstructionManager constructionManager)
        {
            return Regex.Replace(constructionManager.ToJsonObject().ToJsonString(), "\"Guid\":\"[0-9a-fA-F-]{36}\",?", string.Empty);
        }

        // ---- parity ----------------------------------------------------------------------------------------------

        [Test]
        public void RepresentativeLibrary_ConvertsExactlyAsTheLegacyImporter()
        {
            ConstructionManager legacy = ImportLegacy(Representative());
            ConstructionManager current = ImportNew(Representative());

            Assert.That(current.Constructions.Count, Is.EqualTo(legacy.Constructions.Count).And.GreaterThan(0));
            Assert.That(current.MaterialLibrary.Count, Is.EqualTo(legacy.MaterialLibrary.Count).And.GreaterThan(0));
            Assert.That(Signature(current), Is.EqualTo(Signature(legacy)));
        }

        [TestCase(1, 12, 6)]
        [TestCase(2, 60, 25)]
        [TestCase(3, 150, 80)]
        [TestCase(4, 400, 120)]
        public void GeneratedLibraries_ConvertExactlyAsTheLegacyImporter(int seed, int materialCount, int constructionCount)
        {
            ConstructionManager legacy = ImportLegacy(Generated(materialCount, constructionCount, seed));
            ConstructionManager current = ImportNew(Generated(materialCount, constructionCount, seed));

            Assert.That(current.Constructions.Count, Is.EqualTo(legacy.Constructions.Count).And.GreaterThan(0));
            Assert.That(current.MaterialLibrary.Count, Is.EqualTo(legacy.MaterialLibrary.Count).And.GreaterThan(0));
            Assert.That(Signature(current), Is.EqualTo(Signature(legacy)));
        }

        [Test]
        public void ADuplicatedConstructionGuid_KeepsTheFirstPositionAndTheLaterObject_AsTheLegacyImporterDid()
        {
            FakeTcdDocument Build()
            {
                FakeTcdMaterial a = Material("A", Opaque, 0.5f);
                FakeTcdMaterial b = Material("B", Opaque, 0.6f);
                FakeTcdMaterial c = Material("C", Opaque, 0.7f);
                FakeTcdConstructionFolder root = new FakeTcdConstructionFolder("Constructions");
                FakeTcdConstructionFolder one = root.Add(new FakeTcdConstructionFolder("One"));
                FakeTcdConstructionFolder two = root.Add(new FakeTcdConstructionFolder("Two"));
                one.Add(Construction("First", Guid(1), "early", 0f, (a, 0.1f)));
                one.Add(Construction("Between", Guid(2), null, 0f, (c, 0.1f)));
                two.Add(Construction("Later", Guid(1), "late", 0.2f, (b, 0.2f)));                                        // same GUID, replaces "First"
                return new FakeTcdDocument { materialRoot = new FakeTcdMaterialFolder("Materials"), constructionRoot = root };
            }

            ConstructionManager legacy = ImportLegacy(Build());
            ConstructionManager current = ImportNew(Build());

            Assert.That(current.Constructions.Select(x => x.Name), Is.EqualTo(new[] { "Later", "Between" }));
            Assert.That(Signature(current), Is.EqualTo(Signature(legacy)));
        }

        [Test]
        public void NullAndEmptyDocuments_ConvertAsTheLegacyImporterDid()
        {
            Assert.That(SAM.Analytical.Tas.Convert.ToSAM((TCD.Document)null), Is.Null);

            FakeTcdDocument empty = new FakeTcdDocument { materialRoot = new FakeTcdMaterialFolder("Materials"), constructionRoot = new FakeTcdConstructionFolder("Constructions") };
            ConstructionManager current = ImportNew(empty);
            Assert.That(current, Is.Not.Null);
            Assert.That(current.Constructions?.Count ?? 0, Is.EqualTo(0));
            Assert.That(current.MaterialLibrary, Is.Null);

            Assert.That(Signature(current), Is.EqualTo(Signature(ImportLegacy(empty))));
        }

        // ---- the contract, pinned explicitly -----------------------------------------------------------------------

        [Test]
        public void ConstructionNames_DuplicatesGetTheirOrdinalAsASuffix_InLoadOrder()
        {
            ConstructionManager current = ImportNew(Representative());

            // load order: constructions walk their own folder before descending into child folders; the construction
            // without a GUID is never imported.
            Assert.That(current.Constructions.Select(x => x.Name), Is.EquivalentTo(new[] { "Wall A", "Wall A 2", "Double", "Wall A 3", "Empty", "Names" }));
        }

        [Test]
        public void LayersKeepTheirOrderThicknessAndTheMaterialsFallbackWidth()
        {
            ConstructionManager current = ImportNew(Representative());

            Construction wall = current.Constructions.Find(x => x.Name == "Wall A");
            Assert.That(wall.ConstructionLayers.Select(x => x.Name), Is.EqualTo(new[] { "Brick", "Insulation", "Plaster", "Gypsum" }));
            Assert.That(wall.ConstructionLayers[0].Thickness, Is.EqualTo((double)0.215f));
            Assert.That(wall.ConstructionLayers[1].Thickness, Is.EqualTo((double)0.1f), "width 0 in the construction: the material's own width");
            Assert.That(wall.ConstructionLayers[2].Thickness, Is.EqualTo((double)0.013f));
            Assert.That(wall.ConstructionLayers[3].Thickness, Is.EqualTo((double)0.0125f), "width 0 in the construction: the material's own width");

            Construction third = current.Constructions.Find(x => x.Name == "Wall A 3");
            Assert.That(third.ConstructionLayers.Select(x => x.Thickness), Is.EqualTo(new[] { (double)0.215f, (double)0.013f }));

            Construction second = current.Constructions.Find(x => x.Name == "Wall A 2");
            Assert.That(second.ConstructionLayers.Select(x => x.Name), Is.EqualTo(new[] { "Brick 2", "Plaster" }), "the same-named material with other properties is Brick 2");

            Assert.That(current.Constructions.Find(x => x.Name == "Empty").ConstructionLayers, Is.Empty);
        }

        [Test]
        public void RepeatedMaterialReferences_ResolveToOneLibraryMaterial()
        {
            ConstructionManager current = ImportNew(Representative());

            // "Brick" is reached from a folder (twice, identical), from three constructions; "Plaster" from three.
            Assert.That(current.MaterialLibrary.GetMaterials().Count(x => x.Name == "Brick"), Is.EqualTo(1));
            Assert.That(current.MaterialLibrary.GetMaterials().Count(x => x.Name == "Plaster"), Is.EqualTo(1));

            string[] names = current.MaterialLibrary.GetMaterials().Select(x => x.Name).ToArray();
            Assert.That(names, Is.EquivalentTo(new[]
            {
                "Brick", "Brick 2", "Insulation", "Plaster", "Clear 6mm", "Argon 12mm", "Gypsum",
                "Foo", "Foo 2", "foo", string.Empty, " 2",
            }.Distinct()));
        }

        [Test]
        public void NameCollisions_KeepTheFirstMaterialAdded()
        {
            ConstructionManager current = ImportNew(Representative());

            // the second "Foo" and the literal "Foo 2" both end up called "Foo 2": the library holds the first one
            // added (conductivity 1.2), as the lookup-before-add of the previous importer did.
            Assert.That(((SAM.Core.Material)current.MaterialLibrary.GetMaterial("Foo 2")).ThermalConductivity, Is.EqualTo((double)1.2f));
            Assert.That(((SAM.Core.Material)current.MaterialLibrary.GetMaterial("foo")).ThermalConductivity, Is.EqualTo((double)1.4f));
        }

        [Test]
        public void MaterialConversion_PerTypeProperties()
        {
            ConstructionManager current = ImportNew(Representative());
            SAM.Core.Material brick = (SAM.Core.Material)current.MaterialLibrary.GetMaterial("Brick");
            SAM.Core.Material insulation = (SAM.Core.Material)current.MaterialLibrary.GetMaterial("Insulation");
            SAM.Core.Material clear = (SAM.Core.Material)current.MaterialLibrary.GetMaterial("Clear 6mm");
            SAM.Core.Material argon = (SAM.Core.Material)current.MaterialLibrary.GetMaterial("Argon 12mm");

            Assert.That(brick, Is.TypeOf<OpaqueMaterial>());
            Assert.That(brick.ThermalConductivity, Is.EqualTo((double)0.77f));
            Assert.That(brick.Density, Is.EqualTo((double)1800.5f));
            Assert.That(brick.SpecificHeatCapacity, Is.EqualTo((double)840.25f));

            Assert.That(insulation, Is.TypeOf<OpaqueMaterial>());
            Assert.That(insulation.ThermalConductivity, Is.EqualTo((double)0.035f));

            Assert.That(clear, Is.TypeOf<TransparentMaterial>());
            Assert.That(clear.ThermalConductivity, Is.EqualTo(1d));

            Assert.That(argon, Is.TypeOf<GasMaterial>());
            Assert.That(argon.ThermalConductivity, Is.EqualTo((double)0.0162f));
            Assert.That(argon.TryGetValue(GasMaterialParameter.HeatTransferCoefficient, out double heatTransferCoefficient), Is.True);
            Assert.That(heatTransferCoefficient, Is.EqualTo((double)1.7f));
        }

        [Test]
        public void ConstructionDescriptionAndAdditionalHeatTransfer_AreSetOnlyWhenMeaningful()
        {
            ConstructionManager current = ImportNew(Representative());

            Construction wall = current.Constructions.Find(x => x.Name == "Wall A");
            Assert.That(wall.TryGetValue(Analytical.ConstructionParameter.Description, out string description), Is.True);
            Assert.That(description, Is.EqualTo("Cavity wall"));
            Assert.That(wall.TryGetValue(ConstructionParameter.AdditionalHeatTransfer, out double additionalHeatTransfer), Is.True);
            Assert.That(additionalHeatTransfer, Is.EqualTo((double)0.37f));

            Construction second = current.Constructions.Find(x => x.Name == "Wall A 2");
            Assert.That(second.TryGetValue(Analytical.ConstructionParameter.Description, out string _), Is.False, "null description");
            Assert.That(second.TryGetValue(ConstructionParameter.AdditionalHeatTransfer, out double _), Is.False, "0");

            Construction third = current.Constructions.Find(x => x.Name == "Wall A 3");
            Assert.That(third.TryGetValue(Analytical.ConstructionParameter.Description, out string _), Is.False, "empty description");

            Construction glazing = current.Constructions.Find(x => x.Name == "Double");
            Assert.That(glazing.TryGetValue(ConstructionParameter.AdditionalHeatTransfer, out double _), Is.False, "NaN");
        }

        // ---- cost ------------------------------------------------------------------------------------------------

        [Test]
        public void TcdIsAskedForEachMaterialOnce_NotOncePerNameLookup()
        {
            // Every property read against TCD.exe is a cross-process call. The previous importer re-read every
            // material's name for every unique name it computed, so its reads grew with the SQUARE of the library.
            FakeTcdDocument document = Generated(300, 100, 7);
            int materialsWalked = CountMaterials(document);

            FakeTcdMaterial.Reads = 0;
            TcdFakeCounter.Reset();
            ImportNew(document);
            int reads = FakeTcdMaterial.Reads;
            long totalReads = TcdFakeCounter.Reads;

            Assert.That(reads, Is.EqualTo(19 * materialsWalked), "19 properties, read once for each material object TCD hands out");

            FakeTcdMaterial.Reads = 0;
            TcdFakeCounter.Reset();
            ImportLegacy(document);
            long legacyTotalReads = TcdFakeCounter.Reads;

            Assert.That(legacyTotalReads, Is.GreaterThan(5 * totalReads), "the previous importer's COM traffic, for scale");
        }

        [Test]
        [CancelAfter(120000)]
        public void ALargeLibrary_ImportsInOnePassOverTheLibrary()
        {
            // Hang guard rather than a timing threshold: 6000 materials is ~36 million library clones on the previous
            // importer (minutes); the linear importer does it in well under a second. Nothing here asserts a duration.
            FakeTcdDocument document = Generated(6000, 400, 11);
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            ConstructionManager current = ImportNew(document);
            TestContext.WriteLine("import of 6000 materials / 400 constructions: {0} ms", stopwatch.ElapsedMilliseconds);

            Assert.That(current.Constructions.Count, Is.EqualTo(400));
            Assert.That(current.MaterialLibrary.Count, Is.GreaterThan(4000));

            // (one copy of the library: every MaterialLibrary lookup clones the whole library)
            HashSet<string> names = new HashSet<string>(current.MaterialLibrary.GetMaterials().Select(x => x.Name));
            Assert.That(current.Constructions.SelectMany(x => x.ConstructionLayers).All(x => names.Contains(x.Name)), Is.True);
        }

        private static int CountMaterials(FakeTcdDocument document)
        {
            return CountMaterials((TCD.MaterialFolder)document.materialRoot) + CountMaterials((TCD.ConstructionFolder)document.constructionRoot);
        }

        private static int CountMaterials(TCD.MaterialFolder folder)
        {
            int count = 0;
            while (folder.materials(count + 1) != null)
            {
                count++;
            }

            for (int i = 1; folder.childFolders(i) != null; i++)
            {
                count += CountMaterials(folder.childFolders(i));
            }

            return count;
        }

        private static int CountMaterials(TCD.ConstructionFolder folder)
        {
            int count = 0;
            for (int i = 1; folder.constructions(i) != null; i++)
            {
                TCD.Construction construction = folder.constructions(i);
                if (construction.GUID == null)
                {
                    continue;
                }

                count += CountLayers(construction);
            }

            for (int i = 1; folder.childFolders(i) != null; i++)
            {
                count += CountMaterials(folder.childFolders(i));
            }

            return count;
        }

        private static int CountLayers(TCD.Construction construction)
        {
            int count = 0;
            while (construction.materials(count + 1) != null)
            {
                count++;
            }

            return count;
        }
    }
}
