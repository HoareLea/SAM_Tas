namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("SimulationError")]
    public class SimulationError : OptGenObject
    {
        [Attributes.Name("ErrorMessage")]
        public string ErrorMessage { get; set; } = "Error";
    }
}
