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

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            Point2D min = location;
            Point2D max = new Point2D(min.X + 0.6, min.Y + 1);

            result = new DisplayElectricalSystemGroup(result, new BoundingBox2D(min, max));

            return result;
        }
    }
}
