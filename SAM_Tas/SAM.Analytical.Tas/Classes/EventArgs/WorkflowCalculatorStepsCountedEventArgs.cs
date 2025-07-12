namespace SAM.Analytical.Tas
{
    public class WorkflowCalculatorStepsCountedEventArgs
    {
        public int Count { get; }

        public WorkflowCalculatorStepsCountedEventArgs(int count)
        {
            Count = count;
        }
    }
}
