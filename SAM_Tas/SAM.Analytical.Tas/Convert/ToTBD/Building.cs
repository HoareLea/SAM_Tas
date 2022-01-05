using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {

        public static TBD.Building ToTBD(this AnalyticalModel analyticalModel, TBD.TBDDocument tBDDocument)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            TBD.Building building = tBDDocument.Building;
            if(building == null)
            {
                return null;
            }

            List<Space> spaces = analyticalModel.GetSpaces();
            if(spaces == null)
            {
                return building;
            }

            foreach(Space space in spaces)
            {
                space.ToTBD(analyticalModel, building);
            }

            return building;
        }
    }
}
