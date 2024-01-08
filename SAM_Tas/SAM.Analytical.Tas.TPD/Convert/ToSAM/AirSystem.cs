using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static AirSystem ToSAM(this global::TPD.ISystem system)
        {
            if (system == null)
            {
                return null;
            }

            dynamic @dynamic = system;

            AirSystem result = new AirSystem(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            return result;
        }
    }
}
