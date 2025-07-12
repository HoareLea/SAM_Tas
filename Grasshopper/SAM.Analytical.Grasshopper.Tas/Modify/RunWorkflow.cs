﻿using SAM.Analytical.Tas;
using SAM.Core.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public static partial class Modify
    {
        public static AnalyticalModel RunWorkflow(this AnalyticalModel analyticalModel, WorkflowSettings workflowSettings)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            if(workflowSettings == null)
            {
                workflowSettings = new WorkflowSettings();
            }

            AnalyticalModel result = analyticalModel;
            using (ProgressForm progressForm = new ProgressForm("Workflow"))
            {
                WorkflowCalculator workflowCalculator = new WorkflowCalculator(workflowSettings);
                workflowCalculator.StepsCounted += (s, e) =>
                {
                    progressForm.Max = e.Count;
                };

                workflowCalculator.Updating += (s, e) =>
                {
                    progressForm.Update(e.Description);
                };

                result = workflowCalculator.Calculate(analyticalModel);
            }

            return result;
        }
    }
}