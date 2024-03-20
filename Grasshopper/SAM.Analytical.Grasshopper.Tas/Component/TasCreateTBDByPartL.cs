using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasCreateTBDByPartL : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("51b3faa7-adfa-4668-9fce-0498f96b9df4");

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
        public TasCreateTBDByPartL()
          : base("Tas.CreateTBDByPartL", "Tas.CreateTBDByPartL",
              "Create TBD File By PartL",
              "SAM WIP", "Tas")
        {
        }

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_FilePath { Name = "_path_TBD", NickName = "_path_TBD", Description = "Path to TBD Document", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_FilePath filePath;

                filePath = new global::Grasshopper.Kernel.Parameters.Param_FilePath { Name = "path_TIC_", NickName = "path_TIC_", Description = "Path to TIC Document (Internal Conditions)", Access = GH_ParamAccess.item, Optional = true };
                filePath.SetPersistentData(Analytical.Tas.Query.DefaultPath(Analytical.Tas.TasSettingParameter.DefaultTICFileName));
                result.Add(new GH_SAMParam(filePath, ParamVisibility.Voluntary));

                filePath = new global::Grasshopper.Kernel.Parameters.Param_FilePath { Name = "path_TCR_", NickName = "path_TCR_", Description = "Path to TCR Document (Calendar)", Access = GH_ParamAccess.item, Optional = true };
                filePath.SetPersistentData(Analytical.Tas.Query.DefaultPath(Analytical.Tas.TasSettingParameter.DefaultTCRFileName));
                result.Add(new GH_SAMParam(filePath, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_calendarName_", NickName = "_calendarName_", Description = "Calendar Name", Access = GH_ParamAccess.item };
                @string.SetPersistentData("NCM Standard");

                result.Add(new GH_SAMParam(@string, ParamVisibility.Voluntary));
                
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "fileName_", NickName = "fileName_", Description = "New file name for TBD Document", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                
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
            int index_Successful = Params.IndexOfOutputParam("successful");
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
                path_TIC = Analytical.Tas.Query.DefaultPath(Analytical.Tas.TasSettingParameter.DefaultTICFileName);
            }

            if (!System.IO.File.Exists(path_TIC))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "TIC File does not exists");
                return;
            }

            string path_TCR = null;
            index = Params.IndexOfInputParam("path_TCR_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref path_TCR);
            }

            if (string.IsNullOrWhiteSpace(path_TCR))
            {
                path_TCR = Analytical.Tas.Query.DefaultPath(Analytical.Tas.TasSettingParameter.DefaultTCRFileName);
            }

            if (!System.IO.File.Exists(path_TCR))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "TCR File does not exists");
                return;
            }

            string calendarName = null;
            index = Params.IndexOfInputParam("_calendarName_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref calendarName);
            }

            if(string.IsNullOrWhiteSpace(calendarName))
            {
                calendarName = "NCM Standard";
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

            bool result = Analytical.Tas.Create.TBD_ByPartL(analyticalModel, path_TBD, out string path_TBD_Destination, path_TCR, path_TIC, fileName, calendarName);

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

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, result);
            }
        }
    }
}