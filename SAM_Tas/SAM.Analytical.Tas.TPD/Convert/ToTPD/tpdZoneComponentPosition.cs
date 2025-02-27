using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdZoneComponentPosition ToTPD(this SystemSpaceComponentPosition SystemSpaceComponentPosition)
        {
            switch (SystemSpaceComponentPosition)
            {
                case SystemSpaceComponentPosition.InZone:
                    return tpdZoneComponentPosition.tpdZoneComponentInZone;

                case SystemSpaceComponentPosition.ParallelTerminalUnit:
                    return tpdZoneComponentPosition.tpdZoneComponentParallelTerminalUnit;

                case SystemSpaceComponentPosition.TerminalUnit:
                    return tpdZoneComponentPosition.tpdZoneComponentTerminalUnit;
            }

            throw new System.NotImplementedException();
        }
    }
}
