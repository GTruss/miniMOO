using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Script.Ast;
using miniMOO.Script.Lexing;
using miniMOO.Script.Parsing;

using System.Text;
using System.Text.RegularExpressions;

namespace miniMOO.Script.Evaluation;

internal sealed class MooReturnException : Exception {
    public MooValue? Value { get; }
    public MooReturnException(MooValue? value) : base("Script return") => Value = value;
}

public sealed class MooScriptException : Exception {
    public int ErrorCode { get; }
    public int? Line { get; }
    public int? Column { get; }
    public List<ScriptTraceFrame> Trace { get; } = [];
    public string? SourceText { get; set; }
    public string? SourceLabel { get; set; }

    public MooScriptException(int code, string message, int? line = null, int? column = null)
        : base(message) {
        ErrorCode = code;
        Line = line;
        Column = column;
    }

    public MooScriptException(ScriptError error)
        : base(error.Message) {
            ErrorCode = error.ErrorCode ?? MooErrorCode.E_NONE;
            Line = error.Line;
            Column = error.Column;
            SourceText = error.SourceText;
            SourceLabel = error.SourceLabel;
            Trace.AddRange(error.Frames);
    }
}

public sealed class MooEvaluator {
    private readonly ScriptContext _context;
    private readonly Dictionary<string, MooValue> _locals = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<int> _indexLengths = new();

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
        Tick();

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
                throw MooError(MooErrorCode.E_TYPE,
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
            throw MooError(MooErrorCode.E_TYPE, "Range bounds must be integers.");

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
                    if (MatchesErrorCode(codeVal, ex.ErrorCode)) {
                        matches = true;
                        break;
                    }
                }
                if (clause.Codes.Count == 0) matches = true; // no codes = catch any

                if (!matches) continue;

                if (clause.Variable is not null)
                    _locals[clause.Variable] = new MooValue.Error(ex.ErrorCode);

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
        Tick();

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

            case ConditionalExpressionNode conditional:
                return await EvaluateConditionalAsync(conditional);

            case IdentifierExpressionNode identifier:
                return EvaluateIdentifier(identifier);

            case StringLiteralExpressionNode literal:
                return new MooValue.String(literal.Value);

            case IntegerLiteralExpressionNode literal:
                return new MooValue.Integer(literal.Value);

            case ObjectLiteralExpressionNode literal:
                return new MooValue.Object(new ObjectId(checked((int)literal.Value)));

            case LastIndexExpressionNode:
                if (_indexLengths.Count == 0)
                    throw MooError(MooErrorCode.E_INVIND, "'$' is only valid inside an index or slice.");

                return new MooValue.Integer(_indexLengths.Peek());

            case IndexExpressionNode indexExpr:
                return await EvaluateIndexAsync(indexExpr);

            case SliceExpressionNode sliceExpr:
                return await EvaluateSliceAsync(sliceExpr);

            case PropertyAccessExpressionNode property:
                return await EvaluatePropertyAccessAsync(property);

            case PropertyAssignmentExpressionNode propAssign:
                return await EvaluatePropertyAssignmentAsync(propAssign);

            case IndexedAssignmentExpressionNode indexedAssign:
                return await EvaluateIndexedAssignmentAsync(indexedAssign);

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

            case DynamicPropertyAccessExpressionNode property:
                return await EvaluateDynamicPropertyAccessAsync(property);

            case DynamicPropertyAssignmentExpressionNode propAssign:
                return await EvaluateDynamicPropertyAssignmentAsync(propAssign);

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
            var left = await EvaluateExpressionAsync(binary.Left) ?? MooValue.NothingValue;

            if (!IsTruthy(left))
                return left;

            return await EvaluateExpressionAsync(binary.Right) ?? MooValue.NothingValue;
        }

        if (binary.Op == BinaryOp.Or) {
            var left = await EvaluateExpressionAsync(binary.Left) ?? MooValue.NothingValue;

            if (IsTruthy(left))
                return left;

            return await EvaluateExpressionAsync(binary.Right) ?? MooValue.NothingValue;
        }

        var leftVal = await EvaluateExpressionAsync(binary.Left) ?? MooValue.NothingValue;
        var rightVal = await EvaluateExpressionAsync(binary.Right) ?? MooValue.NothingValue;

        return binary.Op switch {
            BinaryOp.Add => Add(leftVal, rightVal),
            BinaryOp.Subtract => ArithNumeric(leftVal, rightVal, (a, b) => a - b, (a, b) => a - b, "subtract"),
            BinaryOp.Multiply => ArithNumeric(leftVal, rightVal, (a, b) => a * b, (a, b) => a * b, "multiply"),
            BinaryOp.Divide => DivideNumeric(leftVal, rightVal),
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

    private async Task<MooValue?> EvaluateConditionalAsync(ConditionalExpressionNode conditional) {
        var condition = await EvaluateExpressionAsync(conditional.Condition);

        return IsTruthy(condition)
            ? await EvaluateExpressionAsync(conditional.TrueExpression)
            : await EvaluateExpressionAsync(conditional.FalseExpression);
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
            "e_none" => new MooValue.Error(MooErrorCode.E_NONE),
            "e_type" => new MooValue.Error(MooErrorCode.E_TYPE),
            "e_div" => new MooValue.Error(MooErrorCode.E_DIV),
            "e_perm" => new MooValue.Error(MooErrorCode.E_PERM),
            "e_propnf" => new MooValue.Error(MooErrorCode.E_PROPNF),
            "e_verbnf" => new MooValue.Error(MooErrorCode.E_VERBNF),
            "e_varnf" => new MooValue.Error(MooErrorCode.E_VARNF),
            "e_invind" => new MooValue.Error(MooErrorCode.E_INVIND),
            "e_range" => new MooValue.Error(MooErrorCode.E_RANGE),
            "e_args" => new MooValue.Error(MooErrorCode.E_ARGS),
            "e_invarg" => new MooValue.Error(MooErrorCode.E_INVARG),
            "e_recmove" => new MooValue.Error(MooErrorCode.E_RECMOVE),
            "e_maxrec" => new MooValue.Error(MooErrorCode.E_MAXREC),
            "e_nacc" => new MooValue.Error(MooErrorCode.E_NACC),
            "e_quota" => new MooValue.Error(MooErrorCode.E_QUOTA),
            "e_float" => new MooValue.Error(MooErrorCode.E_FLOAT),
            "any" => new MooValue.Integer(MooErrorCode.Any),
            "int" => new MooValue.Integer(0),
            "obj" => new MooValue.Integer(1),
            "str" => new MooValue.Integer(2),
            "err" => new MooValue.Integer(3),
            "list" => new MooValue.Integer(4),
            "float" => new MooValue.Integer(9),
            _ => throw MooError(MooErrorCode.E_VARNF, $"Unknown variable: {identifier.Name}")
        };
    }

    private async Task<MooValue?> EvaluateIndexAsync(IndexExpressionNode indexExpr) {
        var target = await EvaluateExpressionAsync(indexExpr.Target) ?? MooValue.NothingValue;

        var length = target switch {
            MooValue.List list1 => list1.Items.Count,
            MooValue.String str1 => str1.Value.Length,
            _ => throw MooError(MooErrorCode.E_TYPE, "Index operator requires a list or string.")
        };

        MooValue index;
        _indexLengths.Push(length);
        try {
            index = await EvaluateExpressionAsync(indexExpr.Index) ?? MooValue.NothingValue;
        }
        finally {
            _indexLengths.Pop();
        }

        if (index is not MooValue.Integer idx)
            throw MooError(MooErrorCode.E_TYPE, "Index must be an integer.");

        var i = (int)idx.Value;

        if (target is MooValue.List list) {
            if (i < 1 || i > list.Items.Count)
                throw MooError(MooErrorCode.E_RANGE,
                    $"List index {i} out of range (length {list.Items.Count}).");
            return list.Items[i - 1];
        }

        if (target is MooValue.String str) {
            if (i < 1 || i > str.Value.Length)
                throw MooError(MooErrorCode.E_RANGE,
                    $"String index {i} out of range (length {str.Value.Length}).");
            return new MooValue.String(str.Value[i - 1].ToString());
        }

        throw MooError(MooErrorCode.E_TYPE, "Index operator requires a list or string.");
    }

    private async Task<MooValue?> EvaluateSliceAsync(SliceExpressionNode sliceExpr) {
        var target = await EvaluateExpressionAsync(sliceExpr.Target) ?? MooValue.NothingValue;

        var length = target switch {
            MooValue.List list1 => list1.Items.Count,
            MooValue.String str1 => str1.Value.Length,
            _ => throw MooError(MooErrorCode.E_TYPE, "Slice operator requires a list or string.")
        };

        MooValue from;
        MooValue to;

        _indexLengths.Push(length);
        try {
            from = await EvaluateExpressionAsync(sliceExpr.From) ?? MooValue.NothingValue;
            to = await EvaluateExpressionAsync(sliceExpr.To) ?? MooValue.NothingValue;
        }
        finally {
            _indexLengths.Pop();
        }

        if (from is not MooValue.Integer fromIndex || to is not MooValue.Integer toIndex)
            throw MooError(MooErrorCode.E_TYPE, "Slice indexes must be integers.");

        var start = (int)fromIndex.Value;
        var end = (int)toIndex.Value;

        if (target is MooValue.String str)
            return SliceString(str.Value, start, end);

        if (target is MooValue.List list)
            return SliceList(list.Items, start, end);

        throw MooError(MooErrorCode.E_TYPE, "Slice operator requires a list or string.");
    }

    private static MooValue.String SliceString(string value, int start, int end) {
        if (start < 1 || start > value.Length + 1)
            throw MooError(MooErrorCode.E_RANGE,
                $"String slice start {start} out of range (length {value.Length}).");

        if (end < 0 || end > value.Length)
            throw MooError(MooErrorCode.E_RANGE,
                $"String slice end {end} out of range (length {value.Length}).");

        if (end < start)
            return new MooValue.String("");

        return new MooValue.String(value.Substring(start - 1, end - start + 1));
    }

    private static MooValue.List SliceList(IReadOnlyList<MooValue> items, int start, int end) {
        if (start < 1 || start > items.Count + 1)
            throw MooError(MooErrorCode.E_RANGE,
                $"List slice start {start} out of range (length {items.Count}).");

        if (end < 0 || end > items.Count)
            throw MooError(MooErrorCode.E_RANGE,
                $"List slice end {end} out of range (length {items.Count}).");

        if (end < start)
            return new MooValue.List([]);

        return new MooValue.List(items.Skip(start - 1).Take(end - start + 1).ToList());
    }

    private async Task<MooValue?> EvaluatePropertyAccessAsync(PropertyAccessExpressionNode property) {

        var target = await EvaluateExpressionAsync(property.Target);

        if (target is not MooValue.Object obj)
            throw MooError(MooErrorCode.E_TYPE,
                $"Cannot read property '{property.PropertyName}' from non-object value.");

        var value = _context.World.GetProperty(obj.Value, property.PropertyName);

        if (value is null)
            throw new MooScriptException(
                MooErrorCode.E_PROPNF,
                "Property not found",
                property.Line,
                property.Column);

        return value;
    }

    private async Task<MooValue?> EvaluatePropertyAssignmentAsync(
        PropertyAssignmentExpressionNode propAssign) {

        var target = await EvaluateExpressionAsync(propAssign.Target);
        if (target is not MooValue.Object obj)
            throw MooError(MooErrorCode.E_TYPE, $"Cannot write property '{propAssign.PropertyName}' on non-object value.");

        var value = await EvaluateExpressionAsync(propAssign.Value);
        _context.World.SetProperty(obj.Value, propAssign.PropertyName, value);

        return value;
    }

    private async Task<MooValue?> EvaluateIndexedAssignmentAsync(
        IndexedAssignmentExpressionNode assignment) {

        var target = await EvaluateExpressionAsync(assignment.Target) ?? MooValue.NothingValue;
        var index = await EvaluateExpressionAsync(assignment.Index) ?? MooValue.NothingValue;
        var value = await EvaluateExpressionAsync(assignment.Value) ?? MooValue.NothingValue;

        if (index is not MooValue.Integer idx)
            throw MooError(MooErrorCode.E_TYPE, "Indexed assignment requires an integer index.");

        var updatedTarget = AssignIndexedValue(target, (int)idx.Value, value);
        await WriteAssignableTargetAsync(assignment.Target, updatedTarget);

        return value;
    }

    private async Task WriteAssignableTargetAsync(ExpressionNode target, MooValue value) {
        switch (target) {
            case IdentifierExpressionNode identifier:
                _locals[identifier.Name.ToLowerInvariant()] = value;
                return;

            case PropertyAccessExpressionNode property: {
                    var objValue = await EvaluateExpressionAsync(property.Target);

                    if (objValue is not MooValue.Object obj)
                        throw MooError(MooErrorCode.E_TYPE,
                            $"Cannot write property '{property.PropertyName}' on non-object value.");

                    _context.World.SetProperty(obj.Value, property.PropertyName, value);
                    return;
                }

            case IndexExpressionNode index: {
                    var container = await EvaluateExpressionAsync(index.Target) ?? MooValue.NothingValue;
                    var indexValue = await EvaluateExpressionAsync(index.Index) ?? MooValue.NothingValue;

                    if (indexValue is not MooValue.Integer idx)
                        throw MooError(MooErrorCode.E_TYPE, "Indexed assignment requires an integer index.");

                    var updatedContainer = AssignIndexedValue(container, (int)idx.Value, value);
                    await WriteAssignableTargetAsync(index.Target, updatedContainer);
                    return;
                }

            default:
                throw MooError(MooErrorCode.E_INVARG, "Invalid indexed assignment target.");
        }
    }

    private static MooValue AssignIndexedValue(MooValue target, int index, MooValue value) {
        if (target is MooValue.List list) {
            if (index < 1 || index > list.Items.Count)
                throw MooError(MooErrorCode.E_RANGE,
                    $"List assignment index {index} out of range (length {list.Items.Count}).");

            var items = list.Items.ToList();
            items[index - 1] = value;
            return new MooValue.List(items);
        }

        if (target is MooValue.String str) {
            if (value is not MooValue.String replacement || replacement.Value.Length != 1)
                throw MooError(MooErrorCode.E_TYPE, "String indexed assignment requires a one-character string.");

            if (index < 1 || index > str.Value.Length)
                throw MooError(MooErrorCode.E_RANGE,
                    $"String assignment index {index} out of range (length {str.Value.Length}).");

            var chars = str.Value.ToCharArray();
            chars[index - 1] = replacement.Value[0];
            return new MooValue.String(new string(chars));
        }

        throw MooError(MooErrorCode.E_TYPE, "Indexed assignment requires a list or string.");
    }

    private async Task<MooValue?> EvaluateDestructuringAsync(DestructuringAssignmentNode destruct) {
        var value = await EvaluateExpressionAsync(destruct.Value) ?? MooValue.NothingValue;

        if (value is not MooValue.List list)
            throw MooError(MooErrorCode.E_TYPE, "Destructuring requires a list on the right side.");

        for (var i = 0; i < destruct.Slots.Count; i++) {
            var slot = destruct.Slots[i];

            if (slot.IsRest) {
                _locals[slot.Name] = new MooValue.List(list.Items.Skip(i).ToList());
                break;
            }

            if (i < list.Items.Count) {
                _locals[slot.Name] = list.Items[i];
                continue;
            }

            _locals[slot.Name] = slot.DefaultValue is not null
                ? await EvaluateExpressionAsync(slot.DefaultValue) ?? MooValue.NothingValue
                : MooValue.NothingValue;
        }

        return value;
    }

    private async Task<MooValue?> EvaluateVerbCallAsync(VerbCallExpressionNode verbCall) {
        var target = await EvaluateExpressionAsync(verbCall.Target);

        if (target is not MooValue.Object obj)
            throw MooError(MooErrorCode.E_TYPE,
                $"Cannot call verb '{verbCall.VerbName}' on non-object value.");

        var args = await EvaluateArgumentsAsync(verbCall.Arguments);
        var result = await _context.World.InvokeVerbAsync(_context, obj.Value, verbCall.VerbName, args);

        if (!result.IsSuccess) {
            if (result.ErrorDetail is not null)
                throw new MooScriptException(result.ErrorDetail);

            throw new MooEvaluationException("Verb call failed.");
        }

        return result.Value;
    }

    private async Task<IReadOnlyList<MooValue>> EvaluateArgumentsAsync(
        IReadOnlyList<ExpressionNode> arguments) {

        var values = new List<MooValue>();

        foreach (var argument in arguments) {
            if (argument is SpliceExpressionNode splice) {
                var splicedValue = await EvaluateExpressionAsync(splice.Expression);

                if (splicedValue is not MooValue.List list)
                    throw MooError(MooErrorCode.E_TYPE, "Splice expression must evaluate to a list.");

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
                    throw MooError(MooErrorCode.E_TYPE, "Splice in list literal must be a list.");
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

                if (MatchesErrorCode(codeVal, ex.ErrorCode))
                    return await EvaluateExpressionAsync(backtick.DefaultValue);
            }
            throw;
        }
    }

    private async Task<MooValue?> EvaluateDynamicPropertyAccessAsync(
        DynamicPropertyAccessExpressionNode property) {

        var target = await EvaluateExpressionAsync(property.Target);
        if (target is not MooValue.Object obj)
            throw MooError(MooErrorCode.E_TYPE, "Cannot read dynamic property from non-object value.");

        var propName = await EvaluatePropertyNameAsync(property.PropertyName);

        var value = _context.World.GetProperty(obj.Value, propName);
        if (value is null)
            throw new MooScriptException(
                MooErrorCode.E_PROPNF,
                "Property not found",
                property.Line,
                property.Column);

        return value;
    }

    private async Task<MooValue?> EvaluateDynamicPropertyAssignmentAsync(
        DynamicPropertyAssignmentExpressionNode assignment) {

        var target = await EvaluateExpressionAsync(assignment.Target);
        if (target is not MooValue.Object obj)
            throw MooError(MooErrorCode.E_TYPE, "Cannot write dynamic property on non-object value.");

        var propName = await EvaluatePropertyNameAsync(assignment.PropertyName);
        var value = await EvaluateExpressionAsync(assignment.Value) ?? MooValue.NothingValue;

        _context.World.SetProperty(obj.Value, propName, value);
        return value;
    }
     
    private async Task<string> EvaluatePropertyNameAsync(ExpressionNode expression) {
        var value = await EvaluateExpressionAsync(expression) ?? MooValue.NothingValue;

        return value switch {
            MooValue.String s => s.Value,
            _ => throw MooError(MooErrorCode.E_TYPE, "Dynamic property name must be a string.")
        };
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
            "eval" => await EvalAsync(args),
            "valid" => Valid(args),
            "is_player" => IsPlayer(args),
            "length" => Length(args),
            "typeof" => TypeOf(args),
            "parent" => Parent(args),
            "children" => Children(args),
            "setadd" => SetAdd(args),
            "setremove" => SetRemove(args),
            "listappend" => ListAppend(args),
            "listdelete" => ListDelete(args),
            "create" => Create(args),
            "move" => await MoveAsync(args),
            "set_name" => SetName(args),
            "index" => Index(args),
            "substr" => Substr(args),
            "add_alias" => AddAlias(args),
            "add_verb" => AddVerbBuiltin(args),
            "verb_info" => VerbInfo(args),
            "match" => Match(args),
            "rmatch" => RMatch(args),
            "ticks_left" => TicksLeft(args),
            "seconds_left" => SecondsLeft(args),
            "toliteral" => ToLiteral(args),
            "toint" => ToInt(args),
            "tonum" => ToInt(args),
            "toobj" => ToObj(args),
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

        if (!result.IsSuccess) {
            if (result.ErrorDetail is not null)
                throw new MooScriptException(result.ErrorDetail);

            throw new MooEvaluationException("pass() failed.");
        }

        return result.Value;
    }

    private async Task<MooValue> NotifyAsync(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object target)
            throw MooError(MooErrorCode.E_TYPE, "notify() first argument must be an object.");

        await _context.World.NotifyAsync(target.Value, args.Skip(1).ToList());
        return MooValue.NothingValue;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private async Task<MooValue> EvalAsync(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.String source)
            throw MooError(MooErrorCode.E_TYPE, "eval() requires a string argument.");

        try {
            var tokens = new MooLexer(source.Value).Lex();
            var program = new MooParser(tokens).ParseProgram();

            var evaluator = new MooEvaluator(_context);
            var result = await evaluator.ExecuteAsync(program);

            if (!result.IsSuccess) {
                if (result.ErrorDetail is not null)
                    throw new MooScriptException(result.ErrorDetail);

                throw new MooEvaluationException(result.Error ?? "eval() failed.");
            }

            return result.Value ?? MooValue.NothingValue;
        }
        catch (MooLexException ex) {
            throw new MooEvaluationException($"eval() lex error: {ex.Message}");
        }
        catch (MooParseException ex) {
            throw new MooEvaluationException($"eval() parse error: {ex.Message}");
        }
        catch (MooScriptException ex) {
            ex.SourceText ??= source.Value;
            ex.SourceLabel ??= "Input to EVAL";

            ex.Trace.Add(new ScriptTraceFrame(
                null,
                null,
                "eval",
                null,
                "... called from built-in function eval()"));
            throw;
        }
    }

    private static MooValue Valid(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object obj)
            return new MooValue.Integer(0);

        return new MooValue.Integer(obj.Value.Value >= 0 ? 1 : 0);
    }

    private MooValue IsPlayer(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object obj)
            throw new MooScriptException(MooErrorCode.E_TYPE, "is_player() requires an object argument.");

        var target = _context.World.Get(obj.Value);

        if (target is null)
            throw new MooScriptException(MooErrorCode.E_INVARG, "is_player() requires a valid object.");

        return new MooValue.Integer(target.Flags.HasFlag(ObjectFlags.User) ? 1 : 0);
    }

    private static MooValue Length(IReadOnlyList<MooValue> args) {
        if (args.Count == 0)
            throw MooError(MooErrorCode.E_ARGS, "length() requires an argument.");

        return args[0] switch {
            MooValue.String s => new MooValue.Integer(s.Value.Length),
            MooValue.List l => new MooValue.Integer(l.Items.Count),
            _ => throw MooError(MooErrorCode.E_TYPE, "length() requires a string or list.")
        };
    }

    private static MooValue TypeOf(IReadOnlyList<MooValue> args) {
        if (args.Count == 0)
            throw MooError(MooErrorCode.E_ARGS, "typeof() requires an argument.");

        // MOO type constants: 0=int, 1=obj, 2=str, 4=list, 9=float
        return args[0] switch {
            MooValue.Integer => new MooValue.Integer(0),
            MooValue.Object => new MooValue.Integer(1),
            MooValue.String => new MooValue.Integer(2),
            MooValue.List => new MooValue.Integer(4),
            MooValue.Float => new MooValue.Integer(9),
            MooValue.Error => new MooValue.Integer(3),
            _ => new MooValue.Integer(-1)
        };
    }

    private MooValue Parent(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object obj)
            throw MooError(MooErrorCode.E_TYPE, "parent() requires an object argument.");

        var target = _context.World.Get(obj.Value);
        if (target?.ParentId is { } parentId)
            return new MooValue.Object(parentId);

        return new MooValue.Object(new ObjectId(-1)); // $nothing
    }

    private MooValue Children(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object obj)
            throw MooError(MooErrorCode.E_TYPE, "children() requires an object argument.");

        var kids = _context.World.GetChildren(obj.Value);
        return new MooValue.List(kids.Select(id => (MooValue)new MooValue.Object(id)).ToList());
    }

    private static MooValue SetAdd(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.List list)
            throw MooError(MooErrorCode.E_TYPE, "setadd() requires a list and a value.");

        var item = args[1];
        if (list.Items.Any(x => MooEqual(x, item))) return list;

        return new MooValue.List(list.Items.Append(item).ToList());
    }

    private static MooValue SetRemove(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.List list)
            throw MooError(MooErrorCode.E_TYPE, "setremove() requires a list and a value.");

        var item = args[1];
        var result = list.Items.ToList();

        var index = result.FindIndex(x => MooEqual(x, item));

        if (index >= 0)
            result.RemoveAt(index);

        return new MooValue.List(result);
    }

    private static MooValue ListAppend(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.List list)
            throw MooError(MooErrorCode.E_TYPE, "listappend() requires a list and a value.");

        return new MooValue.List(list.Items.Append(args[1]).ToList());
    }

    private static MooValue ListDelete(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.List list || args[1] is not MooValue.Integer index)
            throw new MooScriptException(MooErrorCode.E_TYPE, "listdelete() requires a list and an integer index.");

        var zeroBasedIndex = (int)index.Value - 1;

        if (zeroBasedIndex < 0 || zeroBasedIndex >= list.Items.Count)
            throw new MooScriptException(MooErrorCode.E_RANGE, "listdelete() index out of range.");

        var result = list.Items.ToList();
        result.RemoveAt(zeroBasedIndex);

        return new MooValue.List(result);
    }

    private MooValue Create(IReadOnlyList<MooValue> args) {
        if (args.Count == 0 || args[0] is not MooValue.Object parent)
            throw MooError(MooErrorCode.E_TYPE, "create() requires an object argument.");

        return new MooValue.Object(_context.World.CreateObject(parent.Value, _context.PlayerId));
    }

    private async Task<MooValue> MoveAsync(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.Object obj || args[1] is not MooValue.Object dest)
            throw new MooScriptException(MooErrorCode.E_TYPE, "move() requires two object arguments.");

        await _context.World.MoveObjectAsync(_context, obj.Value, dest.Value);
        return MooValue.NothingValue;
    }

    private MooValue SetName(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.Object obj || args[1] is not MooValue.String name)
            throw MooError(MooErrorCode.E_TYPE, "set_name() requires an object and a string.");

        _context.World.SetObjectName(obj.Value, name.Value);
        return MooValue.NothingValue;
    }

    private static MooValue Index(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.String haystack || args[1] is not MooValue.String needle)
            throw MooError(MooErrorCode.E_TYPE, "index() requires two string arguments.");

        var pos = haystack.Value.IndexOf(needle.Value, StringComparison.Ordinal);
        return new MooValue.Integer(pos >= 0 ? pos + 1 : 0); // 1-based, 0 = not found
    }

    private static MooValue Substr(IReadOnlyList<MooValue> args) {
        if (args.Count < 2 || args[0] is not MooValue.String str || args[1] is not MooValue.Integer startArg)
            throw MooError(MooErrorCode.E_TYPE, "substr() requires a string and a start index.");

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
            throw MooError(MooErrorCode.E_TYPE, "add_alias() requires an object and a string.");

        _context.World.AddAlias(obj.Value, alias.Value);
        return MooValue.NothingValue;
    }

    private MooValue AddVerbBuiltin(IReadOnlyList<MooValue> args) {
        if (args.Count < 3
            || args[0] is not MooValue.Object obj
            || args[1] is not MooValue.String names
            || args[2] is not MooValue.String script)
            throw MooError(MooErrorCode.E_TYPE, "add_verb() requires an object, a name string, and a script string.");

        _context.World.AddVerb(obj.Value, names.Value, script.Value, _context.PlayerId);
        return MooValue.NothingValue;
    }

    private MooValue VerbInfo(IReadOnlyList<MooValue> args) {
        if (args.Count < 2
            || args[0] is not MooValue.Object obj
            || args[1] is not MooValue.String name)
            throw MooError(MooErrorCode.E_TYPE, "verb_info() requires an object and a verb name.");

        return _context.World.GetVerbInfo(obj.Value, name.Value)
            ?? throw new MooScriptException(MooErrorCode.E_VERBNF, $"Verb not found: {name.Value}");
    }

    private static MooValue Match(IReadOnlyList<MooValue> args)
        => RegexMatch(args, findLast: false);

    private static MooValue RMatch(IReadOnlyList<MooValue> args)
        => RegexMatch(args, findLast: true);

    private static MooValue RegexMatch(IReadOnlyList<MooValue> args, bool findLast) {
        if (args.Count < 2 ||
            args[0] is not MooValue.String subject ||
            args[1] is not MooValue.String pattern)
            throw MooError(MooErrorCode.E_TYPE, "match()/rmatch() requires two string arguments.");

        var caseMatters = args.Count >= 3 && IsTruthy(args[2]);
        var dotnetPattern = ConvertMooRegex(pattern.Value);

        var options = caseMatters
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;

        Match match;

        try {
            if (findLast) {
                var regex = new Regex(@"\G(?:" + dotnetPattern + ")", options);
                Match? lastMatch = null;

                for (var start = 0; start <= subject.Value.Length; start++) {
                    var candidate = regex.Match(subject.Value, start);

                    if (candidate.Success)
                        lastMatch = candidate;
                }

                if (lastMatch is null)
                    return new MooValue.List([]);

                match = lastMatch;
            }
            else {
                match = Regex.Match(subject.Value, dotnetPattern, options);
            }
        }
        catch (ArgumentException ex) {
            throw MooError(MooErrorCode.E_INVARG, $"match()/rmatch() invalid pattern: {ex.Message}");
        }

        if (!match.Success)
            return new MooValue.List([]);

        var replacements = new List<MooValue>();

        for (var i = 1; i <= 9; i++) {
            if (i < match.Groups.Count && match.Groups[i].Success) {
                replacements.Add(new MooValue.List([
                    new MooValue.Integer(match.Groups[i].Index + 1),
                new MooValue.Integer(match.Groups[i].Index + match.Groups[i].Length)
                ]));
            }
            else {
                replacements.Add(new MooValue.List([
                    new MooValue.Integer(0),
                new MooValue.Integer(-1)
                ]));
            }
        }

        return new MooValue.List([
            new MooValue.Integer(match.Index + 1),
        new MooValue.Integer(match.Index + match.Length),
        new MooValue.List(replacements),
        subject
        ]);
    }

    private MooValue TicksLeft(IReadOnlyList<MooValue> args)
    => new MooValue.Integer(_context.Meter.TicksLeft);

    private MooValue SecondsLeft(IReadOnlyList<MooValue> args)
        => new MooValue.Float(_context.Meter.SecondsLeft);

    private static MooValue ToLiteral(IReadOnlyList<MooValue> args) {
        if (args.Count != 1)
            throw MooError(MooErrorCode.E_ARGS, "toliteral() requires exactly one argument.");

        return new MooValue.String(MooToLiteral(args[0]));
    }

    private static MooValue ToInt(IReadOnlyList<MooValue> args) {
        if (args.Count != 1)
            throw MooError(MooErrorCode.E_ARGS, "toint() requires exactly one argument.");

        return args[0] switch {
            MooValue.Integer i => i,
            MooValue.Float f => new MooValue.Integer((long)Math.Truncate(f.Value)),
            MooValue.Object o => new MooValue.Integer(o.Value.Value),
            MooValue.Error e => new MooValue.Integer(e.Code),
            MooValue.String s => new MooValue.Integer(ParseMooIntegerString(s.Value)),
            MooValue.Nothing => new MooValue.Integer(-1),
            MooValue.List => throw MooError(MooErrorCode.E_TYPE, "toint() cannot convert a list."),
            _ => new MooValue.Integer(0)
        };
    }

    private static MooValue ToObj(IReadOnlyList<MooValue> args) {
        if (args.Count != 1)
            throw MooError(MooErrorCode.E_ARGS, "toobj() requires exactly one argument.");

        return args[0] switch {
            MooValue.Object o => o,
            MooValue.Integer i => new MooValue.Object(new ObjectId(checked((int)i.Value))),
            MooValue.Float f => new MooValue.Object(new ObjectId(checked((int)Math.Truncate(f.Value)))),
            MooValue.Error e => new MooValue.Object(new ObjectId(e.Code)),
            MooValue.String s => new MooValue.Object(new ObjectId(checked((int)ParseMooObjectString(s.Value)))),
            MooValue.Nothing => new MooValue.Object(new ObjectId(-1)),
            MooValue.List => throw MooError(MooErrorCode.E_TYPE, "toobj() cannot convert a list."),
            _ => new MooValue.Object(new ObjectId(0))
        };
    }

    private static long ParseMooObjectString(string value) {
        var text = value.Trim();

        if (text.StartsWith("#", StringComparison.Ordinal))
            text = text[1..].TrimStart();

        return ParseMooIntegerString(text);
    }

    private static long ParseMooIntegerString(string value) {
        var normalized = Regex.Replace(value.Trim(), @"^([+-])\s+", "$1");

        if (!double.TryParse(
                normalized,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var number)) {
            return 0;
        }

        if (double.IsNaN(number) || double.IsInfinity(number))
            return 0;

        return (long)Math.Truncate(number);
    }

    private static string MooToLiteral(MooValue value) => value switch {
        MooValue.Nothing => "#-1",
        MooValue.Integer i => i.Value.ToString(),
        MooValue.Float f => f.Value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
        MooValue.String s => "\"" + EscapeMooStringLiteral(s.Value) + "\"",
        MooValue.Object o => $"#{o.Value.Value}",
        MooValue.Error e => MooErrorName(e.Code),
        MooValue.List l => "{" + string.Join(", ", l.Items.Select(MooToLiteral)) + "}",
        _ => ""
    };

    private static string EscapeMooStringLiteral(string value)
        => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

    private static string MooErrorName(int code) => code switch {
        MooErrorCode.E_NONE => "E_NONE",
        MooErrorCode.E_TYPE => "E_TYPE",
        MooErrorCode.E_DIV => "E_DIV",
        MooErrorCode.E_PERM => "E_PERM",
        MooErrorCode.E_PROPNF => "E_PROPNF",
        MooErrorCode.E_VERBNF => "E_VERBNF",
        MooErrorCode.E_VARNF => "E_VARNF",
        MooErrorCode.E_INVIND => "E_INVIND",
        MooErrorCode.E_RECMOVE => "E_RECMOVE",
        MooErrorCode.E_MAXREC => "E_MAXREC",
        MooErrorCode.E_RANGE => "E_RANGE",
        MooErrorCode.E_ARGS => "E_ARGS",
        MooErrorCode.E_NACC => "E_NACC",
        MooErrorCode.E_INVARG => "E_INVARG",
        MooErrorCode.E_QUOTA => "E_QUOTA",
        MooErrorCode.E_FLOAT => "E_FLOAT",
        _ => $"E_UNKNOWN({code})"
    };

    private static MooValue Add(MooValue left, MooValue right) => (left, right) switch {
        (MooValue.Integer l, MooValue.Integer r) => new MooValue.Integer(l.Value + r.Value),
        (MooValue.String l, MooValue.String r) => new MooValue.String(l.Value + r.Value),
        (MooValue.String l, _) => new MooValue.String(l.Value + MooToString(right)),
        (_, MooValue.String r) => new MooValue.String(MooToString(left) + r.Value),
        _ => throw MooError(MooErrorCode.E_TYPE,
            $"Cannot add {left.GetType().Name} and {right.GetType().Name}.")
    };

    private static MooValue ArithInt(MooValue left, MooValue right,
        Func<long, long, long> op, string opName) {

        if (left is not MooValue.Integer l || right is not MooValue.Integer r)
            throw MooError(MooErrorCode.E_TYPE,
                $"Cannot {opName} {left.GetType().Name} and {right.GetType().Name}.");

        return new MooValue.Integer(op(l.Value, r.Value));
    }

    private static MooValue ArithNumeric(MooValue left, MooValue right, Func<long, long, long> intOp, 
        Func<double, double, double> floatOp,
        string opName) {    

        if (left is MooValue.Integer li && right is MooValue.Integer ri)
            return new MooValue.Integer(intOp(li.Value, ri.Value));

        if (TryDouble(left, out var ld) && TryDouble(right, out var rd))
            return new MooValue.Float(floatOp(ld, rd));

        throw MooError(MooErrorCode.E_TYPE,
            $"Cannot {opName} {left.GetType().Name} and {right.GetType().Name}.");
    }

    private static MooValue DivideNumeric(MooValue left, MooValue right) {
        if (left is MooValue.Integer li && right is MooValue.Integer ri) {
            if (ri.Value == 0)
                throw MooError(MooErrorCode.E_DIV, "Division by zero.");

            return new MooValue.Integer(li.Value / ri.Value);
        }

        if (TryDouble(left, out var ld) && TryDouble(right, out var rd)) {
            if (rd == 0)
                throw MooError(MooErrorCode.E_DIV, "Division by zero.");

            return new MooValue.Float(ld / rd);
        }

        throw MooError(MooErrorCode.E_TYPE,
            $"Cannot divide {left.GetType().Name} and {right.GetType().Name}.");
    }

    private static bool TryDouble(MooValue value, out double result) {
        switch (value) {
            case MooValue.Integer i:
                result = i.Value;
                return true;

            case MooValue.Float f:
                result = f.Value;
                return true;

            default:
                result = 0;
                return false;
        }
    }

    private static MooValue Compare(MooValue left, MooValue right, Func<long, long, bool> intOp) {
        if (left is MooValue.Integer li && right is MooValue.Integer ri)
            return new MooValue.Integer(intOp(li.Value, ri.Value) ? 1 : 0);

        if (left is MooValue.String ls && right is MooValue.String rs) {
            var cmp = string.Compare(ls.Value, rs.Value, StringComparison.Ordinal);
            return new MooValue.Integer(intOp(cmp, 0) ? 1 : 0);
        }

        throw MooError(MooErrorCode.E_TYPE,
            $"Cannot compare {left.GetType().Name} and {right.GetType().Name}.");
    }

    private static MooValue InOperator(MooValue left, MooValue right) {
        if (right is not MooValue.List list)
            throw MooError(MooErrorCode.E_TYPE, "'in' requires a list on the right side.");

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
        (MooValue.Error l, MooValue.Error r) => l.Code == r.Code,
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
        MooValue.Float f => f.Value.ToString("0.####"),
        _ => ""
    };

    private static string ConvertMooRegex(string pattern) {
        var result = new StringBuilder();

        for (var i = 0; i < pattern.Length; i++) {
            var ch = pattern[i];

            if (ch != '%') {
                result.Append(ch);
                continue;
            }

            if (i + 1 >= pattern.Length) {
                result.Append('%');
                continue;
            }

            var next = pattern[++i];

            result.Append(next switch {
                '(' => '(',
                ')' => ')',
                '|' => '|',
                '%' => "%",
                'w' => @"\w",
                'W' => @"\W",
                _ => Regex.Escape(next.ToString())
            });
        }

        return result.ToString();
    }

    private void Tick() {
        if (!_context.Meter.TryTick(out var error))
            throw new MooScriptException(MooErrorCode.E_QUOTA, error ?? "Task quota exceeded.");
    }

    private static MooScriptException MooError(int code, string message)
        => new(code, message);

    private static bool MatchesErrorCode(MooValue? value, int actualCode) => value switch {
        MooValue.Integer i => i.Value == MooErrorCode.Any || i.Value == actualCode,
        MooValue.Error e => e.Code == actualCode,
        _ => false
    };
}
