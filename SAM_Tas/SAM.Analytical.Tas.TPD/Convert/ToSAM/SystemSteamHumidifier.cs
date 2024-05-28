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

            SystemSteamHumidifier systemSteamHumidifier = new SystemSteamHumidifier(@dynamic.Name);
            systemSteamHumidifier.Description = dynamic.Description;
            Modify.SetReference(systemSteamHumidifier, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSteamHumidifier result = Systems.Create.DisplayObject<DisplaySystemSteamHumidifier>(systemSteamHumidifier, location, Systems.Query.DefaultDisplaySystemManager());
            result.Duty = steamHumidifier.Duty.ToSAM();
            result.WaterSupplyTemperature = steamHumidifier.WaterSupplyTemp.ToSAM();
            result.Setpoint = steamHumidifier.Setpoint.ToSAM();

            ITransform2D transform2D = ((ISystemComponent)steamHumidifier).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
