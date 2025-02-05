using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSprayHumidifier ToSAM_SystemSprayHumidifier(this SprayHumidifier sprayHumidifier)
        {
            if (sprayHumidifier == null)
            {
                return null;
            }

            dynamic @dynamic = sprayHumidifier;

            SystemSprayHumidifier result = new SystemSprayHumidifier(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.Setpoint = sprayHumidifier.Setpoint.ToSAM();
            result.Effectiveness = sprayHumidifier.Effectiveness.ToSAM();
            result.WaterFlowCapacity = sprayHumidifier.WaterFlowCapacity.ToSAM();
            result.ElectricalLoad = sprayHumidifier?.ElectricalLoad.ToSAM();

            result.ScheduleName = ((dynamic)sprayHumidifier )?.GetSchedule()?.Name;

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)sprayHumidifier);
            if (collectionLink != null)
            {
                result.SetValue(AirSystemComponentParameter.ElectricalCollection, collectionLink);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSprayHumidifier displaySystemSprayHumidifier = Systems.Create.DisplayObject<DisplaySystemSprayHumidifier>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemSprayHumidifier != null)
            {
                ITransform2D transform2D = ((ISystemComponent)sprayHumidifier).Transform2D();
                if (transform2D != null)
                {
                    displaySystemSprayHumidifier.Transform(transform2D);
                }

                result = displaySystemSprayHumidifier;
            }

            return result;
        }
    }
}
