using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FuelSystemCollection ToSAM(this FuelGroup fuelGroup)
        {
            if (fuelGroup == null)
            {
                return null;
            }

            dynamic @dynamic = fuelGroup;

            FuelSystemCollection result = new FuelSystemCollection(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplayFuelSystemCollection displayFuelSystemCollection = Systems.Create.DisplayObject<DisplayFuelSystemCollection>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displayFuelSystemCollection != null)
            {
                ITransform2D transform2D = ((IPlantComponent)fuelGroup).Transform2D();
                if (transform2D != null)
                {
                    displayFuelSystemCollection.Transform(transform2D);
                }

                result = displayFuelSystemCollection;
            }

            return result;
        }
    }
}