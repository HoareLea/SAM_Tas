using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdSensorPresetType ToTPD(this NormalControllerLimit normalControllerLimit)
        {
            switch (normalControllerLimit)
            {
                case NormalControllerLimit.Lower:
                    return tpdSensorPresetType.tpdLowerLimit;

                case NormalControllerLimit.Upper:
                    return tpdSensorPresetType.tpdUpperLimit;

                case NormalControllerLimit.LowerAndUpper:
                    return tpdSensorPresetType.tpdLowerAndUpperLimit;
            }

            throw new System.NotImplementedException();
        }
    }
}
