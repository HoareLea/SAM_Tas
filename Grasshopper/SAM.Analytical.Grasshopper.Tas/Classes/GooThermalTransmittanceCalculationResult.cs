using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class GooThermalTransmittanceCalculationResult : GH_Goo<IThermalTransmittanceCalculationResult>
    {
        public override bool IsValid => Value != null;


        public GooThermalTransmittanceCalculationResult()
            :base()
        {

        }

        public GooThermalTransmittanceCalculationResult(IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult)
            :base()
        {
            Value = thermalTransmittanceCalculationResult;
        }
        
        public override string TypeName
        {
            get
            {
                return Value != null ? Value.GetType().FullName : typeof(IThermalTransmittanceCalculationResult).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return Value != null ? Value.GetType().FullName.Replace(".", " ") : typeof(IThermalTransmittanceCalculationResult).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooThermalTransmittanceCalculationResult(Value);
        }

        public override string ToString()
        {
            return Value != null ? Value.GetType().Name : typeof(IThermalTransmittanceCalculationResult).Name;
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

    public class GooThermalTransmittanceCalculationResultParam : GH_PersistentParam<GooThermalTransmittanceCalculationResult>
    {
        public override Guid ComponentGuid => new Guid("4a10e155-9ec9-4530-94d5-56e4db812a61");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public GooThermalTransmittanceCalculationResultParam()
            : base(typeof(IThermalTransmittanceCalculationResult).Name, typeof(IThermalTransmittanceCalculationResult).Name, typeof(IThermalTransmittanceCalculationResult).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooThermalTransmittanceCalculationResult> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooThermalTransmittanceCalculationResult value)
        {
            throw new NotImplementedException();
        }
    }
}
