using SAM.Analytical.Tas.OptGen.Interfaces;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{
    public class File : OptGenObject
    {
       public FileType FileType { get; set; }

       public List<string> Names { get; set; }
    }
}
