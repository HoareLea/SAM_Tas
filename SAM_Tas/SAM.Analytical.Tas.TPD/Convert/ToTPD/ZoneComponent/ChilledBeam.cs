using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ChilledBeam ToTPD(this SystemChilledBeam systemChilledBeam, SystemZone systemZone)
        {
            if(systemChilledBeam == null || systemZone == null)
            {
                return null;
            }

            PlantRoom plantRoom = ((dynamic)systemZone).GetSystem()?.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom?.GetEnergyCentre();

            ChilledBeam chilledBeam = systemZone.AddChilledBeam();
            ((dynamic)chilledBeam).Name = systemChilledBeam.Name;
            ((dynamic)chilledBeam).Description = systemChilledBeam.Description;
            chilledBeam.HeatingDuty?.Update(systemChilledBeam.HeatingDuty, energyCentre);
            chilledBeam.CoolingDuty?.Update(systemChilledBeam.CoolingDuty, energyCentre);
            chilledBeam.BypassFactor?.Update(systemChilledBeam.BypassFactor, energyCentre);
            chilledBeam.HeatingEfficiency?.Update(systemChilledBeam.HeatingEfficiency, energyCentre);
            chilledBeam.DesignFlowRate?.Update(systemChilledBeam.DesignFlowRate);
            chilledBeam.DesignFlowType = systemChilledBeam.DesignFlowType.ToTPD();
            chilledBeam.ZonePosition = systemChilledBeam.ZonePosition.ToTPD();

            Modify.SetSchedule((ZoneComponent)chilledBeam, systemChilledBeam.ScheduleName);

            CollectionLink collectionLink = systemChilledBeam.GetValue<CollectionLink>(SystemChilledBeamParameter.CoolingCollection);
            if (collectionLink != null)
            {
                CoolingGroup coolingGroup = plantRoom?.CoolingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (coolingGroup != null)
                {
                    ((dynamic)chilledBeam).SetCoolingGroup(coolingGroup);
                }
            }

            return chilledBeam;
        }
    }
}
