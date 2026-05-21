using System.Text;

namespace miniMOO.Script.Lexing;

public static class LexerDebug {
    public static string Dump(string source)
        => Dump(new MooLexer(source).Lex());

    public static string Dump(IEnumerable<Token> tokens) {
        var builder = new StringBuilder();

        foreach (var token in tokens) {
            builder
                .Append(token.Line)
                .Append(':')
                .Append(token.Column)
                .Append(' ')
                .Append(token.Kind)
                .Append(" text=\"")
                .Append(Escape(token.Text))
                .Append('"');

            if (token.Value is not null) {
                builder
                    .Append(" value=\"")
                    .Append(Escape(token.Value.ToString() ?? ""))
                    .Append('"');
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
