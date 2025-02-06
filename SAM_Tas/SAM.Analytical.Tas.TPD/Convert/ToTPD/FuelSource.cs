using SAM.Core;
using SAM.Core.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FuelSource ToTPD(this SystemEnergySource systemEnergySource, EnergyCentre energyCentre)
        {
            if(systemEnergySource == null || energyCentre == null)
            {
                return null;
            }

            FuelSource result = energyCentre.AddFuelSource();
            ((dynamic)result).Electrical = systemEnergySource is ElectricalEnergySource;

            result.Name = systemEnergySource.Name;
            result.Description = systemEnergySource.Description;

            Modify.SetSchedule(result, systemEnergySource.ScheduleName);

            result.CustomerMonthlyCharge = System.Convert.ToSingle(systemEnergySource.CustomerMonthlyCharge);
            result.FuelCostAdjustment = System.Convert.ToSingle(systemEnergySource.FuelCostAdjustment);
            result.Discount = System.Convert.ToSingle(systemEnergySource.Discount);
            result.OffPeakCost = systemEnergySource.OffPeakCost;

            IndexedDoubles indexedDoubles = null;

            indexedDoubles = systemEnergySource.PeakCost;
            if (indexedDoubles != null)
            {
                if (indexedDoubles.Count == 1)
                {
                    result.TimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseValue;
                    result.PeakCost = indexedDoubles[0];
                }
                else if (indexedDoubles.Count == 24)
                {
                    result.TimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseHourly;
                    foreach (int key in indexedDoubles.Keys)
                    {
                        result.SetHourlyValue(key + 1, System.Convert.ToSingle(indexedDoubles[key]));
                    }
                }
                else if (indexedDoubles.Count == 8760)
                {
                    result.TimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseYearly;
                    foreach (int key in indexedDoubles.Keys)
                    {
                        result.SetYearlyValue(key + 1, System.Convert.ToSingle(indexedDoubles[key]));
                    }
                }
            }

            indexedDoubles = systemEnergySource.CO2Factor;
            if (indexedDoubles != null)
            {
                if(indexedDoubles.Count == 1)
                {
                    result.CO2TimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseValue;
                    result.CO2Factor = indexedDoubles[0];
                }
                else if (indexedDoubles.Count == 12)
                {
                    result.CO2TimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseMonthly;
                    foreach(int key in indexedDoubles.Keys)
                    {
                        result.SetCO2MonthlyValue(key + 1, System.Convert.ToSingle(indexedDoubles[key]));
                    }
                }
                else if (indexedDoubles.Count == 8760)
                {
                    result.CO2TimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseYearly;
                    foreach (int key in indexedDoubles.Keys)
                    {
                        result.SetCO2YearlyValue(key + 1, System.Convert.ToSingle(indexedDoubles[key]));
                    }
                }
            }

            indexedDoubles = systemEnergySource.PrimaryEnergyFactor;
            if (indexedDoubles != null)
            {
                if (indexedDoubles.Count == 1)
                {
                    result.PEFTimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseValue;
                    result.PrimaryEnergyFactor = indexedDoubles[0];
                }
                else if (indexedDoubles.Count == 12)
                {
                    result.PEFTimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseMonthly;
                    foreach (int key in indexedDoubles.Keys)
                    {
                        result.SetPEFMonthlyValue(key + 1, System.Convert.ToSingle(indexedDoubles[key]));
                    }
                }
                else if (indexedDoubles.Count == 8760)
                {
                    result.PEFTimeOfUseType = tpdTimeOfUseType.tpdTimeOfUseYearly;
                    foreach (int key in indexedDoubles.Keys)
                    {
                        result.SetPEFYearlyValue(key + 1, System.Convert.ToSingle(indexedDoubles[key]));
                    }
                }
            }

            return result;
        }
    }
}
