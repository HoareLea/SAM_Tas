using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DomesticHotWaterSystemGroup ToSAM(this DHWGroup dHWGroup, BoundingBox2D boundingBox2D = null)
        {
            if (dHWGroup == null)
            {
                return null;
            }

            dynamic @dynamic = dHWGroup;

            DomesticHotWaterSystemGroup result = new DomesticHotWaterSystemGroup(dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            Point2D min = location;
            Point2D max = new Point2D(min.X + 0.6, min.Y + 1);

            result = new DisplayDomesticHotWaterSystemGroup(result, new BoundingBox2D(min, max));

            return result;
        }
    }
}
