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

            result.SetValue(TasSettingParameter.DefaultTCRFileName, "Calendars.tcr");
            result.SetValue(TasSettingParameter.DefaultTICFileName, "NCMActivities_v6.1.b (Part L 2021).tic");

            TypeMap typeMap = Core.Create.TypeMap();
            //mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "Colour", "colour");
            //mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "Description", "description");
           // mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "External", "external");
            //mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "GUID", "GUID");
            //mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "FloorArea", "floorArea");
            //mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "Volume", "volume");

            //new Revit Type added by MD 2020-07-22
            typeMap.Add(typeof(Space), typeof(TAS3D.Zone), "SAM_SpaceColor", "colour"); //to be added to Parameters
            typeMap.Add(typeof(Space), typeof(TAS3D.Zone), "SAM_SpaceDescription", "description");
            typeMap.Add(typeof(Space), typeof(TAS3D.Zone), "SAM_ExternalZone", "external");
            typeMap.Add(typeof(Space), typeof(TAS3D.Zone), "SAM_SpaceGUID", "GUID");
            typeMap.Add(typeof(Space), typeof(TAS3D.Zone), "SAM_Area", "floorArea"); //SAM_T_Area
            typeMap.Add(typeof(Space), typeof(TAS3D.Zone), "SAM_Volume", "volume"); //SAM_T_Volume
            //mapCluster.Add(typeof(Space), typeof(TAS3D.Zone), "SAM_Height", "height"); //SAM_T_Height to be added volume/area to review correct geometry
            
            
            /*
            SAM_SpaceDescription
            SAM_SpaceColor
            SAM_ExternalZone
            SAM_FacingExternal
            SAM_FacingExternalGlazing
            SAM_SpaceGUID
            SAM_Area
            SAM_Volume
            SAM_Height
            */

            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Colour", "colour");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Description", "description");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "InternalShadows", "internalShadows");
            typeMap.Add(typeof(Panel), typeof(TAS3D.Element), "SecondaryProportion", "secondaryProportion");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Transparent", "transparent");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "Width", "width");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "GHost", "ghost");
            typeMap.Add(typeof(Panel), typeof(TAS3D.Element), "FloorArea", "zoneFloorArea");
            //mapCluster.Add(typeof(Panel), typeof(TAS3D.Element), "GUID", "GUID");
            typeMap.Add(typeof(Panel), typeof(TAS3D.Element), "IsPreset", "isPreset");

            //new Revit Type added by MD 2020-07-22
            typeMap.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementColor", "colour");
            typeMap.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementDescription", "description");
            typeMap.Add(typeof(Panel), typeof(TAS3D.Element), "SAM_BuildingElementGUID", "GUID");
            typeMap.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementInternalShadows", "internalShadows");
            typeMap.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementTransparent", "transparent");
            typeMap.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementThickness", "width");
            typeMap.Add(typeof(Construction), typeof(TAS3D.Element), "SAM_BuildingElementAir", "ghost");
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

            //mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Colour", "colour");
            //mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Description", "description");
            //mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "InternalShadows", "internalShadows");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "FrameDepth", "frameDepth");
            //mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "FrameWidth", "frameWidth");
            //mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Transparent", "transparent");
            //mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Width", "width");
            //mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "Height", "height");
            //mapCluster.Add(typeof(Aperture), typeof(TAS3D.window), "GUID", "GUID");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "IsPreset", "isPreset");

            //new Revit Type added by MD 2020-07-22
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "SAM_BuildingElementColor", "colour");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "SAM_BuildingElementDescription", "description");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "SAM_BuildingElementInternalShadows", "internalShadows");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "SAM_BuildingElementFrameWidth", "frameWidth");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "SAM_BuildingElementTransparent", "transparent");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "SAM_BuildingElementWidth", "width");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "SAM_BuildingElementHeight", "height");
            typeMap.Add(typeof(Aperture), typeof(TAS3D.window), "SAM_BuildingElementGUID", "GUID");


            typeMap.Add(typeof(IRelationCluster), typeof(TAS3D.Building), "Description", "description");
            typeMap.Add(typeof(IRelationCluster), typeof(TAS3D.Building), "Latitude", "latitude");  //this is based on SAM_FutureWeatherFile
            typeMap.Add(typeof(IRelationCluster), typeof(TAS3D.Building), "Longitude", "longitude");//this is based on SAM_FutureWeatherFile
            //mapCluster.Add(typeof(RelationCluster), typeof(TAS3D.Building), "NorthAngle", "northAngle");
            typeMap.Add(typeof(IRelationCluster), typeof(TAS3D.Building), "TimeZone", "timeZone");//this is based on SAM_FutureWeatherFile
            typeMap.Add(typeof(IRelationCluster), typeof(TAS3D.Building), "GUID", "GUID");
            typeMap.Add(typeof(IRelationCluster), typeof(TAS3D.Building), "Year", "year");//this is based on SAM_FutureWeatherFile

            //new Revit Type added by MD 2020-07-22
            typeMap.Add(typeof(IRelationCluster), typeof(TAS3D.Building), "SAM_NorthAngle", "northAngle");
            typeMap.Add(typeof(IRelationCluster), typeof(TAS3D.Building), "SAM_Elevation", "elevation"); //not sure if exisit already

            



            typeMap.Add(typeof(GuidCollection), typeof(TAS3D.zoneSet), "Description", "description");

            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "Colour", "colour");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "Description", "description");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "CentreOffset", "centreOffset");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "FrameDepth", "frameDepth");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "FrameOffset", "frameOffset");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "FrameWidth", "frameWidth");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "Height", "height");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "InternalShadows", "internalShadows");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "Level", "level");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "Transparent", "transparent");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "Width", "width");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "FrameGUID", "frameGUID");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "HorizfinsGUID", "horizfinsGUID");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "VertfinsGUID", "vertfinsGUID");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "hasFrame", "HasFrame");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "HasHorizFins", "hasHorizFins");
            typeMap.Add(typeof(Panel), typeof(TAS3D.shade), "HasVertFins", "hasVertFins");

            typeMap.Add(SpaceParameter.ZoneGuid, typeof(TSD.ZoneData), "zoneGUID");
            typeMap.Add(typeof(Space), typeof(TSD.ZoneData), "ZoneNumber", "zoneNumber");
            typeMap.Add(typeof(Space), typeof(TSD.ZoneData), "Description", "description");
            typeMap.Add(typeof(Space), typeof(TSD.ZoneData), "Volume", "volume");
            typeMap.Add(typeof(Space), typeof(TSD.ZoneData), "FloorArea", "floorArea");
            typeMap.Add(typeof(Space), typeof(TSD.ZoneData), "ConvectiveCommonRatio", "convectiveCommonRatio");
            typeMap.Add(typeof(Space), typeof(TSD.ZoneData), "RadiantCommonRatio", "radiantCommonRatio");

            typeMap.Add(typeof(Panel), typeof(TSD.SurfaceData), "SurfaceNumber", "surfaceNumber");
            typeMap.Add(typeof(Panel), typeof(TSD.SurfaceData), "Area", "area");
            typeMap.Add(typeof(Panel), typeof(TSD.SurfaceData), "Orientation", "orientation");

            typeMap.Add(SpaceSimulationResultParameter.ZoneNumber, typeof(TSD.ZoneData), "zoneNumber");
            typeMap.Add(SpaceSimulationResultParameter.ZoneGuid, typeof(TSD.ZoneData), "zoneGUID");


            result.Add(Core.Tas.ActiveSetting.Name.ParameterMap, typeMap);

            return result;
        }
    }
}