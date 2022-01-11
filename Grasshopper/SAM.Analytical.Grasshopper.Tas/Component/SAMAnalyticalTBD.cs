using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using SAM.Weather;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalTBD : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("f772af0f-7969-4441-8bed-107fcffaacd5");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalTBD()
          : base("SAMAnalytical.TBD", "SAMAnalytical.TBD",
              "Converts SAM Analytical to TBD",
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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_path_TBD", NickName = "_path_TBD", Description = "A file path to a Tas file TBD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new Weather.Grasshopper.GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

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
            if(index_successful != -1)
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

            string path = null;
            index = Params.IndexOfInputParam("_path_TBD");
            if (index == -1 || !dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            WeatherData weatherData = null;
            index = Params.IndexOfInputParam("weatherData_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref weatherData))
                {
                    weatherData = null;
                }
            }

            List<DesignDay> heatingDesignDays = new List<DesignDay>();
            index = Params.IndexOfInputParam("heatingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, heatingDesignDays) || heatingDesignDays == null || heatingDesignDays.Count == 0)
            {
                heatingDesignDays = null;
            }

            List<DesignDay> coolingDesignDays = new List<DesignDay>();
            index = Params.IndexOfInputParam("coolingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, coolingDesignDays) || coolingDesignDays == null || coolingDesignDays.Count == 0)
            {
                coolingDesignDays = null;
            }

            if(System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                if (weatherData != null)
                {
                    Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData);
                }

                TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                List<TBD.dayType> dayTypes = Grashopper.Tas.Query.DayTypes(calendar);
                if(dayTypes.Find(x => x.name == "HDD") == null)
                {
                    TBD.dayType dayType = calendar.AddDayType();
                    dayType.name = "HDD";
                }

                if (dayTypes.Find(x => x.name == "CDD") == null)
                {
                    TBD.dayType dayType = calendar.AddDayType();
                    dayType.name = "CDD";
                }

                Analytical.Tas.Convert.ToTBD(analyticalModel, tBDDocument);
                Analytical.Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                if (coolingDesignDays != null || heatingDesignDays != null)
                {
                    Analytical.Tas.Modify.AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                }

                sAMTBDDocument.Save();
            }

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
                dataAccess.SetData(index, analyticalModel);

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, true);
            }
        }
    }
}