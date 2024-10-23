using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalSystemGroup ToSAM(this ElectricalGroup electricalGroup, BoundingBox2D boundingBox2D = null)
        {
            if (electricalGroup == null)
            {
                return null;
            }

            tpdElectricalGroupType tpdElectricalGroupType = electricalGroup.ElectricalGroupType;

            dynamic @dynamic = electricalGroup;

            ElectricalSystemGroup result = new ElectricalSystemGroup(dynamic.Name, tpdElectricalGroupType.ToSAM());
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            if (boundingBox2D == null)
            {
                boundingBox2D = Query.BoundingBox2D((PlantGroup)electricalGroup);
            }

            result = new DisplayElectricalSystemGroup(result, boundingBox2D);

            return result;
        }
    }
}

