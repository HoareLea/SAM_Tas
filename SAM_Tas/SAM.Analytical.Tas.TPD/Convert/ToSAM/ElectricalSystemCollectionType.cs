using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static EquipmentSequence ToSAM(this tpdBoilerSequence tpdBoilerSequence)
        {
            switch(tpdBoilerSequence)
            {
                case tpdBoilerSequence.tpdBoilerSequenceParallel:
                    return EquipmentSequence.Parallel;

                case tpdBoilerSequence.tpdBoilerSequenceSerial:
                    return EquipmentSequence.Serial;

                case tpdBoilerSequence.tpdBoilerSequenceStaging:
                    return EquipmentSequence.Staging;
            }

            throw new System.NotImplementedException();
        }
    }
}
