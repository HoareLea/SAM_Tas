using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.material> Materials(this TBD.Construction construction)
        {
            List<TBD.material> result = new List<TBD.material>();

            int index = 1;
            TBD.material material = construction.materials(index);
            while (material != null)
            {
                result.Add(material);
                index++;

                material = construction. materials(index);
            }

            return result;
        }

        public static List<TCD.material> Materials(this TCD.Construction construction)
        {
            List<TCD.material> result = new List<TCD.material>();

            int index = 1;
            TCD.material material = construction.materials(index);
            while (material != null)
            {
                result.Add(material);
                index++;

                material = construction.materials(index);
            }

            return result;
        }
    }
}