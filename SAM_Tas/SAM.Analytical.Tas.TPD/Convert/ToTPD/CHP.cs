using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CHP ToTPD(this DisplaySystemCHP displaySystemCHP, PlantRoom plantRoom, CHP cHP = null)
        {
            if (displaySystemCHP == null || plantRoom == null)
            {
                return null;
            }

            CHP result = cHP;
            if(result == null)
            {
                result = plantRoom.AddChp();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemCHP.Name;
            @dynamic.Description = displaySystemCHP.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemCHP.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemCHP.Efficiency, energyCentre);
            result.HeatPowerRatio?.Update(displaySystemCHP.HeatPowerRatio, energyCentre);
            result.Duty?.Update(displaySystemCHP.Duty, plantRoom);
            result.DesignDeltaT = displaySystemCHP.DesignTemperatureDifference;
            result.Capacity = displaySystemCHP.Capacity;
            result.DesignPressureDrop = displaySystemCHP.DesignPressureDrop;
            result.LossesInSizing = displaySystemCHP.LossesInSizing.ToTPD();

            if (displaySystemCHP.LossesInSizing || displaySystemCHP.IsDomesticHotWater)
            {
                tpdCHPFlags tpdCHPFlags = displaySystemCHP.LossesInSizing && displaySystemCHP.IsDomesticHotWater ? tpdCHPFlags.tpdCHPLossesInSizing | tpdCHPFlags.tpdCHPIsDHW :
                    displaySystemCHP.LossesInSizing ? tpdCHPFlags.tpdCHPLossesInSizing : tpdCHPFlags.tpdCHPIsDHW;

                result.Flags = (int)tpdCHPFlags;
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemCHP.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemCHP.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemCHP.GetValue<string>(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if(cHP == null)
            {
                displaySystemCHP.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
