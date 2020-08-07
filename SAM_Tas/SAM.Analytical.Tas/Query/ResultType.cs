using System;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static ResultType ResultType(this int? tsdSurfaceArray)
        {
            if (tsdSurfaceArray == null || !tsdSurfaceArray.HasValue)
                return Tas.ResultType.Undefined;

            return (ResultType)tsdSurfaceArray.Value;
        }

        public static ResultType ResultType(this object @object)
        {
            if (@object is ResultType)
                return (ResultType)@object;

            ResultType result;
            if (@object is string)
            {
                string value = (string)@object;

                if (Enum.TryParse(value, out result))
                    return result;

                value = value.Replace(" ", string.Empty).ToUpper();
                foreach (PanelType panelType in Enum.GetValues(typeof(PanelType)))
                {
                    string value_Type = panelType.ToString().ToUpper();
                    if (value_Type.Equals(value))
                        return result;
                }

                return Tas.ResultType.Undefined;
            }

            if (@object is int)
                return (ResultType)(int)(@object);

            return Tas.ResultType.Undefined;
        }

        public static ResultType ResultType(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Tas.ResultType.Undefined;

            foreach (ResultType resultType in Enum.GetValues(typeof(ResultType)))
            {
                string value = null;

                value = resultType.Text();
                if (text.Equals(value))
                    return resultType;

                value = Text(resultType);
                if (text.Equals(value))
                    return resultType;
            }

            return Tas.ResultType.Undefined;
        }
    }
}