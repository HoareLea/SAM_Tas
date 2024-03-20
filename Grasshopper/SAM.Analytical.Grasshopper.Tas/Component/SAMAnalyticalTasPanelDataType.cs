using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using Grasshopper.Kernel;
using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalTasPanelDataType : GH_SAMEnumComponent<PanelDataType>
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid( "e6728078-da0c-4e9d-ba51-702c8ba49bd8");

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
        /// Tas Panel Data Enum Component
        /// </summary>
        public SAMAnalyticalTasPanelDataType()
          : base("SAMAnalytical.PanelDataType", "SAMAnalytical.PanelDataType",
              "Selects the panel data type.",
              "SAM", "Tas")
        {
        }
    }
}