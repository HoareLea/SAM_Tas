using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Exchanger ToTPD(this DisplaySystemExchanger displaySystemExchanger, global::TPD.System system)
        {
            if(displaySystemExchanger == null || system == null)
            {
                return null;
            }

            Exchanger result = system.AddExchanger();

            dynamic @dynamic = result;
            result.ExchLatType = displaySystemExchanger.ExchangerLatentType.ToTPD();
            result.ExchangerType = displaySystemExchanger.ExchangerType.ToTPD();
            result.SensibleEfficiency?.Update(displaySystemExchanger.SensibleEfficiency);
            result.HeatTransSurfArea = displaySystemExchanger.HeatTransferSurfaceArea;
            result.HeatTransCoeff = displaySystemExchanger.HeatTransferCoefficient;
            result.ExchLatType = displaySystemExchanger.ExchangerLatentType.ToTPD();
            result.LatentEfficiency?.Update(displaySystemExchanger.LatentEfficiency);
            result.SetpointMethod = displaySystemExchanger.SetpointMode.ToTPD();
            result.Setpoint?.Update(displaySystemExchanger.Setpoint);
            result.ElectricalLoad?.Update(displaySystemExchanger.ElectricalLoad);
            result.Duty?.Update(displaySystemExchanger.Duty);
            result.BypassFactor?.Update(displaySystemExchanger.BypassFactor);

            //result.LatentEfficiency.Value = displaySystemExchanger.LatentEfficiency;
            //result.SensibleEfficiency.Value = displaySystemExchanger.SensibleEfficiency;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            displaySystemExchanger.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
