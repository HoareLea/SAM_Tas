using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class Building : UKBRElement
    {
        public override string UKBRName => "Building";

        public Building(XElement xElement)
            : base(xElement)
        {

        }

        public LightSetups LightSetups
        {
            get
            {
                return new LightSetups(xElement);
            }
        }

        public SourceSets SourceSets
        {
            get
            {
                return new SourceSets(xElement);
            }
        }

        public Zone GetZone(global::System.Guid guid, global::System.Guid? sourceSetGuid = null)
        {
            SourceSets sourceSets = SourceSets;
            if(sourceSets == null)
            {
                return null;
            }

            foreach(SourceSet sourceSet in sourceSets)
            {
                if (sourceSetGuid != null && sourceSetGuid.HasValue)
                {
                    if (sourceSet.GUID != sourceSetGuid)
                    {
                        continue;
                    }
                }

                Zones zones = sourceSet?.Zones;
                if(zones == null)
                {
                    continue; 
                }

                foreach(Zone zone in zones)
                {
                    if(zone.GUID == guid)
                    {
                        return zone;
                    }
                }
            }

            return null;
        }

        public List<Zone> GetZones(IEnumerable<global::System.Guid> guids, global::System.Guid? sourceSetGuid = null)
        {
            if(guids == null)
            {
                return null;
            }

            SourceSets sourceSets = SourceSets;
            if (sourceSets == null)
            {
                return null;
            }

            List<Zone> result = new List<Zone>();
            foreach (SourceSet sourceSet in sourceSets)
            {
                if(sourceSetGuid != null && sourceSetGuid.HasValue)
                {
                    if(sourceSet.GUID != sourceSetGuid)
                    {
                        continue;
                    }
                }

                Zones zones = sourceSet?.Zones;
                if (zones == null)
                {
                    continue;
                }

                foreach (Zone zone in zones)
                {
                    if (guids.Contains( zone.GUID))
                    {
                        result.Add(zone);
                    }
                }
            }

            return result;
        }

        public List<Zone> GetZones(ZoneGUIDs zoneGUIDs, global::System.Guid? sourceSetGuid = null)
        {
            List<global::System.Guid> guids = zoneGUIDs?.ToList().ConvertAll(x => x.GUID);
            if(guids == null)
            {
                return null;
            }

            return GetZones(guids, sourceSetGuid);
        }

        public List<BuildingElement> GetBuildingElements(IEnumerable<global::System.Guid> guids, global::System.Guid? sourceSetGuid = null)
        {
            if (guids == null)
            {
                return null;
            }

            SourceSets sourceSets = SourceSets;
            if (sourceSets == null)
            {
                return null;
            }

            List<BuildingElement> result = new List<BuildingElement>();
            foreach (SourceSet sourceSet in sourceSets)
            {
                if (sourceSetGuid != null && sourceSetGuid.HasValue)
                {
                    if (sourceSet.GUID != sourceSetGuid)
                    {
                        continue;
                    }
                }

                BuildingElements buildingElements = sourceSet?.BuildingElements;
                if (buildingElements == null)
                {
                    continue;
                }

                foreach (BuildingElement buildingElement in buildingElements)
                {
                    if (guids.Contains(buildingElement.GUID))
                    {
                        result.Add(buildingElement);
                    }
                }
            }

            return result;
        }

        public string NCMVersion
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("NCMVersion"));
            }
            set
            {
                Modify.SetValue(xElement, "NCMVersion", value);
            }

        }

        public double AirPermeability
        {
            get
            {
                return Query.Value(xElement?.Attribute("AirPermeability"), Query.Invalid<double>());
            }
            set
            {
                Modify.SetValue(xElement, "AirPermeability", value);
            }
        }

        public int CIBSEBuildingTypeIndex
        {
            get
            {
                return Query.Value(xElement?.Attribute("CIBSEBuildingType"), Query.Invalid<int>());
            }
            set
            {
                Modify.SetValue(xElement, "CIBSEBuildingType", value);
            }
        }

        public int CountryIndex
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("Country"));
            }
            set
            {
                Modify.SetValue(xElement, "Country", value);
            }
        }

        public int CIBSEBuildingSizeIndex
        {
            get
            {
                return Query.Value(xElement?.Attribute("CIBSEBuildingSize"), Query.Invalid<int>());
            }
            set
            {
                Modify.SetValue(xElement, "CIBSEBuildingSize", value);
            }
        }

        public bool ModelNatVentVia5ACH
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("ModelNatVentVia5ACH"));
            }
            set
            {
                Modify.SetValue(xElement, "ModelNatVentVia5ACH", value);
            }
        }

        public bool DoBuildingRegsCalcs
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("DoBuildingRegsCalcs"));
            }
            set
            {
                Modify.SetValue(xElement, "DoBuildingRegsCalcs", value);
            }
        }

        public bool DoEPCCalcs
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("DoEPCCalcs"));
            }
            set
            {
                Modify.SetValue(xElement, "DoEPCCalcs", value);
            }
        }

        public double AncillaryFanLoad
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("AncillaryFanLoad"));
            }
            set
            {
                Modify.SetValue(xElement, "AncillaryFanLoad", value);
            }
        }

        public double AncillaryPumpLoad
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("AncillaryPumpLoad"));
            }
            set
            {
                Modify.SetValue(xElement, "AncillaryPumpLoad", value);
            }
        }

        public double TotalVolume
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("TotalVolume"));
            }
            set
            {
                Modify.SetValue(xElement, "TotalVolume", value);
            }
        }

        public int WeatherFileIndex
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("WeatherFile"));
            }
            set
            {
                Modify.SetValue(xElement, "WeatherFile", value);
            }
        }

        public bool HVACMonitoring
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("HVACMonitoring"));
            }
            set
            {
                Modify.SetValue(xElement, "HVACMonitoring", value);
            }
        }

        public bool ShowCrit3
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("ShowCrit3"));
            }
            set
            {
                Modify.SetValue(xElement, "ShowCrit3", value);
            }
        }

        public int TPDxSourceIndex
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("TPDxSource"));
            }
            set
            {
                Modify.SetValue(xElement, "TPDxSource", value);
            }
        }

        public bool bSavePostImport
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("bSavePostImport"));
            }
            set
            {
                Modify.SetValue(xElement, "bSavePostImport", value);
            }
        }

        public double FoundationArea
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("FoundationArea"));
            }
            set
            {
                Modify.SetValue(xElement, "FoundationArea", value);
            }
        }

        public int NumberOfStoreys
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("NumberOfStoreys"));
            }
            set
            {
                Modify.SetValue(xElement, "NumberOfStoreys", value);
            }
        }

        public int FoundationAreaCalculationMethodIndex
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("FoundationAreaCalculationMethod"));
            }
            set
            {
                Modify.SetValue(xElement, "FoundationAreaCalculationMethod", value);
            }
        }

        public int LightSetupSource
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("LightSetupSource"));
            }
            set
            {
                Modify.SetValue(xElement, "LightSetupSource", value);
            }
        }
    }
}
