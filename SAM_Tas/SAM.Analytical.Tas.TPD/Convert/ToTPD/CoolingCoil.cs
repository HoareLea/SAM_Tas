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

            dynamic result = system.AddCoolingCoil();

            result.Duty.Type = tpdSizedVariable.tpdSizedVariableSize;
            result.Duty.SizeFraction = 1.0;
            result.BypassFactor.Value = displaySystemCoolingCoil.BypassFactor;
            result.MinimumOffcoil.Value = 16;

            displaySystemCoolingCoil.SetLocation(result as SystemComponent);

            if (coolingGroup != null)
            {
                result.SetCoolingGroup(coolingGroup);
            }

            if(designConditionLoad != null)
            {
                result.Duty.AddDesignCondition(designConditionLoad);
            }

            return result as global::TPD.CoolingCoil;
        }
    }
}
