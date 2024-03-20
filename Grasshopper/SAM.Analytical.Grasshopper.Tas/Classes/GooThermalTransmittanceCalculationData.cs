using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class GooThermalTransmittanceCalculationData : GH_Goo<IThermalTransmittanceCalculationData>
    {
        public override bool IsValid => Value != null;


        public GooThermalTransmittanceCalculationData()
            :base()
        {

        }

        public GooThermalTransmittanceCalculationData(IThermalTransmittanceCalculationData thermalTransmittanceCalculationData)
            :base()
        {
            Value = thermalTransmittanceCalculationData;
        }
        
        public override string TypeName
        {
            get
            {
                return Value != null ? Value.GetType().FullName : typeof(IThermalTransmittanceCalculationData).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return Value != null ? Value.GetType().FullName.Replace(".", " ") : typeof(IThermalTransmittanceCalculationData).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooThermalTransmittanceCalculationData(Value);
        }

        public override string ToString()
        {
            return Value != null ? Value.GetType().Name : typeof(IThermalTransmittanceCalculationData).Name;
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

            if(typeof(Y).IsAssignableFrom(Value.GetType()))
            {
                target = (Y)(object)Value;
                return true;
            }

            return base.CastTo(ref target);
        }
    }

    public class GooThermalTransmittanceCalculationDataParam : GH_PersistentParam<GooThermalTransmittanceCalculationData>
    {
        public override Guid ComponentGuid => new Guid("8ac71a02-4907-482b-b905-b5a855e2ef29");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public GooThermalTransmittanceCalculationDataParam()
            : base(typeof(IThermalTransmittanceCalculationData).Name, typeof(IThermalTransmittanceCalculationData).Name, typeof(IThermalTransmittanceCalculationData).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooThermalTransmittanceCalculationData> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooThermalTransmittanceCalculationData value)
        {
            throw new NotImplementedException();
        }
    }
}
