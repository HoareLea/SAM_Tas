using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSteamHumidifier ToSAM(this SteamHumidifier steamHumidifier)
        {
            if (steamHumidifier == null)
            {
                return null;
            }

            dynamic @dynamic = steamHumidifier;

            SystemSteamHumidifier result = new SystemSteamHumidifier(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
