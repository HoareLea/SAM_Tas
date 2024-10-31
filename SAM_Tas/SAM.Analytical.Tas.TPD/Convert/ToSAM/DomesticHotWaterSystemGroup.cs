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
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            if (boundingBox2D == null)
            {
                boundingBox2D = Query.BoundingBox2D((PlantGroup)dHWGroup);
            }

            DisplayDomesticHotWaterSystemGroup displayDomesticHotWaterSystemGroup = new DisplayDomesticHotWaterSystemGroup(result, boundingBox2D);
            if(displayDomesticHotWaterSystemGroup != null)
            {
                result = displayDomesticHotWaterSystemGroup;
            }


            return result;
        }
    }
}
