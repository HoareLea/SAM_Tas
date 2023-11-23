using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("Parameter")]
    public class Parameter
    {
        [Attributes.Name("Name")]
        public string Name { get; set; } = "NorthAngle";
        
        [Attributes.Name("Min")]
        public double Min { get; set; } = 0;

        [Attributes.Name("Int")]
        public double Initial { get; set; } = 0;
        
        [Attributes.Name("Max")] 
        public double Max { get; set; } = 360;
        
        [Attributes.Name("Step")] 
        public double Step { get; set; } = 12;
    }
}
