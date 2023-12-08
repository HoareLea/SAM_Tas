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
    public class GooSystemObject : GooJSAMObject<ISystemObject>
    {
        public GooSystemObject()
            : base()
        {
        }

        public GooSystemObject(ISystemObject systemObject)
            : base(systemObject)
        {
        }

        public override IGH_Goo Duplicate()
        {
            return new GooSystemObject(Value);
        }

        public override bool CastFrom(object source)
        {
            return base.CastFrom(source);
        }

        public override bool CastTo<Y>(ref Y target)
        {
            return base.CastTo(ref target);
        }

        public override string TypeName
        {
            get
            {
                return Value == null ? typeof(ISystemObject).Name : Value.GetType().Name;
            }
        }
    }

    public class GooSystemObjectParam : GH_PersistentParam<GooSystemObject>
    {
        public override Guid ComponentGuid => new Guid("c1b92e9d-fd83-4748-8e6f-798aea4461a9");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public GooSystemObjectParam()
            : base(typeof(ISystemObject).Name, typeof(ISystemObject).Name, typeof(ISystemObject).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooSystemObject> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooSystemObject value)
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