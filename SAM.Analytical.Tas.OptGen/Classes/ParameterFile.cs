using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{
    public class ParameterFile : OptGenObject, IOptGenFile
    {
        private List<Parameter> parameters = new List<Parameter>();

        public ParameterFile(IEnumerable<Parameter> parameters)
        {
            if(parameters != null)
            {
                foreach (Parameter parameter in parameters)
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
            foreach(Parameter parameter in parameters)
            {
                if(parameter == null)
                {
                    continue;
                }

                List<string> values = new List<string>() { parameter.Name, parameter.Min.ToString(), parameter.Initial.ToString(), parameter.Max.ToString(), parameter.Step.ToString(), typeof(double).Name.ToString() };

                texts.Add(string.Format("{0}", string.Join(",", values)));
            }

            return string.Join("\n", texts);
        }
    }
}
