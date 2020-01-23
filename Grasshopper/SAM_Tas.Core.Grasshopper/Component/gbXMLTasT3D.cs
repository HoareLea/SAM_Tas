using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Properties;

namespace SAM.Core.Grasshopper
{
    public class gbXMLTasT3D : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public gbXMLTasT3D()
          : base("gbXML.TasT3D", "TasT3D",
              "Import gbXML To Tas T3D",
              "SAM", "Tas")
        {
        }

    //    namespace SAM_TasCoreDynamo
    //{
    //    public static class T3D
    //    {
    //        public static bool BygbXML(string path_T3D, string path_gbXML, bool @override = true, bool fixNormals = true, bool zonesFromSpaces = true)
    //        {
    //            return SAM.Core.Tas.Import.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces);
    //        }

    //    }
    //}

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

            //aIndex = inputParamManager.AddBooleanParameter("_override_", "override", "bool override import setting for gbXML file", GH_ParamAccess.item, true);
            //booleanParameter = (Param_Boolean)inputParamManager[aIndex];
            // booleanParameter.PersistentData.Append(new GH_Boolean(true));

            //aIndex = inputParamManager.AddBooleanParameter("_fixNormals_", "fixNormals", "bool Reverse wrong normals using Tas internal engine", GH_ParamAccess.item);
            //booleanParameter = (Param_Boolean)inputParamManager[aIndex];
            //booleanParameter.Optional = true;
            //booleanParameter.PersistentData.Append(new GH_Boolean(true));

            //aIndex = inputParamManager.AddBooleanParameter("_zonesFromSpaces_", "zonesFromSpaces", "bool transforms Spaces for internal Tas Zones using Tas internal engine", GH_ParamAccess.item);
            //booleanParameter = (Param_Boolean)inputParamManager[aIndex];
            //booleanParameter.Optional = true;
            //booleanParameter.PersistentData.Append(new GH_Boolean(true));

            //aIndex = inputParamManager.AddBooleanParameter("run_", "run_", "Connect Bool Toggle to run", GH_ParamAccess.item);
            //booleanParameter = (Param_Boolean)inputParamManager[aIndex];
            //booleanParameter.Optional = true;
            //booleanParameter.PersistentData.Append(new GH_Boolean(false));
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddBooleanParameter("Success", "Sc", "Success", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            GH_ObjectWrapper objectWrapper = null;

            if (!dataAccess.GetData(5, ref objectWrapper) || objectWrapper.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

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


            bool result = Tas.Import.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces);


            //SAM.Core.Tas.Import.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces);

            //IGeometry geometry = objectWrapper.Value as IGeometry;

            dataAccess.SetData(0, result);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.SAM_TasT3D;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ae9cae75-6909-421f-8e68-f541703347b3"); }
        }
    }
}