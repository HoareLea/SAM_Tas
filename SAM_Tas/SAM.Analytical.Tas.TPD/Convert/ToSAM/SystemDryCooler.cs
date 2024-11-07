using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDryCooler ToSAM(this DryCooler dryCooler)
        {
            if (dryCooler == null)
            {
                return null;
            }

            dynamic @dynamic = dryCooler;

            SystemDryCooler result = new SystemDryCooler(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDryCooler displaySystemDryCooler = Systems.Create.DisplayObject<DisplaySystemDryCooler>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemDryCooler != null)
            {
                ITransform2D transform2D = ((IPlantComponent)dryCooler).Transform2D();
                if (transform2D != null)
                {
                    displaySystemDryCooler.Transform(transform2D);
                }

                result = displaySystemDryCooler;
            }

            return result;
        }
    }
}

