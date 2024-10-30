using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemTank ToSAM(this Tank tank)
        {
            if (tank == null)
            {
                return null;
            }

            dynamic @dynamic = tank;

            SystemTank systemTank = new SystemTank(@dynamic.Name);
            systemTank.Description = dynamic.Description;
            Modify.SetReference(systemTank, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemTank result = Systems.Create.DisplayObject<DisplaySystemTank>(systemTank, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)tank).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
