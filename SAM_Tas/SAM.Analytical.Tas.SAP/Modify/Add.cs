using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public static partial class Modify
    {
        public static bool Add<T>(this List<T> guidCollections, T guidCollection) where T: GuidCollection, INamedSAP
        {
            if (guidCollections == null || guidCollection == null || guidCollection.Name == null)
            {
                return false;
            }

            int index = guidCollections.FindIndex(x => x.Name == guidCollection.Name);
            if (index == -1)
            {
                guidCollections.Add(guidCollection);
                return true;
            }

            T guidCollection_Existing = guidCollections[index];
            foreach (Guid guid in guidCollection_Existing)
            {
                guidCollection.Add(guid);
            }

            guidCollections[index] = guidCollection;
            return true;
        }
    }
}