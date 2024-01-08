using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static AirSystemGroup ToSAM(this ComponentGroup componentGroup)
        {
            if (componentGroup == null)
            {
                return null;
            }

            dynamic @dynamic = componentGroup;

            AirSystemGroup result = new AirSystemGroup(dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
