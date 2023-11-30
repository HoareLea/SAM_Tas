namespace SAM.Analytical.Tas.GenOpt
{
    [Attributes.Name("Parameter")]
    public class NumberParameter : Parameter
    {
        [Attributes.Name("Ini")]
        public double Initial { get; set; } = 0;

        [Attributes.Name("Min")]
        public double Min { get; set; } = 0;

        [Attributes.Name("Max")] 
        public double Max { get; set; } = 360;
        
        [Attributes.Name("Step")] 
        public double Step { get; set; } = 12;


    }
}
