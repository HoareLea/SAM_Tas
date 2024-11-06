using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWindTurbine ToSAM(this WindTurbine windTurbine)
        {
            if (windTurbine == null)
            {
                return null;
            }

            dynamic @dynamic = windTurbine;

            SystemWindTurbine result = new SystemWindTurbine(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemWindTurbine displaySystemWindTurbine = Systems.Create.DisplayObject<DisplaySystemWindTurbine>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemWindTurbine != null)
            {
                ITransform2D transform2D = ((IPlantComponent)windTurbine).Transform2D();
                if (transform2D != null)
                {
                    displaySystemWindTurbine.Transform(transform2D);
                }

                result = displaySystemWindTurbine;
            }

            return result;
        }
    }
}