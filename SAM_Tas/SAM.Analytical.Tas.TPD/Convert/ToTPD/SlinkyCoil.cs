using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SlinkyCoil ToTPD(this DisplaySystemSlinkyCoil displaySystemSlinkyCoil, PlantRoom plantRoom, SlinkyCoil slinkyCoil = null)
        {
            if (displaySystemSlinkyCoil == null || plantRoom == null)
            {
                return null;
            }

            SlinkyCoil result = slinkyCoil;
            if(result == null)
            {
                result = plantRoom.AddSlinkyCoil();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemSlinkyCoil.Name;
            @dynamic.Description = displaySystemSlinkyCoil.Description;

            result.DesignPressureDrop = displaySystemSlinkyCoil.DesignPressureDrop;
            result.Capacity = displaySystemSlinkyCoil.Capacity;
            result.GroundDensity = displaySystemSlinkyCoil.GroundDensity;
            result.GroundHeatCap = displaySystemSlinkyCoil.GroundHeatCapacity;
            result.GroundConductivity = displaySystemSlinkyCoil.GroundConductivity;
            result.GroundSolarReflectance = displaySystemSlinkyCoil.GroundSolarReflectance;
            result.PipeDiamIn = displaySystemSlinkyCoil.InsidePipeDiameter;
            result.PipeDiamOut = displaySystemSlinkyCoil.OutsidePipeDiameter;
            result.PipeConductivity = displaySystemSlinkyCoil.PipeConductivity;
            result.LoopPitch = displaySystemSlinkyCoil.LoopPitch;
            result.LoopWidth = displaySystemSlinkyCoil.LoopWidth;
            result.LoopHeight = displaySystemSlinkyCoil.LoopHeight;
            result.IsUprightCoil = displaySystemSlinkyCoil.IsUprightCoil.ToTPD();
            result.FillDensity = displaySystemSlinkyCoil.FillDensity;
            result.FillHeatCap = displaySystemSlinkyCoil.FillHeatCapacity;
            result.FillConductivity = displaySystemSlinkyCoil.FillConductivity;
            result.TrenchLength = displaySystemSlinkyCoil.TrenchLength;
            result.TrenchDepth = displaySystemSlinkyCoil.TrenchDepth;
            result.TrenchWidth = displaySystemSlinkyCoil.TrenchWidth;

            if(slinkyCoil == null)
            {
                displaySystemSlinkyCoil.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
