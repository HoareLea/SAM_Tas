using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class SAMAnalyticalGPSHookeJeevesAlgorithm : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("98aac621-e5ca-4c7f-a226-7b79482e94c7");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_GenOpt;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalGPSHookeJeevesAlgorithm()
          : base("SAMAnalytical.GPSHookeJeevesAlgorithm", "SAMAnalytical.GPSHookeJeevesAlgorithm",
              "SAM Analytical GenOpt GPSHookeJeeves Algorithm",
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

                Param_Number number = null;
                Param_Integer integer = null;

                GPSHookeJeevesAlgorithm gPSHookeJeevesAlgorithm = new GPSHookeJeevesAlgorithm();

                number = new Param_Number() { Name = "_meshSizeDivider_", NickName = "_meshSizeDivider_", Description = "Mesh Size Divider", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(gPSHookeJeevesAlgorithm.MeshSizeDivider);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_initialMeshSizeExponent_", NickName = "_initialMeshSizeExponent_", Description = "Initial Mesh Size Exponent", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(gPSHookeJeevesAlgorithm.InitialMeshSizeExponent);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                number = new Param_Number() { Name = "_meshSizeExponentIncrement_", NickName = "_meshSizeExponentIncrement_", Description = "Mesh Size Exponent Increment", Optional = true, Access = GH_ParamAccess.item };
                number.SetPersistentData(gPSHookeJeevesAlgorithm.MeshSizeExponentIncrement);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                integer = new Param_Integer() { Name = "_numberOfStepReduction_", NickName = "_numberOfStepReduction_", Description = "Number Of Step Reduction", Optional = true, Access = GH_ParamAccess.item };
                integer.SetPersistentData(gPSHookeJeevesAlgorithm.NumberOfStepReduction);
                result.Add(new GH_SAMParam(integer, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooAlgorithmParam() { Name = "algorithm", NickName = "algorithm", Description = "Algorithm", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
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

            double meshSizeDivider = 0;
            index = Params.IndexOfInputParam("_meshSizeDivider_");
            if (index == -1 || !dataAccess.GetData(index, ref meshSizeDivider))
            {
                meshSizeDivider = 0;
            }

            double initialMeshSizeExponent = 0;
            index = Params.IndexOfInputParam("_initialMeshSizeExponent_");
            if (index == -1 || !dataAccess.GetData(index, ref initialMeshSizeExponent))
            {
                initialMeshSizeExponent = 0;
            }

            double meshSizeExponentIncrement = 0;
            index = Params.IndexOfInputParam("_meshSizeExponentIncrement_");
            if (index == -1 || !dataAccess.GetData(index, ref meshSizeExponentIncrement))
            {
                meshSizeExponentIncrement = 0;
            }

            int numberOfStepReduction = 0;
            index = Params.IndexOfInputParam("_numberOfStepReduction_");
            if (index == -1 || !dataAccess.GetData(index, ref numberOfStepReduction))
            {
                numberOfStepReduction = 0;
            }


            index = Params.IndexOfOutputParam("algorithm");
            if (index != -1)
            {
                dataAccess.SetData(index, new GPSHookeJeevesAlgorithm() 
                {  
                    MeshSizeDivider = meshSizeDivider, 
                    InitialMeshSizeExponent = initialMeshSizeExponent,
                    MeshSizeExponentIncrement = meshSizeExponentIncrement,
                    NumberOfStepReduction = numberOfStepReduction
                });
            }
        }
    }
}