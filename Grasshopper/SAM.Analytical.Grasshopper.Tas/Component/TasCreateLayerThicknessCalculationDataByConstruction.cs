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
    public class TasCreateLayerThicknessCalculationDataByConstruction : GH_SAMVariableOutputParameterComponent
    {
        private static double minThickness = Tolerance.MacroDistance;
        private static double maxThickness = 1;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("8f915aab-e847-4485-9870-f5706f745baa");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.3";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Resources.SAM_TasTBD3;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasCreateLayerThicknessCalculationDataByConstruction()
          : base("Tas.CreateLayerThicknessCalculationDataByConstruction", "Tas.CreateLayerThicknessCalculationDataByConstruction",
              "Tas Create LayerThicknessCalculationData By Construction",
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
                result.Add(new GH_SAMParam(new GooConstructionParam() { Name = "_construction", NickName = "_construction", Description = "Construction Name", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooMaterialLibraryParam() { Name = "_materialLibrary", NickName = "_materialLibrary", Description = "Material Library", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "thermalTransmittance_", NickName = "thermalTransmittance_", Description = "target Thermal Transmittance U-value", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "external_", NickName = "external_", Description = "Set True/False or 1/0 depends if construction is External/Internal to set correct Rse/Rsi", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "constructionName_", NickName = "constructionName_", Description = "Construction Name", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "layerIndex_", NickName = "layerIndex_", Description = "Layer Index to be modified", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "heatFlowDirection_", NickName = "heatFlowDirection_", Description = "HeatFlowDirection \n*Use enum components", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Number @number;

                @number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "minThickness_", NickName = "minThickness_", Description = "Min Thickness", Access = GH_ParamAccess.item };
                @number.SetPersistentData(minThickness);
                result.Add(new GH_SAMParam(@number, ParamVisibility.Voluntary));

                @number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "maxThickness_", NickName = "maxThickness_", Description = "Max Thickness", Access = GH_ParamAccess.item };
                @number.SetPersistentData(maxThickness);
                result.Add(new GH_SAMParam(@number, ParamVisibility.Voluntary));

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

            MaterialLibrary materialLibrary = null;
            index = Params.IndexOfInputParam("_materialLibrary");
            if (index == -1 || !dataAccess.GetData(index, ref materialLibrary) || materialLibrary == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Construction construction = null;
            index = Params.IndexOfInputParam("_construction");
            if (index == -1 || !dataAccess.GetData(index, ref construction) || construction == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }


            LayerThicknessCalculationData layerThicknessCalculationData = Analytical.Tas.Create.LayerThicknessCalculationData(construction, materialLibrary);
            if(layerThicknessCalculationData == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string constructionName_Temp = null;
            index = Params.IndexOfInputParam("constructionName_");
            if (index != -1 && dataAccess.GetData(index, ref constructionName_Temp) && !string.IsNullOrWhiteSpace(constructionName_Temp))
            {
                layerThicknessCalculationData.ConstructionName = constructionName_Temp;
            }

            layerThicknessCalculationData.LayerIndex = -1;
            int layerIndex_Temp = layerThicknessCalculationData.LayerIndex;
            index = Params.IndexOfInputParam("layerIndex_");
            if (index != -1 && dataAccess.GetData(index, ref layerIndex_Temp) && layerIndex_Temp != -1)
            {
                layerThicknessCalculationData.LayerIndex = layerIndex_Temp;
            }

            double thermalTransmittance_Temp = double.NaN;
            index = Params.IndexOfInputParam("thermalTransmittance_");
            if (index != -1 && dataAccess.GetData(index, ref thermalTransmittance_Temp) && !double.IsNaN(thermalTransmittance_Temp))
            {
                layerThicknessCalculationData.ThermalTransmittance = thermalTransmittance_Temp;
            }

            string string_HeatFlowDirection = null; 
            index = Params.IndexOfInputParam("heatFlowDirection_");
            if (index != -1 && dataAccess.GetData(index, ref string_HeatFlowDirection) && !string.IsNullOrWhiteSpace(string_HeatFlowDirection))
            {
                if(Core.Query.TryGetEnum(string_HeatFlowDirection, out HeatFlowDirection heatFlowDirection_Temp))
                {
                    layerThicknessCalculationData.HeatFlowDirection = heatFlowDirection_Temp;
                }
            }

            bool external_Temp = false;
            index = Params.IndexOfInputParam("external_");
            if (index != -1 && dataAccess.GetData(index, ref external_Temp))
            {
                layerThicknessCalculationData.External = external_Temp; 
            }

            double minThickness_Temp = minThickness;
            index = Params.IndexOfInputParam("minThickness_");
            if (index == -1 || !dataAccess.GetData(index, ref minThickness_Temp))
            {
                minThickness_Temp = minThickness;
            }

            double maxThickness_Temp = maxThickness;
            index = Params.IndexOfInputParam("maxThickness_");
            if (index == -1 || !dataAccess.GetData(index, ref maxThickness_Temp))
            {
                maxThickness_Temp = maxThickness;
            }

            layerThicknessCalculationData.ThicknessRange = new Range<double>(minThickness_Temp, maxThickness_Temp);

            index = Params.IndexOfOutputParam("layerThicknessCalculationData");
            if (index != -1)
            {
                if (string.IsNullOrWhiteSpace(layerThicknessCalculationData.ConstructionName) || double.IsNaN(layerThicknessCalculationData.ThermalTransmittance) || layerThicknessCalculationData.HeatFlowDirection == HeatFlowDirection.Undefined)
                {
                    dataAccess.SetData(index, null);
                    return;
                }

                dataAccess.SetData(index, new GooLayerThicknessCalculationData(layerThicknessCalculationData));
            }
        }
    }
}