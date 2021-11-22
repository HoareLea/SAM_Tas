using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Core.MaterialLibrary ToSAM_MaterialLibrary(this TBD.Building building)
        {
            List<TBD.Construction> constructions = building?.Constructions();
            if(constructions == null)
            {
                return null;
            }

            Core.MaterialLibrary result = new Core.MaterialLibrary(building.name);
            foreach(TBD.Construction construction in constructions)
            {
                if(construction == null)
                {
                    continue;
                }

                List<TBD.material> materials_TBD = construction.Materials();
                if(materials_TBD == null || materials_TBD.Count == 0)
                {
                    continue;
                }

                foreach(TBD.material material_TBD in materials_TBD)
                {
                    if(material_TBD == null)
                    {
                        continue;
                    }

                    if(result.GetMaterial(material_TBD.name) != null)
                    {
                        continue;
                    }

                    Core.IMaterial material = material_TBD.ToSAM();
                    if(material == null)
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
