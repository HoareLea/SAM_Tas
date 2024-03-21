using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class GooObjective : GH_Goo<Objective>
    {
        public override bool IsValid => Value != null;


        public GooObjective()
            :base()
        {

        }

        public GooObjective(Objective objective)
            :base()
        {
            Value = objective;
        }
        
        public override string TypeName
        {
            get
            {
                return Value != null ? Value.GetType().FullName : typeof(Objective).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return typeof(Objective).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooObjective(Value);
        }

        public override string ToString()
        {
            return typeof(Objective).Name;
        }

        public override bool CastFrom(object source)
        {
            object source_Temp = source;
            if (source_Temp is IGH_Goo)
            {
                source_Temp = (source as dynamic).Value;
            }

            if (source_Temp is string)
            {
                Value = new Objective((string)source_Temp);
                return true;
            }
            
            return base.CastFrom(source);
        }

        public override bool CastTo<Y>(ref Y target)
        {
            if (Value == null)
            {
                return false;
            }

            if (typeof(Y).IsAssignableFrom(Value?.GetType()))
            {
                target = (Y)(object)Value;
                return true;
            }

            return base.CastTo(ref target);
        }
    }

    public class GooObjectiveParam : GH_PersistentParam<GooObjective>
    {
        public override Guid ComponentGuid => new Guid("efa8de4a-5379-44c1-a01d-b6f21fabc2bc");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_GenOpt;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public GooObjectiveParam()
            : base(typeof(Objective).Name, typeof(Objective).Name, typeof(Objective).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooObjective> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooObjective value)
        {
            throw new NotImplementedException();
        }
    }
}
