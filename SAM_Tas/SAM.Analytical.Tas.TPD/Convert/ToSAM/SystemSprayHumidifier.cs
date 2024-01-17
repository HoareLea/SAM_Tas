using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSprayHumidifier ToSAM(this SprayHumidifier sprayHumidifier)
        {
            if (sprayHumidifier == null)
            {
                return null;
            }

            dynamic @dynamic = sprayHumidifier;

            SystemSprayHumidifier result = new SystemSprayHumidifier(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
