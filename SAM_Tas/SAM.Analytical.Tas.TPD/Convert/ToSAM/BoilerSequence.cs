using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalSystemCollectionType ToSAM(this tpdElectricalGroupType tpdElectricalGroupType)
        {
            switch(tpdElectricalGroupType)
            {
                case tpdElectricalGroupType.tpdElectricalGroupEquipment:
                    return ElectricalSystemCollectionType.Equipment;

                case tpdElectricalGroupType.tpdElectricalGroupNone:
                    return ElectricalSystemCollectionType.None;

                case tpdElectricalGroupType.tpdElectricalGroupHeating:
                    return ElectricalSystemCollectionType.Heating;

                case tpdElectricalGroupType.tpdElectricalGroupLighting:
                    return ElectricalSystemCollectionType.Lighting;

                case tpdElectricalGroupType.tpdElectricalGroupFans:
                    return ElectricalSystemCollectionType.Fans;
            }

            throw new System.NotImplementedException();
        }
    }
}
