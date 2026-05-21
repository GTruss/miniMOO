using miniMOO.Core.Things;

namespace miniMOO.Engine.Repositories;

public interface IObjectRepository {
    void Add(MooObject obj);
    ObjectId AllocateId();
    MooObject? Get(ObjectId id);
    bool Exists(ObjectId id);
    IEnumerable<MooObject> All();
    IEnumerable<MooObject> ContentsOf(ObjectId locationId);
}
