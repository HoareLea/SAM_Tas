using TPD;
using SAM.Analytical.Systems;

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
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
