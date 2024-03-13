using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFan ToSAM(this Fan fan)
        {
            if (fan == null)
            {
                return null;
            }

            dynamic @dynamic = fan;

            SystemFan systemFan = new SystemFan(@dynamic.Name);
            systemFan.Description = dynamic.Description;
            Modify.SetReference(systemFan, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemFan result = Systems.Create.DisplayObject<DisplaySystemFan>(systemFan, location, Systems.Query.DefaultDisplaySystemManager());

            switch(fan.DesignFlowType)
            {
                case tpdFlowRateType.tpdFlowRateValue:
                case tpdFlowRateType.tpdFlowRateNearestZoneFlowRate:
                case tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate:
                case tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir:
                case tpdFlowRateType.tpdFlowRateNearestZoneFreshAir:
                    result.DesignFlowRate = System.Convert.ToDouble(fan.DesignFlowRate.Value);
                    break;
            }

            result.OverallEfficiency = fan.OverallEfficiency.Value;
            result.HeatGainFactor = fan.HeatGainFactor;
            result.Pressure = fan.Pressure;

            ITransform2D transform2D = ((ISystemComponent)fan).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
