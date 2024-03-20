using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class GooSAMT3DDocument : GH_Goo<SAMT3DDocument>
    {
        public override bool IsValid => Value != null;

        public GooSAMT3DDocument(SAMT3DDocument sAMT3DDocument)
        {
            Value = sAMT3DDocument;
        }
        
        public override string TypeName
        {
            get
            {
                return typeof(SAMT3DDocument).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return typeof(SAMT3DDocument).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooSAMT3DDocument(Value);
        }

        public override string ToString()
        {
            return typeof(SAMT3DDocument).Name;
        }
    }

    public class GooSAMT3DDocumentParam : GH_PersistentParam<GooSAMT3DDocument>
    {
        public override Guid ComponentGuid => new Guid("1e33f41d-75d3-49fb-a3eb-adab7d71ff73");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasT3D;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public GooSAMT3DDocumentParam()
            : base(typeof(SAMT3DDocument).Name, typeof(SAMT3DDocument).Name, typeof(SAMT3DDocument).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooSAMT3DDocument> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooSAMT3DDocument value)
        {
            throw new NotImplementedException();
        }
    }
}
