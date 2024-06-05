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

            Point2D point2D = displaySystemExchanger.SystemGeometry?.Location?.ToTPD();
            if (point2D != null)
            {
                result.SetPosition(point2D.X, point2D.Y);
            }

            return result as Exchanger;
        }
    }
}
