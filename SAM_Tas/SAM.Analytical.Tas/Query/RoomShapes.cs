using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.RoomShape> RoomShapes(this TBD.room room)
        {
            List<TBD.RoomShape> result = new List<TBD.RoomShape>();

            int index = 0;
            TBD.RoomShape roomShape = room.GetRoomShape(index);
            while (roomShape != null)
            {
                result.Add(roomShape);
                index++;

                roomShape = room.GetRoomShape(index);
            }

            return result;
        }
    }
}