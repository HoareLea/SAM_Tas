using SAM.Analytical.Systems;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static MultiBoiler ToTPD(this DisplaySystemMultiBoiler displaySystemMultiBoiler, PlantRoom plantRoom, MultiBoiler multiBoiler = null)
        {
            if (displaySystemMultiBoiler == null || plantRoom == null)
            {
                return null;
            }

            MultiBoiler result = multiBoiler;
            if(result == null)
            {
                result = plantRoom.AddMultiBoiler();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemMultiBoiler.Name;
            @dynamic.Description = displaySystemMultiBoiler.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemMultiBoiler.Setpoint, energyCentre);
            result.Duty.Update(displaySystemMultiBoiler.Duty, plantRoom);
            result.DesignDeltaT = displaySystemMultiBoiler.DesignTemperatureDifference;
            result.Capacity = displaySystemMultiBoiler.Capacity;
            result.DesignPressureDrop = displaySystemMultiBoiler.DesignPressureDrop;
            result.Sequence = displaySystemMultiBoiler.Sequence.ToTPD();
            result.Multiplicity = displaySystemMultiBoiler.Multiplicity;
            result.LossesInSizing = displaySystemMultiBoiler.LossesInSizing.ToTPD();

            if(displaySystemMultiBoiler.LossesInSizing || displaySystemMultiBoiler.IsDomesticHotWater)
            {
                tpdMultiBoilerFlags tpdMultiBoilerFlags = displaySystemMultiBoiler.LossesInSizing && displaySystemMultiBoiler.IsDomesticHotWater ? tpdMultiBoilerFlags.tpdMultiBoilerLossesInSizing | tpdMultiBoilerFlags.tpdMultiBoilerIsDHW :
                    displaySystemMultiBoiler.LossesInSizing ? tpdMultiBoilerFlags.tpdMultiBoilerLossesInSizing : tpdMultiBoilerFlags.tpdMultiBoilerIsDHW;

                result.Flags = (int)tpdMultiBoilerFlags;
            }

            IEnumerable<SystemMultiBoilerItem> systemMultiBoilerItems = displaySystemMultiBoiler.Items;
            if(systemMultiBoilerItems != null)
            {
                for(int i =0; i < systemMultiBoilerItems.Count(); i++)
                {
                    SystemMultiBoilerItem systemMultiBoilerItem = systemMultiBoilerItems.ElementAt(i);

                    int index = i + 1;

                    result.SetBoilerPercentage(index, systemMultiBoilerItem.Percentage);
                    result.SetBoilerThreshold(index, systemMultiBoilerItem.Threshold);
                    ProfileData profileData;

                    profileData = result.GetBoilerEfficiency(index);
                    profileData.Update(systemMultiBoilerItem.Efficiency, energyCentre);

                    profileData = result.GetBoilerAncillaryLoad(index);
                    profileData.Update(systemMultiBoilerItem.AncillaryLoad, energyCentre);

                    FuelSource fuelSource;

                    fuelSource = plantRoom.FuelSource(systemMultiBoilerItem.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
                    if (fuelSource != null)
                    {
                        ((@dynamic)result).SetFuelSource(((index - 1) * 2) + 1, fuelSource);
                    }

                    fuelSource = plantRoom.FuelSource(systemMultiBoilerItem.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
                    if (fuelSource != null)
                    {
                        ((@dynamic)result).SetFuelSource(((index - 1) * 2) + 2, fuelSource);
                    }
                }
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemMultiBoiler.ScheduleName);

            if(multiBoiler == null)
            {
                displaySystemMultiBoiler.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
