using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.IRoomSurface> RoomSurfaces(this TBD.IZoneSurface zoneSurface)
        {
            List<TBD.IRoomSurface> result = new List<TBD.IRoomSurface>();

            int index = 0;
            TBD.IRoomSurface roomSurface = zoneSurface.GetRoomSurface(index);
            while (roomSurface != null)
            {
                result.Add(roomSurface);
                index++;

                roomSurface = zoneSurface.GetRoomSurface(index);
            }

            return result;
        }
    }
}