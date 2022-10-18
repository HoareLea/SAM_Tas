using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> RemoveUnusedZones(this SAMT3DDocument sAMT3DDocument)
        {
            if (sAMT3DDocument == null)
                return null;

            return RemoveUnusedZones(sAMT3DDocument.T3DDocument);
        }

        public static List<Guid> RemoveUnusedZones(this TAS3D.Building building)
        {
            List<TAS3D.Zone> zones = building?.Zones();
            if (zones == null)
                return null;

            List<TAS3D.Zone> zones_ToRemove = zones.FindAll(x => x.isUsed == 0);

            List<Guid> guids = zones_ToRemove.ConvertAll(x => new Guid(x.GUID));

            zones_ToRemove.ForEach(x => x.Delete());

            return guids;
        }

        public static List<Guid> RemoveUnusedZones(this TAS3D.T3DDocument t3DDocument)
        {
            return RemoveUnusedZones(t3DDocument?.Building);
        }
    }
}