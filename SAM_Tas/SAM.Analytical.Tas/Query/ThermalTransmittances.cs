using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<double> ThermalTransmittances(this AnalyticalModel analyticalModel, IEnumerable<Guid> apertureGuids)
        {
            if (analyticalModel == null || apertureGuids == null)
            {
                return null;
            }

            ConstructionManager constructionManager = analyticalModel.ConstructionManager;
            if(constructionManager == null)
            {
                return null;
            }

            Dictionary<Guid, HashSet<Guid>> dictionary = new Dictionary<Guid, HashSet<Guid>>();
            Dictionary<Guid, double> dictionary_PaneAreaPercentage = new Dictionary<Guid, double>();
            foreach (Guid apertureGuid in apertureGuids)
            {
                Panel panel = analyticalModel.GetPanels(x => x.HasApertures && x.HasAperture(apertureGuid))?.FirstOrDefault();
                if (panel == null)
                {
                    continue;
                }

                Aperture aperture = panel.GetAperture(apertureGuid);
                if (aperture == null)
                {
                    continue;
                }

                ApertureConstruction apertureConstruction = aperture.ApertureConstruction;
                if (apertureConstruction == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(apertureConstruction.Guid, out HashSet<Guid> apertureGuids_Temp) || apertureGuids_Temp == null)
                {
                    apertureGuids_Temp = new HashSet<Guid>();
                    dictionary[apertureConstruction.Guid] = apertureGuids_Temp;
                }

                apertureGuids_Temp.Add(aperture.Guid);

                double frameArea = aperture.GetFrameArea();
                frameArea = double.IsNaN(frameArea) ? 0 : frameArea;

                double paneArea = aperture.GetPaneArea();
                paneArea = double.IsNaN(paneArea) ? 0 : paneArea;

                double area = frameArea + paneArea;

                dictionary_PaneAreaPercentage[aperture.Guid] = area == 0 ? 0 : paneArea / area * 100;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);
            List<GlazingCalculationResult> glazingCalculationResults = thermalTransmittanceCalculator.CalculateGlazing(dictionary.Keys);

            List<double> result = new List<double>();
            foreach (Guid apertureGuid in apertureGuids)
            {
                Guid constructionGuid = Guid.Empty;
                foreach(KeyValuePair<Guid, HashSet<Guid>> keyValuePair in dictionary)
                {
                    if(keyValuePair.Value.Contains(apertureGuid))
                    {
                        constructionGuid = keyValuePair.Key;
                        break;
                    }
                }

                GlazingCalculationResult glazingCalculationResult = glazingCalculationResults.Find(x => x.Reference == constructionGuid.ToString());
                if(glazingCalculationResult == null)
                {
                    result.Add(double.NaN);
                    continue;
                }

                if(glazingCalculationResult is ApertureGlazingCalculationResult)
                {
                    result.Add(((ApertureGlazingCalculationResult)glazingCalculationResult).GetThermalTransmittance(dictionary_PaneAreaPercentage[apertureGuid]));
                }
                else
                {
                    result.Add(glazingCalculationResult.ThermalTransmittance);
                }

            }

            return result;
        }
  }
}