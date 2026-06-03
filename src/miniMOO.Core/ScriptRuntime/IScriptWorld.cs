using miniMOO.Core.Things;

namespace miniMOO.Core.ScriptRuntime;

public interface IScriptWorld {
    MooObject? Get(ObjectId id);
    MooValue? GetProperty(ObjectId id, string name);
    IEnumerable<MooObject> ContentsOf(ObjectId id);

    Task NotifyAsync(ObjectId playerId, IReadOnlyList<MooValue> values);
    Task<MooValue> ReadInputAsync(ObjectId playerId);
    Task EvalCommandAsync(ObjectId playerId, string command, IReadOnlyList<string>? inputLines = null);
    Task<MooValue> CheckpointAsync();
    Task<MooValue> ShutdownAsync(string message);
    IReadOnlyList<ObjectId> GetConnectedPlayers();
    Task BootPlayerAsync(ObjectId playerId);
    Task<ScriptResult> InvokeVerbAsync(ScriptContext callerContext, ObjectId thisId, 
        string verb, IReadOnlyList<MooValue> args, ObjectId? searchFromId = null);

    IReadOnlyList<ObjectId> GetChildren(ObjectId parentId);

    ObjectId CreateObject(ObjectId parentId, ObjectId ownerId);
    Task MoveObjectAsync(ScriptContext context, ObjectId objId, ObjectId destId);
    void SetObjectName(ObjectId objId, string name);
    void SetProperty(ObjectId objId, string propName, MooValue value);
    void AddProperty(ObjectId objId, string propName, MooValue value, ObjectId ownerId, PropertyFlags flags);
    void AddAlias(ObjectId objId, string alias);
    long AddVerb(ObjectId objId, string verbNames, string script, ObjectId ownerId);
    long AddVerb(ObjectId objId, ObjectId ownerId, VerbFlags flags, string verbNames,
        VerbObjectSpec directObject, string preposition, VerbObjectSpec indirectObject);
    MooValue GetVerbNames(ObjectId id);
    MooValue GetAllVerbNames(ObjectId id);
    MooValue? GetVerbInfo(ObjectId id, MooValue verbRef);
    MooValue? GetVerbArgs(ObjectId id, MooValue verbRef);
    MooValue? GetVerbCode(ObjectId id, MooValue verbRef);
    void SetVerbCode(ObjectId id, MooValue verbRef, IReadOnlyList<string> codeLines);
    MooValue GetPropertyNames(ObjectId id);
    MooValue GetAllPropertyNames(ObjectId id);
    MooValue? GetPropertyInfo(ObjectId id, string propName);
    bool IsClearProperty(ObjectId id, string propName);
}
