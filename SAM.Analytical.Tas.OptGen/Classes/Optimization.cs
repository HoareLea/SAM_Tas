using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{

    public class Optimization : OptGenObject
    {
        [Attributes.Name("Files")]
        public Files Files { get; set; }
    }
}
