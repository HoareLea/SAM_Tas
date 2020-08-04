using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.Construction> Constructions(this TBD.Building building)
        {
            List<TBD.Construction> result = new List<TBD.Construction>();

            int index = 0;
            TBD.Construction construction = building.GetConstruction(index);
            while (construction != null)
            {
                result.Add(construction);
                index++;

                construction = building.GetConstruction(index);
            }

            return result;
        }
    }
}