using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Script.Ast;

namespace miniMOO.Script.Evaluation;

internal sealed class MooReturnException : Exception {
    public MooValue? Value { get; }
    public MooReturnException(MooValue? value) : base("Script return") => Value = value;
}

public sealed class MooEvaluator {
    private readonly ScriptContext _context;
    private readonly Dictionary<string, MooValue> _locals = new(StringComparer.OrdinalIgnoreCase);

    public MooEvaluator(ScriptContext context) {
        _context = context;
    }

    public async Task<ScriptResult> ExecuteAsync(ProgramNode program) {
        try {
            MooValue? lastValue = null;

            foreach (var statement in program.Statements)
                lastValue = await ExecuteStatementAsync(statement);

            return ScriptResult.Success(lastValue);
        }
        catch (MooReturnException ret) {
            return ScriptResult.Success(ret.Value);
        }
    }

    // ── Statements ────────────────────────────────────────────────

    private async Task<MooValue?> ExecuteStatementAsync(StatementNode statement) {
        switch (statement) {
            case ExpressionStatementNode expr:
                return await EvaluateExpressionAsync(expr.Expression);

            case IfStatementNode ifStmt:
                return await ExecuteIfStatementAsync(ifStmt);

            case ForStatementNode forStmt:
                return await ExecuteForStatementAsync(forStmt);

            case ReturnStatementNode ret: {
                    var value = ret.Value is not null
                        ? await EvaluateExpressionAsync(ret.Value)
                        : null;
                    throw new MooReturnException(value);
                }

            default:
                throw new MooEvaluationException(
                    $"Unsupported statement node: {statement.GetType().Name}");
        }
    }

    private async Task<MooValue?> ExecuteIfStatementAsync(IfStatementNode ifStmt) {
        foreach (var branch in ifStmt.Branches) {
            var condition = await EvaluateExpressionAsync(branch.Condition);

            if (!IsTruthy(condition))
                continue;

            MooValue? result = null;
            foreach (var stmt in branch.Body)
                result = await ExecuteStatementAsync(stmt);
            return result;
        }

        if (ifStmt.ElseBranch is { } elseBranch) {
            MooValue? result = null;
            foreach (var stmt in elseBranch)
                result = await ExecuteStatementAsync(stmt);
            return result;
        }

        return null;
    }

    private async Task<MooValue?> ExecuteForStatementAsync(ForStatementNode forStmt) {
        var iterable = await EvaluateExpressionAsync(forStmt.Iterable);

        if (iterable is not MooValue.List list)
            throw new MooEvaluationException(
                $"'for' requires a list, got {iterable?.GetType().Name ?? "nothing"}.");

        MooValue? result = null;

        foreach (var item in list.Items) {
            _locals[forStmt.Variable] = item;
            foreach (var stmt in forStmt.Body)
                result = await ExecuteStatementAsync(stmt);
        }

        return result;
    }

    // ── Expressions ───────────────────────────────────────────────

    private async Task<MooValue?> EvaluateExpressionAsync(ExpressionNode expression) {
        switch (expression) {
            case AssignmentExpressionNode assignment: {
                    var value = await EvaluateExpressionAsync(assignment.Value);
                    _locals[assignment.Variable] = value ?? MooValue.NothingValue;
                    return _locals[assignment.Variable];
                }

            case BinaryExpressionNode binary:
                return await EvaluateBinaryAsync(binary);

            case UnaryExpressionNode unary:
                return await EvaluateUnaryAsync(unary);

            case IdentifierExpressionNode identifier:
                return EvaluateIdentifier(identifier);

            case StringLiteralExpressionNode literal:
                return new MooValue.String(literal.Value);

            case IntegerLiteralExpressionNode literal:
                return new MooValue.Integer(literal.Value);

            case ObjectLiteralExpressionNode literal:
                return new MooValue.Object(new ObjectId(checked((int)literal.Value)));

            case PropertyAccessExpressionNode property:
                return await EvaluatePropertyAccessAsync(property);

            case VerbCallExpressionNode verbCall:
                return await EvaluateVerbCallAsync(verbCall);

            case FunctionCallExpressionNode functionCall:
                return await EvaluateFunctionCallAsync(functionCall);

            case SpliceExpressionNode:
                throw new MooEvaluationException(
                    "Splice expressions can only be used in argument lists.");

            default:
                throw new MooEvaluationException(
                    $"Unsupported expression node: {expression.GetType().Name}");
        }
    }

    private async Task<MooValue> EvaluateBinaryAsync(BinaryExpressionNode binary) {
        // Short-circuit logical operators
        if (binary.Op == BinaryOp.And) {
            var left = await EvaluateExpressionAsync(binary.Left);
            if (!IsTruthy(left)) return new MooValue.Integer(0);
            var right = await EvaluateExpressionAsync(binary.Right);
            return new MooValue.Integer(IsTruthy(right) ? 1 : 0);
        }

        if (binary.Op == BinaryOp.Or) {
            var left = await EvaluateExpressionAsync(binary.Left);
            if (IsTruthy(left)) return new MooValue.Integer(1);
            var right = await EvaluateExpressionAsync(binary.Right);
            return new MooValue.Integer(IsTruthy(right) ? 1 : 0);
        }

        var leftVal = await EvaluateExpressionAsync(binary.Left) ?? MooValue.NothingValue;
        var rightVal = await EvaluateExpressionAsync(binary.Right) ?? MooValue.NothingValue;

        return binary.Op switch {
            BinaryOp.Add => Add(leftVal, rightVal),
            BinaryOp.Subtract => ArithInt(leftVal, rightVal, (a, b) => a - b, "subtract"),
            BinaryOp.Multiply => ArithInt(leftVal, rightVal, (a, b) => a * b, "multiply"),
            BinaryOp.Divide => ArithInt(leftVal, rightVal, (a, b) =>
                b == 0 ? throw new MooEvaluationException("Division by zero.") : a / b, "divide"),
            BinaryOp.Modulo => ArithInt(leftVal, rightVal, (a, b) =>
                b == 0 ? throw new MooEvaluationException("Modulo by zero.") : a % b, "modulo"),

            BinaryOp.Equal => new MooValue.Integer(MooEqual(leftVal, rightVal) ? 1 : 0),
            BinaryOp.NotEqual => new MooValue.Integer(MooEqual(leftVal, rightVal) ? 0 : 1),
            BinaryOp.Less => Compare(leftVal, rightVal, (a, b) => a < b),
            BinaryOp.LessEqual => Compare(leftVal, rightVal, (a, b) => a <= b),
            BinaryOp.Greater => Compare(leftVal, rightVal, (a, b) => a > b),
            BinaryOp.GreaterEqual => Compare(leftVal, rightVal, (a, b) => a >= b),

            _ => throw new MooEvaluationException($"Unknown binary op: {binary.Op}")
        };
    }

    private async Task<MooValue> EvaluateUnaryAsync(UnaryExpressionNode unary) {
        var operand = await EvaluateExpressionAsync(unary.Operand) ?? MooValue.NothingValue;

        return unary.Op switch {
            UnaryOp.Not => new MooValue.Integer(IsTruthy(operand) ? 0 : 1),
            UnaryOp.Negate => operand is MooValue.Integer i
                ? new MooValue.Integer(-i.Value)
                : throw new MooEvaluationException("Unary negation requires an integer."),
            _ => throw new MooEvaluationException($"Unknown unary op: {unary.Op}")
        };
    }

    private MooValue EvaluateIdentifier(IdentifierExpressionNode identifier) {
        var name = identifier.Name.ToLowerInvariant();

        if (_locals.TryGetValue(name, out var local))
            return local;

        return name switch {
            "player" => new MooValue.Object(_context.PlayerId),
            "this" => new MooValue.Object(_context.ThisId),
            "verb" => new MooValue.String(_context.Verb),
            "argstr" => new MooValue.String(_context.ArgStr),
            "args" => new MooValue.List(_context.Args),
            "dobjstr" => new MooValue.String(_context.DobjStr),
            "iobjstr" => new MooValue.String(_context.IobjStr),
            "dobj" => _context.DirectObjectId is { } dobjId
                ? new MooValue.Object(dobjId)
                : MooValue.NothingValue,
            "iobj" => _context.IndirectObjectId is { } iobjId
                ? new MooValue.Object(iobjId)
                : MooValue.NothingValue,
            _ => throw new MooEvaluationException($"Unknown variable: {identifier.Name}")
        };
    }

    private async Task<MooValue?> EvaluatePropertyAccessAsync(
        PropertyAccessExpressionNode property) {

        var target = await EvaluateExpressionAsync(property.Target);

        if (target is not MooValue.Object obj)
            throw new MooEvaluationException(
                $"Cannot read property '{property.PropertyName}' from non-object value.");

        return _context.World.GetProperty(obj.Value, property.PropertyName)
            ?? MooValue.NothingValue;
    }

    private async Task<MooValue?> EvaluateVerbCallAsync(VerbCallExpressionNode verbCall) {
        var target = await EvaluateExpressionAsync(verbCall.Target);

        if (target is not MooValue.Object obj)
            throw new MooEvaluationException(
                $"Cannot call verb '{verbCall.VerbName}' on non-object value.");

        var args = await EvaluateArgumentsAsync(verbCall.Arguments);
        var result = await _context.World.InvokeVerbAsync(_context, obj.Value, verbCall.VerbName, args);

        if (!result.IsSuccess)
            throw new MooEvaluationException(result.Error ?? "Verb call failed.");

        return result.Value;
    }

    private async Task<IReadOnlyList<MooValue>> EvaluateArgumentsAsync(
        IReadOnlyList<ExpressionNode> arguments) {

        var values = new List<MooValue>();

        foreach (var argument in arguments) {
            if (argument is SpliceExpressionNode splice) {
                var splicedValue = await EvaluateExpressionAsync(splice.Expression);

                if (splicedValue is not MooValue.List list)
                    throw new MooEvaluationException("Splice expression must evaluate to a list.");

                values.AddRange(list.Items);
                continue;
            }

            var value = await EvaluateExpressionAsync(argument);
            values.Add(value ?? MooValue.NothingValue);
        }

        return values;
    }

    private async Task<MooValue?> EvaluateFunctionCallAsync(
        FunctionCallExpressionNode functionCall) {

        var name = functionCall.FunctionName.ToLowerInvariant();

        if (name == "pass")
            return await PassAsync(await EvaluateArgumentsAsync(functionCall.Arguments));

        var args = await EvaluateArgumentsAsync(functionCall.Arguments);

        return name switch {
            "notify" => await NotifyAsync(args),
            "tostr" => new MooValue.String(string.Concat(args.Select(MooToString))),
            "str" => new MooValue.String(args.Count > 0 ? MooToString(args[0]) : ""),
            "valid" => Valid(args),
            "length" => Length(args),
            "typeof" => TypeOf(args),
            _ => throw new MooEvaluationException($"Unknown function: {functionCall.FunctionName}")
        };
    }

    private async Task<MooValue?> PassAsync(IReadOnlyList<MooValue> args) {
        if (_context.DefiningObjectId is not { } definingId)
            throw new MooEvaluationException("pass() called outside a verb context.");

        var definingObj = _context.World.Get(definingId);

        if (definingObj?.ParentId is not { } parentId)
            throw new MooEvaluationException(
                $"pass() failed: #{definingId.Value} has no parent.");

        var result = await _context.World.InvokeVerbAsync(
            _context, _context.ThisId, _context.Verb, args, searchFromId: parentId);

        if (!result.IsSuccess)
            throw new MooEvaluationException(result.Error ?? "pass() failed.");

        return result.Value;
    }

    private async Task<MooValue> NotifyAsync(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object target)
            throw new MooEvaluationException("notify() first argument must be an object.");

        await _context.World.NotifyAsync(target.Value, args.Skip(1).ToList());
        return MooValue.NothingValue;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static MooValue Valid(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object obj)
            return new MooValue.Integer(0);
        return new MooValue.Integer(obj.Value.Value >= 0 ? 1 : 0);
    }

    private static MooValue Length(IReadOnlyList<MooValue> args) {
        if (args.Count == 0)
            throw new MooEvaluationException("length() requires an argument.");
        return args[0] switch {
            MooValue.String s => new MooValue.Integer(s.Value.Length),
            MooValue.List l => new MooValue.Integer(l.Items.Count),
            _ => throw new MooEvaluationException("length() requires a string or list.")
        };
    }

    private static MooValue TypeOf(IReadOnlyList<MooValue> args) {
        if (args.Count == 0)
            throw new MooEvaluationException("typeof() requires an argument.");
        // MOO type constants: 0=int, 1=obj, 2=str, 4=list, 9=float
        return args[0] switch {
            MooValue.Integer => new MooValue.Integer(0),
            MooValue.Object => new MooValue.Integer(1),
            MooValue.String => new MooValue.Integer(2),
            MooValue.List => new MooValue.Integer(4),
            MooValue.Float => new MooValue.Integer(9),
            _ => new MooValue.Integer(-1)
        };
    }

    private static MooValue Add(MooValue left, MooValue right) => (left, right) switch {
        (MooValue.Integer l, MooValue.Integer r) => new MooValue.Integer(l.Value + r.Value),
        (MooValue.String l, MooValue.String r) => new MooValue.String(l.Value + r.Value),
        (MooValue.String l, _) => new MooValue.String(l.Value + MooToString(right)),
        (_, MooValue.String r) => new MooValue.String(MooToString(left) + r.Value),
        _ => throw new MooEvaluationException(
            $"Cannot add {left.GetType().Name} and {right.GetType().Name}.")
    };

    private static MooValue ArithInt(MooValue left, MooValue right,
        Func<long, long, long> op, string opName) {

        if (left is not MooValue.Integer l || right is not MooValue.Integer r)
            throw new MooEvaluationException(
                $"Cannot {opName} {left.GetType().Name} and {right.GetType().Name}.");
        return new MooValue.Integer(op(l.Value, r.Value));
    }

    private static MooValue Compare(MooValue left, MooValue right, Func<long, long, bool> intOp) {
        if (left is MooValue.Integer li && right is MooValue.Integer ri)
            return new MooValue.Integer(intOp(li.Value, ri.Value) ? 1 : 0);

        if (left is MooValue.String ls && right is MooValue.String rs) {
            var cmp = string.Compare(ls.Value, rs.Value, StringComparison.Ordinal);
            return new MooValue.Integer(intOp(cmp, 0) ? 1 : 0);
        }

        throw new MooEvaluationException(
            $"Cannot compare {left.GetType().Name} and {right.GetType().Name}.");
    }

    private static bool MooEqual(MooValue left, MooValue right) => (left, right) switch {
        (MooValue.Integer l, MooValue.Integer r) => l.Value == r.Value,
        (MooValue.String l, MooValue.String r) =>
            string.Equals(l.Value, r.Value, StringComparison.OrdinalIgnoreCase),
        (MooValue.Object l, MooValue.Object r) => l.Value == r.Value,
        (MooValue.Nothing, MooValue.Nothing) => true,
        _ => false
    };

    private static bool IsTruthy(MooValue? value) => value switch {
        null => false,
        MooValue.Nothing => false,
        MooValue.Clear => false,
        MooValue.Integer i => i.Value != 0,
        MooValue.String s => s.Value.Length > 0,
        MooValue.Object o => o.Value.Value >= 0,  // #-1 is nothing, falsy
        MooValue.List l => l.Items.Count > 0,
        _ => true
    };

    private static string MooToString(MooValue value) => value switch {
        MooValue.Nothing => "",
        MooValue.Integer i => i.Value.ToString(),
        MooValue.String s => s.Value,
        MooValue.Object o => $"#{o.Value.Value}",
        MooValue.List l => "{" + string.Join(", ", l.Items.Select(MooToString)) + "}",
        MooValue.Float f => f.Value.ToString("G"),
        _ => ""
    };
}