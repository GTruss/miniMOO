namespace miniMOO.Script.Ast;

public enum BinaryOp {
    Add, Subtract, Multiply, Divide, Modulo,
    Equal, NotEqual,
    Less, LessEqual, Greater, GreaterEqual,
    And, Or, In
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
    string PropertyName,
    int Line,
    int Column) : ExpressionNode;

public sealed record VerbCallExpressionNode(
    ExpressionNode Target,
    string VerbName,
    IReadOnlyList<ExpressionNode> Arguments) : ExpressionNode;

public sealed record DynamicVerbCallExpressionNode(
    ExpressionNode Target,
    ExpressionNode VerbName,
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

public sealed record WhileStatementNode(
    ExpressionNode Condition,
    IReadOnlyList<StatementNode> Body) : StatementNode;

public sealed record IndexExpressionNode(
    ExpressionNode Target,
    ExpressionNode Index) : ExpressionNode;

public sealed record SliceExpressionNode(
    ExpressionNode Target,
    ExpressionNode From,
    ExpressionNode To) : ExpressionNode;

public sealed record ListLiteralExpressionNode(
    IReadOnlyList<ExpressionNode> Items) : ExpressionNode;

public sealed record PropertyAssignmentExpressionNode(
    ExpressionNode Target,
    string PropertyName,
    ExpressionNode Value) : ExpressionNode;

public sealed record IndexedAssignmentExpressionNode(
    ExpressionNode Target,
    ExpressionNode Index,
    ExpressionNode Value) : ExpressionNode;

public sealed record RangeForStatementNode(
    string Variable,
    ExpressionNode From,
    ExpressionNode To,
    IReadOnlyList<StatementNode> Body) : StatementNode;

public sealed record DestructuringSlotNode(
    string Name,
    bool IsOptional,
    bool IsRest,
    ExpressionNode? DefaultValue) : ScriptNode;

public sealed record DestructuringAssignmentNode(
    IReadOnlyList<DestructuringSlotNode> Slots,
    ExpressionNode Value) : ExpressionNode;

public sealed record TryStatementNode(
    IReadOnlyList<StatementNode> Body,
    IReadOnlyList<ExceptClauseNode> Clauses) : StatementNode;

public sealed record ExceptClauseNode(
    string? Variable,
    IReadOnlyList<ExpressionNode> Codes,
    IReadOnlyList<StatementNode> Body);

public sealed record BacktickExpressionNode(
    ExpressionNode Expression,
    IReadOnlyList<ExpressionNode> ErrorCodes,
    ExpressionNode? DefaultValue) : ExpressionNode;

public sealed record ConditionalExpressionNode(
    ExpressionNode Condition,
    ExpressionNode TrueExpression,
    ExpressionNode FalseExpression) : ExpressionNode;

public sealed record LastIndexExpressionNode() : ExpressionNode;

public sealed record DynamicPropertyAccessExpressionNode(
    ExpressionNode Target,
    ExpressionNode PropertyName,
    int Line,
    int Column) : ExpressionNode;

public sealed record DynamicPropertyAssignmentExpressionNode(
    ExpressionNode Target,
    ExpressionNode PropertyName,
    ExpressionNode Value) : ExpressionNode;

public sealed record SliceAssignmentExpressionNode(
    ExpressionNode Target,
    ExpressionNode From,
    ExpressionNode To,
    ExpressionNode Value) : ExpressionNode;

public sealed record FloatLiteralExpressionNode(
    double Value) : ExpressionNode;
