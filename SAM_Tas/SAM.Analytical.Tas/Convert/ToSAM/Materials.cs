using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<Core.IMaterial> ToSAM_Materials(this TCD.Construction construction)
        {
            if(construction == null)
            {
                return null;
            }

            List<TCD.material> materials = construction.Materials();

            List<Core.IMaterial> result = null;
            if (materials != null)
            {
                result = new List<Core.IMaterial>();
                for (int i = 0; i < materials.Count; i++)
                {
                    Core.IMaterial material = materials[i]?.ToSAM();
                    if (material == null)
                    {
                        continue;
                    }
                    result.Add(material);
                }
            }

            return result;
        }
    }
}
