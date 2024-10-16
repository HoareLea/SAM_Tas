using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPump ToSAM(this Pump pump)
        {
            if (pump == null)
            {
                return null;
            }

            dynamic @dynamic = pump;

            SystemPump systemPump = new SystemPump(@dynamic.Name);
            systemPump.Description = dynamic.Description;
            Modify.SetReference(systemPump, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemPump result = Systems.Create.DisplayObject<DisplaySystemPump>(systemPump, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)pump).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
