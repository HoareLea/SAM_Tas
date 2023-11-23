namespace SAM.Analytical.Tas.OptGen
{
    public class OptimizationSettings : OptGenObject
    {
        [Attributes.Name("MaxIter")]
        public int MaxIterations { get; set; } = 2000;

        [Attributes.Name("MaxEqualResults")]
        public int MaxEqualResults { get; set; } = 100;

        [Attributes.Name("WriteStepNumber")]
        public bool WriteStepNumber { get; set; } = false;
        
        [Attributes.Name("UnitsOfExecution")] 
        public int UnitsOfExecution { get; set; } = 0;
    }
}
