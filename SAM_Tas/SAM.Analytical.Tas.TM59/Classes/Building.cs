using System.Collections.Generic;
using System.Xml;

namespace SAM.Analytical.Tas.TM59
{
    public class Building
    {
        public List<Zone> Zones { get; }
        public bool ShowWeatherTable { get; }
        public bool DayByDayBreakdown { get; }
        public BuildingCategory BuildingCategory { get; }

        public Building(BuildingCategory buildingCategory, bool showWeatherTable, bool dayByDayBreakdown, IEnumerable<Zone> zones)
        {
            BuildingCategory = buildingCategory;
            ShowWeatherTable = showWeatherTable;
            DayByDayBreakdown = dayByDayBreakdown;
            Zones = zones != null ? new List<Zone>(zones) : null;
        }

        public bool ToXml(XmlWriter xmlWriter)
        {
            if (xmlWriter == null)
            {
                return false;
            }

            xmlWriter.WriteStartElement("DomOverheatXMLItem");

            xmlWriter.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
            xmlWriter.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

            xmlWriter.WriteStartElement("NonZoneElements");
            xmlWriter.WriteElementString("SelectedBuildingCatInt", ((int)BuildingCategory).ToString());
            xmlWriter.WriteElementString("SelectedBuildingCat", Core.Query.Description(BuildingCategory));
            xmlWriter.WriteElementString("ShowWeatherTable", ShowWeatherTable.ToString().ToLower());
            xmlWriter.WriteElementString("DayByDayBreakdown", DayByDayBreakdown.ToString().ToLower());
            xmlWriter.WriteEndElement();
            
            if (Zones != null)
            {
                foreach(Zone zone in Zones)
                {
                    if(zone == null)
                    {
                        continue;
                    }

                    zone.ToXml(xmlWriter);
                }
            }
            xmlWriter.WriteEndElement();

            return true;
        }
    }
}
