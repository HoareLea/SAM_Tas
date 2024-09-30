using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalSurfaceSimulationResults : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("69d7c146-73ad-4730-bd23-57bba11e8612");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.4";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalSurfaceSimulationResults()
          : base("SAMAnalytical.SurfaceSimulationResults", "SAMAnalytical.SurfaceSimulationResults",
              "Query SurfaceSimulationResults from AnalyticalModel or AdjacencyCluster",
              "SAM", "Analytical04")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "_analytical", NickName = "_analytical", Description = "SAM Analytical Object such as AdjacencyCluster or AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "loadType_", NickName = "loadType_", Description = "LoadType", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(@string, ParamVisibility.Voluntary));

                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "space_", NickName = "space_", Description = "SAM Analytical Space", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));

                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "panelOrAperture_", NickName = "panelOrAperture_", Description = "SAM Analytical Panel or SAM Analytical Aperture", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam { Name = "analytical", NickName = "analytical", Description = "SAM Analytical", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "surfaceSimulationResults_Panels", NickName = "surfaceSimulationResults_Panels", Description = "SAM Analytical SurfaceSimulationResults for Panels", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooPanelParam() { Name = "panels", NickName = "panels", Description = "SAM Analytical Panels", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Point() { Name = "internalPoints_Panels", NickName = "internalPoints_Panels", Description = "Internal Points for Panels", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "surfaceSimulationResults_Apertures_Pane", NickName = "surfaceSimulationResults_Apertures_Pane", Description = "SAM Analytical SurfaceSimulationResults for Apertures", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooApertureParam() { Name = "apertures_Pane", NickName = "apertures_Pane", Description = "SAM Analytical Apertures_Pane", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Point() { Name = "internalPoints_Apertures_Pane", NickName = "internalPoints_Apertures_Pane", Description = "Internal Points for Apertures", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "surfaceSimulationResults_Apertures_Frame", NickName = "surfaceSimulationResults_Apertures_Frame", Description = "SAM Analytical SurfaceSimulationResults for Apertures", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooApertureParam() { Name = "apertures_Frame", NickName = "apertures_Frame", Description = "SAM Analytical Apertures", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Point() { Name = "internalPoints_Apertures_Frame", NickName = "internalPoints_Apertures_Frame", Description = "Internal Points for Apertures", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index = -1;

            index = Params.IndexOfInputParam("_analytical");
            SAMObject sAMObject = null;
            if (index == -1 || !dataAccess.GetData(index, ref sAMObject) || sAMObject == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AdjacencyCluster adjacencyCluster = null;
            if (sAMObject is AdjacencyCluster)
            {
                adjacencyCluster = new AdjacencyCluster((AdjacencyCluster)sAMObject);
            }
            else if (sAMObject is AnalyticalModel)
            {
                adjacencyCluster = ((AnalyticalModel)sAMObject).AdjacencyCluster;
            }

            LoadType[] loadTypes = null;
            index = Params.IndexOfInputParam("loadType_");
            if (index != -1)
            {
                string loadTypeString = null;
                if (dataAccess.GetData(index, ref loadTypeString))
                {
                    if (Core.Query.TryGetEnum(loadTypeString, out LoadType loadType) && loadType != LoadType.Undefined)
                    {
                        loadTypes = new LoadType[] { loadType };
                    }
                }
            }

            if (loadTypes == null)
            {
                loadTypes = new LoadType[] { LoadType.Cooling, LoadType.Heating };
            }

            List<Space> spaces = null;
            index = Params.IndexOfInputParam("space_");
            if (index != -1)
            {
                Space space = null;
                if (dataAccess.GetData(index, ref space) && space != null)
                {
                    spaces = new List<Space>() { space };
                }
            }

            if (spaces == null)
            {
                spaces = adjacencyCluster.GetSpaces();
            }

            List<string> space_Guids = new List<string>();
            foreach (Space space in spaces)
            {
                if (space != null && space.TryGetValue(Analytical.Tas.SpaceParameter.ZoneGuid, out string zoneGuid) && zoneGuid != null)
                {
                    space_Guids.Add(zoneGuid);
                }
            }


            List<Aperture> apertures = null;
            List<Panel> panels = null;

            index = Params.IndexOfInputParam("panelOrAperture_");
            if (index != -1)
            {
                IAnalyticalObject analytical = null;
                if (dataAccess.GetData(index, ref analytical) && analytical != null)
                {
                    if (analytical is Panel)
                    {
                        panels = new List<Panel>() { (Panel)analytical };
                    }
                    else if (analytical is Aperture)
                    {
                        apertures = new List<Aperture>() { (Aperture)analytical };
                    }
                }
            }

            if (panels == null)
            {
                panels = adjacencyCluster.GetPanels();
            }

            if (apertures == null)
            {
                apertures = adjacencyCluster.GetApertures();
            }

            Dictionary<SurfaceSimulationResult, Panel> dictionary_Panel = new Dictionary<SurfaceSimulationResult, Panel>();
            Dictionary<SurfaceSimulationResult, Aperture> dictionary_Aperture_Pane = new Dictionary<SurfaceSimulationResult, Aperture>();
            Dictionary<SurfaceSimulationResult, Aperture> dictionary_Aperture_Frame = new Dictionary<SurfaceSimulationResult, Aperture>();

            List<SurfaceSimulationResult> surfaceSimulationResults = adjacencyCluster.GetObjects<SurfaceSimulationResult>();
            surfaceSimulationResults.RemoveAll(x => !loadTypes.Contains(x.LoadType()));
            for (int i = surfaceSimulationResults.Count - 1; i >= 0; i--)
            {
                SurfaceSimulationResult surfaceSimulationResult = surfaceSimulationResults[i];

                List<Guid> guids_Panel = adjacencyCluster.GetRelatedObjects<Panel>(surfaceSimulationResult)?.ConvertAll(x => x.Guid);
                if (guids_Panel == null || guids_Panel.Count == 0)
                {
                    surfaceSimulationResults.RemoveAt(i);
                    continue;
                }

                Panel panel = panels.Find(x => guids_Panel.Contains(x.Guid));
                if (panel == null)
                {
                    surfaceSimulationResults.RemoveAt(i);
                    continue;
                }

                List<Space> spaces_Panel = adjacencyCluster.GetSpaces(panel);
                List<string> space_Guids_Panel = new List<string>();
                foreach (Space space in spaces_Panel)
                {
                    if (space != null && space.TryGetValue(Analytical.Tas.SpaceParameter.ZoneGuid, out string zoneGuid) && zoneGuid != null)
                    {
                        space_Guids_Panel.Add(zoneGuid);
                    }
                }

                if (surfaceSimulationResults[i].TryGetValue(Analytical.Tas.SurfaceSimulationResultParameter.ZoneSurfaceReference, out ZoneSurfaceReference zoneSurfaceReference))
                {
                    if (!space_Guids.Contains(zoneSurfaceReference.ZoneGuid))
                    {
                        surfaceSimulationResults.RemoveAt(i);
                        continue;
                    }

                    if (panel.TryGetValue(Analytical.Tas.PanelParameter.ZoneSurfaceReference_1, out ZoneSurfaceReference zoneSurfaceReference_1) && zoneSurfaceReference_1 != null)
                    {
                        if (zoneSurfaceReference.SurfaceNumber == zoneSurfaceReference_1.SurfaceNumber && space_Guids_Panel.Contains(zoneSurfaceReference_1.ZoneGuid))
                        {
                            dictionary_Panel[surfaceSimulationResult] = panel;
                            continue;
                        }
                    }

                    if (panel.TryGetValue(Analytical.Tas.PanelParameter.ZoneSurfaceReference_2, out ZoneSurfaceReference zoneSurfaceReference_2) && zoneSurfaceReference_2 != null)
                    {
                        if (zoneSurfaceReference.SurfaceNumber == zoneSurfaceReference_2.SurfaceNumber && space_Guids_Panel.Contains(zoneSurfaceReference_2.ZoneGuid))
                        {
                            dictionary_Panel[surfaceSimulationResult] = panel;
                            continue;
                        }
                    }

                    List<Aperture> apertures_Panel = panel.Apertures;
                    if (apertures_Panel == null || apertures_Panel.Count == 0)
                    {
                        surfaceSimulationResults.RemoveAt(i);
                        continue;
                    }

                    Aperture aperture = Analytical.Tas.Query.Match(zoneSurfaceReference, apertures_Panel, out AperturePart aperturePart);
                    if (apertures.Find(x => x.Guid == aperture.Guid) != null)
                    {
                        if(aperturePart == AperturePart.Pane)
                        {
                            dictionary_Aperture_Pane[surfaceSimulationResult] = aperture;
                        }
                        else if(aperturePart == AperturePart.Frame)
                        {
                            dictionary_Aperture_Frame[surfaceSimulationResult] = aperture;
                        }

                        
                        continue;
                    }

                    surfaceSimulationResults.RemoveAt(i);
                }
            }


            if (sAMObject is AdjacencyCluster)
            {
                sAMObject = adjacencyCluster;
            }
            else if (sAMObject is AnalyticalModel)
            {
                sAMObject = new AnalyticalModel((AnalyticalModel)sAMObject, adjacencyCluster);
            }

            index = Params.IndexOfOutputParam("analytical");
            if (index != -1)
                dataAccess.SetData(index, sAMObject);

            index = Params.IndexOfOutputParam("surfaceSimulationResults_Panels");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Panel.Keys);

            index = Params.IndexOfOutputParam("internalPoints_Panels");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Panel.Values?.ToList().ConvertAll(x => Geometry.Rhino.Convert.ToRhino(x.GetFace3D(true).GetInternalPoint3D())));

            index = Params.IndexOfOutputParam("panels");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Panel.Values);

            index = Params.IndexOfOutputParam("surfaceSimulationResults_Apertures_Pane");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Aperture_Pane.Keys);

            index = Params.IndexOfOutputParam("apertures_Pane");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Aperture_Pane.Values);

            index = Params.IndexOfOutputParam("internalPoints_Apertures_Pane");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Aperture_Pane.Values?.ToList().ConvertAll(x => Geometry.Rhino.Convert.ToRhino(x.GetPaneFace3Ds()?.FirstOrDefault()?.GetInternalPoint3D())));

            index = Params.IndexOfOutputParam("surfaceSimulationResults_Apertures_Frame");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Aperture_Frame.Keys);

            index = Params.IndexOfOutputParam("apertures_Frame");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Aperture_Frame.Values);

            index = Params.IndexOfOutputParam("internalPoints_Apertures_Frame");
            if (index != -1)
                dataAccess.SetDataList(index, dictionary_Aperture_Frame.Values?.ToList().ConvertAll(x => Geometry.Rhino.Convert.ToRhino(x.GetFrameFace3D()?.GetInternalPoint3D())));
        }
    }
}