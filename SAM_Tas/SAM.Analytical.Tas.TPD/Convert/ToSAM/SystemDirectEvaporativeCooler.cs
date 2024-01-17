using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDirectEvaporativeCooler ToSAM_SystemDirectEvaporativeCooler(this SprayHumidifier sprayHumidifier)
        {
            if (sprayHumidifier == null)
            {
                return null;
            }

            dynamic @dynamic = sprayHumidifier;

            SystemDirectEvaporativeCooler result = new SystemDirectEvaporativeCooler(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
