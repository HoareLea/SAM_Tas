// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Pump ToTPD(this DisplaySystemPump displaySystemPump, PlantRoom plantRoom, Pump pump = null)
        {
            if (displaySystemPump == null || plantRoom == null)
            {
                return null;
            }

            Pump result = pump;
            if(result == null)
            {
                result = plantRoom.AddPump();
            }
            
            dynamic @dynamic = result;
            @dynamic.Name = displaySystemPump.Name;
            @dynamic.Description = displaySystemPump.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.OverallEfficiency?.Update(displaySystemPump.OverallEfficiency, energyCentre);
            result.Pressure = displaySystemPump.Pressure;
            //result.DesignFlowRate = displaySystemPump.DesignFlowRate; //20206-03-22 this force auto-size consider 0 but then DHW issue
            result.Capacity = displaySystemPump.Capacity;
            result.PartLoad?.Update(displaySystemPump.PartLoad, energyCentre);

            result.ControlType = displaySystemPump.FanControlType.ToTPD();

            FuelSource fuelSource = plantRoom.FuelSource(displaySystemPump.GetValue<string>(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName));
            if(fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemPump.ScheduleName);

            if(pump == null)
            {
                displaySystemPump.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
