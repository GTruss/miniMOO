using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Engine.Parser;
using miniMOO.Engine.Repositories;
using miniMOO.Engine.Services;

namespace miniMOO.Engine.ScriptRuntime;

public sealed class EngineScriptWorld : IScriptWorld {
    private readonly IObjectRepository _objects;
    private readonly IObjectResolver _resolver;
    private readonly OutputService _output;

    public EngineScriptWorld(
        IObjectRepository objects,
        IObjectResolver resolver,
        OutputService output) {

        _objects = objects;
        _resolver = resolver;
        _output = output;
    }

    public MooObject? Get(ObjectId id)
        => _objects.Get(id);

    public MooValue? GetProperty(ObjectId id, string name)
        => _resolver.FindPropertyValue(id, name);

    public IEnumerable<MooObject> ContentsOf(ObjectId id)
        => _objects.ContentsOf(id);

    public Task NotifyAsync(ObjectId playerId, IReadOnlyList<MooValue> values) {
        _output.Notify(playerId, string.Concat(values.Select(value => value.ToString())));
        return Task.CompletedTask;
    }

    public Task<ScriptResult> InvokeVerbAsync(
        ObjectId thisId,
        string verb,
        IReadOnlyList<MooValue> args) {

        return Task.FromResult(
            ScriptResult.Failure("Script verb invocation is not implemented yet."));
    }
}
