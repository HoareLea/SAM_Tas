using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SizingType ToSAM(this global::TPD.tpdSizedVariable tpdSizedVariable)
        {
            switch(tpdSizedVariable)
            {
                case global::TPD.tpdSizedVariable.tpdSizedVariableSize:
                    return SizingType.Sized;

                case global::TPD.tpdSizedVariable.tpdSizedVariableSizeDone:
                    return SizingType.Sized;

                case global::TPD.tpdSizedVariable.tpdSizedVariableValue:
                    return SizingType.Value;

                case global::TPD.tpdSizedVariable.tpdSizedVariableNone:
                    return SizingType.None;
            }

            throw new System.NotImplementedException();
        }
    }
}
