using SAM.Analytical;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string Name(this TAS3D.Element element)
        {
            return Name(element?.name);
        }

        public static string Name(TBD.buildingElement buildingElement)
        {
            return Name(buildingElement?.name);
        }

        public static string Name(this TAS3D.window window)
        {
            if (window == null)
                return null;

            string name = window.name;
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Trim();

            return name;
        }


        private static string Name(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            string result = name.Trim();

            result = result.Replace(" : ", ": ");
            if (result.EndsWith("-ground"))
                result = result.Substring(0, result.Length - 7);
            if (result.EndsWith("-air"))
                result = result.Substring(0, result.Length - 4);
            if (result.StartsWith("Curtain Basic"))
                result = result.Substring(8);
            if (result.StartsWith("Curtain Floor"))
                result = result.Substring(8);

            return result;
        }
    }
}