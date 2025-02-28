using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemRadiator ToSAM(this Radiator radiator)
        {
            if (radiator == null)
            {
                return null;
            }

            dynamic @dynamic = radiator;

            double duty = System.Convert.ToDouble((radiator.Duty as dynamic).Value);
            double efficiency = System.Convert.ToDouble((radiator.Efficiency as dynamic).Value);

            SystemRadiator result = new SystemRadiator(dynamic.Name)
            {
                Duty = radiator.Duty.ToSAM(),
                Efficiency = efficiency,
                Description = dynamic.Description,
                ScheduleName = @dynamic.GetSchedule()?.Name,
            };

            result.SetReference(((ZoneComponent)radiator).Reference());

            HeatingGroup heatingGroup = @dynamic.GetHeatingGroup();
            if (heatingGroup != null)
            {
                result.SetValue(SystemRadiatorParameter.HeatingCollection, new CollectionLink(CollectionType.Heating, ((dynamic)heatingGroup).Name));
            }

            return result;
        }
    }
}
