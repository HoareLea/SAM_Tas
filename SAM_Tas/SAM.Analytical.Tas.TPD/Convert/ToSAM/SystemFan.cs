using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFan ToSAM(this global::TPD.Fan fan)
        {
            if (fan == null)
            {
                return null;
            }

            dynamic @dynamic = fan;

            SystemFan result = new SystemFan(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.OverallEfficiency = fan.OverallEfficiency.Value;
            result.HeatGainFactor = fan.HeatGainFactor;
            result.Pressure = fan.Pressure;

            switch (fan.DesignFlowType)
            {
                case tpdFlowRateType.tpdFlowRateValue:
                case tpdFlowRateType.tpdFlowRateNearestZoneFlowRate:
                case tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate:
                case tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir:
                case tpdFlowRateType.tpdFlowRateNearestZoneFreshAir:
                    result.DesignFlowRate = System.Convert.ToDouble(fan.DesignFlowRate.Value);
                    break;
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemFan displaySystemFan = Systems.Create.DisplayObject<DisplaySystemFan>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemFan != null)
            {

                ITransform2D transform2D = ((ISystemComponent)fan).Transform2D();
                if (transform2D != null)
                {
                    displaySystemFan.Transform(transform2D);
                }

                result = displaySystemFan;
            }

            return result;
        }
    }
}
