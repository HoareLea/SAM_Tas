using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEconomiser ToSAM_SystemEconomiser(this Optimiser optimizer)
        {
            if (optimizer == null)
            {
                return null;
            }

            dynamic @dynamic = optimizer;

            SystemEconomiser systemEconomiser = new SystemEconomiser(@dynamic.Name);
            systemEconomiser.Description = dynamic.Description;
            Modify.SetReference(systemEconomiser, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemEconomiser result = Systems.Create.DisplayObject<DisplaySystemEconomiser>(systemEconomiser, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)optimizer).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
