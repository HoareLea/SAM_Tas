using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalSystemCollection ToSAM(this ElectricalGroup electricalGroup)
        {
            if (electricalGroup == null)
            {
                return null;
            }

            dynamic @dynamic = electricalGroup;

            ElectricalSystemCollection result = new ElectricalSystemCollection(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplayElectricalSystemCollection displayElectricalSystemCollection = Systems.Create.DisplayObject<DisplayElectricalSystemCollection>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displayElectricalSystemCollection != null)
            {
                ITransform2D transform2D = ((IPlantComponent)electricalGroup).Transform2D();
                if (transform2D != null)
                {
                    displayElectricalSystemCollection.Transform(transform2D);
                }

                result = displayElectricalSystemCollection;
            }

            return result;
        }
    }
}