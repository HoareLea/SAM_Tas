// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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

            bool isEfficiency = displayDomesticHotWaterSystemCollection.Distribution?.IsEfficiency ?? false;
            result.UseDistributionHeatLossProfile = (!isEfficiency).ToTPD();

            ProfileData profileData = isEfficiency ? dynamic.DistributionEfficiency : dynamic.DistributionHeatLossProfile;
            profileData?.Update(displayDomesticHotWaterSystemCollection.Distribution, energyCentre);

            if(dHWGroup == null)
            {
                displayDomesticHotWaterSystemCollection.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
