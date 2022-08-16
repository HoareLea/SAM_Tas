using System;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class SourceSet : UKBRElement
    {
        public override string UKBRName => "SourceSet";

        public SourceSet(XElement xElement)
            : base(xElement)
        {

        }

        public Guid GUID
        {
            get
            {
                return Query.Value<Guid>(xElement?.Attribute("GUID"));
            }
        }

        public double AirPermeability
        {
            get
            {
                return Query.Value(xElement?.Attribute("AirPermeability"), Query.Invalid<double>());
            }
        }

        public double CIBSEBuildingTypeIndex
        {
            get
            {
                return Query.Value(xElement?.Attribute("CIBSEBuildingType"), Query.Invalid<int>());
            }
        }

        public double CIBSEBuildingSizeIndex
        {
            get
            {
                return Query.Value(xElement?.Attribute("CIBSEBuildingSize"), Query.Invalid<int>());
            }
        }

        public bool ReExport
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("ReExport"));
            }
        }

        public bool HasExternalZones
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("HasExternalZones"));
            }
        }

        public int ShadowStepSize
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("ShadowStepSize"));
            }
        }

        public bool bRemove
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("bRemove"));
            }
        }

        public double TotalVolume
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("TotalVolume"));
            }
        }

        public double RoofArea
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("RoofArea"));
            }
        }

        public int NumberOutputGroups
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("NumberOutputGroups"));
            }
        }

        public double ActualAreaExt
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("ActualAreaExt"));
            }
        }

        public double ActualWPerM2K
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("ActualWPerM2K"));
            }
        }

        public double ActualAlpha
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("ActualAlpha"));
            }
        }

        public double NotionalAreaExt
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("NotionalAreaExt"));
            }
        }

        public double NotionalWPerK
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("NotionalWPerK"));
            }
        }

        public double NotionalWPerM2K
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("NotionalWPerM2K"));
            }
        }

        public double NotionalAlpha
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("NotionalAlpha"));
            }
        }

        public double ReferenceAreaExt
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("ReferenceAreaExt"));
            }
        }

        public double ReferenceWPerK
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("ReferenceWPerK"));
            }
        }

        public double ReferenceWPerM2K
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("ReferenceWPerM2K"));
            }
        }

        public double ReferenceAlpha
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("ReferenceAlpha"));
            }
        }

        public Zones Zones
        {
            get
            {
                if(xElement == null)
                {
                    return null;
                }

                return new Zones(xElement);
            }
        }

        public BuildingElements BuildingElements
        {
            get
            {
                if (xElement == null)
                {
                    return null;
                }

                return new BuildingElements(xElement);
            }
        }
    }
}
