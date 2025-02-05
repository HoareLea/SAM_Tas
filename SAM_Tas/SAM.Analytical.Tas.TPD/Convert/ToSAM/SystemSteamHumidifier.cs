using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSteamHumidifier ToSAM(this SteamHumidifier steamHumidifier)
        {
            if (steamHumidifier == null)
            {
                return null;
            }

            dynamic @dynamic = steamHumidifier;

            SystemSteamHumidifier result = new SystemSteamHumidifier(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.Duty = steamHumidifier.Duty.ToSAM();
            result.WaterSupplyTemperature = steamHumidifier.WaterSupplyTemp.ToSAM();
            result.Setpoint = steamHumidifier.Setpoint.ToSAM();

            result.ScheduleName = ((dynamic)steamHumidifier )?.GetSchedule()?.Name;

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)steamHumidifier);
            if (collectionLink != null)
            {
                result.SetValue(AirSystemComponentParameter.ElectricalCollection, collectionLink);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSteamHumidifier displaySystemSteamHumidifier = Systems.Create.DisplayObject<DisplaySystemSteamHumidifier>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemSteamHumidifier != null)
            {
                ITransform2D transform2D = ((ISystemComponent)steamHumidifier).Transform2D();
                if (transform2D != null)
                {
                    displaySystemSteamHumidifier.Transform(transform2D);
                }

                result = displaySystemSteamHumidifier;
            }

            return result;
        }
    }
}
