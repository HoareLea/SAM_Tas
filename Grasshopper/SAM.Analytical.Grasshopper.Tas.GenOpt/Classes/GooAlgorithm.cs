using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class GooAlgorithm : GH_Goo<IAlgorithm>
    {
        public override bool IsValid => Value != null;


        public GooAlgorithm()
            :base()
        {

        }

        public GooAlgorithm(IAlgorithm algorithm)
            :base()
        {
            Value = algorithm;
        }
        
        public override string TypeName
        {
            get
            {
                return Value != null ? Value.GetType().FullName : typeof(Algorithm).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return typeof(Algorithm).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooAlgorithm(Value);
        }

        public override string ToString()
        {
            return typeof(Algorithm).Name;
        }

        public override bool CastFrom(object source)
        {
            object source_Temp = source;
            if (source_Temp is IGH_Goo)
            {
                source_Temp = (source as dynamic).Value;
            }

            if(source_Temp is IAlgorithm)
            {
                Value = (IAlgorithm)source_Temp;
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

    public class GooAlgorithmParam : GH_PersistentParam<GooAlgorithm>
    {
        public override Guid ComponentGuid => new Guid("c2afb89d-f839-4c80-a50c-fe9be3ebfb16");

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        public GooAlgorithmParam()
            : base(typeof(Algorithm).Name, typeof(Algorithm).Name, typeof(Algorithm).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooAlgorithm> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooAlgorithm value)
        {
            throw new NotImplementedException();
        }
    }
}
