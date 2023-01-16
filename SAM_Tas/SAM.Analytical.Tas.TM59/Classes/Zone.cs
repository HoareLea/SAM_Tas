using System;
using System.Xml;

namespace SAM.Analytical.Tas.TM59
{
    public class Zone
    {
        public string Name { get; }
        public Guid Guid { get; }
        public double Factor { get; }
        public RoomUse RoomUse { get; }
        public SystemType SystemType { get; }
        public bool Export { get; }
        public double WindSpeed { get; }

        public Zone(Guid guid, string name, double factor, RoomUse roomUse, SystemType systemType, bool export, double windSpeed)
        {
            Guid = guid;
            Name = name;
            Factor = factor;
            RoomUse = roomUse;
            SystemType = systemType;
            Export = export;
            WindSpeed = windSpeed;
        }

        public bool ToXml(XmlWriter xmlWriter)
        {
            if(xmlWriter == null)
            {
                return false;
            }

            xmlWriter.WriteStartElement("DomOverheatZoneItem");
            xmlWriter.WriteElementString("Name", Name == null ? string.Empty : Name);
            xmlWriter.WriteElementString("GUID", "{" + Guid.ToString().ToUpper() + "}");
            xmlWriter.WriteElementString("Factor", Factor.ToString());
            xmlWriter.WriteElementString("RoomUseInt", ((int)RoomUse).ToString());
            xmlWriter.WriteElementString("RoomUse", Core.Query.Description( RoomUse));
            xmlWriter.WriteElementString("SystemTypeInt", ((int)SystemType).ToString());
            xmlWriter.WriteElementString("SystemType", Core.Query.Description(SystemType));
            xmlWriter.WriteElementString("Export", Export.ToString().ToLower());
            xmlWriter.WriteElementString("WindSpeed", WindSpeed.ToString().ToLower());
            xmlWriter.WriteEndElement();

            return true;
        }
    }
}
