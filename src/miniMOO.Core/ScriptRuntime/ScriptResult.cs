using miniMOO.Core.Things;

namespace miniMOO.Core.ScriptRuntime;

public sealed record ScriptTraceFrame(
    ObjectId? DefiningObjectId,
    ObjectId? ThisId,
    string? Verb,
    int? Line,
    string Description);

public sealed record ScriptError(
    string Message,
    int? ErrorCode = null,
    int? Line = null,
    int? Column = null,
    string? SourceText = null,
    string? SourceLabel = null,
    IReadOnlyList<ScriptTraceFrame>? Trace = null) {

    public IReadOnlyList<ScriptTraceFrame> Frames => Trace ?? [];

    public ScriptError WithFrame(ScriptTraceFrame frame)
        => this with { Trace = [.. Frames, frame] };

    public string ToDisplayString() {
        var lines = new List<string>();

        var displayMessage = SourceLabel is not null
            ? $"{SourceLabel}: {Message}"
            : Message;

        if (SourceText is not null && Line is not null && Column is not null)
            lines.AddRange(FormatSourceError(SourceText, Line.Value, Column.Value, displayMessage));
        else
            lines.Add(displayMessage);

        foreach (var frame in Frames)
            lines.Add(frame.Description);

        if (Frames.Count > 0)
            lines.Add("(End of traceback)");

        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> FormatSourceError(string source, int line, int column, string message) {

        var sourceLines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        if (line < 1 || line > sourceLines.Length) {
            yield return message;
            yield break;
        }

        var sourceLine = sourceLines[line - 1];
        var caretColumn = Math.Max(1, column);

        yield return message;
        yield return $"{line,4}: {sourceLine}";
        yield return $"      {new string(' ', caretColumn - 1)}^";
    }
}

public sealed record ScriptResult(
    VerbResultKind Kind,
    MooValue? Value = null,
    ScriptError? ErrorDetail = null) {

    public bool IsSuccess => Kind == VerbResultKind.Success;
    public string? Error => ErrorDetail?.ToDisplayString();

    public static ScriptResult Success(MooValue? value = null)
        => new(VerbResultKind.Success, value);

    public static ScriptResult Failure(string message)
        => new(VerbResultKind.Failure, ErrorDetail: new ScriptError(message));

    public static ScriptResult Failure(ScriptError error)
        => new(VerbResultKind.Failure, ErrorDetail: error);

    public ScriptResult WithFrame(ScriptTraceFrame frame)
        => IsSuccess
            ? this
            : this with {
                ErrorDetail = (ErrorDetail ?? new ScriptError("Script failed."))
                    .WithFrame(frame)
            };
} 