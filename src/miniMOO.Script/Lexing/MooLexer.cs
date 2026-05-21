using System.Globalization;
using System.Text;

namespace miniMOO.Script.Lexing;

public sealed class MooLexer {
    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;

    public MooLexer(string source) {
        _source = source;
    }

    public IReadOnlyList<Token> Lex() {
        var tokens = new List<Token>();

        while (!IsAtEnd()) {
            var token = NextToken();

            if (token is not null)
                tokens.Add(token);
        }

        tokens.Add(new Token(TokenKind.EndOfFile, "", null, _position, _line, _column));
        return tokens;
    }

    private Token? NextToken() {
        SkipWhitespace();

        if (IsAtEnd())
            return null;

        var start = _position;
        var line = _line;
        var column = _column;
        var ch = Advance();

        return ch switch {
            '(' => Make(TokenKind.LeftParen, start, line, column),
            ')' => Make(TokenKind.RightParen, start, line, column),
            '{' => Make(TokenKind.LeftBrace, start, line, column),
            '}' => Make(TokenKind.RightBrace, start, line, column),
            ',' => Make(TokenKind.Comma, start, line, column),
            ';' => Make(TokenKind.Semicolon, start, line, column),
            ':' => Make(TokenKind.Colon, start, line, column),
            '.' => Make(TokenKind.Dot, start, line, column),
            '@' => Make(TokenKind.At, start, line, column),

            '&' => Match('&')
                ? Make(TokenKind.AmpAmp, start, line, column)
                : throw new MooLexException(
                    $"Unexpected character '&' at line {line}, column {column}. Did you mean '&&'?"),

            '|' => Match('|')
                ? Make(TokenKind.PipePipe, start, line, column)
                : throw new MooLexException(
                    $"Unexpected character '|' at line {line}, column {column}. Did you mean '||'?"),

            '+' => Make(TokenKind.Plus, start, line, column),
            '-' => Make(TokenKind.Minus, start, line, column),
            '*' => Make(TokenKind.Star, start, line, column),
            '/' => Make(TokenKind.Slash, start, line, column),
            '%' => Make(TokenKind.Percent, start, line, column),

            '=' => Match('=')
                ? Make(TokenKind.EqualEqual, start, line, column)
                : Make(TokenKind.Equal, start, line, column),

            '!' => Match('=')
                ? Make(TokenKind.BangEqual, start, line, column)
                : Make(TokenKind.Bang, start, line, column),

            '<' => Match('=')
                ? Make(TokenKind.LessEqual, start, line, column)
                : Make(TokenKind.Less, start, line, column),

            '>' => Match('=')
                ? Make(TokenKind.GreaterEqual, start, line, column)
                : Make(TokenKind.Greater, start, line, column),

            '"' => ReadString(start, line, column),
            '#' => ReadObjectId(start, line, column),

            _ when IsIdentifierStart(ch) =>
                ReadIdentifier(start, line, column),

            _ when char.IsDigit(ch) =>
                ReadInteger(start, line, column),

            _ => throw new MooLexException(
                $"Unexpected character '{ch}' at line {line}, column {column}.")
        };
    }

    private bool IsAtEnd()
        => _position >= _source.Length;

    private char Current
        => IsAtEnd() ? '\0' : _source[_position];

    private char Advance() {
        var ch = _source[_position++];

        if (ch == '\n') {
            _line++;
            _column = 1;
        }
        else {
            _column++;
        }

        return ch;
    }

    private bool Match(char expected) {
        if (IsAtEnd() || Current != expected)
            return false;

        Advance();
        return true;
    }

    private void SkipWhitespace() {
        while (!IsAtEnd()) {
            if (Current is ' ' or '\t' or '\r' or '\n') {
                Advance();
                continue;
            }

            break;
        }
    }

    private Token ReadIdentifier(int start, int line, int column) {
        while (!IsAtEnd() && IsIdentifierPart(Current))
            Advance();

        var text = _source[start.._position];
        var kind = KeywordKind(text);

        return new Token(kind, text, null, start, line, column);
    }

    private Token ReadInteger(int start, int line, int column) {
        while (!IsAtEnd() && char.IsDigit(Current))
            Advance();

        var text = _source[start.._position];
        var value = long.Parse(text, CultureInfo.InvariantCulture);

        return new Token(TokenKind.Integer, text, value, start, line, column);
    }

    private Token ReadObjectId(int start, int line, int column) {
        if (Current == '-')
            Advance();

        if (IsAtEnd() || !char.IsDigit(Current))
            throw new MooLexException(
                $"Expected object id after # at line {line}, column {column}.");

        while (!IsAtEnd() && char.IsDigit(Current))
            Advance();

        var text = _source[start.._position];
        var value = long.Parse(text[1..], CultureInfo.InvariantCulture);

        return new Token(TokenKind.ObjectId, text, value, start, line, column);
    }

    private Token ReadString(int start, int line, int column) {
        var value = new StringBuilder();

        while (!IsAtEnd()) {
            var ch = Advance();

            if (ch == '"') {
                var text = _source[start.._position];
                return new Token(TokenKind.String, text, value.ToString(), start, line, column);
            }

            if (ch == '\\') {
                if (IsAtEnd())
                    break;

                var escaped = Advance();

                value.Append(escaped switch {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => escaped
                });

                continue;
            }

            value.Append(ch);
        }

        throw new MooLexException(
            $"Unterminated string at line {line}, column {column}.");
    }

    private Token Make(TokenKind kind, int start, int line, int column)
        => new(kind, _source[start.._position], null, start, line, column);

    private static bool IsIdentifierStart(char ch)
        => char.IsLetter(ch) || ch == '_';

    private static bool IsIdentifierPart(char ch)
        => char.IsLetterOrDigit(ch) || ch == '_';

    private static TokenKind KeywordKind(string text)
        => text.ToLowerInvariant() switch {
            "if" => TokenKind.If,
            "else" => TokenKind.Else,
            "elseif" => TokenKind.ElseIf,
            "endif" => TokenKind.EndIf,
            "return" => TokenKind.Return,
            "for" => TokenKind.For,
            "in" => TokenKind.In,
            "endfor" => TokenKind.EndFor,
            _ => TokenKind.Identifier
        };
}