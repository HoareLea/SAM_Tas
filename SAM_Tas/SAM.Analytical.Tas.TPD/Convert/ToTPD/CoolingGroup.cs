// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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

            bool isEfficiency = displayCoolingSystemCollection.Distribution?.IsEfficiency ?? false;
            result.UseDistributionHeatGainProfile = (!isEfficiency).ToTPD();

            ProfileData profileData = isEfficiency ? dynamic.DistributionEfficiency : dynamic.DistributionHeatGainProfile;
            profileData?.Update(displayCoolingSystemCollection.Distribution, energyCentre);

            if(coolingGroup == null)
            {
                displayCoolingSystemCollection.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}

