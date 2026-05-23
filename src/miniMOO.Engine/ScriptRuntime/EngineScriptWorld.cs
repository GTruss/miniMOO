using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Engine.Parser;
using miniMOO.Engine.Repositories;
using miniMOO.Engine.Services;
using miniMOO.Script.Evaluation;

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
                : new MooValue.Object(new ObjectId(-1)),
            "owner" => new MooValue.Object(obj.OwnerId),
            "parent" => obj.ParentId is { } parentId
                ? new MooValue.Object(parentId)
                : new MooValue.Object(new ObjectId(-1)),
            "contents" => new MooValue.List(
                _objects.ContentsOf(obj.Id)
                    .Select(child => (MooValue)new MooValue.Object(child.Id))
                    .ToList()),
            "aliases" => new MooValue.List(
                obj.Aliases.Select(a => (MooValue)new MooValue.String(a)).ToList()),
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
            World = this,
            Meter = callerContext.Meter,
        };

        var result = await _scriptRuntime.ExecuteAsync(context, mooVerb.Implementation);

        if (!result.IsSuccess) {
            var message = result.Error ?? "Script failed.";
            var frame = $"... (eng) {definingId}:{string.Join("/", mooVerb.Names)} called as {thisId}:{verb}";

            return ScriptResult.Failure(AppendTraceFrame(message, frame));
        }
        return result;
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

    public async Task MoveObjectAsync(ScriptContext context, ObjectId objId, ObjectId destId) {
        var obj = _objects.Get(objId)
            ?? throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {objId} not found.");

        if (destId.Value >= 0 && _objects.Get(destId) is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, $"Destination {destId} not found.");

        if (WouldContain(objId, destId))
            throw new MooScriptException(MooErrorCode.E_RECMOVE, "Recursive move.");

        var oldLocationId = obj.LocationId;

        obj.LocationId = destId.Value >= 0 ? destId : null;

        if (oldLocationId is { } oldLoc)
            await TryInvokeMovementHookAsync(context, oldLoc, "exitfunc", objId);

        if (destId.Value >= 0)
            await TryInvokeMovementHookAsync(context, destId, "enterfunc", objId);
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

    public void AddVerb(ObjectId objId, string verbNames, string script, ObjectId ownerId) {
        var obj = _objects.Get(objId)
            ?? throw new InvalidOperationException($"Object {objId} not found.");

        var verb = new MooVerb {
            OwnerId = ownerId,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = script
        };

        foreach (var name in verbNames.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0))
            verb.Names.Add(name);

        obj.Verbs.Add(verb);
    }

    public MooValue? GetVerbInfo(ObjectId id, string verbName) {
        var obj = _objects.Get(id);
        if (obj is null) return null;

        var verb = obj.Verbs.FirstOrDefault(v => v.MatchesName(verbName));
        if (verb is null) return null;

        return new MooValue.List([
            new MooValue.Object(verb.OwnerId),
        new MooValue.Integer((int)verb.Flags),
        new MooValue.String(string.Join(" ", verb.Names))
        ]);
    }

    private bool WouldContain(ObjectId objId, ObjectId destId) {
        var current = destId;

        while (current.Value >= 0) {
            if (current == objId)
                return true;

            var container = _objects.Get(current);
            if (container?.LocationId is not { } next)
                return false;

            current = next;
        }

        return false;
    }

    private async Task TryInvokeMovementHookAsync(
        ScriptContext context,
        ObjectId targetId,
        string verbName,
        ObjectId movedObjectId) {

        var result = await InvokeVerbAsync(
            context,
            targetId,
            verbName,
            [new MooValue.Object(movedObjectId)]);

        // For now, missing hooks are normal.
        if (!result.IsSuccess && result.Error?.Contains("Verb not found", StringComparison.OrdinalIgnoreCase) == true)
            return;

        if (!result.IsSuccess)
            throw new MooEvaluationException(result.Error ?? $"{verbName} failed.");
    }

    private static string AppendTraceFrame(string message, string frame) {
        const string end = "(End of traceback)";

        var marker = message.LastIndexOf(end, StringComparison.Ordinal);
        if (marker < 0)
            return frame + Environment.NewLine + message;

        return message.Insert(marker, frame + Environment.NewLine);
    }
}
