using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHeatingCoil ToSAM(this HeatingCoil heatingCoil)
        {
            if (heatingCoil == null)
            {
                return null;
            }

            dynamic @dynamic = heatingCoil;

            SystemHeatingCoil result = new SystemHeatingCoil(dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            Point2D location =((TasPosition)@dynamic.GetPosition())?.ToSAM();

            result = Systems.Create.DisplayObject<DisplaySystemHeatingCoil>(result, location, Systems.Query.DefaultDisplaySystemManager());


            return result;
        }
    }
}
