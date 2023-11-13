using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalCalculateConstructions : GH_SAMVariableOutputParameterComponent
    {
        private double tolerance = 0.0001;
        
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("7f71ffed-f257-43a7-aa20-b56c6cdd8898");

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
        public SAMAnalyticalCalculateConstructions()
          : base("SAMAnalytical.CalculateConstructions", "SAMAnalytical.CalculateConstructions",
              "Calculate Constructions",
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
                result.Add(new GH_SAMParam(new GooThermalTransmittanceCalculationDataParam() { Name = "_thermalTransmittanceCalculationDatas", NickName = "_thermalTransmittanceCalculationDatas", Description = "SAM LayerThicknessCalculationDatas", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooThermalTransmittanceCalculationResultParam() { Name = "in", NickName = "in", Description = "Valid Layer Thickness Calculation Result", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooThermalTransmittanceCalculationResultParam() { Name = "out", NickName = "out", Description = "Invalid Layer Thickness Calculation Result", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
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

            List<IThermalTransmittanceCalculationData> thermalTransmittanceCalculationDatas = new List<IThermalTransmittanceCalculationData>();
            index = Params.IndexOfInputParam("_thermalTransmittanceCalculationDatas");
            if (index == -1 || !dataAccess.GetDataList(index, thermalTransmittanceCalculationDatas) || thermalTransmittanceCalculationDatas == null || thermalTransmittanceCalculationDatas.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);
            List<IThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults = thermalTransmittanceCalculator.Calculate(thermalTransmittanceCalculationDatas);
            if(thermalTransmittanceCalculationResults != null)
            {
                constructionManager = new ConstructionManager(constructionManager);
                foreach(IThermalTransmittanceCalculationResult layerThicknessCalculationResult in thermalTransmittanceCalculationResults)
                {
                    constructionManager.Update(layerThicknessCalculationResult, tolerance);
                }
            }

            List<IThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults_In = new List<IThermalTransmittanceCalculationResult>();
            List<IThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults_Out = new List<IThermalTransmittanceCalculationResult>();
            foreach(IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult in thermalTransmittanceCalculationResults)
            {
                if(thermalTransmittanceCalculationResult == null)
                {
                    continue;
                }

                if(thermalTransmittanceCalculationResult is ConstructionCalculationResult)
                {
                    ConstructionCalculationResult constructionCalculationResult = (ConstructionCalculationResult)thermalTransmittanceCalculationResult;

                    double calculatedThermalTransmittance = constructionCalculationResult.CalculatedThermalTransmittance;
                    if (double.IsNaN(calculatedThermalTransmittance))
                    {
                        thermalTransmittanceCalculationResults_Out.Add(thermalTransmittanceCalculationResult);
                        continue;
                    }

                    List<IThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults_Temp = constructionCalculationResult.ThermalTransmittance > constructionCalculationResult.CalculatedThermalTransmittance - Tolerance.MacroDistance ? thermalTransmittanceCalculationResults_In : thermalTransmittanceCalculationResults_Out;

                    thermalTransmittanceCalculationResults_Temp.Add(thermalTransmittanceCalculationResult);
                }
                else if(thermalTransmittanceCalculationResult is LayerThicknessCalculationResult)
                {
                    LayerThicknessCalculationResult layerThicknessCalculationData = (LayerThicknessCalculationResult)thermalTransmittanceCalculationResult;

                    double calculatedThermalTransmittance = layerThicknessCalculationData.CalculatedThermalTransmittance;
                    if (double.IsNaN(calculatedThermalTransmittance))
                    {
                        thermalTransmittanceCalculationResults_Out.Add(thermalTransmittanceCalculationResult);
                        continue;
                    }

                    List<IThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults_Temp = layerThicknessCalculationData.ThermalTransmittance > layerThicknessCalculationData.CalculatedThermalTransmittance - Tolerance.MacroDistance ? thermalTransmittanceCalculationResults_In : thermalTransmittanceCalculationResults_Out;

                    thermalTransmittanceCalculationResults_Temp.Add(thermalTransmittanceCalculationResult);
                }
                else if(thermalTransmittanceCalculationResult is ApertureConstructionCalculationResult)
                {
                    ApertureConstructionCalculationResult apertureConstructionCalculationResult = (ApertureConstructionCalculationResult)thermalTransmittanceCalculationResult;

                    double calculatedFrameThermalTransmittance = apertureConstructionCalculationResult.CalculatedFrameThermalTransmittance;
                    double calculatedPaneThermalTransmittance = apertureConstructionCalculationResult.CalculatedPaneThermalTransmittance;

                    if (double.IsNaN(calculatedFrameThermalTransmittance) && double.IsNaN(calculatedPaneThermalTransmittance))
                    {
                        thermalTransmittanceCalculationResults_Out.Add(thermalTransmittanceCalculationResult);
                        continue;
                    }

                    if (!double.IsNaN(calculatedFrameThermalTransmittance))
                    {
                        if(apertureConstructionCalculationResult.FrameThermalTransmittance < calculatedFrameThermalTransmittance + Tolerance.MacroDistance)
                        {
                            thermalTransmittanceCalculationResults_Out.Add(thermalTransmittanceCalculationResult);
                            continue;
                        }
                    }

                    if (!double.IsNaN(calculatedPaneThermalTransmittance))
                    {
                        if (apertureConstructionCalculationResult.PaneThermalTransmittance < calculatedPaneThermalTransmittance + Tolerance.MacroDistance)
                        {
                            thermalTransmittanceCalculationResults_Out.Add(thermalTransmittanceCalculationResult);
                            continue;
                        }
                    }

                    thermalTransmittanceCalculationResults_In.Add(thermalTransmittanceCalculationResult);

                }
            }

            index = Params.IndexOfOutputParam("constructionManager");
            if (index != -1)
            {
                dataAccess.SetData(index, constructionManager);
            }

            index = Params.IndexOfOutputParam("in");
            if (index != -1)
            {
                dataAccess.SetDataList(index, thermalTransmittanceCalculationResults_In.ConvertAll(x => new GooThermalTransmittanceCalculationResult(x)));
            }

            if(thermalTransmittanceCalculationResults_Out != null && thermalTransmittanceCalculationResults_Out.Count > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some of the results do not meet thermal transmittance (U-value) criteria");
            }

            index = Params.IndexOfOutputParam("out");
            if (index != -1)
            {
                dataAccess.SetDataList(index, thermalTransmittanceCalculationResults_Out.ConvertAll(x => new GooThermalTransmittanceCalculationResult(x)));
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, thermalTransmittanceCalculationResults != null && thermalTransmittanceCalculationResults.Count != 0);
            }
        }
    }
}