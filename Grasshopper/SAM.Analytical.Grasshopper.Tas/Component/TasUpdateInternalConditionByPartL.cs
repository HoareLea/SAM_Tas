using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasUpdateInternalConditionByPartL : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("51b3faa7-adfa-4668-9fce-0498f96b9df4");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;



        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasUpdateInternalConditionByPartL()
          : base("Tas.UpdateInternalConditionByPartL", "Tas.UpdateInternalConditionByPartL",
              "Update InternalCondition By PartL",
              "SAM", "Tas")
        {
        }

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_FilePath { Name = "_path_TBD", NickName = "_path_TBD", Description = "Path to TBD Document", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_FilePath { Name = "path_TIC_", NickName = "path_TIC_", Description = "Path to TIC Document", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "fileName_", NickName = "fileName_", Description = "New file name for TBD Document", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                
                global::Grasshopper.Kernel.Parameters.Param_Boolean boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Run", Access = GH_ParamAccess.item };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));
                
                return result.ToArray();
            }
        }


        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam { Name = "analyticalModel", NickName = "analyticalModel", Description = "SAM Analytical AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_FilePath { Name = "path", NickName = "path", Description = "TBD output file path", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean { Name = "successful", NickName = "successful", Description = "Successful", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_Successful = Params.IndexOfInputParam("successful");
            if(index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, false);
            }

            int index;

            index = Params.IndexOfInputParam("_run");

            bool run = false;
            if (index == -1 || !dataAccess.GetData(index, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if (!run)
                return;

            index = Params.IndexOfInputParam("_path_TBD");
            string path_TBD = null;
            if (index == -1 || !dataAccess.GetData(index, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if(!System.IO.File.Exists(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "TBD File does not exists");
                return;
            }

            index = Params.IndexOfInputParam("_analyticalModel");
            AnalyticalModel analyticalModel = null;
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TIC = null;
            index = Params.IndexOfInputParam("path_TIC_");
            if(index != -1)
            {
                dataAccess.GetData(index, ref path_TIC);
            }


            if(string.IsNullOrWhiteSpace(path_TIC))
            {
                path_TIC = null;
            }

            if (!System.IO.File.Exists(path_TIC))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "TIC File does not exists");
                return;
            }

            index = Params.IndexOfInputParam("fileName_");
            string fileName = null;
            if (index != -1)
            {
                dataAccess.GetData(index, ref fileName);
            }

            if(string.IsNullOrWhiteSpace(fileName))
            {
                fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD) + "_PartL" + System.IO.Path.GetExtension(path_TBD);
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid file name");
                return;
            }

            string path_TBD_Destination = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path_TBD), fileName);

            System.IO.File.Copy(path_TBD, path_TBD_Destination, true);

            bool result = false;
            using (Core.Tas.SAMTBDDocument sAMTBDDocument = new Core.Tas.SAMTBDDocument(path_TBD_Destination))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;
                using (Core.Tas.SAMTICDocument sAMTICDocument = new Core.Tas.SAMTICDocument(path_TIC, true))
                {
                    TIC.Document tICDocument = sAMTICDocument.Document;

                    result = Analytical.Tas.Modify.UpdateInternalConditionByPartL(tBDDocument, tICDocument, analyticalModel);
                }
            }

            index = Params.IndexOfOutputParam("analyticalModel");
            if(index != -1)
            {
                dataAccess.SetData(index, new GooAnalyticalModel(analyticalModel));
            }

            index = Params.IndexOfOutputParam("path");
            if (index != -1)
            {
                dataAccess.SetData(index, path_TBD_Destination);
            }

            index = Params.IndexOfOutputParam("path");
            if (index != -1)
            {
                dataAccess.SetData(index, path_TBD_Destination);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, result);
            }
        }
    }
}