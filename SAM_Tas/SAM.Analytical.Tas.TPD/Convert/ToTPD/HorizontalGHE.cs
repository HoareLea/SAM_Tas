using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HorizontalGHE ToTPD(this DisplaySystemHorizontalExchanger displaySystemHorizontalExchanger, PlantRoom plantRoom, HorizontalGHE horizontalGHE = null)
        {
            if (displaySystemHorizontalExchanger == null || plantRoom == null)
            {
                return null;
            }

            HorizontalGHE result = horizontalGHE;
            if(result == null)
            {
                result = plantRoom.AddHorizontalGHE();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemHorizontalExchanger.Name;
            @dynamic.Description = displaySystemHorizontalExchanger.Description;

            result.DesignPressureDrop = displaySystemHorizontalExchanger.DesignPressureDrop;
            result.Capacity = displaySystemHorizontalExchanger.Capacity;
            result.GroundDensity = displaySystemHorizontalExchanger.GroundDensity;
            result.GroundHeatCap = displaySystemHorizontalExchanger.GroundHeatCapacity;
            result.GroundConductivity = displaySystemHorizontalExchanger.GroundConductivity;
            result.GroundSolarReflectance = displaySystemHorizontalExchanger.GroundSolarReflectance;
            result.PipeDiamIn = displaySystemHorizontalExchanger.InsidePipeDiameter;
            result.PipeDiamOut = displaySystemHorizontalExchanger.OutsidePipeDiameter;
            result.PipeConductivity = displaySystemHorizontalExchanger.PipeConductivity;
            result.PipeLength = displaySystemHorizontalExchanger.PipeLength;
            result.PipeSeparation = displaySystemHorizontalExchanger.PipeSeparation;
            result.PipeLength = displaySystemHorizontalExchanger.PipeLength;
            result.PipeDepth = displaySystemHorizontalExchanger.PipeDepth;

            if(horizontalGHE == null)
            {
                displaySystemHorizontalExchanger.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
