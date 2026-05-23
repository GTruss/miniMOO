using miniMOO.Script.Lexing;

namespace miniMOO.Script.Parsing;

public sealed class MooParseException : Exception {
    public Token Token { get; }

    public MooParseException(Token token, string message)
        : base($"{message} (at line {token.Line}, column {token.Column})") {
        Token = token;
    }
} 