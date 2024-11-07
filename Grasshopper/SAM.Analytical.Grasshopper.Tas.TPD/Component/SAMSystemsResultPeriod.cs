using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Core.Grasshopper;
using SAM.Core.Tas.TPD;
using System;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class SAMSystemsResultPeriod : GH_SAMEnumComponent<ResultPeriod>
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("eb6418e9-b430-4730-a882-b77b3bcaa69d");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        /// <summary>
        /// Panel Type
        /// </summary>
        public SAMSystemsResultPeriod()
          : base("SAMSystems.ResultPeriod", "SAMSystems.ResultPeriod",
              "Select ResultPeriod",
              "SAM", "Analytical04")
        {
        }
    }
}