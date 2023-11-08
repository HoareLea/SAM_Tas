using SAM.Core.Tas;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static ConstructionManager ToSAM_ConstructionManager(string path_TCD)
        {
            ConstructionManager result = null;
            using (SAMTCDDocument sAMTCDDocument = new SAMTCDDocument(path_TCD))
            {
                result = ToSAM(sAMTCDDocument);
            }

            return result;
        }

        public static ConstructionManager ToSAM(this SAMTCDDocument sAMTCDDocument)
        {
            if(sAMTCDDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTCDDocument.Document);

        }

        public static ConstructionManager ToSAM(this TCD.Document document)
        {
            if(document == null)
            {
                return null;
            }

            ConstructionManager result = new ConstructionManager();

            result.Update(document.constructionRoot);
            result.Update(document.materialRoot);

            return result;
        }
    }
}
