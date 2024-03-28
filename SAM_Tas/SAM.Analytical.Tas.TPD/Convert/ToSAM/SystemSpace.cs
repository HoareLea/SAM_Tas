using TPD;
using System.Linq;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSpace ToSAM(this SystemZone systemZone)
        {
            if (systemZone == null)
            {
                return null;
            }

            string name = null;
            double area = double.NaN;
            double volume = double.NaN;

            ZoneLoad zoneLoad = systemZone.ZoneLoads()?.FirstOrDefault();
            if (zoneLoad != null)
            {
                name = zoneLoad.Name;
                area = zoneLoad.FloorArea;
                volume = zoneLoad.Volume;
            }

            dynamic @dynamic = systemZone as dynamic;

            double flowRate = double.NaN;
            if(systemZone.FlowRate.Type != tpdSizedVariable.tpdSizedVariableNone)
            {
                flowRate = systemZone.FlowRate.Value;
            }

            double freshAirRate = double.NaN;
            if (systemZone.FreshAir.Type != tpdSizedVariable.tpdSizedVariableNone)
            {
                freshAirRate = systemZone.FreshAir.Value;
            }

            SystemSpace systemSpace = new SystemSpace(name, area, volume, flowRate, freshAirRate);
            systemSpace.Description = dynamic.Description;
            Modify.SetReference(systemSpace, dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemSpace result = Systems.Create.DisplayObject<DisplaySystemSpace>(systemSpace, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)systemZone).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
