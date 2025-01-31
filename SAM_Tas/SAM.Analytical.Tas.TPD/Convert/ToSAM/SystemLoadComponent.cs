using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemLoadComponent ToSAM(this LoadComponent loadComponent)
        {
            if (loadComponent == null)
            {
                return null;
            }

            dynamic @dynamic = loadComponent;

            SystemLoadComponent result = new SystemLoadComponent(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Load = ((ProfileData)@dynamic.Load).ToSAM();

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemLoadComponent displaySystemLoadComponent = Systems.Create.DisplayObject<DisplaySystemLoadComponent>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemLoadComponent != null)
            {
                ITransform2D transform2D = ((ISystemComponent)loadComponent).Transform2D();
                if (transform2D != null)
                {
                    displaySystemLoadComponent.Transform(transform2D);
                }

                result = displaySystemLoadComponent;
            }

            return result;
        }
    }
}