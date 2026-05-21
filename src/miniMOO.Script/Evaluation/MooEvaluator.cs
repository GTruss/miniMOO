using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Script.Ast;

namespace miniMOO.Script.Evaluation;

public sealed class MooEvaluator {
    private readonly ScriptContext _context;

    public MooEvaluator(ScriptContext context) {
        _context = context;
    }

    public async Task<ScriptResult> ExecuteAsync(ProgramNode program) {
        MooValue? lastValue = null;

        foreach (var statement in program.Statements)
            lastValue = await ExecuteStatementAsync(statement);

        return ScriptResult.Success(lastValue);
    }

    private async Task<MooValue?> ExecuteStatementAsync(StatementNode statement)
        => statement switch {
            ExpressionStatementNode expressionStatement =>
                await EvaluateExpressionAsync(expressionStatement.Expression),

            _ => throw new MooEvaluationException(
                $"Unsupported statement node: {statement.GetType().Name}")
        };

    private async Task<MooValue?> EvaluateExpressionAsync(ExpressionNode expression)
        => expression switch {
            IdentifierExpressionNode identifier =>
                EvaluateIdentifier(identifier),

            StringLiteralExpressionNode literal =>
                new MooValue.String(literal.Value),

            IntegerLiteralExpressionNode literal =>
                new MooValue.Integer(literal.Value),

            ObjectLiteralExpressionNode literal =>
                new MooValue.Object(new ObjectId(checked((int)literal.Value))),

            PropertyAccessExpressionNode property =>
                await EvaluatePropertyAccessAsync(property),

            VerbCallExpressionNode verbCall =>
                await EvaluateVerbCallAsync(verbCall),

            FunctionCallExpressionNode functionCall =>
                await EvaluateFunctionCallAsync(functionCall),

            SpliceExpressionNode splice =>
                throw new MooEvaluationException(
                    "Splice expressions can only be used in argument lists."),

            _ => throw new MooEvaluationException(
                $"Unsupported expression node: {expression.GetType().Name}")
        };

    private MooValue EvaluateIdentifier(IdentifierExpressionNode identifier)
        => identifier.Name.ToLowerInvariant() switch {
            "player" => new MooValue.Object(_context.PlayerId),
            "this" => new MooValue.Object(_context.ThisId),
            "argstr" => new MooValue.String(_context.ArgStr),
            "args" => new MooValue.List(_context.Args),
            "verb" => new MooValue.String(_context.Verb),
            _ => throw new MooEvaluationException(
                $"Unknown variable: {identifier.Name}")
        };

    private async Task<MooValue?> EvaluatePropertyAccessAsync(
        PropertyAccessExpressionNode property) {

        var target = await EvaluateExpressionAsync(property.Target);

        if (target is not MooValue.Object obj)
            throw new MooEvaluationException(
                $"Cannot read property '{property.PropertyName}' from non-object value.");

        return _context.World.GetProperty(obj.Value, property.PropertyName)
            ?? MooValue.NothingValue;
    }

    private async Task<MooValue?> EvaluateVerbCallAsync(
        VerbCallExpressionNode verbCall) {

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

    private async Task<IReadOnlyList<MooValue>> EvaluateArgumentsAsync(IReadOnlyList<ExpressionNode> arguments) {

        var values = new List<MooValue>();

        foreach (var argument in arguments) {
            if (argument is SpliceExpressionNode splice) {
                var splicedValue = await EvaluateExpressionAsync(splice.Expression);

                if (splicedValue is not MooValue.List list)
                    throw new MooEvaluationException(
                        "Splice expression must evaluate to a list.");

                values.AddRange(list.Items);
                continue;
            }

            var value = await EvaluateExpressionAsync(argument);
            values.Add(value ?? MooValue.NothingValue);
        }

        return values;
    }

    private async Task<MooValue?> EvaluateFunctionCallAsync(FunctionCallExpressionNode functionCall) {

        var args = await EvaluateArgumentsAsync(functionCall.Arguments);

        return functionCall.FunctionName.ToLowerInvariant() switch {
            "tostr" => new MooValue.String(string.Concat(args.Select(arg => arg.ToString()))),
            "notify" => await NotifyAsync(args),
            _ => throw new MooEvaluationException(
                $"Unknown function: {functionCall.FunctionName}")
        };
    }

    private async Task<MooValue> NotifyAsync(IReadOnlyList<MooValue> args) {
        if (args.Count == 0)
            throw new MooEvaluationException("notify() requires an object argument.");

        if (args[0] is not MooValue.Object target)
            throw new MooEvaluationException("notify() first argument must be an object.");

        await _context.World.NotifyAsync(target.Value, args.Skip(1).ToList());

        return MooValue.NothingValue;
    }
}