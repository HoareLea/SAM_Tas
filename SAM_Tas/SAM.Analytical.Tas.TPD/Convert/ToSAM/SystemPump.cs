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

            SystemPump result = new SystemPump(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemPump displaySystemPump = Systems.Create.DisplayObject<DisplaySystemPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)pump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemPump.Transform(transform2D);
                }

                result = displaySystemPump;
            }

            return result;
        }
    }
}