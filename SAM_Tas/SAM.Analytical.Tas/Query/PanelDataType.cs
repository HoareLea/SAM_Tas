using System;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static PanelDataType PanelDataType(this int? tsdSurfaceArray)
        {
            if (tsdSurfaceArray == null || !tsdSurfaceArray.HasValue)
                return Tas.PanelDataType.Undefined;

            return (PanelDataType)tsdSurfaceArray.Value;
        }

        public static PanelDataType PanelDataType(this object @object)
        {
            if (@object is PanelDataType)
                return (PanelDataType)@object;

            PanelDataType result;
            if (@object is string)
            {
                string value = (string)@object;

                if (Enum.TryParse(value, out result))
                    return result;

                value = value.Replace(" ", string.Empty).ToUpper();
                foreach (PanelDataType panelDataType in Enum.GetValues(typeof(PanelDataType)))
                {
                    string value_Type = panelDataType.ToString().ToUpper();
                    if (value_Type.Equals(value))
                        return result;
                }

                return Tas.PanelDataType.Undefined;
            }

            if (@object is int)
                return (PanelDataType)(int)@object;

            return Tas.PanelDataType.Undefined;
        }

        public static PanelDataType PanelDataType(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Tas.PanelDataType.Undefined;

            foreach (PanelDataType panelDataType in Enum.GetValues(typeof(PanelDataType)))
            {
                string value = null;

                value = panelDataType.Text();
                if (text.Equals(value))
                    return panelDataType;

                value = Text(panelDataType);
                if (text.Equals(value))
                    return panelDataType;
            }

            return Tas.PanelDataType.Undefined;
        }
    }
}