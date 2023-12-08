using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class GooSystemModel : GooJSAMObject<SystemModel>
    {
        public GooSystemModel()
            : base()
        {
        }

        public GooSystemModel(SystemModel systemModel)
            : base(systemModel)
        {
        }

        public override IGH_Goo Duplicate()
        {
            return new GooSystemModel(Value);
        }
    }

    public class GooSystemModelParam : GH_PersistentParam<GooSystemModel>
    {
        public override Guid ComponentGuid => new Guid("f0eff9bf-449a-472b-adfd-0beb76595819");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public GooSystemModelParam()
            : base(typeof(SystemModel).Name, typeof(SystemModel).Name, typeof(SystemModel).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooSystemModel> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooSystemModel value)
        {
            throw new NotImplementedException();
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Save As...", Menu_SaveAs, VolatileData.AllData(true).Any());

            //Menu_AppendSeparator(menu);

            base.AppendAdditionalMenuItems(menu);
        }

        private void Menu_SaveAs(object sender, EventArgs e)
        {
            Core.Grasshopper.Query.SaveAs(VolatileData);
        }
    }
}