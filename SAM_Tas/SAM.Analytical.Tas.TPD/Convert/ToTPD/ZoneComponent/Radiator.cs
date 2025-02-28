using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Radiator ToTPD(this SystemRadiator systemRadiator, SystemZone systemZone)
        {
            if(systemRadiator == null || systemZone == null)
            {
                return null;
            }

            PlantRoom plantRoom = ((dynamic)systemZone).GetSystem()?.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom?.GetEnergyCentre();

            Radiator radiator = systemZone.AddRadiator();
            ((dynamic)radiator).Name = systemRadiator.Name;
            ((dynamic)radiator).Description = systemRadiator.Description;

            radiator.Duty?.Update(systemRadiator.Duty, energyCentre);
            radiator.Efficiency?.Update(systemRadiator.Efficiency, energyCentre);

            Modify.SetSchedule((ZoneComponent)radiator, systemRadiator.ScheduleName);

            CollectionLink collectionLink = systemRadiator.GetValue<CollectionLink>(SystemRadiatorParameter.HeatingCollection);
            if (collectionLink != null)
            {
                HeatingGroup heatingGroup = plantRoom?.HeatingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (heatingGroup != null)
                {
                    ((dynamic)radiator).SetHeatingGroup(heatingGroup);
                }
            }

            return radiator;
        }
    }
}
