using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FanCoilControlMethod ToSAM(this tpdFancoilControlMethod tpdFancoilControlMethod)
        {
            switch(tpdFancoilControlMethod)
            {
                case tpdFancoilControlMethod.tpdFancoilControlCAV:
                    return FanCoilControlMethod.CAV;

                case tpdFancoilControlMethod.tpdFancoilControlVAV:
                    return FanCoilControlMethod.VAV;

                case tpdFancoilControlMethod.tpdFancoilControlOnOff:
                    return FanCoilControlMethod.OnOff;

            }

            throw new System.NotImplementedException();
        }
    }
}
