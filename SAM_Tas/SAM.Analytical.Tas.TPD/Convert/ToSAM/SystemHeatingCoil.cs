using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHeatingCoil ToSAM(this global::TPD.HeatingCoil heatingCoil)
        {
            if (heatingCoil == null)
            {
                return null;
            }

            dynamic @dynamic = heatingCoil;

            SystemHeatingCoil result = new SystemHeatingCoil(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;
            result.Duty = heatingCoil.Duty.ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemHeatingCoil displaySystemHeatingCoil = Systems.Create.DisplayObject<DisplaySystemHeatingCoil>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemHeatingCoil != null)
            {
                ITransform2D transform2D = ((ISystemComponent)heatingCoil).Transform2D();
                if (transform2D != null)
                {
                    displaySystemHeatingCoil.Transform(transform2D);
                }

                result = displaySystemHeatingCoil;
            }

            return result;
        }
    }
}
