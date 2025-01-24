using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static WaterSourceChiller ToTPD(this DisplaySystemWaterSourceChiller displaySystemWaterSourceChiller, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceChiller == null || plantRoom == null)
            {
                return null;
            }

            WaterSourceChiller result = plantRoom.AddWaterSourceChiller();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceChiller.Name;
            @dynamic.Description = displaySystemWaterSourceChiller.Description;

            result.Setpoint?.Update(displaySystemWaterSourceChiller.Setpoint);
            result.Efficiency?.Update(displaySystemWaterSourceChiller.Efficiency);
            result.Duty?.Update(displaySystemWaterSourceChiller.Duty);
            result.Capacity1 = displaySystemWaterSourceChiller.Capacity1;
            result.DesignPressureDrop1 = displaySystemWaterSourceChiller.DesignPressureDrop1;
            result.DesignDeltaT1 = displaySystemWaterSourceChiller.DesignPressureDrop1;
            result.Capacity2 = displaySystemWaterSourceChiller.Capacity2;
            result.DesignPressureDrop2 = displaySystemWaterSourceChiller.DesignPressureDrop2;
            result.DesignDeltaT2 = displaySystemWaterSourceChiller.DesignPressureDrop2;
            result.MotorEfficiency?.Update(displaySystemWaterSourceChiller.MotorEfficiency);
            result.ExchCalcType = displaySystemWaterSourceChiller.ExchangerCalculationMethod.ToTPD();
            result.ExchangerEfficiency?.Update(displaySystemWaterSourceChiller.ExchangerEfficiency);
            result.HeatTransSurfArea = displaySystemWaterSourceChiller.HeatTransferSurfaceArea;
            result.HeatTransCoeff = displaySystemWaterSourceChiller.HeatTransferCoefficient;
            result.ExchangerType = displaySystemWaterSourceChiller.ExchangerType.ToTPD();
            result.AncillaryLoad?.Update(displaySystemWaterSourceChiller.AncillaryLoad);
            result.FreeCoolingType = displaySystemWaterSourceChiller.FreeCoolingType.ToTPD();
            result.LossesInSizing = displaySystemWaterSourceChiller.LossesInSizing.ToTPD();

            @dynamic.IsDirectAbsChiller = false;

            displaySystemWaterSourceChiller.SetLocation(result as PlantComponent);

            return result;
        }

        public static WaterSourceChiller ToTPD(this DisplaySystemWaterSourceDirectAbsorptionChiller displaySystemWaterSourceDirectAbsorptionChiller, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceDirectAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            WaterSourceChiller result = plantRoom.AddWaterSourceChiller();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceDirectAbsorptionChiller.Name;
            @dynamic.Description = displaySystemWaterSourceDirectAbsorptionChiller.Description;

            result.Setpoint?.Update(displaySystemWaterSourceDirectAbsorptionChiller.Setpoint);
            result.Efficiency?.Update(displaySystemWaterSourceDirectAbsorptionChiller.Efficiency);
            result.Duty?.Update(displaySystemWaterSourceDirectAbsorptionChiller.Duty);
            result.Capacity1 = displaySystemWaterSourceDirectAbsorptionChiller.Capacity1;
            result.DesignPressureDrop1 = displaySystemWaterSourceDirectAbsorptionChiller.DesignPressureDrop1;
            result.DesignDeltaT1 = displaySystemWaterSourceDirectAbsorptionChiller.DesignPressureDrop1;
            result.Capacity2 = displaySystemWaterSourceDirectAbsorptionChiller.Capacity2;
            result.DesignPressureDrop2 = displaySystemWaterSourceDirectAbsorptionChiller.DesignPressureDrop2;
            result.DesignDeltaT2 = displaySystemWaterSourceDirectAbsorptionChiller.DesignPressureDrop2;
            result.MotorEfficiency?.Update(displaySystemWaterSourceDirectAbsorptionChiller.MotorEfficiency);
            result.ExchCalcType = displaySystemWaterSourceDirectAbsorptionChiller.ExchangerCalculationMethod.ToTPD();
            result.ExchangerEfficiency?.Update(displaySystemWaterSourceDirectAbsorptionChiller.ExchangerEfficiency);
            result.HeatTransSurfArea = displaySystemWaterSourceDirectAbsorptionChiller.HeatTransferSurfaceArea;
            result.HeatTransCoeff = displaySystemWaterSourceDirectAbsorptionChiller.HeatTransferCoefficient;
            result.ExchangerType = displaySystemWaterSourceDirectAbsorptionChiller.ExchangerType.ToTPD();
            result.AncillaryLoad?.Update(displaySystemWaterSourceDirectAbsorptionChiller.AncillaryLoad);
            result.FreeCoolingType = displaySystemWaterSourceDirectAbsorptionChiller.FreeCoolingType.ToTPD();
            result.LossesInSizing = displaySystemWaterSourceDirectAbsorptionChiller.LossesInSizing.ToTPD();

            @dynamic.IsDirectAbsChiller = true;

            displaySystemWaterSourceDirectAbsorptionChiller.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
