using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDirectEvaporativeCooler ToSAM_SystemDirectEvaporativeCooler(this SprayHumidifier sprayHumidifier)
        {
            if (sprayHumidifier == null)
            {
                return null;
            }

            dynamic @dynamic = sprayHumidifier;

            SystemDirectEvaporativeCooler result = new SystemDirectEvaporativeCooler(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.Setpoint = sprayHumidifier.Setpoint.ToSAM();
            result.Effectiveness = sprayHumidifier.Effectiveness.ToSAM();
            result.WaterFlowCapacity = sprayHumidifier.WaterFlowCapacity.ToSAM();
            result.ElectricalLoad = sprayHumidifier.ElectricalLoad.ToSAM();
            result.HoursBeforePurgingTank = System.Convert.ToDouble(sprayHumidifier.TankHours);
            result.TankVolume = sprayHumidifier.TankVolume.ToSAM();

            result.ScheduleName = ((dynamic)sprayHumidifier)?.GetSchedule()?.Name;

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)sprayHumidifier);
            if (collectionLink != null)
            {
                result.SetValue(AirSystemComponentParameter.ElectricalCollection, collectionLink);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDirectEvaporativeCooler displaySystemDirectEvaporativeCooler = Systems.Create.DisplayObject<DisplaySystemDirectEvaporativeCooler>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemDirectEvaporativeCooler != null)
            {
                ITransform2D transform2D = ((ISystemComponent)sprayHumidifier).Transform2D();
                if (transform2D != null)
                {
                    displaySystemDirectEvaporativeCooler.Transform(transform2D);
                }

                result = displaySystemDirectEvaporativeCooler;
            }

            return result;
        }
    }
}
