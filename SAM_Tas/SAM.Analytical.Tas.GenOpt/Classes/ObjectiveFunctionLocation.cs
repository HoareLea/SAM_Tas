using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.GenOpt
{
    [Attributes.Name("ObjectiveFunctionLocation")]
    public class ObjectiveFunctionLocation : GenOptObject
    {
        private List<Objective> objectives = new List<Objective>();

        public ObjectiveFunctionLocation()
        {

        }

        public ObjectiveFunctionLocation(IEnumerable<string> names, IEnumerable<string>delimiters)
        {
            if(names != null && delimiters != null)
            {
                int count = System.Math.Min(names.Count(), delimiters.Count());
                for(int i = 0; i < count; i++)
                {
                    Add(names.ElementAt(i), delimiters.ElementAt(i));
                }
            }
        }

        public ObjectiveFunctionLocation(IEnumerable<string> names)
        {
            if(names != null)
            {
                foreach(string name in names)
                {
                    Add(name);
                }
            }
        }

        public bool Add(string name, string delimiter)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(delimiter))
            {
                return false;
            }

            return Add(new Objective(name, delimiter));
        }

        public bool Add(Objective objective)
        {
            if(objective == null || string.IsNullOrEmpty(objective.Name))
            {
                return false;
            }

            if (objectives == null)
            {
                objectives = new List<Objective>();
            }

            Objective objective_Existing = objectives?.Find(x => x.Name == objective.Name);
            if (objective_Existing != null)
            {
                objective_Existing.Delimiter = objective.Delimiter;
                return true;
            }

            objectives.Add(objective);
            return true;
        }

        public bool Add(string name)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return Add(name, string.Format("{0}::", name));
        }

        public List<Objective> Objectives
        {
            get
            {
                return objectives;
            }
        }

        protected override string GetText()
        {
            if(objectives == null)
            {
                return null;
            }

            List<string> texts = new List<string>();
            for (int i = 0; i < objectives.Count; i++)
            {
                string name = objectives[i].Name;
                if(string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string delimiter = objectives[i].Delimiter;
                if (string.IsNullOrWhiteSpace(delimiter))
                {
                    continue;
                }

                texts.Add(string.Format("Name{0} = {1};", i + 1, name));
                texts.Add(string.Format("Delimiter{0} = \"{1}\";", i + 1, delimiter));
            }

            return string.Join("\n", texts);
        }
    }
}
