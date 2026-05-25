using System.Collections.Concurrent;
using System.Threading;

namespace miniMOO.Script.Evaluation;

public static class ScriptTaskScheduler {
    private static int _nextTaskId = 1;
    private static readonly ConcurrentDictionary<int, CancellationTokenSource> Tasks = new();

    public static int AllocateTaskId()
        => Interlocked.Increment(ref _nextTaskId);

    public static int Schedule(TimeSpan delay, Func<int, CancellationToken, Task> action) {
        var taskId = AllocateTaskId();
        var cancellation = new CancellationTokenSource();

        Tasks[taskId] = cancellation;

        _ = Task.Run(async () => {
            try {
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellation.Token);

                cancellation.Token.ThrowIfCancellationRequested();
                await action(taskId, cancellation.Token);
            }
            catch (OperationCanceledException) {
            }
            catch (MooTaskAbortException) {
            }
            finally {
                Tasks.TryRemove(taskId, out _);
                cancellation.Dispose();
            }
        });

        return taskId;
    }

    public static bool Kill(int taskId) {
        if (!Tasks.TryRemove(taskId, out var cancellation))
            return false;

        cancellation.Cancel();
        cancellation.Dispose();
        return true;
    }
}
