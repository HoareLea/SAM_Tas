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
    public class TasCreateApertureConstructionCalculationData : GH_SAMVariableOutputParameterComponent
    {
        private static double minThickness = Tolerance.MacroDistance;
        private static double maxThickness = 1;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0913c7df-0228-4b12-8086-c6bc70ca4c60");

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
        public TasCreateApertureConstructionCalculationData()
          : base("Tas.CreateApertureConstructionCalculationData", "Tas.CreateApertureConstructionCalculationData",
              "Tas Create ConstructionCalculationData",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_apertureType", NickName = "_apertureType", Description = "SAM Analytical ApertureType", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_sourceConstructionName", NickName = "_sourceConstructionName", Description = "Source Construction Name", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_destinationConstructionNames", NickName = "_destinationConstructionNames", Description = "Destination Construction Names", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "paneThermalTransmittance_", NickName = "paneThermalTransmittance_", Description = "Pane Thermal Transmittance", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "frameThermalTransmittance_", NickName = "frameThermalTransmittance_", Description = "Frame Thermal Transmittance", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
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
                result.Add(new GH_SAMParam(new GooThermalTransmittanceCalculationDataParam() { Name = "apertureConstructionCalculationData", NickName = "apertureConstructionCalculationData", Description = "SAM ApertureConstructionCalculationData", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
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

            ApertureType apertureType = ApertureType.Undefined;

            string string_ApertureType = null;
            index = Params.IndexOfInputParam("_apertureType");
            if (index == -1 || !dataAccess.GetData(index, ref string_ApertureType) || string.IsNullOrWhiteSpace(string_ApertureType))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            apertureType = Core.Query.Enum<ApertureType>(string_ApertureType);

            string apertureConstructionName = null;
            index = Params.IndexOfInputParam("_sourceConstructionName");
            if (index == -1 || !dataAccess.GetData(index, ref apertureConstructionName) || string.IsNullOrWhiteSpace(apertureConstructionName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<string> apertureConstructionNames = new List<string>();
            index = Params.IndexOfInputParam("_destinationConstructionNames");
            if (index == -1 || !dataAccess.GetDataList(index, apertureConstructionNames) || apertureConstructionNames == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double paneThermalTransmittance = double.NaN;
            index = Params.IndexOfInputParam("paneThermalTransmittance_");
            if (index == -1 || !dataAccess.GetData(index, ref paneThermalTransmittance) || double.IsNaN(paneThermalTransmittance))
            {
                paneThermalTransmittance = double.NaN;
            }

            double frameThermalTransmittance = double.NaN;
            index = Params.IndexOfInputParam("frameThermalTransmittance_");
            if (index == -1 || !dataAccess.GetData(index, ref frameThermalTransmittance) || double.IsNaN(frameThermalTransmittance))
            {
                frameThermalTransmittance = double.NaN;
            }

            if(double.IsNaN(paneThermalTransmittance) && double.IsNaN(frameThermalTransmittance))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide frame and/or pane thermal transmittance");
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

            index = Params.IndexOfOutputParam("apertureConstructionCalculationData");
            if (index != -1)
            {
                if (string.IsNullOrWhiteSpace(apertureConstructionName) || double.IsNaN(paneThermalTransmittance) || heatFlowDirection == HeatFlowDirection.Undefined)
                {
                    dataAccess.SetData(index, null);
                    return;
                }

                ApertureConstructionCalculationData apertureConstructionCalculationData = new ApertureConstructionCalculationData(apertureType, apertureConstructionName, apertureConstructionNames, paneThermalTransmittance, frameThermalTransmittance, heatFlowDirection, external);
                apertureConstructionCalculationData.ThicknessRange = new Range<double>(minThickness_Temp, maxThickness_Temp);

                dataAccess.SetData(index, new GooThermalTransmittanceCalculationData(apertureConstructionCalculationData));
            }
        }
    }
}