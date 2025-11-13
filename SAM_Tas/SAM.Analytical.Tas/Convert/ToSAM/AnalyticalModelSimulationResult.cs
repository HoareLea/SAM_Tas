using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static AnalyticalModelSimulationResult ToSAM_AnalyticalModelSimulationResult(string path_TSD, AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(path_TSD) || !System.IO.File.Exists(path_TSD))
            {
                return null;
            }

            AnalyticalModelSimulationResult result = null;

            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
            {
                BuildingData buildingData = sAMTSDDocument.TSDDocument?.SimulationData?.GetBuildingData();
                if (buildingData == null)
                {
                    return null;
                }

                List<double> values;

                values = Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.heatingProfile);

                double consumptionHeating = values.Sum();
                double peakHeatingLoad = values.Max();
                int peakHeatingHour = values.IndexOf(peakHeatingLoad);

                values = Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.coolingProfile);

                double consumptionCooling = values.Sum();
                double peakCoolingLoad = values.Max();
                int peakCoolingHour = values.IndexOf(peakCoolingLoad);

                result = new AnalyticalModelSimulationResult(analyticalModel.Name, Assembly.GetExecutingAssembly().GetName()?.Name, path_TSD);
                result.SetValue(Analytical.AnalyticalModelSimulationResultParameter.ConsumptionHeating, consumptionHeating);
                result.SetValue(Analytical.AnalyticalModelSimulationResultParameter.PeakHeatingLoad, peakHeatingLoad);
                result.SetValue(Analytical.AnalyticalModelSimulationResultParameter.PeakHeatingHour, peakHeatingHour);

                result.SetValue(Analytical.AnalyticalModelSimulationResultParameter.ConsumptionCooling, consumptionCooling);
                result.SetValue(Analytical.AnalyticalModelSimulationResultParameter.PeakCoolingLoad, peakCoolingLoad);
                result.SetValue(Analytical.AnalyticalModelSimulationResultParameter.PeakCoolingHour, peakCoolingHour);

                double volume = Core.Tas.Query.Volume(buildingData);
                if(!double.IsNaN(volume))
                {
                    result.SetValue(Analytical.AnalyticalModelSimulationResultParameter.Volume, volume);
                }


                double floorArea = Core.Tas.Query.FloorArea(buildingData);
                if (!double.IsNaN(floorArea))
                {
                    result.SetValue(Analytical.AnalyticalModelSimulationResultParameter.FloorArea, floorArea);
                }
            }

            return result;
        }
    }
}
