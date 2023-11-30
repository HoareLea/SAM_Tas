using System.Collections.Generic;

namespace SAM.Analytical.Tas.GenOpt
{
    public class OutputFile : GenOptFile
    {
        private List<Objective> objectives = new List<Objective>();

        public OutputFile(IEnumerable<Objective> objectives)
        {
            if(objectives != null)
            {
                this.objectives.AddRange(objectives);
            }
        }

        public OutputFile(ObjectiveFunctionLocation objectiveFunctionLocation)
            : this(objectiveFunctionLocation?.Objectives)
        {

        }

        protected override string GetText()
        {
            if(objectives == null)
            {
                return null;
            }

            List<string> texts = new List<string>();
            foreach (Objective objective in objectives)
            {
                if(string.IsNullOrWhiteSpace(objective?.Name))
                {
                    continue;
                }

                texts.Add(objective.Name);
            }

            return string.Join("\n", texts);
        }
    }
}
