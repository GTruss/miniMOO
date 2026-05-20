using miniMOO.Core.Things;

namespace miniMOO.Engine.Repositories;

public interface IObjectRepository {
    MooObject? Get(ObjectId id);
    bool Exists(ObjectId id);
    IEnumerable<MooObject> All();
    IEnumerable<MooObject> ContentsOf(ObjectId locationId);
}
