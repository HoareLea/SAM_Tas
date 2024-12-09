using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdElectricalGroupType ToTPD(this ElectricalSystemCollectionType electricalSystemCollectionType)
        {
            switch (electricalSystemCollectionType)
            {
                case ElectricalSystemCollectionType.Fans:
                    return tpdElectricalGroupType.tpdElectricalGroupFans;

                case ElectricalSystemCollectionType.Equipment:
                    return tpdElectricalGroupType.tpdElectricalGroupEquipment;

                case ElectricalSystemCollectionType.Heating:
                    return tpdElectricalGroupType.tpdElectricalGroupHeating;

                case ElectricalSystemCollectionType.None:
                    return tpdElectricalGroupType.tpdElectricalGroupNone;

                case ElectricalSystemCollectionType.Lighting:
                    return tpdElectricalGroupType.tpdElectricalGroupLighting;
            }

            throw new System.NotImplementedException();
        }
    }
}
