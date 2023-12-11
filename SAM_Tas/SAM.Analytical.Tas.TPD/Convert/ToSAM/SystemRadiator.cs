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

            SystemRadiator result = new SystemRadiator(string.Empty) 
            { 
            };
            
            result.SetReference(((ZoneComponent)radiator).Reference());

            return result;
        }
    }
}
