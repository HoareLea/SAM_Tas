using System;
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
            {
                return null;
            }

            IEnumerable enumerable = @object as IEnumerable;
            if (enumerable == null)
            {
                return null;
            }

            try
            {

                return enumerable.Cast<T>().ToArray();
            }
            catch(Exception exception)
            {

            }

            List<T> list = new List<T>();
            foreach(object value in enumerable)
            {
                if(Core.Query.TryConvert(value, out T value_Result))
                {
                    list.Add(value_Result);
                }
            }

            return list.ToArray();
        }
    }
}