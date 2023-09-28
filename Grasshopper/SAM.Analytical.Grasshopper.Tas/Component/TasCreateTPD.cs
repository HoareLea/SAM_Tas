using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasCreateTPD : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1308af3f-69c3-4fa7-b4ad-c4240074ce93");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Resources.SAM_TasTBD;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasCreateTPD()
          : base("Tas.CreateTPD", "Tas.CreateTPD",
              "Tas Create TPD",
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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "analyticalModel_", NickName = "_analyticalModel_", Description = "SAM Analytical Model", Access = GH_ParamAccess.item, Optional = true}, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_path_TSD", NickName = "_path_TSD", Description = "A file path to a Tas file TSD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                //result.Add(new GH_SAMParam(new Weather.Grasshopper.GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                //result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                //result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "analyticalModel", NickName = "analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                //result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "totalConsumption", NickName = "totalConsumption", Description = "Total Consumption [kWh]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
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

            string path_TSD = null;
            index = Params.IndexOfInputParam("_path_TSD");
            if (index == -1 || !dataAccess.GetData(index, ref path_TSD) || string.IsNullOrWhiteSpace(path_TSD) || !System.IO.File.Exists(path_TSD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("analyticalModel_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref analyticalModel);
            }

            string path_TPD = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path_TSD), string.Format("{0}.{1}", System.IO.Path.GetFileNameWithoutExtension(path_TSD), "tpd"));
            if (System.IO.File.Exists(path_TPD))
            {
                System.IO.File.Delete(path_TPD);
            }

            analyticalModel = analyticalModel == null ? null : new AnalyticalModel(analyticalModel);

            Analytical.Tas.Create.TPD(path_TPD, path_TSD, analyticalModel);

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
            {
                dataAccess.SetData(index, analyticalModel);
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, true);
            }
        }
    }
}