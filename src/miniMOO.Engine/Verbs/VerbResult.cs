using miniMOO.Core.Things;

namespace miniMOO.Engine.Verbs;

public sealed record VerbResult(VerbResultKind Kind, MooValue? Value = null, string? Message = null) {
    public bool IsSuccess => Kind == VerbResultKind.Success;

    public static VerbResult Success(MooValue? value = null)
        => new(VerbResultKind.Success, value);

    public static VerbResult Failure(string message)
        => new(VerbResultKind.Failure, Message: message);
}
