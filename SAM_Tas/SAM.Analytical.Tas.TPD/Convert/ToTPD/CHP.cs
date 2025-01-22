using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CHP ToTPD(this DisplaySystemCHP displaySystemCHP, PlantRoom plantRoom)
        {
            if (displaySystemCHP == null || plantRoom == null)
            {
                return null;
            }

            CHP result = plantRoom.AddChp();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemCHP.Name;
            @dynamic.Description = displaySystemCHP.Description;

            result.Setpoint?.Update(displaySystemCHP.Setpoint);
            result.Efficiency?.Update(displaySystemCHP.Efficiency);
            result.HeatPowerRatio?.Update(displaySystemCHP.HeatPowerRatio);
            result.Duty?.Update(displaySystemCHP.Duty);
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

            displaySystemCHP.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
