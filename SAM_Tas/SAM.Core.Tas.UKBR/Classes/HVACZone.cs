using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class HVACZone : UKBRElement
    {
        public override string UKBRName => "Zone";

        public HVACZone(XElement xElement)
            : base(xElement)
        {

        }

        public string Name
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("Name"));
            }
        }

        public Guid GUID
        {
            get
            {
                return Query.Value<Guid>(xElement?.Attribute("GUID"));
            }
        }

        public int SystemIndex
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("System"));
            }
        }

        public int DHWGroupIndex
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("DHWGroup"));
            }
        }

        public double Area
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("Area"));
            }
        }

        public double Volume
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("Volume"));
            }
        }

        public bool ShowZone
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("ShowZone"));
            }
        }

        public double UpperLimit
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("UpperLimit"));
            }
        }

        public double LowerLimit
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("LowerLimit"));
            }
        }

        public List<int> HVACZoneGroupIndexes
        {
            get
            {
                IEnumerable<XElement> xElements = xElement?.Elements("ZoneGroups");
                if(xElements == null)
                {
                    return null;
                }

                int invalid = Query.Invalid<int>();

                List<int> result = new List<int>();
                foreach(XElement xElement in xElements)
                {
                    int index = Query.Value(xElement?.Attribute("Index"), invalid);
                    if(index == invalid)
                    {
                        continue;
                    }

                    result.Add(index);
                }

                return result;
            }
        }
    }
}
