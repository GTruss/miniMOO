using miniMOO.Script.Ast;
using miniMOO.Script.Lexing;

namespace miniMOO.Script.Parsing;

public sealed class MooParser {
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    public MooParser(IReadOnlyList<Token> tokens) {
        _tokens = tokens;
    }

    public ProgramNode ParseProgram() {
        var statements = new List<StatementNode>();

        while (!Check(TokenKind.EndOfFile))
            statements.Add(ParseStatement());

        Consume(TokenKind.EndOfFile, "Expected end of file.");
        return new ProgramNode(statements);
    }

    private StatementNode ParseStatement() {
        var expression = ParseExpression();
        Consume(TokenKind.Semicolon, "Expected ';' after expression.");

        return new ExpressionStatementNode(expression);
    }

    private ExpressionNode ParseExpression()
        => ParsePostfix();

    private ExpressionNode ParsePostfix() {
        var expression = ParsePrimary();

        while (true) {
            if (Match(TokenKind.Dot)) {
                var property = Consume(TokenKind.Identifier, "Expected property name after '.'.");
                expression = new PropertyAccessExpressionNode(expression, property.Text);
                continue;
            }

            if (Match(TokenKind.Colon)) {
                var verb = Consume(TokenKind.Identifier, "Expected verb name after ':'.");
                Consume(TokenKind.LeftParen, "Expected '(' after verb name.");

                var arguments = ParseArguments();

                Consume(TokenKind.RightParen, "Expected ')' after verb arguments.");
                expression = new VerbCallExpressionNode(expression, verb.Text, arguments);
                continue;
            }

            break;
        }

        return expression;
    }

    private ExpressionNode ParsePrimary() {
        if (Match(TokenKind.At)) {
            var expression = ParseExpression();
            return new SpliceExpressionNode(expression);
        }

        if (Match(TokenKind.Identifier)) {
            var identifier = Previous();

            if (Match(TokenKind.LeftParen)) {
                var arguments = ParseArguments();
                Consume(TokenKind.RightParen, "Expected ')' after function arguments.");

                return new FunctionCallExpressionNode(identifier.Text, arguments);
            }

            return new IdentifierExpressionNode(identifier.Text);
        }

        if (Match(TokenKind.String))
            return new StringLiteralExpressionNode((string)(Previous().Value ?? ""));

        if (Match(TokenKind.Integer))
            return new IntegerLiteralExpressionNode((long)(Previous().Value ?? 0L));

        if (Match(TokenKind.ObjectId))
            return new ObjectLiteralExpressionNode((long)(Previous().Value ?? 0L));

        if (Match(TokenKind.LeftParen)) {
            var expression = ParseExpression();
            Consume(TokenKind.RightParen, "Expected ')' after expression.");
            return expression;
        }

        throw Error(Current, "Expected expression.");
    }

    private IReadOnlyList<ExpressionNode> ParseArguments() {
        var arguments = new List<ExpressionNode>();

        if (Check(TokenKind.RightParen))
            return arguments;

        do {
            arguments.Add(ParseExpression());
        }
        while (Match(TokenKind.Comma));

        return arguments;
    }

    private bool Match(TokenKind kind) {
        if (!Check(kind))
            return false;

        Advance();
        return true;
    }

    private Token Consume(TokenKind kind, string message) {
        if (Check(kind))
            return Advance();

        throw Error(Current, message);
    }

    private bool Check(TokenKind kind)
        => Current.Kind == kind;

    private Token Advance() {
        if (!Check(TokenKind.EndOfFile))
            _position++;

        return Previous();
    }

    private Token Current
        => _tokens[Math.Min(_position, _tokens.Count - 1)];

    private Token Previous()
        => _tokens[Math.Max(_position - 1, 0)];

    private static MooParseException Error(Token token, string message)
        => new(token, message);
}