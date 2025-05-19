using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Tas.Properties;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Grasshopper.Tas
{
    public class GooSurfaceOutputSpec : GooJSAMObject<SurfaceOutputSpec>
    {
        public GooSurfaceOutputSpec()
            : base()
        {
        }

        public GooSurfaceOutputSpec(SurfaceOutputSpec surfaceOutputSpec)
            : base(surfaceOutputSpec)
        {
        }

        public override IGH_Goo Duplicate()
        {
            return new GooSurfaceOutputSpec(Value);
        }
    }

    public class GooSurfaceOutputSpecParam : GH_PersistentParam<GooSurfaceOutputSpec>
    {
        public override Guid ComponentGuid => new Guid("167fe3c5-7dcf-415e-b4e0-a40dc9159b31");

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public GooSurfaceOutputSpecParam()
            : base(typeof(SurfaceOutputSpec).Name, typeof(SurfaceOutputSpec).Name, typeof(SurfaceOutputSpec).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooSurfaceOutputSpec> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooSurfaceOutputSpec value)
        {
            throw new NotImplementedException();
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Save As...", Menu_SaveAs, Resources.SAM_Small3, VolatileData.AllData(true).Any());

            //Menu_AppendSeparator(menu);

            base.AppendAdditionalMenuItems(menu);
        }

        private void Menu_SaveAs(object sender, EventArgs e)
        {
            Query.SaveAs(VolatileData);
        }
    }
}