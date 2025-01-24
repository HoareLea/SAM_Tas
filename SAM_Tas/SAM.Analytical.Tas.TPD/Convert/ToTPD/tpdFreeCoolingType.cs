using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdFreeCoolingType ToTPD(this FreeCoolingType freeCoolingType)
        {
            switch (freeCoolingType)
            {
                case FreeCoolingType.None:
                    return tpdFreeCoolingType.tpdFreeCoolingNone;

                case FreeCoolingType.OnOff:
                    return tpdFreeCoolingType.tpdFreeCoolingOnOff;

                case FreeCoolingType.Variable:
                    return tpdFreeCoolingType.tpdFreeCoolingVariable;

            }

            throw new System.NotImplementedException();
        }
    }
}
