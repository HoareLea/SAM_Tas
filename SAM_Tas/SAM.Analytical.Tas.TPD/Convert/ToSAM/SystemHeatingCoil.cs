using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHeatingCoil ToSAM(this HeatingCoil heatingCoil)
        {
            if (heatingCoil == null)
            {
                return null;
            }

            dynamic @dynamic = heatingCoil;

            SystemHeatingCoil result = new SystemHeatingCoil(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
