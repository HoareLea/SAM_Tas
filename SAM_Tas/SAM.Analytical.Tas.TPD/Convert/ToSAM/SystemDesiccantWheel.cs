using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDesiccantWheel ToSAM(this DesiccantWheel desiccantWheel)
        {
            if (desiccantWheel == null)
            {
                return null;
            }

            dynamic @dynamic = desiccantWheel;

            SystemDesiccantWheel result = new SystemDesiccantWheel(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
