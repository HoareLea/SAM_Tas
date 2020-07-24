using SAM.Analytical;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string Name(this TAS3D.Element element)
        {
            if (element == null)
                return null;

            string name = element.name;
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Replace(" : ", ": ");
            if (name.EndsWith("-ground"))
                name = name.Substring(0, name.Length - 7);
            if (name.EndsWith("-air"))
                name = name.Substring(0, name.Length - 4);
            if (name.StartsWith("Curtain Basic"))
                name = name.Substring(8);
            if (name.StartsWith("Curtain Floor"))
                name = name.Substring(8);

            return name;
        }

        public static string Name(this TAS3D.window window)
        {
            if (window == null)
                return null;

            string name = window.name;
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return name;
        }
    }
}