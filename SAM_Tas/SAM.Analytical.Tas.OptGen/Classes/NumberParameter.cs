namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("Parameter")]
    public class NumberParameter : Parameter
    {
        [Attributes.Name("Int")]
        public double Initial { get; set; } = 0;

        [Attributes.Name("Min")]
        public double Min { get; set; } = 0;

        [Attributes.Name("Max")] 
        public double Max { get; set; } = 360;
        
        [Attributes.Name("Step")] 
        public double Step { get; set; } = 12;


    }
}
