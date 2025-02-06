using SAM.Core.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEnergySource ToSAM(this FuelSource fuelSource)
        {
            if(fuelSource == null)
            {
                return null;
            }

            SystemEnergySource result = null;

            dynamic @dynamic = fuelSource;

            if (@dynamic.Electrical)
            {
                result = new ElectricalEnergySource(fuelSource.Name);
            }
            else
            {
                result = new SystemEnergySource(fuelSource.Name);
            }

            result.Description = fuelSource.Description;
            result.ScheduleName = fuelSource.Schedule?.Name;
            result.CustomerMonthlyCharge = fuelSource.CustomerMonthlyCharge;
            result.FuelCostAdjustment = fuelSource.FuelCostAdjustment;
            result.Discount = fuelSource.Discount;
            result.OffPeakCost = fuelSource.OffPeakCost;

            result.PeakCost = new Core.IndexedDoubles();
            switch (fuelSource.TimeOfUseType)
            {
                case tpdTimeOfUseType.tpdTimeOfUseValue:
                    result.PeakCost.Add(0, fuelSource.PeakCost);
                    break;

                case tpdTimeOfUseType.tpdTimeOfUseHourly:
                    for (int i = 0; i < 24; i++)
                    {
                        result.PeakCost.Add(i, fuelSource.GetHourlyValue(i + 1));
                    }
                    break;

                case tpdTimeOfUseType.tpdTimeOfUseYearly:
                    for (int i = 0; i < 8760; i++)
                    {
                        result.PeakCost.Add(i, fuelSource.GetYearlyValue(i + 1));
                    }
                    break;
            }

            result.CO2Factor = new Core.IndexedDoubles();
            switch (fuelSource.CO2TimeOfUseType)
            {
                case tpdTimeOfUseType.tpdTimeOfUseValue:
                    result.CO2Factor.Add(0, fuelSource.CO2Factor);
                    break;

                case tpdTimeOfUseType.tpdTimeOfUseMonthly:
                    for (int i = 0; i < 12; i++)
                    {
                        result.CO2Factor.Add(i, fuelSource.GetCO2MonthlyValue(i + 1));
                    }
                    break;

                case tpdTimeOfUseType.tpdTimeOfUseYearly:
                    for (int i = 0; i < 8760; i++)
                    {
                        result.CO2Factor.Add(i, fuelSource.GetCO2YearlyValue(i + 1));
                    }
                    break;
            }

            result.PrimaryEnergyFactor = new Core.IndexedDoubles();
            switch (fuelSource.PEFTimeOfUseType)
            {
                case tpdTimeOfUseType.tpdTimeOfUseValue:
                    result.PrimaryEnergyFactor.Add(0, fuelSource.PrimaryEnergyFactor);
                    break;

                case tpdTimeOfUseType.tpdTimeOfUseMonthly:
                    for (int i = 0; i < 12; i++)
                    {
                        result.PrimaryEnergyFactor.Add(i, fuelSource.GetPEFMonthlyValue(i + 1));
                    }
                    break;

                case tpdTimeOfUseType.tpdTimeOfUseYearly:
                    for (int i = 0; i < 8760; i++)
                    {
                        result.PrimaryEnergyFactor.Add(i, fuelSource.GetPEFYearlyValue(i + 1));
                    }
                    break;
            }

            return result;
        }
    }
}
