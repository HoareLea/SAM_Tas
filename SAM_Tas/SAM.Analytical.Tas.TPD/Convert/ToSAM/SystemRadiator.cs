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

            double duty = System.Convert.ToDouble((radiator.Duty as dynamic).Value);
            double efficiency = System.Convert.ToDouble((radiator.Efficiency as dynamic).Value);

            SystemRadiator result = new SystemRadiator(string.Empty) 
            { 
                Duty = duty,
                Efficiency = efficiency,
            };
            
            result.SetReference(((ZoneComponent)radiator).Reference());

            return result;
        }
    }
}
