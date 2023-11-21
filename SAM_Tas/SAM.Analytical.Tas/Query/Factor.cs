namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static double? Factor(double? totalSolarEnergyTransmittance, double? lightTransmittance)
        {
            if ((totalSolarEnergyTransmittance == null && lightTransmittance == null) || (!totalSolarEnergyTransmittance.HasValue && !lightTransmittance.HasValue))
            {
                return null;
            }

            double totalSolarEnergyTransmittance_Temp = totalSolarEnergyTransmittance != null && totalSolarEnergyTransmittance.HasValue ? totalSolarEnergyTransmittance.Value : double.NaN;
            double lightTransmittance_Temp = lightTransmittance != null && lightTransmittance.HasValue ? lightTransmittance.Value : double.NaN;

            if (double.IsNaN(totalSolarEnergyTransmittance_Temp) && double.IsNaN(lightTransmittance_Temp))
            {
                return double.NaN;
            }

            if (double.IsNaN(totalSolarEnergyTransmittance_Temp))
            {
                return lightTransmittance_Temp;
            }

            if (double.IsNaN(lightTransmittance_Temp))
            {
                return totalSolarEnergyTransmittance_Temp;
            }

            return (0.6 * totalSolarEnergyTransmittance_Temp) + (0.4 * lightTransmittance_Temp);
        }
    }
}