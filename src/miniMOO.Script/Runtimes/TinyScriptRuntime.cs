using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Script.Evaluation;
using miniMOO.Script.Lexing;
using miniMOO.Script.Parsing;

namespace miniMOO.Script.Runtimes;

public sealed class TinyScriptRuntime : IScriptRuntime {
    public async Task<ScriptResult> ExecuteAsync(ScriptContext context, string script) {
        try {
            var tokens = new MooLexer(script).Lex();
            var program = new MooParser(tokens).ParseProgram();

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
    }   
}
