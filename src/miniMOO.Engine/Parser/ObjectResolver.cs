using miniMOO.Engine.Repositories;
using miniMOO.Engine.Things;

namespace miniMOO.Engine.Parser;

public sealed class ObjectResolver : IObjectResolver {
    private readonly IObjectRepository _objects;

    public ObjectResolver(IObjectRepository objects) {
        _objects = objects;
    }

    public MooObject? Get(ObjectId id)
        => _objects.Get(id);

    public MooObject? GetParent(MooObject obj)
        => obj.ParentId is { } parentId ? _objects.Get(parentId) : null;

    public IEnumerable<MooObject> SelfAndAncestors(ObjectId id) {
        for (var obj = _objects.Get(id); obj is not null; obj = GetParent(obj))
            yield return obj;
    }

    public MooVerb? FindVerb(ObjectId startId, string name)
        => SelfAndAncestors(startId)
            .SelectMany(obj => obj.Verbs)
            .FirstOrDefault(verb => verb.MatchesName(name));

    public MooProperty? FindProperty(ObjectId startId, string name) {
        foreach (var obj in SelfAndAncestors(startId)) {
            if (!obj.Properties.TryGetValue(name, out var prop))
                continue;

            if (prop.Value is MooValue.Clear)
                continue;

            return prop;
        }

        return null;
    }

    public MooValue? FindPropertyValue(ObjectId startId, string name)
        => FindProperty(startId, name)?.Value;

    public bool HasProperty(ObjectId startId, string name)
        => FindProperty(startId, name) is not null;
}