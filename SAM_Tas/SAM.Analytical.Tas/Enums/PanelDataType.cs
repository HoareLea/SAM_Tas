using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    public enum PanelDataType
    {
        [Description("Undefined")] Undefined,
        [Description("Internal Temperature")]  InternalTemperature = 1,
        [Description("External Temperature")] ExternalTemperature = 2,
        [Description("Internal Solar Gain")] InternalSolarGain = 3,
        [Description("External Solar Gain")] ExternalSolarGain = 4,
        [Description("Aperture Flow In")] ApertureFlowIn = 5,
        [Description("Aperture Flow Out")] ApertureFlowOut = 6,
        [Description("Internal Condensation")] InternalCondensation = 7,
        [Description("External Condensation")] ExternalCondensation = 8,
        [Description("Internal Conduction")] InternalConduction = 9,
        [Description("External Conduction")] ExternalConduction = 10,
        [Description("Aperture Opening")] ApertureOpening = 11,
        [Description("Internal Long Wave")] InternalLongWave = 12,
        [Description("External Long Wave")] ExternalLongWave = 13,
        [Description("Internal Convection")] InternalConvection = 14,
        [Description("External Convection")] ExternalConvection = 15,
        [Description("Interstitial Condensation")] InterstitialCondensation = 16
    }
}
