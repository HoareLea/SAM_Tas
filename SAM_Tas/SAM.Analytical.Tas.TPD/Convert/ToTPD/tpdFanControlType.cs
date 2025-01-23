using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdFanControlType ToTPD(this FanControlType fanControlType)
        {
            switch (fanControlType)
            {
                case FanControlType.VariableSpeed:
                    return tpdFanControlType.tpdFanControlVariableSpeed;

                case FanControlType.FixedSpeed:
                    return tpdFanControlType.tpdFanControlFixedSpeed;

            }

            throw new System.NotImplementedException();
        }
    }
}
