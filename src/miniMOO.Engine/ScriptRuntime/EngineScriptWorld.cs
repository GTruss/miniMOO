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
    private readonly IScriptRuntime _scriptRuntime;

    public EngineScriptWorld(IObjectRepository objects, IObjectResolver resolver, OutputService output,
                             IScriptRuntime scriptRuntime) {

        _objects = objects;
        _resolver = resolver;
        _output = output;
        _scriptRuntime = scriptRuntime;
    }

    public MooObject? Get(ObjectId id)
        => _objects.Get(id);

    public MooValue? GetProperty(ObjectId id, string name) {
        var obj = _objects.Get(id);

        if (obj is null)
            return null;

        var builtin = GetBuiltinProperty(obj, name);

        if (builtin is not null)
            return builtin;

        return _resolver.FindPropertyValue(id, name);
    }

    private MooValue? GetBuiltinProperty(MooObject obj, string name)
        => name.ToLowerInvariant() switch {
            "name" => new MooValue.String(obj.Name),
            "location" => obj.LocationId is { } locationId
                ? new MooValue.Object(locationId)
                : MooValue.NothingValue,
            "owner" => new MooValue.Object(obj.OwnerId),
            "parent" => obj.ParentId is { } parentId
                ? new MooValue.Object(parentId)
                : MooValue.NothingValue,
            "contents" => new MooValue.List(
                _objects.ContentsOf(obj.Id)
                    .Select(child => (MooValue)new MooValue.Object(child.Id))
                    .ToList()),
            _ => null
        };

    public IEnumerable<MooObject> ContentsOf(ObjectId id)
        => _objects.ContentsOf(id);

    public Task NotifyAsync(ObjectId playerId, IReadOnlyList<MooValue> values) {
        _output.Notify(playerId, string.Concat(values.Select(value => value.ToString())));
        return Task.CompletedTask;
    }

    public async Task<ScriptResult> InvokeVerbAsync(ScriptContext callerContext, ObjectId thisId, 
                                              string verb, IReadOnlyList<MooValue> args) {
        var target = _objects.Get(thisId);

        if (target is null)
            return ScriptResult.Failure($"Invalid object: {thisId}");

        var mooVerb = _resolver.FindVerb(thisId, verb);

        if (mooVerb is null)
            return ScriptResult.Failure($"Verb not found: {thisId}:{verb}");

        if (mooVerb.ImplementationKind != VerbImplementationKind.Script)
            return ScriptResult.Failure($"Verb is not script-backed: {thisId}:{verb}");

        var context = new ScriptContext {
            PlayerId = callerContext.PlayerId,
            ThisId = thisId,
            Verb = verb,
            ArgStr = string.Concat(args.Select(arg => arg.ToString())),
            Args = args,
            DirectObjectId = callerContext.DirectObjectId,
            IndirectObjectId = callerContext.IndirectObjectId,
            World = this
        };

        return await _scriptRuntime.ExecuteAsync(context, mooVerb.Implementation);
    }
}
