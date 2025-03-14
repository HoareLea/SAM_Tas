using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CoolingGroup ToTPD(this DisplayCoolingSystemCollection displayCoolingSystemCollection, PlantRoom plantRoom, CoolingGroup coolingGroup = null)
        {
            if (displayCoolingSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            CoolingGroup result = coolingGroup;
            if(result == null)
            {
                result = plantRoom.AddCoolingGroup();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displayCoolingSystemCollection.Name;
            @dynamic.Description = displayCoolingSystemCollection.Description;

            dynamic.DesignPressureDrop = displayCoolingSystemCollection.DesignPressureDrop;
            dynamic.DesignDeltaT = displayCoolingSystemCollection.DesignTemperatureDifference;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.MaximumReturnTemp = displayCoolingSystemCollection.MaximumReturnTemperature;
            result.VariableFlowCapacity = displayCoolingSystemCollection.VariableFlowCapacity.ToTPD();
            //result.PeakDemand = displayCoolingSystemCollection.PeakDemand;
            result.SizeFraction = displayCoolingSystemCollection.SizeFraction;
            result.DistributionHeatGainProfile?.Update(displayCoolingSystemCollection.Distribution, energyCentre);
            result.UseDistributionHeatGainProfile = displayCoolingSystemCollection.Distribution == null ? (false).ToTPD() : (!displayCoolingSystemCollection.Distribution.IsEfficiency).ToTPD();

            if(coolingGroup == null)
            {
                displayCoolingSystemCollection.SetLocation(result as PlantComponent);
            }


            return result;
        }
    }
}

