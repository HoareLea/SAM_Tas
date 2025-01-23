using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PipeLossComponent ToTPD(this DisplaySystemPipeLossComponent displaySystemPipeLossComponent, PlantRoom plantRoom)
        {
            if (displaySystemPipeLossComponent == null || plantRoom == null)
            {
                return null;
            }

            PipeLossComponent result = plantRoom.AddPipeLossComponent();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemPipeLossComponent.Name;
            @dynamic.Description = displaySystemPipeLossComponent.Description;

            result.DesignPressureDrop = displaySystemPipeLossComponent.DesignPressureDrop;
            result.Capacity = displaySystemPipeLossComponent.Capacity;
            result.length = displaySystemPipeLossComponent.Length;
            result.PipeDiamIn = displaySystemPipeLossComponent.InsidePipeDiameter;
            result.PipeDiamOut = displaySystemPipeLossComponent.OutsidePipeDiameter;
            result.PipeConductivity = displaySystemPipeLossComponent.PipeConductivity;
            result.InsThickness = displaySystemPipeLossComponent.InsulationThickness;
            result.InsConductivity = displaySystemPipeLossComponent.InsulationConductivity;
            result.AmbTemp?.Update(displaySystemPipeLossComponent.AmbientTemperature);
            result.IsUnderground = displaySystemPipeLossComponent.IsUnderground.ToTPD();
            result.GrConductivity = displaySystemPipeLossComponent.GroundConductivity;
            result.GrHeatCapacity = displaySystemPipeLossComponent.GroundHeatCapacity;
            result.GrDensity = displaySystemPipeLossComponent.GroundDensity;
            result.GrTemp = displaySystemPipeLossComponent.GroundTemperature;

            displaySystemPipeLossComponent.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
