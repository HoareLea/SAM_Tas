using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static bool UniqueNameDecomposition(string uniqueName, out string prefix, out string name, out System.Guid? guid, out int id)
        {
            if(!Analytical.Query.UniqueNameDecomposition(uniqueName, out prefix, out name, out guid, out id))
            {
                return false;
            }

            if(guid == null || !guid.HasValue)
            {
                List<string> values = name.Split(' ')?.ToList();
                if (values != null)
                {
                    if (global::System.Guid.TryParse(values.Last(), out System.Guid guid_Temp))
                    {
                        guid = guid_Temp;
                        values.RemoveAt(values.Count - 1);
                        name = string.Join(" ", values)?.Trim();
                    }
                    else if(values.Count > 2)
                    {
                        if (global::System.Guid.TryParse(values[values.Count - 2], out guid_Temp))
                        {
                            guid = guid_Temp;
                            values.RemoveAt(values.Count - 1);
                            values.RemoveAt(values.Count - 1);
                            name = string.Join(" ", values)?.Trim();
                        }
                    }
                }
            }

            return true;
        }
    }
}