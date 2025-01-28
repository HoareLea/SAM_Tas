using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.CoolingCoil ToTPD(this DisplaySystemCoolingCoil displaySystemCoolingCoil, global::TPD.System system, CoolingGroup coolingGroup, DesignConditionLoad designConditionLoad)
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
            result.Duty?.Update(displaySystemCoolingCoil.Duty);
            result.MinimumOffcoil?.Update(displaySystemCoolingCoil.MinimumOffcoil);

            //result.Setpoint?.Update(displaySystemCoolingCoil.Setpoint);

            displaySystemCoolingCoil.SetLocation(result as SystemComponent);

            if (coolingGroup != null)
            {
                @dynamic.SetCoolingGroup(coolingGroup);
            }

            if(designConditionLoad != null)
            {
                result.Duty.AddDesignCondition(designConditionLoad);
            }

            return result;
        }
    }
}
