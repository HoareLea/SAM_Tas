using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.System ToTPD(this AirSystem airSystem, PlantRoom plantRoom)
        {
            if(airSystem == null || plantRoom == null)
            {
                return null;
            }

            global::TPD.System result = plantRoom.AddSystem();
            result.Name = airSystem.Name;
            result.Multiplicity = 1;//zoneLoads.Count();

            return result;
        }
    }
}
