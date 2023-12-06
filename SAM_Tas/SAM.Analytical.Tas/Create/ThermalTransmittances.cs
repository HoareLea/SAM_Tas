namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static ThermalTransmittances ThermalTransmittances(this TCD.Construction construction)
        {
            if(construction == null)
            {
                return null;
            }

            object @object = construction?.GetUValue();
            if (@object == null)
            {
                return null;
            }

            float[] values = Query.Array<float>(@object);
            if (values == null || values.Length == 0)
            {
                return null;
            }

            ThermalTransmittances result = new ThermalTransmittances(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
            return result;
        }
    }
}