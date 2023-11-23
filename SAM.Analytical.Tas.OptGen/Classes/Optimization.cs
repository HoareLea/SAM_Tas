using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("Optimization")]
    public class Optimization : OptGenObject
    {
        [Attributes.Name("Files")]
        public Files Files { get; set; }
    }
}
