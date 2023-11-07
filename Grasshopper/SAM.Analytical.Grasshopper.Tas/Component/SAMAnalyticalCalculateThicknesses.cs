using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalCalculateThicknesses : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("2de06015-0a85-49e2-aae5-20e3666503f0");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalCalculateThicknesses()
          : base("SAMAnalytical.CalculateThicknesses", "SAMAnalytical.CalculateThicknesses",
              "Calculate Layer Thicknesses",
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
                result.Add(new GH_SAMParam(new GooLayerThicknessCalculationDataParam() { Name = "_layerThicknessCalculationDatas", NickName = "_layerThicknessCalculationDatas", Description = "SAM LayerThicknessCalculationDatas", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooLayerThicknessCalculationResultParam() { Name = "in", NickName = "in", Description = "Valid Layer Thickness Calculation Result", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooLayerThicknessCalculationResultParam() { Name = "out", NickName = "out", Description = "Invalid Layer Thickness Calculation Result", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
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

            List<LayerThicknessCalculationData> layerThicknessCalculationDatas = new List<LayerThicknessCalculationData>();
            index = Params.IndexOfInputParam("_layerThicknessCalculationDatas");
            if (index == -1 || !dataAccess.GetDataList(index, layerThicknessCalculationDatas) || layerThicknessCalculationDatas == null || layerThicknessCalculationDatas.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);
            List<LayerThicknessCalculationResult> layerThicknessCalculationResults = thermalTransmittanceCalculator.Calculate(layerThicknessCalculationDatas);
            if(layerThicknessCalculationResults != null)
            {
                constructionManager = new ConstructionManager(constructionManager);
                foreach(LayerThicknessCalculationResult layerThicknessCalculationResult in layerThicknessCalculationResults)
                {
                    constructionManager.Update(layerThicknessCalculationResult);
                }
            }

            List<LayerThicknessCalculationResult> layerThicknessCalculationResults_In = new List<LayerThicknessCalculationResult>();
            List<LayerThicknessCalculationResult> layerThicknessCalculationResults_Out = new List<LayerThicknessCalculationResult>();
            foreach(LayerThicknessCalculationResult layerThicknessCalculationResult in layerThicknessCalculationResults)
            {
                if(layerThicknessCalculationResult == null)
                {
                    continue;
                }

                double calculatedThermalTransmittance = layerThicknessCalculationResult.CalculatedThermalTransmittance;
                if (double.IsNaN(calculatedThermalTransmittance))
                {
                    layerThicknessCalculationResults_Out.Add(layerThicknessCalculationResult);
                    continue;
                }

                List<LayerThicknessCalculationResult> layerThicknessCalculationResults_Temp = layerThicknessCalculationResult.ThermalTransmittance < layerThicknessCalculationResult.CalculatedThermalTransmittance + Tolerance.MacroDistance? layerThicknessCalculationResults_In : layerThicknessCalculationResults_Out;

                layerThicknessCalculationResults_Temp.Add(layerThicknessCalculationResult);
            }

            index = Params.IndexOfOutputParam("constructionManager");
            if (index != -1)
            {
                dataAccess.SetData(index, constructionManager);
            }

            index = Params.IndexOfOutputParam("in");
            if (index != -1)
            {
                dataAccess.SetDataList(index, layerThicknessCalculationResults_In.ConvertAll(x => new GooLayerThicknessCalculationResult(x)));
            }

            if(layerThicknessCalculationResults_Out != null && layerThicknessCalculationResults_Out.Count > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some of the results do not meet thermal transmittance (U-value) criteria");
            }

            index = Params.IndexOfOutputParam("out");
            if (index != -1)
            {
                dataAccess.SetDataList(index, layerThicknessCalculationResults_Out.ConvertAll(x => new GooLayerThicknessCalculationResult(x)));
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, layerThicknessCalculationResults != null && layerThicknessCalculationResults.Count != 0);
            }
        }
    }
}