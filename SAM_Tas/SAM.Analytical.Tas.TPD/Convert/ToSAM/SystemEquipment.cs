using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEquipment ToSAM(this ZoneComponent zoneComponent)
        {
            if (zoneComponent == null)
            {
                return null;
            }

            if(zoneComponent is FanCoilUnit)
            {
                return ((FanCoilUnit)zoneComponent).ToSAM();
            }

            if (zoneComponent is Radiator)
            {
                return ((Radiator)zoneComponent).ToSAM();
            }

            return null;
        }
    }
}
