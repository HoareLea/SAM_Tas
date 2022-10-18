namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string Name(string uniqueName, bool includePrefix = true, bool includeName = true, bool includeGuid = true, bool includeId = true)
        {
            if (string.IsNullOrWhiteSpace(uniqueName))
            {
                return uniqueName;
            }

            if (!UniqueNameDecomposition(uniqueName, out string prefix, out string name, out System.Guid? guid, out int id))
            {
                return uniqueName;
            }

            if (!includePrefix)
            {
                prefix = null;
            }

            if (!includeName)
            {
                name = null;
            }

            if (!includeGuid)
            {
                guid = null;
            }

            if (!includeId)
            {
                id = -1;
            }

            string result = null;
            if (!string.IsNullOrWhiteSpace(prefix) && !name.StartsWith(prefix))
            {
                result = string.Format("{0}: {1}", prefix, name);
            }


            if (result == null)
            {
                result = name;
            }

            result.Trim();

            if (guid != null && guid.HasValue)
            {
                result += string.Format(" {0}", guid);
            }

            if (id != -1)
            {
                result += string.Format(" [{0}]", id);
            }

            if (result.EndsWith(":"))
            {
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }

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