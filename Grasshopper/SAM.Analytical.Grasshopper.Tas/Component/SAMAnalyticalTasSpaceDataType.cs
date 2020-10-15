using GH_IO.Serialization;
using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core.Grasshopper;
using System;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalTasSpaceDataType : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("e360ea25-3b42-417a-9d40-7f06c62b5c98");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD;

        private SpaceDataType spaceDataType = SpaceDataType.Undefined;

        /// <summary>
        /// Panel Type
        /// </summary>
        public SAMAnalyticalTasSpaceDataType()
          : base("SAMAnalytical.SpaceDataType", "SAMAnalytical.SpaceDataType",
              "Select Space Data Type",
              "SAM", "Tas")
        {
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("SpaceDataType", (int)spaceDataType);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            int aIndex = -1;
            if (reader.TryGetInt32("SpaceDataType", ref aIndex))
                spaceDataType = (SpaceDataType)aIndex;

            return base.Read(reader);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            foreach (SpaceDataType spaceDataType in Enum.GetValues(typeof(SpaceDataType)))
                Menu_AppendItem(menu, spaceDataType.ToString(), Menu_PanelTypeChanged, true, spaceDataType == this.spaceDataType).Tag = spaceDataType;
        }

        private void Menu_PanelTypeChanged(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is ApertureType)
            {
                //Do something with panelType
                this.spaceDataType = (SpaceDataType)item.Tag;
                ExpireSolution(true);
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddGenericParameter("SpaceDataType", "SpaceDataType", "SAM Analytical SpaceDataType", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            dataAccess.SetData(0, spaceDataType);
        }
    }
}