using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatingGroup ToTPD(this DisplayHeatingSystemCollection displayHeatingSystemCollection, PlantRoom plantRoom, HeatingGroup heatingGroup = null)
        {
            if (displayHeatingSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            HeatingGroup result = heatingGroup;
            if(result == null)
            {
                result = plantRoom.AddHeatingGroup();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displayHeatingSystemCollection.Name;
            @dynamic.Description = displayHeatingSystemCollection.Description;

            dynamic.DesignPressureDrop = displayHeatingSystemCollection.DesignPressureDrop;
            dynamic.DesignDeltaT = displayHeatingSystemCollection.DesignTemperatureDifference;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.MinimumReturnTemp = displayHeatingSystemCollection.MinimumReturnTemperature;
            result.VariableFlowCapacity = displayHeatingSystemCollection.VariableFlowCapacity.ToTPD();
            //result.PeakDemand = displayHeatingSystemCollection.PeakDemand;
            result.SizeFraction = displayHeatingSystemCollection.SizeFraction;
            result.UseDistributionHeatLossProfile = displayHeatingSystemCollection.Distribution == null ? (false).ToTPD() : (!displayHeatingSystemCollection.Distribution.IsEfficiency).ToTPD();
            result.DistributionHeatLossProfile?.Update(displayHeatingSystemCollection.Distribution, energyCentre);

            if(heatingGroup == null)
            {
                displayHeatingSystemCollection.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
