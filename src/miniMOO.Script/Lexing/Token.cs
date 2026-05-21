namespace miniMOO.Script.Lexing;

public sealed record Token(
    TokenKind Kind,
    string Text,
    object? Value,
    int Position,
    int Line,
    int Column
);