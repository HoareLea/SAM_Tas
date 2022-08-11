using System.IO;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string ResourcesDirectory()
        {
            return Path.Combine(Analytical.Query.ResourcesDirectory(), "Tas");
        }
    }
}