namespace miniMOO.Script.Evaluation;

public sealed class MooTaskAbortException : Exception {
    public MooTaskAbortException()
        : base("Task aborted.") {
    }
}
