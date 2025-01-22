using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdSetpointMethod ToTPD(this SetpointMode setpointMode)
        {
            switch (setpointMode)
            {
                case SetpointMode.On:
                    return tpdSetpointMethod.tpdSetpointOn;

                case SetpointMode.None:
                    return tpdSetpointMethod.tpdSetpointNone;

            }

            throw new System.NotImplementedException();
        }
    }
}
