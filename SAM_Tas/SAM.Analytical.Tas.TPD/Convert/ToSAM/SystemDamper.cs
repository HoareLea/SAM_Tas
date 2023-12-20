using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDamper ToSAM(this Damper damper)
        {
            if (damper == null)
            {
                return null;
            }

            dynamic @dynamic = damper;

            SystemDamper result = new SystemDamper(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
