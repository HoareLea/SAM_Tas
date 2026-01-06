// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
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
            using (ProgressForm progressForm = new("Workflow"))
            {
                WorkflowCalculator workflowCalculator = new(workflowSettings);
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