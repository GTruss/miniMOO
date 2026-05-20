using miniMOO.Engine.Things;

namespace miniMOO.Engine.Repositories;

public sealed class InMemoryObjectRepository : IObjectRepository {
    private readonly Dictionary<ObjectId, MooObject> _objects = [];

    public MooObject? Get(ObjectId id)
        => _objects.GetValueOrDefault(id);

    public bool Exists(ObjectId id)
        => _objects.ContainsKey(id);

    public IEnumerable<MooObject> All()
        => _objects.Values;

    public void Add(MooObject obj)
        => _objects.Add(obj.Id, obj);
 
    public IEnumerable<MooObject> ContentsOf(ObjectId locationId)
        => _objects.Values.Where(obj => obj.LocationId == locationId);
}
