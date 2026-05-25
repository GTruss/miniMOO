using miniMOO.Core.Things;

namespace miniMOO.Core.ScriptRuntime;

public sealed class ScriptContext {
    public int TaskId { get; init; } = 1;
    public required ObjectId PlayerId { get; init; }
    public required ObjectId ThisId { get; init; }
    public ObjectId CallerId { get; init; } = new(-1);

    public required string Verb { get; init; }
    public required string ArgStr { get; init; }
    public required IReadOnlyList<MooValue> Args { get; init; }

    public ObjectId? DirectObjectId { get; init; }
    public string DobjStr { get; init; } = "";
    public string PrepStr { get; init; } = "";
    public ObjectId? IndirectObjectId { get; init; }
    public string IobjStr { get; init; } = "";
    public required IScriptWorld World { get; init; }
    public ObjectId? DefiningObjectId { get; init; }  // which ancestor defined the running verb
    public ScriptExecutionMeter Meter { get; init; } = new();
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}
