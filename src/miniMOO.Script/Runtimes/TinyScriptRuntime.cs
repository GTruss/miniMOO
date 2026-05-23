using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Script.Ast;
using miniMOO.Script.Evaluation;
using miniMOO.Script.Lexing;
using miniMOO.Script.Parsing;

namespace miniMOO.Script.Runtimes;

public sealed class TinyScriptRuntime : IScriptRuntime {
    //private readonly Dictionary<string, ProgramNode> _cache = new();

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
            return ScriptResult.Failure(FormatSourceError(script, ex.Line, ex.Column, ex.Message));
        }
        catch (MooLexException ex) {
            return ScriptResult.Failure(ex.Message);
        }
        catch (MooParseException ex) {
            return ScriptResult.Failure(FormatSourceError(script, ex.Token.Line, ex.Token.Column, ex.Message));
        }
        catch (MooEvaluationException ex) {
            return ScriptResult.Failure(ex.Message);
        }
        catch (MooScriptException ex) {
            return ScriptResult.Failure(FormatScriptError(script, ex));
        }
    } 
    private static bool IsDebugEnabled(ScriptContext context)
        => context.World.GetProperty(context.PlayerId, "debug") is MooValue.Integer i
        && i.Value == 1;

    private static string FormatSourceError(string script, int line, int column, string message) {
        var lines = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        if (line < 1 || line > lines.Length)
            return message;

        var sourceLine = lines[line - 1];
        var caretColumn = Math.Max(1, column);

        return string.Join(Environment.NewLine, [
            message,
        $"{line,4}: {sourceLine}",
        $"      {new string(' ', caretColumn - 1)}^"
        ]);
    }

    private static string FormatScriptError(string script, MooScriptException ex) {
        var sourceText = ex.SourceText ?? script;

        var message = ex.Line is not null && ex.Column is not null
            ? FormatSourceError(sourceText, ex.Line.Value, ex.Column.Value, ex.Message)
            : ex.Message;

        if (ex.Trace.Count == 0)
            return message;

        var lines = new List<string> { message };

        foreach (var frame in ex.Trace)
            lines.Add(frame.Description);

        lines.Add("(End of traceback)");
        return string.Join(Environment.NewLine, lines);
    }
}
