namespace SAM.Analytical.Tas
{
    public class WorkflowCalculatorUpdatingEventArgs
    {
        public string Description { get; }

        public WorkflowCalculatorUpdatingEventArgs(string description)
        {
            Description = description;
        }
    }
}
