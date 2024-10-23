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

            if (boundingBox2D == null)
            {
                boundingBox2D = Query.BoundingBox2D((PlantGroup)dHWGroup);
            }

            result = new DisplayDomesticHotWaterSystemGroup(result, boundingBox2D);

            return result;
        }
    }
}
