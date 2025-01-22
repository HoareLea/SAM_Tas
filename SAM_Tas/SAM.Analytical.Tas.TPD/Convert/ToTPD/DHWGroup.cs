using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DHWGroup ToTPD(this DisplayDomesticHotWaterSystemCollection displayDomesticHotWaterSystemCollection, PlantRoom plantRoom)
        {
            if (displayDomesticHotWaterSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            DHWGroup result = plantRoom.AddDHWGroup();

            dynamic @dynamic = result;
            @dynamic.Name = displayDomesticHotWaterSystemCollection.Name;
            @dynamic.Description = displayDomesticHotWaterSystemCollection.Description;

            result.LoadDistribution = displayDomesticHotWaterSystemCollection.LoadDistribution.ToTPD();
            result.MinimumReturnTemp = displayDomesticHotWaterSystemCollection.MinimumReturnTemperature;
            result.UseDistributionHeatLossProfile = displayDomesticHotWaterSystemCollection.Distribution == null ? (false).ToTPD() : displayDomesticHotWaterSystemCollection.Distribution.IsEfficiency.ToTPD();
            result.DistributionHeatLossProfile.Update(displayDomesticHotWaterSystemCollection.Distribution);

            displayDomesticHotWaterSystemCollection.SetLocation(result as PlantComponent);

            return result as DHWGroup;
        }
    }
}
