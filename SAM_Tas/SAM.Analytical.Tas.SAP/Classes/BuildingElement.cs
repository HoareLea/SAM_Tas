using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class BuildingElement : ISAP
    {
        private Guid guid;
        private BuildingElementType buildingElementType;
        private bool zero;

        public BuildingElement(Guid guid, BuildingElementType buildingElementType, bool zero)
        {
            this.guid = guid;
            this.buildingElementType = buildingElementType;
            this.zero = zero;
        }

        public Guid Guid
        {
            get
            {
                return guid;
            }
        }

        public BuildingElementType BuildingElementType
        {
            get
            {
                return buildingElementType;
            }
        }

        public bool Zero
        {
            get
            {
                return zero;
            }
        }

        public List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add("BEGUID = {" + guid.ToString().ToUpper() + "}");
            result.Add(string.Format("BETYPE = {0}", buildingElementType.ToString()));
            result.Add(string.Format("ZERO = {0}", zero ? "True" : "False"));
            return result;
        }
    }
}
