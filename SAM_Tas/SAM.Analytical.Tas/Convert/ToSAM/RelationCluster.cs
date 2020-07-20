using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static RelationCluster ToSAM(this TAS3D.Building building)
        {
            if (building == null)
                return null;

            RelationCluster result = new RelationCluster();

            List<TAS3D.Zone> zones = Query.Zones(building);
            if(zones != null)
            {
                foreach(TAS3D.Zone zone in zones)
                {
                    Space space = zone.ToSAM();
                    if (space != null)
                        result.AddObject(space);
                }
            }

            List<TAS3D.Element> elements = Query.Elements(building);
            if(elements != null)
            {
                foreach (TAS3D.Element element in elements)
                {
                    Panel panel = element.ToSAM();
                    if (panel != null)
                        result.AddObject(panel);
                }
            }

            List<TAS3D.window> windows = Query.Windows(building);
            if (windows != null)
            {
                foreach (TAS3D.window widnow in windows)
                {
                    Aperture aperture = widnow.ToSAM();
                    if (aperture != null)
                        result.AddObject(aperture);
                }
            }

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, building);
            result.Add(parameterSet);

            return result;
        }
    }
}
