using Grasshopper.Kernel;
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
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_T3D3;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public gbXMLTasT3D()
          : base("gbXML.TasT3D", "TasT3D",
              "Imports a gbXML file to a TasT3D file.",
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

            inputParamManager.AddTextParameter("_pathTasT3D", "_pathTasT3D", "The string path to a TasT3D file.", GH_ParamAccess.item);
            inputParamManager.AddTextParameter("_pathgbXML", "_pathgbXML", "The string path to a gbXML file.", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("_override_", "_override_", "Do you want to override the import settings for the gbXML file?", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("_fixNormals_", "_fixNormals_", "Do you want to reverse the wrong normals using the Tas internal engine?", GH_ParamAccess.item, false);
            inputParamManager.AddBooleanParameter("_zonesFromSpaces_", "_zonesFromSpaces_", "Do you want to transform the spaces for the internal Tas zones using the Tas internal engine?", GH_ParamAccess.item, true);
            inputParamManager.AddBooleanParameter("_useWidths_", "_useWidths_", "Do you want to use the building element widths?", GH_ParamAccess.item, false);
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
            if (!dataAccess.GetData(6, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            if (!run)
                return;


            string path_T3D = null;
            if (!dataAccess.GetData(0, ref path_T3D) || string.IsNullOrWhiteSpace(path_T3D))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_gbXML = null;
            if (!dataAccess.GetData(1, ref path_gbXML) || string.IsNullOrWhiteSpace(path_gbXML))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool @override = false;
            if (!dataAccess.GetData(2, ref @override))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool fixNormals = true;
            if (!dataAccess.GetData(3, ref fixNormals))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool zonesFromSpaces = true;
            if (!dataAccess.GetData(4, ref zonesFromSpaces))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool useWidths = true;
            if (!dataAccess.GetData(5, ref useWidths))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }


            bool result = Core.Tas.Convert.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces, useWidths);


            //SAM.Core.Tas.Import.ToT3D(path_T3D, path_gbXML, @override, fixNormals, zonesFromSpaces);

            //IGeometry geometry = objectWrapper.Value as IGeometry;

            dataAccess.SetData(0, result);
        }
    }
}