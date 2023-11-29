using System.Collections.Generic;

namespace SAM.Analytical.Tas.GenOpt
{
    public class ParameterFile : GenOptFile
    {
        private List<IParameter> parameters = new List<IParameter>();

        public ParameterFile(IEnumerable<IParameter> parameters)
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
            if (parameters == null)
            {
                return null;
            }

            List<string> texts = new List<string>();
            foreach (IParameter parameter in parameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                if(parameter is NumberParameter)
                {
                    NumberParameter numberParameter = (NumberParameter)parameter;
                    
                    List<string> values = new List<string>() { numberParameter.Name, numberParameter.Initial.ToString(), numberParameter.Min.ToString(), numberParameter.Max.ToString(), numberParameter.Step.ToString(), typeof(double).FullName.ToString() };

                    texts.Add(string.Format("{0}", string.Join(",", values)));
                }


            }

            return string.Join("\n", texts);
        }
    }
}
