namespace Tms.Adapter.XUnit
{
    public class StepFailedException : Exception
    {
        public StepFailedException(string stepName, Exception inner) : base($"Step failed: {stepName}", inner)
        {
        }
    }
}