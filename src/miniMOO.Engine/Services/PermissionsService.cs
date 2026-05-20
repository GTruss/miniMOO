using miniMOO.Engine.Repositories;
using miniMOO.Core.Things;

namespace miniMOO.Engine.Services;

public sealed class PermissionService {
    private readonly IObjectRepository _objects;

    public PermissionService(IObjectRepository objects) {
        _objects = objects;
    }

    public bool IsWizard(ObjectId actorId) {
        var actor = _objects.Get(actorId);
        return actor is not null && actor.HasFlag(ObjectFlags.Wizard);
    }

    public bool IsProgrammer(ObjectId actorId) {
        var actor = _objects.Get(actorId);
        return actor is not null && actor.HasFlag(ObjectFlags.Programmer);
    }

    public bool IsUser(ObjectId actorId) {
        var actor = _objects.Get(actorId);
        return actor is not null && actor.HasFlag(ObjectFlags.User);
    }

    public bool Controls(ObjectId actorId, ObjectId objectId) {
        var obj = _objects.Get(objectId);

        return obj is not null
            && (obj.OwnerId == actorId || IsWizard(actorId));
    }

    public bool ObjectAllows(ObjectId actorId, ObjectId objectId, ObjectFlags flag) {
        var obj = _objects.Get(objectId);

        return obj is not null
            && (obj.OwnerId == actorId
                || IsWizard(actorId)
                || obj.HasFlag(flag));
    }

    public bool CanReadObject(ObjectId actorId, ObjectId objectId)
        => ObjectAllows(actorId, objectId, ObjectFlags.Readable);

    public bool CanWriteObject(ObjectId actorId, ObjectId objectId)
        => ObjectAllows(actorId, objectId, ObjectFlags.Writable);

    public bool CanCreateChildOf(ObjectId actorId, ObjectId parentId)
        => ObjectAllows(actorId, parentId, ObjectFlags.Fertile);

    private bool HasActorFlag(ObjectId actorId, ObjectFlags flag) {
        var actor = _objects.Get(actorId);
        return actor is not null && actor.HasFlag(flag);
    }
}
