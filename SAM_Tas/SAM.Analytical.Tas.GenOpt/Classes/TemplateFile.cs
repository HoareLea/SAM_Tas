using System.Collections.Generic;

namespace SAM.Analytical.Tas.GenOpt
{
    public class TemplateFile : GenOptFile
    {
        private List<IParameter> parameters = new List<IParameter>();

        public TemplateFile(IEnumerable<IParameter> parameters)
        {
            if(parameters != null)
            {
                foreach (IParameter parameter in parameters)
                {
                    if(parameter == null)
                    {
                        continue;
                    }

                    this.parameters.Add(parameter);
                }
            }
        }

        protected override string GetText()
        {
            if(parameters == null)
            {
                return null;
            }

            List<string> texts = new List<string>();
            foreach(IParameter parameter in parameters)
            {
                if(parameter == null)
                {
                    continue;
                }
                if (parameter is NumberParameter)
                {
                    NumberParameter numberParameter = (NumberParameter)parameter;

                    List<string> values = new List<string>() { numberParameter.Name, string.Format("%{0}%", numberParameter.Name), numberParameter.Min.ToString(), numberParameter.Max.ToString(), numberParameter.Step.ToString(), typeof(double).FullName.ToString() };
                    texts.Add(string.Format("{0}", string.Join(",", values)));
                }


            }

            return string.Join("\n", texts);
        }
    }
}
