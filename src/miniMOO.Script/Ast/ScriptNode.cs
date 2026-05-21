namespace miniMOO.Script.Ast;

public abstract record ScriptNode;

public sealed record ProgramNode(
    IReadOnlyList<StatementNode> Statements) : ScriptNode;

public abstract record StatementNode : ScriptNode;

public sealed record ExpressionStatementNode(
    ExpressionNode Expression) : StatementNode;

public abstract record ExpressionNode : ScriptNode;

public sealed record IdentifierExpressionNode(
    string Name) : ExpressionNode;

public sealed record StringLiteralExpressionNode(
    string Value) : ExpressionNode;

public sealed record IntegerLiteralExpressionNode(
    long Value) : ExpressionNode;

public sealed record ObjectLiteralExpressionNode(
    long Value) : ExpressionNode;

public sealed record PropertyAccessExpressionNode(
    ExpressionNode Target,
    string PropertyName) : ExpressionNode;

public sealed record VerbCallExpressionNode(
    ExpressionNode Target,
    string VerbName,
    IReadOnlyList<ExpressionNode> Arguments) : ExpressionNode;

public sealed record FunctionCallExpressionNode(
    string FunctionName,
    IReadOnlyList<ExpressionNode> Arguments) : ExpressionNode;

public sealed record SpliceExpressionNode(
    ExpressionNode Expression) : ExpressionNode;