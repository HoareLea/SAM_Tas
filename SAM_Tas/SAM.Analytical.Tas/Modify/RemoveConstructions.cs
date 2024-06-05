using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<string> RemoveConstructions(this TBD.Building building, IEnumerable<string> names = null)
        {
            if (building == null)
            {
                return null;
            }

            List<TBD.Construction> constructions = building.Constructions();
            if (constructions == null)
            {
                return null;
            }

            List<int> indexes = new List<int>();
            if(names == null)
            {
                for (int index = 0; index < constructions.Count; index++)
                {
                    indexes.Add(index);
                }
            }
            else
            {
                for (int index = 0; index < constructions.Count; index++)
                {
                    if (names.Contains(constructions[index].name))
                    {
                        indexes.Add(index);
                    }
                }
            }

            indexes.Reverse();

            List<string> result = new List<string>();

            foreach(int index_Temp in indexes)
            {
                TBD.Construction construction = building.GetConstruction(index_Temp);
                result.Add(construction.name);
                building.RemoveConstruction(index_Temp);
            }

            return result;
        }
    }
}