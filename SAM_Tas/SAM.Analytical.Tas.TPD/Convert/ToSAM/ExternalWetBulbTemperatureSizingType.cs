using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ExternalWetBulbTemperatureSizingType ToSAM_ExternalWetBulbTemperatureSizingType(this global::TPD.tpdSizedVariable tpdSizedVariable)
        {
            switch(tpdSizedVariable)
            {
                case global::TPD.tpdSizedVariable.tpdSizedVariableSize:
                    return ExternalWetBulbTemperatureSizingType.MaxOperating;

                case global::TPD.tpdSizedVariable.tpdSizedVariableSizeDone:
                    return ExternalWetBulbTemperatureSizingType.MaxOperating;

                case global::TPD.tpdSizedVariable.tpdSizedVariableValue:
                    return ExternalWetBulbTemperatureSizingType.Value;

                case global::TPD.tpdSizedVariable.tpdSizedVariableNone:
                    return ExternalWetBulbTemperatureSizingType.PeakExternal;
            }

            throw new System.NotImplementedException();
        }
    }
}
