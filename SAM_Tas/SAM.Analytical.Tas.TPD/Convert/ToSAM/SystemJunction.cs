using TPD;
using SAM.Core.Systems;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemJunction ToSAM(this Junction junction)
        {
            if (junction == null)
            {
                return null;
            }

            dynamic @dynamic = junction;

            SystemJunction result = new SystemJunction(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
