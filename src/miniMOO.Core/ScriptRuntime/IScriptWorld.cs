using miniMOO.Core.Things;

namespace miniMOO.Core.ScriptRuntime;

public interface IScriptWorld {
    MooObject? Get(ObjectId id);
    MooValue? GetProperty(ObjectId id, string name);
    IEnumerable<MooObject> ContentsOf(ObjectId id);

    Task NotifyAsync(ObjectId playerId, IReadOnlyList<MooValue> values);
    Task<ScriptResult> InvokeVerbAsync(ScriptContext callerContext, ObjectId thisId, 
        string verb, IReadOnlyList<MooValue> args, ObjectId? searchFromId = null);
}
