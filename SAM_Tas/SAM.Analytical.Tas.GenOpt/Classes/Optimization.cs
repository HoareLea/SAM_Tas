namespace SAM.Analytical.Tas.GenOpt
{
    [Attributes.Name("Optimization")]
    public class Optimization : GenOptObject
    {
        [Attributes.Name("Files")]
        public Files Files { get; set; }
    }
}
