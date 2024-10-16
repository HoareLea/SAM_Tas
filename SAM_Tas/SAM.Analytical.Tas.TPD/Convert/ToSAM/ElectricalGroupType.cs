using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalGroupType ToSAM(this tpdElectricalGroupType tpdElectricalGroupType)
        {
            switch(tpdElectricalGroupType)
            {
                case tpdElectricalGroupType.tpdElectricalGroupEquipment:
                    return ElectricalGroupType.Equipment;

                case tpdElectricalGroupType.tpdElectricalGroupNone:
                    return ElectricalGroupType.None;

                case tpdElectricalGroupType.tpdElectricalGroupHeating:
                    return ElectricalGroupType.Heating;

                case tpdElectricalGroupType.tpdElectricalGroupLighting:
                    return ElectricalGroupType.Lighting;

                case tpdElectricalGroupType.tpdElectricalGroupFans:
                    return ElectricalGroupType.Fans;
            }

            return ElectricalGroupType.Undefined;
        }
    }
}
