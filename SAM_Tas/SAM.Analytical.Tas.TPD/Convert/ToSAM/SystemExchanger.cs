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

            SystemExchanger result = new SystemExchanger(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            result = Systems.Create.DisplayObject<DisplaySystemExchanger>(result, location, Systems.Query.DefaultDisplaySystemManager());

            return result;
        }
    }
}
