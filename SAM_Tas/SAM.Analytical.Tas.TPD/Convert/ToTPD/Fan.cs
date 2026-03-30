// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.Fan ToTPD(this SystemFan systemFan, global::TPD.System system, global::TPD.Fan fan = null)
        {
            if(systemFan == null || system == null)
            {
                return null;
            }

            global::TPD.Fan result = fan;
            if(result == null)
            {
                result = system.AddFan();
            }
            
            dynamic @dynamic = result;
            @dynamic.name = systemFan.Name;
            @dynamic.Description = systemFan.Description;

            PlantRoom plantRoom = system.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.OverallEfficiency?.Update(systemFan.OverallEfficiency, energyCentre);
            result.HeatGainFactor = systemFan.HeatGainFactor;
            result.Pressure = systemFan.Pressure;
            result.DesignFlowRate?.Update(systemFan.DesignFlowRate, energyCentre);
            result.DesignFlowType = systemFan.DesignFlowType.ToTPD();
            result.MinimumFlowRate?.Update(systemFan.MinimumFlowRate, energyCentre);
            result.MinimumFlowType = systemFan.MinimumFlowType.ToTPD();
            result.MinimumFlowFraction = systemFan.MinimumFlowFraction;
            result.Capacity = systemFan.Capacity;
            result.ControlType = systemFan.FanControlType.ToTPD();
            result.PartLoad?.Update(systemFan.PartLoad, energyCentre);

            Modify.SetSchedule((SystemComponent)result, systemFan.ScheduleName);

            // result.DesignFlowRate.Value = displaySystemFan.DesignFlowRate;
            // result.OverallEfficiency.Value = displaySystemFan.OverallEfficiency;
            // result.Pressure = displaySystemFan.Pressure;
            // result.HeatGainFactor = displaySystemFan.HeatGainFactor;
            // result.PartLoad.Value = 0;
            // result.PartLoad.ClearModifiers();
            //// result.SetDirection(tpdDirection.tpdLeftRight);
            // result.DesignFlowType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;



            Modify.SetSchedule((SystemComponent)result, systemFan.ScheduleName);

            CollectionLink collectionLink = systemFan.GetValue<CollectionLink>(AirSystemComponentParameter.ElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    @dynamic.SetElectricalGroup1(electricalGroup);
                }
            }

            //ProfileDataModifierTable profileDataModifierTable = result.PartLoad.AddModifierTable();
            //profileDataModifierTable.Name = "Fan Part Load Curve";
            //profileDataModifierTable.SetVariable(1, tpdProfileDataVariableType.tpdProfileDataVariablePartload);
            //profileDataModifierTable.Multiplier = tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            //profileDataModifierTable.Clear();
            //profileDataModifierTable.AddPoint(0, 0);
            //profileDataModifierTable.AddPoint(10, 3);
            //profileDataModifierTable.AddPoint(20, 7);
            //profileDataModifierTable.AddPoint(30, 13);
            //profileDataModifierTable.AddPoint(40, 21);
            //profileDataModifierTable.AddPoint(50, 30);
            //profileDataModifierTable.AddPoint(60, 41);
            //profileDataModifierTable.AddPoint(70, 54);
            //profileDataModifierTable.AddPoint(80, 68);
            //profileDataModifierTable.AddPoint(90, 83);
            //profileDataModifierTable.AddPoint(100, 100);

            if(fan == null && systemFan is DisplaySystemFan displaySystemFan)
            {
                displaySystemFan.SetLocation(result as SystemComponent);
            }

            return result;
        }
    }
}
