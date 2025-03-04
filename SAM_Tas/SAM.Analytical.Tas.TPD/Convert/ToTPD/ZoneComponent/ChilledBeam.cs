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
            
            chilledBeam.Flags = systemChilledBeam.Heating ? 1 : 0;
            chilledBeam.HeatingDuty?.Update(systemChilledBeam.HeatingDuty, energyCentre);
            chilledBeam.CoolingDuty?.Update(systemChilledBeam.CoolingDuty, energyCentre);
            chilledBeam.BypassFactor?.Update(systemChilledBeam.BypassFactor, energyCentre);
            chilledBeam.HeatingEfficiency?.Update(systemChilledBeam.HeatingEfficiency, energyCentre);
            chilledBeam.DesignFlowRate?.Update(systemChilledBeam.DesignFlowRate, energyCentre);
            chilledBeam.DesignFlowType = systemChilledBeam.DesignFlowType.ToTPD();
            chilledBeam.ZonePosition = systemChilledBeam.ZonePosition.ToTPD();

            Modify.SetSchedule((ZoneComponent)chilledBeam, systemChilledBeam.ScheduleName);

            CollectionLink collectionLink;

            collectionLink = systemChilledBeam.GetValue<CollectionLink>(SystemChilledBeamParameter.CoolingCollection);
            if (collectionLink != null)
            {
                CoolingGroup coolingGroup = plantRoom?.CoolingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (coolingGroup != null)
                {
                    ((dynamic)chilledBeam).SetCoolingGroup(coolingGroup);
                }
            }

            collectionLink = systemChilledBeam.GetValue<CollectionLink>(SystemChilledBeamParameter.HeatingCollection);
            if (collectionLink != null)
            {
                HeatingGroup heatingGroup = plantRoom?.HeatingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (heatingGroup != null)
                {
                    ((dynamic)chilledBeam).SetHeatingGroup(heatingGroup);
                }
            }

            return chilledBeam;
        }
    }
}
