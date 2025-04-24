using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FanCoilUnit ToTPD(this SystemFanCoilUnit systemFanCoilUnit, SystemZone systemZone)
        {
            if(systemFanCoilUnit == null || systemZone == null)
            {
                return null;
            }

            PlantRoom plantRoom = ((dynamic)systemZone).GetSystem()?.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom?.GetEnergyCentre();

            FanCoilUnit fanCoilUnit = systemZone.AddFanCoilUnit();
            ((dynamic)fanCoilUnit).Name = systemFanCoilUnit.Name;
            ((dynamic)fanCoilUnit).Description = systemFanCoilUnit.Description;
            fanCoilUnit.HeatingDuty?.Update(systemFanCoilUnit.HeatingDuty, energyCentre);
            fanCoilUnit.CoolingDuty?.Update(systemFanCoilUnit.CoolingDuty, energyCentre);
            fanCoilUnit.BypassFactor?.Update(systemFanCoilUnit.BypassFactor, energyCentre);
            fanCoilUnit.HeatingEfficiency?.Update(systemFanCoilUnit.HeatingEfficiency, energyCentre);
            fanCoilUnit.OverallEfficiency?.Update(systemFanCoilUnit.OverallEfficiency, energyCentre);
            fanCoilUnit.HeatGainFactor = systemFanCoilUnit.HeatGainFactor;
            fanCoilUnit.Pressure = systemFanCoilUnit.Pressure;
            fanCoilUnit.DesignFlowRate?.Update(systemFanCoilUnit.DesignFlowRate, energyCentre);
            fanCoilUnit.DesignFlowType = systemFanCoilUnit.DesignFlowType.ToTPD();
            fanCoilUnit.MinimumFlowRate?.Update(systemFanCoilUnit.MinimumFlowRate, energyCentre);
            fanCoilUnit.MinimumFlowType = systemFanCoilUnit.MinimumFlowType.ToTPD();
            fanCoilUnit.ZonePosition = systemFanCoilUnit.ZonePosition.ToTPD();
            fanCoilUnit.ControlMethod = systemFanCoilUnit.ControlMethod.ToTPD();
            fanCoilUnit.PartLoad?.Update(systemFanCoilUnit.PartLoad, energyCentre);

            Modify.SetSchedule((ZoneComponent)fanCoilUnit, systemFanCoilUnit.ScheduleName);

            CollectionLink collectionLink;

            collectionLink = systemFanCoilUnit.GetValue<CollectionLink>(SystemFanCoilUnitParameter.CoolingCollection);
            if (collectionLink != null)
            {
                CoolingGroup coolingGroup = plantRoom?.CoolingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (coolingGroup != null)
                {
                    ((dynamic)fanCoilUnit).SetCoolingGroup(coolingGroup);
                }
            }

            collectionLink = systemFanCoilUnit.GetValue<CollectionLink>(SystemFanCoilUnitParameter.HeatingCollection);
            if (collectionLink != null)
            {
                HeatingGroup heatingGroup = plantRoom?.HeatingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (heatingGroup != null)
                {
                    ((dynamic)fanCoilUnit).SetHeatingGroup(heatingGroup);
                }
            }

            collectionLink = systemFanCoilUnit.GetValue<CollectionLink>(SystemFanCoilUnitParameter.ElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = plantRoom?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    ((dynamic)fanCoilUnit).SetElectricalGroup1(electricalGroup);
                }
            }

            return fanCoilUnit;
        }
    }
}
