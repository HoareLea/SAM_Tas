using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static AbsorptionChiller ToTPD(this DisplaySystemAbsorptionChiller displaySystemAbsorptionChiller, PlantRoom plantRoom)
        {
            if (displaySystemAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            AbsorptionChiller result = plantRoom.AddAbsorptionChiller();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAbsorptionChiller.Name;
            @dynamic.Description = displaySystemAbsorptionChiller.Description;
            @dynamic.IsWaterSource = false;

            result.Setpoint?.Update(displaySystemAbsorptionChiller.Setpoint);
            result.Efficiency?.Update(displaySystemAbsorptionChiller.Efficiency);
            result.Duty?.Update(displaySystemAbsorptionChiller.Duty);
            result.Capacity1 = displaySystemAbsorptionChiller.Capacity1;
            result.DesignPressureDrop1 = displaySystemAbsorptionChiller.DesignPressureDrop1;
            result.Capacity2 = displaySystemAbsorptionChiller.Capacity2;
            result.DesignPressureDrop2 = displaySystemAbsorptionChiller.DesignPressureDrop2;
            result.AncillaryLoad?.Update(displaySystemAbsorptionChiller.AncillaryLoad);
            result.MinOutTempSource?.Update(displaySystemAbsorptionChiller.MinimalOutSourceTemperature);

            displaySystemAbsorptionChiller.SetLocation(result as PlantComponent);

            return result;
        }

        public static AbsorptionChiller ToTPD(this DisplaySystemWaterSourceAbsorptionChiller displaySystemWaterSourceAbsorptionChiller, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            AbsorptionChiller result = plantRoom.AddAbsorptionChiller();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceAbsorptionChiller.Name;
            @dynamic.Description = displaySystemWaterSourceAbsorptionChiller.Description;
            @dynamic.IsWaterSource = true;

            result.Setpoint?.Update(displaySystemWaterSourceAbsorptionChiller.Setpoint);
            result.Efficiency?.Update(displaySystemWaterSourceAbsorptionChiller.Efficiency);
            result.Duty?.Update(displaySystemWaterSourceAbsorptionChiller.Duty);
            result.Capacity1 = displaySystemWaterSourceAbsorptionChiller.Capacity1;
            result.DesignPressureDrop1 = displaySystemWaterSourceAbsorptionChiller.DesignPressureDrop1;
            result.Capacity2 = displaySystemWaterSourceAbsorptionChiller.Capacity2;
            result.DesignPressureDrop2 = displaySystemWaterSourceAbsorptionChiller.DesignPressureDrop2;
            result.AncillaryLoad?.Update(displaySystemWaterSourceAbsorptionChiller.AncillaryLoad);
            result.MinOutTempSource?.Update(displaySystemWaterSourceAbsorptionChiller.MinimalOutSourceTemperature);

            displaySystemWaterSourceAbsorptionChiller.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
