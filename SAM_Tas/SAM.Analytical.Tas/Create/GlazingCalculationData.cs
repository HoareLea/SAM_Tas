using SAM.Core;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static GlazingCalculationData GlazingCalculationData(this ConstructionManager constructionManager)
        {
            if(constructionManager == null)
            {
                return null;
            }

            System.Guid guid = System.Guid.Empty;
            if(constructionManager != null)
            {
                SAMObject sAMObject = constructionManager.Constructions?.FirstOrDefault();
                if(sAMObject == null)
                {
                    sAMObject = constructionManager.ApertureConstructions?.FirstOrDefault();
                }

                if(sAMObject != null)
                {
                    guid = sAMObject.Guid;
                }
            }

            GlazingCalculationData result = new GlazingCalculationData()
            {
                ConstructionGuid = guid
            };

            return result;
        }
    }
}