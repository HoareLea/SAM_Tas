namespace SAM.Analytical.Tas.OptGen
{
    public class ConfigFile : OptGenFile
    {
        [Attributes.Name("Simulation")]
        public Simulation Simulation { get; set; }

        [Attributes.Name("Optimization")]
        public Optimization Optimization { get; set; }
    }
}
