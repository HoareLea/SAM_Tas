using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static TemperatureSizingType ToSAM_TemperatureSizingType(this global::TPD.tpdSizedVariable tpdSizedVariable)
        {
            switch(tpdSizedVariable)
            {
                case global::TPD.tpdSizedVariable.tpdSizedVariableSize:
                    return TemperatureSizingType.MaxOperating;

                case global::TPD.tpdSizedVariable.tpdSizedVariableSizeDone:
                    return TemperatureSizingType.MaxOperating;

                case global::TPD.tpdSizedVariable.tpdSizedVariableValue:
                    return TemperatureSizingType.Value;

                case global::TPD.tpdSizedVariable.tpdSizedVariableNone:
                    return TemperatureSizingType.PeakExternal;
            }

            throw new System.NotImplementedException();
        }
    }
}
