using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static RelationCluster<IJSAMObject> ToSAM(this TAS3D.Building building)
        {
            if (building == null)
                return null;

            Setting setting = ActiveSetting.Setting;

            Dictionary<string, ISAMObject> dictionary = null;

            RelationCluster<IJSAMObject> result = new RelationCluster<IJSAMObject>();

            dictionary = new Dictionary<string, ISAMObject>();

            List<TAS3D.Zone> zones = Query.Zones(building);
            if(zones != null)
            {
                foreach(TAS3D.Zone zone in zones)
                {
                    Space space = zone.ToSAM();
                    if (space != null)
                    {
                        result.AddObject(space);
                        dictionary[zone.GUID] = space;
                    }
                        
                }
            }

            List<TAS3D.zoneSet> zoneSets = Query.ZoneSets(building);
            if(zoneSets != null)
            {
                foreach (TAS3D.zoneSet zoneSet in zoneSets)
                {
                    List<ISAMObject> sAMObjects = zoneSet?.Zones()?.ConvertAll(x => dictionary[x.GUID]);

                    Group group = new Group(zoneSet.name);
                    group.Add(Create.ParameterSet(setting, zoneSet));

                    result.AddObject(group);
                    sAMObjects?.ForEach(x => result.AddRelation(group, x));
                }
            }

            dictionary = new Dictionary<string, ISAMObject>();

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

            List<TAS3D.Element> elements = Query.Elements(building);
            if (elements != null)
            {
                foreach (TAS3D.Element element in elements)
                {
                    Panel panel = element.ToSAM();
                    if (panel != null)
                    {
                        result.AddObject(panel);
                        dictionary[element.GUID] = panel;
                    }
                }
            }

            List<TAS3D.shade> shades = Query.Shades(building);
            if(shades != null)
            {
                foreach (TAS3D.shade shade in shades)
                {
                    Panel panel = shade.ToSAM();
                    if (panel != null)
                        result.AddObject(panel);
                }
            }

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, building);
            result.Add(parameterSet);

            return result;
        }
    }
}
