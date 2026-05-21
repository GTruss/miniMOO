namespace miniMOO.Script.Ast;

public enum BinaryOp {
    Add, Subtract, Multiply, Divide, Modulo,
    Equal, NotEqual,
    Less, LessEqual, Greater, GreaterEqual,
    And, Or
}

public enum UnaryOp { Not, Negate }

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

public sealed record BinaryExpressionNode(
    ExpressionNode Left,
    BinaryOp Op,
    ExpressionNode Right) : ExpressionNode;

public sealed record UnaryExpressionNode(
    UnaryOp Op,
    ExpressionNode Operand) : ExpressionNode;

public sealed record AssignmentExpressionNode(
    string Variable,
    ExpressionNode Value) : ExpressionNode;

public sealed record IfBranchNode(
    ExpressionNode Condition,
    IReadOnlyList<StatementNode> Body);

public sealed record IfStatementNode(
    IReadOnlyList<IfBranchNode> Branches,
    IReadOnlyList<StatementNode>? ElseBranch) : StatementNode;

public sealed record ForStatementNode(
    string Variable,
    ExpressionNode Iterable,
    IReadOnlyList<StatementNode> Body) : StatementNode;

public sealed record ReturnStatementNode(
    ExpressionNode? Value) : StatementNode; 