namespace SAM.Analytical.Tas.OptGen
{
    [Attributes.Name("IO")]
    public class IO : OptGenObject
    {
        [Attributes.Name("NumberFormat")]
        public NumberFormat NumberFormat { get; set; } = NumberFormat.Double;
    }
}
