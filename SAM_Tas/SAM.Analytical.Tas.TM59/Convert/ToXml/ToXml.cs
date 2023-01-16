using System.Xml;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Convert
    {
        public static bool ToXml(this Building building, string path)
        {
            if (building == null || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                OmitXmlDeclaration = false,
                Indent = true,
            };

            using (XmlWriter xmlWriter = XmlWriter.Create(path, xmlWriterSettings))
            {
                xmlWriter.WriteStartDocument();
                building.ToXml(xmlWriter);
                xmlWriter.WriteEndDocument();
                xmlWriter.Flush();
            }

            return true;
        }

        public static bool ToXml(this AnalyticalModel analyticalModel, string path, TM59Manager tM59Manager = null)
        {
            if(tM59Manager == null)
            {
                tM59Manager = new TM59Manager();
            }

            Building builidng = analyticalModel.ToTM59(tM59Manager);
            if(builidng == null)
            {
                return false;
            }

            return ToXml(builidng, path);
        }
    }
}
