using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPipeLossComponent ToSAM(this PipeLossComponent pipeLossComponent)
        {
            if (pipeLossComponent == null)
            {
                return null;
            }

            dynamic @dynamic = pipeLossComponent;

            SystemPipeLossComponent result = new SystemPipeLossComponent(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemPipeLossComponent displaySystemPipeLossComponent = Systems.Create.DisplayObject<DisplaySystemPipeLossComponent>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemPipeLossComponent != null)
            {
                ITransform2D transform2D = ((IPlantComponent)pipeLossComponent).Transform2D();
                if (transform2D != null)
                {
                    displaySystemPipeLossComponent.Transform(transform2D);
                }

                result = displaySystemPipeLossComponent;
            }

            return result;
        }
    }
}
