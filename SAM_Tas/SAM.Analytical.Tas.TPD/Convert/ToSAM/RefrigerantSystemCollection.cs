using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static RefrigerantSystemCollection ToSAM(this RefrigerantGroup refrigerantGroup)
        {
            if (refrigerantGroup == null)
            {
                return null;
            }

            dynamic @dynamic = refrigerantGroup;

            RefrigerantSystemCollection result = new RefrigerantSystemCollection(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.PipeLength = refrigerantGroup.PipeLength?.ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplayRefrigerantSystemCollection displayRefrigerantSystemCollection = Systems.Create.DisplayObject<DisplayRefrigerantSystemCollection>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displayRefrigerantSystemCollection != null)
            {
                ITransform2D transform2D = ((IPlantComponent)refrigerantGroup).Transform2D();
                if (transform2D != null)
                {
                    displayRefrigerantSystemCollection.Transform(transform2D);
                }

                result = displayRefrigerantSystemCollection;
            }

            return result;
        }
    }
}