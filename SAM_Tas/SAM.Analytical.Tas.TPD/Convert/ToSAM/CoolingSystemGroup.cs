using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CoolingSystemGroup ToSAM(this CoolingGroup coolingGroup, BoundingBox2D boundingBox2D = null)
        {
            if (coolingGroup == null)
            {
                return null;
            }

            dynamic @dynamic = coolingGroup;

            CoolingSystemGroup result = new CoolingSystemGroup(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            if (boundingBox2D == null)
            {
                boundingBox2D = Query.BoundingBox2D((PlantGroup)coolingGroup);
            }

            result = new DisplayCoolingSystemGroup(result, boundingBox2D);

            return result;
        }
    }
}