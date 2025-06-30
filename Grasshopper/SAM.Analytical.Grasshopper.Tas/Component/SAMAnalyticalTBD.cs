using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

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
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;

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

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "saveWeather_", NickName = "saveWeather_", Description = "Save Wetaher in Analytical Model", Optional = true, Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

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
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel) || analyticalModel == null)
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

            if(weatherData == null)
            {
                analyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out weatherData);
            }

            if(weatherData != null)
            {
                weatherData = new WeatherData(weatherData);
            }

            List<DesignDay> heatingDesignDays = new List<DesignDay>();
            index = Params.IndexOfInputParam("heatingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, heatingDesignDays) || heatingDesignDays == null || heatingDesignDays.Count == 0)
            {
                heatingDesignDays = null;
            }

            if (heatingDesignDays == null)
            {
                if(analyticalModel.TryGetValue(AnalyticalModelParameter.HeatingDesignDays, out SAMCollection<DesignDay> heatingDesignDays_Temp) && heatingDesignDays_Temp != null)
                {
                    heatingDesignDays = new List<DesignDay>(heatingDesignDays_Temp);
                }
            }

            if(heatingDesignDays != null)
            {
                heatingDesignDays = heatingDesignDays.ConvertAll(x => x?.Clone());
            }

            List<DesignDay> coolingDesignDays = new List<DesignDay>();
            index = Params.IndexOfInputParam("coolingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, coolingDesignDays) || coolingDesignDays == null || coolingDesignDays.Count == 0)
            {
                coolingDesignDays = null;
            }

            if (coolingDesignDays == null)
            {
                if (analyticalModel.TryGetValue(AnalyticalModelParameter.CoolingDesignDays, out SAMCollection<DesignDay> coolingDesignDays_Temp) && coolingDesignDays_Temp != null)
                {
                    coolingDesignDays = new List<DesignDay>(coolingDesignDays_Temp);
                }
            }

            if (coolingDesignDays != null)
            {
                coolingDesignDays = coolingDesignDays.ConvertAll(x => x?.Clone());
            }

            Analytical.Tas.Convert.ToTBD(analyticalModel, path, weatherData, coolingDesignDays, heatingDesignDays);

            bool saveWeather = false;
            index = Params.IndexOfInputParam("saveWeather_");
            if (index != -1 && dataAccess.GetData(index, ref saveWeather) && saveWeather)
            {
                analyticalModel = new AnalyticalModel(analyticalModel);
                analyticalModel.UpdateWeather(weatherData, coolingDesignDays, heatingDesignDays);
            }

            //if(System.IO.File.Exists(path))
            //{
            //    System.IO.File.Delete(path);
            //}

            //using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path))
            //{
            //    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

            //    if (weatherData != null)
            //    {
            //        Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, 0);
            //    }

            //    TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

            //    List<TBD.dayType> dayTypes = Grashopper.Tas.Query.DayTypes(calendar);
            //    if(dayTypes.Find(x => x.name == "HDD") == null)
            //    {
            //        TBD.dayType dayType = calendar.AddDayType();
            //        dayType.name = "HDD";
            //    }

            //    if (dayTypes.Find(x => x.name == "CDD") == null)
            //    {
            //        TBD.dayType dayType = calendar.AddDayType();
            //        dayType.name = "CDD";
            //    }

            //    Analytical.Tas.Convert.ToTBD(analyticalModel, tBDDocument);
            //    Analytical.Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

            //    if (coolingDesignDays != null || heatingDesignDays != null)
            //    {
            //        Analytical.Tas.Modify.AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
            //    }

            //    sAMTBDDocument.Save();
            //}

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
                dataAccess.SetData(index, analyticalModel);

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, true);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Open TBD", Menu_OpenTBD, Resources.SAM_TasTBD3, true, false);
        }

        private void Menu_OpenTBD(object sender, EventArgs e)
        {
            int index_Path = Params.IndexOfInputParam("_path_TBD");
            if (index_Path == -1)
            {
                return;
            }

            string path = null;

            object @object = null;

            @object = Params.Input[index_Path].VolatileData.AllData(true)?.OfType<object>()?.ElementAt(0);
            if (@object is IGH_Goo)
            {
                path = (@object as dynamic).Value?.ToString();
            }

            if(string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            Core.Query.StartProcess(path);
        }
    }
}