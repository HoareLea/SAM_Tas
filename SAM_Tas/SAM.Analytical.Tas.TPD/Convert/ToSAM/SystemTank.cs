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

            SystemTank result = new SystemTank(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemTank displaySystemTank = Systems.Create.DisplayObject<DisplaySystemTank>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemTank != null)
            {
                ITransform2D transform2D = ((IPlantComponent)tank).Transform2D();
                if (transform2D != null)
                {
                    displaySystemTank.Transform(transform2D);
                }

                result = displaySystemTank;
            }

            return result;
        }
    }
}
