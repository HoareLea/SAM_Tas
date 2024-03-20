using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasUpdateDesignDays : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("568df94a-ae3d-417d-b83e-b3d6af00bb1a");

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
        public TasUpdateDesignDays()
          : base("Tas.UpdateDesignDays", "Tas.UpdateDesignDays",
              "Updates the Design Days  in a TBD file.",
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

            inputParamManager.AddTextParameter("_pathTasTBD", "_pathTasTBD", "A string path to a TasTBD file", GH_ParamAccess.item);
            inputParamManager.AddParameter(new GooAnalyticalObjectParam() { Optional = true}, "_coolingDesignDays_", "_coolingDesignDays_", "The SAM Analytical Design Days for Cooling",  GH_ParamAccess.list);
            inputParamManager.AddParameter(new GooAnalyticalObjectParam() { Optional = true }, "_heatingDesignDays_", "_heatingDesignDays_", "The SAM Analytical Design Days for Heating", GH_ParamAccess.list);
            inputParamManager.AddIntegerParameter("_repetitions_", "_repetitions_", "Repetitions", GH_ParamAccess.item, 30);
            inputParamManager.AddBooleanParameter("_run", "_run", "Connect a boolean toggle to run.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddTextParameter("guids", "guids", "The GUIDS (Globally Unique Identifiers) of the Tas Design Days.", GH_ParamAccess.list);
            outputParamManager.AddBooleanParameter("successful", "successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(1, false);

            bool run = false;
            if (!dataAccess.GetData(4, ref run))
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

            List<DesignDay> coolingDesignDays = new List<DesignDay>();
            dataAccess.GetDataList(1, coolingDesignDays);

            List<DesignDay> heatingDesignDays = new List<DesignDay>();
            dataAccess.GetDataList(2, heatingDesignDays);

            int repetitions = 30;
            dataAccess.GetData(3, ref repetitions);

            List<Guid> guids = null;
            if (coolingDesignDays != null || heatingDesignDays != null)
            {
                if(coolingDesignDays.Count != 0 || heatingDesignDays.Count != 0)
                {
                    guids = Analytical.Tas.Modify.AddDesignDays(path_TBD, coolingDesignDays, heatingDesignDays, repetitions);
                }
            }

            dataAccess.SetData(0, guids?.ConvertAll(x => x.ToString()));
            dataAccess.SetData(1, guids != null);
        }
    }
}