using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Tas.Properties;
using System;

namespace SAM.Core.Grasshopper.Tas
{
    public class gbXMLTasT3D : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ae9cae75-6909-421f-8e68-f541703347b3");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public gbXMLTasT3D()
          : base("gbXML.TasT3D", "TasT3D",
              "Import gbXML To Tas T3D",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            //int aIndex = -1;
            //Param_Boolean booleanParameter = null;

            inputParamManager.AddTextParameter("_path_TasT3D", "pathTasT3D", "string path to TasT3D file", GH_ParamAccess.item);
            inputParamManager.AddTextParameter("_path_gbXML", "pathgbXML", "string path to gbXML file", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("_override_", "override", "bool override import setting for gbXML file", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("_fixNormals_", "fixNormals", "bool Reverse wrong normals using Tas internal engine", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("_zonesFromSpaces_", "zonesFromSpaces", "bool transforms Spaces for internal Tas Zones using Tas internal engine", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("run_", "run_", "Connect Bool Toggle to run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddBooleanParameter("Successful", "Successful", "Correctly imported?", GH_ParamAccess.item);
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

            GH_ObjectWrapper objectWrapper = null;

            if (!(objectWrapper.Value as GH_Boolean).Value)
                return;

            if (!dataAccess.GetData(0, ref objectWrapper) || objectWrapper.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }


            string path_T3D = (objectWrapper.Value as GH_String).Value;

            if (!dataAccess.GetData(1, ref objectWrapper) || objectWrapper.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_gbXML = (objectWrapper.Value as GH_String).Value;


            if (!dataAccess.GetData(2, ref objectWrapper) || objectWrapper.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool @override = (objectWrapper.Value as GH_Boolean).Value;

            if (!dataAccess.GetData(3, ref objectWrapper) || objectWrapper.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool fixNormals = (objectWrapper.Value as GH_Boolean).Value;

            if (!dataAccess.GetData(4, ref objectWrapper) || objectWrapper.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool zonesFromSpaces = (objectWrapper.Value as GH_Boolean).Value;


            bool result = Core.Tas.Convert.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces);


            //SAM.Core.Tas.Import.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces);

            //IGeometry geometry = objectWrapper.Value as IGeometry;

            dataAccess.SetData(0, result);
        }
    }
}