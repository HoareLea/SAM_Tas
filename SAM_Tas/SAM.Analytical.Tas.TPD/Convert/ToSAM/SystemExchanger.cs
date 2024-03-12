using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemExchanger ToSAM(this Exchanger exchanger)
        {
            if (exchanger == null)
            {
                return null;
            }

            dynamic @dynamic = exchanger;

            SystemExchanger systemExchanger = new SystemExchanger(@dynamic.Name);
            systemExchanger.Description = dynamic.Description;
            Modify.SetReference(systemExchanger, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemExchanger result = Systems.Create.DisplayObject<DisplaySystemExchanger>(systemExchanger, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)exchanger).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
