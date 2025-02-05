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

            SystemFan result = new SystemFan(@dynamic.Name)
            {
                Description = dynamic.Description,
                OverallEfficiency = fan.OverallEfficiency?.ToSAM(),
                HeatGainFactor = fan.HeatGainFactor,
                Pressure = fan.Pressure,
                DesignFlowRate = fan.DesignFlowRate?.ToSAM(),
                DesignFlowType = fan.DesignFlowType.ToSAM(),
                MinimumFlowRate = fan.MinimumFlowRate?.ToSAM(),
                MinimumFlowType = fan.MinimumFlowType.ToSAM(),
                MinimumFlowFraction = fan.MinimumFlowFraction,
                Capacity = fan.Capacity,
                FanControlType = fan.ControlType.ToSAM(),
                PartLoad = fan.PartLoad?.ToSAM(),
            };


            Modify.SetReference(result, @dynamic.GUID);

            result.ScheduleName = ((dynamic)fan )?.GetSchedule()?.Name;

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)fan);
            if (collectionLink != null)
            {
                result.SetValue(AirSystemComponentParameter.ElectricalCollection, collectionLink);
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
