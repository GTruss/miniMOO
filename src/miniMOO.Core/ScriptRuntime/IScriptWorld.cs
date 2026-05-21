using miniMOO.Core.Things;

namespace miniMOO.Core.ScriptRuntime;

public interface IScriptWorld {
    MooObject? Get(ObjectId id);
    MooValue? GetProperty(ObjectId id, string name);
    IEnumerable<MooObject> ContentsOf(ObjectId id);

    Task NotifyAsync(ObjectId playerId, IReadOnlyList<MooValue> values);
    Task<ScriptResult> InvokeVerbAsync(ScriptContext callerContext, ObjectId thisId, 
        string verb, IReadOnlyList<MooValue> args, ObjectId? searchFromId = null);

    IReadOnlyList<ObjectId> GetChildren(ObjectId parentId);

    ObjectId CreateObject(ObjectId parentId, ObjectId ownerId);
    void MoveObject(ObjectId objId, ObjectId destId);
    void SetObjectName(ObjectId objId, string name);
    void SetProperty(ObjectId objId, string propName, MooValue value);
    void AddAlias(ObjectId objId, string alias);
}
