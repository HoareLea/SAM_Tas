namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Light Transmittance (LT Value) [-]
        /// </summary>
        /// <param name="buildingElement">TBD buildingElement</param>
        /// <param name="tolerance">Tolerance</param>
        /// <returns>LightTransmittance [-]</returns>
        public static double LightTransmittance(this TBD.buildingElement buildingElement, double tolerance = Core.Tolerance.MacroDistance)
        {
            TBD.Construction construction = buildingElement?.GetConstruction();
            if (construction == null)
                return double.NaN;

            return LightTransmittance(construction, tolerance);
        }

        /// <summary>
        /// Light Transmittance (LT Value) [-]
        /// </summary>
        /// <param name="construction">TBD construction</param>
        /// <param name="tolerance">Tolerance</param>
        /// <returns>LightTransmittance [-]</returns>
        public static double LightTransmittance(this TBD.Construction construction, double tolerance = Core.Tolerance.MacroDistance)
        {
            if (construction == null)
                return double.NaN;
            
            TBD.ConstructionTypes constructionTypes =construction.type;
            if (constructionTypes == TBD.ConstructionTypes.tcdTransparentConstruction)
            {
                object @object = construction.GetGlazingValues();
                float[] values = Array<float>(@object);
                if (values == null || values.Length == 0)
                    return double.NaN;

                return Core.Query.Round(values[0], tolerance);
            }

            return 0;
        }
    }
}