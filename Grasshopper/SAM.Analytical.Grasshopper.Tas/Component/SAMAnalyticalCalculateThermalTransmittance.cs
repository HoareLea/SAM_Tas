using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalCalculateThermalTransmittance : GH_SAMVariableOutputParameterComponent
    {
        private double tolerance = 0.0001;
        
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("5d74d30d-0b06-465c-8856-3f861635daba");

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
        public SAMAnalyticalCalculateThermalTransmittance()
          : base("SAMAnalytical.CalculateUValues", "SAMAnalytical.CalculateUValues",
              "Calculate Calculate UValues (Thermal Transmittances)",
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
                result.Add(new GH_SAMParam(new GooConstructionParam() { Name = "_constructions", NickName = "_constructions", Description = "SAM Constructions", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "panelTypes_", NickName = "panelTypes_", Description = "SAM Panel Types", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooThermalTransmittanceCalculationResultParam() { Name = "thermalTransmittanceCalculationResults", NickName = "thermalTransmittanceCalculationResults", Description = "ThermalTransmittanceCalculationResults", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number { Name = "uValues", NickName = "uValues", Description = "Thermal Transmittances", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
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

            List<Construction> constructions = new List<Construction>();
            index = Params.IndexOfInputParam("_constructions");
            if (index == -1 || !dataAccess.GetDataList(index, constructions) || constructions == null || constructions.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);
            List<ThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults = thermalTransmittanceCalculator.Calculate(constructions.ConvertAll(x => x.Guid));

            index = Params.IndexOfOutputParam("constructionManager");
            if (index != -1)
            {
                dataAccess.SetData(index, constructionManager);
            }

            index = Params.IndexOfOutputParam("thermalTransmittanceCalculationResults");
            if (index != -1)
            {
                dataAccess.SetDataList(index, thermalTransmittanceCalculationResults.ConvertAll(x => new GooThermalTransmittanceCalculationResult(x)));
            }

            List<PanelType> panelTypes = new List<PanelType>();

            List<string> panelTypesTexts = new List<string>();
            index = Params.IndexOfInputParam("panelTypes_");
            if (index != -1 && dataAccess.GetDataList(index, panelTypesTexts) && panelTypesTexts != null && panelTypesTexts.Count != 0)
            {
                foreach(string panelTypeText in panelTypesTexts)
                {
                    PanelType panelType = Core.Query.Enum<PanelType>(panelTypeText);
                    panelTypes.Add(panelType);
                }
            }


            List<double> thermalResistances = new List<double>();
            for(int i =0; i < thermalTransmittanceCalculationResults.Count; i++)
            {
                ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult = thermalTransmittanceCalculationResults[i];
                if(thermalTransmittanceCalculationResult == null)
                {
                    thermalResistances.Add(double.NaN);
                    continue;
                }

                Construction construction = constructionManager.Constructions.Find(x => x.Guid.ToString() == thermalTransmittanceCalculationResult.Reference);
                PanelType panelType = PanelType.Undefined;
                if(panelTypes != null && panelTypes.Count > 0)
                {
                    panelType = i < panelTypes.Count ? panelTypes[i] : panelTypes.Last();
                }

                if(panelType == PanelType.Undefined && construction != null)
                {
                    panelType = construction.PanelType();
                }

                thermalResistances.Add(thermalTransmittanceCalculationResult.GetThermalTransmittance(panelType));

            }

            index = Params.IndexOfOutputParam("uValues");
            if (index != -1)
            {
                dataAccess.SetDataList(index, thermalResistances);
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, thermalTransmittanceCalculationResults != null && thermalTransmittanceCalculationResults.Count != 0);
            }
        }
    }
}