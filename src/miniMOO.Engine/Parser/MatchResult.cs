using miniMOO.Core.Things;

namespace miniMOO.Engine.Parser;

public sealed record MatchResult(
    MatchResultKind Kind,
    ObjectId? ObjectId = null) {
    public static MatchResult None()
        => new(MatchResultKind.None);

    public static MatchResult Found(ObjectId id)
        => new(MatchResultKind.Found, id);

    public static MatchResult NotFound()
        => new(MatchResultKind.NotFound);

    public static MatchResult Ambiguous()
        => new(MatchResultKind.Ambiguous);
}
