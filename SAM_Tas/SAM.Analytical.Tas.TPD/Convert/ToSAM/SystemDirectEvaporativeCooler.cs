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

            SystemDirectEvaporativeCooler systemDirectEvaporativeCooler = new SystemDirectEvaporativeCooler(@dynamic.Name);
            systemDirectEvaporativeCooler.Description = dynamic.Description;
            Modify.SetReference(systemDirectEvaporativeCooler, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDirectEvaporativeCooler result = Systems.Create.DisplayObject<DisplaySystemDirectEvaporativeCooler>(systemDirectEvaporativeCooler, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)sprayHumidifier).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
