using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class GooLayerThicknessCalculationResult : GH_Goo<LayerThicknessCalculationResult>
    {
        public override bool IsValid => Value != null;

        public GooLayerThicknessCalculationResult()
            : base()
        {

        }
        public GooLayerThicknessCalculationResult(LayerThicknessCalculationResult layerThicknessCalculationResult)
            :base()
        {
            Value = layerThicknessCalculationResult;
        }
        
        public override string TypeName
        {
            get
            {
                return typeof(LayerThicknessCalculationResult).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return typeof(LayerThicknessCalculationResult).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooLayerThicknessCalculationResult(Value);
        }

        public override string ToString()
        {
            return typeof(LayerThicknessCalculationResult).Name;
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

    public class GooLayerThicknessCalculationResultParam : GH_PersistentParam<GooLayerThicknessCalculationResult>
    {
        public override Guid ComponentGuid => new Guid("6b8d9a23-d379-4725-b1c2-43f18d591ccb");

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        public GooLayerThicknessCalculationResultParam()
            : base(typeof(LayerThicknessCalculationData).Name, typeof(LayerThicknessCalculationResult).Name, typeof(LayerThicknessCalculationResult).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooLayerThicknessCalculationResult> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooLayerThicknessCalculationResult value)
        {
            throw new NotImplementedException();
        }
    }
}
