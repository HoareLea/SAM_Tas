using SAM.Core.Tas;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static ConstructionManager ToSAM_ConstructionManager(string path_TCD, double tolerance = Core.Tolerance.MacroDistance)
        {
            ConstructionManager result = null;
            using (SAMTCDDocument sAMTCDDocument = new SAMTCDDocument(path_TCD))
            {
                result = ToSAM(sAMTCDDocument, tolerance);
            }

            return result;
        }

        public static ConstructionManager ToSAM(this SAMTCDDocument sAMTCDDocument, double tolerance = Core.Tolerance.MacroDistance)
        {
            if(sAMTCDDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTCDDocument.Document, tolerance);

        }

        public static ConstructionManager ToSAM(this TCD.Document document, double tolerance = Core.Tolerance.MacroDistance)
        {
            if(document == null)
            {
                return null;
            }

            TCDDatabase tCDDatabase = new TCDDatabase(document);

            ConstructionManager result = new ConstructionManager();
            tCDDatabase.Update(result);

            //result.Update(document.materialRoot);
            //result.Update(document.constructionRoot, tolerance: tolerance);

            return result;
        }
    }
}
