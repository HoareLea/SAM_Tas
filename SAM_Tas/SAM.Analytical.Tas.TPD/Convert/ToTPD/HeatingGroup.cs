// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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

            bool isEfficiency = displayHeatingSystemCollection.Distribution?.IsEfficiency ?? false;
            result.UseDistributionHeatLossProfile = (!isEfficiency).ToTPD();

            ProfileData profileData = isEfficiency ? dynamic.DistributionEfficiency : dynamic.DistributionHeatLossProfile;
            profileData?.Update(displayHeatingSystemCollection.Distribution, energyCentre);

            if(heatingGroup == null)
            {
                displayHeatingSystemCollection.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
