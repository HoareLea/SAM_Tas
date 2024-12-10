using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SAM.Analytical.Systems.BoilerSequence ToSAM(this tpdBoilerSequence tpdBoilerSequence)
        {
            switch(tpdBoilerSequence)
            {
                case tpdBoilerSequence.tpdBoilerSequenceParallel:
                    return BoilerSequence.Parallel;

                case tpdBoilerSequence.tpdBoilerSequenceSerial:
                    return BoilerSequence.Serial;

                case tpdBoilerSequence.tpdBoilerSequenceStaging:
                    return BoilerSequence.Staging;
            }

            throw new System.NotImplementedException();
        }
    }
}
