using SAM.Core.Tas.UKBR;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateUKBRFile(this AnalyticalModel analyticalModel, string path)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return false;
            }

            bool result = false;
            using (UKBRFile uKBRFile  = new UKBRFile(path))
            {
                result = UpdateUKBRFile(analyticalModel, uKBRFile);
                uKBRFile.Close(result);
            }

            return result;
        }

        public static bool UpdateUKBRFile(this AnalyticalModel analyticalModel, UKBRFile uKBRFile)
        {
            if (analyticalModel == null || uKBRFile == null)
            {
                return false;
            }

            List<Space> spaces = analyticalModel.GetSpaces();
            if (spaces == null || spaces.Count == 0)
            {
                return false;
            }

            uKBRFile.Open();

            UKBRData uKBRData = uKBRFile.UKBRData;
            if (uKBRData == null)
            {
                return false;
            }

            Building building = uKBRData.Project.Building;

            LightSetups lightSetups = building.LightSetups;
            if (lightSetups == null)
            {
                return false;
            }

            bool result = false;
            foreach (LightSetup lightSetup in lightSetups)
            {
                List<Core.Tas.UKBR.Zone> zones = building.GetZones(lightSetup?.ZoneGUIDs);
                if(zones == null || zones.Count == 0)
                {
                    continue;
                }

                foreach(Core.Tas.UKBR.Zone zone in zones)
                {
                    LightingDetail lightingDetail = lightSetup.LightingDetail(zone.GUID);
                    if(lightingDetail == null)
                    {
                        continue;
                    }
                    
                    Space space = zone.Match(spaces);
                    if(space == null)
                    {
                        continue;
                    }

                    InternalCondition internalCondition = space.InternalCondition;
                    if(internalCondition == null)
                    {
                        continue;
                    }

                    if(!internalCondition.TryGetValue(InternalConditionParameter.NCMData, out NCMData nCMData) || nCMData == null)
                    {
                        continue;
                    }

                    lightingDetail.bEfficacyLightingFunc = false;
                    if(internalCondition.TryGetValue(InternalConditionParameter.LightingLevel, out double lightingLevel) && !double.IsNaN(lightingLevel))
                    {
                        lightingDetail.DesignIlluminance = lightingLevel;
                    }

                    lightingDetail.SetAPD(Core.Query.Description(nCMData.LightingOccupancyControls)?.ToUpper());

                    lightingDetail.SetDaylightControl(Core.Query.Description(nCMData.LightingPhotoelectricControls)?.ToUpper());

                    //lightingDetail.SetCountry(Core.Query.Description(nCMData.Country)?.ToUpper());

                    lightingDetail.bBackSpaceSensor = nCMData.LightingPhotoelectricBackSpaceSensor;

                    lightingDetail.PhotocellClock = nCMData.LightingPhotoelectricControlsTimeSwitch;

                    lightingDetail.bUserDaylightFactor = nCMData.LightingDaylightFactorMethod;

                    //lightingDetail.IsMainsGasAvailable = nCMData.IsMainsGasAvailable;

                    lightingDetail.ParasiticPower = nCMData.LightingPhotoelectricParasiticPower;

                    //lightingDetail.AirPermeability = nCMData.AirPermeability;

                    if (internalCondition.TryGetValue(InternalConditionParameter.LightingGainPerArea, out double lightingGainPerArea) && !double.IsNaN(lightingGainPerArea))
                    {
                        lightingDetail.LampGeneralPowerDensity = lightingGainPerArea;
                    }

                    result = true;
                }
            }

            return result;
        }
    }
}