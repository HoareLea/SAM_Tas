using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFan ToSAM(this Fan fan)
        {
            if (fan == null)
            {
                return null;
            }

            dynamic @dynamic = fan;

            SystemFan result = new SystemFan(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
