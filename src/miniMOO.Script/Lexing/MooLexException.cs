namespace miniMOO.Script.Lexing;

public sealed class MooLexException : Exception {
    public int Line { get; }
    public int Column { get; }

    public MooLexException(string message, int line, int column)
        : base($"{message} at line {line}, column {column}.") {
        Line = line;
        Column = column;
    }

    public MooLexException(string message) : base(message) {
        Line = 0;
        Column = 0;
    }
} 