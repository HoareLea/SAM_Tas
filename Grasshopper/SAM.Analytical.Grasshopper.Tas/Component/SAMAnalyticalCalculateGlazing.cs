using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalCalculateGlazing : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("520c56cc-0bcd-42f9-9672-2e61b25f3c51");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalCalculateGlazing()
          : base("SAMAnalytical.CalculateGlazing", "SAMAnalytical.CalculateGlazing",
              "Calculate Glazing",
              "SAM WIP", "Tas")
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
                result.Add(new GH_SAMParam(new GooConstructionManagerParam() { Name = "_constructionManager", NickName = "_constructionManager", Description = "SAM Analytical Construction Manager", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "constructions_", NickName = "constructions_", Description = "SAM Constructions or ApertureConstructions", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooConstructionManagerParam() { Name = "constructionManager", NickName = "constructionManager", Description = "SAM Analytical ConstructionManager", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "results", NickName = "results", Description = "Glazing Calculation Result", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "constructions", NickName = "constructions", Description = "Constructions or ApertureConstructions", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly imported?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_successful = Params.IndexOfOutputParam("successful");
            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
                return;

            ConstructionManager constructionManager = null;
            index = Params.IndexOfInputParam("_constructionManager");
            if (index == -1 || !dataAccess.GetData(index, ref constructionManager) || constructionManager == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Guid> guids = null;

            List<IAnalyticalObject> analyticalObjects = new List<IAnalyticalObject>();
            index = Params.IndexOfInputParam("constructions_");
            if (index != -1)
            {
                if(dataAccess.GetDataList(index, analyticalObjects) && analyticalObjects != null && analyticalObjects.Count != 0)
                {
                    guids = new List<Guid>();
                    foreach(IAnalyticalObject analyticalObject in analyticalObjects)
                    {
                        if(analyticalObject is Construction)
                        {
                            guids.Add(((Construction)analyticalObject).Guid);
                        }
                        else if (analyticalObject is ApertureConstruction)
                        {
                            guids.Add(((ApertureConstruction)analyticalObject).Guid);
                        }
                    }

                    if(guids.Count == 0)
                    {
                        guids = null;
                    }
                }
            }

            List<string> names = Analytical.Query.MissingMaterialsNames(constructionManager, guids);
            if (names != null && names.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("{0}: {1}", "ConstructionManager is missing following materials:", string.Join(", ", names)));
                return;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);
            List<GlazingCalculationResult> glazingCalculationResults = thermalTransmittanceCalculator.CalculateGlazing(guids);

            analyticalObjects = null;
            if(glazingCalculationResults != null)
            {
                analyticalObjects = new List<IAnalyticalObject>();
                foreach (GlazingCalculationResult glazingCalculationResult in glazingCalculationResults)
                {
                    if(glazingCalculationResult == null || !Guid.TryParse(glazingCalculationResult.Reference, out Guid guid))
                    {
                        analyticalObjects.Add(null);
                        continue;
                    }

                    IAnalyticalObject analyticalObject = constructionManager.Constructions?.Find(x => x.Guid == guid);
                    if(analyticalObject == null)
                    {
                        analyticalObject = constructionManager.ApertureConstructions?.Find(x => x.Guid == guid);
                    }

                    analyticalObjects.Add(analyticalObject);
                }
            }

            index = Params.IndexOfOutputParam("constructionManager");
            if (index != -1)
            {
                dataAccess.SetData(index, constructionManager);
            }

            index = Params.IndexOfOutputParam("results");
            if (index != -1)
            {
                dataAccess.SetDataList(index, glazingCalculationResults.ConvertAll(x => new GooResult(x)));
            }

            index = Params.IndexOfOutputParam("constructions");
            if (index != -1)
            {
                dataAccess.SetDataList(index, analyticalObjects.ConvertAll(x => new GooAnalyticalObject(x)));
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, glazingCalculationResults != null && glazingCalculationResults.Count != 0);
            }
        }
    }
}