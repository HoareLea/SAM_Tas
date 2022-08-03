using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static List<InternalCondition> InternalConditions(this TIC.Document document)
        {
            List<TIC.InternalCondition> internalConditions = Query.InternalConditions(document);

            return internalConditions?.ConvertAll(x => Convert.ToSAM(x));
        }
    }
}