using miniMOO.Engine.Things;

namespace miniMOO.Engine.Parser;

public interface IObjectResolver {
    MooObject? Get(ObjectId id);
    MooObject? GetParent(MooObject obj);
    IEnumerable<MooObject> SelfAndAncestors(ObjectId id);

    MooVerb? FindVerb(ObjectId startId, string name);
    MooProperty? FindProperty(ObjectId startId, string name);
    MooValue? FindPropertyValue(ObjectId startId, string name);
    bool HasProperty(ObjectId startId, string name);
}