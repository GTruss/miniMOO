using System.Collections.Concurrent;
using System.Threading;
using miniMOO.Core.Things;

namespace miniMOO.Script.Evaluation;

public static class ScriptTaskScheduler {
    private static int _nextTaskId = 1;
    private static readonly ConcurrentDictionary<int, ScheduledTask> Tasks = new();

    public static int AllocateTaskId()
        => Interlocked.Increment(ref _nextTaskId);

    public static int Schedule(TimeSpan delay, Func<int, CancellationToken, Task> action)
        => Schedule(delay, ScheduledTaskInfo.Unknown, action);

    public static int Schedule(TimeSpan delay, ScheduledTaskInfo info, Func<int, CancellationToken, Task> action) {
        var taskId = AllocateTaskId();
        var cancellation = new CancellationTokenSource();
        var scheduledFor = DateTimeOffset.UtcNow.Add(delay).ToUnixTimeSeconds();

        Tasks[taskId] = new ScheduledTask(cancellation, info with {
            Id = taskId,
            StartTime = scheduledFor
        });

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
                Tasks.TryRemove(taskId, out var task);
                task?.Cancellation.Dispose();
            }
        });

        return taskId;
    }

    public static bool Kill(int taskId) {
        if (!Tasks.TryRemove(taskId, out var task))
            return false;

        task.Cancellation.Cancel();
        task.Cancellation.Dispose();
        return true;
    }

    public static IReadOnlyList<ScheduledTaskInfo> QueuedTasks()
        => Tasks.Values
            .Select(task => task.Info)
            .OrderBy(task => task.StartTime)
            .ThenBy(task => task.Id)
            .ToList();

    private sealed record ScheduledTask(
        CancellationTokenSource Cancellation,
        ScheduledTaskInfo Info);
}

public readonly record struct ScheduledTaskInfo(
    int Id,
    long StartTime,
    ObjectId OwnerId,
    ObjectId VerbLocationId,
    string VerbName,
    int LineNumber,
    ObjectId ThisId) {

    public static ScheduledTaskInfo Unknown { get; } = new(
        0,
        0,
        ObjectId.Nothing,
        ObjectId.Nothing,
        "",
        1,
        ObjectId.Nothing);
}
