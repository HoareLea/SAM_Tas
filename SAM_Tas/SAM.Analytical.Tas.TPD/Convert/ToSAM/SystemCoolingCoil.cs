using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCoolingCoil ToSAM(this CoolingCoil coolingCoil)
        {
            if (coolingCoil == null)
            {
                return null;
            }

            dynamic @dynamic = coolingCoil;

            SystemCoolingCoil systemCoolingCoil = new SystemCoolingCoil(@dynamic.Name);
            systemCoolingCoil.Description = dynamic.Description;
            Modify.SetReference(systemCoolingCoil, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemCoolingCoil result = Systems.Create.DisplayObject<DisplaySystemCoolingCoil>(systemCoolingCoil, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)coolingCoil).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
