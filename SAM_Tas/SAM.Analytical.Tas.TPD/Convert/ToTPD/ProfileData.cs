using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ProfileData ToTPD(this Core.ModifiableValue modifiableValue)
        {
            if (modifiableValue == null)
            {
                return null;
            }

            ProfileData result = new ProfileData();
            result.Value = modifiableValue.Value;
            result.AddModifier(modifiableValue.Modifier);

            return result;
        }
    }
}
