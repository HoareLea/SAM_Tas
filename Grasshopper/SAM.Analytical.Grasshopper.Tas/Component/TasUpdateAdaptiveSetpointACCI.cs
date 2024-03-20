using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasUpdateAdaptiveSetpointACCI : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("12c19e3e-6cf1-4112-b7a4-0c4bcaa408e2");

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
        public TasUpdateAdaptiveSetpointACCI()
          : base("Tas.UpdateAdaptiveSetpointACCI", "Tas.UpdateAdaptiveSetpointACCI",
        "https://accim.readthedocs.io/en/latest/4_detailed%20use.html\n" +
        "https://github.com/dsanchez-garcia/accim/blob/master/accim/sample_files/jupyter_notebooks/addAccis/using_addAccis.ipynb\n" +
        "Adopted 2->INT ASHRAE55->90->3->Adap. Limits   Horizont. Extended\n" +
        "https://htmlpreview.github.io/?https://github.com/dsanchez-garcia/accim/blob/master/accim/docs/html_files/full_setpoint_table.html\n\n" +
        "1. Input Parameter:\n" +
        "   - dryBulbTemperature (External Weather Data): The dry bulb temperature from the weather data, which is used to calculate the upper and lower limits.\n" +
        "\n" +
        "2. Conditions:\n" +
        "   - Within Range (10°C to 33.5°C):\n" +
        "     - If the dryBulbTemperature is between 10°C and 33.5°C, inclusive, the following equations are used to calculate the upper and lower limits:\n" +
        "       - upperLimit = (dryBulbTemperature * 0.31) + 17.8 + 3.5\n" +
        "       - lowerLimit = (dryBulbTemperature * 0.31) + 17.8 - 3.5\n" +
        "   - Below Range (< 10°C):\n" +
        "     - If the dryBulbTemperature is below 10°C, a fixed dry bulb temperature of 10°C is used in the equations to calculate the upper and lower limits:\n" +
        "       - upperLimit = (10 * 0.31) + 17.8 + 3.5\n" +
        "       - lowerLimit = (10 * 0.31) + 17.8 - 3.5\n" +
        "   - Above Range (> 33.5°C):\n" +
        "     - If the dryBulbTemperature is above 33.5°C, a fixed dry bulb temperature of 33.5°C is used in the equations to calculate the upper and lower limits:\n" +
        "       - upperLimit = (33.5 * 0.31) + 17.8 + 3.5\n" +
        "       - lowerLimit = (33.5 * 0.31) + 17.8 - 3.5\n" +
        "\n" +
        "3. Output Parameters:\n" +
        "   - upperLimit: The calculated upper limit value.\n" +
        "   - lowerLimit: The calculated lower limit value.",
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

            inputParamManager.AddTextParameter("_pathTasTBD", "pathTasTBD", "The string path to TasTBD file.", GH_ParamAccess.item);

            GooSpaceParam spaceParam = new GooSpaceParam() { Name = "spaces_", NickName = "spaces_", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list, Optional = true };
            inputParamManager.AddParameter(spaceParam);
            
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
            if (!dataAccess.GetData(2, ref run))
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

            List<Space> spaces = new List<Space>();
            if (!dataAccess.GetDataList(1, spaces) || spaces.Count == 0)
            {
                spaces = null;
            }

            bool result = Analytical.Tas.Modify.UpdateACCI(path_TBD, spaces?.ConvertAll(x => x?.Name));

            dataAccess.SetData(0, result);
        }
    }
}