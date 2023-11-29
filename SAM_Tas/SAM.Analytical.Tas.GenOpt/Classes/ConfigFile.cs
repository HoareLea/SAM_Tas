namespace SAM.Analytical.Tas.GenOpt
{
    public class ConfigFile : GenOptFile
    {
        [Attributes.Name("Simulation")]
        public Simulation Simulation { get; set; }

        [Attributes.Name("Optimization")]
        public Optimization Optimization { get; set; }
    }
}
