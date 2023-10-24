using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static TCD.material ToTCD(this IMaterial material, TCD.Document tcdDocument)
        {
            if (tcdDocument == null || material == null)
            {
                return null;
            }

            return ToTCD(material, tcdDocument.materialRoot);
        }

        public static TCD.material ToTCD(this IMaterial material, TCD.MaterialFolder materialFolder)
        {
            if(material == null || materialFolder == null)
            {
                return null;
            }

            TCD.material result = materialFolder.AddMaterial();
            Modify.UpdateMaterial(result, material);

            return result;
        }
    }
}
