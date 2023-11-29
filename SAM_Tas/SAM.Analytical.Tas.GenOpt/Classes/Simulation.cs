namespace SAM.Analytical.Tas.GenOpt
{
    [Attributes.Name("Simulation")]
    public class Simulation : GenOptObject
    {
        [Attributes.Name("Files")]
        public Files Files { get; set; }

        [Attributes.Name("ObjectiveFunctionLocation")]
        public ObjectiveFunctionLocation ObjectiveFunctionLocation { get; set; }
    }
}
