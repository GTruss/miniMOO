
using miniMOO.Engine.Verbs;

namespace miniMOO.Engine.BuiltinVerbs;

public interface IBuiltinVerb {
    string Name { get; }

    Task<VerbResult> ExecuteAsync(VerbContext context);
}
