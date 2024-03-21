using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Tas.UKBR.Properties;
using SAM.Core.Tas.UKBR;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper.Tas.UKBR
{
    public class GooUKBRFile : GH_Goo<UKBRFile>
    {
        public override bool IsValid => Value != null;

        public GooUKBRFile(UKBRFile uKBRFile)
        {
            Value = uKBRFile;
        }
        
        public override string TypeName
        {
            get
            {
                return typeof(UKBRFile).FullName;
            }
        }

        public override string TypeDescription
        {
            get
            {
                return typeof(UKBRFile).FullName.Replace(".", " ");
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooUKBRFile(Value);
        }

        public override string ToString()
        {
            return typeof(UKBRFile).Name;
        }
    }

    public class GooUKBRFileParam : GH_PersistentParam<GooUKBRFile>
    {
        public override Guid ComponentGuid => new Guid("b8da455e-b8aa-4740-b770-c8c3581ff0d7");

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override System.Drawing.Bitmap Icon => Resources.SAM_T3D3;

        public GooUKBRFileParam()
            : base(typeof(UKBRFile).Name, typeof(UKBRFile).Name, typeof(UKBRFile).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooUKBRFile> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooUKBRFile value)
        {
            throw new NotImplementedException();
        }
    }
}
