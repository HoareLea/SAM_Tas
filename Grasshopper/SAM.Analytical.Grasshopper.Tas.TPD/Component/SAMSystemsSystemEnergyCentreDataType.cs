using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Core.Grasshopper;
using System;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class SAMSystemsSystemEnergyCentreDataType : GH_SAMEnumComponent<SystemEnergyCentreDataType>
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("2ce0997f-a5e2-457d-9baa-39fbc2a5ebb6");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        /// <summary>
        /// Panel Type
        /// </summary>
        public SAMSystemsSystemEnergyCentreDataType()
          : base("SAMSystems.SystemEnergyCentreDataType", "SAMSystems.SystemEnergyCentreDataType",
              "Select SystemEnergyCentreDataType",
              "SAM", "Analytical")
        {
        }
    }
}