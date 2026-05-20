using miniMOO.Engine.Services;
using miniMOO.Engine.Repositories;
using miniMOO.Engine.Things;
using miniMOO.Engine.Parser;

namespace miniMOO.Engine.Verbs;

public sealed class VerbContext {
    public required ObjectId PlayerId { get; init; }
    public required ObjectId ThisId { get; init; }

    public ObjectId? DirectObjectId { get; init; }
    public ObjectId? IndirectObjectId { get; init; }

    public required ParsedCommand Command { get; init; }

    public required IObjectRepository Objects { get; init; }
    public required PermissionService Permissions { get; init; }
    public required OutputService Output { get; init; }
}
