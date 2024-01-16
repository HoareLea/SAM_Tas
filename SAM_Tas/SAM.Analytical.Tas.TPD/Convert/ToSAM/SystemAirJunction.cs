using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAirJunction ToSAM(this Junction junction)
        {
            if (junction == null)
            {
                return null;
            }

            dynamic @dynamic = junction;

            SystemAirJunction result = new SystemAirJunction(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
