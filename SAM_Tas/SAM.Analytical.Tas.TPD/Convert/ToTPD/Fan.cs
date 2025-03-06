using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.Fan ToTPD(this DisplaySystemFan displaySystemFan, global::TPD.System system, global::TPD.Fan fan = null)
        {
            if(displaySystemFan == null || system == null)
            {
                return null;
            }

            global::TPD.Fan result = fan;
            if(result == null)
            {
                result = system.AddFan();
            }
            
            dynamic @dynamic = result;
            @dynamic.name = displaySystemFan.Name;
            @dynamic.Description = displaySystemFan.Description;

            PlantRoom plantRoom = system.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.OverallEfficiency?.Update(displaySystemFan.OverallEfficiency, energyCentre);
            result.HeatGainFactor = displaySystemFan.HeatGainFactor;
            result.Pressure = displaySystemFan.Pressure;
            result.DesignFlowRate?.Update(displaySystemFan.DesignFlowRate, energyCentre);
            result.DesignFlowType = displaySystemFan.DesignFlowType.ToTPD();
            result.MinimumFlowRate?.Update(displaySystemFan.MinimumFlowRate, energyCentre);
            result.MinimumFlowType = displaySystemFan.MinimumFlowType.ToTPD();
            result.MinimumFlowFraction = displaySystemFan.MinimumFlowFraction;
            result.Capacity = displaySystemFan.Capacity;
            result.ControlType = displaySystemFan.FanControlType.ToTPD();
            result.PartLoad?.Update(displaySystemFan.PartLoad, energyCentre);

            Modify.SetSchedule((SystemComponent)result, displaySystemFan.ScheduleName);

            // result.DesignFlowRate.Value = displaySystemFan.DesignFlowRate;
            // result.OverallEfficiency.Value = displaySystemFan.OverallEfficiency;
            // result.Pressure = displaySystemFan.Pressure;
            // result.HeatGainFactor = displaySystemFan.HeatGainFactor;
            // result.PartLoad.Value = 0;
            // result.PartLoad.ClearModifiers();
            //// result.SetDirection(tpdDirection.tpdLeftRight);
            // result.DesignFlowType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;



            Modify.SetSchedule((SystemComponent)result, displaySystemFan.ScheduleName);

            CollectionLink collectionLink = displaySystemFan.GetValue<CollectionLink>(AirSystemComponentParameter.ElectricalCollection);
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

            if(fan == null)
            {
                displaySystemFan.SetLocation(result as SystemComponent);
            }

            return result;
        }
    }
}
