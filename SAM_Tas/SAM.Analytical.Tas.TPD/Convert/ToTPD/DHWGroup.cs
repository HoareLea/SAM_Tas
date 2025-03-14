using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DHWGroup ToTPD(this DisplayDomesticHotWaterSystemCollection displayDomesticHotWaterSystemCollection, PlantRoom plantRoom, DHWGroup dHWGroup = null)
        {
            if (displayDomesticHotWaterSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            DHWGroup result = dHWGroup;
            if(result == null)
            {
                result = plantRoom.AddDHWGroup();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displayDomesticHotWaterSystemCollection.Name;
            @dynamic.Description = displayDomesticHotWaterSystemCollection.Description;

            dynamic.DesignPressureDrop = displayDomesticHotWaterSystemCollection.DesignPressureDrop;
            dynamic.DesignDeltaT = displayDomesticHotWaterSystemCollection.DesignTemperatureDifference;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.LoadDistribution = displayDomesticHotWaterSystemCollection.LoadDistribution.ToTPD();
            result.MinimumReturnTemp = displayDomesticHotWaterSystemCollection.MinimumReturnTemperature;
            result.UseDistributionHeatLossProfile = displayDomesticHotWaterSystemCollection.Distribution == null ? (false).ToTPD() : (!displayDomesticHotWaterSystemCollection.Distribution.IsEfficiency).ToTPD();
            result.DistributionHeatLossProfile.Update(displayDomesticHotWaterSystemCollection.Distribution, energyCentre);

            if(dHWGroup == null)
            {
                displayDomesticHotWaterSystemCollection.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
