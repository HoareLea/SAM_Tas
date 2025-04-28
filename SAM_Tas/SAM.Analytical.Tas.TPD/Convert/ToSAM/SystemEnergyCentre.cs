using TPD;
using System.Collections.Generic;
using SAM.Core.Tas;
using SAM.Core.Systems;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEnergyCentre ToSAM(string path_TPD, SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = null)
        {
            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                return null;
            }

            SystemEnergyCentre result = null;
            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {

                result = ToSAM(sAMTPDDocument, systemEnergyCentreConversionSettings);
            }

            return result;
        }

        public static SystemEnergyCentre ToSAM(this SAMTPDDocument sAMTPDDocument, SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = null)
        {
            if (sAMTPDDocument == null)
            {
                return null;
            }

            return ToSAM(sAMTPDDocument.TPDDocument, systemEnergyCentreConversionSettings);
        }

        public static SystemEnergyCentre ToSAM(this TPDDoc tPDDoc, SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = null)
        {
            EnergyCentre energyCentre = tPDDoc?.EnergyCentre;
            if(energyCentre == null)
            {
                return null;
            }

            SystemEnergyCentre result = new SystemEnergyCentre(energyCentre.Name);

            List<FuelSource> fuelSources = energyCentre.FuelSources();
            if (fuelSources != null)
            {
                foreach (FuelSource fuelSource in fuelSources)
                {
                    SystemEnergySource systemEnergySource = fuelSource.ToSAM();
                    if (systemEnergySource != null)
                    {
                        result.Add(systemEnergySource);
                    }
                }
            }

            List<PlantRoom> plantRooms = tPDDoc.PlantRooms();
            if (plantRooms == null)
            {
                return result;
            }

            if(systemEnergyCentreConversionSettings == null)
            {
                systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings();
            }

            ComponentConversionSettings componentConversionSettings = systemEnergyCentreConversionSettings.GetComponentConversionSettings();

            AnalyticalSystemsProperties analyticalSystemsProperties = new AnalyticalSystemsProperties();
            List<PlantSchedule> plantSchedules = energyCentre.Schedules();
            if (plantSchedules != null)
            {
                foreach(PlantSchedule plantSchedule in plantSchedules)
                {
                    ISchedule schedule = plantSchedule.ToSAM();
                    if(schedule == null)
                    {
                        continue;
                    }

                    analyticalSystemsProperties.Add(schedule);
                }
            }

            List<fluid> fluids = energyCentre.fluids();
            if(fluids != null)
            {
                foreach(fluid fluid in fluids)
                {
                    FluidType fluidType = fluid.ToSAM();
                    if(fluidType == null)
                    {
                        continue;
                    }
                    analyticalSystemsProperties.Add(fluidType);
                }
            }

            List<DesignConditionLoad> designConditionLoads = energyCentre.DesignConditionLoads();
            if(designConditionLoads != null)
            {
                foreach(DesignConditionLoad designConditionLoad in designConditionLoads)
                {
                    DesignCondition designCondition = designConditionLoad.ToSAM();
                    if (designCondition == null)
                    {
                        continue;
                    }
                    analyticalSystemsProperties.Add(designCondition);
                }
            }

            result.SetValue(SystemEnergyCentreParameter.AnalyticalSystemsProperties, analyticalSystemsProperties);
            foreach (PlantRoom plantRoom in plantRooms)
            {
                if (systemEnergyCentreConversionSettings.Simulate)
                {
                    //designConditionLoads.Add(energyCentre.GetNullDesignCondition());

                    //plantRoom.SimulateExx(
                    //    systemEnergyCentreConversionSettings.StartHour + 1,
                    //    systemEnergyCentreConversionSettings.EndHour + 1,
                    //    0,
                    //    1,
                    //    0,
                    //    0,
                    //    designConditionLoads.ToArray(),
                    //    (int)tpdSimulationData.tpdSimulationDataLoad + (int)tpdSimulationData.tpdSimulationDataPipe + (int)tpdSimulationData.tpdSimulationDataDuct + (int)tpdSimulationData.tpdSimulationDataSimEvents,
                    //    1,
                    //    0);

                    plantRoom.SimulateEx(systemEnergyCentreConversionSettings.StartHour + 1, systemEnergyCentreConversionSettings.EndHour + 1, 0, energyCentre.ExternalPollutant.Value, 10.0, (int)tpdSimulationData.tpdSimulationDataLoad + (int)tpdSimulationData.tpdSimulationDataPipe + (int)tpdSimulationData.tpdSimulationDataDuct + (int)tpdSimulationData.tpdSimulationDataSimEvents + (int)tpdSimulationData.tpdSimulationDataCont, 1, 0);
                }

                SystemPlantRoom systemPlantRoom = plantRoom.ToSAM();

                List<global::TPD.System> systems = plantRoom?.Systems();
                if (systems == null)
                {
                    continue;
                }

                foreach (global::TPD.System system in systems)
                {
                    systemPlantRoom.Add(system, tPDDoc, componentConversionSettings);
                }

                systemPlantRoom.Add(tPDDoc, componentConversionSettings);

                result.Add(systemPlantRoom);
            }

            if (systemEnergyCentreConversionSettings.RenameAirSystemGroups)
            {
                result.RenameAirSystemGroups();
            }

            return result;
        }
    }
}
