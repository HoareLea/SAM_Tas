using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Core.Tas
{
    public static class Import
    {
        public static bool ToT3D(string path_T3D, string path_gbXML, bool @override, bool fixNormals, bool zonesFromSpaces)
        {
            bool result = false;
            
            using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
            {
                result = sAMT3DDocument.ImportgbXML(path_gbXML, @override, fixNormals, zonesFromSpaces);
                sAMT3DDocument.Save();
            }

            return result;
        }
    }
}
