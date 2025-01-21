using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdBoilerSequence ToTPD(this EquipmentSequence equipmentSequence)
        {
            switch (equipmentSequence)
            {
                case EquipmentSequence.Parallel:
                    return tpdBoilerSequence.tpdBoilerSequenceParallel;

                case EquipmentSequence.Serial:
                    return tpdBoilerSequence.tpdBoilerSequenceSerial;

                case EquipmentSequence.Staging:
                    return tpdBoilerSequence.tpdBoilerSequenceStaging;
            }

            throw new System.NotImplementedException();
        }
    }
}
