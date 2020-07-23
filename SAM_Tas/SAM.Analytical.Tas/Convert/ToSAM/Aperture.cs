using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Aperture ToSAM(this TAS3D.window window)
        {
            if (window == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, window);

            Aperture aperture = new Aperture(new ApertureConstruction(window.name, ApertureType.Window), null);
            aperture.Add(parameterSet);

            return aperture;
        }
    }
}
