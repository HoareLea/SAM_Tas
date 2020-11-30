namespace SAM.Core.Tas
{
    public static partial class Convert
    {
        public static bool ToT3D(string path_T3D, string path_gbXML, bool @override, bool fixNormals = true, bool zonesFromSpaces = true, bool useWidths = false)
        {
            bool result = false;

            using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
            {
                result = sAMT3DDocument.TogbXML(path_gbXML, @override, fixNormals, zonesFromSpaces);
                sAMT3DDocument.T3DDocument.SetUseBEWidths(useWidths);
                sAMT3DDocument.Save();
            }

            return result;
        }
    }
}
