using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TBD.Construction Construction(this TBD.Building building, string name, bool caseSesitive = true, bool trim = false)
        {
            if (building == null || string.IsNullOrWhiteSpace(name))
                return null;

            string name_Temp = name;

            if (trim)
                name_Temp = name_Temp.Trim();

            if (!caseSesitive)
                name_Temp = name_Temp.ToUpper();

            int index = 0;
            TBD.Construction construction = building.GetConstruction(index);
            while (construction != null)
            {
                string name_Construction = construction.name;
                if (string.IsNullOrWhiteSpace(name_Construction))
                    continue;

                if (trim)
                    name_Construction = name_Construction.Trim();

                if (!caseSesitive)
                    name_Construction = name_Construction.ToUpper();

                if (name_Construction.Equals(name_Temp))
                    return construction;


                index++;

                construction = building.GetConstruction(index);
            }

            return null;
        }

        public static Construction Construction(this SurfaceData surfaceData, IEnumerable<Construction> constructions, bool caseSesitive = true, bool trim = false)
        {
            if (surfaceData == null || constructions == null)
                return null;

            string name_SurfaceData = surfaceData.BEName;
            if (string.IsNullOrEmpty(name_SurfaceData))
                return null;

            if (trim)
                name_SurfaceData = name_SurfaceData.Trim();

            if (!caseSesitive)
                name_SurfaceData = name_SurfaceData.ToUpper();


            foreach (Construction construction in constructions)
            {
                if (construction == null)
                    continue;

                string name_Construction = construction.Name;

                if (string.IsNullOrEmpty(name_Construction))
                    continue;

                if (trim)
                    name_Construction = name_Construction.Trim();

                if (!caseSesitive)
                    name_Construction = name_Construction.ToUpper();

                if (name_Construction.Equals(name_SurfaceData))
                    return construction;
            }

            return null;
        }
    
        public static Construction Construction(this LayerThicknessCalculationResult layerThicknessCalculationResult, ConstructionManager constructionManager)
        {
            if (layerThicknessCalculationResult == null || constructionManager == null)
            {
                return null;
            }

            return constructionManager.GetConstructions(layerThicknessCalculationResult.ConstructionName)?.FirstOrDefault();
        }
    }
}