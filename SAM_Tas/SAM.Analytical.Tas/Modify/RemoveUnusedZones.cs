using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> RemoveUnsusedZones(this SAMT3DDocument sAMT3DDocument)
        {
            if (sAMT3DDocument == null)
                return null;

            return RemoveUnsusedZones(sAMT3DDocument.T3DDocument);
        }

        public static List<Guid> RemoveUnsusedZones(this TAS3D.Building building)
        {
            List<TAS3D.Zone> zones = building?.Zones();
            if (zones == null)
                return null;

            List<TAS3D.Zone> zones_ToRemove = zones.FindAll(x => x.isUsed == 0);

            List<Guid> guids = zones_ToRemove.ConvertAll(x => new Guid(x.GUID));

            zones_ToRemove.ForEach(x => x.Delete());

            return guids;
        }

        public static List<Guid> RemoveUnsusedZones(this TAS3D.T3DDocument t3DDocument)
        {
            return RemoveUnsusedZones(t3DDocument?.Building);
        }
    }
}