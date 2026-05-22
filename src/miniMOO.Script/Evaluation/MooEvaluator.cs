using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Script.Ast;

namespace miniMOO.Script.Evaluation;

internal sealed class MooReturnException : Exception {
    public MooValue? Value { get; }
    public MooReturnException(MooValue? value) : base("Script return") => Value = value;
}
public sealed class MooScriptException : Exception {
    public int ErrorCode { get; }
    public MooScriptException(int code, string message) : base(message) => ErrorCode = code;
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

            case RangeForStatementNode rangeFor:
                return await ExecuteRangeForStatementAsync(rangeFor);

            case WhileStatementNode whileStmt:
                return await ExecuteWhileStatementAsync(whileStmt);

            case ReturnStatementNode ret: {
                    var value = ret.Value is not null
                        ? await EvaluateExpressionAsync(ret.Value)
                        : null;
                    throw new MooReturnException(value);
                }

            case TryStatementNode tryStmt:
                return await ExecuteTryStatementAsync(tryStmt);

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

    private async Task<MooValue?> ExecuteRangeForStatementAsync(RangeForStatementNode rangeFor) {
        var fromVal = await EvaluateExpressionAsync(rangeFor.From);
        var toVal = await EvaluateExpressionAsync(rangeFor.To);

        if (fromVal is not MooValue.Integer fromInt || toVal is not MooValue.Integer toInt)
            throw new MooEvaluationException("Range bounds must be integers.");

        MooValue? result = null;
        for (var i = fromInt.Value; i <= toInt.Value; i++) {
            _locals[rangeFor.Variable] = new MooValue.Integer(i);
            foreach (var stmt in rangeFor.Body)
                result = await ExecuteStatementAsync(stmt);
        }
        return result;
    }

    private async Task<MooValue?> ExecuteWhileStatementAsync(WhileStatementNode whileStmt) {
        MooValue? result = null;
        while (IsTruthy(await EvaluateExpressionAsync(whileStmt.Condition)))
            foreach (var stmt in whileStmt.Body)
                result = await ExecuteStatementAsync(stmt);
        return result;
    }

    private async Task<MooValue?> ExecuteTryStatementAsync(TryStatementNode tryStmt) {
        try {
            MooValue? result = null;
            foreach (var stmt in tryStmt.Body)
                result = await ExecuteStatementAsync(stmt);
            return result;
        }
        catch (MooScriptException ex) {
            foreach (var clause in tryStmt.Clauses) {
                var matches = false;
                foreach (var codeExpr in clause.Codes) {
                    var codeVal = await EvaluateExpressionAsync(codeExpr);
                    if (codeVal is MooValue.Integer codeInt
                        && (codeInt.Value == MooErrorCode.Any || codeInt.Value == ex.ErrorCode)) {
                        matches = true;
                        break;
                    }
                }
                if (clause.Codes.Count == 0) matches = true; // no codes = catch any

                if (!matches) continue;

                if (clause.Variable is not null)
                    _locals[clause.Variable] = new MooValue.Integer(ex.ErrorCode);

                MooValue? result = null;
                foreach (var stmt in clause.Body)
                    result = await ExecuteStatementAsync(stmt);
                return result;
            }
            throw; // no clause matched
        }
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

            case IndexExpressionNode indexExpr:
                return await EvaluateIndexAsync(indexExpr);

            case PropertyAccessExpressionNode property:
                return await EvaluatePropertyAccessAsync(property);

            case PropertyAssignmentExpressionNode propAssign:
                return await EvaluatePropertyAssignmentAsync(propAssign);

            case DestructuringAssignmentNode destruct:
                return await EvaluateDestructuringAsync(destruct);

            case VerbCallExpressionNode verbCall:
                return await EvaluateVerbCallAsync(verbCall);

            case FunctionCallExpressionNode functionCall:
                return await EvaluateFunctionCallAsync(functionCall);

            case ListLiteralExpressionNode listLit:
                return await EvaluateListLiteralAsync(listLit);

            case BacktickExpressionNode backtick:
                return await EvaluateBacktickAsync(backtick);

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
            BinaryOp.In => InOperator(leftVal, rightVal),

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
            "here" => _context.World.GetProperty(_context.PlayerId, "location") ?? MooValue.NothingValue,
            "e_none" => new MooValue.Integer(MooErrorCode.E_NONE),
            "e_type" => new MooValue.Integer(MooErrorCode.E_TYPE),
            "e_div" => new MooValue.Integer(MooErrorCode.E_DIV),
            "e_perm" => new MooValue.Integer(MooErrorCode.E_PERM),
            "e_propnf" => new MooValue.Integer(MooErrorCode.E_PROPNF),
            "e_verbnf" => new MooValue.Integer(MooErrorCode.E_VERBNF),
            "e_varnf" => new MooValue.Integer(MooErrorCode.E_VARNF),
            "e_invind" => new MooValue.Integer(MooErrorCode.E_INVIND),
            "e_range" => new MooValue.Integer(MooErrorCode.E_RANGE),
            "e_args" => new MooValue.Integer(MooErrorCode.E_ARGS),
            "e_invarg" => new MooValue.Integer(MooErrorCode.E_INVARG),
            "any" => new MooValue.Integer(MooErrorCode.Any),
            _ => throw new MooEvaluationException($"Unknown variable: {identifier.Name}")
        };
    }

    private async Task<MooValue?> EvaluateIndexAsync(IndexExpressionNode indexExpr) {
        var target = await EvaluateExpressionAsync(indexExpr.Target) ?? MooValue.NothingValue;
        var index = await EvaluateExpressionAsync(indexExpr.Index) ?? MooValue.NothingValue;

        if (index is not MooValue.Integer idx)
            throw new MooEvaluationException("Index must be an integer.");

        var i = (int)idx.Value;

        if (target is MooValue.List list) {
            if (i < 1 || i > list.Items.Count)
                throw new MooEvaluationException(
                    $"List index {i} out of range (length {list.Items.Count}).");
            return list.Items[i - 1];
        }

        if (target is MooValue.String str) {
            if (i < 1 || i > str.Value.Length)
                throw new MooEvaluationException(
                    $"String index {i} out of range (length {str.Value.Length}).");
            return new MooValue.String(str.Value[i - 1].ToString());
        }

        throw new MooEvaluationException("Index operator requires a list or string.");
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

    private async Task<MooValue?> EvaluatePropertyAssignmentAsync(
        PropertyAssignmentExpressionNode propAssign) {

        var target = await EvaluateExpressionAsync(propAssign.Target);
        if (target is not MooValue.Object obj)
            throw new MooEvaluationException($"Cannot write property '{propAssign.PropertyName}' on non-object value.");

        var value = await EvaluateExpressionAsync(propAssign.Value);
        _context.World.SetProperty(obj.Value, propAssign.PropertyName, value);

        return value;
    }

    private async Task<MooValue?> EvaluateDestructuringAsync(DestructuringAssignmentNode destruct) {
        var value = await EvaluateExpressionAsync(destruct.Value) ?? MooValue.NothingValue;

        if (value is not MooValue.List list)
            throw new MooEvaluationException("Destructuring requires a list on the right side.");

        for (var i = 0; i < destruct.Variables.Count; i++) {
            _locals[destruct.Variables[i]] = i < list.Items.Count
                ? list.Items[i]
                : MooValue.NothingValue;
        }

        return value;
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

    private async Task<MooValue> EvaluateListLiteralAsync(ListLiteralExpressionNode listLit) {
        var items = new List<MooValue>();
        foreach (var item in listLit.Items) {
            if (item is SpliceExpressionNode splice) {
                var spliced = await EvaluateExpressionAsync(splice.Expression);
                if (spliced is not MooValue.List list)
                    throw new MooEvaluationException("Splice in list literal must be a list.");
                items.AddRange(list.Items);
            }
            else {
                items.Add(await EvaluateExpressionAsync(item) ?? MooValue.NothingValue);
            }
        }
        return new MooValue.List(items);
    }

    private async Task<MooValue?> EvaluateBacktickAsync(BacktickExpressionNode backtick) {
        try {
            return await EvaluateExpressionAsync(backtick.Expression);
        }
        catch (MooScriptException ex) {
            foreach (var codeExpr in backtick.ErrorCodes) {
                var codeVal = await EvaluateExpressionAsync(codeExpr);

                if (codeVal is MooValue.Integer codeInt
                    && (codeInt.Value == MooErrorCode.Any || codeInt.Value == ex.ErrorCode))
                    return await EvaluateExpressionAsync(backtick.DefaultValue);
            }
            throw;
        }
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
            "parent" => Parent(args),
            "children" => Children(args),
            "setadd" => SetAdd(args),
            "listappend" => ListAppend(args),
            "create" => Create(args),
            "move" => Move(args),
            "set_name" => SetName(args),
            "index" => Index(args),
            "substr" => Substr(args),
            "add_alias" => AddAlias(args),
            "add_verb" => AddVerbBuiltin(args),
            "verb_info" => VerbInfo(args),
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

    private MooValue Parent(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object obj)
            throw new MooEvaluationException("parent() requires an object argument.");

        var target = _context.World.Get(obj.Value);
        if (target?.ParentId is { } parentId)
            return new MooValue.Object(parentId);

        return new MooValue.Object(new ObjectId(-1)); // $nothing
    }

    private MooValue Children(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object obj)
            throw new MooEvaluationException("children() requires an object argument.");

        var kids = _context.World.GetChildren(obj.Value);
        return new MooValue.List(kids.Select(id => (MooValue)new MooValue.Object(id)).ToList());
    }

    private static MooValue SetAdd(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.List list)
            throw new MooEvaluationException("setadd() requires a list and a value.");

        var item = args[1];
        if (list.Items.Any(x => MooEqual(x, item))) return list;

        return new MooValue.List(list.Items.Append(item).ToList());
    }

    private static MooValue ListAppend(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.List list)
            throw new MooEvaluationException("listappend() requires a list and a value.");

        return new MooValue.List(list.Items.Append(args[1]).ToList());
    }

    private MooValue Create(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object parent)
            throw new MooEvaluationException("create() requires an object argument.");

        return new MooValue.Object(_context.World.CreateObject(parent.Value, _context.PlayerId));
    }

    private MooValue Move(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.Object obj || args[1] is not MooValue.Object dest)
            throw new MooEvaluationException("move() requires two object arguments.");

        _context.World.MoveObject(obj.Value, dest.Value);
        return MooValue.NothingValue;
    }

    private MooValue SetName(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.Object obj || args[1] is not MooValue.String name)
            throw new MooEvaluationException("set_name() requires an object and a string.");

        _context.World.SetObjectName(obj.Value, name.Value);
        return MooValue.NothingValue;
    }

    private static MooValue Index(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.String haystack || args[1] is not MooValue.String needle)
            throw new MooEvaluationException("index() requires two string arguments.");

        var pos = haystack.Value.IndexOf(needle.Value, StringComparison.Ordinal);
        return new MooValue.Integer(pos >= 0 ? pos + 1 : 0); // 1-based, 0 = not found
    }

    private static MooValue Substr(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.String str || args[1] is not MooValue.Integer startArg)
            throw new MooEvaluationException("substr() requires a string and a start index.");

        var start = Math.Max(0, (int)startArg.Value - 1); // 1-based → 0-based

        if (start >= str.Value.Length) return new MooValue.String("");

        if (args.Count >= 3 && args[2] is MooValue.Integer lenArg) {
            var len = Math.Min((int)lenArg.Value, str.Value.Length - start);
            return new MooValue.String(str.Value.Substring(start, Math.Max(0, len)));
        }

        return new MooValue.String(str.Value[start..]);
    }

    private MooValue AddAlias(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.Object obj || args[1] is not MooValue.String alias)
            throw new MooEvaluationException("add_alias() requires an object and a string.");

        _context.World.AddAlias(obj.Value, alias.Value);
        return MooValue.NothingValue;
    }

    private MooValue AddVerbBuiltin(IReadOnlyList<MooValue> args) {
        if (args.Count < 3
            || args[0] is not MooValue.Object obj
            || args[1] is not MooValue.String names
            || args[2] is not MooValue.String script)
            throw new MooEvaluationException("add_verb() requires an object, a name string, and a script string.");

        _context.World.AddVerb(obj.Value, names.Value, script.Value, _context.PlayerId);
        return MooValue.NothingValue;
    }

    private MooValue VerbInfo(IReadOnlyList<MooValue> args) {
        if (args.Count < 2
            || args[0] is not MooValue.Object obj
            || args[1] is not MooValue.String name)
            throw new MooEvaluationException("verb_info() requires an object and a verb name.");

        return _context.World.GetVerbInfo(obj.Value, name.Value)
            ?? throw new MooScriptException(MooErrorCode.E_VERBNF, $"Verb not found: {name.Value}");
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

    private static MooValue InOperator(MooValue left, MooValue right) {
        if (right is not MooValue.List list)
            throw new MooEvaluationException("'in' requires a list on the right side.");

        for (int i = 0; i < list.Items.Count; i++)
            if (MooEqual(list.Items[i], left))
                return new MooValue.Integer(i + 1); // 1-based

        return new MooValue.Integer(0);
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