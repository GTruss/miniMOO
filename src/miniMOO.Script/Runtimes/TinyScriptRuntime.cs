using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Script.Ast;
using miniMOO.Script.Evaluation;
using miniMOO.Script.Lexing;
using miniMOO.Script.Parsing;

namespace miniMOO.Script.Runtimes;

public sealed class TinyScriptRuntime : IScriptRuntime {
    //private readonly Dictionary<string, ProgramNode> _cache = new();

    private static string SourceLabel(ScriptContext context)
        => context.DefiningObjectId is { } defining
            ? $"{defining}:{context.Verb}"
            : $"{context.ThisId}:{context.Verb}";

    public async Task<ScriptResult> ExecuteAsync(ScriptContext context, string script) {
        try {

            if (IsDebugEnabled(context)) {
                await context.World.NotifyAsync(
                    context.PlayerId,
                    [new MooValue.String(LexerDebug.Dump(script))]);
            }

            //if (!_cache.TryGetValue(script, out var program)) {
                var tokens = new MooLexer(script).Lex();
                var program = new MooParser(tokens).ParseProgram();
                //_cache[script] = program;
            //}

            return await new MooEvaluator(context).ExecuteAsync(program);
        }
        catch (MooLexException ex) when (ex.Line > 0) {
            return ScriptResult.Failure(new ScriptError(
                ex.Message,
                Line: ex.Line,
                Column: ex.Column,
                SourceText: script,            
                SourceLabel: SourceLabel(context)));
        }
        catch (MooLexException ex) {
            return ScriptResult.Failure(ex.Message);
        }
        catch (MooParseException ex) {
            return ScriptResult.Failure(new ScriptError(
                ex.Message,
                Line: ex.Token.Line,
                Column: ex.Token.Column,
                SourceText: script,
                SourceLabel: SourceLabel(context)));
        }
        catch (MooEvaluationException ex) {
            return ScriptResult.Failure(ex.Message);
        }
        catch (MooScriptException ex) {
            return ScriptResult.Failure(new ScriptError(
                ex.Message,
                ErrorCode: ex.ErrorCode,
                Line: ex.Line,
                Column: ex.Column,
                SourceText: ex.SourceText ?? script,
                SourceLabel: ex.SourceLabel ?? SourceLabel(context),
                Trace: ex.Trace));
        }
    } 

    private static bool IsDebugEnabled(ScriptContext context)
        => context.World.GetProperty(context.PlayerId, "debug") is MooValue.Integer i
        && i.Value == 1;
}
