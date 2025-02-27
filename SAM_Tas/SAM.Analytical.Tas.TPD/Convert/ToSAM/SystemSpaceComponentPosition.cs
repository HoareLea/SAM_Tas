using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSpaceComponentPosition ToSAM(this tpdZoneComponentPosition tpdZoneComponentPosition)
        {
            switch(tpdZoneComponentPosition)
            {
                case tpdZoneComponentPosition.tpdZoneComponentInZone:
                    return SystemSpaceComponentPosition.InZone;

                case tpdZoneComponentPosition.tpdZoneComponentTerminalUnit:
                    return SystemSpaceComponentPosition.TerminalUnit;

                case tpdZoneComponentPosition.tpdZoneComponentParallelTerminalUnit:
                    return SystemSpaceComponentPosition.ParallelTerminalUnit;

            }

            throw new System.NotImplementedException();
        }
    }
}
