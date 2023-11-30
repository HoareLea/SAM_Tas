namespace SAM.Analytical.Tas.GenOpt
{
    public abstract class Parameter : GenOptObject, IParameter
    {
        [Attributes.Name("Name")]
        public string Name { get; set; } = null;
    }
}
