using miniMOO.Core.Things;

namespace miniMOO.Core.ScriptRuntime;

public sealed record ScriptResult(VerbResultKind Kind, MooValue? Value = null, string? Error = null) {
    public bool IsSuccess => Kind == VerbResultKind.Success;

    public static ScriptResult Success(MooValue? value = null)
        => new(VerbResultKind.Success, value);

    public static ScriptResult Failure(string message)
        => new(VerbResultKind.Failure, Error: message);

}
