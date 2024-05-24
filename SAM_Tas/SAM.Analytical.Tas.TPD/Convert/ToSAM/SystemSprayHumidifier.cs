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

            ITransform2D transform2D = ((ISystemComponent)systemSprayHumidifier).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
