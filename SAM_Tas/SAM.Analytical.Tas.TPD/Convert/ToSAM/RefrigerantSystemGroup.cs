using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static RefrigerantSystemGroup ToSAM(this RefrigerantGroup refrigerantGroup, BoundingBox2D boundingBox2D = null)
        {
            if (refrigerantGroup == null)
            {
                return null;
            }

            dynamic @dynamic = refrigerantGroup;

            RefrigerantSystemGroup result = new RefrigerantSystemGroup(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            if (boundingBox2D == null)
            {
                boundingBox2D = Query.BoundingBox2D((PlantGroup)refrigerantGroup);
            }

            DisplayRefrigerantSystemGroup displayRefrigerantSystemGroup = new DisplayRefrigerantSystemGroup(result, boundingBox2D);
            if(displayRefrigerantSystemGroup != null)
            {
                result = displayRefrigerantSystemGroup;
            }

            return result;
        }
    }
}