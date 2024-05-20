using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> RemoveUnusedElements(this SAMT3DDocument sAMT3DDocument)
        {
            if (sAMT3DDocument == null)
                return null;

            return RemoveUnusedElements(sAMT3DDocument.T3DDocument);
        }

        public static List<Guid> RemoveUnusedElements(this TAS3D.Building building, IEnumerable<string> excludedNames = null)
        {
            List<TAS3D.Element> elements = building?.Elements();
            if (elements == null)
                return null;

            List<TAS3D.Element> elements_ToRemove = elements.FindAll(x => x.isUsed == 0);
            if(excludedNames != null && excludedNames.Count() != 0)
            {
                for(int i = elements_ToRemove.Count - 1; i >= 0; i--)
                {
                    if(excludedNames.Contains(elements_ToRemove[i]?.name))
                    {
                        elements_ToRemove.RemoveAt(i);
                    }
                }
            }

            List<Guid> guids = elements_ToRemove.ConvertAll(x => new Guid(x.GUID));

            elements_ToRemove.ForEach(x => x.Delete());

            return guids;
        }

        public static List<Guid> RemoveUnusedElements(this TAS3D.T3DDocument t3DDocument)
        {
            return RemoveUnusedElements(t3DDocument?.Building);
        }
    }
}