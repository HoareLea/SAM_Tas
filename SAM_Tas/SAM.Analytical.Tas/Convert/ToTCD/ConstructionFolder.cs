using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static TCD.ConstructionFolder ToTCD_ConstructionFolder(this Category category, TCD.Document document)
        {
            if (category == null || document == null)
            {
                return null;
            }

            if(category.Name == document.constructionRoot.name)
            {
                if(category.SubCategory == null)
                {
                    return document.constructionRoot;
                }
                
                return ToTCD_ConstructionFolder(category.SubCategory, document.constructionRoot);
            }
            else
            {
                return ToTCD_ConstructionFolder(category, document.constructionRoot);
            }
        }

        public static TCD.ConstructionFolder ToTCD_ConstructionFolder(this Category category, TCD.ConstructionFolder constructionFolder)
        {
            if(category == null || constructionFolder == null)
            {
                return null;
            }

            TCD.ConstructionFolder result = null;

            int index = 1;

            result = constructionFolder.childFolders(index);
            while (result != null)
            {
                if(result.name == category.Name)
                {
                    if (category.SubCategory != null)
                    {
                        return ToTCD_ConstructionFolder(category.SubCategory, result);
                    }

                    return result;
                }

                index++;
                result = constructionFolder.childFolders(index);
            }

            result = constructionFolder.AddChildFolder();
            result.name = category.Name;

            if(category.SubCategory != null)
            {
                return ToTCD_ConstructionFolder(category.SubCategory, result);
            }

            return result;
        }
    }
}
