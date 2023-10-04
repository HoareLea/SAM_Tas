using SAM.Core.Tas;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static NCMNameCollection ToSAM(this SAMTICDocument sAMTICDocument)
        {
            if (sAMTICDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTICDocument.Document);
        }

        public static NCMNameCollection ToSAM(this TIC.Document document)
        {
            if(document == null)
            {
                return null;
            }

            TIC.InternalConditionFolder internalConditionFolder = document.internalConditionRoot;
            if (internalConditionFolder == null)
            {
                return null;
            }

            NCMNameCollection result = new NCMNameCollection();

            int index = 1;
            TIC.InternalConditionFolder internalConditionFolder_Child = internalConditionFolder.childFolders(index);
            while (internalConditionFolder_Child != null)
            {
                string group = internalConditionFolder_Child.name; 

                List<TIC.InternalCondition> internalConditions = internalConditionFolder_Child?.InternalConditions();
                if (internalConditions != null)
                {
                   foreach(TIC.InternalCondition internalCondition in internalConditions)
                    {
                        if(internalCondition == null)
                        {
                            continue;
                        }

                        string fullName = internalCondition.name;
                        if(string.IsNullOrWhiteSpace(fullName))
                        {
                            continue;
                        }

                        string description = internalCondition.description;

                        string version = fullName?.Split('_')?.Last();

                        string name = fullName;
                        if(name.StartsWith(group))
                        {
                            int length = group.Length;

                            if(name.Length > length)
                            {
                                length++;
                            }

                            name = name.Substring(length);
                        }

                        if(name.EndsWith(version))
                        {
                            int length = version.Length;

                            if (name.Length > length)
                            {
                                length++;
                            }

                            name = name.Substring(0, name.Length - length);
                        }
                        
                        NCMName nCMName = new NCMName(name, version, description, group);

                        result.Add(nCMName);
                    }
                }

                index++;
                internalConditionFolder_Child = internalConditionFolder.childFolders(index);
            }

            return result;
        }
    }
}
