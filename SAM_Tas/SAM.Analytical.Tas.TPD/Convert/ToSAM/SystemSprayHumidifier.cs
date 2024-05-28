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

            SystemSprayHumidifier systemSprayHumidifier = new SystemSprayHumidifier(@dynamic.Name);
            systemSprayHumidifier.Description = dynamic.Description;
            Modify.SetReference(systemSprayHumidifier, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSprayHumidifier result = Systems.Create.DisplayObject<DisplaySystemSprayHumidifier>(systemSprayHumidifier, location, Systems.Query.DefaultDisplaySystemManager());
            result.Setpoint = sprayHumidifier.Setpoint.ToSAM();
            result.Effectiveness = sprayHumidifier.Effectiveness.ToSAM();
            result.WaterFlowCapacity = sprayHumidifier.WaterFlowCapacity.ToSAM();
            result.ElectricalLoad = sprayHumidifier?.ElectricalLoad.ToSAM();

            ITransform2D transform2D = ((ISystemComponent)sprayHumidifier).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
