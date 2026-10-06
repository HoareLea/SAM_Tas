// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Managed stand-ins for the TPD COM objects the zone-load enumeration touches, in the same spirit as
    /// <see cref="FakeProfile"/> and friends in <c>TasProfileFakes.cs</c>.
    /// <para>
    /// <c>TPD.SystemComponent</c> and <c>TPD.ZoneLoad</c> are ordinary interfaces in <c>Interop.TPD</c>, so a
    /// plain C# class can implement them. Nothing here instantiates a coclass: no TAS licence, no TAS install
    /// and no COM server is involved. What that buys is that the REAL production
    /// <c>SAM.Analytical.Tas.TPD.Query.ZoneLoads</c> runs inside a unit test rather than being mirrored by hand.
    /// </para>
    /// <para>
    /// <b>Why the production call is made by reflection.</b> <c>SAM.Analytical.Tas.TPD</c> references
    /// <c>Interop.TPD</c> with <c>EmbedInteropTypes=True</c>, so <c>Query.ZoneLoads</c> returns
    /// <c>List&lt;ZoneLoad&gt;</c> over an <i>embedded</i> interop type. C# refuses that across an assembly
    /// boundary - <b>CS1769</b>, "cannot be used across assembly boundaries because it has a generic type
    /// argument that is an embedded interop type" - so the method cannot be named directly from here. It is
    /// invoked through <see cref="TpdReflection"/> instead, which the CLR resolves by COM type equivalence.
    /// That keeps the production embedding exactly as it is: no production build setting is changed to make a
    /// test compile.
    /// </para>
    /// <para>
    /// Every member the enumeration does not use throws, so a test that silently starts depending on more of
    /// the COM surface fails loudly instead of quietly reading a default.
    /// </para>
    /// </summary>
    public class FakeZoneLoad : global::TPD.ZoneLoad
    {
        public string Description { get; set; }
        public double FloorArea { get; set; }
        public string GUID { get; set; }
        public string Name { get; set; }
        public double Volume { get; set; }

        public int GetHasDHWLoad() => throw new global::System.NotSupportedException("ZoneLoad.GetHasDHWLoad");
        public global::TPD.PlantComponent GetPlantComponent(int index) => throw new global::System.NotSupportedException("ZoneLoad.GetPlantComponent");
        public int GetPlantComponentCount() => throw new global::System.NotSupportedException("ZoneLoad.GetPlantComponentCount");
        public global::TPD.SurfaceLoad GetSurface(int index) => throw new global::System.NotSupportedException("ZoneLoad.GetSurface");
        public int GetSurfaceCount() => throw new global::System.NotSupportedException("ZoneLoad.GetSurfaceCount");
        public global::TPD.SystemComponent GetSystemComponent(int index) => throw new global::System.NotSupportedException("ZoneLoad.GetSystemComponent");
        public int GetSystemComponentCount() => throw new global::System.NotSupportedException("ZoneLoad.GetSystemComponentCount");
        public global::TPD.TSDData GetTSDData() => throw new global::System.NotSupportedException("ZoneLoad.GetTSDData");
        public global::TPD.ZoneLoadGroup GetZoneLoadGroup(object index) => throw new global::System.NotSupportedException("ZoneLoad.GetZoneLoadGroup");
        public int GetZoneLoadGroupCount() => throw new global::System.NotSupportedException("ZoneLoad.GetZoneLoadGroupCount");

    }

    /// <summary>
    /// A <c>TPD.SystemComponent</c> carrying an explicit, ordered list of zone loads.
    /// <para>
    /// <c>GetZoneLoad</c> is <b>1-based</b>, matching TAS and matching the two already-correct
    /// <c>TSDData</c> overloads of <c>Query.ZoneLoads</c>. An out-of-range index returns <c>null</c> rather
    /// than throwing, which is what a COM accessor does and what lets the null-skip behaviour be tested.
    /// </para>
    /// </summary>
    public class FakeSystemComponent : global::TPD.SystemComponent
    {
        private readonly global::System.Collections.Generic.List<global::TPD.ZoneLoad> zoneLoads
            = new global::System.Collections.Generic.List<global::TPD.ZoneLoad>();

        /// <summary>Appends a zone load. Order is the enumeration order TAS would report.</summary>
        public void Add(global::TPD.ZoneLoad zoneLoad)
        {
            zoneLoads.Add(zoneLoad);
        }

        /// <summary>How many times <see cref="GetZoneLoad"/> has been called, per 1-based index.</summary>
        public global::System.Collections.Generic.List<int> RequestedIndexes { get; }
            = new global::System.Collections.Generic.List<int>();

        public int GetZoneLoadCount()
        {
            return zoneLoads.Count;
        }

        public global::TPD.ZoneLoad GetZoneLoad(int index)
        {
            RequestedIndexes.Add(index);

            if (index < 1 || index > zoneLoads.Count)
            {
                return null;
            }

            return zoneLoads[index - 1];
        }

        public string Description { get; set; }
        public string GUID { get; set; }
        public global::TPD.tpdManufacturerIcon ManufacturerLogo { get; set; }
        public string ManufacturerName { get; set; }
        public string Name { get; set; }
        public string ProductName { get; set; }

        public global::TPD.SystemLabel AddLabel(string titleString) => throw new global::System.NotSupportedException("SystemComponent.AddLabel");
        public void AddZoneLoad(global::TPD.ZoneLoad zone) => throw new global::System.NotSupportedException("SystemComponent.AddZoneLoad");
        public void DeleteAllControllers() => throw new global::System.NotSupportedException("SystemComponent.DeleteAllControllers");
        public int GetBaseHeight() => throw new global::System.NotSupportedException("SystemComponent.GetBaseHeight");
        public int GetBaseWidth() => throw new global::System.NotSupportedException("SystemComponent.GetBaseWidth");
        public string GetComponentType() => throw new global::System.NotSupportedException("SystemComponent.GetComponentType");
        public global::TPD.ControlArc GetControlArc(int port, int index) => throw new global::System.NotSupportedException("SystemComponent.GetControlArc");
        public int GetControlArcCount(int port) => throw new global::System.NotSupportedException("SystemComponent.GetControlArcCount");
        public int GetControlPortCount() => throw new global::System.NotSupportedException("SystemComponent.GetControlPortCount");
        public global::TPD.CoolingGroup GetCoolingGroup() => throw new global::System.NotSupportedException("SystemComponent.GetCoolingGroup");
        public global::TPD.DHWGroup GetDHWGroup() => throw new global::System.NotSupportedException("SystemComponent.GetDHWGroup");
        public global::TPD.tpdDirection GetDirection() => throw new global::System.NotSupportedException("SystemComponent.GetDirection");
        public string GetDynamicName() => throw new global::System.NotSupportedException("SystemComponent.GetDynamicName");
        public global::TPD.ElectricalGroup GetElectricalGroup1() => throw new global::System.NotSupportedException("SystemComponent.GetElectricalGroup1");
        public global::TPD.ElectricalGroup GetElectricalGroup2() => throw new global::System.NotSupportedException("SystemComponent.GetElectricalGroup2");
        public global::TPD.FuelGroup GetFuelGroup() => throw new global::System.NotSupportedException("SystemComponent.GetFuelGroup");
        public string GetFullName() => throw new global::System.NotSupportedException("SystemComponent.GetFullName");
        public global::TPD.ComponentGroup GetGroup() => throw new global::System.NotSupportedException("SystemComponent.GetGroup");
        public global::TPD.HeatingGroup GetHeatingGroup() => throw new global::System.NotSupportedException("SystemComponent.GetHeatingGroup");
        public global::TPD.Duct GetInputDuct(int port, int index) => throw new global::System.NotSupportedException("SystemComponent.GetInputDuct");
        public int GetInputDuctCount(int port) => throw new global::System.NotSupportedException("SystemComponent.GetInputDuctCount");
        public int GetInputPortCount() => throw new global::System.NotSupportedException("SystemComponent.GetInputPortCount");
        public global::TPD.Duct GetOutputDuct(int port, int index) => throw new global::System.NotSupportedException("SystemComponent.GetOutputDuct");
        public int GetOutputDuctCount(int port) => throw new global::System.NotSupportedException("SystemComponent.GetOutputDuctCount");
        public int GetOutputPortCount() => throw new global::System.NotSupportedException("SystemComponent.GetOutputPortCount");
        public global::TPD.TasPosition GetPosition() => throw new global::System.NotSupportedException("SystemComponent.GetPosition");
        public global::TPD.RefrigerantGroup GetRefrigerantGroup() => throw new global::System.NotSupportedException("SystemComponent.GetRefrigerantGroup");
        public object GetResultsData(global::TPD.tpdResultsPeriod period, global::TPD.tpdCombinerType combiner, int seriesIndex, int start, int length) => throw new global::System.NotSupportedException("SystemComponent.GetResultsData");
        public global::TPD.PlantSchedule GetSchedule() => throw new global::System.NotSupportedException("SystemComponent.GetSchedule");
        public global::TPD.SensorArc GetSensorArc(int port, int index) => throw new global::System.NotSupportedException("SystemComponent.GetSensorArc");
        public int GetSensorArcCount(int port) => throw new global::System.NotSupportedException("SystemComponent.GetSensorArcCount");
        public int GetSensorPortCount() => throw new global::System.NotSupportedException("SystemComponent.GetSensorPortCount");
        public global::TPD.SteamGroup GetSteamGroup() => throw new global::System.NotSupportedException("SystemComponent.GetSteamGroup");
        public global::TPD.System GetSystem() => throw new global::System.NotSupportedException("SystemComponent.GetSystem");
        public void RemoveCoolingGroup() => throw new global::System.NotSupportedException("SystemComponent.RemoveCoolingGroup");
        public void RemoveDHWGroup() => throw new global::System.NotSupportedException("SystemComponent.RemoveDHWGroup");
        public void RemoveElectricalGroup1() => throw new global::System.NotSupportedException("SystemComponent.RemoveElectricalGroup1");
        public void RemoveElectricalGroup2() => throw new global::System.NotSupportedException("SystemComponent.RemoveElectricalGroup2");
        public void RemoveFuelGroup() => throw new global::System.NotSupportedException("SystemComponent.RemoveFuelGroup");
        public void RemoveHeatingGroup() => throw new global::System.NotSupportedException("SystemComponent.RemoveHeatingGroup");
        public void RemoveRefrigerantGroup() => throw new global::System.NotSupportedException("SystemComponent.RemoveRefrigerantGroup");
        public void RemoveSchedule() => throw new global::System.NotSupportedException("SystemComponent.RemoveSchedule");
        public void RemoveSteamGroup() => throw new global::System.NotSupportedException("SystemComponent.RemoveSteamGroup");
        public void RemoveZoneLoad(global::TPD.ZoneLoad zone) => throw new global::System.NotSupportedException("SystemComponent.RemoveZoneLoad");
        public void SetCoolingGroup(global::TPD.CoolingGroup group) => throw new global::System.NotSupportedException("SystemComponent.SetCoolingGroup");
        public void SetDHWGroup(global::TPD.DHWGroup group) => throw new global::System.NotSupportedException("SystemComponent.SetDHWGroup");
        public void SetDirection(global::TPD.tpdDirection direction) => throw new global::System.NotSupportedException("SystemComponent.SetDirection");
        public void SetElectricalGroup1(global::TPD.ElectricalGroup group) => throw new global::System.NotSupportedException("SystemComponent.SetElectricalGroup1");
        public void SetElectricalGroup2(global::TPD.ElectricalGroup group) => throw new global::System.NotSupportedException("SystemComponent.SetElectricalGroup2");
        public void SetFuelGroup(global::TPD.FuelGroup group) => throw new global::System.NotSupportedException("SystemComponent.SetFuelGroup");
        public void SetHeatingGroup(global::TPD.HeatingGroup group) => throw new global::System.NotSupportedException("SystemComponent.SetHeatingGroup");
        public void SetPosition(int x, int y) => throw new global::System.NotSupportedException("SystemComponent.SetPosition");
        public void SetRefrigerantGroup(global::TPD.RefrigerantGroup group) => throw new global::System.NotSupportedException("SystemComponent.SetRefrigerantGroup");
        public void SetSchedule(global::TPD.PlantSchedule Schedule) => throw new global::System.NotSupportedException("SystemComponent.SetSchedule");
        public void SetSteamGroup(global::TPD.SteamGroup group) => throw new global::System.NotSupportedException("SystemComponent.SetSteamGroup");

    }
}
