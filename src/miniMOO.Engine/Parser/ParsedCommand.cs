

namespace miniMOO.Engine.Parser;

public sealed record ParsedCommand {
    public required string Raw { get; init; }

    public required string Verb { get; init; }

    public required string ArgumentText { get; init; }

    public required IReadOnlyList<string> Arguments { get; init; }

    public required string DirectObjectText { get; init; }

    public MatchResult? DirectObject { get; init; }

    public required string Preposition { get; init; }

    public required string IndirectObjectText { get; init; }

    public MatchResult? IndirectObject { get; init; }

    public bool HasDirectObject => DirectObjectText.Length > 0;

    public bool HasIndirectObject => IndirectObjectText.Length > 0;

    public bool HasPreposition => Preposition.Length > 0;
}
