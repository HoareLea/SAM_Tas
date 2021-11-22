using SAM.Core.Tas;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {      
        public static AnalyticalModel ToSAM(this SAMTBDDocument sAMTBDDocument)
        {
            if(sAMTBDDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTBDDocument.TBDDocument);

        }

        public static AnalyticalModel ToSAM(this TBD.TBDDocument tBDDocument)
        {
            if(tBDDocument == null)
            {
                return null;
            }

            return ToSAM_AnalyticalModel(tBDDocument.Building);
        }

        public static AnalyticalModel ToSAM_AnalyticalModel(this TBD.Building building)
        {
            if (building == null)
            {
                return null;
            }

            ProfileLibrary profileLibrary = building.ToSAM_ProfileLibrary();
            Core.MaterialLibrary materialLibrary = building.ToSAM_MaterialLibrary();

            Core.Location location = new Core.Location(building.name, building.longitude, building.latitude, 0);
            Core.Address address = new Core.Address(null, null, null, Core.CountryCode.Undefined);

            return new AnalyticalModel(building.name, null, location, address, ToSAM(building), materialLibrary, profileLibrary);
        }
    }
}
