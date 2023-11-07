using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class OptionsData : ISAP
    {
        public bool SummariseBridges { get; set; } = false;
        public bool ExtraBridgeInfo { get; set; } = true;
        public bool WireframeImages { get; set; } = true;
        public bool PlanAssessorXml { get; set; } = false;
        public bool JPADesignerXml { get; set; } = false;
        public bool Reexport { get; set; } = false;


        public OptionsData()
        {
        }

        public List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add("START_OPTIONS_DATA");
            result.Add(string.Format("SUMMARISE_BRIDGES = {0}", SummariseBridges.ToString().ToUpper()));
            result.Add(string.Format("EXTRA_BRIDGE_INFO = {0}", ExtraBridgeInfo.ToString().ToUpper()));
            result.Add(string.Format("WIREFRAME_IMAGES = {0}", WireframeImages.ToString().ToUpper()));
            result.Add(string.Format("PLAN_ASSESSOR_XML = {0}", PlanAssessorXml.ToString().ToUpper()));
            result.Add(string.Format("JPA_DESIGNER_XML = {0}", JPADesignerXml.ToString().ToUpper()));
            result.Add(string.Format("REEXPORT = {0}", Reexport.ToString().ToUpper()));
            result.Add("END_OPTIONS_DATA");
            return result;
        }
    }
}
