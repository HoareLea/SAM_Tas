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
    public class GooSystemPlantRoom : GooJSAMObject<SystemPlantRoom>
    {
        public GooSystemPlantRoom()
            : base()
        {
        }

        public GooSystemPlantRoom(SystemPlantRoom systemPlantRoom)
            : base(systemPlantRoom)
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
                return Value == null ? typeof(SystemPlantRoom).Name : Value.GetType().Name;
            }
        }
    }

    public class GooSystemPlantRoomParam : GH_PersistentParam<GooSystemPlantRoom>
    {
        public override Guid ComponentGuid => new Guid("f8edccd6-bb65-4095-8ba0-da179b2d4225");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public GooSystemPlantRoomParam()
            : base(typeof(SystemPlantRoom).Name, typeof(SystemPlantRoom).Name, typeof(SystemPlantRoom).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooSystemPlantRoom> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooSystemPlantRoom value)
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