using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DXCoilUnit ToTPD(this SystemDXCoilUnit systemDXCoilUnit, SystemZone systemZone)
        {
            if(systemDXCoilUnit == null || systemZone == null)
            {
                return null;
            }

            PlantRoom plantRoom = ((dynamic)systemZone).GetSystem()?.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom?.GetEnergyCentre();

            DXCoilUnit dXCoilUnit = systemZone.AddDXCoilUnit();
            ((dynamic)dXCoilUnit).Name = systemDXCoilUnit.Name;
            ((dynamic)dXCoilUnit).Description = systemDXCoilUnit.Description;
            dXCoilUnit.HeatingDuty?.Update(systemDXCoilUnit.HeatingDuty, energyCentre);
            dXCoilUnit.CoolingDuty?.Update(systemDXCoilUnit.CoolingDuty, energyCentre);
            dXCoilUnit.BypassFactor?.Update(systemDXCoilUnit.BypassFactor, energyCentre);
            dXCoilUnit.OverallEfficiency?.Update(systemDXCoilUnit.OverallEfficiency, energyCentre);
            dXCoilUnit.HeatGainFactor = systemDXCoilUnit.HeatGainFactor;
            dXCoilUnit.Pressure = systemDXCoilUnit.Pressure;
            dXCoilUnit.DesignFlowRate?.Update(systemDXCoilUnit.DesignFlowRate, energyCentre);
            dXCoilUnit.DesignFlowType = systemDXCoilUnit.DesignFlowType.ToTPD();
            dXCoilUnit.MinimumFlowRate?.Update(systemDXCoilUnit.MinimumFlowRate, energyCentre);
            dXCoilUnit.MinimumFlowType = systemDXCoilUnit.MinimumFlowType.ToTPD();
            dXCoilUnit.ZonePosition = systemDXCoilUnit.ZonePosition.ToTPD();
            dXCoilUnit.ControlMethod = systemDXCoilUnit.ControlMethod.ToTPD();
            dXCoilUnit.PartLoad?.Update(systemDXCoilUnit.PartLoad, energyCentre);

            Modify.SetSchedule((ZoneComponent)dXCoilUnit, systemDXCoilUnit.ScheduleName);

            CollectionLink collectionLink;

            collectionLink = systemDXCoilUnit.GetValue<CollectionLink>(SystemDXCoilUnitParameter.RefrigerantCollection);
            if (collectionLink != null)
            {
                RefrigerantGroup refrigerantGroup = plantRoom?.RefrigerantGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (refrigerantGroup != null)
                {
                    ((dynamic)dXCoilUnit).SetRefrigeranGroup(refrigerantGroup);
                }
            }

            collectionLink = systemDXCoilUnit.GetValue<CollectionLink>(SystemDXCoilUnitParameter.ElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = plantRoom?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    ((dynamic)dXCoilUnit).SetElectricalGroup1(electricalGroup);
                }
            }

            return dXCoilUnit;
        }
    }
}
