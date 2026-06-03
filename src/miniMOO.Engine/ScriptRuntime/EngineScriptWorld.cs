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
    private Func<ObjectId, string, Task>? _evalCommand;
    private Func<ObjectId, Task<string?>>? _readInput;
    private Func<Task<MooValue>>? _checkpoint;
    private Func<string, Task<MooValue>>? _shutdown;
    private Func<IReadOnlyList<ObjectId>>? _connectedPlayers;
    private Func<ObjectId, Task>? _bootPlayer;
    private readonly Dictionary<ObjectId, Stack<Queue<string>>> _scriptedInput = new();

    public EngineScriptWorld(IObjectRepository objects, IObjectResolver resolver, OutputService output,
                             IScriptRuntime scriptRuntime) {

        _objects = objects;
        _resolver = resolver;
        _output = output;
        _scriptRuntime = scriptRuntime;
    }

    public void SetCommandEvaluator(Func<ObjectId, string, Task> evalCommand)
        => _evalCommand = evalCommand;

    public void SetInputReader(Func<ObjectId, Task<string?>> readInput)
        => _readInput = readInput;

    public void SetCheckpoint(Func<Task<MooValue>> checkpoint)
        => _checkpoint = checkpoint;

    public void SetShutdown(Func<string, Task<MooValue>> shutdown)
        => _shutdown = shutdown;

    public void SetConnectedPlayers(Func<IReadOnlyList<ObjectId>> connectedPlayers)
        => _connectedPlayers = connectedPlayers;

    public void SetBootPlayer(Func<ObjectId, Task> bootPlayer)
        => _bootPlayer = bootPlayer;

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
            "r" => new MooValue.Integer(obj.HasFlag(ObjectFlags.Readable) ? 1 : 0),
            "w" => new MooValue.Integer(obj.HasFlag(ObjectFlags.Writable) ? 1 : 0),
            "f" => new MooValue.Integer(obj.HasFlag(ObjectFlags.Fertile) ? 1 : 0),
            "programmer" => new MooValue.Integer(obj.HasFlag(ObjectFlags.Programmer) ? 1 : 0),
            "wizard" => new MooValue.Integer(obj.HasFlag(ObjectFlags.Wizard) ? 1 : 0),
            _ => null
        };

    public IEnumerable<MooObject> ContentsOf(ObjectId id)
        => _objects.ContentsOf(id);

    public Task NotifyAsync(ObjectId playerId, IReadOnlyList<MooValue> values) {
        _output.Notify(playerId, string.Concat(values.Select(value => value.ToString())));
        return Task.CompletedTask;
    }

    public async Task<MooValue> ReadInputAsync(ObjectId playerId) {
        if (_scriptedInput.TryGetValue(playerId, out var inputStack)
            && inputStack.Count > 0
            && inputStack.Peek().Count > 0)
            return new MooValue.String(inputStack.Peek().Dequeue());

        if (_readInput is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, "read() is not available.");

        var line = await _readInput(playerId);

        if (line is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, "No input available.");

        return new MooValue.String(line);
    }

    public async Task EvalCommandAsync(ObjectId playerId, string command, IReadOnlyList<string>? inputLines = null) {
        if (_evalCommand is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, "eval_command() is not available.");

        if (inputLines is null) {
            await _evalCommand(playerId, command);
            return;
        }

        if (!_scriptedInput.TryGetValue(playerId, out var inputStack)) {
            inputStack = new Stack<Queue<string>>();
            _scriptedInput[playerId] = inputStack;
        }

        inputStack.Push(new Queue<string>(inputLines));

        try {
            await _evalCommand(playerId, command);
        }
        finally {
            inputStack.Pop();

            if (inputStack.Count == 0)
                _scriptedInput.Remove(playerId);
        }
    }

    public Task<MooValue> CheckpointAsync() {
        if (_checkpoint is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, "checkpoint() is not available.");

        return _checkpoint();
    }

    public async Task<MooValue> ShutdownAsync(string message) {
        if (_shutdown is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, "shutdown() is not available.");

        if (_checkpoint is not null)
            await _checkpoint();

        return await _shutdown(message);
    }

    public IReadOnlyList<ObjectId> GetConnectedPlayers()
        => _connectedPlayers?.Invoke() ?? [];

    public Task BootPlayerAsync(ObjectId playerId) {
        if (_bootPlayer is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, "boot_player() is not available.");

        return _bootPlayer(playerId);
    }

    public async Task<ScriptResult> InvokeVerbAsync(ScriptContext callerContext, ObjectId thisId,
        string verb, IReadOnlyList<MooValue> args, ObjectId? searchFromId = null) {

        var searchId = searchFromId ?? thisId;
        var (mooVerb, definingId) = _resolver.FindVerbWithOwner(searchId, verb);

        if (mooVerb is null)
            return ScriptResult.Failure(new ScriptError(
                $"Verb not found: {thisId}:{verb}",
                ErrorCode: MooErrorCode.E_VERBNF));

        if (mooVerb.ImplementationKind != VerbImplementationKind.Script)
            return ScriptResult.Failure($"Verb is not script-backed: {thisId}:{verb}");

        var context = new ScriptContext {
            PlayerId = callerContext.PlayerId,
            ThisId = thisId,
            CallerId = callerContext.ThisId,
            Verb = verb,
            Debug = mooVerb.Flags.HasFlag(VerbFlags.Debug),
            ArgStr = string.Concat(args.Select(arg => arg.ToString())),
            Args = args,
            DirectObjectId = callerContext.DirectObjectId,
            IndirectObjectId = callerContext.IndirectObjectId,
            DobjStr = callerContext.DobjStr,
            PrepStr = callerContext.PrepStr,
            IobjStr = callerContext.IobjStr,
            DefiningObjectId = definingId,
            World = this,
            Meter = callerContext.Meter,
        };

        var result = await _scriptRuntime.ExecuteAsync(context, mooVerb.Code);

        if (!result.IsSuccess) {
            if (!mooVerb.Flags.HasFlag(VerbFlags.Debug)
                && result.ErrorDetail?.ErrorCode is { } errorCode)
                return ScriptResult.Success(new MooValue.Error(errorCode));

            return result.WithFrame(new ScriptTraceFrame(
                definingId,
                thisId,
                verb,
                null,
                $"... (eng) {definingId}:{string.Join("/", mooVerb.Names)} (this == {thisId})"))
                .WithSuppressible(false);
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

        switch (propName.ToLowerInvariant()) {
            case "name":
                if (value is not MooValue.String name)
                    throw new InvalidOperationException("Object name must be a string.");

                obj.Name = name.Value;
                return;

            case "aliases":
                if (value is not MooValue.List aliases)
                    throw new InvalidOperationException("Object aliases must be a list.");

                obj.Aliases.Clear();

                foreach (var alias in aliases.Items) {
                    if (alias is not MooValue.String s)
                        throw new InvalidOperationException("Object aliases must be strings.");

                    if (!obj.Aliases.Contains(s.Value, StringComparer.OrdinalIgnoreCase))
                        obj.Aliases.Add(s.Value);
                }

                return;

            case "owner":
                if (value is not MooValue.Object owner)
                    throw new InvalidOperationException("Object owner must be an object.");

                obj.OwnerId = owner.Value;
                return;

            case "parent":
                if (value is MooValue.Object parent) {
                    if (parent.Value.IsNothing) {
                        obj.ParentId = null;
                        return;
                    }

                    if (_objects.Get(parent.Value) is null)
                        throw new InvalidOperationException($"Parent object {parent.Value} not found.");

                    obj.ParentId = parent.Value;
                    return;
                }

                throw new InvalidOperationException("Object parent must be an object.");

            case "r":
                SetObjectFlag(obj, ObjectFlags.Readable, ToTruthyInteger(value));
                return;

            case "w":
                SetObjectFlag(obj, ObjectFlags.Writable, ToTruthyInteger(value));
                return;

            case "f":
                SetObjectFlag(obj, ObjectFlags.Fertile, ToTruthyInteger(value));
                return;

            case "programmer":
                SetObjectFlag(obj, ObjectFlags.Programmer, ToTruthyInteger(value));
                return;

            case "wizard":
                SetObjectFlag(obj, ObjectFlags.Wizard, ToTruthyInteger(value));
                return;
        }

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

    public void AddProperty(ObjectId objId, string propName, MooValue value, ObjectId ownerId, PropertyFlags flags) {
        var obj = _objects.Get(objId)
            ?? throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {objId} not found.");

        if (string.IsNullOrWhiteSpace(propName))
            throw new MooScriptException(MooErrorCode.E_INVARG, "Property name cannot be empty.");

        if (_resolver.FindProperty(objId, propName) is not null)
            throw new MooScriptException(MooErrorCode.E_INVARG, $"Property already exists: {propName}");

        obj.Properties[propName] = new MooProperty {
            Name = propName,
            OwnerId = ownerId,
            Flags = flags,
            Value = value
        };
    }

    private static int ToTruthyInteger(MooValue value)
        => value switch {
            MooValue.Integer i => i.Value == 0 ? 0 : 1,
            MooValue.Float f => f.Value == 0 ? 0 : 1,
            _ => throw new InvalidOperationException("Flag values must be numeric.")
        };

    private static void SetObjectFlag(MooObject obj, ObjectFlags flag, int enabled) {
        if (enabled != 0)
            obj.Flags |= flag;
        else
            obj.Flags &= ~flag;
    }

    public long AddVerb(ObjectId objId, string verbNames, string script, ObjectId ownerId) {
        var obj = _objects.Get(objId)
            ?? throw new InvalidOperationException($"Object {objId} not found.");

        var verb = new MooVerb {
            OwnerId = ownerId,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Code = script
        };

        foreach (var name in verbNames.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0))
            verb.Names.Add(name);

        obj.Verbs.Add(verb);
        return obj.Verbs.Count;
    }

    public long AddVerb(ObjectId objId, ObjectId ownerId, VerbFlags flags, string verbNames,
        VerbObjectSpec directObject, string preposition, VerbObjectSpec indirectObject) {

        var obj = _objects.Get(objId)
            ?? throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {objId} not found.");

        var names = verbNames
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (names.Count == 0)
            throw new MooScriptException(MooErrorCode.E_INVARG, "Verb names cannot be empty.");

        var verb = new MooVerb {
            OwnerId = ownerId,
            Flags = flags,
            DirectObject = directObject,
            Preposition = preposition,
            IndirectObject = indirectObject,
            ImplementationKind = VerbImplementationKind.Script,
            Code = ""
        };

        verb.Names.AddRange(names);
        obj.Verbs.Add(verb);
        return obj.Verbs.Count;
    }

    public MooValue GetVerbNames(ObjectId id) {
        var obj = _objects.Get(id);
        if (obj is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {id} not found.");

        return new MooValue.List(
            obj.Verbs
                .Select(verb => (MooValue)new MooValue.String(string.Join(" ", verb.Names)))
                .ToList());
    }

    public MooValue GetAllVerbNames(ObjectId id) {
        if (_objects.Get(id) is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {id} not found.");

        return new MooValue.List(
            _resolver.SelfAndAncestors(id)
                .SelectMany(obj => obj.Verbs)
                .Select(verb => (MooValue)new MooValue.String(string.Join(" ", verb.Names)))
                .ToList());
    }

    public MooValue? GetVerbInfo(ObjectId id, MooValue verbRef) {
        var obj = _objects.Get(id);
        if (obj is null) return null;

        var verb = ResolveVerb(obj, verbRef);
        if (verb is null) return null;

        return new MooValue.List([
            new MooValue.Object(verb.OwnerId),
        new MooValue.String(VerbPermsToString(verb.Flags)),
        new MooValue.String(string.Join(" ", verb.Names))
        ]);
    }

    private static string VerbPermsToString(VerbFlags flags) {
        var perms = "";

        if (flags.HasFlag(VerbFlags.Readable))
            perms += "r";

        if (flags.HasFlag(VerbFlags.Writable))
            perms += "w";

        if (flags.HasFlag(VerbFlags.Executable))
            perms += "x";

        if (flags.HasFlag(VerbFlags.Debug))
            perms += "d";

        return perms;
    }

    public MooValue? GetVerbArgs(ObjectId id, MooValue verbRef) {
        var obj = _objects.Get(id);
        if (obj is null) return null;

        var verb = ResolveVerb(obj, verbRef);
        if (verb is null) return null;

        return new MooValue.List([
            new MooValue.String(VerbObjectSpecToString(verb.DirectObject)),
        new MooValue.String(verb.Preposition),
        new MooValue.String(VerbObjectSpecToString(verb.IndirectObject))
        ]);
    }

    private static string VerbObjectSpecToString(VerbObjectSpec spec)
        => spec switch {
            VerbObjectSpec.This => "this",
            VerbObjectSpec.Any => "any",
            VerbObjectSpec.None => "none",
            _ => "any"
        };

    public MooValue? GetVerbCode(ObjectId id, MooValue verbRef) {
        var obj = _objects.Get(id);
        if (obj is null) return null;

        var verb = ResolveVerb(obj, verbRef);
        if (verb is null) return null;

        var lines = verb.Code
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => (MooValue)new MooValue.String(line))
            .ToList();

        return new MooValue.List(lines);
    }

    public void SetVerbCode(ObjectId id, MooValue verbRef, IReadOnlyList<string> codeLines) {
        var obj = _objects.Get(id);
        if (obj is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {id} not found.");

        var verb = ResolveVerb(obj, verbRef);
        if (verb is null)
            throw new MooScriptException(MooErrorCode.E_VERBNF, $"Verb not found: {verbRef}");

        if (verb.ImplementationKind != VerbImplementationKind.Script)
            throw new MooScriptException(MooErrorCode.E_INVARG, "Cannot set code on a non-script verb.");

        verb.Code = string.Join("\n", codeLines);
    }

    private static MooVerb? ResolveVerb(MooObject obj, MooValue verbRef)
        => verbRef switch {
            MooValue.String name => obj.Verbs.FirstOrDefault(v => v.MatchesName(name.Value)),
            MooValue.Integer index when index.Value >= 1 && index.Value <= obj.Verbs.Count =>
                obj.Verbs[(int)index.Value - 1],
            _ => null
        };

    public MooValue GetPropertyNames(ObjectId id) {
        var obj = _objects.Get(id)
            ?? throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {id} not found.");

        return new MooValue.List(
            obj.Properties.Keys
                .Select(name => (MooValue)new MooValue.String(name))
                .ToList());
    }

    public MooValue GetAllPropertyNames(ObjectId id) {
        if (_objects.Get(id) is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {id} not found.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new List<MooValue>();

        foreach (var obj in _resolver.SelfAndAncestors(id)) {
            foreach (var name in obj.Properties.Keys) {
                if (seen.Add(name))
                    names.Add(new MooValue.String(name));
            }
        }

        return new MooValue.List(names);
    }

    public MooValue? GetPropertyInfo(ObjectId id, string propName) {
        var property = _resolver.FindProperty(id, propName);
        if (property is null)
            return null;

        return new MooValue.List([
            new MooValue.Object(property.OwnerId),
            new MooValue.String(PropertyPermissions(property.Flags))
        ]);
    }

    public bool IsClearProperty(ObjectId id, string propName) {
        var obj = _objects.Get(id)
            ?? throw new MooScriptException(MooErrorCode.E_INVARG, $"Object {id} not found.");

        return obj.Properties.TryGetValue(propName, out var property)
            && property.Value is MooValue.Clear;
    }

    private static string PropertyPermissions(PropertyFlags flags) {
        var permissions = "";

        if (flags.HasFlag(PropertyFlags.Readable))
            permissions += "r";

        if (flags.HasFlag(PropertyFlags.Writable))
            permissions += "w";

        if (flags.HasFlag(PropertyFlags.Chown))
            permissions += "c";

        return permissions;
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
}
