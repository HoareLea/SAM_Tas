using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatingSystemGroup ToSAM(this HeatingGroup heatingGroup, BoundingBox2D boundingBox2D = null)
        {
            if (heatingGroup == null)
            {
                return null;
            }

            dynamic @dynamic = heatingGroup;

            HeatingSystemGroup result = new HeatingSystemGroup(dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            if (boundingBox2D == null)
            {
                boundingBox2D = Query.BoundingBox2D((PlantGroup)heatingGroup);
            }

            DisplayHeatingSystemGroup displayHeatingSystemGroup = new DisplayHeatingSystemGroup(result, boundingBox2D);
            if(displayHeatingSystemGroup != null)
            {
                result = displayHeatingSystemGroup;
            }

            return result;
        }
    }
}