using Grasshopper.Kernel;
using SAM.Core.Grasshopper.Tas.Properties;
using System;

namespace SAM.Core.Grasshopper.Tas
{
    public class TasCreateSurfaceOutputSpec : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("d928f41d-d39e-4fa7-811d-b42ba33b6c12");

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
        public TasCreateSurfaceOutputSpec()
          : base("Tas.CreateSurfaceOutputSpec", "TasCreateSurfaceOutputSpec",
              "Creates SAM SurfaceOutputSpec \n * For Condensation you need also convection, temperature \n* For LongWave you need also solarGain, condensation, convection.",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            int index = -1;

            index = inputParamManager.AddTextParameter("_name_", "_name_", "Name", GH_ParamAccess.item, "SAM SurfaceOutputSpec");
            inputParamManager[index].Optional = true;
            
            index = inputParamManager.AddTextParameter("_description_", "_description_", "Description", GH_ParamAccess.item);
            inputParamManager[index].Optional = true;

            index = inputParamManager.AddBooleanParameter("_apertureData_", "_apertureData_", "Aperture Data", GH_ParamAccess.item, false);
            inputParamManager[index].Optional = true;

            index = inputParamManager.AddBooleanParameter("_condensation_", "_condensation_", "Condensation \n* you need also  convection, temperature", GH_ParamAccess.item, false);
            inputParamManager[index].Optional = true;

            index = inputParamManager.AddBooleanParameter("_convection_", "_convection_", "Convection", GH_ParamAccess.item, false);
            inputParamManager[index].Optional = true;

            index = inputParamManager.AddBooleanParameter("_solarGain_", "_solarGain_", "Solar Gain", GH_ParamAccess.item, false);
            inputParamManager[index].Optional = true;

            index = inputParamManager.AddBooleanParameter("_conduction_", "_conduction_", "Conduction", GH_ParamAccess.item, false);
            inputParamManager[index].Optional = true;

            index = inputParamManager.AddBooleanParameter("_longWave_", "_longWave_", "LongWave \n* you need also solarGain, condensation, convection  ", GH_ParamAccess.item, false);
            inputParamManager[index].Optional = true;

            index = inputParamManager.AddBooleanParameter("_temperature_", "_temperature_", "Temperature", GH_ParamAccess.item, false);
            inputParamManager[index].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new GooSurfaceOutputSpecParam(), "surfaceOutputSpec", "surfaceOutputSpec", "SAM Core Tas SurfaceOutputSpec", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            string name = null;
            dataAccess.GetData(0, ref name);
            Core.Tas.SurfaceOutputSpec surfaceOutputSpec = new Core.Tas.SurfaceOutputSpec(name);

            string description = null;
            dataAccess.GetData(1, ref description);
            surfaceOutputSpec.Description = description;

            bool apertureData = false;
            dataAccess.GetData(2, ref apertureData);
            surfaceOutputSpec.ApertureData = apertureData;

            bool condensation = false;
            dataAccess.GetData(3, ref condensation);
            surfaceOutputSpec.Condensation = condensation;

            bool convection = false;
            dataAccess.GetData(4, ref convection);
            surfaceOutputSpec.Convection = convection;

            bool solarGain = false;
            dataAccess.GetData(5, ref solarGain);
            surfaceOutputSpec.SolarGain = solarGain;

            bool conduction = false;
            dataAccess.GetData(6, ref conduction);
            surfaceOutputSpec.Conduction = conduction;

            bool longWave = false;
            dataAccess.GetData(7, ref longWave);
            surfaceOutputSpec.LongWave = longWave;

            bool temperature = false;
            dataAccess.GetData(8, ref temperature);
            surfaceOutputSpec.Temperature = temperature;


            dataAccess.SetData(0, surfaceOutputSpec);
        }
    }
}