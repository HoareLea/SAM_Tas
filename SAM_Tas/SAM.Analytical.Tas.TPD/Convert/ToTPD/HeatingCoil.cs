using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.HeatingCoil ToTPD(this DisplaySystemHeatingCoil displaySystemHeatingCoil, global::TPD.System system, HeatingGroup heatingGroup, DesignConditionLoad designConditionLoad)
        {
            if(displaySystemHeatingCoil == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddHeatingCoil();
            result.Setpoint.Value = 14;
            result.Duty.Type = tpdSizedVariable.tpdSizedVariableSize;
            result.Duty.SizeFraction = 1.0;
            result.Duty.AddDesignCondition(designConditionLoad);
            result.MaximumOffcoil.Value = 28;

            displaySystemHeatingCoil.SetLocation(result as SystemComponent);

            if (heatingGroup != null)
            {
                result.SetHeatingGroup(heatingGroup);
            }

            if(designConditionLoad != null)
            {
                result.Duty.AddDesignCondition(designConditionLoad);
            }

            return result as global::TPD.HeatingCoil;
        }
    }
}
