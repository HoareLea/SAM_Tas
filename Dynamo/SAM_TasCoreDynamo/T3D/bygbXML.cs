namespace SAM_TasCoreDynamo
{
    public static class T3D
    {
        public static bool BygbXML(string path_T3D, string path_gbXML, bool @override, bool fixNormals, bool zonesFromSpaces)
        {
            return SAM.Core.Tas.Convert.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces);
        }

    }
}
