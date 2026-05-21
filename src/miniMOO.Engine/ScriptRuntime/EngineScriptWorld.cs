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
        string verb, IReadOnlyList<MooValue> args, ObjectId? searchFromId = null) {

        var searchId = searchFromId ?? thisId;
        var (mooVerb, definingId) = _resolver.FindVerbWithOwner(searchId, verb);

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
            DobjStr = callerContext.DobjStr,
            IobjStr = callerContext.IobjStr,
            DefiningObjectId = definingId,
            World = this
        };

        return await _scriptRuntime.ExecuteAsync(context, mooVerb.Implementation);
    }

    public IReadOnlyList<ObjectId> GetChildren(ObjectId parentId) 
        => _objects.All()
            .Where(o => o.ParentId == parentId)
            .Select(o => o.Id)
            .ToList();

    public ObjectId CreateObject(ObjectId parentId, ObjectId ownerId) {
        var newId = _objects.AllocateId();
        _objects.Add(new MooObject {
            Id = newId,
            OwnerId = ownerId,
            ParentId = parentId,
            LocationId = null,
            Name = "object",
            Flags = ObjectFlags.Readable
        });
        return newId;
    }

    public void MoveObject(ObjectId objId, ObjectId destId) {
        var obj = _objects.Get(objId)
            ?? throw new InvalidOperationException($"Object {objId} not found.");

        obj.LocationId = destId;
    }

    public void SetObjectName(ObjectId objId, string name) {
        var obj = _objects.Get(objId)
            ?? throw new InvalidOperationException($"Object {objId} not found.");

        obj.Name = name;
    }

    public void SetProperty(ObjectId objId, string propName, MooValue value) {
        var obj = _objects.Get(objId)
            ?? throw new InvalidOperationException($"Object {objId} not found.");

        obj.Properties[propName] = new MooProperty {
            Name = propName,
            OwnerId = obj.OwnerId,
            Value = value
        };
    }

    public void AddAlias(ObjectId objId, string alias) {
        var obj = _objects.Get(objId)
            ?? throw new InvalidOperationException($"Object {objId} not found.");
        if (!obj.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
            obj.Aliases.Add(alias);
    }
}
