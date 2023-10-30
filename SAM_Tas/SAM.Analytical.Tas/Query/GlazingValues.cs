namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static void GlazingValues(this TBD.Construction construction, 
            out double lightTransmittance, 
            out double lightReflectance, 
            out double directSolarEnergyTransmittance, 
            out double directSolarEnergyReflectance, 
            out double directSolarEnergyAbsorptance,
            out double totalSolarEnergyTransmittance,
            out double pilkingtonShortWavelengthCoefficient,
            out double pilkingtonLongWavelengthCoefficient,
            double tolerance = Core.Tolerance.MacroDistance)
        {
            lightTransmittance = double.NaN;
            lightReflectance = double.NaN;
            directSolarEnergyTransmittance = double.NaN;
            directSolarEnergyReflectance = double.NaN;
            directSolarEnergyAbsorptance = double.NaN;
            totalSolarEnergyTransmittance = double.NaN;
            pilkingtonShortWavelengthCoefficient = double.NaN;
            pilkingtonLongWavelengthCoefficient = double.NaN;

            if (construction == null)
            {
                return;
            }

            TBD.ConstructionTypes constructionTypes = construction.type;
            if (constructionTypes != TBD.ConstructionTypes.tcdTransparentConstruction)
            {
                return;
            }

            object values_Temp = construction.GetGlazingValues();

            float[] values = Array<float>(values_Temp);

            GlazingValues(values,
                out lightTransmittance,
                out lightReflectance,
                out directSolarEnergyTransmittance,
                out directSolarEnergyReflectance,
                out directSolarEnergyAbsorptance,
                out totalSolarEnergyTransmittance,
                out pilkingtonShortWavelengthCoefficient,
                out pilkingtonLongWavelengthCoefficient,
                tolerance);
        }

        public static void GlazingValues(this TCD.Construction construction,
            out double lightTransmittance,
            out double lightReflectance,
            out double directSolarEnergyTransmittance,
            out double directSolarEnergyReflectance,
            out double directSolarEnergyAbsorptance,
            out double totalSolarEnergyTransmittance,
            out double pilkingtonShortWavelengthCoefficient,
            out double pilkingtonLongWavelengthCoefficient,
            double tolerance = Core.Tolerance.MacroDistance)
        {
            lightTransmittance = double.NaN;
            lightReflectance = double.NaN;
            directSolarEnergyTransmittance = double.NaN;
            directSolarEnergyReflectance = double.NaN;
            directSolarEnergyAbsorptance = double.NaN;
            totalSolarEnergyTransmittance = double.NaN;
            pilkingtonShortWavelengthCoefficient = double.NaN;
            pilkingtonLongWavelengthCoefficient = double.NaN;

            if (construction == null)
            {
                return;
            }

            TCD.ConstructionTypes constructionTypes = construction.type;
            if (constructionTypes != TCD.ConstructionTypes.tcdTransparentConstruction)
            {
                return;
            }

            object values_Temp = construction.GetGlazingValues();

            float[] values = Array<float>(values_Temp);

            GlazingValues(values,
                out lightTransmittance,
                out lightReflectance,
                out directSolarEnergyTransmittance,
                out directSolarEnergyReflectance,
                out directSolarEnergyAbsorptance,
                out totalSolarEnergyTransmittance,
                out pilkingtonShortWavelengthCoefficient,
                out pilkingtonLongWavelengthCoefficient,
                tolerance);
        }

        private static void GlazingValues(this float[] values,
            out double lightTransmittance,
            out double lightReflectance,
            out double directSolarEnergyTransmittance,
            out double directSolarEnergyReflectance,
            out double directSolarEnergyAbsorptance,
            out double totalSolarEnergyTransmittance,
            out double pilkingtonShortWavelengthCoefficient,
            out double pilkingtonLongWavelengthCoefficient,
            double tolerance = Core.Tolerance.MacroDistance)
        {
            lightTransmittance = double.NaN;
            lightReflectance = double.NaN;
            directSolarEnergyTransmittance = double.NaN;
            directSolarEnergyReflectance = double.NaN;
            directSolarEnergyAbsorptance = double.NaN;
            totalSolarEnergyTransmittance = double.NaN;
            pilkingtonShortWavelengthCoefficient = double.NaN;
            pilkingtonLongWavelengthCoefficient = double.NaN;

            if (values == null || values.Length == 0)
            {
                return;
            }

            if (values.Length > 0)
            {
                lightTransmittance = Core.Query.Round(values[0], tolerance);
                if (values.Length > 1)
                {
                    lightReflectance = Core.Query.Round(values[1], tolerance);
                    if (values.Length > 2)
                    {
                        directSolarEnergyTransmittance = Core.Query.Round(values[2], tolerance);
                        if (values.Length > 3)
                        {
                            directSolarEnergyReflectance = Core.Query.Round(values[3], tolerance);
                            if (values.Length > 4)
                            {
                                directSolarEnergyAbsorptance = Core.Query.Round(values[4], tolerance);
                                if (values.Length > 5)
                                {
                                    totalSolarEnergyTransmittance = Core.Query.Round(values[5], tolerance);
                                    if (values.Length > 6)
                                    {
                                        pilkingtonShortWavelengthCoefficient = Core.Query.Round(values[6], tolerance);
                                        if (values.Length > 7)
                                        {
                                            pilkingtonLongWavelengthCoefficient = Core.Query.Round(values[7], tolerance);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}