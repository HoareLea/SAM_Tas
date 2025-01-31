using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.CoolingCoil ToTPD(this DisplaySystemCoolingCoil displaySystemCoolingCoil, global::TPD.System system)
        {
            if(displaySystemCoolingCoil == null || system == null)
            {
                return null;
            }

            global::TPD.CoolingCoil result = system.AddCoolingCoil();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemCoolingCoil.Name;
            @dynamic.Description = displaySystemCoolingCoil.Description;

            result.Setpoint?.Update(displaySystemCoolingCoil.Setpoint);
            result.BypassFactor?.Update(displaySystemCoolingCoil.BypassFactor);
            result.Duty?.Update(displaySystemCoolingCoil.Duty, system);
            result.MinimumOffcoil?.Update(displaySystemCoolingCoil.MinimumOffcoil);

            //result.Setpoint?.Update(displaySystemCoolingCoil.Setpoint);

            Modify.SetSchedule((SystemComponent)result, displaySystemCoolingCoil.ScheduleName);

            displaySystemCoolingCoil.SetLocation(result as SystemComponent);


            CollectionLink collectionLink = displaySystemCoolingCoil.GetValue<CollectionLink>(SystemCoolingCoilParameter.CoolingCollection);
            if(collectionLink != null)
            {
                CoolingGroup coolingGroup = system.GetPlantRoom()?.CoolingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if(coolingGroup != null)
                {
                    @dynamic.SetCoolingGroup(coolingGroup);
                }
            }

            return result;
        }
    }
}
