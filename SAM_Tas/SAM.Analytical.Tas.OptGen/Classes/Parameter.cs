namespace SAM.Analytical.Tas.OptGen
{
    public abstract class Parameter : OptGenObject, IParameter
    {
        [Attributes.Name("Name")]
        public string Name { get; set; } = null;
    }
}
