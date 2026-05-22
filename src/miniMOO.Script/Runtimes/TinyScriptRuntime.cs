using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Script.Ast;
using miniMOO.Script.Evaluation;
using miniMOO.Script.Lexing;
using miniMOO.Script.Parsing;

namespace miniMOO.Script.Runtimes;

public sealed class TinyScriptRuntime : IScriptRuntime {
    private readonly Dictionary<string, ProgramNode> _cache = new();

    public async Task<ScriptResult> ExecuteAsync(ScriptContext context, string script) {
        try {

            if (IsDebugEnabled(context)) {
                await context.World.NotifyAsync(
                    context.PlayerId,
                    [new MooValue.String(LexerDebug.Dump(script))]);
            }

            if (!_cache.TryGetValue(script, out var program)) {
                var tokens = new MooLexer(script).Lex();
                program = new MooParser(tokens).ParseProgram();
                _cache[script] = program;
            }

            return await new MooEvaluator(context).ExecuteAsync(program);
        }
        catch (MooLexException ex) {
            return ScriptResult.Failure(ex.Message);
        }
        catch (MooParseException ex) {
            return ScriptResult.Failure(ex.Message);
        }
        catch (MooEvaluationException ex) {
            return ScriptResult.Failure(ex.Message);
        }
        catch (MooScriptException ex) {
            return ScriptResult.Failure(ex.Message);
        }
    } 
    private static bool IsDebugEnabled(ScriptContext context)
        => context.World.GetProperty(context.PlayerId, "debug") is MooValue.Integer i
        && i.Value == 1;
}
