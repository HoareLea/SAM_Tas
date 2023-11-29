using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class GooParameter : GH_Goo<IParameter>
    {
        public override bool IsValid => Value != null;


        public GooParameter()
            :base()
        {

        }

        public GooParameter(IParameter parameter)
            :base()
        {
            Value = parameter;
        }
        
        public override string TypeName
        {
            get
            {
                return Value != null ? Value.GetType().FullName : typeof(Parameter).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return typeof(Parameter).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooParameter(Value);
        }

        public override string ToString()
        {
            return typeof(Parameter).Name;
        }

        public override bool CastFrom(object source)
        {
            return base.CastFrom(source);
        }

        public override bool CastTo<Y>(ref Y target)
        {
            if (Value == null)
            {
                return false;
            }

            if(typeof(Y) == Value?.GetType())
            {
                target = (Y)(object)Value;
                return true;
            }

            return base.CastTo(ref target);
        }
    }

    public class GooParameterParam : GH_PersistentParam<GooParameter>
    {
        public override Guid ComponentGuid => new Guid("6f222653-57cb-437c-80fe-b5ba318a4183");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        public GooParameterParam()
            : base(typeof(Parameter).Name, typeof(Parameter).Name, typeof(Parameter).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooParameter> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooParameter value)
        {
            throw new NotImplementedException();
        }
    }
}
