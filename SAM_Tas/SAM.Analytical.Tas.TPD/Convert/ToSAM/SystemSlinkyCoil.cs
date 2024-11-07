using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSlinkyCoil ToSAM(this SlinkyCoil slinkyCoil)
        {
            if (slinkyCoil == null)
            {
                return null;
            }

            dynamic @dynamic = slinkyCoil;

            SystemSlinkyCoil result = new SystemSlinkyCoil(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSlinkyCoil displaySystemSlinkyCoil = Systems.Create.DisplayObject<DisplaySystemSlinkyCoil>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemSlinkyCoil != null)
            {
                ITransform2D transform2D = ((IPlantComponent)slinkyCoil).Transform2D();
                if (transform2D != null)
                {
                    displaySystemSlinkyCoil.Transform(transform2D);
                }

                result = displaySystemSlinkyCoil;
            }

            return result;
        }
    }
}