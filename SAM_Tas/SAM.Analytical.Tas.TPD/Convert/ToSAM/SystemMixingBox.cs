using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMixingBox ToSAM_SystemMixingBox(this Optimiser optimizer)
        {
            if (optimizer == null)
            {
                return null;
            }

            dynamic @dynamic = optimizer;

            SystemMixingBox result = new SystemMixingBox(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
