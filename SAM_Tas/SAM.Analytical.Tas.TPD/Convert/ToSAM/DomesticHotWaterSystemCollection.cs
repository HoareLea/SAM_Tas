using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DomesticHotWaterSystemCollection ToSAM(this DHWGroup dHWGroup)
        {
            if (dHWGroup == null)
            {
                return null;
            }

            dynamic @dynamic = dHWGroup;

            DomesticHotWaterSystemCollection result = new DomesticHotWaterSystemCollection(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplayDomesticHotWaterSystemCollection displayDomesticHotWaterSystemCollection = Systems.Create.DisplayObject<DisplayDomesticHotWaterSystemCollection>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displayDomesticHotWaterSystemCollection != null)
            {
                ITransform2D transform2D = ((IPlantComponent)dHWGroup).Transform2D();
                if (transform2D != null)
                {
                    displayDomesticHotWaterSystemCollection.Transform(transform2D);
                }

                result = displayDomesticHotWaterSystemCollection;
            }

            return result;
        }
    }
}