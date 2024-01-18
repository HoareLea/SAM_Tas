using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDXCoil ToSAM(this DXCoil dxCoil)
        {
            if (dxCoil == null)
            {
                return null;
            }

            dynamic @dynamic = dxCoil;

            SystemDXCoil result = new SystemDXCoil(dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
