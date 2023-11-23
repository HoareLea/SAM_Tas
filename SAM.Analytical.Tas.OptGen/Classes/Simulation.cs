using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("Simulation")]
    public class Simulation : OptGenObject
    {
        [Attributes.Name("Files")]
        public Files Files { get; set; }
    }
}
