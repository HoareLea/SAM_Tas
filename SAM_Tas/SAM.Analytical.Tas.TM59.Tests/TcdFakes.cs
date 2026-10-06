// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Managed stand-ins for the TCD (construction database) COM objects the .tcd importer walks, so the REAL
    /// <c>Convert.ToSAM(TCD.Document)</c> can run in a unit test without a TAS licence or a TCD.exe process.
    /// <para>
    /// <c>Interop.TCD</c> is referenced with <c>EmbedInteropTypes=false</c>, so <c>TCD.material</c>,
    /// <c>TCD.Construction</c>, the two folders and <c>TCD.Document</c> are plain interfaces. Like TCD, the
    /// collections are walked with 1-based indexers that answer <c>null</c> past the end (and
    /// <c>materialWidth</c>, a COM parameterised property, surfaces as <c>get_materialWidth(int)</c>).
    /// </para>
    /// <para>
    /// Every property getter and indexer counts its calls. Each of them is a cross-process call against a real
    /// TCD.exe, which is what made the importer slow, so the counts let a test assert that the importer asks TCD
    /// for each fact a bounded number of times instead of once per object pair.
    /// </para>
    /// </summary>
    internal static class TcdFakeCounter
    {
        public static long Reads;

        public static void Reset()
        {
            Reads = 0;
        }
    }

    internal class FakeTcdMaterial : TCD.material
    {
        private string name_;
        private string description_;
        private float width_;
        private float externalSolarReflectance_;
        private float internalSolarReflectance_;
        private float solarTransmittance_;
        private float externalEmissivity_;
        private float internalEmissivity_;
        private float lightTransmittance_;
        private float externalLightReflectance_;
        private float internalLightReflectance_;
        private bool isBlind_;
        private float conductivity_;
        private float density_;
        private float specificHeat_;
        private float vapourDiffusionFactor_;
        private float convectionCoefficient_;
        private int type_;
        private float dynamicViscosity_;

        public static int Reads;

        private T Read<T>(T value)
        {
            Reads++;
            TcdFakeCounter.Reads++;
            return value;
        }

        public string name { get { return Read(name_); } set { name_ = value; } }
        public string description { get { return Read(description_); } set { description_ = value; } }
        public float width { get { return Read(width_); } set { width_ = value; } }
        public float externalSolarReflectance { get { return Read(externalSolarReflectance_); } set { externalSolarReflectance_ = value; } }
        public float internalSolarReflectance { get { return Read(internalSolarReflectance_); } set { internalSolarReflectance_ = value; } }
        public float solarTransmittance { get { return Read(solarTransmittance_); } set { solarTransmittance_ = value; } }
        public float externalEmissivity { get { return Read(externalEmissivity_); } set { externalEmissivity_ = value; } }
        public float internalEmissivity { get { return Read(internalEmissivity_); } set { internalEmissivity_ = value; } }
        public float lightTransmittance { get { return Read(lightTransmittance_); } set { lightTransmittance_ = value; } }
        public float externalLightReflectance { get { return Read(externalLightReflectance_); } set { externalLightReflectance_ = value; } }
        public float internalLightReflectance { get { return Read(internalLightReflectance_); } set { internalLightReflectance_ = value; } }
        public bool isBlind { get { return Read(isBlind_); } set { isBlind_ = value; } }
        public float conductivity { get { return Read(conductivity_); } set { conductivity_ = value; } }
        public float density { get { return Read(density_); } set { density_ = value; } }
        public float specificHeat { get { return Read(specificHeat_); } set { specificHeat_ = value; } }
        public float vapourDiffusionFactor { get { return Read(vapourDiffusionFactor_); } set { vapourDiffusionFactor_ = value; } }
        public float convectionCoefficient { get { return Read(convectionCoefficient_); } set { convectionCoefficient_ = value; } }
        public int type { get { return Read(type_); } set { type_ = value; } }
        public float dynamicViscosity { get { return Read(dynamicViscosity_); } set { dynamicViscosity_ = value; } }

        public TCD.material DuplicateMaterial(TCD.material material)
        {
            throw new NotSupportedException();
        }
    }

    internal class FakeTcdConstruction : TCD.Construction
    {
        private readonly List<FakeTcdMaterial> layers = new List<FakeTcdMaterial>();
        private readonly List<float> widths = new List<float>();

        private T Read<T>(T value)
        {
            TcdFakeCounter.Reads++;
            return value;
        }

        public string name_;
        public string description_;
        public string guid_;
        public float additionalHeatTransfer_;

        public string name { get { return Read(name_); } set { name_ = value; } }
        public string description { get { return Read(description_); } set { description_ = value; } }
        public string GUID { get { return Read(guid_); } set { guid_ = value; } }
        public float additionalHeatTransfer { get { return Read(additionalHeatTransfer_); } set { additionalHeatTransfer_ = value; } }

        public float solarTransmittance { get; set; }
        public float externalSolarAbsorptanceExtSurf { get; set; }
        public float externalSolarAbsorptanceIntSurf { get; set; }
        public float internalSolarAbsorptanceIntSurf { get; set; }
        public float internalSolarAbsorptanceExtSurf { get; set; }
        public float lightTransmittance { get; set; }
        public float externalEmissivity { get; set; }
        public float internalEmissivity { get; set; }
        public float conductance { get; set; }
        public bool externalBlind { get; set; }
        public bool internalBlind { get; set; }
        public float timeConstant { get; set; }
        public TCD.ConstructionTypes type { get; set; }
        public float FFactor { get; set; }
        public float lightReflectance { get; set; }

        public void AddLayer(FakeTcdMaterial material, float width)
        {
            layers.Add(material);
            widths.Add(width);
        }

        // 1-based, null past the end - as TCD.
        public TCD.material materials(int index)
        {
            TcdFakeCounter.Reads++;
            return index >= 1 && index <= layers.Count ? layers[index - 1] : null;
        }

        public float get_materialWidth(int index)
        {
            TcdFakeCounter.Reads++;
            return widths[index - 1];
        }

        public void set_materialWidth(int index, float value)
        {
            widths[index - 1] = value;
        }

        public TCD.material AddMaterial() { throw new NotSupportedException(); }
        public object GetCondAnalysis(float tin, float tout, float rhin, float rhout, float rin, float rout, string bucketArray) { throw new NotSupportedException(); }
        public object GetUValue() { throw new NotSupportedException(); }
        public bool LinkMaterial(TCD.material material) { throw new NotSupportedException(); }
        public object GetGlazingValues() { throw new NotSupportedException(); }
        public void RemoveMaterial(int index) { throw new NotSupportedException(); }
        public int UValueSearch(int iFlowDir, float fTargetU, float fTargetG, TCD.material material, int iProperty, int iStandard) { throw new NotSupportedException(); }
        public float GetUValueISO(int iAngle) { throw new NotSupportedException(); }
    }

    internal class FakeTcdConstructionFolder : TCD.ConstructionFolder
    {
        private readonly List<FakeTcdConstruction> constructions_ = new List<FakeTcdConstruction>();
        private readonly List<FakeTcdConstructionFolder> folders_ = new List<FakeTcdConstructionFolder>();
        private string name_;

        public FakeTcdConstructionFolder(string name)
        {
            name_ = name;
        }

        public string name { get { TcdFakeCounter.Reads++; return name_; } set { name_ = value; } }
        public string description { get; set; }

        public FakeTcdConstruction Add(FakeTcdConstruction construction)
        {
            constructions_.Add(construction);
            return construction;
        }

        public FakeTcdConstructionFolder Add(FakeTcdConstructionFolder folder)
        {
            folders_.Add(folder);
            return folder;
        }

        public TCD.ConstructionFolder childFolders(int index)
        {
            TcdFakeCounter.Reads++;
            return index >= 1 && index <= folders_.Count ? folders_[index - 1] : null;
        }

        public TCD.Construction constructions(int index)
        {
            TcdFakeCounter.Reads++;
            return index >= 1 && index <= constructions_.Count ? constructions_[index - 1] : null;
        }

        public TCD.ConstructionFolder AddChildFolder() { throw new NotSupportedException(); }
        public TCD.Construction AddConstruction() { throw new NotSupportedException(); }
    }

    internal class FakeTcdMaterialFolder : TCD.MaterialFolder
    {
        private readonly List<FakeTcdMaterial> materials_ = new List<FakeTcdMaterial>();
        private readonly List<FakeTcdMaterialFolder> folders_ = new List<FakeTcdMaterialFolder>();
        private string name_;

        public FakeTcdMaterialFolder(string name)
        {
            name_ = name;
        }

        public string name { get { TcdFakeCounter.Reads++; return name_; } set { name_ = value; } }
        public string description { get; set; }

        public FakeTcdMaterial Add(FakeTcdMaterial material)
        {
            materials_.Add(material);
            return material;
        }

        public FakeTcdMaterialFolder Add(FakeTcdMaterialFolder folder)
        {
            folders_.Add(folder);
            return folder;
        }

        public TCD.MaterialFolder childFolders(int index)
        {
            TcdFakeCounter.Reads++;
            return index >= 1 && index <= folders_.Count ? folders_[index - 1] : null;
        }

        public TCD.material materials(int index)
        {
            TcdFakeCounter.Reads++;
            return index >= 1 && index <= materials_.Count ? materials_[index - 1] : null;
        }

        public TCD.MaterialFolder AddChildFolder() { throw new NotSupportedException(); }
        public TCD.material AddMaterial() { throw new NotSupportedException(); }
        public void RemoveMaterial(int index) { throw new NotSupportedException(); }
    }

    internal class FakeTcdDocument : TCD.Document
    {
        public TCD.MaterialFolder materialRoot { get; set; }
        public TCD.ConstructionFolder constructionRoot { get; set; }

        public bool create(string path) { throw new NotSupportedException(); }
        public bool open(string path) { throw new NotSupportedException(); }
        public bool openReadOnly(string path) { throw new NotSupportedException(); }
        public bool save() { throw new NotSupportedException(); }
        public bool close() { throw new NotSupportedException(); }
    }
}
