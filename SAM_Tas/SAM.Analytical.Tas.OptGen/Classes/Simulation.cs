namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("Simulation")]
    public class Simulation : OptGenObject
    {
        [Attributes.Name("Files")]
        public Files Files { get; set; }

        [Attributes.Name("ObjectiveFunctionLocation")]
        public ObjectiveFunctionLocation ObjectiveFunctionLocation { get; set; }
    }
}
