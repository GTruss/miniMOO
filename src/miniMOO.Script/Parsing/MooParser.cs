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

        while (!Check(TokenKind.EndOfFile)) {
            if (Match(TokenKind.Semicolon))
                continue;

            statements.Add(ParseStatement());
        }

        Consume(TokenKind.EndOfFile, "Expected end of file.");
        return new ProgramNode(statements);
    }

    // ── Statements ────────────────────────────────────────────────

    private StatementNode ParseStatement() {
        if (Check(TokenKind.If)) return ParseIfStatement();
        if (Check(TokenKind.For)) return ParseForStatement();
        if (Check(TokenKind.Return)) return ParseReturnStatement();
        if (Check(TokenKind.While)) return ParseWhileStatement();
        if (Check(TokenKind.Try)) return ParseTryStatement();

        var expression = ParseExpression();
        Consume(TokenKind.Semicolon, "Expected ';' after expression.");
        return new ExpressionStatementNode(expression);
    }

    private IfStatementNode ParseIfStatement() {
        Consume(TokenKind.If, "Expected 'if'.");
        Consume(TokenKind.LeftParen, "Expected '(' after 'if'.");
        var condition = ParseExpression();
        Consume(TokenKind.RightParen, "Expected ')' after condition.");

        var branches = new List<IfBranchNode> {
            new(condition, ParseBlock())
        };

        while (Check(TokenKind.ElseIf)) {
            Advance();
            Consume(TokenKind.LeftParen, "Expected '(' after 'elseif'.");
            var elseifCondition = ParseExpression();
            Consume(TokenKind.RightParen, "Expected ')' after condition.");
            branches.Add(new(elseifCondition, ParseBlock()));
        }

        IReadOnlyList<StatementNode>? elseBranch = null;
        if (Match(TokenKind.Else))
            elseBranch = ParseBlock();

        Consume(TokenKind.EndIf, "Expected 'endif'.");
        return new IfStatementNode(branches, elseBranch);
    }

    private StatementNode ParseForStatement() {
        Consume(TokenKind.For, "Expected 'for'.");
        var variable = Consume(TokenKind.Identifier, "Expected variable name after 'for'.");
        Consume(TokenKind.In, "Expected 'in' after variable.");

        if (Match(TokenKind.LeftBracket)) {
            // Numeric range: for i in [from..to]
            var from = ParseExpression();
            Consume(TokenKind.DotDot, "Expected '..' in range.");
            var to = ParseExpression();
            Consume(TokenKind.RightBracket, "Expected ']' after range.");
            var body = ParseBlock();
            Consume(TokenKind.EndFor, "Expected 'endfor'.");
            return new RangeForStatementNode(variable.Text, from, to, body);
        }
        else {
            // Existing list form: for x in (list)
            Consume(TokenKind.LeftParen, "Expected '(' after 'in'.");
            var iterable = ParseExpression();
            Consume(TokenKind.RightParen, "Expected ')' after iterable.");
            var body = ParseBlock();
            Consume(TokenKind.EndFor, "Expected 'endfor'.");
            return new ForStatementNode(variable.Text, iterable, body);
        }
    }

    private WhileStatementNode ParseWhileStatement() {
        Consume(TokenKind.While, "Expected 'while'.");
        Consume(TokenKind.LeftParen, "Expected '(' after 'while'.");
        var condition = ParseExpression();
        Consume(TokenKind.RightParen, "Expected ')' after condition.");
        var body = ParseBlock();
        Consume(TokenKind.EndWhile, "Expected 'endwhile'.");
        return new WhileStatementNode(condition, body);
    }

    private StatementNode ParseTryStatement() {
        Consume(TokenKind.Try, "Expected 'try'.");
        var body = ParseBlock();

        var clauses = new List<ExceptClauseNode>();
        while (Check(TokenKind.Except)) {
            Consume(TokenKind.Except, "Expected 'except'.");

            // Optional variable binding: except e (...)
            string? variable = null;
            if (Check(TokenKind.Identifier)) {
                var saved = _position;
                var name = Advance().Text;
                if (Check(TokenKind.LeftParen))
                    variable = name;
                else
                    _position = saved;
            }

            Consume(TokenKind.LeftParen, "Expected '(' after 'except'.");
            var codes = new List<ExpressionNode>();
            if (!Check(TokenKind.RightParen)) {
                do { codes.Add(ParseExpression()); }
                while (Match(TokenKind.Comma));
            }
            Consume(TokenKind.RightParen, "Expected ')' after error codes.");

            clauses.Add(new ExceptClauseNode(variable, codes, ParseBlock()));
        }

        Consume(TokenKind.EndTry, "Expected 'endtry'.");
        return new TryStatementNode(body, clauses);
    }

    private ReturnStatementNode ParseReturnStatement() {
        Consume(TokenKind.Return, "Expected 'return'.");

        if (Match(TokenKind.Semicolon))
            return new ReturnStatementNode(null);

        var value = ParseExpression();
        Consume(TokenKind.Semicolon, "Expected ';' after return value.");
        return new ReturnStatementNode(value);
    }

    private IReadOnlyList<StatementNode> ParseBlock() {
        var statements = new List<StatementNode>();

        while (!IsBlockTerminator()) {
            if (Match(TokenKind.Semicolon))
                continue;

            statements.Add(ParseStatement());
        }

        return statements;
    }

    private bool IsBlockTerminator()
        => Check(TokenKind.EndIf)
        || Check(TokenKind.Else)
        || Check(TokenKind.ElseIf)
        || Check(TokenKind.EndFor)
        || Check(TokenKind.EndWhile)
        || Check(TokenKind.Except)
        || Check(TokenKind.EndTry)
        || Check(TokenKind.EndOfFile);

    // ── Expressions (precedence, lowest → highest) ─────────────────

    private ExpressionNode ParseExpression()
        => ParseAssignment();

    private ExpressionNode ParseAssignment() {
        if (Check(TokenKind.Identifier)) {
            var saved = _position;
            var name = Advance().Text;
            if (Match(TokenKind.Equal))
                return new AssignmentExpressionNode(name, ParseAssignment());
            _position = saved;
        }

        if (LooksLikeDestructuringAssignment()) {
            var saved = _position;
            if (TryParseDestructuringAssignment(out var destructuring))
                return destructuring;
            _position = saved;
        }

        var expr = ParseConditional();

        // Indexed assignment: list[i] = value, obj.prop[i] = value
        if (expr is IndexExpressionNode indexAccess && Match(TokenKind.Equal))
            return new IndexedAssignmentExpressionNode(
                indexAccess.Target,
                indexAccess.Index,
                ParseAssignment());

        if (expr is SliceExpressionNode sliceAccess && Match(TokenKind.Equal))
            return new SliceAssignmentExpressionNode(
                sliceAccess.Target,
                sliceAccess.From,
                sliceAccess.To,
                ParseAssignment());

        // Property assignment: obj.prop = value
        if (expr is PropertyAccessExpressionNode propAccess && Match(TokenKind.Equal))
            return new PropertyAssignmentExpressionNode(
                propAccess.Target, propAccess.PropertyName, ParseAssignment());

        if (expr is DynamicPropertyAccessExpressionNode dynPropAccess && Match(TokenKind.Equal))
            return new DynamicPropertyAssignmentExpressionNode(
                dynPropAccess.Target,
                dynPropAccess.PropertyName,
                ParseAssignment());

        return expr;
    }

    private bool LooksLikeDestructuringAssignment() {
        if (!Check(TokenKind.LeftBrace))
            return false;

        var depth = 0;

        for (var i = _position; i < _tokens.Count; i++) {
            var token = _tokens[i];

            if (token.Kind == TokenKind.LeftBrace) {
                depth++;
                continue;
            }

            if (token.Kind == TokenKind.RightBrace) {
                depth--;

                if (depth == 0) {
                    var next = i + 1;
                    return next < _tokens.Count
                        && _tokens[next].Kind == TokenKind.Equal;
                }
            }
        }

        return false;
    }

    private bool TryParseDestructuringAssignment(out DestructuringAssignmentNode destructuring) {
        destructuring = null!;

        var slots = ParseDestructuringSlots();
        if (!Match(TokenKind.Equal))
            return false;

        destructuring = new DestructuringAssignmentNode(slots, ParseAssignment());
        return true;
    }

    private IReadOnlyList<DestructuringSlotNode> ParseDestructuringSlots() {
        var slots = new List<DestructuringSlotNode>();

        Consume(TokenKind.LeftBrace, "Expected '{' to begin destructuring assignment.");

        if (!Check(TokenKind.RightBrace)) {
            do {
                var isRest = Match(TokenKind.At);

                if (isRest) {
                    var restName = Consume(TokenKind.Identifier, "Expected variable name after '@' in destructuring assignment.");
                    slots.Add(new DestructuringSlotNode(restName.Text, IsOptional: false, IsRest: true, DefaultValue: null));

                    if (!Check(TokenKind.RightBrace))
                        throw new MooParseException(Current, "Rest destructuring slot must be last.");

                    break;
                }

                var isOptional = Match(TokenKind.Question);
                var name = Consume(TokenKind.Identifier, "Expected variable name in destructuring assignment.");
                ExpressionNode? defaultValue = null;

                if (Match(TokenKind.Equal))
                    defaultValue = ParseExpression();

                if (!isOptional && defaultValue is not null)
                    throw new MooParseException(name, "Only optional destructuring slots can have defaults.");

                slots.Add(new DestructuringSlotNode(name.Text, isOptional, IsRest: false, defaultValue));
            } while (Match(TokenKind.Comma));
        }

        Consume(TokenKind.RightBrace, "Expected '}' after destructuring assignment.");
        return slots;
    }

    private ExpressionNode ParseConditional() {
        var condition = ParseOr();

        if (!Match(TokenKind.Question))
            return condition;

        var trueExpr = ParseExpression();
        Consume(TokenKind.Pipe, "Expected '|' in conditional expression.");
        var falseExpr = ParseConditional();

        return new ConditionalExpressionNode(condition, trueExpr, falseExpr);
    }

    private ExpressionNode ParseOr() {
        var left = ParseAnd();

        while (Match(TokenKind.PipePipe))
            left = new BinaryExpressionNode(left, BinaryOp.Or, ParseAnd());

        return left;
    }

    private ExpressionNode ParseAnd() {
        var left = ParseEquality();

        while (Match(TokenKind.AmpAmp))
            left = new BinaryExpressionNode(left, BinaryOp.And, ParseEquality());

        return left;
    }

    private ExpressionNode ParseEquality() {
        var left = ParseComparison();

        while (true) {
            if (Match(TokenKind.EqualEqual))
                left = new BinaryExpressionNode(left, BinaryOp.Equal, ParseComparison());
            else if (Match(TokenKind.BangEqual))
                left = new BinaryExpressionNode(left, BinaryOp.NotEqual, ParseComparison());
            else
                break;
        }

        return left;
    }

    private ExpressionNode ParseComparison() {
        var left = ParseAdditive();

        while (true) {
            if (Match(TokenKind.Less))
                left = new BinaryExpressionNode(left, BinaryOp.Less, ParseAdditive());
            else if (Match(TokenKind.LessEqual))
                left = new BinaryExpressionNode(left, BinaryOp.LessEqual, ParseAdditive());
            else if (Match(TokenKind.Greater))
                left = new BinaryExpressionNode(left, BinaryOp.Greater, ParseAdditive());
            else if (Match(TokenKind.GreaterEqual))
                left = new BinaryExpressionNode(left, BinaryOp.GreaterEqual, ParseAdditive());
            else if (Match(TokenKind.In)) {
                var right = ParseAdditive();
                left = new BinaryExpressionNode(left, BinaryOp.In, right);
            }
            break;
        }

        return left;
    }

    private ExpressionNode ParseAdditive() {
        var left = ParseMultiplicative();

        while (true) {
            if (Match(TokenKind.Plus))
                left = new BinaryExpressionNode(left, BinaryOp.Add, ParseMultiplicative());
            else if (Match(TokenKind.Minus))
                left = new BinaryExpressionNode(left, BinaryOp.Subtract, ParseMultiplicative());
            else
                break;
        }

        return left;
    }

    private ExpressionNode ParseMultiplicative() {
        var left = ParseUnary();

        while (true) {
            if (Match(TokenKind.Star))
                left = new BinaryExpressionNode(left, BinaryOp.Multiply, ParseUnary());
            else if (Match(TokenKind.Slash))
                left = new BinaryExpressionNode(left, BinaryOp.Divide, ParseUnary());
            else if (Match(TokenKind.Percent))
                left = new BinaryExpressionNode(left, BinaryOp.Modulo, ParseUnary());
            else
                break;
        }

        return left;
    }

    private ExpressionNode ParseUnary() {
        if (Match(TokenKind.Bang))
            return new UnaryExpressionNode(UnaryOp.Not, ParseUnary());

        if (Match(TokenKind.Minus))
            return new UnaryExpressionNode(UnaryOp.Negate, ParseUnary());

        return ParsePostfix();
    }

    // ── Postfix & Primary (unchanged logic) ───────────────────────

    private ExpressionNode ParsePostfix() {
        var expression = ParsePrimary();

        while (true) {
            if (Match(TokenKind.Dot)) {
                if (Match(TokenKind.LeftParen)) {
                    var line = Previous().Line;
                    var column = Previous().Column;
                    var propertyExpr = ParseExpression();
                    Consume(TokenKind.RightParen, "Expected ')' after dynamic property name.");

                    expression = new DynamicPropertyAccessExpressionNode(
                        expression,
                        propertyExpr,
                        line,
                        column);

                    continue;
                }

                var property = Consume(TokenKind.Identifier, "Expected property name after '.'.");
                expression = new PropertyAccessExpressionNode(
                    expression,
                    property.Text,
                    property.Line,
                    property.Column);
                continue;
            }

            if (Match(TokenKind.Colon)) {
                var verbName = ParseVerbCallName();
                Consume(TokenKind.LeftParen, "Expected '(' after verb name.");
                var args = ParseArguments();
                Consume(TokenKind.RightParen, "Expected ')' after verb arguments.");
                expression = new VerbCallExpressionNode(expression, verbName, args);
                continue;
            }

            if (Match(TokenKind.LeftBracket)) {
                var from = ParseExpression();

                if (Match(TokenKind.DotDot)) {
                    var to = ParseExpression();
                    Consume(TokenKind.RightBracket, "Expected ']' after slice.");
                    expression = new SliceExpressionNode(expression, from, to);
                    continue;
                }

                Consume(TokenKind.RightBracket, "Expected ']' after index.");
                expression = new IndexExpressionNode(expression, from);
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
    
        if (Match(TokenKind.Float))
            return new FloatLiteralExpressionNode((double)(Previous().Value ?? 0.0));

        if (Match(TokenKind.ObjectId))
            return new ObjectLiteralExpressionNode((long)(Previous().Value ?? 0L));

        if (Match(TokenKind.LeftParen)) {
            var expression = ParseExpression();
            Consume(TokenKind.RightParen, "Expected ')' after expression.");
            return expression;
        }

        if (Match(TokenKind.LeftBrace)) {
            var items = new List<ExpressionNode>();
            if (!Check(TokenKind.RightBrace)) {
                do {
                    if (Match(TokenKind.At))
                        items.Add(new SpliceExpressionNode(ParseExpression()));
                    else
                        items.Add(ParseExpression());
                } while (Match(TokenKind.Comma));
            }
            Consume(TokenKind.RightBrace, "Expected '}' after list.");
            return new ListLiteralExpressionNode(items);
        }

        if (Match(TokenKind.Dollar))
            return new LastIndexExpressionNode();

        if (Match(TokenKind.DollarIdentifier)) {
            // $wiz desugars to #0.wiz at parse time — faithful to LambdaMOO
            var token = Previous();
            var name = (string)(token.Value ?? "");

            return new PropertyAccessExpressionNode(
                new ObjectLiteralExpressionNode(0L),
                name,
                token.Line,
                token.Column);
        }

        if (Match(TokenKind.Backtick)) {
            var expr = ParseExpression();
            Consume(TokenKind.Bang, "Expected '!' after backtick expression.");

            var codes = new List<ExpressionNode>();
            do { 
                codes.Add(ParseExpression()); 
            }
            while (Match(TokenKind.Comma));

            ExpressionNode? defaultVal = null;
            if (Match(TokenKind.FatArrow))
                defaultVal = ParseExpression();

            Consume(TokenKind.Apostrophe, "Expected closing \"'\" after backtick expression.");

            return new BacktickExpressionNode(expr, codes, defaultVal);
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

    private string ParseVerbCallName() {
        if (Match(TokenKind.At)) {
            var name = Consume(TokenKind.Identifier, "Expected verb name after '@'.");
            return "@" + name.Text;
        }

        var verb = Consume(TokenKind.Identifier, "Expected verb name after ':'.");
        return verb.Text;
    }

    // ── Helpers (unchanged) ───────────────────────────────────────

    private bool Match(TokenKind kind) {
        if (!Check(kind)) return false;
        Advance();
        return true;
    }

    private Token Consume(TokenKind kind, string message) {
        if (Check(kind)) return Advance();
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
