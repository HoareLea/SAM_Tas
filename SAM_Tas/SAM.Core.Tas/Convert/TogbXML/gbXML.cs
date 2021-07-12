namespace SAM.Core.Tas
{
    public static partial class Convert
    {
        public static bool TogbXML(this SAMT3DDocument sAMT3DDocument, string path, bool @override, bool fixNormals, bool zonesFromSpaces)
        {
            if (sAMT3DDocument == null || string.IsNullOrWhiteSpace(path))
                return false;

            return TogbXML(sAMT3DDocument.T3DDocument, path, @override, fixNormals, zonesFromSpaces);
        }

        public static bool TogbXML(this TAS3D.T3DDocument t3DDocument, string path, bool @override, bool fixNormals, bool zonesFromSpaces)
        {
            if (t3DDocument == null || string.IsNullOrWhiteSpace(path))
                return false;
            
            int overrideInt = 0;
            if (@override)
                overrideInt = 1;

            int fixNormalsInt = 0;
            if (fixNormals)
                fixNormalsInt = 1;

            int zonesFromSpacesInt = 0;
            if (zonesFromSpaces)
                zonesFromSpacesInt = 1;

            return t3DDocument.ImportGBXML(path, overrideInt, fixNormalsInt, zonesFromSpacesInt);
        }

    }
}
