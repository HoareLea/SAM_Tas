using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using Grasshopper.Kernel;
using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalSizingType : GH_SAMEnumComponent<SizingType>
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("2f67b1c7-82ea-4db2-bd2a-711e97908c10");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Panel Type
        /// </summary>
        public SAMAnalyticalSizingType()
          : base("SAMAnalytical.SizingType", "SAMAnalytical.SizingType",
              "Sizing Type.",
              "SAM", "Tas")
        {
        }
    }
}