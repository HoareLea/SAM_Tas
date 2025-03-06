using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PipeLossComponent ToTPD(this DisplaySystemPipeLossComponent displaySystemPipeLossComponent, PlantRoom plantRoom, PipeLossComponent pipeLossComponent = null)
        {
            if (displaySystemPipeLossComponent == null || plantRoom == null)
            {
                return null;
            }

            PipeLossComponent result = pipeLossComponent;
            if(result == null)
            {
                result = plantRoom.AddPipeLossComponent();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemPipeLossComponent.Name;
            @dynamic.Description = displaySystemPipeLossComponent.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.DesignPressureDrop = displaySystemPipeLossComponent.DesignPressureDrop;
            result.Capacity = displaySystemPipeLossComponent.Capacity;
            result.length = displaySystemPipeLossComponent.Length;
            result.PipeDiamIn = displaySystemPipeLossComponent.InsidePipeDiameter;
            result.PipeDiamOut = displaySystemPipeLossComponent.OutsidePipeDiameter;
            result.PipeConductivity = displaySystemPipeLossComponent.PipeConductivity;
            result.InsThickness = displaySystemPipeLossComponent.InsulationThickness;
            result.InsConductivity = displaySystemPipeLossComponent.InsulationConductivity;
            result.AmbTemp?.Update(displaySystemPipeLossComponent.AmbientTemperature, energyCentre);
            result.IsUnderground = displaySystemPipeLossComponent.IsUnderground.ToTPD();
            result.GrConductivity = displaySystemPipeLossComponent.GroundConductivity;
            result.GrHeatCapacity = displaySystemPipeLossComponent.GroundHeatCapacity;
            result.GrDensity = displaySystemPipeLossComponent.GroundDensity;
            result.GrTemp = displaySystemPipeLossComponent.GroundTemperature;

            if(pipeLossComponent == null)
            {
                displaySystemPipeLossComponent.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
