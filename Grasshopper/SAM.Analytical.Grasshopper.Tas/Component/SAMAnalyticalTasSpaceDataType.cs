using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using Grasshopper.Kernel;
using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalTasSpaceDataType : GH_SAMEnumComponent<SpaceDataType>
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("e360ea25-3b42-417a-9d40-7f06c62b5c98");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Panel Type
        /// </summary>
        public SAMAnalyticalTasSpaceDataType()
          : base("SAMAnalytical.SpaceDataType", "SAMAnalytical.SpaceDataType",
              "Selects the space data type.",
              "SAM", "Tas")
        {
        }
    }
}