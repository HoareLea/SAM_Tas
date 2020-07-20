using SAM.Core;
using System.Reflection;

namespace SAM.Analytical.Tas
{
    public static partial class ActiveSetting
    {
        public static class Name
        {
            //public const string ParameterMap = "Parameter Map";
        }

        private static Setting setting = Load();

        private static Setting Load()
        {
            Setting setting = ActiveManager.GetSetting(Assembly.GetExecutingAssembly());
            if (setting == null)
                setting = GetDefault();

            return setting;
        }

        public static Setting Setting
        {
            get
            {
                return setting;
            }
        }

        public static Setting GetDefault()
        {
            Setting result = new Setting(Assembly.GetExecutingAssembly());

            MapCluster mapCluster = new MapCluster();
            mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "Colour", "colour");
            mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "Description", "description");
            mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "External", "external");
            mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "GUID", "GUID");
            mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "FloorArea", "floorArea");
            mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "Volume", "volume");

            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Colour", "colour");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Description", "description");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "InternalShadows", "internalShadows");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "SecondaryProportion", "secondaryProportion");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Transparent", "transparent");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Width", "width");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "GHost", "ghost");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "FloorArea", "zoneFloorArea");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "GUID", "GUID");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "IsPreset", "isPreset");

            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Colour", "colour");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Description", "description");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "InternalShadows", "internalShadows");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "FrameDepth", "frameDepth");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "FrameWidth", "frameWidth");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Transparent", "transparent");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Width", "width");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Height", "height");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "GUID", "GUID");
            mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "IsPreset", "isPreset");

            mapCluster.Add(typeof(RelationCluster), typeof(TAS3D.Building), "Description", "description");
            mapCluster.Add(typeof(RelationCluster), typeof(TAS3D.Building), "Latitude", "latitude");
            mapCluster.Add(typeof(RelationCluster), typeof(TAS3D.Building), "Longitude", "longitude");
            mapCluster.Add(typeof(RelationCluster), typeof(TAS3D.Building), "NorthAngle", "northAngle");
            mapCluster.Add(typeof(RelationCluster), typeof(TAS3D.Building), "TimeZone", "timeZone");
            mapCluster.Add(typeof(RelationCluster), typeof(TAS3D.Building), "GUID", "GUID");
            mapCluster.Add(typeof(RelationCluster), typeof(TAS3D.Building), "Year", "year");

            result.Add(Core.Tas.ActiveSetting.Name.ParameterMap, mapCluster);

            return setting;
        }
    }
}