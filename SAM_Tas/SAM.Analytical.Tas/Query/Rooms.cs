using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.room> Rooms(this TBD.zone zone)
        {
            List<TBD.room> result = new List<TBD.room>();

            int index = 0;
            TBD.room room = zone.GetRoom(index);
            while (room != null)
            {
                result.Add(room);
                index++;

                room = zone.GetRoom(index);
            }

            return result;
        }
    }
}