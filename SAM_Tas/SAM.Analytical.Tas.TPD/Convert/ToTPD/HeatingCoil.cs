using SAM.Analytical.Systems;
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

            global::TPD.HeatingCoil result = system.AddHeatingCoil();

            dynamic @dynamic = result;

            result.Setpoint?.Update(displaySystemHeatingCoil.Setpoint);
            result.Efficiency?.Update(displaySystemHeatingCoil.Efficiency);
            result.Duty?.Update(displaySystemHeatingCoil.Duty);
            result.MaximumOffcoil?.Update(displaySystemHeatingCoil.MaximumOffcoil);


            //result.Setpoint.Value = 14;
            //result.Duty.Type = tpdSizedVariable.tpdSizedVariableSize;
            //result.Duty.SizeFraction = 1.0;
            //result.MaximumOffcoil.Value = 28;

            displaySystemHeatingCoil.SetLocation(result as SystemComponent);

            if (heatingGroup != null)
            {
                @dynamic.SetHeatingGroup(heatingGroup);
            }

            if(designConditionLoad != null)
            {
                @dynamic.Duty.AddDesignCondition(designConditionLoad);
            }

            return result;
        }
    }
}
