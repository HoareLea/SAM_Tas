using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
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

            dynamic result = system.AddExchanger();
            result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            result.LatentEfficiency.Value = displaySystemExchanger.LatentEfficiency;
            result.SensibleEfficiency.Value = displaySystemExchanger.SensibleEfficiency;
            result.Setpoint.Value = 14;
            result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            Point2D location = displaySystemExchanger.SystemGeometry?.Location;
            result.SetPosition(location.X, location.Y);

            return result as Exchanger;
        }
    }
}
