using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Guid> SetBlinds(this AnalyticalModel analyticalModel,  string path_TBD, string path_TSD, string path_TBD_Output = null)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD) || string.IsNullOrWhiteSpace(path_TSD))
                return null;

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
                return null;

            List<SAMType> sAMTypes = new List<SAMType>();

            List<Construction> constructions = adjacencyCluster.GetConstructions();
            if (constructions != null)
                sAMTypes.AddRange(constructions);

            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions();
            if (apertureConstructions != null)
                sAMTypes.AddRange(apertureConstructions);

            string path = path_TBD;
            if (!string.IsNullOrWhiteSpace(path_TBD_Output))
            {
                System.IO.File.Copy(path_TBD, path_TBD_Output, true);
                path = path_TBD_Output;
            }

            return SetBlinds(sAMTypes, path, path_TSD);
        }

        public static List<Guid> SetBlinds(this IEnumerable<SAMType> sAMTypes, string path_TBD, string path_TSD)
        {
            List<Tuple<SAMType, double, double>> tuples = new List<Tuple<SAMType, double, double>>();
            foreach(SAMType sAMType in sAMTypes)
            {
                double blindSolarGainPerAreaToClose;
                if (!sAMType.TryGetValue("SAM_BuildingElementBlindSolarGainPerAreaToClose", out blindSolarGainPerAreaToClose, true) || double.IsNaN(blindSolarGainPerAreaToClose) || blindSolarGainPerAreaToClose == 0)
                    continue;

                double blindFactor;
                if (!sAMType.TryGetValue("SAM_BuildingElementBlindFactor", out blindFactor, true) || double.IsNaN(blindFactor) || blindFactor == 0)
                    continue;

                tuples.Add(new Tuple<SAMType, double, double> (sAMType, blindSolarGainPerAreaToClose, blindFactor));
            }

            if (tuples == null || tuples.Count == 0)
                return new List<Guid>();


            AdjacencyCluster adjacencyCluster_TSD = path_TSD.ToSAM_AdjacencyCluster(null, new PanelDataType[] { PanelDataType.ExternalSolarGain });

            throw new NotImplementedException();
            
            //using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            //{
            //    result = SetBlinds(sAMTBDDocument, apertureConstructions);
            //    if (result != null)
            //        sAMTBDDocument.Save();
            //}

            //return result;
        }

        public static List<Guid> SetBlinds(this SAMTBDDocument sAMTBDDocument, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if (sAMTBDDocument == null)
                return null;

            return SetBlinds(sAMTBDDocument.TBDDocument, apertureConstructions);
        }

        public static List<Guid> SetBlinds(this TBDDocument tBDDocument, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if (tBDDocument == null || apertureConstructions == null)
                return null;

            Building builidng = tBDDocument.Building;
            if (builidng == null)
                return null;

            builidng.RemoveApertureTypes();
            tBDDocument.RemoveSchedules("_APSCHED");

            List<Guid> result = new List<Guid>();

            List<buildingElement> buildingElements = builidng.BuildingElements();
            if (buildingElements == null)
                return result;

            List<dayType> dayTypes = builidng.DayTypes();
            dayTypes.RemoveAll(x => x.name.Equals("CDD") || x.name.Equals("HDD"));

            foreach (ApertureConstruction apertureConstruction in apertureConstructions)
            {
                string paneApertureConstructionName = apertureConstruction.PaneApertureConstructionUniqueName();
                
                buildingElement buildingElement = buildingElements.Find(x => x.name.Equals(paneApertureConstructionName));
                if (buildingElement == null)
                    continue;

                if (builidng.AssignApertureTypes(buildingElement, dayTypes, apertureConstruction))
                {
                    Guid guid;
                    if (Guid.TryParse(buildingElement.GUID, out guid))
                        result.Add(guid);
                }
            }

            return result;
        }
    }
}