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
            object source_Temp = source;
            if (source_Temp is IGH_Goo)
            {
                source_Temp = (source as dynamic).Value;
            }

            if (source_Temp is string)
            {
                string @string = (string)source_Temp;
                if (@string.Contains(","))
                {
                    string[] values = @string.Split(',');
                    if (values.Length > 5)
                    {
                        string name = values[0]?.Trim();
                        if (!string.IsNullOrWhiteSpace(name) &&
                            double.TryParse(values[1], out double initial) &&
                            double.TryParse(values[2], out double min) &&
                            double.TryParse(values[3], out double max) &&
                            double.TryParse(values[4], out double step))
                        {
                            Value = new NumberParameter() { Name = name, Initial = initial, Min = min, Max = max, Step = step };
                            return true;
                        }
                    }

                }
            }

            return base.CastFrom(source);
        }

        public override bool CastTo<Y>(ref Y target)
        {
            if (Value == null)
            {
                return false;
            }

            if(typeof(Y).IsAssignableFrom(Value?.GetType()))
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

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override System.Drawing.Bitmap Icon => Resources.SAM_GenOpt;

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
