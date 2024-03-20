using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasCreateLayerThicknessCalculationData : GH_SAMVariableOutputParameterComponent
    {
        private static double minThickness = Tolerance.MacroDistance;
        private static double maxThickness = 1;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3c8cad89-80e9-42c8-8de2-9bea8803ee0f");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Resources.SAM_TasTBD3;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasCreateLayerThicknessCalculationData()
          : base("Tas.CreateLayerThicknessCalculationData", "Tas.CreateLayerThicknessCalculationData",
              "Tas Create LayerThicknessCalculationData",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_constructionName", NickName = "_constructionName", Description = "Construction Name", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "layerIndex_", NickName = "layerIndex_", Description = "Layer Index", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_thermalTransmittance", NickName = "_thermalTransmittance", Description = "Thermal Transmittance", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_heatFlowDirection", NickName = "_heatFlowDirection", Description = "HeatFlowDirection", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_external", NickName = "_external", Description = "External", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number @number;

                @number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_minThickness_", NickName = "_minThickness_", Description = "Min Thickness", Access = GH_ParamAccess.item };
                @number.SetPersistentData(minThickness);
                result.Add(new GH_SAMParam(@number, ParamVisibility.Binding));

                @number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_maxThickness_", NickName = "_maxThickness_", Description = "Max Thickness", Access = GH_ParamAccess.item };
                @number.SetPersistentData(maxThickness);
                result.Add(new GH_SAMParam(@number, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooLayerThicknessCalculationDataParam() { Name = "layerThicknessCalculationData", NickName = "layerThicknessCalculationData", Description = "LayerThicknessCalculationData", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            string constructionName = null;
            index = Params.IndexOfInputParam("_constructionName");
            if (index == -1 || !dataAccess.GetData(index, ref constructionName) || string.IsNullOrWhiteSpace(constructionName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            int layerIndex = -1;
            index = Params.IndexOfInputParam("layerIndex_");
            if (index != -1 )
            {
                dataAccess.GetData(index, ref layerIndex);
            }

            double thermalTransmittance = double.NaN;
            index = Params.IndexOfInputParam("_thermalTransmittance");
            if (index == -1 || !dataAccess.GetData(index, ref thermalTransmittance) || double.IsNaN(thermalTransmittance))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string string_HeatFlowDirection = null; 
            index = Params.IndexOfInputParam("_heatFlowDirection");
            if (index == -1 || !dataAccess.GetData(index, ref string_HeatFlowDirection) || string.IsNullOrWhiteSpace(string_HeatFlowDirection))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            HeatFlowDirection heatFlowDirection = Core.Query.Enum<HeatFlowDirection>(string_HeatFlowDirection);
            if(heatFlowDirection == HeatFlowDirection.Undefined)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool external = false;
            index = Params.IndexOfInputParam("_external");
            if (index == -1 || !dataAccess.GetData(index, ref external))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double minThickness_Temp = minThickness;
            index = Params.IndexOfInputParam("_minThickness_");
            if (index == -1 || !dataAccess.GetData(index, ref minThickness_Temp))
            {
                minThickness_Temp = minThickness;
            }

            double maxThickness_Temp = maxThickness;
            index = Params.IndexOfInputParam("_maxThickness_");
            if (index == -1 || !dataAccess.GetData(index, ref maxThickness_Temp))
            {
                maxThickness_Temp = maxThickness;
            }

            index = Params.IndexOfOutputParam("layerThicknessCalculationData");
            if (index != -1)
            {
                if (string.IsNullOrWhiteSpace(constructionName) || double.IsNaN(thermalTransmittance) || heatFlowDirection == HeatFlowDirection.Undefined)
                {
                    dataAccess.SetData(index, null);
                    return;
                }

                LayerThicknessCalculationData layerThicknessCalculationData = new LayerThicknessCalculationData(constructionName, layerIndex, thermalTransmittance, heatFlowDirection, external);
                layerThicknessCalculationData.ThicknessRange = new Range<double>(minThickness_Temp, maxThickness_Temp);

                dataAccess.SetData(index, new GooLayerThicknessCalculationData(layerThicknessCalculationData));
            }
        }
    }
}