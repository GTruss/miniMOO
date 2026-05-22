using System.Diagnostics;

namespace miniMOO.Core.ScriptRuntime;

public sealed class ScriptExecutionMeter {
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public long MaxTicks { get; init; } = 100_000;
    public double MaxSeconds { get; init; } = 30.0;

    public long UsedTicks { get; private set; }

    public long TicksLeft => Math.Max(0, MaxTicks - UsedTicks);

    public double SecondsLeft
        => Math.Max(0, MaxSeconds - _stopwatch.Elapsed.TotalSeconds);

    public bool TryTick(out string? error) {
        UsedTicks++;

        if (UsedTicks > MaxTicks) {
            error = "Task exceeded tick limit.";
            return false;
        }

        if (_stopwatch.Elapsed.TotalSeconds > MaxSeconds) {
            error = "Task exceeded time limit.";
            return false;
        }

        error = null;
        return true;
    }
}
