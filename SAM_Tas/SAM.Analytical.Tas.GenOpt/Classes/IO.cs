namespace SAM.Analytical.Tas.GenOpt
{
    [Attributes.Name("IO")]
    public class IO : GenOptObject
    {
        [Attributes.Name("NumberFormat")]
        public NumberFormat NumberFormat { get; set; } = NumberFormat.Double;
    }
}
