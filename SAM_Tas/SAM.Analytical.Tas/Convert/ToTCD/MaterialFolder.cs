using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static TCD.MaterialFolder ToTCD_MaterialFolder(this Category category, TCD.Document document)
        {
            if (category == null || document == null)
            {
                return null;
            }

            if(category.Name == document.materialRoot.name)
            {
                if(category.SubCategory == null)
                {
                    return document.materialRoot;
                }
                
                return ToTCD_MaterialFolder(category.SubCategory, document.materialRoot);
            }
            else
            {
                return ToTCD_MaterialFolder(category, document.materialRoot);
            }
        }

        public static TCD.MaterialFolder ToTCD_MaterialFolder(this Category category, TCD.MaterialFolder materialFolder)
        {
            if(category == null || materialFolder == null)
            {
                return null;
            }

            TCD.MaterialFolder result = null;

            int index = 1;

            result = materialFolder.childFolders(index);
            while (result != null)
            {
                if(result.name == category.Name)
                {
                    if (category.SubCategory != null)
                    {
                        return ToTCD_MaterialFolder(category.SubCategory, result);
                    }

                    return result;
                }

                index++;
                result = materialFolder.childFolders(index);
            }

            result = materialFolder.AddChildFolder();
            result.name = category.Name;

            if(category.SubCategory != null)
            {
                return ToTCD_MaterialFolder(category.SubCategory, result);
            }

            return result;
        }
    }
}
