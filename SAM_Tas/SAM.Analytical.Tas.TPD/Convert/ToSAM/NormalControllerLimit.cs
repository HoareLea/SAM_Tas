using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static NormalControllerLimit ToSAM(this global::TPD.tpdSensorPresetType tpdSensorPresetType)
        {
            switch(tpdSensorPresetType)
            {
                case global::TPD.tpdSensorPresetType.tpdUpperLimit:
                    return NormalControllerLimit.Upper;

                case global::TPD.tpdSensorPresetType.tpdLowerLimit:
                    return NormalControllerLimit.Lower;

                case global::TPD.tpdSensorPresetType.tpdLowerAndUpperLimit:
                    return NormalControllerLimit.LowerAndUpper;
            }

            throw new System.NotImplementedException();
        }
    }
}
