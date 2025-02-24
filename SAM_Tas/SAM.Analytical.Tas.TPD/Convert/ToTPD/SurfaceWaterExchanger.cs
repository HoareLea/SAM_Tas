using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SurfaceWaterExchanger ToTPD(this DisplaySystemSurfaceWaterExchanger displaySystemSurfaceWaterExchanger, PlantRoom plantRoom)
        {
            if (displaySystemSurfaceWaterExchanger == null || plantRoom == null)
            {
                return null;
            }

            SurfaceWaterExchanger result = plantRoom.AddSurfaceWaterExchanger();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemSurfaceWaterExchanger.Name;
            @dynamic.Description = displaySystemSurfaceWaterExchanger.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Capacity = displaySystemSurfaceWaterExchanger.Capacity;
            result.DesignPressureDrop = displaySystemSurfaceWaterExchanger.DesignPressureDrop;
            result.Efficiency?.Update(displaySystemSurfaceWaterExchanger.Efficiency, energyCentre);
            result.PondVolume = displaySystemSurfaceWaterExchanger.PondVolume;
            result.PondSurfaceArea = displaySystemSurfaceWaterExchanger.PondSurfaceArea;
            result.PondPerimeter = displaySystemSurfaceWaterExchanger.PondPerimeter;
            result.GroundConductivity = displaySystemSurfaceWaterExchanger.GroundConductivity;
            result.WaterTableDepth = displaySystemSurfaceWaterExchanger.WaterTableDepth;

            displaySystemSurfaceWaterExchanger.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
