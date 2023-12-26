namespace NeeLaboratory.Threading.Jobs
{
    public enum SlimJobStates
    {
        Pending,
        Aborted,
        Completed,
        Executing,
    }

    public static class SlimJobStatesExtensions
    {
        public static bool IsFinished(this SlimJobStates state)
        {
            return state == SlimJobStates.Aborted || state == SlimJobStates.Completed;
        }
    }
}
