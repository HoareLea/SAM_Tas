namespace SAM.Analytical.Tas.GenOpt
{
    [Attributes.Name("OptimizationSettings")]
    public class OptimizationSettings : GenOptObject
    {
        [Attributes.Name("MaxIte")]
        public int MaxIterations { get; set; } = 2000;

        [Attributes.Name("MaxEqualResults")]
        public int MaxEqualResults { get; set; } = 100;

        [Attributes.Name("WriteStepNumber")]
        public bool WriteStepNumber { get; set; } = false;
        
        [Attributes.Name("UnitsOfExecution")] 
        public int UnitsOfExecution { get; set; } = 0;
    }
}
