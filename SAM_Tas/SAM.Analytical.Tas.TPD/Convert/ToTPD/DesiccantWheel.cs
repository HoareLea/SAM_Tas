using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DesiccantWheel ToTPD(this DisplaySystemDesiccantWheel displaySystemDesiccantWheel, global::TPD.System system, DesiccantWheel desiccantWheel = null)
        {
            if(displaySystemDesiccantWheel == null || system == null)
            {
                return null;
            }

            DesiccantWheel result = desiccantWheel;
            if(result == null)
            {
                result = system.AddDesiccantWheel();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemDesiccantWheel.Name;
            @dynamic.Description = displaySystemDesiccantWheel.Description;

            PlantRoom plantRoom = system.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom?.GetEnergyCentre();

            result.SensibleEfficiency?.Update(displaySystemDesiccantWheel.SensibleEfficiency, energyCentre);
            result.Reactivation?.Update(displaySystemDesiccantWheel.Reactivation, energyCentre);
            result.MinimumRH?.Update(displaySystemDesiccantWheel.MinimumRH, energyCentre);
            result.MaximumRH?.Update(displaySystemDesiccantWheel.MaximumRH, energyCentre);
            result.SensibleHEEfficiency?.Update(displaySystemDesiccantWheel.SensibleHEEfficiency, energyCentre);
            result.HESetpointMethod = displaySystemDesiccantWheel.HESetpointMethod.ToTPD();
            result.HESetpoint?.Update(displaySystemDesiccantWheel.HESetpoint, energyCentre);
            result.ElectricalLoad?.Update(displaySystemDesiccantWheel.ElectricalLoad, energyCentre);

            Modify.SetSchedule((SystemComponent)result, displaySystemDesiccantWheel.ScheduleName);

            CollectionLink collectionLink = displaySystemDesiccantWheel.GetValue<CollectionLink>(AirSystemComponentParameter.ElectricalCollection);
            if (collectionLink != null)
            {
                ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (electricalGroup != null)
                {
                    @dynamic.SetElectricalGroup1(electricalGroup);
                }
            }

            if(desiccantWheel == null)
            {
                displaySystemDesiccantWheel.SetLocation(result as SystemComponent);
            }

            return result;
        }
    }
}
