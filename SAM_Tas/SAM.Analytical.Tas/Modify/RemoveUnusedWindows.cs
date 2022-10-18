using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> RemoveUnusedWindows(this SAMT3DDocument sAMT3DDocument)
        {
            if (sAMT3DDocument == null)
                return null;

            return RemoveUnusedWindows(sAMT3DDocument.T3DDocument);
        }

        public static List<Guid> RemoveUnusedWindows(this TAS3D.Building building)
        {
            List<TAS3D.window> windows = building?.Windows();
            if (windows == null)
                return null;

            List<TAS3D.window> windows_ToRemove = windows.FindAll(x => x.isUsed == 0);

            List<Guid> guids = windows_ToRemove.ConvertAll(x => new Guid(x.frameGUID));
            guids.AddRange(windows_ToRemove.ConvertAll(x => new Guid(x.paneGUID)));

            windows_ToRemove.ForEach(x => x.Delete());

            return guids;
        }

        public static List<Guid> RemoveUnusedWindows(this TAS3D.T3DDocument t3DDocument)
        {
            return RemoveUnusedWindows(t3DDocument?.Building);
        }
    }
}