using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CurveModifierVariableType ToSAM(this tpdProfileDataVariableType tpdProfileDataVariableType)
        {
            switch(tpdProfileDataVariableType)
            {
                case tpdProfileDataVariableType.tpdProfileDataVariableODB:
                    return CurveModifierVariableType.ODB;

                case tpdProfileDataVariableType.tpdProfileDataVariableOHumRat:
                    return CurveModifierVariableType.OHumRat;

                case tpdProfileDataVariableType.tpdProfileDataVariableORH:
                    return CurveModifierVariableType.ORH;

                case tpdProfileDataVariableType.tpdProfileDataVariableOWB:
                    return CurveModifierVariableType.OWB;

                case tpdProfileDataVariableType.tpdProfileDataVariableOEnthalpy:
                    return CurveModifierVariableType.OEnthalpy;

                case tpdProfileDataVariableType.tpdProfileDataVariableOPollutant:
                    return CurveModifierVariableType.OPollutant;

                case tpdProfileDataVariableType.tpdProfileDataVariableEDB:
                    return CurveModifierVariableType.EDB;

                case tpdProfileDataVariableType.tpdProfileDataVariableEHumRat:
                    return CurveModifierVariableType.EHumRat;

                case tpdProfileDataVariableType.tpdProfileDataVariableERH:
                    return CurveModifierVariableType.ERH;

                case tpdProfileDataVariableType.tpdProfileDataVariableEWB:
                    return CurveModifierVariableType.EWB;

                case tpdProfileDataVariableType.tpdProfileDataVariableEEnthalpy:
                    return CurveModifierVariableType.EEnthalpy;

                case tpdProfileDataVariableType.tpdProfileDataVariableEPollutant:
                    return CurveModifierVariableType.EPollutant;

                case tpdProfileDataVariableType.tpdProfileDataVariableEFlow:
                    return CurveModifierVariableType.EFlow;

                case tpdProfileDataVariableType.tpdProfileDataVariableEDB2:
                    return CurveModifierVariableType.EDB2;

                case tpdProfileDataVariableType.tpdProfileDataVariableEHumRat2:
                    return CurveModifierVariableType.EHumRat2;

                case tpdProfileDataVariableType.tpdProfileDataVariableERH2:
                    return CurveModifierVariableType.ERH2;

                case tpdProfileDataVariableType.tpdProfileDataVariableEWB2:
                    return CurveModifierVariableType.EWB2;

                case tpdProfileDataVariableType.tpdProfileDataVariableEEnthalpy2:
                    return CurveModifierVariableType.EEnthalpy2;

                case tpdProfileDataVariableType.tpdProfileDataVariableEPollutant2:
                    return CurveModifierVariableType.EPollutant2;

                case tpdProfileDataVariableType.tpdProfileDataVariableEFlow2:
                    return CurveModifierVariableType.EFlow2;

                case tpdProfileDataVariableType.tpdProfileDataVariableWTemp:
                    return CurveModifierVariableType.WTemp;

                case tpdProfileDataVariableType.tpdProfileDataVariableWFlow:
                    return CurveModifierVariableType.WFlow;

                case tpdProfileDataVariableType.tpdProfileDataVariableWTemp2:
                    return CurveModifierVariableType.WTemp2;

                case tpdProfileDataVariableType.tpdProfileDataVariableWFlow2:
                    return CurveModifierVariableType.WFlow2;

                case tpdProfileDataVariableType.tpdProfileDataVariablePartload:
                    return CurveModifierVariableType.Partload;

                case tpdProfileDataVariableType.tpdProfileDataVariableDemand:
                    return CurveModifierVariableType.Demand;

                case tpdProfileDataVariableType.tpdProfileDataVariableIrradiance:
                    return CurveModifierVariableType.Irradiance;

                case tpdProfileDataVariableType.tpdProfileDataVariableArea:
                    return CurveModifierVariableType.Area;

                case tpdProfileDataVariableType.tpdProfileDataVariableWindSpeed:
                    return CurveModifierVariableType.WindSpeed;

                case tpdProfileDataVariableType.tpdProfileDataVariableWindDirection:
                    return CurveModifierVariableType.WindDirection;

                case tpdProfileDataVariableType.tpdProfileDataVariableRPipeLength:
                    return CurveModifierVariableType.RPipeLength;

                case tpdProfileDataVariableType.tpdProfileDataVariableTotalInternal:
                    return CurveModifierVariableType.TotalInternal;

                case tpdProfileDataVariableType.tpdProfileDataVariableDeltaT:
                    return CurveModifierVariableType.DeltaT;

                case tpdProfileDataVariableType.tpdProfileDataVariableTotalIntCap:
                    return CurveModifierVariableType.TotalIntCap;

                case tpdProfileDataVariableType.tpdProfileDataVariableWTemp3:
                    return CurveModifierVariableType.WTemp3;

                case tpdProfileDataVariableType.tpdProfileDataVariableWFlow3:
                    return CurveModifierVariableType.WFlow3;

                case tpdProfileDataVariableType.tpdProfileDataVariableWPipeLength:
                    return CurveModifierVariableType.WPipeLength;

                case tpdProfileDataVariableType.tpdProfileDataVariableHeatPartload:
                    return CurveModifierVariableType.Partload;

                case tpdProfileDataVariableType.tpdProfileDataVariableCoolPartload:
                    return CurveModifierVariableType.CoolPartload;

                case tpdProfileDataVariableType.tpdProfileDataVariableControlSignal:
                    return CurveModifierVariableType.ControlSignal;

                case tpdProfileDataVariableType.tpdProfileDataVariableCloudCover:
                    return CurveModifierVariableType.CloudCover;

                case tpdProfileDataVariableType.tpdProfileDataVariableDiffuseRadiation:
                    return CurveModifierVariableType.DiffuseRadiation;

                case tpdProfileDataVariableType.tpdProfileDataVariableGlobalRadiation:
                    return CurveModifierVariableType.GlobalRadiation;

                case tpdProfileDataVariableType.tpdProfileDataVariableOWaterTemp:
                    return CurveModifierVariableType.OWaterTemp;

                case tpdProfileDataVariableType.tpdProfileDataVariableLoad:
                    return CurveModifierVariableType.Load;

                case tpdProfileDataVariableType.tpdProfileDataVariableLAST:
                    return CurveModifierVariableType.LAST;
            }

            throw new System.NotImplementedException();
        }
    }
}
