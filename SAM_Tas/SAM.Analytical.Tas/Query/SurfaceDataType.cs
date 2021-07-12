using System;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static SpaceDataType SpaceDataType(this int? tsdZoneArray)
        {
            if (tsdZoneArray == null || !tsdZoneArray.HasValue)
                return Tas.SpaceDataType.Undefined;

            return (SpaceDataType)tsdZoneArray.Value;
        }

        public static SpaceDataType SpaceDataType(this object @object)
        {
            if (@object is SpaceDataType)
                return (SpaceDataType)@object;

            SpaceDataType result;
            if (@object is string)
            {
                string value = (string)@object;

                if (Enum.TryParse(value, out result))
                    return result;

                value = value.Replace(" ", string.Empty).ToUpper();
                foreach (SpaceDataType spaceDataType in Enum.GetValues(typeof(SpaceDataType)))
                {
                    string value_Type = spaceDataType.ToString().ToUpper();
                    if (value_Type.Equals(value))
                        return result;
                }

                return Tas.SpaceDataType.Undefined;
            }

            if (@object is int)
                return (SpaceDataType)(int)@object;

            return Tas.SpaceDataType.Undefined;
        }

        public static SpaceDataType SpaceDataType(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Tas.SpaceDataType.Undefined;

            foreach (SpaceDataType spaceDataType in Enum.GetValues(typeof(SpaceDataType)))
            {
                string value = null;

                value = spaceDataType.Text();
                if (text.Equals(value))
                    return spaceDataType;

                value = Text(spaceDataType);
                if (text.Equals(value))
                    return spaceDataType;
            }

            return Tas.SpaceDataType.Undefined;
        }
    }
}