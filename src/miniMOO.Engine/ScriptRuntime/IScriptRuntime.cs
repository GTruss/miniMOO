using miniMOO.Engine.Verbs;

namespace miniMOO.Engine.ScriptRuntime;

public interface IScriptRuntime {
    Task<VerbResult> ExecuteAsync(VerbContext context, string script);
}
