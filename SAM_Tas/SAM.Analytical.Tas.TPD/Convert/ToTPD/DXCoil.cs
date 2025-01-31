using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DXCoil ToTPD(this DisplaySystemDXCoil displaySystemDXCoil, global::TPD.System system)
        {
            if(displaySystemDXCoil == null || system == null)
            {
                return null;
            }

            DXCoil result = system.AddDXCoil();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemDXCoil.Name;
            @dynamic.Description = displaySystemDXCoil.Description;

            result.CoolingSetpoint?.Update(displaySystemDXCoil.CoolingSetpoint);
            result.HeatingSetpoint?.Update(displaySystemDXCoil.HeatingSetpoint);
            result.MinimumOffcoil?.Update(displaySystemDXCoil.MinOffcoilTemperature);
            result.MaximumOffcoil?.Update(displaySystemDXCoil.MaxOffcoilTemperature);
            result.BypassFactor?.Update(displaySystemDXCoil.BypassFactor);
            result.CoolingDuty?.Update(displaySystemDXCoil.CoolingDuty, system);
            result.HeatingDuty?.Update(displaySystemDXCoil.HeatingDuty, system);

            Modify.SetSchedule((SystemComponent)result, displaySystemDXCoil.ScheduleName);

            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            SystemComponent systemComponent = result as SystemComponent;

            displaySystemDXCoil.SetLocation(systemComponent);

            return result;
        }
    }
}
