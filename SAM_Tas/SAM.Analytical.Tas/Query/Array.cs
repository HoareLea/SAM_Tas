using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static T[] Array<T>(object @object)
        {
            if (@object is null)
                return null;

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
                return null;

            return enumerable.Cast<T>().ToArray();
        }
    }
}