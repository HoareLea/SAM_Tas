using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasSimulate : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("787e4fd5-d41c-4b71-b98a-fb20750362b5");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasSimulate()
          : base("Tas.Simulate", "Tas.Simulate",
              "Simulates the TasTBD file.",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            int index = -1;

            index = inputParamManager.AddTextParameter("_pathTasTBD", "_pathTasTBD", "The string path to a TasTBD file.", GH_ParamAccess.item);
            inputParamManager[index].WireDisplay = GH_ParamWireDisplay.hidden;
            
            index = inputParamManager.AddTextParameter("_path_TasTSD", "pathTasTSD", "The string path to a TasTSD file.", GH_ParamAccess.item);
            inputParamManager[index].WireDisplay = GH_ParamWireDisplay.hidden;

            global::Grasshopper.Kernel.Parameters.Param_GenericObject genericObject = new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "_surfaceOutputSpec", NickName = "_surfaceOutputSpec", Description = "Surface Output Spec.", Access = GH_ParamAccess.list, Optional = true };
            inputParamManager.AddParameter(genericObject);

            inputParamManager.AddIntegerParameter("_dayFirst_", "_dayFirst_", "The first day", GH_ParamAccess.item, 1);
            inputParamManager.AddIntegerParameter("_dayLast_", "_dayLast_", "The last day", GH_ParamAccess.item, 365);
            inputParamManager.AddBooleanParameter("_run", "_run", "Connect a boolean toggle to run.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddBooleanParameter("successful", "successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(0, false);

            bool run = false;
            if (!dataAccess.GetData(5, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            if (!run)
                return;

            string path_TBD = null;
            if (!dataAccess.GetData(0, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TSD = null;
            if (!dataAccess.GetData(1, ref path_TSD) || string.IsNullOrWhiteSpace(path_TSD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            int day_First = -1;
            if (!dataAccess.GetData(3, ref day_First))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            int day_Last = -1;
            if (!dataAccess.GetData(4, ref day_Last))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<SurfaceOutputSpec> surfaceOutputSpecs = null;

            List<GH_ObjectWrapper> objectWrappers = new List<GH_ObjectWrapper>();
            if(dataAccess.GetDataList(2, objectWrappers) && objectWrappers != null && objectWrappers.Count != 0)
            {
                surfaceOutputSpecs = new List<SurfaceOutputSpec>();
                foreach (GH_ObjectWrapper objectWrapper in objectWrappers)
                {
                    object value = objectWrapper.Value;
                    if (value is IGH_Goo)
                    {
                        value = (value as dynamic)?.Value;
                    }

                    if (value is bool && ((bool)value))
                    {
                        SurfaceOutputSpec surfaceOutputSpec = new SurfaceOutputSpec("Tas.Simulate");
                        surfaceOutputSpec.SolarGain = true;
                        surfaceOutputSpec.Conduction = true;
                        surfaceOutputSpec.ApertureData = false;
                        surfaceOutputSpec.Condensation = false;
                        surfaceOutputSpec.Convection = false;
                        surfaceOutputSpec.LongWave = false;
                        surfaceOutputSpec.Temperature = false;

                        surfaceOutputSpecs.Add(surfaceOutputSpec);
                    }
                    else if(Core.Query.IsNumeric(value) && Core.Query.TryConvert(value, out double @double) && @double == 2.0)
                    {
                        surfaceOutputSpecs = new List<SurfaceOutputSpec>() { new SurfaceOutputSpec("Tas.Simulate") };
                        surfaceOutputSpecs[0].SolarGain = true;
                        surfaceOutputSpecs[0].Conduction = true;
                        surfaceOutputSpecs[0].ApertureData = true;
                        surfaceOutputSpecs[0].Condensation = true;
                        surfaceOutputSpecs[0].Convection = true;
                        surfaceOutputSpecs[0].LongWave = true;
                        surfaceOutputSpecs[0].Temperature = true;
                    }
                    else if(value is SurfaceOutputSpec)
                    {
                        surfaceOutputSpecs.Add((SurfaceOutputSpec)value);
                    }

                }
            }
            
            if (surfaceOutputSpecs != null && surfaceOutputSpecs.Count > 0)
            {
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs);
                    Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs[0].Name);
                    sAMTBDDocument.Save();
                }
            }

            bool result = Analytical.Tas.Modify.Simulate(path_TBD, path_TSD, day_First, day_Last);

            dataAccess.SetData(0, result);
        }
    }
}