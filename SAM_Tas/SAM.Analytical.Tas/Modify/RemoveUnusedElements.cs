using SAM.Core.Tas;
using System;
using System.Collections.Generic;

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

        public static List<Guid> RemoveUnusedElements(this TAS3D.Building building)
        {
            List<TAS3D.Element> elements = building?.Elements();
            if (elements == null)
                return null;

            List<TAS3D.Element> elements_ToRemove = elements.FindAll(x => x.isUsed == 0);

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