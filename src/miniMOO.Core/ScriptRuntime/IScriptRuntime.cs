namespace miniMOO.Core.ScriptRuntime;

public interface IScriptRuntime {
    Task<ScriptResult> ExecuteAsync(ScriptContext context, string script);
}
