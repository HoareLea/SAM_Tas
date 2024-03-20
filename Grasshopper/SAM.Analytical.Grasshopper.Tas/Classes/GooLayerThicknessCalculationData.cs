using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class GooLayerThicknessCalculationData : GH_Goo<LayerThicknessCalculationData>
    {
        public override bool IsValid => Value != null;


        public GooLayerThicknessCalculationData()
            :base()
        {

        }

        public GooLayerThicknessCalculationData(LayerThicknessCalculationData layerThicknessCalculationData)
            :base()
        {
            Value = layerThicknessCalculationData;
        }
        
        public override string TypeName
        {
            get
            {
                return typeof(LayerThicknessCalculationData).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return typeof(LayerThicknessCalculationData).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooLayerThicknessCalculationData(Value);
        }

        public override string ToString()
        {
            return typeof(LayerThicknessCalculationData).Name;
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

    public class GooLayerThicknessCalculationDataParam : GH_PersistentParam<GooLayerThicknessCalculationData>
    {
        public override Guid ComponentGuid => new Guid("d1145ce7-d393-40f7-ade7-5cf02f085922");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public GooLayerThicknessCalculationDataParam()
            : base(typeof(LayerThicknessCalculationData).Name, typeof(LayerThicknessCalculationData).Name, typeof(LayerThicknessCalculationData).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooLayerThicknessCalculationData> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooLayerThicknessCalculationData value)
        {
            throw new NotImplementedException();
        }
    }
}
