using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Pump ToTPD(this DisplaySystemPump displaySystemPump, PlantRoom plantRoom)
        {
            if (displaySystemPump == null || plantRoom == null)
            {
                return null;
            }

            Pump result = plantRoom.AddPump();
            
            dynamic @dynamic = result;
            @dynamic.Name = displaySystemPump.Name;
            @dynamic.Description = displaySystemPump.Description;

            result.OverallEfficiency?.Update(displaySystemPump.OverallEfficiency);
            result.Pressure = displaySystemPump.Pressure;
            result.DesignFlowRate = displaySystemPump.DesignFlowRate;
            result.Capacity = displaySystemPump.Capacity;
            result.PartLoad?.Update(displaySystemPump.PartLoad);

            result.ControlType = displaySystemPump.FanControlType.ToTPD();

            FuelSource fuelSource = plantRoom.FuelSource(displaySystemPump.GetValue<string>(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName));
            if(fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemPump.ScheduleName);

            displaySystemPump.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
