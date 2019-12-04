using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM_TasCoreDynamo
{
    public static class T3DDocument
    {
        public static bool  ToT3D(string path_T3D, string path_gbXML, bool @override = true, bool fixNormals = true, bool zonesFromSpaces = true)
        {
            return SAM.Core.Tas.Import.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces);
        }
    }
}
