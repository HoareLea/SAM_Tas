using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static GroundSource ToTPD(this DisplaySystemVerticalBorehole displaySystemVerticalBorehole, PlantRoom plantRoom, GroundSource groundSource = null)
        {
            if (displaySystemVerticalBorehole == null || plantRoom == null)
            {
                return null;
            }

            GroundSource result = groundSource;
            if(result == null)
            {
                result = plantRoom.AddGroundSource();
            }

            dynamic @dynamic = result;
            dynamic.Name = displaySystemVerticalBorehole.Name;
            dynamic.Description = displaySystemVerticalBorehole.Description;

            result.Capacity = displaySystemVerticalBorehole.Capacity;
            result.DesignPressureDrop = displaySystemVerticalBorehole.DesignPressureDrop;
            result.Bhlength = displaySystemVerticalBorehole.Length;
            result.Bhdiameter = displaySystemVerticalBorehole.Diameter;
            result.GroundConductivity = displaySystemVerticalBorehole.GroundConductivity;
            result.GroundHeatCapacity = displaySystemVerticalBorehole.GroundHeatCapacity;
            result.GroundDensity = displaySystemVerticalBorehole.GroundDensity;
            result.NumBoreholes = displaySystemVerticalBorehole.NumberOfBoreholes;
            result.Gfunction?.Update(displaySystemVerticalBorehole.GFunction);
            result.GfunctionReferenceRatio = displaySystemVerticalBorehole.GFunctionReferenceRatio;
            result.PipeInDiameter = displaySystemVerticalBorehole.PipeInDiameter;
            result.PipeOutDiameter = displaySystemVerticalBorehole.PipeOutDiameter;
            result.PipeConductivity = displaySystemVerticalBorehole.PipeConductivity;
            result.GroutConductivity = displaySystemVerticalBorehole.GroutConductivity;
            result.GroundTemp = displaySystemVerticalBorehole.GroundTemperatureAve;

            if(groundSource == null)
            {
                displaySystemVerticalBorehole.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
