using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{
    public class CommandFile: IOptGenFile
    {
        [Attributes.Name("OptimizationSettings")]
        public OptimizationSettings OptimizationSettings { get; set; }

        [Attributes.Name("Algorithm")]
        public Algorithm Algorithm { get; set; }

        [Attributes.Name("Vary")]
        public List<Parameter> Parameters { get; set; }
    }
}
