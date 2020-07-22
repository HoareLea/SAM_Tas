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

            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Colour", "colour");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Description", "description");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "InternalShadows", "internalShadows");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "SecondaryProportion", "secondaryProportion");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Transparent", "transparent");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Width", "width");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "GHost", "ghost");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "FloorArea", "zoneFloorArea");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "GUID", "GUID");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "IsPreset", "isPreset");

            //new Revit Type added by MD
            mapCluster.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementColor", "colour");
            mapCluster.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementDescription", "description");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "SAM_BuildingElementGUID", "GUID");
            mapCluster.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementInternalShadows", "internalShadows");
            mapCluster.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementTransparent", "transparent");
            mapCluster.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementThickness", "width");
            mapCluster.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementAir", "ghost");
            /*
            SAM_BuildingElementAir
            SAM_BuildingElementDescription
            SAM_BuildingElementGround
            SAM_BuildingElementGUID
            SAM_BuildingElementInternalShadows
            SAM_BuildingElementThickness
            SAM_BuildingElementTransparent
            SAM_BuildingElementType
            */




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

            mapCluster.Add(typeof(GuidCollection), typeof(TAS3D.zoneSet), "Description", "description");

            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "Colour", "colour");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "Description", "description");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "CentreOffset", "centreOffset");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "FrameDepth", "frameDepth");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "FrameOffset", "frameOffset");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "FrameWidth", "frameWidth");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "Height", "height");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "InternalShadows", "internalShadows");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "Level", "level");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "Transparent", "transparent");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "Width", "width");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "FrameGUID", "frameGUID");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "HorizfinsGUID", "horizfinsGUID");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "VertfinsGUID", "vertfinsGUID");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "hasFrame", "HasFrame");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "HasHorizFins", "hasHorizFins");
            mapCluster.Add(typeof(Panel), typeof(TAS3D.shade), "HasVertFins", "hasVertFins");

            result.Add(Core.Tas.ActiveSetting.Name.ParameterMap, mapCluster);

            return setting;
        }
    }
}